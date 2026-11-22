using System;
using CesiumLoader.SDK.Configuration;
using UnityEngine;

namespace AttackNancyLuMod
{
    internal sealed class AttackNancyLuSettings
    {
        public bool Enabled = true;
        public string FocusKey = "X";
        public string WarnKey = "";
        public string TackleKey = "";
        public float ScreenRadius = 100.0f;
        public bool UnlockPvpMark = true;
        public bool AllowSelfFocus = true;
        public bool OverrideWheel = false;

        public KeyCode ParsedFocusKey { get; private set; } = KeyCode.X;
        public KeyCode ParsedWarnKey { get; private set; } = KeyCode.None;
        public KeyCode ParsedTackleKey { get; private set; } = KeyCode.None;

        public static AttackNancyLuSettings Read(ModConfig config, Action<string> warn)
        {
            var value = new AttackNancyLuSettings();
            if (config == null) return value;

            value.Enabled = config.GetBool("enabled", true);
            value.FocusKey = config.GetString("focusKey", "X") ?? "X";
            value.WarnKey = config.GetString("warnKey", "") ?? "";
            value.TackleKey = config.GetString("tackleKey", "") ?? "";
            value.ScreenRadius = (float)config.GetDouble("screenRadius", 100.0);
            value.UnlockPvpMark = config.GetBool("unlockPvpMark", true);
            value.AllowSelfFocus = config.GetBool("allowSelfFocus", true);
            value.OverrideWheel = config.GetBool("overrideWheel", false);

            value.ParsedFocusKey = ParseKey(value.FocusKey, KeyCode.X);
            value.ParsedWarnKey = ParseKey(value.WarnKey, KeyCode.None);
            value.ParsedTackleKey = ParseKey(value.TackleKey, KeyCode.None);

            return value;
        }

        public static void EnsureDefaults(ModConfig config)
        {
            if (config == null) return;

            bool added = AttackNancyLuConfigDocs.Apply(config);
            Add(config, "enabled", true, ref added);
            Add(config, "focusKey", "X", ref added);
            Add(config, "warnKey", "", ref added);
            Add(config, "tackleKey", "", ref added);
            Add(config, "screenRadius", 100.0, ref added);
            Add(config, "unlockPvpMark", true, ref added);
            Add(config, "allowSelfFocus", true, ref added);
            Add(config, "overrideWheel", false, ref added);
            if (added) config.SaveIfDirty();
        }

        private static void Add(ModConfig config, string key, object value, ref bool added)
        {
            if (config.Has(key)) return;
            config.Set(key, value);
            added = true;
        }

        private static KeyCode ParseKey(string text, KeyCode fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return KeyCode.None;
            if (Enum.TryParse<KeyCode>(text.Trim(), true, out var parsed))
            {
                return parsed;
            }
            return fallback;
        }
    }
}
