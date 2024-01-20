namespace Nanoray.Pintail.Tests.Private
{
    public interface IConsumerApiWithDefaultMethods
    {
        int CallHookNewMethod(IHook hook);

        int CallHookNewMethod();
        void SetHook(IHook hook);

        public interface IHook
        {
            int ExistingMethod() => 123;
            int NewMethod(string text) => 456;
        }

        public class HookImplementation : IHook
        {
        }
    }
}
