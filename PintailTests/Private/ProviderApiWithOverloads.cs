using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Nanoray.Pintail.Tests.Provider
{

    public interface IInputWithGeneric<T>
    {
        string? RefProperty { get; set; }
        int ValProperty { get; set; }
        T? GenericProperty { get; set; }

        void SetValue(T? value);
        T? GetValue();
    }

    public interface IInputWithTwoGenerics<T, U>
    {
        string? RefProperty { get; set; }
        int ValProperty { get; set; }
        T? GenericProperty { get; set; }

        void SetValue(T? value);
        T? GetValue();
    }

    public class SimpleProviderApiWithOverloads
    {
        // base object.
        public Type MethodWithOverload(object value)
            => typeof(object);

        // value type.
        public Type MethodWithOverload(int value)
            => typeof(int);

        // reference type
        public Type MethodWithOverload(StringBuilder value)
            => typeof(StringBuilder);

        // enum
        public Type MethodWithOverload(DayOfWeek value)
            => typeof(DayOfWeek);

        // out params.
        public Type MethodWithOverload(out int value)
        {
            value = 5;
            return typeof(int);
        }

        // different return type.
        public string MethodWithOverload(double value)
            => value.ToString();

    }

    public class ComplexProviderApiWithOverloads: SimpleProviderApiWithOverloads
    {
        public string MethodWithOverload(IProxiedInput proxy)
            => "proxy";

        public string MethodWithOverload(Func<IProxiedInput> callback)
            => callback().teststring;

        public string MethodWithOverload(Func<IProxiedInput2> callback)
            => callback().otherteststring;

        public string MethodWithArrayOverload(LocalVariableInfo[] locals)
            => "LocalVariableInfo array!";

        public string MethodWithArrayOverload(LocalBuilder[] locals)
            => "LocalBuilder array!"; // LocalBuilder inherits from LocalVariableInfo.

        public string MethodWithArrayOverload(int[] locals)
            => "int array!";

        public string MethodWithArrayOverload(IProxiedInput[] locals)
            => "proxied array!";
    }

    public class ComplexProviderApiWithOverloadsWithGenerics: SimpleProviderApiWithOverloads
    {
        public string MethodWithOverload<T>(IInputWithGeneric<T> proxy)
            => "One Generic";

        public string MethodWithOverload(IInputWithGeneric<string> proxy)
            => "string?";

        public string MethodWithOverload(IInputWithTwoGenerics<string, int> proxy)
            => "string, int";
    }

    public class ProviderWithComplexProxiedInputs
    {
        public string MethodWithProxiedOverload(Func<IProxyInputA> value) => value().hi;

        public string MethodWithProxiedOverload(Func<IProxyInputB> value) => value().bye;

        public event Action<IProxyInputA>? FancyEvent;

        public void FireEvent(IProxyInputA val) => FancyEvent?.Invoke(val);
    }
}
