using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    // All of those tests should currently throw, as the features they test are not yet supported.
    [TestFixture]
    public sealed class CurrentlyUnsupportedRealWorldStatusRenderHookTests
    {
        public static class Provider
        {
            public sealed class Manager
            {
                public void RegisterStatusRenderHook(IStatusRenderHook hook, double priority) { }
            }

            public interface IStatusRenderHook
            {
                BarConfig? OverrideStatusRendering(string status, int amount) => new();
            }

            public record struct BarConfig(
                IReadOnlyList<Color> Colors,
                int? BarTickWidth = null
            );
        }

        public static class Client
        {
            public interface IApi
            {
                void RegisterStatusRenderHook(IStatusRenderHook hook, double priority);
            }

            public interface IStatusRenderHook
            {
                BarConfig? OverrideStatusRendering(string status, int amount) => new();
            }

            public record struct BarConfig(
                IReadOnlyList<Color> Colors,
                int? BarTickWidth = null
            );
        }

        [Test]
        public void TestRealWorldScenario()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Nanoray.Pintail.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies");
            var manager = new ProxyManager<Nothing>(moduleBuilder);
            object providerApi = new Provider.Manager();

            Assert.Throws<ArgumentException>(() =>
            {
                _ = manager.ObtainProxy<Client.IApi>(providerApi)!;
            });
        }
    }
}
