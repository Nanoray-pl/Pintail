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
            bool result = CanProxy<TOriginal, TProxy>();
            processor = result ? new DelegateProxyProcessor<TOriginal, TProxy>(Priority, original, ObtainProxy<TOriginal, TProxy>) : null;
            return result;
        }

        private static bool CanProxy<TOriginal, TProxy>()
            => typeof(TOriginal) == typeof(TProxy);

        private static TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original)
            => Unsafe.As<TOriginal, TProxy>(ref original);
    }
}