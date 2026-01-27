using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 武器数据 - 战斗用武器
    /// 动画命名规范：{ActionType}_{Direction}_Clip_{itemID}_{quality}
    /// 例如：Pierce_Down_Clip_200_0（itemID=200的剑，品质0）
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_New", menuName = "Farm/Items/Weapon", order = 5)]
    public class WeaponData : ItemData
    {
        [Header("=== 武器专属属性 ===")]
        [Tooltip("武器类型")]
        public WeaponType weaponType;

        // 注意：武器没有"等级"属性，不同材质的武器是独立的 ItemID
        // 品质通过后缀命名规范区分，由用户在 SO 中自行编辑

        [Tooltip("攻击力")]
        [Range(1, 200)]
        public int attackPower = 10;

        [Tooltip("攻击速度（越小越快）")]
        [Range(0.3f, 3.0f)]
        public float attackSpeed = 1.0f;

        [Tooltip("暴击率（%）")]
        [Range(0, 100)]
        public float criticalChance = 5f;

        [Tooltip("暴击伤害倍率")]
        [Range(1.5f, 3.0f)]
        public float criticalDamageMultiplier = 2.0f;

        [Tooltip("攻击范围（像素）")]
        public float attackRange = 1.5f;

        [Tooltip("击退力度")]
        [Range(0, 10)]
        public float knockbackForce = 2f;

        [Header("=== 材料等级 ===")]
        [Tooltip("武器的材料等级（0=木质, 1=石质, 2=生铁, 3=黄铜, 4=钢质, 5=金质）")]
        public MaterialTier materialTier = MaterialTier.Wood;

        [Header("=== 消耗 ===")]
        [Tooltip("每次攻击消耗精力")]
        [Range(0, 10)]
        public int energyCostPerAttack = 1;

        [Header("=== 耐久度（可选）===")]
        [Tooltip("是否有耐久度")]
        public bool hasDurability = false;

        [Tooltip("最大耐久度")]
        public int maxDurability = 200;

        [Header("=== 动画配置 ===")]
        [Tooltip("武器专用的AnimatorController（直接拖拽赋值）")]
        public RuntimeAnimatorController animatorController;

        [Tooltip("武器动画帧数（用于帧同步）")]
        [Range(1, 30)]
        public int animationFrameCount = 8;

        [Tooltip("动画动作类型（Pierce=刺出/长剑, Slice=挥砍）")]
        public AnimActionType animActionType = AnimActionType.Pierce;

        [Header("=== 音效 ===")]
        [Tooltip("攻击音效")]
        public AudioClip attackSound;

        [Tooltip("击中音效")]
        public AudioClip hitSound;

        // 注意：动画状态名使用 itemID，格式为 {ActionType}_{Direction}_Clip_{itemID}_{quality}
        // 同一武器的不同品质使用相同的 itemID，通过 quality 参数区分

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证武器ID范围（02XX）
            if (itemID < 200 || itemID >= 300)
            {
                Debug.LogWarning($"[{itemName}] 武器ID应在0200-0299范围内！");
            }

            // 武器不可堆叠
            if (maxStackSize > 1)
            {
                maxStackSize = 1;
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            text += $"\n\n<color=red>攻击力: {attackPower}</color>";
            text += $"\n<color=red>攻击速度: {attackSpeed:F2}</color>";
            text += $"\n<color=orange>暴击率: {criticalChance}%</color>";

            return text;
        }

        /// <summary>
        /// 计算最终伤害（考虑暴击）
        /// </summary>
        public int CalculateDamage()
        {
            bool isCrit = Random.value * 100 < criticalChance;
            float damage = attackPower;
            
            if (isCrit)
                damage *= criticalDamageMultiplier;

            return Mathf.RoundToInt(damage);
        }

        /// <summary>
        /// 获取动画ID（用于动画状态名拼接）
        /// 动画状态名格式：{ActionType}_{Direction}_Clip_{itemID}_{quality}
        /// 直接使用物品的 itemID，同一武器不同品质使用相同 ID
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

