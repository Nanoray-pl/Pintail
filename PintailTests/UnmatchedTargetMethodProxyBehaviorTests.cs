using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class UnmatchedTargetMethodProxyBehaviorTests
    {
        private sealed class Provider
        {
            public int TestMatched() => 123;
            public int TestUnmatched() => 456;
        }

        public interface IConsumer
        {
            int TestMatched();
        }

        private ProxyManager<Nothing> CreateProxyManager(ProxyManagerConfiguration<Nothing>? configuration = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Nanoray.Pintail.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies");
            var manager = new ProxyManager<Nothing>(moduleBuilder, configuration);
            return manager;
        }

        [Test]
        public void TestIgnoreUnmatchedTargetMethodProxyBehavior()
        {
            var manager = this.CreateProxyManager(new() { AccessLevelChecking = AccessLevelChecking.DisabledButOnlyAllowPublicMembers, UnmatchedTargetMethodProxyBehavior = UnmatchedTargetMethodProxyBehavior.Ignore });
            var providerApi = new Provider();

            var consumerApi = manager.ObtainProxy<IConsumer>(providerApi)!;
            Assert.AreEqual(123, consumerApi.TestMatched());
            Assert.IsNull(consumerApi.GetType().GetMethod("TestUnmatched", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void TestProxyUnmatchedTargetMethodProxyBehavior()
        {
            var manager = this.CreateProxyManager(new() { AccessLevelChecking = AccessLevelChecking.DisabledButOnlyAllowPublicMembers, UnmatchedTargetMethodProxyBehavior = UnmatchedTargetMethodProxyBehavior.Proxy });
            var providerApi = new Provider();

            var consumerApi = manager.ObtainProxy<IConsumer>(providerApi)!;
            Assert.AreEqual(123, consumerApi.TestMatched());
            Assert.IsNotNull(consumerApi.GetType().GetMethod("TestUnmatched", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.AreEqual(456, consumerApi.GetType().GetMethod("TestUnmatched", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(consumerApi, null));
        }
    }
}
