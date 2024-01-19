namespace Nanoray.Pintail.Tests.Private
{
    public interface IConsumerApiWithDefaultMethods
    {
        int CallHookNewMethod(IHook hook);

        public interface IHook
        {
            int ExistingMethod() => 123;
        }

        public class HookImplementation : IHook
        {
        }
    }
}
