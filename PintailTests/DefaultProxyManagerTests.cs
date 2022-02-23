using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Nanoray.Pintail.Tests.Consumer;
using Nanoray.Pintail.Tests.Provider;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class DefaultProxyManagerTests
	{
        private static int nextModuleIndex = 0;

        private ProxyManager<Nothing> CreateProxyManager(ProxyManagerConfiguration<Nothing>? configuration = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Proxies_{nextModuleIndex++}, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies_{nextModuleIndex++}");
            var manager = new ProxyManager<Nothing>(moduleBuilder, configuration);
            return manager;
        }

        [Test]
        public void TestSuccessfulBasicApi()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            consumerApi.VoidMethod();
            Assert.AreEqual(123, consumerApi.IntMethod(123));
            Assert.AreEqual(144, consumerApi.DefaultMethod(12));
            Assert.AreEqual(42, consumerApi.IntProperty);
            Assert.AreEqual("asdf", consumerApi["asdf"]);
            Assert.AreEqual(5, consumerApi.MapperMethod("word.", (t) => t.Length));
        }

        [Test]
        public void TestIsAssignable()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            object? obj = null;
            Assert.DoesNotThrow(() => obj = consumerApi.IsAssignableTest("testing"));
            Assert.AreEqual("testing", obj);
        }

        [Test]
        public void TestEnum()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            Assert.AreEqual(Consumer.StateEnum.State1, consumerApi.GetStateEnum());
        }

        [Test]
        public void TestOutEnum()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            consumerApi.GetOutStateEnum(out Consumer.StateEnum state);
            Assert.AreEqual(Consumer.StateEnum.State1, state);
        }

        [Test]
        public void TestReturnSameEnum()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var state = consumerApi.GetSameEnumState(Consumer.StateEnum.State2);
            Assert.AreEqual(Consumer.StateEnum.State2, state);
        }

        [Test]
        public void TestReturnArray()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var result = consumerApi.GetArray();
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("0", result[0].Text);
        }

        [Test]
        public void TestReturnJaggedArray()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var result = consumerApi.GetJaggedArray();
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1, result[0].Length);
            Assert.AreEqual("0", result[0][0].Text);
        }

        [Test]
        public void TestReturn2DArray()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var result = consumerApi.Get2DArray();
            Assert.AreEqual(1, result.GetLength(0));
            Assert.AreEqual(2, result.GetLength(1));
            Assert.AreEqual("0, 0", result[0, 0].Text);
            Assert.AreEqual("0, 1", result[0, 1].Text);
        }

        [Test]
        public void TestMatchingArrayParameter()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var input = new Consumer.IApiResult[] { new Consumer.ApiResult("0"), new Consumer.ApiResult("1") };
            consumerApi.ArrayMethod(input);
            Assert.AreEqual(2, input.Length);
            Assert.AreEqual("modified", input[0].Text);
            Assert.AreEqual("1", input[1].Text);
        }

        [Test]
        public void TestMismatchedArrayParameterAndThrow()
        {
            var manager = this.CreateProxyManager(new(
                mismatchedArrayMappingBehavior: ProxyManagerMismatchedArrayMappingBehavior.Throw
            ));
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var input = new Consumer.ApiResult[] { new Consumer.ApiResult("0"), new Consumer.ApiResult("1") };
            Assert.Throws<ArgumentException>(() => consumerApi.ArrayMethod(input));
        }

        [Test]
        public void TestMismatchedArrayParameterAndAllowAndDontMapBack()
        {
            var manager = this.CreateProxyManager(new(
                mismatchedArrayMappingBehavior: ProxyManagerMismatchedArrayMappingBehavior.AllowAndDontMapBack
            ));
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var input = new Consumer.ApiResult[] { new Consumer.ApiResult("0"), new Consumer.ApiResult("1") };
            consumerApi.ArrayMethod(input);
            Assert.AreEqual(2, input.Length);
            Assert.AreEqual("0", input[0].Text);
            Assert.AreEqual("1", input[1].Text);
        }

        [Test]
        public void TestReturnList()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var result = consumerApi.GetList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("0", result[0].Text);
        }

        [Test]
        public void TestOutParameters()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            consumerApi.GetOutResult("testing", out Consumer.IApiResult result);
            Assert.AreEqual("testing", result.Text);
        }

        [Test]
        public void TestInputOutputApi()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;

            {
                Consumer.IApiResult input = new Consumer.ApiResult("input");
                Consumer.IApiResult output = consumerApi.GetSameResult(input);
                Assert.AreEqual(input.Text, output.Text);
                Assert.IsTrue(ReferenceEquals(input, output));
            }

            {
                Consumer.IApiResult input = new Consumer.ApiResult("input");
                Consumer.IApiResult output = consumerApi.GetModifiedResult(input);
                Assert.AreEqual(input.Text, output.Text);
                Assert.IsFalse(ReferenceEquals(input, output));
            }
        }

        [Test]
        public void TestComplexType()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var result = consumerApi.GetComplexType();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(Consumer.StateEnum.State0, result.Keys.ToList()[0]);
            Assert.AreEqual(1, result[Consumer.StateEnum.State0].Count);
            Assert.AreEqual("0", result[Consumer.StateEnum.State0].ToList()[0].Text);
        }

        [Test]
        public void TestSystemDelegates()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;

            var result1 = new Consumer.ApiResult("asdf");
            var result2 = consumerApi.GetMapper()(result1);
            Assert.AreEqual(result1.Text, result2.Text);
            Assert.AreEqual(result1, result2);

            consumerApi.SetMapper((r) => new Consumer.ApiResult($"{r.Text}{r.Text}"));
            var result3 = consumerApi.GetMapper()(result2);
            Assert.AreEqual($"{result2.Text}{result2.Text}", result3.Text);
            Assert.AreNotEqual(result2, result3);
        }

        [Test]
        public void TestCustomGenericOutDelegate()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;

            consumerApi.GetCustomOutDelegate()(out Consumer.StateEnum result1);
            Assert.AreEqual(Consumer.StateEnum.State0, result1);
            consumerApi.SetCustomOutDelegate((out Consumer.StateEnum p) => p = Consumer.StateEnum.State2);
            consumerApi.GetCustomOutDelegate()(out Consumer.StateEnum result2);
            Assert.AreEqual(Consumer.StateEnum.State2, result2);
        }

        [Test]
        public void TestTryProxy()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();
            Assert.IsTrue(manager.TryProxy(providerApi, out IConsumerApi? consumerApi));
            Assert.NotNull(consumerApi);
        }

        [Test]
        public void TestNonExistentApiMethodByThrowingOnPrepare()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowExceptionNoMatchingMethodHandler
            ));
            var providerApi = new ProviderApi();
            Assert.Throws<ArgumentException>(() => manager.ObtainProxy<IInvalidConsumerApi>(providerApi));
        }

        [Test]
        public void TestNonExistentApiMethodByThrowingImplementation()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowingImplementationNoMatchingMethodHandler
            ));
            var providerApi = new ProviderApi();
            IInvalidConsumerApi consumerApi = null!;
            Assert.DoesNotThrow(() => consumerApi = manager.ObtainProxy<IInvalidConsumerApi>(providerApi)!);
            Assert.Throws<NotImplementedException>(() => consumerApi.NonExistentApiMethod());
        }

        [Test]
        public void TestMarkerInterfaceWithoutProperty()
        {
            var manager = this.CreateProxyManager(new(
                proxyObjectInterfaceMarking: ProxyObjectInterfaceMarking.Marker
            ));
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            Assert.IsTrue(consumerApi is IProxyObject);
            Assert.IsFalse(consumerApi is IProxyObject.IWithProxyTargetInstanceProperty);
        }

        [Test]
        public void TestMarkerInterfaceWithProperty()
        {
            var manager = this.CreateProxyManager(new(
                proxyObjectInterfaceMarking: ProxyObjectInterfaceMarking.MarkerWithProperty
            ));
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            Assert.IsTrue(consumerApi is IProxyObject);
            Assert.IsTrue(consumerApi is IProxyObject.IWithProxyTargetInstanceProperty);
            Assert.IsTrue(ReferenceEquals(providerApi, ((IProxyObject.IWithProxyTargetInstanceProperty)consumerApi).ProxyTargetInstance));
        }

        [Test]
        public void TestGMCM()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;

            consumerApi.RegisterSimpleOption(new Consumer.ApiResult(""), "optionName", "optionDesc", () => true, (b) => { });
            consumerApi.RegisterSimpleOption(new Consumer.ApiResult(""), "optionName", "optionDesc", () => "value", (s) => { });
        }

        [Test]
        public void TestComplexGenericMethod()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;

            var result = consumerApi.ComplexGenericMethod<string>("");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestEnumConstrainedGenericMethod()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;

            var result = consumerApi.EnumConstrainedGenericMethod<ProxyManagerEnumMappingBehavior>("Strict");
            Assert.AreEqual(ProxyManagerEnumMappingBehavior.Strict, result);
        }

        [Test]
        public void TestConstructorConstrainedGenericMethod()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;

            var result = consumerApi.ConstructorConstrainedGenericMethod<System.Collections.Generic.List<string>>();
            Assert.AreEqual(0, result.Count);
        }
    }
}
