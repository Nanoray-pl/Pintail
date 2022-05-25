using System;

namespace Nanoray.Pintail.Tests.Consumer
{
    public interface IFluentConsumerApi
    {
        public int valueState { get; set; }

        public string referenceState { get; set; }

        public int Prop { get; set; }

        public IFluentConsumerApi DoSomething();

        public IFluentConsumerApi MethodWithOverload();

        //public IFluentConsumerApi MethodWithOverload(int testmethod);

        //public IFluentConsumerApi MethodWithOverload(IProxiedInput testmethod);

        //public IFluentConsumerApi MethodWithOverload(Func<IProxiedInput> func, Action<IProxiedInput> action);
    }

    public interface IFluentConsumerApiManager
    {
        public IFluentConsumerApi GetOne();
    }

}
