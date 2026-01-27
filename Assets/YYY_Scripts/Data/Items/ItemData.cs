using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 物品数据基类 - 所有物品的共同属性
    /// ScriptableObject 可以在编辑器中创建资产文件
    /// </summary>
    public class ItemData : ScriptableObject
    {
        [Header("=== 基础识别信息 ===")]
        [Tooltip("物品唯一ID（0000-9999）")]
        [Range(0, 9999)]
        public int itemID;

        [Tooltip("物品名称")]
        public string itemName = "新物品";

        [Tooltip("物品描述（悬停时显示）")]
        [TextArea(3, 5)]
        public string description = "这是一个物品。";

        [Tooltip("物品大类")]
        public ItemCategory category;

        [Header("=== 视觉资源 ===")]
        [Tooltip("基础图标（用于生成其他形式）")]
        public Sprite icon;

        [Tooltip("背包/工具栏显示图标（为空时使用icon）")]
        public Sprite bagSprite;
        
        [Tooltip("背包图标是否旋转45度显示")]
        public bool rotateBagIcon = true;

        [Tooltip("世界预制体（含动画、阴影，无碰撞体）")]
        public GameObject worldPrefab;

        [Header("=== 经济属性 ===")]
        [Tooltip("购买价格（0=不可购买）")]
        public int buyPrice = 0;

        [Tooltip("出售价格（卖给商店）")]
        public int sellPrice = 0;

        [Header("=== 堆叠属性 ===")]
        [Tooltip("最大堆叠数量（1=不可堆叠，如工具）")]
        [Range(1, 999)]
        public int maxStackSize = 99;

        [Header("=== 显示尺寸配置 ===")]
        
        [Header("--- 背包显示 ---")]
        [Tooltip("是否启用自定义背包图标尺寸")]
        public bool useCustomBagDisplaySize = false;
        
        [Tooltip("背包图标像素尺寸限定（8-128），图标将等比例缩放至适配此方框边长")]
        [Range(8, 128)]
        public int bagDisplayPixelSize = 52;
        
        [Tooltip("背包图标位置偏移（像素）")]
        public Vector2 bagDisplayOffset = Vector2.zero;
        
        [Header("--- 世界显示 ---")]
        [Tooltip("是否启用自定义世界物品尺寸")]
        public bool useCustomDisplaySize = false;

        [Tooltip("世界物品像素尺寸限定（8-128），物品 Sprite 将等比例缩放至适配此方框边长")]
        [Range(8, 128)]
        public int displayPixelSize = 32;
        
        [Tooltip("世界物品位置偏移（单位）")]
        public Vector2 worldDisplayOffset = Vector2.zero;

        [Header("=== 功能标记 ===")]
        [Tooltip("是否可以丢弃")]
        public bool canBeDiscarded = true;

        [Tooltip("是否是任务物品")]
        public bool isQuestItem = false;

        [Header("=== 放置配置 ===")]
        [Tooltip("是否可以放置到世界中")]
        public bool isPlaceable = false;

        [Tooltip("放置类型（决定放置规则）")]
        public PlacementType placementType = PlacementType.None;

        [Tooltip("放置时实例化的预制体")]
        public GameObject placementPrefab;

        [Tooltip("建筑尺寸（仅建筑类型有效，单位：格子）")]
        public Vector2Int buildingSize = Vector2Int.one;

        [Header("=== 装备配置 ===")]
        [Tooltip("装备类型（None表示非装备，用于快速装备功能）")]
        public EquipmentType equipmentType = EquipmentType.None;

        [Header("=== 消耗品配置 ===")]
        [Tooltip("消耗品类型（None表示非消耗品，用于右键使用功能）")]
        public ConsumableType consumableType = ConsumableType.None;

        /// <summary>
        /// 是否可以堆叠
        /// </summary>
        public bool IsStackable => maxStackSize > 1;

        /// <summary>
        /// 获取背包/工具栏显示Sprite（优先bagSprite，回退到icon）
        /// </summary>
        public Sprite GetBagSprite() => bagSprite != null ? bagSprite : icon;
        
        /// <summary>
        /// 获取背包图标的自定义显示尺寸（像素）
        /// </summary>
        /// <returns>自定义尺寸，未启用时返回 -1 表示使用默认</returns>
        public int GetBagDisplayPixelSize()
        {
            if (!useCustomBagDisplaySize) return -1;
            return Mathf.Clamp(bagDisplayPixelSize, 8, 128);
        }

        /// <summary>
        /// 获取世界物品的缩放比例（基于 displayPixelSize）
        /// 用于 World Prefab 和运行时掉落物的尺寸控制
        /// </summary>
        /// <returns>缩放比例，未启用自定义尺寸时返回 1.0</returns>
        public float GetWorldDisplayScale()
        {
            if (!useCustomDisplaySize) return 1f;
            if (icon == null) return 1f;
            
            float spriteWidth = icon.rect.width;
            float spriteHeight = icon.rect.height;
            float maxDimension = Mathf.Max(spriteWidth, spriteHeight);
            
            if (maxDimension <= 0) return 1f;
            
            // 确保 displayPixelSize 在有效范围内
            int clampedSize = Mathf.Clamp(displayPixelSize, 8, 128);
            return clampedSize / maxDimension;
        }

        /// <summary>
        /// 获取品质加成后的售价（向上取整）
        /// </summary>
        public int GetSellPriceWithQuality(ItemQuality quality)
        {
            float multiplier = quality switch
            {
                ItemQuality.Normal => 1.0f,
                ItemQuality.Rare => 1.25f,
                ItemQuality.Epic => 2.0f,
                ItemQuality.Legendary => 3.25f,
                _ => 1.0f
            };
            return Mathf.CeilToInt(sellPrice * multiplier);
        }

        /// <summary>
        /// 获取品质星星颜色（用于UI显示）
        /// Normal 无星星，其他品质显示对应颜色星星
        /// </summary>
        public Color GetQualityStarColor(ItemQuality quality)
        {
            return quality switch
            {
                ItemQuality.Normal => Color.clear,                    // 无星星
                ItemQuality.Rare => new Color(0.3f, 0.7f, 1f),       // 蓝色（稀有）
                ItemQuality.Epic => new Color(0.6f, 0.2f, 0.8f),     // 紫色（罕见）
                ItemQuality.Legendary => new Color(1f, 0.8f, 0.2f),  // 金色（猎奇）
                _ => Color.clear
            };
        }

        /// <summary>
        /// 获取品质名称（中文）
        /// </summary>
        public string GetQualityName(ItemQuality quality)
        {
            return quality switch
            {
                ItemQuality.Normal => "普通",
                ItemQuality.Rare => "稀有",
                ItemQuality.Epic => "罕见",
                ItemQuality.Legendary => "猎奇",
                _ => "未知"
            };
        }

        /// <summary>
        /// 验证数据完整性（在Inspector中显示警告）
        /// </summary>
        protected virtual void OnValidate()
        {
            // 验证ID范围
            if (itemID < 0 || itemID > 9999)
            {
                Debug.LogWarning($"[{itemName}] ID超出范围！应在0-9999之间。");
            }

            // 验证名称
            if (string.IsNullOrEmpty(itemName))
            {
                Debug.LogWarning($"[ID:{itemID}] 物品名称为空！");
            }

            // 验证图标
            if (icon == null)
            {
                Debug.LogWarning($"[{itemName}] 缺少图标！");
            }

            // 验证价格逻辑
            if (sellPrice > buyPrice && buyPrice > 0)
            {
                Debug.LogWarning($"[{itemName}] 售价({sellPrice})高于买价({buyPrice})，不合理！");
            }
            
            // 验证显示尺寸参数
            if (useCustomBagDisplaySize)
            {
                if (bagDisplayPixelSize <= 0)
                {
                    bagDisplayPixelSize = 52;
                    Debug.LogWarning($"[{itemName}] bagDisplayPixelSize 无效，已重置为默认值 52");
                }
                else if (bagDisplayPixelSize < 8 || bagDisplayPixelSize > 128)
                {
                    Debug.LogWarning($"[{itemName}] bagDisplayPixelSize({bagDisplayPixelSize}) 超出推荐范围 8-128");
                }
            }
            
            if (useCustomDisplaySize)
            {
                if (displayPixelSize <= 0)
                {
                    displayPixelSize = 32;
                    Debug.LogWarning($"[{itemName}] displayPixelSize 无效，已重置为默认值 32");
                }
                else if (displayPixelSize < 8 || displayPixelSize > 128)
                {
                    Debug.LogWarning($"[{itemName}] displayPixelSize({displayPixelSize}) 超出推荐范围 8-128");
                }
            }

            // 验证放置配置
            if (isPlaceable)
            {
                if (placementType == PlacementType.None)
                {
                    Debug.LogWarning($"[{itemName}] 已启用放置但未设置放置类型！");
                }
                if (placementPrefab == null)
                {
                    Debug.LogWarning($"[{itemName}] 已启用放置但未设置放置预制体！");
                }
                if (placementType == PlacementType.Building && (buildingSize.x <= 0 || buildingSize.y <= 0))
                {
                    Debug.LogWarning($"[{itemName}] 建筑类型但尺寸无效！");
                }
            }
        }

        /// <summary>
        /// 获取物品的完整信息文本（用于Tooltip显示）
        /// </summary>
        public virtual string GetTooltipText()
        {
            string text = $"<b>{itemName}</b>\n\n{description}";
            
            if (sellPrice > 0)
                text += $"\n\n<color=yellow>售价: {sellPrice}金币</color>";
            
            if (buyPrice > 0)
                text += $"\n<color=yellow>购买: {buyPrice}金币</color>";

            return text;
        }
    }
}

