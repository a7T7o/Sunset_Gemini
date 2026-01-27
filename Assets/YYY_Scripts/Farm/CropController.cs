using UnityEngine;
using FarmGame.Data;

namespace FarmGame.Farm
{
    /// <summary>
    /// 作物控制器（自治版）
    /// 附加到单个作物 GameObject 上，负责作物的渲染和交互
    /// 自己订阅时间事件，自己检查脚下土地是否湿润，自己决定是否生长
    /// 不依赖 FarmingManagerNew 或 CropManager 的遍历
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CropController : MonoBehaviour
    {
        #region 组件引用
        
        [Header("组件引用")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        #endregion

        #region 成熟闪烁配置
        
        [Header("成熟特效")]
        [SerializeField] private bool enableMatureGlow = true;
        [SerializeField] private float glowSpeed = 2f;
        [SerializeField] private Color glowColor = new Color(1f, 1f, 0.8f, 1f);
        
        #endregion
        
        #region 生长规则配置
        
        [Header("生长规则")]
        [Tooltip("连续多少天未浇水后作物枯萎")]
        [SerializeField] private int daysUntilWithered = 3;
        
        [Tooltip("连续多少天未浇水后生长停滞")]
        [SerializeField] private int daysUntilStagnant = 2;
        
        #endregion

        #region 位置信息（用于查找脚下的耕地）
        
        /// <summary>
        /// 所在楼层索引
        /// </summary>
        private int layerIndex;
        
        /// <summary>
        /// 所在格子坐标
        /// </summary>
        private Vector3Int cellPosition;
        
        #endregion

        #region 运行时数据
        
        /// <summary>
        /// 种子数据引用
        /// </summary>
        private SeedData seedData;
        
        /// <summary>
        /// 作物实例数据
        /// </summary>
        private CropInstanceData instanceData;
        
        /// <summary>
        /// 是否成熟
        /// </summary>
        private bool isMature = false;
        
        /// <summary>
        /// 闪烁计时器
        /// </summary>
        private float glowTime = 0f;
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool isInitialized = false;
        
        #endregion

        #region 常量
        
        /// <summary>
        /// 统一枯萎颜色
        /// </summary>
        private static readonly Color WitheredColor = new Color(0.8f, 0.7f, 0.4f, 1f);
        
        #endregion

        #region 兼容旧版
        
        /// <summary>
        /// [已废弃] 旧版作物实例引用
        /// </summary>
        [System.Obsolete("使用 Initialize(SeedData, CropInstanceData) 替代")]
        private CropInstance cropInstance;
        
        #endregion

        #region 生命周期
        
        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
        
        private void OnEnable()
        {
            // 订阅时间事件（自治）
            TimeManager.OnDayChanged += OnDayChanged;
        }
        
        private void OnDisable()
        {
            // 取消订阅（防止内存泄漏）
            TimeManager.OnDayChanged -= OnDayChanged;
        }
        
        private void Update()
        {
            // 成熟作物的闪烁效果
            if (isMature && enableMatureGlow && !IsWithered())
            {
                glowTime += Time.deltaTime * glowSpeed;
                float glow = Mathf.PingPong(glowTime, 1f);
                spriteRenderer.color = Color.Lerp(Color.white, glowColor, glow * 0.3f);
            }
        }
        
        #endregion
        
        #region 时间事件处理（自治）
        
        /// <summary>
        /// 每天开始时触发：自己检查脚下土地，自己决定是否生长
        /// </summary>
        private void OnDayChanged(int year, int day, int totalDays)
        {
            if (!isInitialized) return;
            if (instanceData == null) return;
            if (instanceData.isWithered) return; // 已枯萎的作物不处理
            
            // 获取脚下的耕地数据
            var farmTileManager = FarmTileManager.Instance;
            if (farmTileManager == null) return;
            
            var tileData = farmTileManager.GetTileData(layerIndex, cellPosition);
            if (tileData == null) return;
            
            // 检查昨天是否浇水
            if (tileData.wateredYesterday)
            {
                // 浇水了：生长
                instanceData.grownDays++;
                instanceData.daysWithoutWater = 0;
                
                // 更新生长阶段
                UpdateGrowthStage();
                UpdateVisuals();
            }
            else
            {
                // 未浇水：增加未浇水天数
                instanceData.daysWithoutWater++;
                
                // 检查是否枯萎
                if (instanceData.daysWithoutWater >= daysUntilWithered)
                {
                    instanceData.isWithered = true;
                    UpdateVisuals();
                }
                // 生长停滞（不增加生长天数）
            }
        }
        
        /// <summary>
        /// 更新生长阶段（基于生长天数）
        /// </summary>
        private void UpdateGrowthStage()
        {
            if (seedData == null || instanceData == null) return;
            
            int totalStages = seedData.growthStageSprites?.Length ?? 1;
            int growthDays = seedData.growthDays;
            
            if (growthDays > 0 && totalStages > 1)
            {
                // 线性分布：每个阶段需要的天数
                float daysPerStage = (float)growthDays / (totalStages - 1);
                int newStage = Mathf.FloorToInt(instanceData.grownDays / daysPerStage);
                instanceData.currentStage = Mathf.Clamp(newStage, 0, totalStages - 1);
            }
        }
        
        #endregion

        #region 初始化
        
        /// <summary>
        /// 初始化作物（新版）
        /// </summary>
        /// <param name="seed">种子数据</param>
        /// <param name="data">作物实例数据</param>
        public void Initialize(SeedData seed, CropInstanceData data)
        {
            seedData = seed;
            instanceData = data;
            
            // 从世界坐标计算格子坐标
            var farmTileManager = FarmTileManager.Instance;
            if (farmTileManager != null)
            {
                layerIndex = farmTileManager.GetCurrentLayerIndex(transform.position);
                var tilemaps = farmTileManager.GetLayerTilemaps(layerIndex);
                if (tilemaps != null)
                {
                    cellPosition = tilemaps.WorldToCell(transform.position);
                }
            }
            
            isInitialized = true;
            UpdateVisuals();
        }
        
        /// <summary>
        /// 初始化作物（带位置信息）
        /// </summary>
        /// <param name="seed">种子数据</param>
        /// <param name="data">作物实例数据</param>
        /// <param name="layer">楼层索引</param>
        /// <param name="cell">格子坐标</param>
        public void Initialize(SeedData seed, CropInstanceData data, int layer, Vector3Int cell)
        {
            seedData = seed;
            instanceData = data;
            layerIndex = layer;
            cellPosition = cell;
            
            isInitialized = true;
            UpdateVisuals();
        }
        
        /// <summary>
        /// [已废弃] 初始化作物（旧版兼容）
        /// </summary>
        [System.Obsolete("使用 Initialize(SeedData, CropInstanceData) 替代")]
        public void Initialize(CropInstance instance)
        {
            #pragma warning disable 0618
            cropInstance = instance;
            #pragma warning restore 0618
            
            // 尝试转换为新版数据
            if (instance != null && instance.seedData != null)
            {
                seedData = instance.seedData;
                instanceData = new CropInstanceData(instance.seedData.itemID, instance.plantedDay);
                instanceData.currentStage = instance.currentStage;
                instanceData.grownDays = instance.grownDays;
                instanceData.daysWithoutWater = instance.daysWithoutWater;
                instanceData.isWithered = instance.isWithered;
                instanceData.harvestCount = instance.harvestCount;
                instanceData.lastHarvestDay = instance.lastHarvestDay;
                instanceData.quality = (int)instance.quality;
            }
            
            // 从世界坐标计算格子坐标
            var farmTileManager = FarmTileManager.Instance;
            if (farmTileManager != null)
            {
                layerIndex = farmTileManager.GetCurrentLayerIndex(transform.position);
                var tilemaps = farmTileManager.GetLayerTilemaps(layerIndex);
                if (tilemaps != null)
                {
                    cellPosition = tilemaps.WorldToCell(transform.position);
                }
            }
            
            isInitialized = true;
            UpdateVisuals();
        }
        
        #endregion

        #region 视觉更新
        
        /// <summary>
        /// 更新作物外观
        /// </summary>
        public void UpdateVisuals()
        {
            if (spriteRenderer == null) return;
            
            // 更新 Sprite
            Sprite sprite = GetCurrentSprite();
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
            
            // 更新颜色
            if (IsWithered())
            {
                // 枯萎：统一枯萎颜色
                spriteRenderer.color = WitheredColor;
                isMature = false;
            }
            else
            {
                // 正常颜色
                spriteRenderer.color = Color.white;
                
                // 检查是否成熟
                isMature = IsMature();
            }
        }
        
        /// <summary>
        /// 获取当前阶段的 Sprite
        /// </summary>
        private Sprite GetCurrentSprite()
        {
            if (seedData == null || seedData.growthStageSprites == null || seedData.growthStageSprites.Length == 0)
            {
                return null;
            }
            
            int stage = instanceData?.currentStage ?? 0;
            int index = Mathf.Clamp(stage, 0, seedData.growthStageSprites.Length - 1);
            return seedData.growthStageSprites[index];
        }
        
        #endregion

        #region 状态查询
        
        /// <summary>
        /// 是否成熟
        /// </summary>
        public bool IsMature()
        {
            if (instanceData == null || seedData == null) return false;
            if (instanceData.isWithered) return false;
            
            // 检查是否达到最后阶段
            if (seedData.growthStageSprites == null || seedData.growthStageSprites.Length == 0)
                return false;
            
            return instanceData.currentStage >= seedData.growthStageSprites.Length - 1;
        }
        
        /// <summary>
        /// 是否枯萎
        /// </summary>
        public bool IsWithered()
        {
            return instanceData?.isWithered ?? false;
        }
        
        /// <summary>
        /// 获取当前生长阶段
        /// </summary>
        public int GetCurrentStage()
        {
            return instanceData?.currentStage ?? 0;
        }
        
        /// <summary>
        /// 获取总生长阶段数
        /// </summary>
        public int GetTotalStages()
        {
            return seedData?.growthStageSprites?.Length ?? 0;
        }
        
        /// <summary>
        /// 获取生长进度（0-1）
        /// </summary>
        public float GetGrowthProgress()
        {
            int totalStages = GetTotalStages();
            if (totalStages <= 1) return 1f;
            
            return (float)GetCurrentStage() / (totalStages - 1);
        }
        
        #endregion

        #region 生长操作
        
        /// <summary>
        /// 生长（增加生长天数，更新阶段）
        /// </summary>
        public void Grow()
        {
            if (instanceData == null || seedData == null) return;
            if (instanceData.isWithered) return;
            
            instanceData.grownDays++;
            
            // 计算新阶段（线性分布）
            int totalStages = seedData.growthStageSprites?.Length ?? 1;
            int growthDays = seedData.growthDays;
            
            if (growthDays > 0 && totalStages > 1)
            {
                // 线性分布：每个阶段需要的天数
                float daysPerStage = (float)growthDays / (totalStages - 1);
                int newStage = Mathf.FloorToInt(instanceData.grownDays / daysPerStage);
                instanceData.currentStage = Mathf.Clamp(newStage, 0, totalStages - 1);
            }
            
            UpdateVisuals();
        }
        
        /// <summary>
        /// 设置枯萎状态
        /// </summary>
        public void SetWithered()
        {
            if (instanceData == null) return;
            
            instanceData.isWithered = true;
            UpdateVisuals();
        }
        
        /// <summary>
        /// 重置为可重复收获状态
        /// </summary>
        /// <param name="reGrowStage">重置到的阶段</param>
        public void ResetForReHarvest(int reGrowStage)
        {
            if (instanceData == null) return;
            
            instanceData.currentStage = Mathf.Max(0, reGrowStage);
            
            // 重新计算生长天数
            if (seedData != null)
            {
                int totalStages = seedData.growthStageSprites?.Length ?? 1;
                int growthDays = seedData.growthDays;
                
                if (growthDays > 0 && totalStages > 1)
                {
                    float daysPerStage = (float)growthDays / (totalStages - 1);
                    instanceData.grownDays = Mathf.FloorToInt(reGrowStage * daysPerStage);
                }
            }
            
            UpdateVisuals();
        }
        
        #endregion

        #region 交互
        
        private void OnMouseOver()
        {
            // TODO: 显示作物信息 UI
            // 例如：作物名称、生长进度、是否可收获等
        }
        
        private void OnMouseExit()
        {
            // TODO: 隐藏作物信息 UI
        }
        
        #endregion

        #region 数据访问
        
        /// <summary>
        /// 获取种子数据
        /// </summary>
        public SeedData GetSeedData() => seedData;
        
        /// <summary>
        /// 获取作物实例数据
        /// </summary>
        public CropInstanceData GetInstanceData() => instanceData;
        
        #endregion
    }
}
