namespace Nanoray.Pintail.Tests.Provider
{
    public class FluentProviderApiManager
    {
        public IFluentProviderApi GetOne() => new FluentProviderApi();
    }

    public class FluentProviderApi : IFluentProviderApi
    {
        public int valueState { get; set; }

        public string? referenceState { get; set; }

        private int privateState;

        public int Prop
        {
            get => this.privateState;
            set => this.privateState = value;
        }

        public IFluentProviderApi DoSomething()
        {
            this.valueState = 7;
            return this;
        }

        public IFluentProviderApi MethodWithOverload()
        {
            this.referenceState = "ZeroArgs";
            return this;
        }
    }

    public interface IFluentProviderApi
    {
        public int valueState { get; set; }

        public string referenceState { get; set; }

        public IFluentProviderApi DoSomething();

        public IFluentProviderApi MethodWithOverload();
    }
}
