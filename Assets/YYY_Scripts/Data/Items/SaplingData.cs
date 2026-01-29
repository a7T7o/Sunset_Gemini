using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 树苗数据 - 可种植的树苗
    /// 放置后会实例化关联的树木预制体，并设置为阶段0（树苗）
    /// 季节样式由 TreeController 自动处理
    /// </summary>
    [CreateAssetMenu(fileName = "Sapling_New", menuName = "Farm/Placeable/Sapling", order = 1)]
    public class SaplingData : PlaceableItemData
    {
        [Header("=== 树苗专属属性 ===")]
        [Tooltip("关联的树木预制体（必须包含 TreeController 组件）")]
        public GameObject treePrefab;

        [Tooltip("种植经验值")]
        [Range(0, 100)]
        public int plantingExp = 5;

        [Header("=== 树苗显示 ===")]
        [Tooltip("手持时显示的树苗 Sprite（可选，为空时使用 icon）")]
        public Sprite heldSprite;

        #region PlaceableItemData 实现

        public override PlacementType GetPlacementType() => PlacementType.Sapling;

        public override GameObject GetPlacementPrefab() => treePrefab;

        /// <summary>
        /// 检查是否可以在指定位置放置
        /// 冬季无法种植树苗
        /// </summary>
        public override bool CanPlaceAt(Vector3 position)
        {
            // 检查冬季
            if (IsWinter())
            {
                Debug.Log("[SaplingData] 冬天无法种植树木");
                return false;
            }
            return base.CanPlaceAt(position);
        }

        /// <summary>
        /// 放置成功后的回调
        /// 设置树木为阶段0并应用当前季节样式
        /// </summary>
        public override void OnPlaced(Vector3 position, GameObject instance)
        {
            base.OnPlaced(position, instance);

            // 获取 TreeController 并设置为阶段0
            var treeController = instance.GetComponentInChildren<TreeController>();
            if (treeController != null)
            {
                // TreeController 会自动处理季节样式
                treeController.SetStage(0);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取手持显示 Sprite
        /// </summary>
        public Sprite GetHeldSprite() => heldSprite != null ? heldSprite : icon;

        /// <summary>
        /// 检查当前是否为冬季
        /// </summary>
        public bool IsWinter()
        {
            // 尝试通过 TimeManager 获取季节
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.GetSeason() == SeasonManager.Season.Winter;
            }
            // 尝试通过 SeasonManager 获取季节
            if (SeasonManager.Instance != null)
            {
                return SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Winter;
            }
            return false;
        }

        /// <summary>
        /// 获取树木预制体上的 TreeController 组件
        /// </summary>
        public TreeController GetTreeController()
        {
            if (treePrefab == null) return null;
            return treePrefab.GetComponentInChildren<TreeController>();
        }

        /// <summary>
        /// 获取阶段0的成长边距参数
        /// </summary>
        public bool GetStage0Margins(out float verticalMargin, out float horizontalMargin)
        {
            verticalMargin = 0.2f;
            horizontalMargin = 0.15f;

            var treeController = GetTreeController();
            if (treeController == null) return false;

            var stageConfig = treeController.CurrentStageConfig;
            if (stageConfig != null)
            {
                verticalMargin = stageConfig.verticalMargin;
                horizontalMargin = stageConfig.horizontalMargin;
                return true;
            }

            return false;
        }

        #endregion

        #region 验证

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证树苗ID范围（12XX - 树苗类）
            if (itemID < 1200 || itemID >= 1300)
            {
                Debug.LogWarning($"[{itemName}] 树苗ID建议在1200-1299范围内！当前:{itemID}");
            }

            // 验证树木预制体
            if (treePrefab == null)
            {
                Debug.LogWarning($"[{itemName}] 缺少关联的树木预制体！");
            }
            else
            {
                // 检查预制体是否包含 TreeController
                var treeController = treePrefab.GetComponentInChildren<TreeController>();
                if (treeController == null)
                {
                    Debug.LogError($"[{itemName}] 树木预制体 {treePrefab.name} 缺少 TreeController 组件！");
                }
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            
            // 冬季提示
            if (IsWinter())
            {
                text += "\n\n<color=red>冬天无法种植树木</color>";
            }
            
            if (plantingExp > 0)
                text += $"\n<color=cyan>种植经验: +{plantingExp}</color>";

            return text;
        }

        #endregion
    }
}
