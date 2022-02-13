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

        private DefaultProxyManager<int> CreateModuleBuilder()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Proxies_{nextModuleIndex++}, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies_{nextModuleIndex++}");
            var manager = new DefaultProxyManager<int>(moduleBuilder);
            return manager;
        }

        [Test]
        public void TestSuccessfulBasicApi()
        {
            var manager = this.CreateModuleBuilder();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<int, IConsumerApi>(providerApi, 0, 0)!;
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
            var manager = this.CreateModuleBuilder();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<int, IConsumerApi>(providerApi, 0, 0)!;
            object? obj = null;
            Assert.DoesNotThrow(() => obj = consumerApi.IsAssignableTest("testing"));
            Assert.AreEqual("testing", obj);
        }

        [Test]
        public void TestOutParameters()
        {
            var manager = this.CreateModuleBuilder();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<int, IConsumerApi>(providerApi, 0, 0)!;
            consumerApi.GetOutResult("testing", out Consumer.IApiResult result);
            Assert.AreEqual("testing", result.Text);
        }

        [Test]
        public void TestInputOutputApi()
        {
            var manager = this.CreateModuleBuilder();
            var providerApi = new ProviderApi();
            var consumerApi = manager.ObtainProxy<int, IConsumerApi>(providerApi, 0, 0)!;

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
        public void TestTryProxy()
        {
            var manager = this.CreateModuleBuilder();
            var providerApi = new ProviderApi();
            Assert.IsTrue(manager.TryProxy(providerApi, 0, 0, out IConsumerApi? consumerApi));
            Assert.NotNull(consumerApi);
        }
	}
}
