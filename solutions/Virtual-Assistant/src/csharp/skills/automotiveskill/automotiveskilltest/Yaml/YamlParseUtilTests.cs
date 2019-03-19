using AutomotiveSkill.Models;
using AutomotiveSkill.Yaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutomotiveSkillTest.Yaml
{
    [TestClass]
    public class YamlParseUtilTests
    {
        [TestMethod]
        public void Test_ParseDocument()
        {
            AvailableSetting foo = new AvailableSetting
            {
                CanonicalName = "Foo",
                Values = new List<AvailableSettingValue>
                {
                    new AvailableSettingValue
                    {
                        CanonicalName = "Set",
                        RequiresAmount = true,
                    },
                    new AvailableSettingValue
                    {
                        CanonicalName = "Decrease",
                        ChangesSignOfAmount = true,
                    },
                    new AvailableSettingValue
                    {
                        CanonicalName = "Increase",
                        Antonym = "Decrease",
                    },
                },
                AllowsAmount = true,
                Amounts = new List<AvailableSettingAmount>
                {
                    new AvailableSettingAmount
                    {
                        Unit = "bar",
                        Min = 14,
                        Max = 32,
                    },
                    new AvailableSettingAmount
                    {
                        Unit = "",
                        Min = -5,
                    },
                },
                IncludedSettings = new List<string>
                {
                    "Front Foo",
                    "Rear Foo",
                },
            };

            AvailableSetting qux = new AvailableSetting
            {
                CanonicalName = "Qux",
                Values = new List<AvailableSettingValue>
                {
                    new AvailableSettingValue
                    {
                        CanonicalName = "Off",
                        RequiresConfirmation = true,
                    },
                    new AvailableSettingValue
                    {
                        CanonicalName = "On",
                    },
                },
            };

            List<AvailableSetting> expectedAvailableSettings = new List<AvailableSetting>
            {
                foo,
                qux,
            };

            Assembly resourceAssembly = typeof(YamlParseUtilTests).Assembly;
            var settingFile = resourceAssembly
                .GetManifestResourceNames()
                .Where(x => x.Contains("test_available_settings.yaml"))
                .First();

            using (TextReader reader = new StreamReader(resourceAssembly.GetManifestResourceStream(settingFile)))
            {
                var availableSettings = YamlParseUtil.ParseDocument<List<AvailableSetting>>(reader);
                CollectionAssert.AreEqual(expectedAvailableSettings, availableSettings);
            }
        }
    }
}
