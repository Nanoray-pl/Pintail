using System.Reflection.Emit;

namespace Nanoray.Pintail.Tests.Provider
{
    public enum ATooBigEnum { Not, Enough, Values }

    public class EnumInsufficientlyBig
    {
        public ATooBigEnum method() => ATooBigEnum.Enough;
    }

    public class InvalidNotMatchingEnumBackingField
    {
        public void NotMatchingEnumBackingType(UIntEnum @enum) { }
    }

    public class InvalidNotMatchingArrayInput
    {
        public void NotMatchingArrayInput(LocalBuilder[] input) { }
    }

    public class InvalidIncorrectByRef
    {
        public void NotMatchingIncorrectByRef(out int value)
        {
            value = 5;
        }
    }

    public class RequiresBoxing
    {
        public object TestMethod(object value) => value;
    }

    public class RequiresUnboxing
    {
        public object TestMethod(int value) => value;
    }
}
