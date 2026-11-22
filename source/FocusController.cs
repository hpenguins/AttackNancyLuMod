using System;
using System.Reflection;
using CesiumLoader.SDK.Inputs;
using Core;
using Core.Mark;
using Core.Scene;
using Core.Unit;
using FairyGUI;
using GameLogic;
using Tools;
using UI;
using UnityEngine;

namespace AttackNancyLuMod
{
    internal sealed class FocusController
    {
        private const int MarkFocusId = 40013;   // 集火{0}
        private const int MarkWarnId = 40012;    // 小心{0}
        private const int MarkTackleId = 40014;  // 我来解决{0}

        private static readonly FieldInfo BtnMapChatField = typeof(ExpressionWindow).GetField("_btn_MapChat", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo BtnMapToolField = typeof(ExpressionWindow).GetField("_btn_MapTool", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// UIExpression_Com_ChatMenu 里绑定单个标点项的私有方法
        /// （按钮, 悬浮高亮图, 图标纹理, 标点 ID, 目标 ID）：图标、悬浮提示与点击发送都在里面。
        /// 取不到时退回 <see cref="WireMenuItemFallback"/>（只用公有成员）。
        /// </summary>
        private static readonly MethodInfo RefreshPlayerMenuItemMethod = typeof(UIExpression_Com_ChatMenu).GetMethod(
            "RefreshPlayerMenuItem",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(UIExpression_Button_MenuItem), typeof(GImage), typeof(NTexture), typeof(int), typeof(long) },
            null);

        private bool _warnedWheelWiring;

        private readonly Action<string> _logInfo;
        private readonly Action<string> _logWarn;

        public FocusController(Action<string> logInfo, Action<string> logWarn)
        {
            _logInfo = logInfo;
            _logWarn = logWarn;
        }

        public void Update(AttackNancyLuSettings settings)
        {
            if (settings == null || !settings.Enabled) return;

            // Only run during battle scene
            if (SimpleSingletonProvider<SceneManager>.inst?.currentType?.Value != SceneType.Battle) return;

            var battle = SimpleSingletonProvider<GameLogicManager>.inst?.battle;
            if (battle == null || !battle.clientFinishReady) return;

            // 1. PVP Mark System Unlock
            if (settings.UnlockPvpMark)
            {
                HandlePvpMarkUnlock();
            }

            // 2. Shortcut Keys Detection
            CheckHotkeys(settings);

            // 3. Native Mark Wheel Override
            if (settings.OverrideWheel)
            {
                ApplyWheelOverride(settings);
            }
        }

        private void CheckHotkeys(AttackNancyLuSettings settings)
        {
            if (settings.ParsedFocusKey != KeyCode.None && InputService.IsKeyPressed(settings.ParsedFocusKey))
            {
                TriggerMark(MarkFocusId, settings, "集火");
            }
            else if (settings.ParsedWarnKey != KeyCode.None && InputService.IsKeyPressed(settings.ParsedWarnKey))
            {
                TriggerMark(MarkWarnId, settings, "小心");
            }
            else if (settings.ParsedTackleKey != KeyCode.None && InputService.IsKeyPressed(settings.ParsedTackleKey))
            {
                TriggerMark(MarkTackleId, settings, "我来解决");
            }
        }

        private void TriggerMark(int markId, AttackNancyLuSettings settings, string actionName)
        {
            long targetId = FindTargetUnderMouse(settings.ScreenRadius);
            if (targetId == 0) return;

            if (DispatchMark(targetId, markId, settings))
            {
                _logInfo?.Invoke($"[{actionName}] 标点已触发: 目标 ID={targetId}");
            }
        }

        /// <summary>
        /// 把原生标点轮盘的「玩家页」改写成「小心 / 集火 / 我来解决」三格页。
        ///
        /// 原生轮盘按队伍分支（<c>UIExpression_Com_ChatMenu.ShowPlayerMenu</c>）：只有目标队伍与自己
        /// 不同才走三格页，同队与自己是四格页（准备治疗 / 准备转账 / 准备发牌 / 技能）。三格页本身
        /// 就在预制体里（<c>com_TabThree</c>），这里只是把控制器切过去并按原生的参数顺序重新绑定三个
        /// 按钮，因此点击后走的是原生 onClick（原生协议 + 原生 CD）。
        ///
        /// 地面（40001~40004）与卡牌、筹码等其他标点菜单不碰：它们的 MarkTarget 不是玩家目标，
        /// <see cref="ResolveMarkTargetId"/> 返回 0。
        /// </summary>
        private void ApplyWheelOverride(AttackNancyLuSettings settings)
        {
            try
            {
                var expression = SimpleSingletonProvider<UIManager>.inst?.expression;
                if (expression == null || !expression.isShowing) return;
                if (!(expression.contentPane is UIExpressionWindow window)) return;

                var menu = window.com_ChatMenu;
                if (menu == null || !menu.visible || menu.type == null) return;

                long targetId = ResolveMarkTargetId(menu.MarkTarget);
                long selfId = GetSelfPlayerId();

                if (!WheelOverrideRules.ShouldRewrite(settings.OverrideWheel, menu.type.selectedIndex, targetId, selfId, settings.AllowSelfFocus))
                {
                    return;
                }

                menu.type.selectedIndex = WheelOverrideRules.ThreeSlotPageIndex;
                WireThreeSlotPage(menu, targetId);
            }
            catch (Exception ex)
            {
                _logWarn?.Invoke("覆写标点轮盘失败: " + ex.Message);
            }
        }

        /// <summary>轮盘当前目标对应的玩家 ID；不是玩家标点菜单时返回 0。</summary>
        private static long ResolveMarkTargetId(IMarkTarget target)
        {
            if (target is UIBattleInfo_Button_PlayerInfo playerButton)
            {
                return playerButton.PlayerData?.player?.Id ?? 0;
            }
            if (target is CharacterAnimator animator)
            {
                return animator.owner?.player?.Id ?? 0;
            }
            return 0;
        }

        private static long GetSelfPlayerId()
        {
            var self = SimpleSingletonProvider<GameLogicManager>.inst?.battle?.GetSelfPlayerData();
            return self?.player?.Id ?? 0;
        }

        /// <summary>按原生敌方分支的参数顺序绑定三格页：Top=小心，Left=集火，Right=我来解决。</summary>
        private void WireThreeSlotPage(UIExpression_Com_ChatMenu menu, long targetId)
        {
            var tab = menu.com_TabThree;
            if (tab == null) return;

            bool wired = true;
            wired &= TryWireMenuItem(menu, tab.btn_Top, tab.image_Top, menu.image_Exclamation, MarkWarnId, targetId);
            wired &= TryWireMenuItem(menu, tab.btn_Left, tab.image_Left, menu.image_Attack, MarkFocusId, targetId);
            wired &= TryWireMenuItem(menu, tab.btn_Right, tab.image_Right, menu.image_Praise, MarkTackleId, targetId);

            if (!wired && !_warnedWheelWiring)
            {
                _warnedWheelWiring = true;
                _logWarn?.Invoke("覆写标点轮盘失败: 游戏 UI 结构已变化，本次保持原版轮盘");
            }
        }

        private bool TryWireMenuItem(UIExpression_Com_ChatMenu menu, UIExpression_Button_MenuItem button, GImage highlight, GImage icon, int markId, long targetId)
        {
            if (button == null || icon == null) return false;

            if (RefreshPlayerMenuItemMethod != null)
            {
                try
                {
                    RefreshPlayerMenuItemMethod.Invoke(menu, new object[] { button, highlight, icon.texture, markId, targetId });
                    return true;
                }
                catch
                {
                    // 落到下面的公有 API 兜底实现。
                }
            }

            return WireMenuItemFallback(menu, button, highlight, icon.texture, markId, targetId);
        }

        /// <summary>原生 RefreshItem + onClick + onRollOver 的等价实现，只用公有成员。</summary>
        private static bool WireMenuItemFallback(UIExpression_Com_ChatMenu menu, UIExpression_Button_MenuItem button, GImage highlight, NTexture texture, int markId, long targetId)
        {
            try
            {
                if (button.loader_Icon == null || button.image_Hover == null || button.image_Frame == null) return false;

                button.loader_Icon.texture = texture;
                float rotation = (button.loader_Icon.rotation = 0f - button.rotation);
                button.image_Frame.rotation = rotation;
                button.image_Hover.rotation = rotation;
                if (highlight != null) highlight.visible = false;

                button.onRollOut.Set(() =>
                {
                    if (menu.showTip != null) menu.showTip.selectedIndex = 0;
                    if (highlight != null) highlight.visible = false;
                });

                button.onRollOver.Set(() =>
                {
                    if (menu.com_Preview != null) menu.com_Preview.txt_Tip.text = BattlePlayerMarkMessage.GetMsg(markId, targetId);
                    if (menu.showTip != null) menu.showTip.selectedIndex = 1;
                    if (highlight != null) highlight.visible = true;
                });

                button.onClick.Set(() =>
                {
                    menu.touchable = false;
                    menu.visible = false;
                    var self = SimpleSingletonProvider<GameLogicManager>.inst?.battle?.GetSelfPlayerData();
                    if (self?.player != null)
                    {
                        var message = new BattlePlayerMarkMessage(self, targetId, markId);
                        SimpleSingletonProvider<GameLogicManager>.inst.communicate.RequestSendMessageC2S(message, markId);
                    }
                    menu.touchable = true;
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        public long FindTargetUnderMouse(float screenRadius)
        {
            // A. Check FairyGUI touchTarget for HUD player avatars
            if (GRoot.inst != null)
            {
                for (GObject g = GRoot.inst.touchTarget; g != null; g = g.parent)
                {
                    if (g is UIBattleInfo_Button_PlayerInfo playerBtn && playerBtn.PlayerData?.player != null)
                    {
                        return playerBtn.PlayerData.player.Id;
                    }
                }
            }

            // B. Check 3D scene characters via camera screen distance
            Camera cam = BattleSceneController.inst?.mainCamera;
            var battle = SimpleSingletonProvider<GameLogicManager>.inst?.battle;
            if (cam != null && battle?.PlayerDatas != null)
            {
                Vector2 mousePos = Vector2.zero;
                if (!InputService.TryGetMousePosition(out mousePos))
                {
                    // Fallback to Stage touch position transformed if possible
                    if (Stage.inst != null)
                    {
                        mousePos = new Vector2(Stage.inst.touchPosition.x, (float)Screen.height - Stage.inst.touchPosition.y);
                    }
                }

                float bestDist = screenRadius;
                long bestId = 0;

                for (int i = 0; i < battle.PlayerDatas.Count; i++)
                {
                    var p = battle.PlayerDatas[i];
                    if (p?.player == null) continue;
                    if (p.Property != null && p.Property.HP.Value <= 0 && p.characterType == CharacterType.Monster) continue;
                    if (p.CharacterInst == null || !p.CharacterInst.gameObject.activeInHierarchy) continue;

                    Vector3 worldPos = p.CharacterInst.transform.position + Vector3.up * 1.0f;
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                    if (screenPos.z <= 0f) continue; // Behind camera

                    float dist = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), mousePos);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestId = p.player.Id;
                    }
                }

                if (bestId != 0) return bestId;
            }

            return 0;
        }

        private bool DispatchMark(long targetPlayerId, int markId, AttackNancyLuSettings settings)
        {
            var gameLogic = SimpleSingletonProvider<GameLogicManager>.inst;
            if (gameLogic?.battle == null || gameLogic.communicate == null) return false;

            var self = gameLogic.battle.GetSelfPlayerData();
            if (self?.player == null) return false;

            if (!settings.AllowSelfFocus && targetPlayerId == self.player.Id)
            {
                return false;
            }

            // Check rate limit license once
            if (!gameLogic.communicate.SendChatLicense())
            {
                return false;
            }

            BattlePlayerMarkMessage msg = new BattlePlayerMarkMessage(self, targetPlayerId, markId);
            // CD_CHECK must be false here because SendChatLicense() was already validated above.
            // If true, RequestSendMessageC2S would re-check in the same frame, fail at delta=0ms, and drop the message.
            gameLogic.communicate.RequestSendMessageC2S(msg, markId, CD_CHECK: false);
            return true;
        }

        private void HandlePvpMarkUnlock()
        {
            var roomInfo = SimpleSingletonProvider<GameLogicManager>.inst?.room?.curRoomInfo;
            if (roomInfo == null || !roomInfo.IsPVP()) return;

            var exprWin = SimpleSingletonProvider<UIManager>.inst?.expression;
            if (exprWin != null && exprWin.isShowing)
            {
                try
                {
                    if (BtnMapChatField != null && BtnMapChatField.GetValue(exprWin) is GObject btnChat && !btnChat.visible)
                    {
                        btnChat.visible = true;
                    }
                    if (BtnMapToolField != null && BtnMapToolField.GetValue(exprWin) is GObject btnTool && !btnTool.visible)
                    {
                        btnTool.visible = true;
                    }
                }
                catch
                {
                    // Ignore UI reflection exceptions
                }
            }

            // PVP Shortcut Key Handling
            try
            {
                var markKey = ReadMarkShortcutKey();
                if (markKey.HasValue && markKey.Value != KeyCode.None && InputService.IsKeyPressed(markKey.Value))
                {
                    var markMgr = BattleSceneController.inst?.MarkManager;
                    if (markMgr != null)
                    {
                        if (markMgr.IsActive)
                        {
                            markMgr.MarkInput?.ExitMark();
                        }
                        else
                        {
                            markMgr.MarkInput?.EnterMark();
                        }
                    }
                }
            }
            catch
            {
                // Ignore input handling exceptions
            }
        }

        /// <summary>
        /// 读取"进入标记模式"的快捷键。
        ///
        /// 游戏把快捷键枚举换过：旧版本是 <c>Core.GameShortcut</c>（成员 <c>Mark</c>），
        /// 2026-11-12 热更后换成全局 <c>KeyboardType</c>（成员 <c>MarkMode</c>），
        /// <c>GameSettings.GetShortcutKey</c> 的参数类型随之改变。直接写死枚举会让本程序集
        /// 只能跑在一个游戏版本上（旧版本每帧抛 <c>Could not load type</c>），所以这里按名字反射取值，
        /// 两个版本都能编译、都能取值。
        /// </summary>
        private static KeyCode? ReadMarkShortcutKey()
        {
            try
            {
                var method = typeof(GameSettings).GetMethod("GetShortcutKey", BindingFlags.Public | BindingFlags.Static);
                if (method == null) return null;

                var parameters = method.GetParameters();
                if (parameters.Length != 1) return null;

                var actionType = parameters[0].ParameterType;
                if (!actionType.IsEnum) return null;

                var names = Enum.GetNames(actionType);
                var actionName = Array.IndexOf(names, "MarkMode") >= 0 ? "MarkMode"
                               : Array.IndexOf(names, "Mark") >= 0 ? "Mark"
                               : null;
                if (actionName == null) return null;

                var action = Enum.Parse(actionType, actionName);
                return (KeyCode)method.Invoke(null, new[] { action });
            }
            catch
            {
                return null;
            }
        }
    }
}
