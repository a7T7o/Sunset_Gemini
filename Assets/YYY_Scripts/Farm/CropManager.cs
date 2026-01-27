using UnityEngine;
using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.Farm
{
    /// <summary>
    /// 作物工厂管理器（自治版）
    /// 只负责作物的创建和销毁，不负责生长逻辑
    /// 生长逻辑由 CropController 自己处理（订阅时间事件）
    /// </summary>
    public class CropManager : MonoBehaviour
    {
        #region 单例
        
        public static CropManager Instance { get; private set; }
        
        #endregion

        #region 配置
        
        [Header("作物配置")]
        [SerializeField] private GameObject cropPrefab;
        [SerializeField] private Transform defaultCropsContainer;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        #endregion

        #region 数据存储
        
        /// <summary>
        /// 运行时作物实例（仅用于查找和销毁，不用于遍历生长）
        /// Key: (layerIndex, cellPosition), Value: CropController
        /// </summary>
        private Dictionary<(int layer, Vector3Int pos), CropController> activeCrops;
        
        #endregion

        #region 生命周期
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                activeCrops = new Dictionary<(int, Vector3Int), CropController>();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // 确保有默认容器
            if (defaultCropsContainer == null)
            {
                GameObject container = new GameObject("Crops");
                defaultCropsContainer = container.transform;
                defaultCropsContainer.SetParent(transform);
            }
        }
        
        #endregion

        #region 作物创建/销毁（工厂功能）
        
        /// <summary>
        /// 创建作物（工厂方法）
        /// 只负责实例化 GameObject 并初始化数据，不维护全局列表进行遍历
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <param name="cellPosition">格子坐标</param>
        /// <param name="seedData">种子数据</param>
        /// <param name="plantedDay">种植天数</param>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="container">作物容器（可选）</param>
        /// <returns>作物控制器</returns>
        public CropController CreateCrop(int layerIndex, Vector3Int cellPosition, SeedData seedData, 
            int plantedDay, Vector3 worldPosition, Transform container = null)
        {
            if (seedData == null)
            {
                Debug.LogError("[CropManager] CreateCrop 失败: seedData 为 null");
                return null;
            }
            
            if (cropPrefab == null)
            {
                Debug.LogError("[CropManager] CreateCrop 失败: cropPrefab 未设置");
                return null;
            }
            
            // 检查是否已存在作物
            var key = (layerIndex, cellPosition);
            if (activeCrops.ContainsKey(key))
            {
                if (showDebugInfo)
                    Debug.LogWarning($"[CropManager] 该位置已有作物: Layer={layerIndex}, Pos={cellPosition}");
                return null;
            }
            
            // 确定容器
            Transform targetContainer = container ?? defaultCropsContainer;
            
            // 创建作物 GameObject
            GameObject cropObj = Instantiate(cropPrefab, worldPosition, Quaternion.identity, targetContainer);
            cropObj.name = $"Crop_{seedData.itemName}_{cellPosition}";
            
            // 获取或添加 CropController
            CropController controller = cropObj.GetComponent<CropController>();
            if (controller == null)
            {
                controller = cropObj.AddComponent<CropController>();
            }
            
            // 创建作物实例数据
            CropInstanceData instanceData = new CropInstanceData(seedData.itemID, plantedDay);
            
            // 初始化控制器（带位置信息，让 CropController 知道自己在哪）
            controller.Initialize(seedData, instanceData, layerIndex, cellPosition);
            
            // 注册到活动作物字典（仅用于查找和销毁）
            activeCrops[key] = controller;
            
            if (showDebugInfo)
                Debug.Log($"[CropManager] 创建作物: {seedData.itemName}, Layer={layerIndex}, Pos={cellPosition}");
            
            return controller;
        }
        
        /// <summary>
        /// 销毁作物
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <param name="cellPosition">格子坐标</param>
        /// <returns>是否销毁成功</returns>
        public bool DestroyCrop(int layerIndex, Vector3Int cellPosition)
        {
            var key = (layerIndex, cellPosition);
            
            if (!activeCrops.TryGetValue(key, out CropController controller))
            {
                return false;
            }
            
            // 从字典中移除
            activeCrops.Remove(key);
            
            // 销毁 GameObject
            if (controller != null && controller.gameObject != null)
            {
                Destroy(controller.gameObject);
            }
            
            if (showDebugInfo)
                Debug.Log($"[CropManager] 销毁作物: Layer={layerIndex}, Pos={cellPosition}");
            
            return true;
        }
        
        /// <summary>
        /// 获取指定位置的作物控制器
        /// </summary>
        public CropController GetCrop(int layerIndex, Vector3Int cellPosition)
        {
            var key = (layerIndex, cellPosition);
            activeCrops.TryGetValue(key, out CropController controller);
            return controller;
        }
        
        #endregion

        #region [已废弃] 每日生长逻辑 - 现在由 CropController 自己处理
        
        /// <summary>
        /// [已废弃] 处理单个耕地的每日生长
        /// 现在由 CropController 自己订阅 TimeManager.OnDayChanged 处理
        /// </summary>
        [System.Obsolete("生长逻辑已移至 CropController.OnDayChanged()")]
        public void ProcessDailyGrowth(FarmTileData tileData)
        {
            Debug.LogWarning("[CropManager] ProcessDailyGrowth 已废弃，生长逻辑由 CropController 自己处理");
        }
        
        /// <summary>
        /// [已废弃] 处理所有作物的每日生长
        /// 现在由各个 CropController 自己订阅 TimeManager.OnDayChanged 处理
        /// </summary>
        [System.Obsolete("生长逻辑已移至 CropController.OnDayChanged()")]
        public void ProcessAllCropsGrowth()
        {
            Debug.LogWarning("[CropManager] ProcessAllCropsGrowth 已废弃，生长逻辑由各个 CropController 自己处理");
        }
        
        #endregion

        #region 收获逻辑
        
        /// <summary>
        /// 尝试收获作物
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <param name="cellPosition">格子坐标</param>
        /// <param name="tileData">耕地数据</param>
        /// <param name="seedData">种子数据</param>
        /// <param name="cropID">输出：收获的作物 ID</param>
        /// <param name="amount">输出：收获数量</param>
        /// <returns>是否收获成功</returns>
        public bool TryHarvest(int layerIndex, Vector3Int cellPosition, FarmTileData tileData, 
            SeedData seedData, out int cropID, out int amount)
        {
            cropID = 0;
            amount = 0;
            
            if (tileData == null || !tileData.HasCrop())
            {
                return false;
            }
            
            CropInstanceData cropData = tileData.cropData;
            
            // 检查是否枯萎
            if (cropData.isWithered)
            {
                if (showDebugInfo)
                    Debug.Log("[CropManager] 作物已枯萎，无法收获");
                return false;
            }
            
            // 获取控制器检查是否成熟
            var controller = GetCrop(layerIndex, cellPosition);
            if (controller == null || !controller.IsMature())
            {
                if (showDebugInfo)
                    Debug.Log("[CropManager] 作物未成熟，无法收获");
                return false;
            }
            
            // 计算收获数量
            if (seedData != null)
            {
                cropID = seedData.harvestCropID;
                amount = Random.Range(seedData.harvestAmountRange.x, seedData.harvestAmountRange.y + 1);
            }
            else
            {
                amount = 1;
            }
            
            // 处理可重复收获
            if (seedData != null && seedData.isReHarvestable)
            {
                // 检查最大收获次数
                if (seedData.maxHarvestCount > 0 && cropData.harvestCount >= seedData.maxHarvestCount)
                {
                    // 达到最大收获次数，销毁作物
                    DestroyCrop(layerIndex, cellPosition);
                    tileData.ClearCropData();
                    
                    if (showDebugInfo)
                        Debug.Log($"[CropManager] 作物达到最大收获次数，已移除");
                }
                else
                {
                    // 重置到指定阶段
                    int reGrowStage = Mathf.Max(1, seedData.growthStageSprites.Length - 3);
                    controller.ResetForReHarvest(reGrowStage);
                    
                    cropData.harvestCount++;
                    cropData.lastHarvestDay = TimeManager.Instance?.GetTotalDaysPassed() ?? 0;
                    
                    if (showDebugInfo)
                        Debug.Log($"[CropManager] 可重复收获作物，重置到阶段 {reGrowStage}，已收获 {cropData.harvestCount} 次");
                }
            }
            else
            {
                // 普通作物：销毁
                DestroyCrop(layerIndex, cellPosition);
                tileData.ClearCropData();
                
                if (showDebugInfo)
                    Debug.Log($"[CropManager] 收获作物，已移除");
            }
            
            return true;
        }
        
        #endregion

        #region 查询方法
        
        /// <summary>
        /// 获取所有活动作物数量
        /// </summary>
        public int GetActiveCropCount()
        {
            return activeCrops.Count;
        }
        
        /// <summary>
        /// 获取所有活动作物
        /// </summary>
        public IEnumerable<CropController> GetAllActiveCrops()
        {
            return activeCrops.Values;
        }
        
        #endregion
    }
}
