namespace Nanoray.Pintail.Tests.Private
{
    public class ProviderApiWithDefaultMethods
    {
        private IHook Hook = null!;

        public int CallHookNewMethod(IHook hook)
            => hook.NewMethod("asdf");

        public int CallHookNewMethod()
            => this.CallHookNewMethod(this.Hook);

        public void SetHook(IHook hook)
            => this.Hook = hook;

        public interface IHook
        {
            int ExistingMethod() => 123;
            int NewMethod(string text) => 456;
            int NewMethod(int number) => this.NewMethod(number.ToString());
        }
    }
}
