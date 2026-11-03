using System;
using System.Reflection;
using CesiumLoader.SDK.Inputs;
using Core;
using Core.Mark;
using Core.Scene;
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
                KeyCode markKey = GameSettings.GetShortcutKey(GameShortcut.Mark);
                if (markKey != KeyCode.None && InputService.IsKeyPressed(markKey))
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
    }
}
