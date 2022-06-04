namespace Nanoray.Pintail.Tests.Provider
{
    public class ProviderApiWithOverrides
    {
        public virtual string MethodWithoutOverride() => "heya";

        public virtual string MethodWithOverride() => "BASESTRING";
    }

    public class ProviderApiWithOverridesMeow: ProviderApiWithOverrides
    {
        public override string MethodWithOverride() => "MEOW";
    }
}
