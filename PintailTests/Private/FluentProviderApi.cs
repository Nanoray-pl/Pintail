using System;

namespace Nanoray.Pintail.Tests.Provider
{
    public class FluentProviderApiManager
    {
        public IFluentProviderApi GetOne() => new FluentProviderApi();
    }

    public class FluentProviderApi: IFluentProviderApi
    {
        public int valueState { get; set; }

        public string? referenceState { get; set; }

        private int privateState;

        private IProxiedInput? proxied;

        private Func<IProxiedInput>? func;

        private Action<IProxiedInput>? action;

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

        public IFluentProviderApi MethodWithOverload(int testmethod)
        {
            this.privateState = testmethod;
            return this;
        }

        public IFluentProviderApi MethodWithOverload(IProxiedInput testmethod)
        {
            this.proxied = testmethod;
            return this;
        }

        public IFluentProviderApi MethodWithDelegates(Func<IProxiedInput> func, Action<IProxiedInput> action)
        {
            this.action = action;
            this.func = func;
            return this;
        }

        public IFluentProviderApi NewMethodAdded() => this;
    }

    public interface IFluentProviderApi
    {
        public int valueState { get; set; }

        public string? referenceState { get; set; }

        public IFluentProviderApi DoSomething();

        public IFluentProviderApi MethodWithOverload();

        public IFluentProviderApi MethodWithOverload(int testmethod);

        public IFluentProviderApi MethodWithOverload(IProxiedInput testmethod);

        public IFluentProviderApi MethodWithDelegates(Func<IProxiedInput> func, Action<IProxiedInput> action);

        public IFluentProviderApi NewMethodAdded();

        public int Prop { get; set; }
    }

    public interface IFluentProviderResult
    {
        public int state { get; set; }
    }

    public interface IFluentProviderGeneric<T> where T: new()
    {
        public T value { get; set; }
    }
}