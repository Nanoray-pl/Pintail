using System;

namespace Nanoray.Pintail.Tests.Provider
{
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

    public class ProviderApi
    {
        public void VoidMethod() { }

        public int IntMethod(int num)
            => num;

        public R MapperMethod<T, R>(T t, Func<T, R> mapper)
            => mapper(t);

        public IApiResult GetSameResult(IApiResult result)
            => result;

        public IApiResult GetModifiedResult(IApiResult result)
            => new ApiResult(result.Text);
    }
}
