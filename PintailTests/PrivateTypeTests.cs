using System;
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

        private ProxyManager<Nothing> CreateProxyManager(AccessLevelChecking accessLevelChecking)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Nanoray.Pintail.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies");
            var manager = new ProxyManager<Nothing>(moduleBuilder, new(accessLevelChecking: accessLevelChecking));
            return manager;
        }

        [Test]
        public void TestEnabledAccessLevelCheckingPrivateProviderAndPrivateClient()
        {
            var manager = this.CreateProxyManager(AccessLevelChecking.Enabled);
            object providerApi = ProviderWrapper.GetPrivateProvider();

            Assert.Throws<ArgumentException>(() =>
            {
                var consumerApi = manager.ObtainProxy<IPrivateClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestEnabledAccessLevelCheckingPublicProviderAndPrivateClient()
        {
            var manager = this.CreateProxyManager(AccessLevelChecking.Enabled);
            object providerApi = ProviderWrapper.GetPublicProvider();

            Assert.Throws<ArgumentException>(() =>
            {
                var consumerApi = manager.ObtainProxy<IPrivateClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestEnabledAccessLevelCheckingPrivateProviderAndPublicClient()
        {
            var manager = this.CreateProxyManager(AccessLevelChecking.Enabled);
            object providerApi = ProviderWrapper.GetPrivateProvider();

            Assert.Throws<MethodAccessException>(() =>
            {
                var consumerApi = manager.ObtainProxy<IPublicClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestEnabledAccessLevelCheckingPublicProviderAndPublicClient()
        {
            var manager = this.CreateProxyManager(AccessLevelChecking.Enabled);
            object providerApi = ProviderWrapper.GetPublicProvider();

            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IPublicClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestDisabledAccessLevelCheckingPrivateProviderAndPrivateClient()
        {
            var manager = this.CreateProxyManager(AccessLevelChecking.DisabledButOnlyAllowPublicMembers);
            object providerApi = ProviderWrapper.GetPrivateProvider();

            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IPrivateClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestDisabledAccessLevelCheckingPublicProviderAndPrivateClient()
        {
            var manager = this.CreateProxyManager(AccessLevelChecking.DisabledButOnlyAllowPublicMembers);
            object providerApi = ProviderWrapper.GetPublicProvider();

            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IPrivateClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestDisabledAccessLevelCheckingPrivateProviderAndPublicClient()
        {
            var manager = this.CreateProxyManager(AccessLevelChecking.DisabledButOnlyAllowPublicMembers);
            object providerApi = ProviderWrapper.GetPrivateProvider();

            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IPublicClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }

        [Test]
        public void TestDisabledAccessLevelCheckingPublicProviderAndPublicClient()
        {
            var manager = this.CreateProxyManager(AccessLevelChecking.DisabledButOnlyAllowPublicMembers);
            object providerApi = ProviderWrapper.GetPublicProvider();

            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IPublicClient>(providerApi)!;
                Assert.AreEqual("Lorem ipsum", consumerApi.Test);
            });
        }
    }
}
