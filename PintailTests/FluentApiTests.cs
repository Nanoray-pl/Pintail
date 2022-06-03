using System.Reflection;
using System.Reflection.Emit;
using Nanoray.Pintail.Tests.Consumer;
using Nanoray.Pintail.Tests.Provider;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    internal class FluentApiTests
    {
        private ProxyManager<Nothing> CreateProxyManager(ProxyManagerConfiguration<Nothing>? configuration = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Nanoray.Pintail.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies");
            var manager = new ProxyManager<Nothing>(moduleBuilder, configuration);
            return manager;
        }

        [Test]
        public void BasicFluentTest()
        {
            var manager = this.CreateProxyManager();
            var providerApiManager = new FluentProviderApiManager();
            var consumerApiManager = manager.ObtainProxy<IFluentConsumerApiManager>(providerApiManager)!;

            var consumerApi = consumerApiManager.GetOne();

            var otherConsumerApiManager = manager.ObtainProxy<IFluentConsumerApiManager>(providerApiManager)!;

            var otherConsumerApi = otherConsumerApiManager.GetOne();

            consumerApi.valueState = 5;
            Assert.AreEqual(5, consumerApi.valueState);

            otherConsumerApi.valueState = 7;
            Assert.AreEqual(7, otherConsumerApi.valueState);
            Assert.AreEqual(5, consumerApi.valueState);

            consumerApi.Prop = 1337;
            otherConsumerApi.Prop = 2500;

            Assert.AreEqual(1337, consumerApi.Prop);
            Assert.AreEqual(2500, otherConsumerApi.Prop);
        }
    }
}
