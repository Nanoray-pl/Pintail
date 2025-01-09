using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    file static class InterfaceOrDelegateProxyFactory
    {
        public static readonly ConstructorInfo StringTypeDictionaryConstructor = typeof(Dictionary<string, Type>).GetConstructor([typeof(int)])!;
        public static readonly MethodInfo StringTypeDictionarySetItemMethod = typeof(Dictionary<string, Type>).GetMethod("set_Item")!;
        public static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;
    }

    internal class InterfaceOrDelegateProxyFactory<Context>: IProxyFactory<Context>
    {
        private const string TargetFieldName = "__Target";
        private const string GlueFieldName = "__Glue";
        private const string ProxyInfosFieldName = "__ProxyInfos";
        private static readonly MethodInfo UnproxyOrObtainProxyMethod = typeof(ProxyGlue<Context>).GetMethod(nameof(ProxyGlue<Context>.UnproxyOrObtainProxy))!;
        private static readonly MethodInfo MapArrayContentsMethod = typeof(ProxyGlue<Context>).GetMethod(nameof(ProxyGlue<Context>.MapArrayContents))!;
        private static readonly MethodInfo ProxyInfoListGetMethod = typeof(List<ProxyInfo<Context>>).GetProperty("Item")!.GetGetMethod()!;

        public ProxyInfo<Context> ProxyInfo { get; }
        private readonly EarlyProxyManagerNoMatchingMethodHandler<Context>? EarlyNoMatchingMethodHandler;
        private readonly ProxyManagerNoMatchingMethodHandler<Context> NoMatchingMethodHandler;
        private readonly ProxyManagerProxyPrepareBehavior ProxyPrepareBehavior;
        private readonly ProxyManagerEnumMappingBehavior EnumMappingBehavior;
        private readonly ProxyObjectInterfaceMarking ProxyObjectInterfaceMarking;
        private readonly AccessLevelChecking AccessLevelChecking;
        private readonly ConcurrentDictionary<string, List<Type>> InterfaceMappabilityCache;

        private readonly ConditionalWeakTable<object, object> ProxyCache = new();
        private Type? BuiltProxyType;

        internal InterfaceOrDelegateProxyFactory(
            ProxyInfo<Context> proxyInfo,
            EarlyProxyManagerNoMatchingMethodHandler<Context>? earlyNoMatchingMethodHandler,
            ProxyManagerNoMatchingMethodHandler<Context> noMatchingMethodHandler,
            ProxyManagerProxyPrepareBehavior proxyPrepareBehavior,
            ProxyManagerEnumMappingBehavior enumMappingBehavior,
            ProxyObjectInterfaceMarking proxyObjectInterfaceMarking,
            AccessLevelChecking accessLevelChecking,
            ConcurrentDictionary<string, List<Type>> interfaceMappabilityCache
        )
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
            this.EarlyNoMatchingMethodHandler = earlyNoMatchingMethodHandler;
            this.NoMatchingMethodHandler = noMatchingMethodHandler;
            this.ProxyPrepareBehavior = proxyPrepareBehavior;
            this.EnumMappingBehavior = enumMappingBehavior;
            this.ProxyObjectInterfaceMarking = proxyObjectInterfaceMarking;
            this.InterfaceMappabilityCache = interfaceMappabilityCache;
            this.AccessLevelChecking = accessLevelChecking;
        }

        internal void Prepare(ProxyManager<Context> manager, string typeName)
        {
            // crosscheck this.
            bool filterOnlyInvokeMethods = this.ProxyInfo.Proxy.Type.IsAssignableTo(typeof(Delegate));

            // Groupby might make this more efficient.
            var allTargetMethods = this.ProxyInfo.Target.Type.FindInterfaceMethods(this.AccessLevelChecking == AccessLevelChecking.Disabled, filterOnlyInvokeMethods).ToList();
            var allProxyMethods = this.ProxyInfo.Proxy.Type.FindInterfaceMethods(this.AccessLevelChecking == AccessLevelChecking.Disabled, filterOnlyInvokeMethods).ToList();

            var methodsToProxy = new List<MethodProxyInfo>(allProxyMethods.Count);
            var methodsFailedToProxy = new List<MethodInfo>();

#if DEBUG
            Console.WriteLine($"Looking at {allProxyMethods.Count} proxy methods and {allTargetMethods.Count} target methods for proxy {this.ProxyInfo.Proxy.Type.FullName} and target {this.ProxyInfo.Target.Type.FullName}");
            Console.WriteLine(string.Join(", ", allProxyMethods.Select(a => a.DeclaringType!.ToString() + '.' + a.Name.ToString())));
#endif

            // proxy methods
            var relatedProxyInfos = new List<ProxyInfo<Context>>();
            foreach (var proxyMethod in allProxyMethods)
            {
                var candidates = new Dictionary<MethodInfo, TypeUtilities.PositionConversion?[]>();
                foreach (var targetMethod in allTargetMethods)
                {
                    var positionConversions = TypeUtilities.MatchProxyMethod(targetMethod, proxyMethod, this.EnumMappingBehavior, ImmutableHashSet.Create(this.ProxyInfo.Target.Type, this.ProxyInfo.Proxy.Type), this.InterfaceMappabilityCache, this.AccessLevelChecking == AccessLevelChecking.Disabled);
                    if (positionConversions is null)
                        continue;

                    // no inputs are proxied.
                    if (positionConversions.All(a => a is null))
                    {
                        methodsToProxy.Add(new(proxyMethod, targetMethod, positionConversions, relatedProxyInfos));
                        goto proxyMethodLoopContinue;
                    }
                    candidates[targetMethod] = positionConversions;
                }

                if (candidates.Any())
                {
#if DEBUG
                    Console.WriteLine($"Found {candidates.Count} candidates for {proxyMethod.DeclaringType}.{proxyMethod.Name}");
#endif
                    var (targetMethod, positionConversions) = TypeUtilities.RankMethods(candidates, proxyMethod).First();

                    methodsToProxy.Add(new(proxyMethod, targetMethod, positionConversions, relatedProxyInfos));
                }
                else if (proxyMethod is { IsAbstract: false, DeclaringType.IsInterface: true })
                {
                    var positionConversions = TypeUtilities.MatchProxyMethod(proxyMethod, proxyMethod, this.EnumMappingBehavior, ImmutableHashSet.Create(this.ProxyInfo.Target.Type, this.ProxyInfo.Proxy.Type), this.InterfaceMappabilityCache, this.AccessLevelChecking == AccessLevelChecking.Disabled);
                    if (positionConversions is null)
                    {
                        this.EarlyNoMatchingMethodHandler?.Invoke(this.ProxyInfo, proxyMethod);
                        methodsFailedToProxy.Add(proxyMethod);
                    }
                }
                else
                {
                    this.EarlyNoMatchingMethodHandler?.Invoke(this.ProxyInfo, proxyMethod);
                    methodsFailedToProxy.Add(proxyMethod);
                }
                proxyMethodLoopContinue:;
            }

            // done matching methods to each other. if no `EarlyNoMatchingMethodHandler` threw, proceed to define the type

            // define proxy type
            var moduleBuilder = manager.GetModuleBuilder(this.ProxyInfo);
            var proxyBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
            if (this.ProxyInfo.Proxy.Type.IsInterface) // false for delegates
                proxyBuilder.AddInterfaceImplementation(this.ProxyInfo.Proxy.Type);

            // allows ignoring access levels - we only need this so we can access public methods in otherwise private types
            if (this.AccessLevelChecking != AccessLevelChecking.Enabled)
            {
                (moduleBuilder.Assembly as AssemblyBuilder)?.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        typeof(IgnoresAccessChecksToAttribute).GetConstructor([typeof(string)])!,
                        [this.ProxyInfo.Target.Type.Assembly.GetName().Name!]
                    )
                );
                (moduleBuilder.Assembly as AssemblyBuilder)?.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        typeof(IgnoresAccessChecksToAttribute).GetConstructor([typeof(string)])!,
                        [this.ProxyInfo.Proxy.Type.Assembly.GetName().Name!]
                    )
                );
            }

            // create fields to store target instance and proxy factory
            var targetField = proxyBuilder.DefineField(TargetFieldName, this.ProxyInfo.Target.Type, FieldAttributes.Private | FieldAttributes.InitOnly);
            var glueField = proxyBuilder.DefineField(GlueFieldName, typeof(ProxyGlue<Context>), FieldAttributes.Private | FieldAttributes.InitOnly);
            var proxyInfosField = proxyBuilder.DefineField(ProxyInfosFieldName, typeof(List<ProxyInfo<Context>>), FieldAttributes.Private | FieldAttributes.Static);

            // create constructor which accepts target instance + factory, and sets fields
            {
                var constructor = proxyBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, [this.ProxyInfo.Target.Type, typeof(ProxyGlue<Context>)]);
                var il = constructor.GetILGenerator();

                // call base constructor
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(object).GetConstructor([])!);

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
                    var markerInterfaceType = typeof(IProxyObject.IWithProxyTargetInstanceProperty);
                    proxyBuilder.AddInterfaceImplementation(markerInterfaceType);

                    var proxyTargetInstanceGetter = markerInterfaceType.GetProperty(nameof(IProxyObject.IWithProxyTargetInstanceProperty.ProxyTargetInstance))!.GetGetMethod()!;
                    var methodBuilder = proxyBuilder.DefineMethod(proxyTargetInstanceGetter.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);
                    methodBuilder.SetParameters(Array.Empty<Type>());
                    methodBuilder.SetReturnType(typeof(object));

                    var il = methodBuilder.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, targetField);
                    if (this.ProxyInfo.Target.Type.IsValueType)
                        il.Emit(OpCodes.Box, this.ProxyInfo.Target.Type);
                    il.Emit(OpCodes.Ret);

                    break;
            }

            {
                (moduleBuilder.Assembly as AssemblyBuilder)?.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        typeof(IgnoresAccessChecksToAttribute).GetConstructor([typeof(string)])!,
                        [typeof(IInternalProxyObject).Assembly.GetName().Name!]
                    )
                );

                var markerInterfaceType = typeof(IInternalProxyObject);
                proxyBuilder.AddInterfaceImplementation(markerInterfaceType);

                var proxyTargetInstanceGetter = markerInterfaceType.GetProperty(nameof(IInternalProxyObject.ProxyTargetInstance))!.GetGetMethod()!;
                var methodBuilder = proxyBuilder.DefineMethod(proxyTargetInstanceGetter.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);
                methodBuilder.SetParameters(Array.Empty<Type>());
                methodBuilder.SetReturnType(typeof(object));

                var il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, targetField);
                if (this.ProxyInfo.Target.Type.IsValueType)
                    il.Emit(OpCodes.Box, this.ProxyInfo.Target.Type);
                il.Emit(OpCodes.Ret);
            }

            foreach (var methodFailedToProxy in methodsFailedToProxy)
                this.NoMatchingMethodHandler(proxyBuilder, this.ProxyInfo, targetField, glueField, proxyInfosField, methodFailedToProxy);

            foreach (var methodProxyInfo in methodsToProxy)
                this.ProxyMethod(manager, proxyBuilder, methodProxyInfo.Proxy, methodProxyInfo.Target, targetField, glueField, proxyInfosField, methodProxyInfo.PositionConversions, methodProxyInfo.RelatedProxyInfos);

#if DEBUG
            Console.WriteLine($"Trying to save! {proxyBuilder.FullName}");
#endif
            // save info
            this.BuiltProxyType = proxyBuilder.CreateType();
            var actualProxyInfosField = this.BuiltProxyType!.GetField(ProxyInfosFieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
            actualProxyInfosField.SetValue(null, relatedProxyInfos);
        }

        private void ProxyMethod(ProxyManager<Context> manager, TypeBuilder proxyBuilder, MethodInfo proxy, MethodInfo target, FieldBuilder instanceField, FieldBuilder glueField, FieldBuilder proxyInfosField, TypeUtilities.PositionConversion?[] positionConversions, List<ProxyInfo<Context>> relatedProxyInfos)
        {
#if DEBUG
            Console.WriteLine($"Proxying {proxy.DeclaringType}.{proxy.Name}[{string.Join(", ", proxy.GetParameters().Select(a => a.Name))}] to {target.DeclaringType}.{target.Name}");
#endif
            var methodBuilder = proxyBuilder.DefineMethod(
                name: proxy.Name,
                attributes: MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual
            );

            // set up generic arguments
            var proxyGenericArguments = proxy.GetGenericArguments();
            string[] genericArgNames = proxyGenericArguments.Select(a => a.Name).ToArray();
            var genericTypeParameterBuilders = proxyGenericArguments.Length == 0 ? [] : methodBuilder.DefineGenericParameters(genericArgNames);
            for (int i = 0; i < proxyGenericArguments.Length; i++)
            {
                genericTypeParameterBuilders[i].SetGenericParameterAttributes(proxyGenericArguments[i].GenericParameterAttributes);
                var constraints = proxyGenericArguments[i].GetGenericParameterConstraints();
                var baseConstraint = constraints.FirstOrDefault(t => !t.IsInterface);
                var interfaceConstraints = constraints.Where(t => t.IsInterface).ToArray();
                if (baseConstraint is not null)
                    genericTypeParameterBuilders[i].SetBaseTypeConstraint(baseConstraint);
                if (interfaceConstraints.Length != 0)
                    genericTypeParameterBuilders[i].SetInterfaceConstraints(interfaceConstraints);
            }

            // set up parameters
            var targetParameters = target.GetParameters();
            var argTypes = proxy.GetParameters()
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

            var returnType = proxy.ReturnType.IsGenericMethodParameter ? genericTypeParameterBuilders[proxy.ReturnType.GenericParameterPosition] : proxy.ReturnType;

            // we must set the constraints correctly
            // or in params fail.
            // see: https://stackoverflow.com/questions/56564992/when-implementing-an-interface-that-has-a-method-with-in-parameter-by-typebuil
            var param = proxy.GetParameters();
            methodBuilder.SetSignature(
                returnType: returnType,
                returnTypeRequiredCustomModifiers: proxy.ReturnParameter.GetRequiredCustomModifiers(),
                returnTypeOptionalCustomModifiers: proxy.ReturnParameter.GetOptionalCustomModifiers(),
                parameterTypes: argTypes,
                parameterTypeRequiredCustomModifiers: param.Select(p => p.GetRequiredCustomModifiers()).ToArray(),
                parameterTypeOptionalCustomModifiers: param.Select(p => p.GetOptionalCustomModifiers()).ToArray()
            );

            for (int i = 0; i < argTypes.Length; i++)
                methodBuilder.DefineParameter(i, targetParameters[i].Attributes, targetParameters[i].Name);

            var typeGenericArguments = target.DeclaringType?.GetGenericArguments() ?? [];
            var methodGenericArguments = target.GetGenericArguments();
            var allTargetGenericArguments = typeGenericArguments.Union(methodGenericArguments).ToArray();

            var allProxyGenericArguments = proxyGenericArguments;

            // create method body
            {
                var il = methodBuilder.GetILGenerator();
                var proxyLocals = new LocalBuilder?[argTypes.Length];
                var targetLocals = new LocalBuilder?[argTypes.Length];

                void ConvertIfNeededAndStore(LocalBuilder inputLocal, LocalBuilder outputLocal, int? proxyInfoIndex, bool isReverse)
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

                    // load target generic arguments
                    if (allTargetGenericArguments.Length == 0)
                    {
                        il.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, allTargetGenericArguments.Length);
                        il.Emit(OpCodes.Newobj, InterfaceOrDelegateProxyFactory.StringTypeDictionaryConstructor);
                        foreach (var type in allTargetGenericArguments)
                        {
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Ldstr, type.Name);
                            il.Emit(OpCodes.Ldtoken, type);
                            il.Emit(OpCodes.Call, InterfaceOrDelegateProxyFactory.GetTypeFromHandleMethod);
                            il.Emit(OpCodes.Call, InterfaceOrDelegateProxyFactory.StringTypeDictionarySetItemMethod);
                        }
                    }

                    // load proxy generic arguments
                    if (allProxyGenericArguments.Length == 0)
                    {
                        il.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, allProxyGenericArguments.Length);
                        il.Emit(OpCodes.Newobj, InterfaceOrDelegateProxyFactory.StringTypeDictionaryConstructor);
                        foreach (var type in allProxyGenericArguments)
                        {
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Ldstr, type.Name);
                            il.Emit(OpCodes.Ldtoken, type);
                            il.Emit(OpCodes.Call, InterfaceOrDelegateProxyFactory.GetTypeFromHandleMethod);
                            il.Emit(OpCodes.Call, InterfaceOrDelegateProxyFactory.StringTypeDictionarySetItemMethod);
                        }
                    }

                    // load proxy ProxyInfo
                    il.Emit(OpCodes.Ldsfld, proxyInfosField);
                    il.Emit(OpCodes.Ldc_I4, proxyInfoIndex.Value);
                    il.Emit(OpCodes.Call, ProxyInfoListGetMethod);
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

                    bool IsValueType(Type type)
                    {
                        if (type.IsValueType || type == typeof(Enum) || (type is not GenericTypeParameterBuilder && type.IsEnum))
                            return true;
                        if (!type.IsGenericParameter)
                            return false;
                        foreach (var genericArgument in proxyGenericArguments)
                        {
                            if (genericArgument.Name != type.Name)
                                continue;

                            foreach (var constraint in genericArgument.GetGenericParameterConstraints())
                                if (constraint.IsValueType || constraint == typeof(Enum) || constraint.IsEnum)
                                    return true;
                        }
                        return false;
                    }
                }

                // calling the proxied method
                var resultTargetLocal = target.ReturnType == typeof(void) ? null : il.DeclareLocal(target.ReturnType);
                var resultProxyLocal = returnType == typeof(void) ? null : il.DeclareLocal(returnType);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(this.ProxyInfo.Target.Type.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, instanceField);
                for (int i = 0; i < argTypes.Length; i++)
                {
                    switch (positionConversions[i + 1])
                    {
                        case TypeUtilities.PositionConversion.Proxy:
                            if (argTypes[i].IsByRef)
                            {
                                proxyLocals[i] = il.DeclareLocal(argTypes[i].GetNonRefType());
                                targetLocals[i] = il.DeclareLocal(targetParameters[i].ParameterType.GetNonRefType());
                                if (!targetParameters[i].IsOut)
                                {
                                    il.Emit(OpCodes.Ldarg, i + 1);
                                    il.Emit(OpCodes.Ldind_Ref);
                                    il.Emit(OpCodes.Stloc, proxyLocals[i]!);
                                    ConvertIfNeededAndStore(proxyLocals[i]!, targetLocals[i]!, parameterTargetToArgProxyInfoIndexes[i], isReverse: true);
                                }
                                il.Emit(OpCodes.Ldloca, targetLocals[i]!);
                            }
                            else
                            {
                                proxyLocals[i] = il.DeclareLocal(argTypes[i]);
                                targetLocals[i] = il.DeclareLocal(targetParameters[i].ParameterType);
                                il.Emit(OpCodes.Ldarg, i + 1);
                                il.Emit(OpCodes.Stloc, proxyLocals[i]!);
                                ConvertIfNeededAndStore(proxyLocals[i]!, targetLocals[i]!, parameterTargetToArgProxyInfoIndexes[i], isReverse: true);
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
                                ConvertIfNeededAndStore(targetLocals[i]!, proxyLocals[i]!, parameterTargetToArgProxyInfoIndexes[i], isReverse: false);
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
                                il.Emit(OpCodes.Call, ProxyInfoListGetMethod);
                                il.Emit(OpCodes.Ldc_I4, argTypes[i].IsByRef ? 1 : 0);

                                il.Emit(OpCodes.Ldloc, targetLocals[i]!);
                                il.Emit(OpCodes.Ldloc, proxyLocals[i]!);
                                il.Emit(OpCodes.Call, MapArrayContentsMethod);
                            }
                            break;
                        case null:
                            break;
                    }
                }

                // proxying return value
                if (target.ReturnType != typeof(void))
                    ConvertIfNeededAndStore(resultTargetLocal!, resultProxyLocal!, returnValueTargetToArgProxyInfoIndex, isReverse: false);

                // return result
                if (target.ReturnType != typeof(void))
                    il.Emit(OpCodes.Ldloc, resultProxyLocal!);
                il.Emit(OpCodes.Ret);
            }
        }

        /// <inheritdoc/>
        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            lock (this.ProxyCache)
            {
                if (this.ProxyCache.TryGetValue(targetInstance, out object? proxyInstance))
                    return proxyInstance;

                var constructor = this.BuiltProxyType?.GetConstructor([this.ProxyInfo.Target.Type, typeof(ProxyGlue<Context>)]);
                if (constructor is null)
                    throw new InvalidOperationException($"Couldn't find the constructor for generated proxy type '{this.ProxyInfo.Proxy.Type.Name}'."); // should never happen
                proxyInstance = constructor.Invoke([targetInstance, new ProxyGlue<Context>(manager)]);

                if (this.ProxyInfo.Proxy.Type.IsInterface)
                {
                    this.ProxyCache.Add(targetInstance, proxyInstance);
                    return proxyInstance;
                }

                // has to be a delegate
                var invokeMethod = this.BuiltProxyType?.GetMethod("Invoke");
                if (invokeMethod is null)
                    throw new InvalidOperationException($"Couldn't find the Invoke method for generated proxy delegate type '{this.ProxyInfo.Proxy.Type.Name}'."); // should never happen
                var @delegate = Delegate.CreateDelegate(this.ProxyInfo.Proxy.Type, proxyInstance, invokeMethod);
                this.ProxyCache.Add(targetInstance, @delegate);
                return @delegate;
            }
        }

        /// <inheritdoc/>
        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            if (potentialProxyInstance is not IInternalProxyObject proxyInstance)
            {
                targetInstance = null;
                return false;
            }

            targetInstance = proxyInstance.ProxyTargetInstance;
            return true;
        }

        private record MethodProxyInfo(
            MethodInfo Proxy,
            MethodInfo Target,
            TypeUtilities.PositionConversion?[] PositionConversions,
            List<ProxyInfo<Context>> RelatedProxyInfos
        );

        internal interface IInternalProxyObject
        {
            object ProxyTargetInstance { get; }
        }
    }
}
