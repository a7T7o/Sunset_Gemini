using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 品质系统辅助类 - 提供品质相关的工具方法
    /// </summary>
    public static class QualityHelper
    {
        /// <summary>
        /// 随机生成作物品质（收获时调用）
        /// 基于星露谷的品质计算逻辑
        /// </summary>
        /// <param name="farmingLevel">玩家种田技能等级（0-10）</param>
        /// <param name="useFertilizer">是否使用了肥料</param>
        /// <returns>随机品质</returns>
        public static ItemQuality RandomCropQuality(int farmingLevel = 0, bool useFertilizer = false)
        {
            // 基础品质分数（0-1之间）
            float qualityScore = Random.value;

            // 种田等级加成（每级+0.02）
            qualityScore += farmingLevel * 0.02f;

            // 肥料加成（+0.1）
            if (useFertilizer)
                qualityScore += 0.1f;

            // 根据分数判定品质
            if (qualityScore >= 0.9f)
                return ItemQuality.Legendary;  // 猎奇（10%基础概率）
            else if (qualityScore >= 0.7f)
                return ItemQuality.Epic;       // 罕见（20%基础概率）
            else if (qualityScore >= 0.45f)
                return ItemQuality.Rare;       // 稀有（25%基础概率）
            else
                return ItemQuality.Normal;     // 普通（45%基础概率）
        }

        /// <summary>
        /// 获取品质显示文本（包含星星符号）
        /// </summary>
        public static string GetQualityDisplayText(ItemQuality quality)
        {
            return quality switch
            {
                ItemQuality.Normal => "",
                ItemQuality.Rare => "★",
                ItemQuality.Epic => "★",
                ItemQuality.Legendary => "★",
                _ => ""
            };
        }

        /// <summary>
        /// 获取品质星星颜色（用于UI着色）
        /// </summary>
        public static Color GetQualityStarColor(ItemQuality quality)
        {
            return quality switch
            {
                ItemQuality.Normal => Color.clear,
                ItemQuality.Rare => new Color(0.3f, 0.6f, 1f),        // 蓝色
                ItemQuality.Epic => new Color(0.6f, 0.3f, 0.9f),      // 紫色
                ItemQuality.Legendary => new Color(1f, 0.84f, 0f),    // 金色 #FFD700
                _ => Color.clear
            };
        }

        /// <summary>
        /// 获取品质名称（中文）
        /// </summary>
        public static string GetQualityName(ItemQuality quality)
        {
            return quality switch
            {
                ItemQuality.Normal => "普通",
                ItemQuality.Rare => "稀有",
                ItemQuality.Epic => "罕见",
                ItemQuality.Legendary => "猎奇",
                _ => "未知品质"
            };
        }

        /// <summary>
        /// 获取品质价格倍率
        /// </summary>
        public static float GetQualityPriceMultiplier(ItemQuality quality)
        {
            return quality switch
            {
                ItemQuality.Normal => 1.0f,
                ItemQuality.Rare => 1.25f,
                ItemQuality.Epic => 2.0f,
                ItemQuality.Legendary => 3.25f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// 计算带品质的售价
        /// </summary>
        public static int CalculatePriceWithQuality(int basePrice, ItemQuality quality)
        {
            return Mathf.RoundToInt(basePrice * GetQualityPriceMultiplier(quality));
        }
    }
}

