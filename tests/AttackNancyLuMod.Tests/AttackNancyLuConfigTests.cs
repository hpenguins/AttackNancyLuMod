using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AttackNancyLuMod.Tests
{
    public sealed class AttackNancyLuConfigTests
    {
        [Fact]
        public void ManifestJson_IsValid()
        {
            string manifestPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "AttackNancyLuMod.json"));

            Assert.True(File.Exists(manifestPath), "Manifest file should exist at: " + manifestPath);
            string json = File.ReadAllText(manifestPath);
            var obj = JObject.Parse(json);

            Assert.Equal("AttackNancyLuMod", (string)obj["id"]);
            Assert.Equal("集火南希露", (string)obj["name"]);
            Assert.Equal("hpenguins", (string)obj["author"]);
            Assert.False(string.IsNullOrWhiteSpace((string)obj["version"]));
        }

        [Fact]
        public void ConfigJson_ContainsAllOptions_AndFullI18nComments()
        {
            string configPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "config.json"));

            Assert.True(File.Exists(configPath), "config.json should exist at: " + configPath);
            string json = File.ReadAllText(configPath);
            var obj = JObject.Parse(json);

            Assert.True(obj.ContainsKey("enabled"), "config.json must have enabled");
            Assert.True(obj.ContainsKey("focusKey"), "config.json must have focusKey");
            Assert.Equal("X", (string)obj["focusKey"]);
            Assert.True(obj.ContainsKey("warnKey"), "config.json must have warnKey");
            Assert.True(obj.ContainsKey("tackleKey"), "config.json must have tackleKey");
            Assert.True(obj.ContainsKey("screenRadius"), "config.json must have screenRadius");
            Assert.True(obj.ContainsKey("unlockPvpMark"), "config.json must have unlockPvpMark");
            Assert.True(obj.ContainsKey("allowSelfFocus"), "config.json must have allowSelfFocus");

            var comments = obj["_comments"] as JObject;
            Assert.NotNull(comments);

            string[] keys = { "enabled", "focusKey", "warnKey", "tackleKey", "screenRadius", "unlockPvpMark", "allowSelfFocus" };
            string[] langs = { "zh-Hans", "zh-Hant", "en", "ja" };

            foreach (var key in keys)
            {
                Assert.True(comments.ContainsKey(key), $"_comments must contain '{key}'");
                foreach (var lang in langs)
                {
                    var text = comments[key]?[lang]?.ToString();
                    Assert.False(string.IsNullOrWhiteSpace(text), $"_comments.{key} must have non-empty {lang}");
                }
            }
        }

        [Fact]
        public void Settings_DefaultValues_AreCorrect()
        {
            var settings = new AttackNancyLuSettings();
            Assert.True(settings.Enabled);
            Assert.Equal("X", settings.FocusKey);
            Assert.Equal("", settings.WarnKey);
            Assert.Equal("", settings.TackleKey);
            Assert.Equal(100.0f, settings.ScreenRadius);
            Assert.True(settings.UnlockPvpMark);
            Assert.True(settings.AllowSelfFocus);
        }
    }
}
