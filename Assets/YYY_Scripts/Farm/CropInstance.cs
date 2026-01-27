using UnityEngine;
using FarmGame.Data;

namespace FarmGame.Farm
{
    /// <summary>
    /// [已废弃] 作物实例 - 运行时的单个作物对象
    /// 请使用 CropInstanceData 替代
    /// 此类保留用于兼容旧代码，将在后续版本中删除
    /// </summary>
    [System.Obsolete("使用 CropInstanceData 替代，此类将在后续版本中删除")]
    [System.Serializable]
    public class CropInstance
    {
        /// <summary>
        /// 种子数据引用
        /// </summary>
        public SeedData seedData;

        /// <summary>
        /// 场景中的作物GameObject
        /// </summary>
        [System.NonSerialized]
        public GameObject cropObject;

        /// <summary>
        /// 当前生长阶段（0 = 种子，N = 成熟）
        /// </summary>
        public int currentStage;

        /// <summary>
        /// 种植时的游戏天数
        /// </summary>
        public int plantedDay;

        /// <summary>
        /// 当前生长的总天数（实际生长天数，不包含停滞天数）
        /// </summary>
        public int grownDays;

        /// <summary>
        /// 连续未浇水天数
        /// </summary>
        public int daysWithoutWater;

        /// <summary>
        /// 是否枯萎
        /// </summary>
        public bool isWithered;

        /// <summary>
        /// 已收获次数（可重复收获用）
        /// </summary>
        public int harvestCount;

        /// <summary>
        /// 作物品质（收获时确定）
        /// </summary>
        public ItemQuality quality;

        /// <summary>
        /// 上次收获的天数（可重复收获用）
        /// </summary>
        public int lastHarvestDay;

        public CropInstance(SeedData seed, int currentDay)
        {
            seedData = seed;
            cropObject = null;
            currentStage = 0;
            plantedDay = currentDay;
            grownDays = 0;
            daysWithoutWater = 0;
            isWithered = false;
            harvestCount = 0;
            quality = ItemQuality.Normal;
            lastHarvestDay = -1;
        }

        /// <summary>
        /// 是否已成熟可收获
        /// </summary>
        public bool IsMature()
        {
            if (isWithered) return false;
            
            // 检查是否达到最后阶段
            if (seedData.growthStageSprites == null || seedData.growthStageSprites.Length == 0)
                return false;

            return currentStage >= seedData.growthStageSprites.Length - 1;
        }

        /// <summary>
        /// 是否可以再次收获（可重复收获作物）
        /// </summary>
        public bool CanReHarvest(int currentDay)
        {
            if (!seedData.isReHarvestable) return false;
            if (!IsMature()) return false;
            if (lastHarvestDay < 0) return true; // 第一次收获

            // 检查是否达到重复收获间隔
            int daysSinceLastHarvest = currentDay - lastHarvestDay;
            return daysSinceLastHarvest >= seedData.reHarvestDays;
        }

        /// <summary>
        /// 获取当前阶段的Sprite
        /// </summary>
        public Sprite GetCurrentSprite()
        {
            if (seedData.growthStageSprites == null || seedData.growthStageSprites.Length == 0)
                return null;

            int index = Mathf.Clamp(currentStage, 0, seedData.growthStageSprites.Length - 1);
            return seedData.growthStageSprites[index];
        }

        /// <summary>
        /// 更新作物外观
        /// </summary>
        public void UpdateVisuals()
        {
            if (cropObject == null) return;

            SpriteRenderer renderer = cropObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Sprite sprite = GetCurrentSprite();
                if (sprite != null)
                {
                    renderer.sprite = sprite;
                }

                // 枯萎时变黄
                if (isWithered)
                {
                    renderer.color = new Color(0.8f, 0.8f, 0.5f, 1f);
                }
                else
                {
                    renderer.color = Color.white;
                }
            }
        }
    }
}
