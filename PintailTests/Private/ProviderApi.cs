using System;
using System.Collections.Generic;

namespace Nanoray.Pintail.Tests.Provider
{
    public delegate void CustomGenericOutDelegate<T>(out T param);

    public enum StateEnum
    {
        State0, State1, State2
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

    public class ProviderApi: IProviderApiDefaultMethods
    {
        private Func<IApiResult, IApiResult> Mapper = (r) => r;
        private CustomGenericOutDelegate<StateEnum> CustomOutDelegate = (out StateEnum p) => p = StateEnum.State0;

        public void VoidMethod() { }

        public int IntMethod(int num)
            => num;

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
    }
}
