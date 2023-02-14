using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    public sealed class AssignableTypeProxyProvider : IProxyProvider
    {
        public static double DefaultAssignableButNotSameTypePriority { get; private set; } = 0.9;

        private static IProxyProvider SameTypeProxyProvider { get; set; } = Pintail.SameTypeProxyProvider.Instance;

        public double AssignableButNotSameTypePriority { get; private init; }

        public AssignableTypeProxyProvider(double? assignableButNotSameTypePriority = null)
        {
            this.AssignableButNotSameTypePriority = assignableButNotSameTypePriority is null ? DefaultAssignableButNotSameTypePriority : assignableButNotSameTypePriority.Value;
        }

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider)
        {
            if (SameTypeProxyProvider.CanProxy(original, out processor, rootProvider))
                return true;

            bool result = CanProxy<TOriginal, TProxy>();
            processor = result ? new DelegateProxyProcessor<TOriginal, TProxy>(this.AssignableButNotSameTypePriority, original, ObtainProxy<TOriginal, TProxy>) : null;
            return result;
        }

        private static bool CanProxy<TOriginal, TProxy>()
            => typeof(TOriginal).IsAssignableTo(typeof(TProxy));

        private static TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original)
            => Unsafe.As<TOriginal, TProxy>(ref original);
    }
}
