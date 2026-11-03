using System;
using CesiumLoader.SDK.Manifests;
using CesiumLoader.SDK.Mods;

namespace AttackNancyLuMod
{
    public static class ModEntry
    {
        public static void Main()
        {
            SdkManifest.ExportSidecar();
            ModBase.Run(new AttackNancyLuModController(), tag: "AttackNancyLu");
        }
    }

    public sealed class AttackNancyLuModController : ModBase
    {
        private AttackNancyLuSettings _settings = new AttackNancyLuSettings();
        private FocusController _controller;

        public override string Name => "集火南希露";
        public override string Version => "1.0.0";
        public override string Author => "hpenguins";
        public override string Description => "一键快捷集火鼠标指向的玩家或怪物，支持在战场 3D 模型与顶部 HUD 头像上秒发标点；同步解锁 PVP 模式标点系统。";

        public override void OnInitialize()
        {
            _controller = new FocusController(msg => Log.Info(msg), msg => Log.Warn(msg));
            AttackNancyLuSettings.EnsureDefaults(Config);
            _settings = AttackNancyLuSettings.Read(Config, warning => Log.Warn(warning));

            Log.Info("集火南希露: " + DescribeSettings());
        }

        public override void OnConfigChanged()
        {
            _settings = AttackNancyLuSettings.Read(Config, warning => Log.Warn(warning));
            Log.Info("集火南希露配置已更新: " + DescribeSettings());
        }

        public override void OnUpdate()
        {
            if (!_settings.Enabled || _controller == null) return;

            try
            {
                _controller.Update(_settings);
            }
            catch (Exception ex)
            {
                Log.Warn("集火南希露 Update 异常: " + ex.Message);
            }
        }

        private string DescribeSettings()
        {
            return "enabled=" + _settings.Enabled
                + ", focusKey=" + _settings.FocusKey
                + ", warnKey=" + _settings.WarnKey
                + ", tackleKey=" + _settings.TackleKey
                + ", radius=" + _settings.ScreenRadius
                + ", unlockPvp=" + _settings.UnlockPvpMark
                + ", allowSelf=" + _settings.AllowSelfFocus;
        }
    }
}
