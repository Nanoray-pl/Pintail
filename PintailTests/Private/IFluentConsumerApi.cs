using System;
using System.Collections.Generic;

namespace Nanoray.Pintail.Tests.Consumer
{
    public interface IFluentConsumerApi
    {
        int valueState { get; set; }

        string referenceState { get; set; }

        int Prop { get; set; }

        IFluentConsumerApi DoSomething();

        IFluentConsumerApi MethodWithOverload();

        IFluentConsumerApi MethodWithOverload(int testmethod);

        IFluentConsumerApi MethodWithOverload(IProxiedInput testmethod);

        IFluentConsumerApi MethodWithDelegates(Func<IProxiedInput> func, Action<IProxiedInput> action);

        IFluentConsumerApi ArrayMethod(IProxiedInput[] arraymethod);

        IProxiedInput[]? ArrayReturn();

        IList<IProxiedInput>? ListReturn();

        //public IList<IProxiedInput> list { get; }
    }

    public interface IFluentConsumerApiManager
    {
        IFluentConsumerApi GetOne();
    }
}
