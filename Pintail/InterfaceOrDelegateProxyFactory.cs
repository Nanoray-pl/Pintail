using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    internal class InterfaceOrDelegateProxyFactory<Context>: IProxyFactory<Context>
    {
        private static readonly string TargetFieldName = "__Target";
        private static readonly string GlueFieldName = "__Glue";
        private static readonly string ProxyInfosFieldName = "__ProxyInfos";
        private static readonly MethodInfo UnproxyOrObtainProxyMethod = typeof(ProxyGlue<Context>).GetMethod(nameof(ProxyGlue<Context>.UnproxyOrObtainProxy))!;
        private static readonly MethodInfo MapArrayContentsMethod = typeof(ProxyGlue<Context>).GetMethod(nameof(ProxyGlue<Context>.MapArrayContents))!;
        private static readonly MethodInfo ProxyInfoListGetMethod = typeof(IList<ProxyInfo<Context>>).GetProperty("Item")!.GetGetMethod()!;
        private static readonly ConstructorInfo StringTypeDictionaryConstructor = typeof(Dictionary<string, Type>).GetConstructor(Array.Empty<Type>())!;
        private static readonly MethodInfo StringTypeDictionarySetItemMethod = typeof(IDictionary<string, Type>).GetMethod("set_Item")!;
        private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;

        public ProxyInfo<Context> ProxyInfo { get; private set; }
        private readonly ProxyManagerNoMatchingMethodHandler<Context> NoMatchingMethodHandler;
        private readonly ProxyManagerProxyPrepareBehavior ProxyPrepareBehavior;
        private readonly ProxyManagerEnumMappingBehavior EnumMappingBehavior;
        private readonly ProxyObjectInterfaceMarking ProxyObjectInterfaceMarking;
        private readonly ConditionalWeakTable<object, object> ProxyCache = new();
        private Type? BuiltProxyType;

        internal InterfaceOrDelegateProxyFactory(ProxyInfo<Context> proxyInfo, ProxyManagerNoMatchingMethodHandler<Context> noMatchingMethodHandler, ProxyManagerProxyPrepareBehavior proxyPrepareBehavior, ProxyManagerEnumMappingBehavior enumMappingBehavior, ProxyObjectInterfaceMarking proxyObjectInterfaceMarking)
        {
            bool isProxyDelegate = proxyInfo.Proxy.Type.IsAssignableTo(typeof(Delegate));
            bool isTargetDelegate = proxyInfo.Target.Type.IsAssignableTo(typeof(Delegate));
            if (isProxyDelegate || isTargetDelegate)
            {
                if (!isProxyDelegate)
                    throw new ArgumentException($"{proxyInfo.Proxy.Type.GetShortName()} is not a delegate type.");
                if (!isTargetDelegate)
                    throw new ArgumentException($"{proxyInfo.Target.Type.GetShortName()} is not a delegate type.");
            }
            else
            {
                if (!proxyInfo.Proxy.Type.IsInterface)
                    throw new ArgumentException($"{proxyInfo.Proxy.Type.GetShortName()} is not an interface.");
            }

            this.ProxyInfo = proxyInfo;
            this.NoMatchingMethodHandler = noMatchingMethodHandler;
            this.ProxyPrepareBehavior = proxyPrepareBehavior;
            this.EnumMappingBehavior = enumMappingBehavior;
            this.ProxyObjectInterfaceMarking = proxyObjectInterfaceMarking;
        }

        internal void Prepare(ProxyManager<Context> manager, string typeName)
        {
            // define proxy type
            TypeBuilder proxyBuilder = manager.ModuleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
            if (this.ProxyInfo.Proxy.Type.IsInterface) // false for delegates
                proxyBuilder.AddInterfaceImplementation(this.ProxyInfo.Proxy.Type);

            // create fields to store target instance and proxy factory
            FieldBuilder targetField = proxyBuilder.DefineField(TargetFieldName, this.ProxyInfo.Target.Type, FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder glueField = proxyBuilder.DefineField(GlueFieldName, typeof(ProxyGlue<Context>), FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder proxyInfosField = proxyBuilder.DefineField(ProxyInfosFieldName, typeof(IList<ProxyInfo<Context>>), FieldAttributes.Private | FieldAttributes.Static);

            // create constructor which accepts target instance + factory, and sets fields
            {
                ConstructorBuilder constructor = proxyBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new[] { this.ProxyInfo.Target.Type, typeof(ProxyGlue<Context>) });
                ILGenerator il = constructor.GetILGenerator();

                // call base constructor
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(object).GetConstructor(Array.Empty<Type>())!);

                // set target instance field
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, targetField);

                // set glue field
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, glueField);

                il.Emit(OpCodes.Ret);
            }

            // marking with an IProxyObject interface if needed
            switch (this.ProxyObjectInterfaceMarking)
            {
                case ProxyObjectInterfaceMarking.Disabled:
                    break;
                case ProxyObjectInterfaceMarking.Marker:
                    proxyBuilder.AddInterfaceImplementation(typeof(IProxyObject));
                    break;
                case ProxyObjectInterfaceMarking.MarkerWithProperty:
                    Type markerInterfaceType = typeof(IProxyObject.IWithProxyTargetInstanceProperty);
                    proxyBuilder.AddInterfaceImplementation(markerInterfaceType);

                    MethodInfo proxyTargetInstanceGetter = markerInterfaceType.GetProperty(nameof(IProxyObject.IWithProxyTargetInstanceProperty.ProxyTargetInstance))!.GetGetMethod()!;
                    MethodBuilder methodBuilder = proxyBuilder.DefineMethod(proxyTargetInstanceGetter.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);
                    methodBuilder.SetParameters(Array.Empty<Type>());
                    methodBuilder.SetReturnType(typeof(object));

                    ILGenerator il = methodBuilder.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, targetField);
                    il.Emit(OpCodes.Castclass, typeof(object));
                    il.Emit(OpCodes.Ret);

                    break;
            }

            // crosscheck this.
            Func<MethodInfo, bool>? filter = this.ProxyInfo.Proxy.Type.IsAssignableTo(typeof(Delegate)) ? (f => f.Name == "Invoke") : (_) => true; 

            // Groupby might make this more efficient.
            var allTargetMethods = this.ProxyInfo.Target.Type.FindInterfaceMethods(filter).ToList();
            var allProxyMethods = this.ProxyInfo.Proxy.Type.FindInterfaceMethods(filter);

            // proxy methods
            IList<ProxyInfo<Context>> relatedProxyInfos = new List<ProxyInfo<Context>>();
            foreach (MethodInfo proxyMethod in allProxyMethods)
            {
                var candidates = new Dictionary<MethodInfo, TypeUtilities.PositionConversion?[]>();
                foreach (MethodInfo targetMethod in allTargetMethods)
                {
                    var positionConversions = TypeUtilities.MatchProxyMethod(targetMethod, proxyMethod, this.EnumMappingBehavior, (new[]{this.ProxyInfo.Target.Type, this.ProxyInfo.Proxy.Type}).ToImmutableHashSet() );
                    if (positionConversions is null)
                        continue;

                    // no inputs are proxied.
                    if (positionConversions.All((a) => a is null))
                    {
                        this.ProxyMethod(manager, proxyBuilder, proxyMethod, targetMethod, targetField, glueField, proxyInfosField, positionConversions, relatedProxyInfos);
                        goto proxyMethodLoopContinue;
                    }
                    else
                    {
                        candidates[targetMethod] = positionConversions;
                    }
                }

                if (candidates.Any())
                {
                    if (candidates.Count == 1)
                    { // Only have one option, bypass ranking.
                        var (targetMethod, positionConversions) = candidates.First();
                        this.ProxyMethod(manager, proxyBuilder, proxyMethod, targetMethod, targetField, glueField, proxyInfosField, positionConversions, relatedProxyInfos);
                        goto proxyMethodLoopContinue;
                    }
                    else
                    {
                        List<Exception> exceptions = new();
                        foreach (var (targetMethod, positionConversions) in TypeUtilities.RankMethods(candidates, proxyMethod))
                        {
                            try
                            {
                                this.ProxyMethod(manager, proxyBuilder, proxyMethod, targetMethod, targetField, glueField, proxyInfosField, positionConversions, relatedProxyInfos);
                                goto proxyMethodLoopContinue;
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                            }
                        }
                        throw new AggregateException($"Errors generated while attempting to map {proxyMethod.Name}", exceptions);
                    }
                }
                else
                {
                    this.NoMatchingMethodHandler(proxyBuilder, this.ProxyInfo, targetField, glueField, proxyInfosField, proxyMethod);
                }
proxyMethodLoopContinue:
                ;
            }

            // save info
            this.BuiltProxyType = proxyBuilder.CreateType();
            var actualProxyInfosField = this.BuiltProxyType!.GetField(ProxyInfosFieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
            actualProxyInfosField.SetValue(null, relatedProxyInfos);
        }

        private void ProxyMethod(ProxyManager<Context> manager, TypeBuilder proxyBuilder, MethodInfo proxy, MethodInfo target, FieldBuilder instanceField, FieldBuilder glueField, FieldBuilder proxyInfosField, TypeUtilities.PositionConversion?[] positionConversions, IList<ProxyInfo<Context>> relatedProxyInfos)
        {
            MethodBuilder methodBuilder = proxyBuilder.DefineMethod(proxy.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);

            // set up generic arguments
            Type[] proxyGenericArguments = proxy.GetGenericArguments();
            string[] genericArgNames = proxyGenericArguments.Select(a => a.Name).ToArray();
            GenericTypeParameterBuilder[] genericTypeParameterBuilders = proxyGenericArguments.Length == 0 ? Array.Empty<GenericTypeParameterBuilder>() : methodBuilder.DefineGenericParameters(genericArgNames);
            for (int i = 0; i < proxyGenericArguments.Length; i++)
            {
                genericTypeParameterBuilders[i].SetGenericParameterAttributes(proxyGenericArguments[i].GenericParameterAttributes);
                Type[] constraints = proxyGenericArguments[i].GetGenericParameterConstraints();
                Type? baseConstraint = constraints.Where(t => !t.IsInterface).FirstOrDefault();
                Type[] interfaceConstraints = constraints.Where(t => t.IsInterface).ToArray();
                if (baseConstraint is not null)
                    genericTypeParameterBuilders[i].SetBaseTypeConstraint(baseConstraint);
                if (interfaceConstraints.Length != 0)
                    genericTypeParameterBuilders[i].SetInterfaceConstraints(interfaceConstraints);
            }

            // set up parameters
            var targetParameters = target.GetParameters();
            Type[] argTypes = proxy.GetParameters()
                .Select(a => a.ParameterType)
                .Select(t => t.IsGenericMethodParameter ? genericTypeParameterBuilders[t.GenericParameterPosition] : t)
                .ToArray();

            // proxy additional types
            int? returnValueTargetToArgProxyInfoIndex = null;
            int?[] parameterTargetToArgProxyInfoIndexes = new int?[argTypes.Length];

            switch (positionConversions[0])
            {
                case TypeUtilities.PositionConversion.Proxy:
                    returnValueTargetToArgProxyInfoIndex = relatedProxyInfos.Count;
                    relatedProxyInfos.Add(this.ProxyInfo.Copy(targetType: target.ReturnType.GetNonRefType(), proxyType: proxy.ReturnType.GetNonRefType()));
                    switch (this.ProxyPrepareBehavior)
                    {
                        case ProxyManagerProxyPrepareBehavior.Eager:
                            var proxyInfo = relatedProxyInfos.Last();
                            if (!proxyInfo.Proxy.Type.ContainsGenericParameters && !proxyInfo.Target.Type.ContainsGenericParameters)
                                manager.ObtainProxyFactory(relatedProxyInfos.Last());
                            break;
                        case ProxyManagerProxyPrepareBehavior.Lazy:
                            break;
                    }
                    break;
                case null:
                    break;
            }

            for (int i = 0; i < targetParameters.Length; i++)
            {
                switch (positionConversions[i + 1])
                {
                    case TypeUtilities.PositionConversion.Proxy:
                        var targetType = targetParameters[i].ParameterType;
                        var argType = argTypes[i];

                        parameterTargetToArgProxyInfoIndexes[i] = relatedProxyInfos.Count;
                        relatedProxyInfos.Add(this.ProxyInfo.Copy(targetType: targetType.GetNonRefType(), proxyType: argType.GetNonRefType()));
                        switch (this.ProxyPrepareBehavior)
                        {
                            case ProxyManagerProxyPrepareBehavior.Eager:
                                var proxyInfo = relatedProxyInfos.Last();
                                if (!proxyInfo.Proxy.Type.ContainsGenericParameters && !proxyInfo.Target.Type.ContainsGenericParameters)
                                    manager.ObtainProxyFactory(relatedProxyInfos.Last());
                                break;
                            case ProxyManagerProxyPrepareBehavior.Lazy:
                                break;
                        }
                        break;
                    case null:
                        break;
                }
            }

            Type returnType = proxy.ReturnType.IsGenericMethodParameter ? genericTypeParameterBuilders[proxy.ReturnType.GenericParameterPosition] : proxy.ReturnType;
            methodBuilder.SetReturnType(returnType);
            methodBuilder.SetParameters(argTypes);
            for (int i = 0; i < argTypes.Length; i++)
                methodBuilder.DefineParameter(i, targetParameters[i].Attributes, targetParameters[i].Name);

            Type[] typeGenericArguments = target.DeclaringType?.GetGenericArguments() ?? Array.Empty<Type>();
            Type[] methodGenericArguments = target.GetGenericArguments();
            Type[] allTargetGenericArguments = typeGenericArguments.Union(methodGenericArguments).ToArray();

            Type[] allProxyGenericArguments = proxyGenericArguments;

            // create method body
            {
                ILGenerator il = methodBuilder.GetILGenerator();
                LocalBuilder?[] proxyLocals = new LocalBuilder?[argTypes.Length];
                LocalBuilder?[] targetLocals = new LocalBuilder?[argTypes.Length];

                void ConvertIfNeededAndStore(LocalBuilder inputLocal, LocalBuilder outputLocal, int? proxyInfoIndex, bool isReverse, TypeUtilities.PositionConversion? positionConversion)
                {
                    bool IsValueType(Type type)
                    {
                        if (type.IsValueType || type == typeof(Enum) || (type is not GenericTypeParameterBuilder && type.IsEnum))
                            return true;
                        if (!type.IsGenericParameter)
                            return false;
                        foreach (var genericArgument in proxyGenericArguments)
                        {
                            if (genericArgument.Name == type.Name)
                            {
                                foreach (var constraint in genericArgument.GetGenericParameterConstraints())
                                {
                                    if (constraint.IsValueType || constraint == typeof(Enum) || constraint.IsEnum)
                                        return true;
                                }
                            }
                        }
                        return false;
                    }

                    if (proxyInfoIndex is null)
                    {
                        il.Emit(OpCodes.Ldloc, inputLocal);
                        il.Emit(OpCodes.Stloc, outputLocal);
                        return;
                    }

                    Label? isNullLabel = null;
                    if (!inputLocal.LocalType.IsValueType)
                    {
                        isNullLabel = il.DefineLabel();
                        il.Emit(OpCodes.Ldloc, inputLocal);
                        il.Emit(OpCodes.Brfalse, isNullLabel.Value);
                    }

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, glueField);

                    // load target generic arguments
                    il.Emit(OpCodes.Newobj, StringTypeDictionaryConstructor);
                    foreach (Type type in allTargetGenericArguments)
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldstr, type.Name);
                        il.Emit(OpCodes.Ldtoken, type);
                        il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
                        il.Emit(OpCodes.Callvirt, StringTypeDictionarySetItemMethod);
                    }

                    // load proxy generic arguments
                    il.Emit(OpCodes.Newobj, StringTypeDictionaryConstructor);
                    foreach (Type type in allProxyGenericArguments)
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldstr, type.Name);
                        il.Emit(OpCodes.Ldtoken, type);
                        il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
                        il.Emit(OpCodes.Callvirt, StringTypeDictionarySetItemMethod);
                    }

                    // load proxy ProxyInfo
                    il.Emit(OpCodes.Ldsfld, proxyInfosField);
                    il.Emit(OpCodes.Ldc_I4, proxyInfoIndex.Value);
                    il.Emit(OpCodes.Callvirt, ProxyInfoListGetMethod);
                    il.Emit(OpCodes.Ldc_I4, isReverse ? 1 : 0);

                    // load instance to proxy and call method
                    il.Emit(OpCodes.Ldloc, inputLocal);
                    if (IsValueType(inputLocal.LocalType))
                        il.Emit(OpCodes.Box, inputLocal.LocalType);
                    il.Emit(OpCodes.Call, UnproxyOrObtainProxyMethod);

                    if (IsValueType(outputLocal.LocalType))
                        il.Emit(OpCodes.Unbox_Any, outputLocal.LocalType);
                    il.Emit(OpCodes.Stloc, outputLocal);

                    if (!inputLocal.LocalType.IsValueType)
                        il.MarkLabel(isNullLabel!.Value);
                }

                // calling the proxied method
                LocalBuilder? resultTargetLocal = target.ReturnType == typeof(void) ? null : il.DeclareLocal(target.ReturnType);
                LocalBuilder? resultProxyLocal = returnType == typeof(void) ? null : il.DeclareLocal(returnType);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, instanceField);
                for (int i = 0; i < argTypes.Length; i++)
                {
                    switch (positionConversions[i + 1])
                    {
                        case TypeUtilities.PositionConversion.Proxy:
                            if (argTypes[i].IsByRef)
                            {
                                proxyLocals[i] = il.DeclareLocal(argTypes[i].GetNonRefType());
                                targetLocals[i] = il.DeclareLocal(targetParameters[i].ParameterType.GetNonRefType());
                                if (targetParameters[i].IsOut)
                                {
                                    il.Emit(OpCodes.Ldloca, targetLocals[i]!);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Ldarg, i + 1);
                                    il.Emit(OpCodes.Ldind_Ref);
                                    il.Emit(OpCodes.Stloc, proxyLocals[i]!);
                                    ConvertIfNeededAndStore(proxyLocals[i]!, targetLocals[i]!, parameterTargetToArgProxyInfoIndexes[i], isReverse: true, positionConversions[i + 1]);
                                    il.Emit(OpCodes.Ldloca, targetLocals[i]!);
                                }
                            }
                            else
                            {
                                proxyLocals[i] = il.DeclareLocal(argTypes[i]);
                                targetLocals[i] = il.DeclareLocal(targetParameters[i].ParameterType);
                                il.Emit(OpCodes.Ldarg, i + 1);
                                il.Emit(OpCodes.Stloc, proxyLocals[i]!);
                                ConvertIfNeededAndStore(proxyLocals[i]!, targetLocals[i]!, parameterTargetToArgProxyInfoIndexes[i], isReverse: true, positionConversions[i + 1]);
                                il.Emit(OpCodes.Ldloc, targetLocals[i]!);
                            }
                            break;
                        case null:
                            il.Emit(OpCodes.Ldarg, i + 1);
                            break;
                    }
                }
                il.Emit(target.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, target);
                if (target.ReturnType != typeof(void))
                    il.Emit(OpCodes.Stloc, resultTargetLocal!);

                // proxying back `out`/`ref`/array parameters
                for (int i = 0; i < argTypes.Length; i++)
                {
                    switch (positionConversions[i + 1])
                    {
                        case TypeUtilities.PositionConversion.Proxy:
                            if (argTypes[i].IsByRef)
                            {
                                ConvertIfNeededAndStore(targetLocals[i]!, proxyLocals[i]!, parameterTargetToArgProxyInfoIndexes[i], isReverse: false, positionConversions[i + 1]);
                                il.Emit(OpCodes.Ldarg, i + 1);
                                il.Emit(OpCodes.Ldloc, proxyLocals[i]!);
                                il.Emit(OpCodes.Stind_Ref);
                            }
                            else if (argTypes[i].IsArray)
                            {
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldfld, glueField);

                                // load proxy ProxyInfo
                                il.Emit(OpCodes.Ldsfld, proxyInfosField);
                                il.Emit(OpCodes.Ldc_I4, parameterTargetToArgProxyInfoIndexes[i]!.Value);
                                il.Emit(OpCodes.Callvirt, ProxyInfoListGetMethod);
                                il.Emit(OpCodes.Ldc_I4, argTypes[i].IsByRef ? 1 : 0);

                                il.Emit(OpCodes.Ldloc, targetLocals[i]!);
                                il.Emit(OpCodes.Ldloc, proxyLocals[i]!);
                                il.Emit(OpCodes.Callvirt, MapArrayContentsMethod);
                            }
                            break;
                        case null:
                            break;
                    }
                }

                // proxying return value
                if (target.ReturnType != typeof(void))
                    ConvertIfNeededAndStore(resultTargetLocal!, resultProxyLocal!, returnValueTargetToArgProxyInfoIndex, isReverse: false, positionConversions[0]);

                // return result
                if (target.ReturnType != typeof(void))
                    il.Emit(OpCodes.Ldloc, resultProxyLocal!);
                il.Emit(OpCodes.Ret);
            }
        }

        /// <inheritdoc/>
        [return: NotNullIfNotNull("targetInstance")]
        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            lock (this.ProxyCache)
            {
                if (this.ProxyCache.TryGetValue(targetInstance, out object? proxyInstance))
                    return proxyInstance;

                ConstructorInfo? constructor = this.BuiltProxyType?.GetConstructor(new[] { this.ProxyInfo.Target.Type, typeof(ProxyGlue<Context>) });
                if (constructor is null)
                    throw new InvalidOperationException($"Couldn't find the constructor for generated proxy type '{this.ProxyInfo.Proxy.Type.Name}'."); // should never happen
                proxyInstance = constructor.Invoke(new[] { targetInstance, new ProxyGlue<Context>(manager) });

                if (this.ProxyInfo.Proxy.Type.IsInterface)
                {
                    this.ProxyCache.Add(targetInstance, proxyInstance);
                    return proxyInstance;
                }
                else // has to be a delegate
                {
                    MethodInfo? invokeMethod = this.BuiltProxyType?.GetMethod("Invoke");
                    if (invokeMethod is null)
                        throw new InvalidOperationException($"Couldn't find the Invoke method for generated proxy delegate type '{this.ProxyInfo.Proxy.Type.Name}'."); // should never happen
                    var @delegate = Delegate.CreateDelegate(this.ProxyInfo.Proxy.Type, proxyInstance, invokeMethod);
                    this.ProxyCache.Add(targetInstance, @delegate);
                    return @delegate;
                }
            }
        }

        /// <inheritdoc/>
        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            lock (this.ProxyCache)
            {
                foreach ((object cachedTargetInstance, object cachedProxyInstance) in this.ProxyCache)
                {
                    if (ReferenceEquals(potentialProxyInstance, cachedProxyInstance))
                    {
                        targetInstance = cachedTargetInstance;
                        return true;
                    }
                }
                targetInstance = null;
                return false;
            }
        }
    }
}
