namespace Nanoray.Pintail.Tests.Consumer
{
    public enum ATooBigEnum { Just, A, Few, Too, Many, Values }

    public interface IInsufficientEnumValues
    {
        ATooBigEnum method();
    }

    public interface IInvalidConsumerApi
    {
        void NonExistentApiMethod();
    }

    public interface IInvalidNotMatchingArrayInput
    {
        void NotMatchingArrayInput(int[] input);
    }

    public interface IInvalidNotMatchingEnumBackingField
    {
        void NotMatchingEnumBackingType(StateEnum @enum);
    }

    public interface IInvalidIncorrectByRef
    {
        void NotMatchingIncorrectByRef(int value);
    }

    public interface IRequiresBoxing
    {
        object TestMethod(int value);
    }

    public interface IRequiresUnboxing
    {
        object TestMethod(object value);
    }
}
