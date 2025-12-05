using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class ThreeApiTests
    {
        public static class ModSettings
        {
            public interface IModSettingsApi
            {
                IModSetting MakeSetting(string value);

                interface IModSetting
                {
                    string Value { get; set; }

                    IModSetting SetValue(string value);
                }
            }

            public class ModSettingsApiImpl : IModSettingsApi
            {
                private class ModSetting(string value) : IModSettingsApi.IModSetting
                {
                    public string Value { get; set; } = value;

                    public IModSettingsApi.IModSetting SetValue(string value)
                    {
                        this.Value = value;
                        return this;
                    }
                }

                public IModSettingsApi.IModSetting MakeSetting(string value)
                    => new ModSetting(value);
            }
        }

        public static class CustomRunOptions
        {
            public interface IModSettingsApi
            {
                IModSetting MakeSetting(string value);

                interface IModSetting
                {
                    string Value { get; set; }

                    IModSetting SetValue(string value);
                }
            }

            public interface ICustomRunOptionsApi
            {
                void Register(ICustomRunOption option);

                interface ICustomRunOption
                {
                    IModSettingsApi.IModSetting MakeSetting();
                }
            }

            public class CustomRunOptionsApiImpl : ICustomRunOptionsApi
            {
                public readonly List<ICustomRunOptionsApi.ICustomRunOption> Options = new();

                public void Register(ICustomRunOptionsApi.ICustomRunOption option)
                {
                }
            }
        }

        public static class MoreDifficultyOptions
        {
            public interface IModSettingsApi
            {
                IModSetting MakeSetting(string value);

                interface IModSetting
                {
                    string Value { get; set; }

                    IModSetting SetValue(string value);
                }
            }

            public interface ICustomRunOptionsApi
            {
                void Register(ICustomRunOption option);

                interface ICustomRunOption
                {
                    IModSettingsApi.IModSetting MakeSetting();
                }
            }

            public class CustomRunOption(IModSettingsApi.IModSetting setting) : ICustomRunOptionsApi.ICustomRunOption
            {
                public IModSettingsApi.IModSetting MakeSetting()
                    => setting;
            }
        }

        private ProxyManager<Nothing> CreateProxyManager(ProxyManagerConfiguration<Nothing>? configuration = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Nanoray.Pintail.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies");
            var manager = new ProxyManager<Nothing>(moduleBuilder, configuration);
            return manager;
        }

        [Test]
        public void TestThreeApis()
        {
            var manager = this.CreateProxyManager();
            var ms_api = new ModSettings.ModSettingsApiImpl();
            var cro_api = new CustomRunOptions.CustomRunOptionsApiImpl();

            Assert.DoesNotThrow(() =>
            {
                var cro_ms_api = manager.ObtainProxy<CustomRunOptions.IModSettingsApi>(ms_api);

                var mdo_ms_api = manager.ObtainProxy<MoreDifficultyOptions.IModSettingsApi>(ms_api);
                var mdo_cro_api = manager.ObtainProxy<MoreDifficultyOptions.ICustomRunOptionsApi>(cro_api);

                var setting = mdo_ms_api.MakeSetting("test");
                var newOption = new MoreDifficultyOptions.CustomRunOption(setting);
                mdo_cro_api.Register(newOption);

                var allSettings = new List<CustomRunOptions.IModSettingsApi.IModSetting>(2);
                allSettings.Add(cro_ms_api.MakeSetting("test"));
                allSettings.AddRange(cro_api.Options.Select(o => o.MakeSetting()));
                allSettings.Add(cro_ms_api.MakeSetting("test"));
            });
        }
    }
}
