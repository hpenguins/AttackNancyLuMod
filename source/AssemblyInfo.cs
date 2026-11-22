using System.Runtime.CompilerServices;
using CesiumLoader.SDK.Manifests;

[assembly: InternalsVisibleTo("AttackNancyLuMod.Tests")]

[assembly: ModManifest("集火南希露", "1.1.0", "hpenguins",
    "一键快捷集火鼠标指向的玩家或怪物，支持在战场 3D 模型与顶部 HUD 头像上秒发标点；可解锁 PVP 标点系统，并可把原生标点轮盘的玩家页覆写为「小心/集火/我来解决」。",
    Permissions = ModPermission.ReadGameState | ModPermission.Input | ModPermission.UI,
    SdkVersion = "2.8.6")]
