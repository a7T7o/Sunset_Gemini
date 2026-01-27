using UnityEngine;
using FarmGame.Data;

namespace FarmGame.Farm
{
    /// <summary>
    /// [已废弃] 农田管理器（重构版）
    /// 
    /// ⚠️ 此类已被废弃，不应再使用！
    /// 
    /// 原因：
    /// 1. 违反"去中心化"原则 - 作为"上帝类"集中管理所有子系统
    /// 2. FarmTileManager 现在自己订阅时间事件，处理水渍干涸
    /// 3. CropController 现在自己订阅时间事件，处理作物生长
    /// 4. GameInputManager 直接调用 FarmTileManager 和 CropManager
    /// 
    /// 替代方案：
    /// - 锄地/浇水：直接调用 FarmTileManager.Instance
    /// - 种植：直接调用 CropManager.Instance.CreateCrop()
    /// - 收获：直接调用 CropManager.Instance.TryHarvest()
    /// - 时间事件：各子系统自己订阅 TimeManager 事件
    /// 
    /// 验证标准：删掉场景里的 FarmingManagerNew 物体后，
    /// 整个农田系统（锄地、浇水、干涸、生长、收获）依然能完美运行。
    /// </summary>
    [System.Obsolete("此类已废弃。请直接使用 FarmTileManager 和 CropManager。")]
    public class FarmingManagerNew : MonoBehaviour
    {
        #region 单例
        
        [System.Obsolete("此类已废弃")]
        public static FarmingManagerNew Instance { get; private set; }
        
        #endregion

        #region 子管理器引用
        
        [Header("子管理器")]
        [SerializeField] private FarmTileManager tileManager;
        [SerializeField] private CropManager cropManager;
        [SerializeField] private FarmVisualManager visualManager;
        
        #endregion

        #region 系统引用
        
        [Header("系统引用")]
        [SerializeField] private ItemDatabase itemDatabase;
        
        #endregion

        #region 配置
        
        [Header("视觉切换设置")]
        [Tooltip("浇水多少小时后，水渍消失变为深色")]
        [SerializeField] private float hoursUntilPuddleDry = 2f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        #endregion

        #region 内部变量
        
        private TimeManager timeManager;
        
        #endregion

        #region 生命周期
        
        private void Awake()
        {
            // ⚠️ 此类已废弃，但保留单例以防止旧代码崩溃
            if (Instance == null)
            {
                Instance = this;
                Debug.LogWarning("[FarmingManagerNew] ⚠️ 此类已废弃！请从场景中移除此组件。");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            // ⚠️ 不再订阅时间事件 - FarmTileManager 和 CropController 现在自己处理
            Debug.LogWarning("[FarmingManagerNew] ⚠️ 此类已废弃！时间事件现在由 FarmTileManager 和 CropController 自己处理。");
        }
        
        private void OnDestroy()
        {
            // 不再需要取消订阅
        }
        
        #endregion

        #region [已废弃] 公共接口
        
        /// <summary>
        /// [已废弃] 请直接使用 FarmTileManager.Instance.CreateTile()
        /// </summary>
        [System.Obsolete("请直接使用 FarmTileManager.Instance.CreateTile()")]
        public bool TillSoil(Vector3 worldPosition)
        {
            Debug.LogWarning("[FarmingManagerNew] TillSoil 已废弃，请使用 FarmTileManager.Instance.CreateTile()");
            return false;
        }
        
        /// <summary>
        /// [已废弃] 请直接使用 CropManager.Instance.CreateCrop()
        /// </summary>
        [System.Obsolete("请直接使用 CropManager.Instance.CreateCrop()")]
        public bool PlantSeed(Vector3 worldPosition, int seedDataID)
        {
            Debug.LogWarning("[FarmingManagerNew] PlantSeed 已废弃，请使用 CropManager.Instance.CreateCrop()");
            return false;
        }
        
        /// <summary>
        /// [已废弃] 请直接使用 CropManager.Instance.CreateCrop()
        /// </summary>
        [System.Obsolete("请直接使用 CropManager.Instance.CreateCrop()")]
        public bool PlantSeed(Vector3 worldPosition, SeedData seedData)
        {
            Debug.LogWarning("[FarmingManagerNew] PlantSeed 已废弃，请使用 CropManager.Instance.CreateCrop()");
            return false;
        }
        
        /// <summary>
        /// [已废弃] 请直接使用 FarmTileManager.Instance.SetWatered()
        /// </summary>
        [System.Obsolete("请直接使用 FarmTileManager.Instance.SetWatered()")]
        public bool WaterTile(Vector3 worldPosition)
        {
            Debug.LogWarning("[FarmingManagerNew] WaterTile 已废弃，请使用 FarmTileManager.Instance.SetWatered()");
            return false;
        }
        
        /// <summary>
        /// [已废弃] 请直接使用 CropManager.Instance.TryHarvest()
        /// </summary>
        [System.Obsolete("请直接使用 CropManager.Instance.TryHarvest()")]
        public bool HarvestCrop(Vector3 worldPosition, out int cropID, out int amount)
        {
            cropID = 0;
            amount = 0;
            Debug.LogWarning("[FarmingManagerNew] HarvestCrop 已废弃，请使用 CropManager.Instance.TryHarvest()");
            return false;
        }
        
        /// <summary>
        /// [已废弃] 请直接使用 FarmTileManager.Instance.GetTileData()
        /// </summary>
        [System.Obsolete("请直接使用 FarmTileManager.Instance.GetTileData()")]
        public FarmTileData GetFarmTileData(Vector3 worldPosition)
        {
            Debug.LogWarning("[FarmingManagerNew] GetFarmTileData 已废弃，请使用 FarmTileManager.Instance.GetTileData()");
            return null;
        }
        
        /// <summary>
        /// [已废弃] 请直接使用 FarmTileManager.Instance.CanTillAt()
        /// </summary>
        [System.Obsolete("请直接使用 FarmTileManager.Instance.CanTillAt()")]
        public bool CanTillAt(Vector3 worldPosition)
        {
            Debug.LogWarning("[FarmingManagerNew] CanTillAt 已废弃，请使用 FarmTileManager.Instance.CanTillAt()");
            return false;
        }
        
        /// <summary>
        /// [已废弃] 请直接检查 FarmTileData.CanPlant()
        /// </summary>
        [System.Obsolete("请直接检查 FarmTileData.CanPlant()")]
        public bool CanPlantAt(Vector3 worldPosition)
        {
            Debug.LogWarning("[FarmingManagerNew] CanPlantAt 已废弃，请直接检查 FarmTileData.CanPlant()");
            return false;
        }
        
        #endregion
    }
}
