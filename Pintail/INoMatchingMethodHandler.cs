using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.Pintail
{
    /// <summary>
    /// A type which defines the behavior to use if a given proxy method could not be implemented when using <see cref="ProxyManager{Context}"/>.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public interface INoMatchingMethodHandler<Context>
    {
        /// <summary>
        /// A method called whenever a given proxy method could not be implemented when using <see cref="ProxyManager{Context}"/>.
        /// </summary>
        /// <param name="proxyBuilder">The <see cref="TypeBuilder"/> used to construct this specific proxy type.</param>
        /// <param name="proxyInfo">The proxy information describing this specific proxy process.</param>
        /// <param name="targetField">The field holding the proxied instance.</param>
        /// <param name="glueField">The field holding a <see cref="ProxyGlue{Context}"/> instance.</param>
        /// <param name="proxyInfosField">The field holding an <see cref="System.Collections.Generic.IList{ProxyInfo}"/> instance where <c>T</c> is <see cref="ProxyInfo{Context}"/>.</param>
        /// <param name="proxyMethod">The method that is being proxied.</param>
        void HandleNoMatchingMethod(TypeBuilder proxyBuilder, ProxyInfo<Context> proxyInfo, FieldBuilder targetField, FieldBuilder glueField, FieldBuilder proxyInfosField, MethodInfo proxyMethod);
    }

    /// <summary>
    /// The default <see cref="INoMatchingMethodHandler{Context}"/> implementation.<br/>
    /// If a method cannot be implemented, <see cref="ArgumentException"/> will be thrown right away.
    /// </summary>
    public sealed class ThrowingDuringProxyingNoMatchingMethodHandler<Context> : INoMatchingMethodHandler<Context>
    {
        /// <inheritdoc/>
        public void HandleNoMatchingMethod(TypeBuilder proxyBuilder, ProxyInfo<Context> proxyInfo, FieldBuilder targetField, FieldBuilder glueField, FieldBuilder proxyInfosField, MethodInfo proxyMethod)
        {
            throw new ArgumentException($"The {proxyInfo.Proxy.Type.GetShortName()} interface defines method {proxyMethod.Name} which doesn't exist in the API or depends on an interface that cannot be mapped!");
        }
    }

    /// <summary>
    /// If a method cannot be implemented, a blank implementation will be created instead, which will throw <see cref="NotImplementedException"/> when called.
    /// </summary>
    public sealed class ThrowingImplementationNoMatchingMethodHandler<Context> : INoMatchingMethodHandler<Context>
    {
        /// <inheritdoc/>
        public void HandleNoMatchingMethod(TypeBuilder proxyBuilder, ProxyInfo<Context> proxyInfo, FieldBuilder targetField, FieldBuilder glueField, FieldBuilder proxyInfosField, MethodInfo proxyMethod)
        {
            MethodBuilder methodBuilder = proxyBuilder.DefineMethod(proxyMethod.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);

            Type[] proxyGenericArguments = proxyMethod.GetGenericArguments();
            string[] genericArgNames = proxyGenericArguments.Select(a => a.Name).ToArray();
            GenericTypeParameterBuilder[] genericTypeParameterBuilders = proxyGenericArguments.Length == 0 ? Array.Empty<GenericTypeParameterBuilder>() : methodBuilder.DefineGenericParameters(genericArgNames);
            for (int i = 0; i < proxyGenericArguments.Length; i++)
                genericTypeParameterBuilders[i].SetGenericParameterAttributes(proxyGenericArguments[i].GenericParameterAttributes);

            Type returnType = proxyMethod.ReturnType.IsGenericMethodParameter ? genericTypeParameterBuilders[proxyMethod.ReturnType.GenericParameterPosition] : proxyMethod.ReturnType;
            methodBuilder.SetReturnType(returnType);

            Type[] argTypes = proxyMethod.GetParameters()
                .Select(a => a.ParameterType)
                .Select(t => t.IsGenericMethodParameter ? genericTypeParameterBuilders[t.GenericParameterPosition] : t)
                .ToArray();
            methodBuilder.SetParameters(argTypes);

            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldstr, $"The {proxyInfo.Proxy.Type.GetShortName()} interface defines method {proxyMethod.Name} which doesn't exist in the API. (It may depend on an interface that was not mappable).");
            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) })!);
            il.Emit(OpCodes.Throw);
        }
    }
}
