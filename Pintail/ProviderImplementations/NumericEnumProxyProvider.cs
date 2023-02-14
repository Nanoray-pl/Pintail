using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    public sealed class NumericEnumProxyProvider : IProxyProvider
    {
        public static double DefaultPriority { get; private set; } = 0.8;

        public double Priority { get; private init; }

        public NumericEnumProxyProvider(double? priority = null)
        {
            this.Priority = priority is null ? DefaultPriority : priority.Value;
        }

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider)
        {
            bool result = CanProxy<TOriginal, TProxy>();
            processor = result ? new DelegateProxyProcessor<TOriginal, TProxy>(this.Priority, original, ObtainProxy<TOriginal, TProxy>) : null;
            return result;
        }

        private static bool CanProxy<TOriginal, TProxy>()
            => typeof(TOriginal).IsEnum && typeof(TProxy).IsEnum && typeof(TOriginal).GetEnumUnderlyingType() == typeof(TProxy).GetEnumUnderlyingType();

        private static TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original)
            => Unsafe.As<TOriginal, TProxy>(ref original);
    }
}
