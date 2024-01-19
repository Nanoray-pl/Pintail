namespace Nanoray.Pintail.Tests.Private
{
    public class ProviderApiWithDefaultMethods
    {
        public int CallHookNewMethod(IHook hook)
            => hook.NewMethod();

        public interface IHook
        {
            int ExistingMethod() => 123;
            int NewMethod() => 456;
        }
    }
}
