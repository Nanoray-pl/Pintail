using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    internal class DefaultProxyFactory<Context>: IProxyFactory<Context>
    {
        private enum MethodTypeMatchingPart { ReturnType, Parameter }

        private enum MatchingTypesResult { False, IfProxied, True }

        private enum PositionConversion { Proxy }

        private static readonly string TargetFieldName = "__Target";
        private static readonly string GlueFieldName = "__Glue";
        private static readonly string ProxyInfosFieldName = "__ProxyInfos";
        private static readonly MethodInfo ObtainProxyMethod = typeof(DefaultProxyGlue<Context>).GetMethod(nameof(DefaultProxyGlue<Context>.ObtainProxy), new Type[] { typeof(ProxyInfo<Context>), typeof(object) })!;
        private static readonly MethodInfo UnproxyOrObtainProxyMethod = typeof(DefaultProxyGlue<Context>).GetMethod(nameof(DefaultProxyGlue<Context>.UnproxyOrObtainProxy), new Type[] { typeof(ProxyInfo<Context>), typeof(ProxyInfo<Context>), typeof(object) })!;
        private static readonly MethodInfo MapArrayContentsMethod = typeof(DefaultProxyGlue<Context>).GetMethod(nameof(DefaultProxyGlue<Context>.MapArrayContents), new Type[] { typeof(ProxyInfo<Context>), typeof(Array), typeof(Array) })!;
        private static readonly MethodInfo ProxyInfoListGetMethod = typeof(IList<ProxyInfo<Context>>).GetProperty("Item")!.GetGetMethod()!;

        public ProxyInfo<Context> ProxyInfo { get; private set; }
        private readonly DefaultProxyManagerNoMatchingMethodHandler<Context> NoMatchingMethodHandler;
        private readonly DefaultProxyManagerEnumMappingBehavior EnumMappingBehavior;
        private readonly ProxyObjectInterfaceMarking ProxyObjectInterfaceMarking;
        private readonly ConditionalWeakTable<object, object> ProxyCache = new();
        private Type? BuiltProxyType;

        internal DefaultProxyFactory(ProxyInfo<Context> proxyInfo, DefaultProxyManagerNoMatchingMethodHandler<Context> noMatchingMethodHandler, DefaultProxyManagerEnumMappingBehavior enumMappingBehavior, ProxyObjectInterfaceMarking proxyObjectInterfaceMarking)
        {
            this.ProxyInfo = proxyInfo;
            this.NoMatchingMethodHandler = noMatchingMethodHandler;
            this.EnumMappingBehavior = enumMappingBehavior;
            this.ProxyObjectInterfaceMarking = proxyObjectInterfaceMarking;
        }

        internal void Prepare(DefaultProxyManager<Context> manager, string typeName)
        {
            // define proxy type
            TypeBuilder proxyBuilder = manager.ModuleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
            proxyBuilder.AddInterfaceImplementation(this.ProxyInfo.Proxy.Type);

            // create fields to store target instance and proxy factory
            FieldBuilder targetField = proxyBuilder.DefineField(TargetFieldName, this.ProxyInfo.Target.Type, FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder glueField = proxyBuilder.DefineField(GlueFieldName, typeof(DefaultProxyGlue<Context>), FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder proxyInfosField = proxyBuilder.DefineField(ProxyInfosFieldName, typeof(IList<ProxyInfo<Context>>), FieldAttributes.Private | FieldAttributes.Static);

            // create constructor which accepts target instance + factory, and sets fields
            {
                ConstructorBuilder constructor = proxyBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new[] { this.ProxyInfo.Target.Type, typeof(DefaultProxyGlue<Context>) });
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

            IEnumerable<MethodInfo> FindInterfaceMethods(Type baseType)
            {
                foreach (MethodInfo method in baseType.GetMethods())
                {
                    yield return method;
                }
                foreach (Type interfaceType in baseType.GetInterfaces())
                {
                    foreach (var method in FindInterfaceMethods(interfaceType))
                    {
                        yield return method;
                    }
                }
            }

            var allTargetMethods = FindInterfaceMethods(this.ProxyInfo.Target.Type).ToHashSet();
            var allProxyMethods = FindInterfaceMethods(this.ProxyInfo.Proxy.Type).ToHashSet();

            MatchingTypesResult AreTypesMatching(Type targetType, Type proxyType, MethodTypeMatchingPart part)
            {
                var typeA = part == MethodTypeMatchingPart.Parameter ? targetType : proxyType;
                var typeB = part == MethodTypeMatchingPart.Parameter ? proxyType : targetType;

                if (typeA.IsGenericMethodParameter != typeB.IsGenericMethodParameter)
                    return MatchingTypesResult.False;
                if (proxyType.IsEnum && targetType.IsEnum)
                {
                    if (proxyType == targetType)
                        return MatchingTypesResult.True;
                    var proxyEnumRawValues = proxyType.GetEnumerableEnumValues().Select(e => (int)(object)e).ToList();
                    var targetEnumRawValues = targetType.GetEnumerableEnumValues().Select(e => (int)(object)e).ToList();
                    switch (this.EnumMappingBehavior)
                    {
                        case DefaultProxyManagerEnumMappingBehavior.Strict:
                            return proxyEnumRawValues.OrderBy(e => e).SequenceEqual(targetEnumRawValues.OrderBy(e => e)) ? MatchingTypesResult.IfProxied : MatchingTypesResult.False;
                        case DefaultProxyManagerEnumMappingBehavior.AllowAdditive:
                            return targetEnumRawValues.ToHashSet().Except(proxyEnumRawValues).Any() ? MatchingTypesResult.False : MatchingTypesResult.IfProxied;
                        case DefaultProxyManagerEnumMappingBehavior.ThrowAtRuntime:
                            return MatchingTypesResult.IfProxied;
                    }
                }
                if (proxyType.IsArray && targetType.IsArray)
                    return proxyType == targetType ? MatchingTypesResult.True : MatchingTypesResult.IfProxied;
                if (typeA.IsGenericMethodParameter)
                    return typeA.GenericParameterPosition == typeB.GenericParameterPosition ? MatchingTypesResult.True : MatchingTypesResult.False;

                if (typeA.IsAssignableFrom(typeB))
                    return MatchingTypesResult.True;

                if (proxyType.GetNonRefType().IsInterface)
                    return MatchingTypesResult.IfProxied;
                if (targetType.GetNonRefType().IsInterface)
                    return MatchingTypesResult.IfProxied;

                var targetTypeGenericArguments = targetType.GetGenericArguments();
                var proxyTypeGenericArguments = proxyType.GetGenericArguments();
                if (targetTypeGenericArguments.Length != proxyTypeGenericArguments.Length || targetTypeGenericArguments.Length == 0)
                    return MatchingTypesResult.False;

                var genericTargetType = targetType.GetGenericTypeDefinition();
                var genericProxyType = proxyType.GetGenericTypeDefinition();

                var matchingTypesResult = MatchingTypesResult.True;
                switch (AreTypesMatching(genericTargetType, genericProxyType, part))
                {
                    case MatchingTypesResult.True:
                        break;
                    case MatchingTypesResult.IfProxied:
                        matchingTypesResult = MatchingTypesResult.IfProxied;
                        break;
                    case MatchingTypesResult.False:
                        return MatchingTypesResult.False;
                }
                for (int i = 0; i < targetTypeGenericArguments.Length; i++)
                {
                    switch (AreTypesMatching(targetTypeGenericArguments[i], proxyTypeGenericArguments[i], part))
                    {
                        case MatchingTypesResult.True:
                            break;
                        case MatchingTypesResult.IfProxied:
                            matchingTypesResult = MatchingTypesResult.IfProxied;
                            break;
                        case MatchingTypesResult.False:
                            return MatchingTypesResult.False;
                    }
                }
                return matchingTypesResult;
            }

            // proxy methods
            IList<ProxyInfo<Context>> relatedProxyInfos = new List<ProxyInfo<Context>>();
            foreach (MethodInfo proxyMethod in allProxyMethods)
            {
                var proxyMethodParameters = proxyMethod.GetParameters();
                var proxyMethodGenericArguments = proxyMethod.GetGenericArguments();

                PositionConversion?[]? MatchProxyMethod(MethodInfo targetMethod)
                {
                    // checking if `targetMethod` matches `proxyMethod`

                    if (targetMethod.Name != proxyMethod.Name)
                        return null;
                    if (targetMethod.GetGenericArguments().Length != proxyMethodGenericArguments!.Length)
                        return null;
                    var mParameters = targetMethod.GetParameters();
                    if (mParameters.Length != proxyMethodParameters!.Length)
                        return null;
                    var positionConversions = new PositionConversion?[mParameters.Length + 1]; // 0 = return type; n + 1 = parameter position n

                    switch (AreTypesMatching(targetMethod.ReturnType, proxyMethod.ReturnType, MethodTypeMatchingPart.ReturnType))
                    {
                        case MatchingTypesResult.False:
                            return null;
                        case MatchingTypesResult.True:
                            break;
                        case MatchingTypesResult.IfProxied:
                            positionConversions[0] = PositionConversion.Proxy;
                            break;
                    }

                    for (int i = 0; i < mParameters.Length; i++)
                    {
                        switch (AreTypesMatching(mParameters[i].ParameterType.GetNonRefType(), proxyMethodParameters[i].ParameterType.GetNonRefType(), MethodTypeMatchingPart.Parameter))
                        {
                            case MatchingTypesResult.False:
                                return null;
                            case MatchingTypesResult.True:
                                break;
                            case MatchingTypesResult.IfProxied:
                                positionConversions[i + 1] = PositionConversion.Proxy;
                                break;
                        }
                    }

                    // method matched
                    return positionConversions;
                }

                foreach (MethodInfo targetMethod in allTargetMethods)
                {
                    var positionConversions = MatchProxyMethod(targetMethod);
                    if (positionConversions is null)
                        continue;
                    this.ProxyMethod(manager, proxyBuilder, proxyMethod, targetMethod, targetField, glueField, proxyInfosField, positionConversions, relatedProxyInfos);
                    goto proxyMethodLoopContinue;
                }

                this.NoMatchingMethodHandler(proxyBuilder, this.ProxyInfo, targetField, glueField, proxyInfosField, proxyMethod);
                proxyMethodLoopContinue:;
            }

            // save info
            this.BuiltProxyType = proxyBuilder.CreateType();
            var actualProxyInfosField = this.BuiltProxyType!.GetField(ProxyInfosFieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
            actualProxyInfosField.SetValue(null, relatedProxyInfos);
        }

        private void ProxyMethod(DefaultProxyManager<Context> manager, TypeBuilder proxyBuilder, MethodInfo proxy, MethodInfo target, FieldBuilder instanceField, FieldBuilder glueField, FieldBuilder proxyInfosField, PositionConversion?[] positionConversions, IList<ProxyInfo<Context>> relatedProxyInfos)
        {
            MethodBuilder methodBuilder = proxyBuilder.DefineMethod(proxy.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);

            // set up generic arguments
            Type[] proxyGenericArguments = proxy.GetGenericArguments();
            string[] genericArgNames = proxyGenericArguments.Select(a => a.Name).ToArray();
            GenericTypeParameterBuilder[] genericTypeParameterBuilders = proxyGenericArguments.Length == 0 ? Array.Empty<GenericTypeParameterBuilder>() : methodBuilder.DefineGenericParameters(genericArgNames);
            for (int i = 0; i < proxyGenericArguments.Length; i++)
                genericTypeParameterBuilders[i].SetGenericParameterAttributes(proxyGenericArguments[i].GenericParameterAttributes);

            // set up parameters
            var targetParameters = target.GetParameters();
            Type[] argTypes = proxy.GetParameters()
                .Select(a => a.ParameterType)
                .Select(t => t.IsGenericMethodParameter ? genericTypeParameterBuilders[t.GenericParameterPosition] : t)
                .ToArray();

            // proxy additional types
            int? returnValueTargetToArgProxyInfoIndex = null;
            int? returnValueArgToTargetProxyInfoIndex = null;
            int?[] parameterTargetToArgProxyInfoIndexes = new int?[argTypes.Length];
            int?[] parameterArgToTargetProxyInfoIndexes = new int?[argTypes.Length];

            switch (positionConversions[0])
            {
                case PositionConversion.Proxy:
                    var targetToArgFactory = manager.ObtainProxyFactory(this.ProxyInfo.Copy(targetType: target.ReturnType.GetNonRefType(), proxyType: proxy.ReturnType.GetNonRefType()));
                    returnValueTargetToArgProxyInfoIndex = relatedProxyInfos.Count;
                    relatedProxyInfos.Add(targetToArgFactory.ProxyInfo);

                    var argToTargetFactory = manager.ObtainProxyFactory(this.ProxyInfo.Copy(targetType: proxy.ReturnType.GetNonRefType(), proxyType: target.ReturnType.GetNonRefType()));
                    returnValueArgToTargetProxyInfoIndex = relatedProxyInfos.Count;
                    relatedProxyInfos.Add(argToTargetFactory.ProxyInfo);
                    break;
                case null:
                    break;
            }

            for (int i = 0; i < targetParameters.Length; i++)
            {
                switch (positionConversions[i + 1])
                {
                    case PositionConversion.Proxy:
                        var targetType = targetParameters[i].ParameterType;
                        var argType = argTypes[i];

                        var targetToArgFactory = manager.ObtainProxyFactory(this.ProxyInfo.Copy(targetType: targetType.GetNonRefType(), proxyType: argType.GetNonRefType()));
                        parameterTargetToArgProxyInfoIndexes[i] = relatedProxyInfos.Count;
                        relatedProxyInfos.Add(targetToArgFactory.ProxyInfo);

                        var argToTargetFactory = manager.ObtainProxyFactory(this.ProxyInfo.Copy(targetType: argType.GetNonRefType(), proxyType: targetType.GetNonRefType()));
                        parameterArgToTargetProxyInfoIndexes[i] = relatedProxyInfos.Count;
                        relatedProxyInfos.Add(argToTargetFactory.ProxyInfo);
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

            // create method body
            {
                if (target.Name == "TryGetValue")
                    Console.WriteLine("huhu");
                ILGenerator il = methodBuilder.GetILGenerator();
                LocalBuilder?[] proxyLocals = new LocalBuilder?[argTypes.Length];
                LocalBuilder?[] targetLocals = new LocalBuilder?[argTypes.Length];

                void ConvertIfNeededAndStore(LocalBuilder inputLocal, LocalBuilder outputLocal, int? proxyInfoIndex, int? unproxyInfoIndex, PositionConversion? positionConversion)
                {
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
                    if (unproxyInfoIndex is null)
                    {
                        // load proxy ProxyInfo
                        il.Emit(OpCodes.Ldsfld, proxyInfosField);
                        il.Emit(OpCodes.Ldc_I4, proxyInfoIndex.Value);
                        il.Emit(OpCodes.Callvirt, ProxyInfoListGetMethod);

                        // load instance to proxy and call method
                        il.Emit(OpCodes.Ldloc, inputLocal);
                        il.Emit(OpCodes.Call, ObtainProxyMethod);
                    }
                    else
                    {
                        // load proxy ProxyInfo
                        il.Emit(OpCodes.Ldsfld, proxyInfosField);
                        il.Emit(OpCodes.Ldc_I4, proxyInfoIndex.Value);
                        il.Emit(OpCodes.Callvirt, ProxyInfoListGetMethod);

                        // load unproxy ProxyInfo
                        il.Emit(OpCodes.Ldsfld, proxyInfosField);
                        il.Emit(OpCodes.Ldc_I4, unproxyInfoIndex.Value);
                        il.Emit(OpCodes.Callvirt, ProxyInfoListGetMethod);

                        // load instance to proxy and call method
                        il.Emit(OpCodes.Ldloc, inputLocal);
                        if (inputLocal.LocalType.IsValueType)
                            il.Emit(OpCodes.Box, inputLocal.LocalType);
                        il.Emit(OpCodes.Call, UnproxyOrObtainProxyMethod);
                    }
                    if (outputLocal.LocalType.IsValueType)
                        il.Emit(OpCodes.Unbox_Any, outputLocal.LocalType);
                    else
                        il.Emit(OpCodes.Castclass, outputLocal.LocalType);
                    il.Emit(OpCodes.Stloc, outputLocal);

                    if (!inputLocal.LocalType.IsValueType)
                        il.MarkLabel(isNullLabel!.Value);
                }

                // calling the proxied method
                LocalBuilder? resultInputLocal = target.ReturnType == typeof(void) ? null : il.DeclareLocal(target.ReturnType);
                LocalBuilder? resultOutputLocal = returnType == typeof(void) ? null : il.DeclareLocal(returnType);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, instanceField);
                for (int i = 0; i < argTypes.Length; i++)
                {
                    switch (positionConversions[i + 1])
                    {
                        case PositionConversion.Proxy:
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
                                    ConvertIfNeededAndStore(proxyLocals[i]!, targetLocals[i]!, parameterArgToTargetProxyInfoIndexes[i], parameterTargetToArgProxyInfoIndexes[i], positionConversions[i + 1]);
                                    il.Emit(OpCodes.Ldloca, targetLocals[i]!);
                                }
                            }
                            else
                            {
                                proxyLocals[i] = il.DeclareLocal(argTypes[i]);
                                targetLocals[i] = il.DeclareLocal(targetParameters[i].ParameterType);
                                il.Emit(OpCodes.Ldarg, i + 1);
                                il.Emit(OpCodes.Stloc, proxyLocals[i]!);
                                ConvertIfNeededAndStore(proxyLocals[i]!, targetLocals[i]!, parameterArgToTargetProxyInfoIndexes[i], parameterTargetToArgProxyInfoIndexes[i], positionConversions[i + 1]);
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
                    il.Emit(OpCodes.Stloc, resultInputLocal!);

                // proxying `out` parameters
                for (int i = 0; i < argTypes.Length; i++)
                {
                    switch (positionConversions[i + 1])
                    {
                        case PositionConversion.Proxy:
                            if (argTypes[i].IsByRef)
                            {
                                ConvertIfNeededAndStore(proxyLocals[i]!, targetLocals[i]!, parameterTargetToArgProxyInfoIndexes[i], null, positionConversions[i + 1]);
                                il.Emit(OpCodes.Ldarg, i + 1);
                                il.Emit(OpCodes.Ldloc, targetLocals[i]!);
                                il.Emit(OpCodes.Stind_Ref);
                            }
                            else if (argTypes[i].IsArray)
                            {
                                int arrayProxyInfoIndex = argTypes[i].IsByRef ? parameterTargetToArgProxyInfoIndexes[i]!.Value : parameterArgToTargetProxyInfoIndexes[i]!.Value;

                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldfld, glueField);

                                // load proxy ProxyInfo
                                il.Emit(OpCodes.Ldsfld, proxyInfosField);
                                il.Emit(OpCodes.Ldc_I4, arrayProxyInfoIndex);
                                il.Emit(OpCodes.Callvirt, ProxyInfoListGetMethod);

                                il.Emit(OpCodes.Ldloc, targetLocals[i]!);
                                il.Emit(OpCodes.Ldloc, proxyLocals[i]!);
                                il.Emit(OpCodes.Callvirt, MapArrayContentsMethod);
                            }
                            break;
                        case null:
                            il.Emit(OpCodes.Ldarg, i + 1);
                            break;
                    }
                }

                // proxying return value
                if (target.ReturnType != typeof(void))
                    ConvertIfNeededAndStore(resultInputLocal!, resultOutputLocal!, returnValueTargetToArgProxyInfoIndex, returnValueArgToTargetProxyInfoIndex, positionConversions[0]);

                // return result
                if (target.ReturnType != typeof(void))
                    il.Emit(OpCodes.Ldloc, resultOutputLocal!);
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

                ConstructorInfo? constructor = this.BuiltProxyType?.GetConstructor(new[] { this.ProxyInfo.Target.Type, typeof(DefaultProxyGlue<Context>) });
                if (constructor is null)
                    throw new InvalidOperationException($"Couldn't find the constructor for generated proxy type '{this.ProxyInfo.Proxy.Type.Name}'."); // should never happen
                proxyInstance = constructor.Invoke(new[] { targetInstance, new DefaultProxyGlue<Context>(manager) });
                this.ProxyCache.Add(targetInstance, proxyInstance);
                return proxyInstance;
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
