using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Nanoray.Pintail.Tests.Consumer
{

    public interface ISimpleConsumerApiWithOverloads
    {
        public Type MethodWithOverload(object value);
        public Type MethodWithOverload(int value);
        public Type MethodWithOverload(StringBuilder value);
        public Type MethodWithOverload(DayOfWeek value);

        public Type MethodWithOverload(out int value);
        public string MethodWithOverload(double value);

    }

    public interface IComplexConsumerApiWithOverloads : ISimpleConsumerApiWithOverloads
    {
        public string MethodWithOverload(IProxiedInput proxy);
        public string MethodWithOverload(Func<IProxiedInput> callback);
        public string MethodWithOverload(Func<IProxiedInput2> callback);

        public string MethodWithArrayOverload(LocalVariableInfo[] locals);
        public string MethodWithArrayOverload(LocalBuilder[] locals); // LocalBuilder inherits from LocalVariableInfo.

        public string MethodWithArrayOverload(int[] locals);

        public string MethodWithArrayOverload(IProxiedInput[] locals);
    }

    public interface IConsumerApiWithOverloadsWithGenerics : ISimpleConsumerApiWithOverloads
    {
        public string MethodWithOverload<T>(IInputWithGeneric<T> proxy);

        public string MethodWithOverload(IInputWithGeneric<string> proxy);

        public string MethodWithOverload(IInputWithTwoGenerics<string,int> proxy);
    }

}
