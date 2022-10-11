using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    public sealed class DummyProxyProvider : IProxyProvider
    {
        public static DummyProxyProvider Instance { get; private set; } = new();

        private DummyProxyProvider() { }

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider)
        {
            processor = null;
            return false;
        }
    }
}