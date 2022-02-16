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

        private DefaultProxyManager<Nothing> CreateProxyManager(DefaultProxyManagerConfiguration<Nothing>? configuration = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Proxies_{nextModuleIndex++}, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies_{nextModuleIndex++}");
            var manager = new DefaultProxyManager<Nothing>(moduleBuilder, configuration);
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
        public void TestArrayParameter()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            var input = new Consumer.ApiResult[] { new Consumer.ApiResult("1") };
            consumerApi.ArrayMethod(input);
            Assert.AreEqual(1, input.Length);
            Assert.AreEqual("1", input[0].Text);
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
                noMatchingMethodHandler: DefaultProxyManagerConfiguration<Nothing>.ThrowExceptionNoMatchingMethodHandler
            ));
            var providerApi = new ProviderApi();
            Assert.Throws<ArgumentException>(() => manager.ObtainProxy<IInvalidConsumerApi>(providerApi));
        }

        [Test]
        public void TestNonExistentApiMethodByThrowingImplementation()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: DefaultProxyManagerConfiguration<Nothing>.ThrowingImplementationNoMatchingMethodHandler
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
    }
}
