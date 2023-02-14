using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider)
        {
            foreach (var provider in this.Providers)
            {
                if (provider.CanProxy<TOriginal, TProxy>(original, out var providerProcessor, rootProvider))
                {
                    processor = providerProcessor;
                    return true;
                }
            }

            processor = null;
            return false;
        }
    }
}
