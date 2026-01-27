using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 作物数据 - 收获的农作物
    /// </summary>
    [CreateAssetMenu(fileName = "Crop_New", menuName = "Farm/Items/Crop", order = 2)]
    public class CropData : ItemData
    {
        [Header("=== 作物专属属性 ===")]
        [Tooltip("对应的种子ID")]
        public int seedID;

        [Tooltip("收获经验值")]
        public int harvestExp = 10;

        [Tooltip("是否可以制作成食物")]
        public bool canBeCrafted = true;

        [Tooltip("用于哪些配方（仅显示用）")]
        [TextArea(2, 3)]
        public string usedInRecipes = "番茄汤、披萨";

        [Header("=== 品质说明 ===")]
        [Tooltip("品质影响售价，不影响外观。品质通过UI星星显示在物品槽左下角")]
        [TextArea(2, 3)]
        public string qualityInfo = "收获时随机判定品质（普通/铜星/银星/金星/彩星）\n品质越高售价越高，但作物外观不变";

        /// <summary>
        /// 获取作物图标（品质不影响外观，始终返回icon）
        /// </summary>
        public Sprite GetCropIcon()
        {
            return icon;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证作物ID范围（11XX）
            if (itemID < 1100 || itemID >= 1200)
            {
                Debug.LogWarning($"[{itemName}] 作物ID应在1100-1199范围内！");
            }

            // 验证种子ID
            if (seedID < 1000 || seedID >= 1100)
            {
                Debug.LogWarning($"[{itemName}] 对应种子ID应在1000-1099范围内！");
            }
        }
    }
}

