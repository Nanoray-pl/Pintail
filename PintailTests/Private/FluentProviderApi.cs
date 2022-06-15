using System;
using System.Collections.Generic;
using System.Linq;

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

        private IProxiedInput[]? array;

        //public IList<IProxiedInput> list { get; } = new List<IProxiedInput>();

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

        public IFluentProviderApi ArrayMethod(IProxiedInput[] arraymethod)
        {
            this.array = arraymethod;
            return this;
        }

        public IProxiedInput[]? ArrayReturn() => this.array;

        public IList<IProxiedInput>? ListReturn() => this.array?.ToList();
    }

    public interface IFluentProviderApi
    {
        int valueState { get; set; }

        string? referenceState { get; set; }

        IFluentProviderApi DoSomething();

        IFluentProviderApi MethodWithOverload();

        IFluentProviderApi MethodWithOverload(int testmethod);

        IFluentProviderApi MethodWithOverload(IProxiedInput testmethod);

        IFluentProviderApi MethodWithDelegates(Func<IProxiedInput> func, Action<IProxiedInput> action);

        IFluentProviderApi NewMethodAdded();

        IFluentProviderApi ArrayMethod(IProxiedInput[] arraymethod);

        IProxiedInput[]? ArrayReturn();

        IList<IProxiedInput>? ListReturn();

        int Prop { get; set; }
    }

    public interface IFluentProviderResult
    {
        int state { get; set; }
    }

    public interface IFluentProviderGeneric<T> where T : new()
    {
        T value { get; set; }
    }
}
