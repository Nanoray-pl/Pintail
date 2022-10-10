using System;

namespace Nanoray.Pintail
{
    public sealed class DummyProxyProvider : IProxyProvider
    {
        public static DummyProxyProvider Instance { get; private set; } = new();

        private DummyProxyProvider() { }

        public bool CanProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider = null)
            => false;

        public TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider = null)
            => throw new ArgumentException($"{typeof(DummyProxyProvider).Name} cannot proxy any value.");
    }
}