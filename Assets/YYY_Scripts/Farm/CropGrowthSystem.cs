using UnityEngine;
using FarmGame.Data;

namespace FarmGame.Farm
{
    /// <summary>
    /// [已废弃] 作物生长系统 - 每天检查所有作物并更新生长状态
    /// 请使用 CropManager 替代
    /// 此类保留用于兼容旧版 FarmingManager，将在后续版本中删除
    /// </summary>
    [System.Obsolete("使用 CropManager 替代，此类将在后续版本中删除")]
    public class CropGrowthSystem : MonoBehaviour
    {
        public static CropGrowthSystem Instance { get; private set; }

        [Header("=== 生长设置 ===")]
        [Tooltip("连续未浇水几天后生长停滞")]
        [SerializeField] private int daysWithoutWaterForStagnation = 2;

        [Tooltip("连续未浇水几天后作物枯萎")]
        [SerializeField] private int daysWithoutWaterForWithering = 3;

        [Header("=== 调试 ===")]
        [SerializeField] private bool enableDebugLog = true;

        private FarmingManager farmingManager;
        private TimeManager timeManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            farmingManager = FarmingManager.Instance;
            timeManager = TimeManager.Instance;

            if (timeManager == null)
            {
                Debug.LogError("[CropGrowthSystem] TimeManager未找到！");
                return;
            }

            // 订阅时间系统的每日事件
            TimeManager.OnDayChanged += OnDayChanged;

            if (enableDebugLog)
                Debug.Log("[CropGrowthSystem] 已初始化并订阅OnDayChanged事件");
        }

        private void OnDestroy()
        {
            TimeManager.OnDayChanged -= OnDayChanged;
        }

        /// <summary>
        /// 每天触发，检查所有作物
        /// </summary>
        private void OnDayChanged(int year, int day, int totalDays)
        {
            if (farmingManager == null)
            {
                Debug.LogWarning("[CropGrowthSystem] FarmingManager未找到！");
                return;
            }

            if (enableDebugLog)
                Debug.Log($"[CropGrowthSystem] 开始每日作物生长检查 - 第{day}天");

            UpdateAllCrops(day);
        }

        /// <summary>
        /// 更新所有作物的生长状态
        /// </summary>
        private void UpdateAllCrops(int currentDay)
        {
            var allTiles = farmingManager.GetAllFarmTiles();
            int updatedCount = 0;
            int witheredCount = 0;
            int stagnantCount = 0;

            foreach (var kvp in allTiles)
            {
                FarmTileData tileData = kvp.Value;
                
                // 跳过没有作物的格子
                if (!tileData.HasCrop())
                    continue;

                CropInstance crop = tileData.crop;
                
                // 跳过已经枯萎的作物
                if (crop.isWithered)
                    continue;

                // 检查浇水状态（昨天是否浇水）
                if (tileData.wateredYesterday)
                {
                    // 已浇水，正常生长
                    crop.daysWithoutWater = 0;
                    GrowCrop(crop);
                    updatedCount++;
                }
                else
                {
                    // 未浇水
                    crop.daysWithoutWater++;

                    // 检查是否枯萎
                    if (crop.daysWithoutWater >= daysWithoutWaterForWithering)
                    {
                        WitherCrop(crop);
                        witheredCount++;
                    }
                    // 检查是否停滞
                    else if (crop.daysWithoutWater >= daysWithoutWaterForStagnation)
                    {
                        stagnantCount++;
                        if (enableDebugLog)
                            Debug.Log($"[CropGrowthSystem] 作物生长停滞: {crop.seedData.itemName} (连续{crop.daysWithoutWater}天未浇水)");
                    }
                    else
                    {
                        // 还没达到停滞天数，继续生长（但警告）
                        GrowCrop(crop);
                        updatedCount++;
                        
                        if (enableDebugLog)
                            Debug.Log($"[CropGrowthSystem] 作物缺水但仍生长: {crop.seedData.itemName} (连续{crop.daysWithoutWater}天未浇水)");
                    }
                }
            }

            if (enableDebugLog)
            {
                Debug.Log($"[CropGrowthSystem] 生长检查完成: " +
                    $"生长{updatedCount}个, 停滞{stagnantCount}个, 枯萎{witheredCount}个");
            }
        }

        /// <summary>
        /// 作物生长一天
        /// </summary>
        private void GrowCrop(CropInstance crop)
        {
            if (crop.seedData.growthStageSprites == null || crop.seedData.growthStageSprites.Length == 0)
            {
                Debug.LogWarning($"[CropGrowthSystem] {crop.seedData.itemName} 没有设置生长阶段Sprite！");
                return;
            }

            // 增加生长天数
            crop.grownDays++;

            // 计算当前应该在哪个阶段
            int maxStage = crop.seedData.growthStageSprites.Length - 1;
            int totalGrowthDays = crop.seedData.growthDays;
            
            if (totalGrowthDays <= 0)
            {
                Debug.LogWarning($"[CropGrowthSystem] {crop.seedData.itemName} 的growthDays设置为0或负数！");
                return;
            }

            // 计算目标阶段（线性分布）
            float growthProgress = (float)crop.grownDays / totalGrowthDays;
            int targetStage = Mathf.FloorToInt(growthProgress * (maxStage + 1));
            targetStage = Mathf.Clamp(targetStage, 0, maxStage);

            // 更新阶段
            if (targetStage > crop.currentStage)
            {
                int oldStage = crop.currentStage;
                crop.currentStage = targetStage;
                crop.UpdateVisuals();

                if (enableDebugLog)
                {
                    Debug.Log($"[CropGrowthSystem] {crop.seedData.itemName} 生长: " +
                        $"阶段 {oldStage} → {targetStage} (天数: {crop.grownDays}/{totalGrowthDays})");
                }

                // 检查是否成熟
                if (crop.IsMature())
                {
                    if (enableDebugLog)
                        Debug.Log($"[CropGrowthSystem] {crop.seedData.itemName} 已成熟！可以收获");
                    
                    // TODO: 触发成熟特效（发光、闪烁等）
                }
            }
        }

        /// <summary>
        /// 作物枯萎
        /// </summary>
        private void WitherCrop(CropInstance crop)
        {
            crop.isWithered = true;
            crop.UpdateVisuals();

            if (enableDebugLog)
            {
                Debug.Log($"[CropGrowthSystem] {crop.seedData.itemName} 已枯萎 " +
                    $"(连续{crop.daysWithoutWater}天未浇水)");
            }

            // TODO: 播放枯萎音效
            // TODO: 播放枯萎粒子效果
        }

        #region 手动触发（测试用）

        /// <summary>
        /// 手动触发生长检查（用于测试）
        /// </summary>
        [ContextMenu("手动触发生长检查")]
        public void ManualGrowthUpdate()
        {
            if (timeManager != null)
            {
                UpdateAllCrops(timeManager.GetDay());
            }
            else
            {
                UpdateAllCrops(0);
            }
        }

        #endregion
    }
}
