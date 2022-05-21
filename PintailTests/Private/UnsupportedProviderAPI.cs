using System.Reflection.Emit;

namespace Nanoray.Pintail.Tests.Provider
{
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

    public interface IProxiedInput
    {
        public string teststring { get; set; }
    }

    public interface IProxiedInput2
    {
        public string otherteststring { get; set; }
    }
}
