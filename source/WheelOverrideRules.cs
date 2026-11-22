namespace AttackNancyLuMod
{
    /// <summary>
    /// 「覆写标点轮盘」的判定逻辑，不依赖 Unity / FairyGUI，便于离线单元测试。
    /// </summary>
    internal static class WheelOverrideRules
    {
        /// <summary>
        /// 原生 <c>UIExpression_Com_ChatMenu</c> 里三格页（小心 / 集火 / 我来解决）的控制器下标；
        /// 0 是同队/自己用的四格页。
        /// </summary>
        internal const int ThreeSlotPageIndex = 1;

        /// <summary>
        /// 判断当前轮盘是否要改写成三格页。
        ///
        /// <paramref name="targetId"/> 为 0 表示这不是玩家标点菜单（地面 / 卡牌 / 筹码等），
        /// 必须原样放行；已经处于三格页时也不处理（原生敌队分支本来就是这三条，
        /// 同时避免每帧重复绑定）。关掉「允许集火自己」时，自己的轮盘保持原版。
        /// </summary>
        internal static bool ShouldRewrite(bool overrideWheel, int pageIndex, long targetId, long selfId, bool allowSelfFocus)
        {
            if (!overrideWheel) return false;
            if (pageIndex == ThreeSlotPageIndex) return false;
            if (targetId == 0) return false;
            if (selfId != 0 && targetId == selfId && !allowSelfFocus) return false;
            return true;
        }
    }
}
