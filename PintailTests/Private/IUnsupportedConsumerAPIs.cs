namespace Nanoray.Pintail.Tests.Consumer
{
    public interface IInvalidConsumerApi
    {
        public void NonExistentApiMethod();
    }

    public interface IInvalidNotMatchingArrayInput
    {
        public void NotMatchingArrayInput(int[] input);
    }

    public interface IInvalidNotMatchingEnumBackingField
    {
        public void NotMatchingEnumBackingType(StateEnum @enum);
    }

    public interface IInvalidIncorrectByRef
    {
        public void NotMatchingIncorrectByRef(int value);
    }

    public interface IRequiresBoxing
    {
        public void TestMethod(int value);
    }
}
