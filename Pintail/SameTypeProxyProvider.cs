using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    public sealed class SameTypeProxyProvider : IProxyProvider
    {
        public static SameTypeProxyProvider Instance { get; private set; } = new();

        public static double Priority { get; private set; } = 1;

        private SameTypeProxyProvider() { }

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider)
        {
            var result = CanProxy<TOriginal, TProxy>(original, rootProvider);
            processor = result ? new DelegateProxyProcessor<TOriginal, TProxy>(Priority, original, o => ObtainProxy<TOriginal, TProxy>(o, rootProvider)) : null;
            return result;
        }

        private static bool CanProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => typeof(TOriginal) == typeof(TProxy);

        private static TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => Unsafe.As<TOriginal, TProxy>(ref original);
    }
}