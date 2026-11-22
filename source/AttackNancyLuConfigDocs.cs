using CesiumLoader.SDK.Configuration;

namespace AttackNancyLuMod
{
    /// <summary>config.json 里各字段的多语言说明。</summary>
    internal static class AttackNancyLuConfigDocs
    {
        internal static bool Apply(ModConfig config)
        {
            if (config == null) return false;
            bool changed = false;

            changed |= config.Describe("enabled", D(
                "总开关：开启后生效。",
                "總開關：開啟後生效。",
                "Master switch: activates the mod when true.",
                "全体スイッチ：有効にすると Mod が動作します。"));

            changed |= config.Describe("focusKey", D(
                "一键集火快捷键（默认 X）：鼠标悬停在角色或顶部头像上时按下，立即向全场广播「集火目标」。",
                "一鍵集火快捷鍵（預設 X）：滑鼠懸停在角色或頂部頭像上時按下，立即向全場廣播「集火目標」。",
                "Quick focus hotkey (default X): Press while hovering over a character or top HUD avatar to focus-mark the target.",
                "クイック集火ショートカットキー（既定 X）：キャラクターまたは上部アバターにカーソルを合わせて押すと、ターゲットへの集火を即座に送信します。"));

            changed |= config.Describe("warnKey", D(
                "一键小心快捷键（默认空）：鼠标悬停在角色或顶部头像上时按下，立即向全场广播「小心目标」。留空表示不绑定。",
                "一鍵小心快捷鍵（預設空）：滑鼠懸停在角色或頂部頭像上時按下，立即向全場廣播「小心目標」。留空表示不綁定。",
                "Quick warning hotkey (default empty): Press while hovering to broadcast 'Careful of [Target]'. Leave empty to disable.",
                "クイック警戒ショートカットキー（既定空）：カーソルを合わせて押すと「ターゲットに注意」を送信します。空で無効。"));

            changed |= config.Describe("tackleKey", D(
                "一键我来解决快捷键（默认空）：鼠标悬停在角色或顶部头像上时按下，立即向全场广播「我来解决目标」。留空表示不绑定。",
                "一鍵我來解決快捷鍵（預設空）：滑鼠懸停在角色或頂部頭像上時按下，立即向全場廣播「我來解決目標」。留空表示不綁定。",
                "Quick tackle hotkey (default empty): Press while hovering to broadcast 'I will handle [Target]'. Leave empty to disable.",
                "クイック対処ショートカットキー（既定空）：カーソルを合わせて押すと「自分がターゲットを倒す」を送信します。空で無効。"));

            changed |= config.Describe("screenRadius", D(
                "屏幕吸附半径（像素，默认 100）：鼠标与 3D 角色的屏幕距离在此范围内时即可识别为标点目标。",
                "螢幕吸附半徑（像素，預設 100）：滑鼠與 3D 角色的螢幕距離在此範圍內時即可識別為標點目標。",
                "Screen targeting radius in pixels (default 100): Identifies the character closest to the mouse within this screen distance.",
                "画面吸着半径（ピクセル、既定 100）：この距離内の 3D キャラクターをターゲットとして認識します。"));

            changed |= config.Describe("unlockPvpMark", D(
                "解锁 PVP 模式标点系统（默认开启）：解除原版在 PVP 中隐藏标点按钮和禁用快捷键的限制，支持 PVP 标点地面、卡牌与对手。",
                "解鎖 PVP 模式標點系統（預設開啟）：解除原版在 PVP 中隱藏標點按鈕和禁用快捷鍵的限制，支援 PVP 標點地面、卡牌與對手。",
                "Unlock ping system in PVP (default true): Removes the vanilla restriction hiding the ping button and shortcut in PVP mode.",
                "PVP 標点システムのロック解除（既定有効）：PVP で標点ボタンやショートカットが無効化される制限を解除します。"));

            changed |= config.Describe("allowSelfFocus", D(
                "允许集火自己（默认开启）：鼠标悬停在自己角色或头像上按集火键时，允许发出「集火[自己]」趣味标点。",
                "允許集火自己（預設開啟）：滑鼠懸停在自己角色或頭像上按集火鍵時，允許發出「集火[自己]」趣味標點。",
                "Allow focusing yourself (default true): Allows focus-marking yourself when hovering over your own character or avatar.",
                "自分自身への集火を許可（既定有効）：自身のアバターやキャラクターに合わせて押したときに自分への集火を送信できるようにします。"));

            changed |= config.Describe("overrideWheel", D(
                "覆写标点轮盘（默认关闭）：开启后，把游戏原生标点轮盘的玩家页改写成「小心 / 集火 / 我来解决」三条；关闭时保持原版（对队友只显示准备治疗 / 准备转账 / 准备发牌 / 技能）。地面与卡牌标点不受影响。",
                "覆寫標點輪盤（預設關閉）：開啟後，把遊戲原生標點輪盤的玩家頁改寫成「小心 / 集火 / 我來解決」三條；關閉時保持原版（對隊友只顯示準備治療 / 準備轉帳 / 準備發牌 / 技能）。地面與卡牌標點不受影響。",
                "Override the ping wheel (default false): Rewrites the player page of the vanilla ping wheel to 'Careful / Focus / I'll handle it'. When off, the vanilla content stays (teammates show heal / transfer / deal / skill). Ground and card pings are not affected.",
                "標点ホイールを上書き（既定オフ）：オンにすると、ゲーム標準の標点ホイールのプレイヤーページを「警戒 / 集火 / 自分が対処」の 3 項目に書き換えます。オフのままなら従来どおり（味方には回復 / 送金 / 配布 / スキル）。地面とカードの標点には影響しません。"));

            return changed;
        }

        private static ConfigDoc D(string zhHans, string zhHant, string en, string ja)
            => ConfigDoc.Of(zhHans, zhHant, en, ja);
    }
}
