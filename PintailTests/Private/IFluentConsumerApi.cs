using System;
using System.Collections.Generic;

namespace Nanoray.Pintail.Tests.Consumer
{
    public interface IFluentConsumerApi
    {
        public int valueState { get; set; }

        public string referenceState { get; set; }

        public int Prop { get; set; }

        public IFluentConsumerApi DoSomething();

        public IFluentConsumerApi MethodWithOverload();

        public IFluentConsumerApi MethodWithOverload(int testmethod);

        public IFluentConsumerApi MethodWithOverload(IProxiedInput testmethod);

        public IFluentConsumerApi MethodWithDelegates(Func<IProxiedInput> func, Action<IProxiedInput> action);

        public IFluentConsumerApi ArrayMethod(IProxiedInput[] arraymethod);

        public IProxiedInput[]? ArrayReturn();

        public IList<IProxiedInput>? ListReturn();
        //public IList<IProxiedInput> list { get; }
    }

    public interface IFluentConsumerApiManager
    {
        public IFluentConsumerApi GetOne();
    }

}
