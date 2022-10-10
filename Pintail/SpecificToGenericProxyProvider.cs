using System;

namespace Nanoray.Pintail
{
    public class SpecificToGenericProxyProvider<TSpecificOriginal, TSpecificProxy> : IProxyProvider
    {
        private IProxyProvider<TSpecificOriginal, TSpecificProxy> Wrapped { get; }

        public SpecificToGenericProxyProvider(IProxyProvider<TSpecificOriginal, TSpecificProxy> wrapped)
        {
            this.Wrapped = wrapped;
        }

        public bool CanProxy<TGenericOriginal, TGenericProxy>(TGenericOriginal original, IProxyProvider? rootProvider)
        {
            if (!typeof(TGenericOriginal).IsAssignableTo(typeof(TSpecificOriginal)))
                return false;
            if (!typeof(TGenericProxy).IsAssignableFrom(typeof(TSpecificProxy)))
                return false;
            return Wrapped.CanProxy((TSpecificOriginal)(object)original, rootProvider);
        }

        public TGenericProxy ObtainProxy<TGenericOriginal, TGenericProxy>(TGenericOriginal original, IProxyProvider? rootProvider)
        {
            if (!typeof(TGenericOriginal).IsAssignableTo(typeof(TSpecificOriginal)))
                throw new ArgumentException($"Provided object of type {typeof(TGenericOriginal).Name} cannot be proxied to type {typeof(TGenericProxy).Name}");
            if (!typeof(TGenericProxy).IsAssignableFrom(typeof(TSpecificProxy)))
                throw new ArgumentException($"Provided object of type {typeof(TGenericOriginal).Name} cannot be proxied to type {typeof(TGenericProxy).Name}");
            return (TGenericProxy)(object)Wrapped.ObtainProxy((TSpecificOriginal)(object)original, rootProvider);
        }
    }
}