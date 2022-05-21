using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Nanoray.Pintail.Tests.Provider
{
    internal class ProviderApiWithOverloads
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
    }
}
