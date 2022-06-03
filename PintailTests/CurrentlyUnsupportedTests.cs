using System;
using System.Reflection;
using System.Reflection.Emit;
using Nanoray.Pintail.Tests.Consumer;
using Nanoray.Pintail.Tests.Provider;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    // All of those tests should currently throw, as the features they test are not yet supported.
    [TestFixture]
    internal class CurrentlyUnsupportedTests
    {
        private ProxyManager<Nothing> CreateProxyManager(ProxyManagerConfiguration<Nothing>? configuration = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Nanoray.Pintail.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("Proxies");
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

        [Test]
        public void TestBoxing()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowExceptionNoMatchingMethodHandler
            ));
            var invalidProviderApi = new RequiresBoxing();
            Assert.Throws<ArgumentException>(() => manager.ObtainProxy<IRequiresBoxing>(invalidProviderApi));
        }

        [Test]
        public void TestUnboxing()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowExceptionNoMatchingMethodHandler
            ));
            var invalidProviderApi = new RequiresUnboxing();
            Assert.Throws<ArgumentException>(() => manager.ObtainProxy<IRequiresUnboxing>(invalidProviderApi));
        }

        [Test]
        public void TestIncorrectlySizedEnum()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowExceptionNoMatchingMethodHandler,
                enumMappingBehavior: ProxyManagerEnumMappingBehavior.AllowAdditive
            ));
            var invalidProviderApi = new EnumInsufficientlyBig();
            Assert.Throws<ArgumentException>(() => manager.ObtainProxy<IInsufficientEnumValues>(invalidProviderApi));
        }
    }
}
