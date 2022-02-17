using System;
using System.Collections.Generic;

namespace Nanoray.Pintail.Tests.Consumer
{
    public delegate void CustomGenericOutDelegate<T>(out T param);

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

    public interface IConsumerApi
    {
        void VoidMethod();
        int IntMethod(int num);
        int DefaultMethod(int num);
        int IntProperty { get; }
        string this[string key] { get; }
        R MapperMethod<T, R>(T t, Func<T, R> mapper);
        object? IsAssignableTest(string? anyObj);

        StateEnum GetStateEnum();
        void GetOutStateEnum(out StateEnum state);
        StateEnum GetSameEnumState(StateEnum state);

        IApiResult[] GetArray();
        IApiResult[][] GetJaggedArray();
        void ArrayMethod(IApiResult[] array);
        IList<IApiResult> GetList();

        void GetOutResult(string text, out IApiResult result);
        IApiResult GetSameResult(IApiResult result);
        IApiResult GetModifiedResult(IApiResult result);

        IDictionary<StateEnum, ISet<IApiResult>> GetComplexType();

        Func<IApiResult, IApiResult> GetMapper();
        void SetMapper(Func<IApiResult, IApiResult> mapper);

        CustomGenericOutDelegate<StateEnum> GetCustomOutDelegate();
        void SetCustomOutDelegate(CustomGenericOutDelegate<StateEnum> @delegate);
    }

    public interface IInvalidConsumerApi
    {
        void NonExistentApiMethod();
    }
}
