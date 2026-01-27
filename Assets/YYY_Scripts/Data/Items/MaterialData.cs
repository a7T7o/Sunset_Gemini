using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 材料数据 - 矿石、木材、怪物掉落等
    /// </summary>
    [CreateAssetMenu(fileName = "Material_New", menuName = "Farm/Items/Material", order = 6)]
    public class MaterialData : ItemData
    {
        [Header("=== 材料专属属性 ===")]
        [Tooltip("材料子类型")]
        public MaterialSubType materialSubType;

        [Tooltip("来源描述")]
        public string sourceDescription = "在洞穴挖矿获得";

        [Header("=== 加工相关（矿石）===")]
        [Tooltip("是否可以熔炼")]
        public bool canBeSmelt = false;

        [Tooltip("熔炼产物ID（铜矿石→铜锭）")]
        public int smeltResultID = 0;

        [Tooltip("熔炼时间（游戏小时）")]
        public int smeltTime = 5;

        [Header("=== 用途标记 ===")]
        [Tooltip("用于哪些配方/制作（显示用）")]
        [TextArea(2, 3)]
        public string craftingUse = "可用于制作铁剑、铁锄头等";

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证材料ID范围（3XXX）
            if (itemID < 3000 || itemID >= 4000)
            {
                Debug.LogWarning($"[{itemName}] 材料ID应在3000-3999范围内！");
            }

            // 如果可熔炼，验证产物ID
            if (canBeSmelt && (smeltResultID < 3100 || smeltResultID >= 3200))
            {
                Debug.LogWarning($"[{itemName}] 熔炼产物ID应在3100-3199（锭类）范围内！");
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            text += $"\n\n<color=grey>来源: {sourceDescription}</color>";

            if (canBeSmelt)
                text += $"\n<color=orange>可熔炼 ({smeltTime}小时)</color>";

            if (!string.IsNullOrEmpty(craftingUse))
                text += $"\n<color=cyan>{craftingUse}</color>";

            return text;
        }
    }
}

