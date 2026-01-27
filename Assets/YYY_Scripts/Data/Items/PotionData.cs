using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 药水数据 - HP药水、精力药水、Buff药水等
    /// </summary>
    [CreateAssetMenu(fileName = "Potion_New", menuName = "Farm/Items/Potion", order = 7)]
    public class PotionData : ItemData
    {
        [Header("=== 药水专属属性 ===")]
        [Tooltip("恢复HP值")]
        public int healthRestore = 50;

        [Tooltip("恢复精力值")]
        public int energyRestore = 0;

        [Tooltip("使用时间（秒）")]
        public float useTime = 0.5f;

        [Header("=== Buff效果 ===")]
        [Tooltip("Buff类型")]
        public BuffType buffType = BuffType.None;

        [Tooltip("Buff数值")]
        public float buffValue = 0f;

        [Tooltip("Buff持续时间（秒）")]
        public float buffDuration = 300f;

        [Header("=== 配方关联 ===")]
        [Tooltip("制作配方ID")]
        public int recipeID = 0;

        [Header("=== 视觉效果 ===")]
        [Tooltip("使用时的粒子效果")]
        public GameObject useEffectPrefab;

        [Tooltip("使用音效")]
        public AudioClip useSound;

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证药水ID范围（40XX）
            if (itemID < 4000 || itemID >= 4100)
            {
                Debug.LogWarning($"[{itemName}] 药水ID应在4000-4099范围内！");
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();

            if (healthRestore > 0)
                text += $"\n<color=red>+{healthRestore} HP</color>";

            if (energyRestore > 0)
                text += $"\n<color=yellow>+{energyRestore} 精力</color>";

            if (buffType != BuffType.None)
            {
                text += $"\n<color=cyan>{GetBuffDescription()}</color>";
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
                _ => ""
            };
        }
    }
}

