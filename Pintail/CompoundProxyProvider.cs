using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.Pintail
{
    public sealed class CompoundProxyProvider : IProxyProvider
    {
        private List<IProxyProvider> Providers { get; }

        public CompoundProxyProvider(params IProxyProvider[] providers)
        {
            this.Providers = new(providers);
        }

        public CompoundProxyProvider(IReadOnlyList<IProxyProvider> providers)
        {
            this.Providers = new(providers);
        }

        public bool CanProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider = null)
            => Providers.Any(provider => provider.CanProxy<TOriginal, TProxy>(original, rootProvider));

        public TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider = null)
            => Providers.First(provider => provider.CanProxy<TOriginal, TProxy>(original, rootProvider))
                .ObtainProxy<TOriginal, TProxy>(original, rootProvider);
    }
}