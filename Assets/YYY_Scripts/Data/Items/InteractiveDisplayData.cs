using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 交互展示数据 - 交互后显示 UI 提示信息
    /// 包括：告示牌、信息板、纪念碑等
    /// </summary>
    [CreateAssetMenu(fileName = "InteractiveDisplay_New", menuName = "Farm/Placeable/Interactive Display", order = 4)]
    public class InteractiveDisplayData : PlaceableItemData
    {
        [Header("=== 展示内容 ===")]
        [Tooltip("显示标题")]
        public string displayTitle = "标题";

        [Tooltip("显示内容（支持多行）")]
        [TextArea(3, 10)]
        public string displayContent = "这里是内容...";

        [Tooltip("显示图片（可选）")]
        public Sprite displayImage;

        [Header("=== 显示配置 ===")]
        [Tooltip("显示时长（秒，0=手动关闭）")]
        [Range(0f, 30f)]
        public float displayDuration = 0f;

        [Tooltip("再次交互是否关闭")]
        public bool closeOnInteract = true;

        [Header("=== 预制体 ===")]
        [Tooltip("展示物预制体")]
        public GameObject displayPrefab;

        #region PlaceableItemData 实现

        public override PlacementType GetPlacementType() => PlacementType.InteractiveDisplay;

        public override GameObject GetPlacementPrefab() => displayPrefab;

        public override void OnPlaced(Vector3 position, GameObject instance)
        {
            base.OnPlaced(position, instance);
            
            // TODO: 初始化展示组件
            Debug.Log($"[InteractiveDisplayData] 交互展示放置成功: {itemName}");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取显示时长描述
        /// </summary>
        public string GetDurationDescription()
        {
            if (displayDuration <= 0)
                return "手动关闭";
            return $"{displayDuration}秒后自动关闭";
        }

        /// <summary>
        /// 是否有图片
        /// </summary>
        public bool HasImage => displayImage != null;

        #endregion

        #region 验证

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证交互展示ID范围（15XX - 交互展示类）
            if (itemID < 1500 || itemID >= 1600)
            {
                Debug.LogWarning($"[{itemName}] 交互展示ID建议在1500-1599范围内！当前:{itemID}");
            }

            // 验证预制体
            if (displayPrefab == null)
            {
                Debug.LogWarning($"[{itemName}] 缺少展示物预制体！");
            }

            // 验证内容
            if (string.IsNullOrEmpty(displayTitle))
            {
                Debug.LogWarning($"[{itemName}] 显示标题为空！");
            }

            if (string.IsNullOrEmpty(displayContent))
            {
                Debug.LogWarning($"[{itemName}] 显示内容为空！");
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            text += $"\n\n<color=white>交互查看内容</color>";
            text += $"\n<color=gray>{GetDurationDescription()}</color>";

            return text;
        }

        #endregion
    }
}
