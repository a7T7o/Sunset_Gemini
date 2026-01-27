namespace FarmGame.Data
{
    /// <summary>
    /// 技能类型枚举（5大类）
    /// 定义玩家可以提升的5种独立技能类别
    /// 
    /// 变更说明：
    /// - 原有的 Farming/Mining/Woodcutting 已合并为 Gathering（采集）
    /// - 新增 Crafting（制作）和 Cooking（烹饪）
    /// </summary>
    public enum SkillType
    {
        /// <summary>战斗 - 打猎和打怪</summary>
        Combat = 0,
        
        /// <summary>采集 - 挖矿、砍树、耕种、收获</summary>
        Gathering = 1,
        
        /// <summary>制作 - 配方制作、NPC制作</summary>
        Crafting = 2,
        
        /// <summary>烹饪 - 食材处理加工</summary>
        Cooking = 3,
        
        /// <summary>钓鱼 - 钓鱼相关活动</summary>
        Fishing = 4,
        
        // ===== 以下为兼容旧代码的别名（已废弃，请使用新枚举） =====
        
        /// <summary>[已废弃] 请使用 Gathering</summary>
        [System.Obsolete("请使用 Gathering")]
        Farming = 1,
        
        /// <summary>[已废弃] 请使用 Gathering</summary>
        [System.Obsolete("请使用 Gathering")]
        Mining = 1,
        
        /// <summary>[已废弃] 请使用 Gathering</summary>
        [System.Obsolete("请使用 Gathering")]
        Woodcutting = 1
    }
}
