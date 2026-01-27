using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 工作台数据 - 可制作物品的设施
    /// 包括：取暖设施、冶炼设施、制药设施、锯木设施、烹饪设施、装备和工具制作设施
    /// </summary>
    [CreateAssetMenu(fileName = "Workstation_New", menuName = "Farm/Placeable/Workstation", order = 2)]
    public class WorkstationData : PlaceableItemData
    {
        [Header("=== 工作台专属属性 ===")]
        [Tooltip("工作台类型")]
        public WorkstationType workstationType = WorkstationType.Crafting;

        [Tooltip("可制作配方列表引用（ScriptableObject）")]
        public ScriptableObject recipeListRef;

        [Tooltip("制作时间倍率（1.0 = 正常速度）")]
        [Range(0.1f, 5f)]
        public float craftTimeMultiplier = 1f;

        [Header("=== 燃料配置 ===")]
        [Tooltip("是否需要燃料")]
        public bool requiresFuel = false;

        [Tooltip("燃料槽数量")]
        [Range(0, 4)]
        public int fuelSlotCount = 1;

        [Tooltip("可接受的燃料物品类型")]
        public ItemCategory[] acceptedFuelTypes;

        [Header("=== 预制体 ===")]
        [Tooltip("工作台预制体")]
        public GameObject workstationPrefab;

        #region PlaceableItemData 实现

        public override PlacementType GetPlacementType() => PlacementType.Workstation;

        public override GameObject GetPlacementPrefab() => workstationPrefab;

        public override void OnPlaced(Vector3 position, GameObject instance)
        {
            base.OnPlaced(position, instance);
            
            // TODO: 初始化工作台组件
            Debug.Log($"[WorkstationData] 工作台放置成功: {itemName} ({workstationType})");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取工作台类型名称（中文）
        /// </summary>
        public string GetWorkstationTypeName()
        {
            return workstationType switch
            {
                WorkstationType.Heating => "取暖设施",
                WorkstationType.Smelting => "冶炼设施",
                WorkstationType.Pharmacy => "制药设施",
                WorkstationType.Sawmill => "锯木设施",
                WorkstationType.Cooking => "烹饪设施",
                WorkstationType.Crafting => "制作设施",
                _ => "未知设施"
            };
        }

        #endregion

        #region 验证

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证工作台ID范围（13XX - 工作台类）
            if (itemID < 1300 || itemID >= 1400)
            {
                Debug.LogWarning($"[{itemName}] 工作台ID建议在1300-1399范围内！当前:{itemID}");
            }

            // 验证预制体
            if (workstationPrefab == null)
            {
                Debug.LogWarning($"[{itemName}] 缺少工作台预制体！");
            }

            // 燃料配置验证
            if (requiresFuel && fuelSlotCount <= 0)
            {
                fuelSlotCount = 1;
                Debug.LogWarning($"[{itemName}] 需要燃料但燃料槽数量为0，已自动设为1");
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            text += $"\n\n<color=orange>类型: {GetWorkstationTypeName()}</color>";
            
            if (requiresFuel)
                text += $"\n<color=yellow>需要燃料</color>";

            return text;
        }

        #endregion
    }
}
