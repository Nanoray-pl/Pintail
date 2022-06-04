using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Nanoray.Pintail.Tests.Consumer
{
    public interface ISimpleConsumerApiWithOverloads
    {
        Type MethodWithOverload(object value);
        Type MethodWithOverload(int value);
        Type MethodWithOverload(StringBuilder value);
        Type MethodWithOverload(DayOfWeek value);

        Type MethodWithOverload(out int value);
        string MethodWithOverload(double value);
    }

    public interface IComplexConsumerApiWithOverloads: ISimpleConsumerApiWithOverloads
    {
        string MethodWithOverload(IProxiedInput proxy);
        string MethodWithOverload(Func<IProxiedInput> callback);
        string MethodWithOverload(Func<IProxiedInput2> callback);

        string MethodWithArrayOverload(LocalVariableInfo[] locals);
        string MethodWithArrayOverload(LocalBuilder[] locals); // LocalBuilder inherits from LocalVariableInfo.

        string MethodWithArrayOverload(int[] locals);

        string MethodWithArrayOverload(IProxiedInput[] locals);
    }

    public interface IConsumerApiWithOverloadsWithGenerics: ISimpleConsumerApiWithOverloads
    {
        string MethodWithOverload<T>(IInputWithGeneric<T> proxy);

        string MethodWithOverload(IInputWithGeneric<string> proxy);

        string MethodWithOverload(IInputWithTwoGenerics<string, int> proxy);
    }

    public interface IConsumerApiWithComplexProxiedInputs
    {
        string MethodWithProxiedOverload(Func<IProxyInputA> value);

        string MethodWithProxiedOverload(Func<IProxyInputB> value);

        event Action<IProxyInputA> FancyEvent;

        void FireEvent(IProxyInputA val);
    }
}
