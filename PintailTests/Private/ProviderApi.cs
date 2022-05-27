using System;
using System.Collections.Generic;
using System.Text;

namespace Nanoray.Pintail.Tests.Provider
{
    public delegate void CustomGenericOutDelegate<T>(out T param);

    public abstract class ATestClass
    {
        public abstract class InnerClass
        {
            public abstract string sigh { get; }
        }

        public class InnerClassImpl: InnerClass
        {
            public override string sigh { get; } = "helloworld";
        }
        public abstract InnerClass[]? inner { get; }

        public abstract string? Name { get; }
    }

    public class ATestClassImpl : ATestClass
    {
        private class InnerClassImpl : InnerClass
        {
            public override string sigh { get; } = "sigh";
        }

        public override InnerClass[] inner { get; } = new[] { new InnerClassImpl() };

        public override string? Name { get; } = "Hi!";
    }

    public enum StateEnum
    {
        State0, State1, State2
    }

    public enum UIntEnum : uint
    {
        State0 = 0,
        State1 = 1,
        State2 = 2,
    }

    public interface IApiResult
    {
        string Text { get; }
    }

    public class ApiResult : IApiResult
    {
        public string Text { get; private set; }

        public ApiResult(string text)
        {
            this.Text = text;
        }
    }

    public interface IProviderApiDefaultMethods
    {
        int IntMethod(int num);

        int DefaultMethod(int num)
            => this.IntMethod(num * num);
    }

    public class SimpleFluentProviderApi
    {
        public int state { get; set; }
        private int otherstate;

        public SimpleFluentProviderApi method()
        {
            this.state = 10;
            this.otherstate = 1337;
            return this;
        }

        public int GetOtherState() => this.otherstate;

        public SimpleFluentProviderApi unusedMethod() => this;
    }

    public class SimpleProviderApi<T>
    {
        private T? Value;

        public void SetValue(T? value)
        {
            this.Value = value;
        }

        public T? GetValue()
        {
            return this.Value;
        }

        public T? UnusedMethod => this.Value;
    }

    public class SimpleProviderApi : IProviderApiDefaultMethods
    {
        protected Func<IApiResult, IApiResult> Mapper = (r) => r;
        protected CustomGenericOutDelegate<StateEnum> CustomOutDelegate = (out StateEnum p) => p = StateEnum.State0;

        public void VoidMethod() { }

        public int IntMethod(int num)
            => num;

        public void OutIntMethod(out int num)
        {
            num = 1;
        }

        public void OutObjectMethod(out object? obj)
        {
            obj = new StringBuilder();
        }

        public void RefIntMethod(ref int num)
        {
            num = 1;
        }

        public void RefObjectMethod(ref object? obj)
        {
            obj = new StringBuilder();
        }

        public int IntProperty
            => 42;

        public string this[string key]
            => key;

        public R MapperMethod<T, R>(T t, Func<T, R> mapper)
            => mapper(t);

        //public string? IsAssignableTest(object? anyObj)
        //    => anyObj?.ToString();

    }

    public class ComplexProviderApi: SimpleProviderApi
    {

        public IList<IProxiedInput> list { get; } = new List<IProxiedInput>();
        public StateEnum GetStateEnum()
            => StateEnum.State1;

        public void GetOutStateEnum(out StateEnum state)
            => state = StateEnum.State1;

        public StateEnum GetSameEnumState(StateEnum state)
            => state;

        public IApiResult[] GetArray()
            => new IApiResult[] { new ApiResult("0") };

        public IApiResult[][] GetJaggedArray()
            => new IApiResult[][] { new IApiResult[] { new ApiResult("0") } };

        public IApiResult[,] Get2DArray()
        {
            var result = new IApiResult[1, 2];
            result[0, 0] = new ApiResult("0, 0");
            result[0, 1] = new ApiResult("0, 1");
            return result;
        }

        public void ArrayMethod(IApiResult[] array)
            => array[0] = new ApiResult("modified");

        public IList<IApiResult> GetList()
        {
            var list = new List<IApiResult>();
            list.Add(new ApiResult("0"));
            return list;
        }

        public void GetOutResult(string text, out IApiResult result)
            => result = new ApiResult(text);

        public IApiResult GetSameResult(IApiResult result)
            => result;

        public IApiResult GetModifiedResult(IApiResult result)
            => new ApiResult(result.Text);

        public IDictionary<StateEnum, ISet<IApiResult>> GetComplexType()
        {
            return new Dictionary<StateEnum, ISet<IApiResult>>
            {
                [StateEnum.State0] = new HashSet<IApiResult> { new ApiResult("0") }
            };
        }

        public Func<IApiResult, IApiResult> GetMapper()
            => this.Mapper;

        public void SetMapper(Func<IApiResult, IApiResult> mapper)
            => this.Mapper = mapper;

        public CustomGenericOutDelegate<StateEnum> GetCustomOutDelegate()
            => this.CustomOutDelegate;

        public void SetCustomOutDelegate(CustomGenericOutDelegate<StateEnum> @delegate)
            => this.CustomOutDelegate = @delegate;

        public void RegisterSimpleOption(IApiResult result, string optionName, string optionDesc, Func<bool> optionGet, Action<bool> optionSet)
        { }

        public void RegisterSimpleOption(IApiResult result, string optionName, string optionDesc, Func<string> optionGet, Action<string> optionSet)
        { }

        public IList<T> ComplexGenericMethod<T>(string key)
            => new List<T>();

        public EnumType? EnumConstrainedGenericMethod<EnumType>(string name) where EnumType : notnull, Enum
        {
            foreach (object enumValue in Enum.GetValues(typeof(EnumType)))
            {
                if (Enum.GetName(typeof(EnumType), enumValue) == name)
                    return (EnumType)enumValue;
            }
            return default;
        }

        public T ConstructorConstrainedGenericMethod<T>() where T: new()
            => new();

        public void FireStringEvent(string value)
            => StringEvent?.Invoke(value);

        public event Action<string>? StringEvent;

        public void FireApiResultEvent(IApiResult value)
            => ApiResultEvent?.Invoke(value);

        public event Action<IApiResult>? ApiResultEvent;
    }

    public interface IProxyInputA
    {
        public string hi { get; set; }
    }

    public interface IProxyInputB
    {
        public string bye { get; set; }
    }

    public class ProviderWithTwoProxiedInputs
    {
        public void MethodWithTwoInputs(IProxyInputA a, IProxyInputB b) { }

        public void MethodWithNoOverload(IProxyInputA a) { }

        public string MethodWithProxiedOverload(IProxyInputA value) => value.hi;
        public string MethodWithProxiedOverload(IProxyInputB value) => value.bye;
    }
}
