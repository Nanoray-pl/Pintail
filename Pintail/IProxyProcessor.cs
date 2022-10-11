using System;

namespace Nanoray.Pintail
{
    public interface IProxyProcessor<TOriginal, TProxy>
    {
        double Priority { get; }
        TOriginal Original { get; }

        TProxy ObtainProxy();
    }
}