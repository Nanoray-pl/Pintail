using System;
using System.Collections.Generic;

namespace Nanoray.Pintail.Tests.Provider
{
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

        public void ArrayMethod(IApiResult[] array)
        { }

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

        //public IDictionary<ISet<IApiResult[]>, StateEnum> GetComplexType()
        //    => new Dictionary<ISet<IApiResult[]>, StateEnum>();
    }
}
