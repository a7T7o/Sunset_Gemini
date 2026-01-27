using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 食物数据 - 可食用的料理
    /// </summary>
    [CreateAssetMenu(fileName = "Food_New", menuName = "Farm/Items/Food", order = 3)]
    public class FoodData : ItemData
    {
        [Header("=== 食物专属属性 ===")]
        [Tooltip("恢复精力值")]
        public int energyRestore = 30;

        [Tooltip("恢复HP值")]
        public int healthRestore = 15;

        [Tooltip("食用动画时长（秒）")]
        public float consumeTime = 1.0f;

        [Header("=== Buff效果（可选）===")]
        [Tooltip("Buff类型")]
        public BuffType buffType = BuffType.None;

        [Tooltip("Buff数值（如速度+20表示填20）")]
        public float buffValue = 0f;

        [Tooltip("Buff持续时间（秒，0=永久直到睡觉）")]
        public float buffDuration = 0f;

        [Header("=== 配方关联 ===")]
        [Tooltip("对应的配方ID（如果是制作出来的）")]
        public int recipeID = 0;

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证食物ID范围（5XXX）
            if (itemID < 5000 || itemID >= 6000)
            {
                Debug.LogWarning($"[{itemName}] 食物ID应在5000-5999范围内！");
            }

            // 食物应该不可堆叠太多（通常最多20个）
            if (maxStackSize > 20)
            {
                Debug.LogWarning($"[{itemName}] 食物堆叠数建议不超过20！");
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            
            if (energyRestore > 0)
                text += $"\n<color=yellow>+{energyRestore} 精力</color>";
            
            if (healthRestore > 0)
                text += $"\n<color=red>+{healthRestore} HP</color>";

            if (buffType != BuffType.None)
            {
                text += $"\n<color=cyan>{GetBuffDescription()}</color>";
                if (buffDuration > 0)
                    text += $"\n<color=cyan>持续 {buffDuration}秒</color>";
            }

            return text;
        }

        private string GetBuffDescription()
        {
            return buffType switch
            {
                BuffType.Speed => $"移动速度 +{buffValue}%",
                BuffType.Attack => $"攻击力 +{buffValue}",
                BuffType.Defense => $"防御力 +{buffValue}",
                BuffType.Luck => $"幸运 +{buffValue}",
                BuffType.Fishing => $"钓鱼效率 +{buffValue}%",
                BuffType.Mining => $"挖矿效率 +{buffValue}%",
                BuffType.Farming => $"种田效率 +{buffValue}%",
                _ => ""
            };
        }
    }
}

