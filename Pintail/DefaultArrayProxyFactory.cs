using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    internal class DefaultArrayProxyFactory<Context>: IProxyFactory<Context>
    {
        public ProxyInfo<Context> ProxyInfo { get; private set; }

        internal DefaultArrayProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            if (!proxyInfo.Target.Type.IsArray)
                throw new ArgumentException($"{proxyInfo.Target.Type.GetBestName()} is not an array.");
            if (!proxyInfo.Proxy.Type.IsArray)
                throw new ArgumentException($"{proxyInfo.Proxy.Type.GetBestName()} is not an array.");
            this.ProxyInfo = proxyInfo;
        }

        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            if (targetInstance is not Array)
                throw new ArgumentException($"{targetInstance} is not an array.");
            return this.MapArray(manager, (Array)targetInstance, this.ProxyInfo.Proxy.Type.GetElementType()!);
        }

        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            if (potentialProxyInstance is not Array)
                throw new ArgumentException($"{potentialProxyInstance} is not an array.");
            targetInstance = this.MapArray(manager, (Array)potentialProxyInstance, this.ProxyInfo.Target.Type.GetElementType()!);
            return true;
        }

        private Array MapArray(IProxyManager<Context> manager, Array inputArray, Type outputType)
        {
            var outputArray = Array.CreateInstance(outputType, inputArray.Length);
            this.MapArrayContents(manager, inputArray, outputArray);
            return outputArray;
        }

        internal void MapArrayContents(IProxyManager<Context> manager, Array inputArray, Array outputArray)
        {
            if (inputArray.Length != outputArray.Length)
                throw new ArgumentException("Arrays have different lengths.");
            var proxyInfo = this.ProxyInfo.Copy(targetType: inputArray.GetType().GetElementType()!, proxyType: outputArray.GetType().GetElementType()!);
            var unproxyInfo = this.ProxyInfo.Copy(targetType: outputArray.GetType().GetElementType()!, proxyType: inputArray.GetType().GetElementType()!);
            for (int i = 0; i < inputArray.Length; i++)
            {
                object? outputValue = inputArray.GetValue(i);
                if (outputValue is null)
                {
                    outputArray.SetValue(null, i);
                    continue;
                }

                var unproxyFactory = manager.GetProxyFactory(unproxyInfo);
                if (unproxyFactory is not null && unproxyFactory.TryUnproxy(manager, outputValue, out object? targetInstance))
                {
                    outputArray.SetValue(targetInstance, i);
                    continue;
                }
                var proxyFactory = manager.ObtainProxyFactory(proxyInfo);
                outputArray.SetValue(proxyFactory.ObtainProxy(manager, outputValue), i);
            }
        }
    }
}
