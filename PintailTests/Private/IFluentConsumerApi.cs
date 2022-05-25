namespace Nanoray.Pintail.Tests.Consumer
{
    public interface IFluentConsumerApi
    {
        public int valueState { get; set; }

        public string referenceState { get; set; }

        public IFluentConsumerApi DoSomething();

        public IFluentConsumerApi MethodWithOverload();
    }

    public interface IFluentConsumerApiManager
    {
        public IFluentConsumerApi GetOne();
    }
}
