using System;
using System.Collections.Generic;
using System.Text;

namespace Nanoray.Pintail.Tests.Consumer
{
    public delegate IApiResult CustomDelegate(IReadOnlyList<IApiResult> list);
    public delegate void CustomGenericOutDelegate<T>(out T param);

    public interface IATestClass
    {
        public interface IInnerClass
        {
            string sigh { get; }
        }

        string? Name { get; }

        IInnerClass[] inner { get; }
    }

    public enum StateEnum
    {
        State0, State1, State2, StateNonExisting
    }

    public interface IApiResult
    {
        string Text { get; }
    }

    public class ApiResult: IApiResult
    {
        public string Text { get; private set; }

        public ApiResult(string text)
        {
            this.Text = text;
        }
    }

    public interface IProxiedInput
    {
        string teststring { get; set; }
    }

    public interface IProxiedInput2
    {
        string otherteststring { get; set; }
    }

    public interface IProxiedInputWithGenerics<T>
    {
        T Value { get; set; }
    }

    public interface IProxiedInputWithTwoGenerics<T, U>
    {
        T TValue { get; set; }
        U UValue { get; set; }
    }


    public class ProxiedInput: IProxiedInput
    {
        public string teststring { get; set; }

        public ProxiedInput(string teststring)
        {
            this.teststring = teststring;
        }
    }

    public class ProxiedInput2: IProxiedInput2
    {
        public string otherteststring { get; set; }
        public ProxiedInput2(string otherteststring)
        {
            this.otherteststring = otherteststring;
        }
    }

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

    public class InputWithGeneric<T>: IInputWithGeneric<T>
    {
        public string? RefProperty { get; set; }
        public int ValProperty { get; set; }
        public T? GenericProperty { get; set; }

        public T? GetValue() => default;
        public void SetValue(T? value) => this.GenericProperty = value;
    }

    public class InputWithTwoGenerics<T, U>: IInputWithTwoGenerics<T, U>
    {
        public string? RefProperty { get; set; }
        public int ValProperty { get; set; }
        public T? GenericProperty { get; set; }

        public T? GetValue() => default;
        public void SetValue(T? value) => this.GenericProperty = value;
    }

    public interface ISimpleConsumerFluentAPI
    {
        int state { get; set; }

        ISimpleConsumerFluentAPI method();

        public int GetOtherState();
    }

    public interface ISimpleConsumerApi<T>
    {
        void SetValue(T? value);
        T? GetValue();
    }

    public interface ISimpleConsumerApi
    {
        void VoidMethod();
        int IntMethod(int num);
        void OutIntMethod(out int num);
        void OutObjectMethod(out object? obj);
        void RefIntMethod(ref int num);
        void RefObjectMethod(ref object? obj);
        int DefaultMethod(int num);
        int IntProperty { get; }
        string this[string key] { get; }
        R MapperMethod<T, R>(T t, Func<T, R> mapper);
        //object? IsAssignableTest(string? anyObj);

        string InMethod(in string str);

        KeyValuePair<int, int> InStructMethod(in KeyValuePair<int, int> test);
    }

    public interface IComplexConsumerApi: ISimpleConsumerApi
    {
        public IList<IProxiedInput> list { get; }

        StateEnum GetStateEnum();
        void GetOutStateEnum(out StateEnum state);
        StateEnum GetSameEnumState(StateEnum state);

        IApiResult[] GetArray();
        IApiResult[][] GetJaggedArray();
        IApiResult[,] Get2DArray();
        void ArrayMethod(IApiResult[] array);
        IList<IApiResult> GetList();
        IApiResult? GetNullableObject(string text);
        StateEnum? GetNullableEnum();
        StateEnum? GetNullEnum();
        ValueTuple<IApiResult, StateEnum> GetValueTuple(ValueTuple<IApiResult, StateEnum> tuple);
        Tuple<IApiResult, StateEnum> GetTuple(Tuple<IApiResult, StateEnum> tuple);

        void GetOutResult(string text, out IApiResult result);
        IApiResult GetSameResult(IApiResult result);
        IApiResult GetModifiedResult(IApiResult result);

        IDictionary<StateEnum, ISet<IApiResult>> GetComplexType();

        Func<IApiResult, IApiResult> GetMapper();
        void SetMapper(Func<IApiResult, IApiResult> mapper);

        void SetCustomDelegate(CustomDelegate @delegate);
        IApiResult CallCustomDelegate(IReadOnlyList<IApiResult> list);

        CustomGenericOutDelegate<StateEnum> GetCustomOutDelegate();
        void SetCustomOutDelegate(CustomGenericOutDelegate<StateEnum> @delegate);

        void RegisterSimpleOption(IApiResult result, string optionName, string optionDesc, Func<bool> optionGet, Action<bool> optionSet);
        void RegisterSimpleOption(IApiResult result, string optionName, string optionDesc, Func<string> optionGet, Action<string> optionSet);

        IList<T> ComplexGenericMethod<T>(string key);

        EnumType? EnumConstrainedGenericMethod<EnumType>(string name) where EnumType : notnull, Enum;
        T ConstructorConstrainedGenericMethod<T>() where T : new();

        void FireStringEvent(string value);
        event Action<string>? StringEvent;

        void FireApiResultEvent(IApiResult value);
        event Action<IApiResult>? ApiResultEvent;

        KeyValuePair<bool, IProxiedInput> GetKeyValuePairWithProxiedValue(bool key, string wrappedValue);

        Type MethodWithOverload(object value);
        Type MethodWithOverload(int value);
        Type MethodWithOverload(StringBuilder value);
        Type MethodWithOverload(DayOfWeek value);

        Type MethodWithOverload(out int value);

        string MethodWithOverload(double value);
        string MethodWithOverload(IProxiedInput proxy);
        string MethodWithOverload(Func<IProxiedInput> callback);
        string MethodWithOverload(Func<IProxiedInput2> callback);
    }

    public interface IProxyInputA
    {
        string hi { get; set; }
    }

    public interface IProxyInputB
    {
        string bye { get; set; }
    }

    public class ProxyInputA: IProxyInputA
    {
        public string hi { get; set; }

        public ProxyInputA(string hi)
        {
            this.hi = hi;
        }
    }

    public class ProxyInputB: IProxyInputB
    {
        public string bye { get; set; }

        public ProxyInputB(string bye)
        {
            this.bye = bye;
        }
    }

    public interface IConsumerWithTwoProxiedInputs
    {
        void MethodWithTwoInputs(IProxyInputA a, IProxyInputB b);

        void MethodWithNoOverload(IProxyInputA a);

        string MethodWithProxiedOverload(IProxyInputA value);
        string MethodWithProxiedOverload(IProxyInputB value);
    }
}
