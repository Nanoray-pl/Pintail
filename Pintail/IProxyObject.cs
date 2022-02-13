namespace Nanoray.Pintail
{
    public interface IProxyObject
    {
        public interface IWithProxyTargetInstanceProperty: IProxyObject
        {
            object ProxyTargetInstance { get; }
        }
    }
}
