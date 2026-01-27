using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.Farm
{
    /// <summary>
    /// 农田管理器 - 支持3种土壤湿度视觉状态
    /// 逻辑：浇水当天记录，第二天才影响作物生长
    /// 视觉：浇水2小时内有水渍，2小时后深色，第二天变干
    /// </summary>
    public class FarmingManager : MonoBehaviour
    {
        public static FarmingManager Instance { get; private set; }

        [Header("=== Tilemap引用 ===")]
        [SerializeField] private Tilemap farmlandTilemap;
        [SerializeField] private Tilemap groundTilemap;

        [Header("=== Tile资源（3种状态）===")]
        [Tooltip("干燥状态")]
        [SerializeField] private TileBase dryFarmlandTile;
        
        [Tooltip("湿润+水渍状态（浇水后2小时内）")]
        [SerializeField] private TileBase[] wetWithPuddleTiles; // 3种水渍变体
        
        [Tooltip("湿润深色状态（浇水2小时后）")]
        [SerializeField] private TileBase wetDarkTile;

        [Header("=== 视觉切换设置 ===")]
        [Tooltip("浇水多少小时后，水渍消失变为深色")]
        [SerializeField] private float hoursUntilPuddleDry = 2f;

        [Header("=== 作物设置 ===")]
        [SerializeField] private GameObject cropPrefab;
        [SerializeField] private Transform cropsContainer;

        [Header("=== 音效与特效 ===")]
        [SerializeField] private AudioClip tillingSoundClip;
        [SerializeField] private AudioClip wateringSoundClip;
        [SerializeField] private AudioClip harvestSoundClip;
        [SerializeField] private GameObject tillingParticlePrefab;
        [SerializeField] private GameObject wateringParticlePrefab;

        [Header("=== 精力消耗 ===")]
        [Tooltip("锄地消耗的精力（TODO: 集成精力系统后启用）")]
        #pragma warning disable 0414
        [SerializeField] private float tillingStaminaCost = 3f;
        
        [Tooltip("浇水消耗的精力（TODO: 集成精力系统后启用）")]
        [SerializeField] private float wateringStaminaCost = 2f;
        #pragma warning restore 0414

        // 数据存储
        private Dictionary<Vector3Int, FarmTileData> farmTiles = new Dictionary<Vector3Int, FarmTileData>();
        
        // 引用
        private TimeManager timeManager;
        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            timeManager = TimeManager.Instance;
            
            if (cropsContainer == null)
            {
                GameObject container = new GameObject("Crops");
                cropsContainer = container.transform;
                cropsContainer.SetParent(transform);
            }

            ValidateReferences();
            
            // 订阅时间事件（静态事件）
            TimeManager.OnDayChanged += OnDayChanged;
            TimeManager.OnHourChanged += OnHourChanged;
        }

        private void OnDestroy()
        {
            TimeManager.OnDayChanged -= OnDayChanged;
            TimeManager.OnHourChanged -= OnHourChanged;
        }

        private void ValidateReferences()
        {
            if (farmlandTilemap == null)
                Debug.LogError("[FarmingManager] Farmland Tilemap未设置！");
            
            if (dryFarmlandTile == null)
                Debug.LogError("[FarmingManager] Dry Farmland Tile未设置！");
            
            if (wetWithPuddleTiles == null || wetWithPuddleTiles.Length == 0)
                Debug.LogError("[FarmingManager] Wet Puddle Tiles未设置！至少需要1个");
            
            if (wetDarkTile == null)
                Debug.LogError("[FarmingManager] Wet Dark Tile未设置！");
            
            if (cropPrefab == null)
                Debug.LogError("[FarmingManager] Crop Prefab未设置！");
        }

        #region 时间事件处理

        /// <summary>
        /// 每天开始时触发：更新所有土地的浇水状态
        /// 逻辑：昨天浇水的记录→今天生效；今天的浇水状态→重置为干燥
        /// </summary>
        private void OnDayChanged(int year, int day, int totalDays)
        {
            Debug.Log($"[FarmingManager] 新的一天开始 - 第{day}天");

            foreach (var kvp in farmTiles)
            {
                FarmTileData tile = kvp.Value;
                
                // 昨天的浇水记录 → 成为今天的生效状态
                tile.wateredYesterday = tile.wateredToday;
                
                // 今天的浇水状态重置为未浇水
                tile.wateredToday = false;
                tile.waterTime = -1f;
                
                // 第二天土地变干（视觉）
                tile.moistureState = SoilMoistureState.Dry;
                UpdateTileVisual(kvp.Key, tile);
            }
        }

        /// <summary>
        /// 每小时触发：更新土壤视觉状态（水渍→深色）
        /// </summary>
        private void OnHourChanged(int currentHour)
        {
            if (timeManager == null) return;

            float currentTime = timeManager.GetHour() + timeManager.GetMinute() / 60f;

            foreach (var kvp in farmTiles)
            {
                FarmTileData tile = kvp.Value;
                
                // 只处理今天浇过水的土地
                if (tile.wateredToday && tile.waterTime >= 0)
                {
                    float hoursSinceWatering = currentTime - tile.waterTime;
                    
                    // 处理跨天情况（例如23点浇水，1点检查）
                    if (hoursSinceWatering < 0)
                        hoursSinceWatering += 24f;
                    
                    // 超过2小时：水渍消失，变为深色
                    if (hoursSinceWatering >= hoursUntilPuddleDry)
                    {
                        if (tile.moistureState == SoilMoistureState.WetWithPuddle)
                        {
                            tile.moistureState = SoilMoistureState.WetDark;
                            UpdateTileVisual(kvp.Key, tile);
                        }
                    }
                }
            }
        }

        #endregion

        #region 锄地系统

        public bool TillSoil(Vector3 worldPosition)
        {
            Vector3Int cellPosition = farmlandTilemap.WorldToCell(worldPosition);
            return TillSoilAtCell(cellPosition);
        }

        public bool TillSoilAtCell(Vector3Int cellPosition)
        {
            if (!CanTillAtPosition(cellPosition))
            {
                Debug.Log($"[FarmingManager] 此位置不能耕作: {cellPosition}");
                return false;
            }

            // TODO: 检查精力
            // if (!StaminaSystem.Instance.ConsumeStamina(tillingStaminaCost))
            //     return false;

            // 创建或获取FarmTileData
            if (!farmTiles.ContainsKey(cellPosition))
            {
                farmTiles[cellPosition] = new FarmTileData(cellPosition);
            }

            FarmTileData tileData = farmTiles[cellPosition];
            tileData.isTilled = true;
            tileData.moistureState = SoilMoistureState.Dry;

            // 设置Tile
            UpdateTileVisual(cellPosition, tileData);

            // 播放音效和粒子
            PlayTillingEffects(farmlandTilemap.GetCellCenterWorld(cellPosition));

            Debug.Log($"[FarmingManager] 成功耕作: {cellPosition}");
            return true;
        }

        private bool CanTillAtPosition(Vector3Int cellPosition)
        {
            if (farmTiles.ContainsKey(cellPosition) && farmTiles[cellPosition].isTilled)
            {
                return false;
            }

            if (groundTilemap != null)
            {
                TileBase groundTile = groundTilemap.GetTile(cellPosition);
                if (groundTile == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void PlayTillingEffects(Vector3 worldPosition)
        {
            if (tillingSoundClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(tillingSoundClip);
            }

            if (tillingParticlePrefab != null)
            {
                GameObject particles = Instantiate(tillingParticlePrefab, worldPosition, Quaternion.identity);
                Destroy(particles, 2f);
            }
        }

        #endregion

        #region 种植系统

        public bool PlantSeed(Vector3 worldPosition, SeedData seedData)
        {
            Vector3Int cellPosition = farmlandTilemap.WorldToCell(worldPosition);
            return PlantSeedAtCell(cellPosition, seedData);
        }

        public bool PlantSeedAtCell(Vector3Int cellPosition, SeedData seedData)
        {
            if (seedData == null)
            {
                Debug.LogWarning("[FarmingManager] SeedData为空！");
                return false;
            }

            if (!CanPlantAtPosition(cellPosition, out FarmTileData tileData))
            {
                Debug.Log($"[FarmingManager] 无法在此位置种植: {cellPosition}");
                return false;
            }

            if (timeManager != null && !IsCorrectSeason(seedData))
            {
                Debug.Log($"[FarmingManager] {seedData.itemName} 不适合当前季节种植！");
                return false;
            }

            // 创建作物实例
            int currentDay = timeManager != null ? timeManager.GetDay() : 0;
            CropInstance cropInstance = new CropInstance(seedData, currentDay);

            // 生成作物GameObject
            Vector3 worldPos = farmlandTilemap.GetCellCenterWorld(cellPosition);
            GameObject cropObj = Instantiate(cropPrefab, worldPos, Quaternion.identity, cropsContainer);
            cropObj.name = $"Crop_{seedData.itemName}_{cellPosition}";
            
            cropInstance.cropObject = cropObj;
            
            // 初始化CropController
            CropController controller = cropObj.GetComponent<CropController>();
            if (controller != null)
            {
                controller.Initialize(cropInstance);
            }
            
            cropInstance.UpdateVisuals();

            // 保存到tile数据
            tileData.crop = cropInstance;

            Debug.Log($"[FarmingManager] 成功种植 {seedData.itemName} 于 {cellPosition}");
            
            // TODO: 从背包移除种子
            // InventoryService.Instance.RemoveItem(seedData.itemID, 1);

            return true;
        }

        private bool CanPlantAtPosition(Vector3Int cellPosition, out FarmTileData tileData)
        {
            tileData = null;

            if (!farmTiles.ContainsKey(cellPosition))
            {
                return false;
            }

            tileData = farmTiles[cellPosition];
            return tileData.CanPlant();
        }

        private bool IsCorrectSeason(SeedData seedData)
        {
            if (timeManager == null) return true;
            
            // 全季节种子可以任何季节种植
            if (seedData.season == Season.AllSeason)
                return true;

            // 比较枚举值（整数）来判断季节是否匹配
            // ItemEnums.Season: Spring=0, Summer=1, Fall=2, Winter=3
            // SeasonManager.Season: Spring=0, Summer=1, Autumn=2, Winter=3
            SeasonManager.Season currentSeason = timeManager.GetSeason();
            return (int)seedData.season == (int)currentSeason;
        }

        #endregion

        #region 浇水系统

        public bool WaterTile(Vector3 worldPosition)
        {
            Vector3Int cellPosition = farmlandTilemap.WorldToCell(worldPosition);
            return WaterTileAtCell(cellPosition);
        }

        public bool WaterTileAtCell(Vector3Int cellPosition)
        {
            if (!farmTiles.ContainsKey(cellPosition))
            {
                Debug.Log($"[FarmingManager] 此位置没有耕地: {cellPosition}");
                return false;
            }

            FarmTileData tileData = farmTiles[cellPosition];
            
            if (!tileData.isTilled)
            {
                Debug.Log($"[FarmingManager] 此位置未耕作: {cellPosition}");
                return false;
            }

            // 今天已经浇过水了
            if (tileData.wateredToday)
            {
                Debug.Log($"[FarmingManager] 今天已经浇过水了: {cellPosition}");
                return false;
            }

            // TODO: 检查精力
            // if (!StaminaSystem.Instance.ConsumeStamina(wateringStaminaCost))
            //     return false;

            // 浇水：记录参数（第二天才生效作物生长）
            tileData.wateredToday = true;
            
            // 记录浇水时间（用于视觉切换）
            if (timeManager != null)
            {
                tileData.waterTime = timeManager.GetHour() + timeManager.GetMinute() / 60f;
            }
            else
            {
                tileData.waterTime = 0f;
            }
            
            // 立即更新视觉：变为水渍状态
            tileData.moistureState = SoilMoistureState.WetWithPuddle;
            UpdateTileVisual(cellPosition, tileData);

            // 播放音效和粒子
            PlayWateringEffects(farmlandTilemap.GetCellCenterWorld(cellPosition));

            Debug.Log($"[FarmingManager] 成功浇水: {cellPosition}（明天生效作物生长）");
            return true;
        }

        private void PlayWateringEffects(Vector3 worldPosition)
        {
            if (wateringSoundClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(wateringSoundClip);
            }

            if (wateringParticlePrefab != null)
            {
                GameObject particles = Instantiate(wateringParticlePrefab, worldPosition, Quaternion.identity);
                Destroy(particles, 2f);
            }
        }

        #endregion

        #region 收获系统

        public bool HarvestCrop(Vector3 worldPosition, out CropData harvestedCrop, out int harvestAmount)
        {
            Vector3Int cellPosition = farmlandTilemap.WorldToCell(worldPosition);
            return HarvestCropAtCell(cellPosition, out harvestedCrop, out harvestAmount);
        }

        public bool HarvestCropAtCell(Vector3Int cellPosition, out CropData harvestedCrop, out int harvestAmount)
        {
            harvestedCrop = null;
            harvestAmount = 0;

            if (!farmTiles.ContainsKey(cellPosition))
            {
                return false;
            }

            FarmTileData tileData = farmTiles[cellPosition];
            if (!tileData.HasCrop())
            {
                return false;
            }

            CropInstance crop = tileData.crop;

            if (crop.isWithered)
            {
                Debug.Log("[FarmingManager] 作物已枯萎，无法收获！");
                return false;
            }

            if (!crop.IsMature())
            {
                Debug.Log("[FarmingManager] 作物还未成熟！");
                return false;
            }

            // TODO: 获取作物数据（ItemDatabase访问需要实现）
            // 临时：直接从SeedData获取收获信息
            harvestedCrop = null;
            harvestAmount = Random.Range(
                crop.seedData.harvestAmountRange.x,
                crop.seedData.harvestAmountRange.y + 1
            );

            // 播放音效
            if (harvestSoundClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(harvestSoundClip);
            }

            // 可重复收获？
            if (crop.seedData.isReHarvestable)
            {
                int reGrowStage = Mathf.Max(1, crop.seedData.growthStageSprites.Length - 3);
                crop.currentStage = reGrowStage;
                crop.harvestCount++;
                crop.lastHarvestDay = timeManager != null ? timeManager.GetDay() : 0;
                crop.UpdateVisuals();

                Debug.Log($"[FarmingManager] 收获作物 x{harvestAmount}（可重复收获）");
            }
            else
            {
                tileData.ClearCrop();
                Debug.Log($"[FarmingManager] 收获作物 x{harvestAmount}（移除作物）");
            }

            // TODO: 添加到背包
            // InventoryService.Instance.AddItem(harvestedCrop.itemID, harvestAmount);

            return true;
        }

        public bool ClearCrop(Vector3 worldPosition)
        {
            Vector3Int cellPosition = farmlandTilemap.WorldToCell(worldPosition);
            return ClearCropAtCell(cellPosition);
        }

        public bool ClearCropAtCell(Vector3Int cellPosition)
        {
            if (!farmTiles.ContainsKey(cellPosition))
            {
                return false;
            }

            FarmTileData tileData = farmTiles[cellPosition];
            if (!tileData.HasCrop())
            {
                return false;
            }

            tileData.ClearCrop();
            Debug.Log($"[FarmingManager] 清除作物: {cellPosition}");
            return true;
        }

        #endregion

        #region Tile视觉更新

        /// <summary>
        /// 根据土壤状态更新Tile显示
        /// </summary>
        private void UpdateTileVisual(Vector3Int cellPosition, FarmTileData tileData)
        {
            if (farmlandTilemap == null) return;

            TileBase targetTile = null;

            switch (tileData.moistureState)
            {
                case SoilMoistureState.Dry:
                    targetTile = dryFarmlandTile;
                    break;

                case SoilMoistureState.WetWithPuddle:
                    // 随机选择一种水渍Tile
                    if (wetWithPuddleTiles != null && wetWithPuddleTiles.Length > 0)
                    {
                        int randomIndex = Random.Range(0, wetWithPuddleTiles.Length);
                        targetTile = wetWithPuddleTiles[randomIndex];
                    }
                    break;

                case SoilMoistureState.WetDark:
                    targetTile = wetDarkTile;
                    break;
            }

            if (targetTile != null)
            {
                farmlandTilemap.SetTile(cellPosition, targetTile);
            }
        }

        #endregion

        #region 查询方法

        public FarmTileData GetFarmTileData(Vector3Int cellPosition)
        {
            farmTiles.TryGetValue(cellPosition, out FarmTileData data);
            return data;
        }

        public CropInstance GetCropAtPosition(Vector3Int cellPosition)
        {
            if (farmTiles.TryGetValue(cellPosition, out FarmTileData data))
            {
                return data.crop;
            }
            return null;
        }

        public Dictionary<Vector3Int, FarmTileData> GetAllFarmTiles()
        {
            return farmTiles;
        }

        #endregion

        #region 调试

        private void OnDrawGizmos()
        {
            if (farmlandTilemap == null) return;

            foreach (var kvp in farmTiles)
            {
                Vector3 worldPos = farmlandTilemap.GetCellCenterWorld(kvp.Key);
                FarmTileData data = kvp.Value;

                if (data.HasCrop())
                {
                    Gizmos.color = data.crop.IsMature() ? Color.green : Color.yellow;
                }
                else if (data.moistureState == SoilMoistureState.WetWithPuddle)
                {
                    Gizmos.color = Color.cyan;
                }
                else if (data.moistureState == SoilMoistureState.WetDark)
                {
                    Gizmos.color = Color.blue;
                }
                else if (data.isTilled)
                {
                    Gizmos.color = new Color(0.6f, 0.4f, 0.2f);
                }
                else
                {
                    continue;
                }

                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.3f);
            }
        }

        #endregion
    }
}
