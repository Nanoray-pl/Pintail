using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Nanoray.Pintail.Tests.Consumer;
using Nanoray.Pintail.Tests.Provider;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    internal class CurrentlyUnsupportedTests
    {
        private ProxyManager<Nothing> CreateProxyManager(ProxyManagerConfiguration<Nothing>? configuration = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Nanoray.Pintail.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies");
            var manager = new ProxyManager<Nothing>(moduleBuilder, configuration);
            return manager;
        }

        [Test]
        public void TestNotMatchingEnumShouldThrow()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowExceptionNoMatchingMethodHandler
                ));
            var invalidProviderApi = new InvalidNotMatchingEnumBackingField();
            Assert.Throws<ArgumentException>(() => manager.ObtainProxy<IInvalidNotMatchingEnumBackingField>(invalidProviderApi));
        }

        [Test]
        public void TestNotMatchingArrayShouldThrow()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowExceptionNoMatchingMethodHandler
                ));
            var invalidProviderApi = new InvalidNotMatchingArrayInput();
            Assert.Throws<ArgumentException>(() => manager.ObtainProxy<IInvalidNotMatchingArrayInput>(invalidProviderApi));
        }

        [Test]
        public void TestByRefNotMatchingShouldThrow()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowExceptionNoMatchingMethodHandler
                ));
            var invalidProviderApi = new InvalidIncorrectByRef();
            Assert.Throws<ArgumentException>(() => manager.ObtainProxy<IInvalidIncorrectByRef>(invalidProviderApi));
        }
    }
}
