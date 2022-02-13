using System;

namespace Nanoray.Pintail.Tests.Consumer
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

    public interface IConsumerApi
    {
        void VoidMethod();
        int IntMethod(int num);
        R MapperMethod<T, R>(T t, Func<T, R> mapper);
        void GetOutResult(string text, out IApiResult result);
        IApiResult GetSameResult(IApiResult result);
        IApiResult GetModifiedResult(IApiResult result);
    }
}
