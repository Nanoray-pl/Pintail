using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class DefaultProxyManagerTests
	{
        [Test]
        public void TestSuccessfulApi()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("Proxies");
            var manager = new DefaultProxyManager<int>(moduleBuilder);

            var providerApi = new ProviderApi();
            IConsumerApi consumerApi = null!;
            Assert.DoesNotThrow(() => consumerApi = manager.ObtainProxy<int, IConsumerApi>(providerApi, 0, 0)!);

            Assert.DoesNotThrow(() => consumerApi.VoidMethod());
            Assert.DoesNotThrow(() =>
            {
                Assert.AreEqual(123, consumerApi.IntMethod(123));
            });
        }
	}
}
