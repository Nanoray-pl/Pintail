using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    internal class ArrayProxyFactory<Context>: IProxyFactory<Context>
    {
        public ProxyInfo<Context> ProxyInfo { get; private set; }
        private readonly ProxyManagerMismatchedArrayMappingBehavior MismatchedArrayMappingBehavior;

        internal ArrayProxyFactory(ProxyInfo<Context> proxyInfo, ProxyManagerMismatchedArrayMappingBehavior mismatchedArrayMappingBehavior)
        {
            if (!proxyInfo.Target.Type.IsArray)
                throw new ArgumentException($"{proxyInfo.Target.Type.GetShortName()} is not an array.");
            if (!proxyInfo.Proxy.Type.IsArray)
                throw new ArgumentException($"{proxyInfo.Proxy.Type.GetShortName()} is not an array.");
            this.ProxyInfo = proxyInfo;
            this.MismatchedArrayMappingBehavior = mismatchedArrayMappingBehavior;
        }

        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            if (targetInstance is not Array)
                throw new ArgumentException($"{targetInstance} is not an array.");
            return this.MapArray(manager, (Array)targetInstance);
        }

        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            if (potentialProxyInstance is not Array)
                throw new ArgumentException($"{potentialProxyInstance} is not an array.");
            targetInstance = this.MapArray(manager, (Array)potentialProxyInstance);
            return true;
        }

        private Array MapArray(IProxyManager<Context> manager, Array inputArray)
        {
            int[] lengths = new int[inputArray.Rank];
            for (int i = 0; i < lengths.Length; i++)
                lengths[i] = inputArray.GetLength(i);
            var proxyInfo = this.ProxyInfo.Target.Type.IsAssignableFrom(inputArray.GetType()) ? this.ProxyInfo : this.ProxyInfo.Reversed();
            var outputArray = Array.CreateInstance(proxyInfo.Proxy.Type.GetElementType()!, lengths);
            this.MapArrayContents(manager, inputArray, outputArray);
            return outputArray;
        }

        internal void MapArrayContents(IProxyManager<Context> manager, Array inputArray, Array outputArray)
        {
            if (inputArray.Rank != outputArray.Rank)
                throw new ArgumentException("Arrays have different dimension counts.");
            for (int i = 0; i < inputArray.Rank; i++)
                if (inputArray.GetLength(i) != outputArray.GetLength(i))
                    throw new ArgumentException("Arrays have different lengths.");

            var elementProxyInfo = this.ProxyInfo.Target.Type.IsAssignableFrom(inputArray.GetType()) ? this.ProxyInfo : this.ProxyInfo.Reversed();
            var elementUnproxyInfo = this.ProxyInfo.Target.Type.IsAssignableFrom(inputArray.GetType()) ? this.ProxyInfo.Reversed() : this.ProxyInfo;
            elementProxyInfo = elementProxyInfo.Copy(targetType: elementProxyInfo.Target.Type.GetElementType()!, proxyType: elementProxyInfo.Proxy.Type.GetElementType()!);
            elementUnproxyInfo = elementUnproxyInfo.Copy(targetType: elementUnproxyInfo.Target.Type.GetElementType()!, proxyType: elementUnproxyInfo.Proxy.Type.GetElementType()!);

            Type outputElementType = outputArray.GetType().GetElementType()!;
            if (outputElementType != elementProxyInfo.Proxy.Type)
            {
                switch (this.MismatchedArrayMappingBehavior)
                {
                    case ProxyManagerMismatchedArrayMappingBehavior.Throw:
                        throw new ArgumentException($"Array uses more concrete type {outputElementType.GetShortName()} than expected {elementProxyInfo.Proxy.Type.GetShortName()}, cannot map.");
                    case ProxyManagerMismatchedArrayMappingBehavior.AllowAndDontMapBack:
                        return;
                }
            }

            void Map(int[] position)
            {
                int dimension = 0;
                while (dimension < position.Length && position[dimension] != -1)
                    dimension++;

                if (dimension == position.Length)
                {
                    object? outputValue = inputArray.GetValue(position);
                    if (outputValue is null)
                    {
                        outputArray.SetValue(null, position);
                        return;
                    }

                    var unproxyFactory = manager.GetProxyFactory(elementUnproxyInfo);
                    if (unproxyFactory is not null && unproxyFactory.TryUnproxy(manager, outputValue, out object? targetInstance))
                    {
                        outputArray.SetValue(targetInstance, position);
                        return;
                    }
                    var proxyFactory = manager.ObtainProxyFactory(elementProxyInfo);
                    object proxyInstance = proxyFactory.ObtainProxy(manager, outputValue);
                    outputArray.SetValue(proxyInstance, position);
                }
                else
                {
                    for (int i = 0; i < inputArray.GetLength(dimension); i++)
                    {
                        position[dimension] = i;
                        Map(position);
                    }
                    position[dimension] = -1;
                }
            }

            int[] position = new int[inputArray.Rank];
            for (int i = 0; i < position.Length; i++)
                position[i] = -1;
            Map(position);
        }
    }
}
