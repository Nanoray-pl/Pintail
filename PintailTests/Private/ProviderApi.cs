using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Nanoray.Pintail.Tests.Provider
{
    public delegate void CustomGenericOutDelegate<T>(out T param);

    public enum StateEnum
    {
        State0, State1, State2
    }

    public enum UIntEnum: uint
    {
        State0 = 0,
        State1 = 1,
        State2 = 2,
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

    public interface IProviderApiDefaultMethods
    {
        int IntMethod(int num);

        int DefaultMethod(int num)
            => this.IntMethod(num * num);
    }

    public class ProviderApi<T>
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
    }

    public class ProviderApi: IProviderApiDefaultMethods
    {
        private Func<IApiResult, IApiResult> Mapper = (r) => r;
        private CustomGenericOutDelegate<StateEnum> CustomOutDelegate = (out StateEnum p) => p = StateEnum.State0;

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

        public string? IsAssignableTest(object? anyObj)
            => anyObj?.ToString();

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

 #region OVERLOADS
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
#endregion
    }

    public class InvalidNotMatchingEnumBackingField
    {
        public void NotMatchingEnumBackingType(UIntEnum @enum) { }
    }

    public class InvalidNotMatchingArrayInput
    {
        public void NotMatchingArrayInput(LocalBuilder[] input) { }
    }

    public interface IProxiedInput
    {
        public string teststring { get; set;}
    }

    public interface IProxiedInput2
    {
        public string otherteststring { get; set;}
    }
}
