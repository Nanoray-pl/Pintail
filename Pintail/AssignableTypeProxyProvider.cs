using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    public sealed class AssignableTypeProxyProvider : IProxyProvider
    {
        public static double DefaultAssignableButNotSameTypePriority { get; private set; } = 0.9;

        private static IProxyProvider SameTypeProxyProvider { get; set; } = Nanoray.Pintail.SameTypeProxyProvider.Instance;

        public double AssignableButNotSameTypePriority { get; private init; }

        public AssignableTypeProxyProvider(double? assignableButNotSameTypePriority = null)
        {
            this.AssignableButNotSameTypePriority = assignableButNotSameTypePriority is null ? DefaultAssignableButNotSameTypePriority : assignableButNotSameTypePriority.Value;
        }

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider)
        {
            if (SameTypeProxyProvider.CanProxy<TOriginal, TProxy>(original, out processor, rootProvider))
                return true;

            var result = CanProxy<TOriginal, TProxy>(original, rootProvider);
            processor = result ? new DelegateProxyProcessor<TOriginal, TProxy>(AssignableButNotSameTypePriority, original, o => ObtainProxy<TOriginal, TProxy>(o, rootProvider)) : null;
            return result;
        }

        private static bool CanProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => typeof(TOriginal).IsAssignableTo(typeof(TProxy));

        private static TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => Unsafe.As<TOriginal, TProxy>(ref original);
    }
}