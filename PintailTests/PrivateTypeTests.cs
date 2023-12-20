using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class PrivateTypeTests
    {
        public static class ProviderWrapper
        {
            public static object GetPrivateProvider()
                => new PrivateProvider();

            public static object GetPublicProvider()
                => new PublicProvider();

            private sealed class PrivateProvider
            {
                public string Test => "Lorem ipsum";
            }

            public sealed class PublicProvider
            {
                public string Test => "Lorem ipsum";
            }
        }

        public interface IPublicClient
        {
            string Test { get; }
        }

        private interface IPrivateClient
        {
            string Test { get; }
        }

        private ProxyManager<Nothing> CreateProxyManager(ProxyManagerConfiguration<Nothing>? configuration = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Nanoray.Pintail.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies");
            var manager = new ProxyManager<Nothing>(moduleBuilder, configuration);
            return manager;
        }

        [Test]
        public void TestPrivateProviderAndPrivateClient()
        {
            var manager = this.CreateProxyManager();
            object providerApi = ProviderWrapper.GetPrivateProvider();

            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IPrivateClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestPublicProviderAndPrivateClient()
        {
            var manager = this.CreateProxyManager();
            object providerApi = ProviderWrapper.GetPublicProvider();

            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IPrivateClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestPrivateProviderAndPublicClient()
        {
            var manager = this.CreateProxyManager();
            object providerApi = ProviderWrapper.GetPrivateProvider();

            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IPublicClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestPublicProviderAndPublicClient()
        {
            var manager = this.CreateProxyManager();
            object providerApi = ProviderWrapper.GetPublicProvider();

            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IPublicClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }
    }
}
