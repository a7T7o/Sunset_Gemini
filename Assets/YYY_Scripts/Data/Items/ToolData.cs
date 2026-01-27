using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 动画动作类型枚举 - 对应 AnimState 中的工具动作
    /// 
    /// 工具类型到动画状态映射：
    /// - Axe（斧头）→ Slice（挥砍）
    /// - Sickle（镰刀）→ Slice（挥砍）
    /// - Sword（长剑）→ Pierce（刺出）
    /// - Pickaxe（镐子）→ Crush（挖掘）
    /// - Hoe（锄头）→ Crush（挖掘）
    /// - FishingRod（鱼竿）→ Fish（钓鱼）
    /// - WateringCan（洒水壶）→ Watering（浇水）
    /// </summary>
    public enum AnimActionType
    {
        Slice = 6,      // 挥砍（斧头、镰刀）
        Pierce = 7,     // 刺出（长剑）
        Crush = 8,      // 挖掘（镐子、锄头）
        Fish = 9,       // 钓鱼（鱼竿）
        Watering = 10   // 浇水（洒水壶）
    }

    /// <summary>
    /// 工具数据 - 锄头、水壶、镐子、斧头等
    /// 动画命名规范：{ActionType}_{Direction}_Clip_{itemID}_{quality}
    /// 例如：Slice_Down_Clip_0_0（itemID=0的斧头，品质0）
    /// 同一工具的不同品质使用相同的 itemID，通过 quality 参数区分
    /// </summary>
    [CreateAssetMenu(fileName = "Tool_New", menuName = "Farm/Items/Tool", order = 4)]
    public class ToolData : ItemData
    {
        [Header("=== 工具专属属性 ===")]
        [Tooltip("工具类型（决定使用哪种动画动作）")]
        public ToolType toolType;

        // 注意：每个品质的工具都是独立 ItemID，不再需要 quality 字段
        // 品质信息可以通过 ItemID 范围或命名规范来区分

        [Tooltip("使用消耗精力")]
        [Range(1, 20)]
        public int energyCost = 2;

        [Tooltip("作用范围（1=单格，3=3x3范围）")]
        [Range(1, 5)]
        public int effectRadius = 1;

        [Tooltip("效率加成（1.0=基础，2.0=两倍速）")]
        [Range(0.5f, 5.0f)]
        public float efficiencyMultiplier = 1.0f;

        [Header("=== 耐久度系统（可选）===")]
        [Tooltip("是否有耐久度")]
        public bool hasDurability = false;

        [Tooltip("最大耐久度")]
        public int maxDurability = 100;

        [Header("=== 材料等级 ===")]
        [Tooltip("工具的材料等级（0=木质, 1=石质, 2=生铁, 3=黄铜, 4=钢质, 5=金质）")]
        public MaterialTier materialTier = MaterialTier.Wood;

        [Header("=== 伤害配置（可选）===")]
        [Tooltip("是否可以造成伤害（如斧头可以攻击敌人）")]
        public bool canDealDamage = false;

        [Tooltip("伤害值（仅当 canDealDamage=true 时有效）")]
        [Range(1, 100)]
        public int damageAmount = 5;

        [Header("=== 动画配置 ===")]
        [Tooltip("工具专用的AnimatorController（直接拖拽赋值）")]
        public RuntimeAnimatorController animatorController;

        [Tooltip("工具动画帧数（用于帧同步）")]
        [Range(1, 30)]
        public int animationFrameCount = 8;

        [Tooltip("动画动作类型（Slice=斧头/镰刀, Crush=镐子, Pierce=锄头等）")]
        public AnimActionType animActionType = AnimActionType.Slice;
        
        // 注意：动画状态名使用 itemID，格式为 {ActionType}_{Direction}_Clip_{itemID}_{quality}
        // 同一工具的不同品质使用相同的 itemID，通过 quality 参数区分

        [Header("=== 音效 ===")]
        [Tooltip("工具使用音效")]
        public AudioClip useSound;

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证工具ID范围（00XX或01XX）
            if (itemID < 0 || itemID >= 200)
            {
                Debug.LogWarning($"[{itemName}] 工具ID应在0000-0199范围内！");
            }

            // 工具不应该可堆叠
            if (maxStackSize > 1)
            {
                Debug.LogWarning($"[{itemName}] 工具不应该可堆叠！");
                maxStackSize = 1;
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            text += $"\n\n<color=cyan>类型: {GetToolTypeName()}</color>";
            text += $"\n<color=yellow>消耗精力: {energyCost}</color>";

            if (effectRadius > 1)
                text += $"\n<color=green>范围: {effectRadius}x{effectRadius}</color>";

            if (hasDurability)
                text += $"\n<color=orange>耐久度: {maxDurability}</color>";

            return text;
        }

        private string GetToolTypeName()
        {
            return toolType switch
            {
                ToolType.Hoe => "锄头",
                ToolType.WateringCan => "水壶",
                ToolType.Sickle => "镰刀",
                ToolType.FishingRod => "钓鱼竿",
                ToolType.Pickaxe => "镐子",
                ToolType.Axe => "斧头",
                _ => "工具"
            };
        }

        /// <summary>
        /// 获取动画ID（用于动画状态名拼接）
        /// 动画状态名格式：{ActionType}_{Direction}_Clip_{itemID}_{quality}
        /// 直接使用物品的 itemID，同一工具不同品质使用相同 ID
        /// </summary>
        public int GetAnimationId()
        {
            return itemID;
        }

        /// <summary>
        /// 获取动画State值（用于Animator参数）
        /// </summary>
        public int GetAnimStateValue()
        {
            return (int)animActionType;
        }

        /// <summary>
        /// 获取动画动作类型名称（用于状态名拼接）
        /// </summary>
        public string GetAnimActionName()
        {
            return animActionType.ToString();
        }

        /// <summary>
        /// 获取材料等级值（用于材料等级判定）
        /// </summary>
        public int GetMaterialTierValue()
        {
            return (int)materialTier;
        }
    }
}