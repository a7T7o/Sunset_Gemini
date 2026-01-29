using UnityEngine;
using System.Collections.Generic;
using FarmGame.Combat;
using FarmGame.Data;
using FarmGame.Events;
using FarmGame.Utils;

/// <summary>
/// 树木控制器 - 6阶段成长系统
/// 
/// 核心特性：
/// - 6阶段成长（0-5）
/// - 每个阶段有独立的配置（StageConfig）
/// - 每个阶段有独立的Sprite数据（StageSpriteData）
/// - 阶段0只能用锄头挖出，阶段1-5用斧头砍
/// - 阶段3-5有独立的树桩
/// 
/// GameObject结构：
/// Tree_M1_00 (父物体) ← 位置 = 树根 = 种植点
/// ├─ Tree (本脚本所在，SpriteRenderer) ← sprite底部对齐父物体中心
/// └─ Shadow (同级兄弟，SpriteRenderer) ← 中心对齐父物体中心
/// </summary>
public class TreeController : MonoBehaviour, IResourceNode
{
    #region 常量
    private const int STAGE_COUNT = 6;
    private const int STAGE_SAPLING = 0;
    private const int STAGE_MAX = 5;
    #endregion
    
    #region 序列化字段 - 阶段配置
    [Header("━━━━ 6阶段配置 ━━━━")]
    [Tooltip("6个阶段的配置（成长天数、血量、掉落表等）")]
    [SerializeField] private StageConfig[] stageConfigs = new StageConfig[]
    {
        // 阶段0：树苗
        new StageConfig { daysToNextStage = 1, health = 0, hasStump = false, stumpHealth = 0, enableCollider = false, enableOcclusion = false, acceptedToolType = ToolType.Hoe },
        // 阶段1：小树苗
        new StageConfig { daysToNextStage = 2, health = 4, hasStump = false, stumpHealth = 0, enableCollider = true, enableOcclusion = true, acceptedToolType = ToolType.Axe },
        // 阶段2：中等树
        new StageConfig { daysToNextStage = 2, health = 9, hasStump = false, stumpHealth = 4, enableCollider = true, enableOcclusion = true, acceptedToolType = ToolType.Axe },
        // 阶段3：大树
        new StageConfig { daysToNextStage = 4, health = 17, hasStump = true, stumpHealth = 9, enableCollider = true, enableOcclusion = true, acceptedToolType = ToolType.Axe },
        // 阶段4：成熟树
        new StageConfig { daysToNextStage = 5, health = 28, hasStump = true, stumpHealth = 12, enableCollider = true, enableOcclusion = true, acceptedToolType = ToolType.Axe },
        // 阶段5：完全成熟
        new StageConfig { daysToNextStage = 0, health = 40, hasStump = true, stumpHealth = 16, enableCollider = true, enableOcclusion = true, acceptedToolType = ToolType.Axe }
    };
    
    [Header("━━━━ 6阶段Sprite数据 ━━━━")]
    [Tooltip("6个阶段的Sprite配置")]
    [SerializeField] private TreeSpriteConfig spriteConfig;
    #endregion
    
    #region 序列化字段 - 当前状态
    [Header("━━━━ 当前状态 ━━━━")]
    [Tooltip("树木ID（基于InstanceID，0-9999循环）")]
    [SerializeField] private int treeID = -1;
    
    [Tooltip("当前阶段索引（0-5）")]
    [Range(0, 5)]
    [SerializeField] private int currentStageIndex = 0;
    
    [Tooltip("当前树的状态")]
    [SerializeField] private TreeState currentState = TreeState.Normal;
    
    [Tooltip("当前日历季节（只读，由SeasonManager控制）")]
    [SerializeField] private SeasonManager.Season currentSeason = SeasonManager.Season.Spring;
    #endregion
    
    #region 序列化字段 - 成长设置
    [Header("━━━━ 成长设置 ━━━━")]
    [Tooltip("是否启用自动成长（基于天数）")]
    [SerializeField] private bool autoGrow = true;
    
    [Tooltip("种植日期（游戏开始后的第几天，0=未种植）")]
    [SerializeField] private int plantedDay = 0;
    
    [Tooltip("当前阶段已经过的天数")]
    [SerializeField] private int daysInCurrentStage = 0;
    
    [Header("成长空间检测")]
    [Tooltip("是否启用成长空间检测（检测周围是否有足够空间成长）")]
    [SerializeField] private bool enableGrowthSpaceCheck = true;
    
    [Tooltip("阻挡成长的物体标签（多选）")]
    [SerializeField] private string[] growthObstacleTags = new string[] { "Tree", "Rock", "Building" };
    
    [Tooltip("成长受阻时是否显示调试信息")]
    [SerializeField] private bool showGrowthBlockedInfo = true;
    #endregion
    
    #region 序列化字段 - 血量
    [Header("━━━━ 血量状态 ━━━━")]
    [Tooltip("当前血量")]
    [SerializeField] private int currentHealth = 0;
    
    [Tooltip("树桩当前血量（仅树桩状态有效）")]
    [SerializeField] private int currentStumpHealth = 0;
    #endregion
    
    #region 序列化字段 - 影子
    [Header("━━━━ 影子设置 ━━━━")]
    [Tooltip("阶段1-5的影子配置（5个元素，阶段0无影子）")]
    [SerializeField] private ShadowConfig[] shadowConfigs = new ShadowConfig[]
    {
        new ShadowConfig { sprite = null, scale = 0f },    // 阶段1（无影子）
        new ShadowConfig { sprite = null, scale = 0.6f },  // 阶段2
        new ShadowConfig { sprite = null, scale = 0.8f },  // 阶段3
        new ShadowConfig { sprite = null, scale = 0.9f },  // 阶段4
        new ShadowConfig { sprite = null, scale = 1.0f }   // 阶段5
    };
    #endregion
    
    #region 序列化字段 - Sprite对齐
    [Header("━━━━ Sprite底部对齐 ━━━━")]
    [Tooltip("是否自动对齐Sprite底部到父物体位置（种植点）")]
    [SerializeField] private bool alignSpriteBottom = true;
    #endregion
    
    #region 序列化字段 - 倒下动画
    [Header("━━━━ 倒下动画 ━━━━")]
    [Tooltip("是否启用倒下动画")]
    [SerializeField] private bool enableFallAnimation = true;
    
    [Tooltip("倒下动画时长（秒）")]
    [Range(0.5f, 2f)]
    [SerializeField] private float fallDuration = 0.8f;
    
    [Header("向上倒参数")]
    [Range(1f, 3f)]
    [SerializeField] private float fallUpMaxStretch = 1.2f;
    
    [Range(0.01f, 2f)]
    [SerializeField] private float fallUpMinScale = 1f;
    
    [Range(0.1f, 0.9f)]
    [SerializeField] private float fallUpStretchPhase = 0.4f;
    #endregion
    
    #region 序列化字段 - 音效
    [Header("━━━━ 音效设置 ━━━━")]
    [Tooltip("砍击音效（每次命中播放）")]
    [SerializeField] private AudioClip chopHitSound;
    
    [Tooltip("砍倒音效（树木倒下时播放）")]
    [SerializeField] private AudioClip chopFellSound;
    
    [Tooltip("挖出音效（锄头挖出树苗时播放）")]
    [SerializeField] private AudioClip digOutSound;
    
    [Tooltip("斧头等级不足音效（金属碰撞）")]
    [SerializeField] private AudioClip tierInsufficientSound;
    
    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.8f;
    #endregion
    
    #region 序列化字段 - 经验配置
    [Header("━━━━ 砍树经验 ━━━━")]
    [Tooltip("各阶段砍伐经验（阶段0-5）")]
    [SerializeField] private int[] stageExperience = new int[] { 0, 0, 2, 4, 6, 20 };
    #endregion
    
    #region 序列化字段 - 掉落配置
    [Header("━━━━ 掉落配置 ━━━━")]
    [Tooltip("掉落的物品 SO（如木头）")]
    [SerializeField] private ItemData dropItemData;
    
    [Tooltip("各阶段掉落数量（阶段0-5）")]
    [SerializeField] private int[] stageDropAmounts = new int[] { 0, 1, 2, 3, 5, 8 };
    
    [Tooltip("树桩掉落数量（阶段0-5，只有阶段3-5有效）")]
    [SerializeField] private int[] stumpDropAmounts = new int[] { 0, 0, 0, 1, 2, 3 };
    
    [Tooltip("掉落物分散半径")]
    [Range(0.1f, 1f)]
    [SerializeField] private float dropSpreadRadius = 0.4f;
    #endregion
    
    #region 序列化字段 - 调试
    [Header("━━━━ 调试 ━━━━")]
    [SerializeField] private bool showDebugInfo = false;
    
    [Tooltip("编辑器实时预览")]
    [SerializeField] private bool editorPreview = true;
    #endregion
    
    // ★ enableSeasonEvents 已移除：调试开关已移至 TimeManager 集中管理
    // 树木始终订阅所有事件，由 TimeManager 的事件发布开关控制是否触发
    
    #region 私有字段
    private SpriteRenderer spriteRenderer;
    private OcclusionTransparency occlusionTransparency;
    private int lastCheckDay = -1;
    private bool isWeatherWithered = false;
    // ★ isFrozenSapling 已移除：树苗在冬季直接死亡，不再需要冰封状态
    
    // 影子缓存
    private Transform _shadowTransform;
    private SpriteRenderer _shadowRenderer;
    private Sprite _originalShadowSprite;
    
    // 记录最后一次命中时玩家的朝向
    private int lastHitPlayerDirection = 0;
    private bool lastHitPlayerFlipX = false;
    
    #if UNITY_EDITOR
    private int lastEditorStageIndex;
    private TreeState lastEditorState;
    #endif
    #endregion
    
    #region 属性
    /// <summary>
    /// 当前阶段配置
    /// </summary>
    public StageConfig CurrentStageConfig => GetStageConfig(currentStageIndex);
    
    /// <summary>
    /// 当前阶段Sprite数据
    /// </summary>
    public StageSpriteData CurrentSpriteData => spriteConfig?.GetStageData(currentStageIndex);
    #endregion

    
    #region Unity生命周期
    private void Awake()
    {
        // 初始化阶段配置（如果为空则使用默认配置）
        if (stageConfigs == null || stageConfigs.Length != STAGE_COUNT)
        {
            stageConfigs = StageConfigFactory.CreateDefaultConfigs();
        }
    }
    
    private void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"[TreeController] {gameObject.name} 缺少SpriteRenderer组件！");
            enabled = false;
            return;
        }
        
        // 缓存组件引用
        occlusionTransparency = GetComponent<OcclusionTransparency>();
        
        // 缓存影子引用
        InitializeShadowCache();
        
        // 生成树木ID
        treeID = Mathf.Abs(gameObject.GetInstanceID()) % 10000;
        
        #if UNITY_EDITOR
        lastEditorStageIndex = currentStageIndex;
        lastEditorState = currentState;
        #endif
        
        // ★ 始终订阅所有事件（调试开关已移至 TimeManager 集中管理）
        // 订阅季节事件
        SeasonManager.OnSeasonChanged += OnSeasonChanged;
        SeasonManager.OnVegetationSeasonChanged += OnVegetationSeasonChanged;
        
        // 订阅天气事件
        WeatherSystem.OnPlantsWither += OnWeatherWither;
        WeatherSystem.OnPlantsRecover += OnWeatherRecover;
        WeatherSystem.OnWinterSnow += OnWinterSnow;
        WeatherSystem.OnWinterMelt += OnWinterMelt;
        
        // 同步当前季节
        if (SeasonManager.Instance != null)
        {
            currentSeason = SeasonManager.Instance.GetCurrentSeason();
        }
        
        // 初始检查天气
        if (WeatherSystem.Instance != null && WeatherSystem.Instance.IsWithering())
        {
            OnWeatherWither();
        }
        
        if (showDebugInfo)
            Debug.Log($"<color=lime>[TreeController] {gameObject.name} 季节/天气事件已订阅</color>");
        
        // 订阅每日成长事件
        if (autoGrow)
        {
            TimeManager.OnDayChanged += OnDayChanged;
            
            if (plantedDay == 0 && TimeManager.Instance != null)
            {
                plantedDay = TimeManager.Instance.GetTotalDaysPassed();
            }
        }
        
        // 初始化血量
        InitializeHealth();
        
        // 初始化显示
        StartCoroutine(WaitForSeasonManagerAndInitialize());
        
        // 注册到资源节点注册表
        if (ResourceNodeRegistry.Instance != null)
        {
            ResourceNodeRegistry.Instance.Register(this, gameObject.GetInstanceID());
        }
    }
    
    private void OnDestroy()
    {
        // ★ 始终取消订阅所有事件
        SeasonManager.OnSeasonChanged -= OnSeasonChanged;
        SeasonManager.OnVegetationSeasonChanged -= OnVegetationSeasonChanged;
        TimeManager.OnDayChanged -= OnDayChanged;
        WeatherSystem.OnPlantsWither -= OnWeatherWither;
        WeatherSystem.OnPlantsRecover -= OnWeatherRecover;
        WeatherSystem.OnWinterSnow -= OnWinterSnow;
        WeatherSystem.OnWinterMelt -= OnWinterMelt;
        
        if (ResourceNodeRegistry.Instance != null)
        {
            ResourceNodeRegistry.Instance.Unregister(gameObject.GetInstanceID());
        }
    }
    #endregion
    
    #region 初始化
    private System.Collections.IEnumerator WaitForSeasonManagerAndInitialize()
    {
        int retryCount = 0;
        while (SeasonManager.Instance == null && retryCount < 100)
        {
            retryCount++;
            yield return null;
        }

        if (SeasonManager.Instance == null)
        {
            Debug.LogError($"[TreeController] {gameObject.name} - SeasonManager初始化超时");
            yield break;
        }

        InitializeDisplay();
    }
    
    private void InitializeDisplay()
    {
        if (SeasonManager.Instance == null) return;
        
        currentSeason = SeasonManager.Instance.GetCurrentSeason();
        UpdateSprite();
    }
    
    /// <summary>
    /// 初始化血量（根据当前阶段）
    /// </summary>
    private void InitializeHealth()
    {
        var config = CurrentStageConfig;
        if (config != null)
        {
            currentHealth = config.health;
        }
    }
    
    /// <summary>
    /// 初始化影子缓存
    /// </summary>
    private void InitializeShadowCache()
    {
        if (transform.parent == null) return;
        
        _shadowTransform = transform.parent.Find("Shadow");
        if (_shadowTransform != null)
        {
            _shadowRenderer = _shadowTransform.GetComponent<SpriteRenderer>();
            if (_shadowRenderer != null)
            {
                _originalShadowSprite = _shadowRenderer.sprite;
            }
        }
    }
    #endregion
    
    #region 阶段配置访问
    /// <summary>
    /// 获取指定阶段的配置
    /// </summary>
    private StageConfig GetStageConfig(int stageIndex)
    {
        if (stageConfigs == null || stageIndex < 0 || stageIndex >= stageConfigs.Length)
        {
            return null;
        }
        return stageConfigs[stageIndex];
    }
    #endregion
    
    #region 事件回调
    private void OnSeasonChanged(SeasonManager.Season newSeason)
    {
        currentSeason = newSeason;
        
        // 春季：所有枯萎植物复苏
        if (newSeason == SeasonManager.Season.Spring)
        {
            if (currentState == TreeState.Withered || currentState == TreeState.Frozen || currentState == TreeState.Melted)
            {
                currentState = TreeState.Normal;
                isWeatherWithered = false;
                if (showDebugInfo)
                    Debug.Log($"<color=lime>[TreeController] {gameObject.name} 春季复苏！</color>");
            }
        }
        
        // 冬季：树苗直接死亡（销毁）
        if (newSeason == SeasonManager.Season.Winter)
        {
            if (currentStageIndex == STAGE_SAPLING)
            {
                // ★ 树苗在冬季直接死亡，销毁物体
                if (showDebugInfo)
                    Debug.Log($"<color=red>[TreeController] {gameObject.name} 冬季到来，树苗死亡！</color>");
                
                DestroyTree();
                return;
            }
        }
        
        UpdateSprite();
    }
    
    private void OnVegetationSeasonChanged()
    {
        UpdateSprite();
    }
    
    private void OnDayChanged(int year, int seasonDay, int totalDays)
    {
        if (lastCheckDay == totalDays) return;
        lastCheckDay = totalDays;
        
        // 不成长的条件
        if (currentState != TreeState.Normal) return;
        if (currentStageIndex >= STAGE_MAX) return;
        if (currentSeason == SeasonManager.Season.Winter) return;
        if (isWeatherWithered) return;
        
        // 增加当前阶段天数
        daysInCurrentStage++;
        
        // 检查是否可以成长到下一阶段
        var config = CurrentStageConfig;
        if (config != null && config.daysToNextStage > 0 && daysInCurrentStage >= config.daysToNextStage)
        {
            // ★ 新增：检查成长空间
            if (enableGrowthSpaceCheck && !CanGrowToNextStage())
            {
                // 空间不足，无法成长，但天数不重置（继续等待空间）
                if (showGrowthBlockedInfo && showDebugInfo)
                    Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 成长空间不足，等待空间...</color>");
                return;
            }
            
            GrowToNextStage();
        }
    }
    
    private void OnWeatherWither()
    {
        if (currentState == TreeState.Normal)
        {
            isWeatherWithered = true;
            currentState = TreeState.Withered;
            UpdateSprite();
            if (showDebugInfo)
                Debug.Log($"<color=red>[TreeController] {gameObject.name} 因天气枯萎</color>");
        }
    }
    
    private void OnWeatherRecover()
    {
        if (isWeatherWithered)
        {
            isWeatherWithered = false;
            currentState = TreeState.Normal;
            UpdateSprite();
            if (showDebugInfo)
                Debug.Log($"<color=green>[TreeController] {gameObject.name} 天气恢复</color>");
        }
    }
    
    private void OnWinterSnow()
    {
        if (currentSeason != SeasonManager.Season.Winter) return;
        
        // ★ 树苗在冬季已经死亡，不会进入这里
        // 只有阶段1-5的树木会进入冰封状态
        if (currentStageIndex == STAGE_SAPLING)
        {
            // 树苗不应该存在于冬季，如果存在则销毁
            if (showDebugInfo)
                Debug.Log($"<color=red>[TreeController] {gameObject.name} 冬季下雪，树苗死亡！</color>");
            DestroyTree();
            return;
        }
        
        currentState = TreeState.Frozen;
        UpdateSprite();
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[TreeController] {gameObject.name} 下雪天，进入冰封状态</color>");
    }
    
    private void OnWinterMelt()
    {
        if (currentSeason != SeasonManager.Season.Winter) return;
        
        // ★ 树苗在冬季已经死亡，不会进入这里
        if (currentStageIndex == STAGE_SAPLING)
        {
            // 树苗不应该存在于冬季，如果存在则销毁
            if (showDebugInfo)
                Debug.Log($"<color=red>[TreeController] {gameObject.name} 冬季融化，树苗死亡！</color>");
            DestroyTree();
            return;
        }
        
        currentState = TreeState.Melted;
        UpdateSprite();
        if (showDebugInfo)
            Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 大太阳，冰雪融化</color>");
    }
    #endregion

    
    #region 成长系统
    /// <summary>
    /// 检查是否有足够空间成长到下一阶段
    /// ★ v5 重构：基于 Collider 边界的四方向边距检测
    /// </summary>
    /// <returns>true 表示可以成长，false 表示空间不足</returns>
    public bool CanGrowToNextStage()
    {
        if (!enableGrowthSpaceCheck) return true;
        if (currentStageIndex >= STAGE_MAX) return false;
        
        int nextStage = currentStageIndex + 1;
        var nextStageConfig = GetStageConfig(nextStage);
        if (nextStageConfig == null) return true;
        
        // 使用下一阶段的边距配置进行检测
        return CheckGrowthMargin(nextStageConfig.verticalMargin, nextStageConfig.horizontalMargin);
    }
    
    /// <summary>
    /// 检测四个方向的成长边距
    /// </summary>
    /// <param name="verticalMargin">上下边距</param>
    /// <param name="horizontalMargin">左右边距</param>
    /// <returns>true 表示所有方向都无障碍物，可以成长</returns>
    private bool CheckGrowthMargin(float verticalMargin, float horizontalMargin)
    {
        Vector2 center = GetColliderCenter();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[TreeController] {gameObject.name} 成长边距检测 v5：\n" +
                      $"  - 当前阶段: {currentStageIndex} → {currentStageIndex + 1}\n" +
                      $"  - Collider 中心: {center}\n" +
                      $"  - 上下边距: {verticalMargin}, 左右边距: {horizontalMargin}</color>");
        }
        
        // 检测四个方向
        if (HasObstacleInDirection(center, Vector2.up, verticalMargin))
        {
            if (showGrowthBlockedInfo && showDebugInfo)
                Debug.Log($"<color=orange>[TreeController] {gameObject.name} 上方有障碍物，无法成长</color>");
            return false;
        }
        
        if (HasObstacleInDirection(center, Vector2.down, verticalMargin))
        {
            if (showGrowthBlockedInfo && showDebugInfo)
                Debug.Log($"<color=orange>[TreeController] {gameObject.name} 下方有障碍物，无法成长</color>");
            return false;
        }
        
        if (HasObstacleInDirection(center, Vector2.left, horizontalMargin))
        {
            if (showGrowthBlockedInfo && showDebugInfo)
                Debug.Log($"<color=orange>[TreeController] {gameObject.name} 左方有障碍物，无法成长</color>");
            return false;
        }
        
        if (HasObstacleInDirection(center, Vector2.right, horizontalMargin))
        {
            if (showGrowthBlockedInfo && showDebugInfo)
                Debug.Log($"<color=orange>[TreeController] {gameObject.name} 右方有障碍物，无法成长</color>");
            return false;
        }
        
        if (showDebugInfo)
            Debug.Log($"<color=green>[TreeController] {gameObject.name} 四方向检测通过，可以成长</color>");
        
        return true;
    }
    
    /// <summary>
    /// 检测指定方向上是否有障碍物
    /// </summary>
    /// <param name="center">检测起点（Collider 中心）</param>
    /// <param name="direction">检测方向</param>
    /// <param name="distance">检测距离（边距）</param>
    /// <returns>true 表示有障碍物</returns>
    private bool HasObstacleInDirection(Vector2 center, Vector2 direction, float distance)
    {
        if (growthObstacleTags == null || growthObstacleTags.Length == 0) return false;
        
        // 计算检测点（从中心向指定方向偏移）
        Vector2 checkPoint = center + direction * distance;
        
        // 使用小范围圆形检测
        float checkRadius = 0.1f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPoint, checkRadius);
        
        foreach (var hit in hits)
        {
            // 跳过自己和子物体
            if (hit.transform == transform) continue;
            if (transform.parent != null && hit.transform == transform.parent) continue;
            if (transform.parent != null && hit.transform.IsChildOf(transform.parent)) continue;
            
            // 检查标签（包括父级）
            if (HasAnyTag(hit.transform, growthObstacleTags))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 在 {direction} 方向检测到障碍物: {hit.gameObject.name} (Tag: {hit.tag})</color>");
                }
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查 Transform 或其父级是否有指定标签
    /// </summary>
    private bool HasAnyTag(Transform t, string[] tags)
    {
        Transform current = t;
        while (current != null)
        {
            foreach (var tag in tags)
            {
                if (current.CompareTag(tag))
                    return true;
            }
            current = current.parent;
        }
        return false;
    }
    
    /// <summary>
    /// 获取 Collider 中心点
    /// </summary>
    private Vector2 GetColliderCenter()
    {
        // 尝试获取 Collider2D
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.enabled)
        {
            return col.bounds.center;
        }
        
        // 如果没有 Collider，使用父物体位置（树根位置）
        if (transform.parent != null)
        {
            return transform.parent.position;
        }
        
        return transform.position;
    }
    
    /// <summary>
    /// 成长到下一阶段
    /// </summary>
    public void GrowToNextStage()
    {
        if (currentStageIndex >= STAGE_MAX) return;
        
        currentStageIndex++;
        daysInCurrentStage = 0;
        
        // 重新初始化血量
        InitializeHealth();
        
        // 更新显示
        UpdateSprite();
        
        // ★ 阶段变化时更新碰撞体形状
        UpdatePolygonColliderShape();
        
        if (showDebugInfo)
            Debug.Log($"<color=lime>[TreeController] {gameObject.name} 成长到阶段 {currentStageIndex}！</color>");
    }
    
    /// <summary>
    /// 更新 PolygonCollider2D 形状（仅在阶段变化时调用）
    /// </summary>
    private void UpdatePolygonColliderShape()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            if (collider is PolygonCollider2D poly)
            {
                UpdatePolygonColliderFromSprite(poly, spriteRenderer.sprite);
            }
        }
        
        // 🔥 关键修复：碰撞体形状变化后，通知 NavGrid 刷新
        // 树木成长时碰撞体变大，需要更新导航网格的阻挡区域
        RequestNavGridRefresh();
    }
    
    /// <summary>
    /// 设置阶段（用于调试或初始化）
    /// </summary>
    public void SetStage(int stageIndex)
    {
        currentStageIndex = Mathf.Clamp(stageIndex, 0, STAGE_MAX);
        daysInCurrentStage = 0;
        InitializeHealth();
        UpdateSprite();
    }
    #endregion
    
    #region IResourceNode 接口实现
    public string ResourceTag => "Tree";
    
    /// <summary>
    /// 资源是否已耗尽
    /// ★ 修复：树桩状态不算耗尽，树桩可以继续被砍
    /// 只有当树木被完全销毁时才算耗尽（但此时对象已不存在）
    /// </summary>
    public bool IsDepleted => false;
    
    /// <summary>
    /// 检查是否接受此工具类型
    /// </summary>
    public bool CanAccept(ToolHitContext ctx)
    {
        // ★ 修复：树桩状态下，只接受斧头
        if (currentState == TreeState.Stump)
        {
            return ctx.toolType == ToolType.Axe;
        }
        
        var config = CurrentStageConfig;
        if (config == null) return false;
        
        // 检查工具类型是否匹配
        return ctx.toolType == config.acceptedToolType;
    }
    
    /// <summary>
    /// 处理命中效果
    /// </summary>
    public void OnHit(ToolHitContext ctx)
    {
        // 树桩状态：检查是否可以继续砍树桩
        if (currentState == TreeState.Stump)
        {
            HandleStumpHit(ctx);
            return;
        }
        
        // 记录玩家朝向（用于倒下动画）
        RecordPlayerDirection(ctx);
        
        // 计算被砍方向
        Vector2 chopDirection = -ctx.hitDir;
        
        // 检查工具类型
        var config = CurrentStageConfig;
        if (config == null) return;
        
        bool isCorrectTool = ctx.toolType == config.acceptedToolType;
        
        if (isCorrectTool)
        {
            // 阶段0（树苗）：锄头挖出
            if (currentStageIndex == STAGE_SAPLING)
            {
                HandleSaplingDigOut(ctx);
                return;
            }
            
            // 阶段1-5：斧头砍伐
            HandleAxeChop(ctx, chopDirection);
        }
        else
        {
            // 错误工具：只抖动
            PlayHitEffect(chopDirection);
            if (showDebugInfo)
                Debug.Log($"<color=gray>[TreeController] {gameObject.name} 被错误工具击中，只抖动</color>");
        }
    }
    
    public Bounds GetBounds()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.bounds;
        }
        return new Bounds(GetPosition(), Vector3.one * 0.5f);
    }
    
    /// <summary>
    /// 获取碰撞体边界（用于精确命中检测）
    /// 返回 Collider bounds，无 Collider 时回退到 Sprite bounds
    /// </summary>
    public Bounds GetColliderBounds()
    {
        // 优先使用 Collider2D 的 bounds
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null && collider.enabled)
        {
            return collider.bounds;
        }
        
        // 检查父物体的 CompositeCollider2D
        if (transform.parent != null)
        {
            var compositeCollider = transform.parent.GetComponent<CompositeCollider2D>();
            if (compositeCollider != null && compositeCollider.enabled)
            {
                return compositeCollider.bounds;
            }
        }
        
        // 回退到 Sprite bounds
        return GetBounds();
    }
    
    public Vector3 GetPosition()
    {
        return transform.parent != null ? transform.parent.position : transform.position;
    }
    #endregion
    
    #region 工具交互处理
    /// <summary>
    /// 记录玩家朝向
    /// </summary>
    private void RecordPlayerDirection(ToolHitContext ctx)
    {
        if (ctx.attacker != null)
        {
            var playerAnimator = ctx.attacker.GetComponentInChildren<Animator>();
            if (playerAnimator != null)
            {
                lastHitPlayerDirection = playerAnimator.GetInteger("Direction");
            }
            var playerSprite = ctx.attacker.GetComponentInChildren<SpriteRenderer>();
            if (playerSprite != null)
            {
                lastHitPlayerFlipX = playerSprite.flipX;
            }
        }
    }
    
    /// <summary>
    /// 处理树苗挖出（阶段0，锄头）
    /// </summary>
    private void HandleSaplingDigOut(ToolHitContext ctx)
    {
        // 尝试消耗精力
        float energyCost = GetEnergyCost(ctx);
        if (!TryConsumeEnergy(energyCost))
        {
            if (showDebugInfo)
                Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 精力不足，无法挖出树苗</color>");
            return;
        }
        
        // 播放挖出音效
        PlayDigOutSound();
        
        // 生成掉落物
        SpawnDrops();
        
        // 销毁树苗
        DestroyTree();
        
        if (showDebugInfo)
            Debug.Log($"<color=orange>[TreeController] {gameObject.name} 树苗被挖出！</color>");
    }
    
    /// <summary>
    /// 处理斧头砍伐（阶段1-5）
    /// </summary>
    private void HandleAxeChop(ToolHitContext ctx, Vector2 chopDirection)
    {
        // ★ 先尝试消耗精力（无论等级是否足够，只要挥动斧头就消耗精力）
        float energyCost = GetEnergyCost(ctx);
        bool hasEnergy = TryConsumeEnergy(energyCost);
        
        if (!hasEnergy)
        {
            PlayHitEffect(chopDirection);
            if (showDebugInfo)
                Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 精力不足，无法砍伐</color>");
            return;
        }
        
        // ★ 检查斧头材料等级（精力已消耗，但等级不足则不造成伤害）
        int axeTier = GetAxeTier(ctx);
        if (!MaterialTierHelper.CanChopTree(axeTier, currentStageIndex))
        {
            // 等级不足：播放金属碰撞音效和提示（精力已消耗，但不造成伤害）
            PlayTierInsufficientFeedback(axeTier);
            PlayHitEffect(chopDirection);
            if (showDebugInfo)
                Debug.Log($"<color=red>[TreeController] {gameObject.name} 斧头等级不足！需要 {MaterialTierHelper.GetTierName(MaterialTierHelper.GetRequiredAxeTier(currentStageIndex))} 斧头，当前 {MaterialTierHelper.GetTierName(axeTier)} 斧头（精力已消耗）</color>");
            return;
        }
        
        // ✅ 设置砍伐状态（通过 OcclusionManager 确保单一高亮）
        if (occlusionTransparency != null)
        {
            if (OcclusionManager.Instance != null)
            {
                OcclusionManager.Instance.SetChoppingTree(occlusionTransparency, 0.5f);
            }
            else
            {
                occlusionTransparency.SetChoppingState(true, 0.25f);
            }
        }
        
        // ★ 计算伤害（使用 ctx.baseDamage）
        int damage = Mathf.Max(1, Mathf.RoundToInt(ctx.baseDamage));
        
        // ★ 调试输出
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[TreeController] {gameObject.name} 砍伐信息：\n" +
                      $"  - 斧头等级：{MaterialTierHelper.GetTierName(axeTier)}\n" +
                      $"  - 基础伤害：{ctx.baseDamage}\n" +
                      $"  - 实际伤害：{damage}\n" +
                      $"  - 精力消耗：{energyCost}\n" +
                      $"  - 当前血量：{currentHealth}/{CurrentStageConfig.health}</color>");
        }
        
        // 扣血
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            // 砍倒
            ChopDown();
        }
        else
        {
            // 未砍倒：播放效果
            PlayHitEffect(chopDirection);
            SpawnLeafParticles();
            PlayChopHitSound();
        }
        
        if (showDebugInfo)
            Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 受到 {damage} 点伤害，剩余血量 {currentHealth}</color>");
    }
    
    /// <summary>
    /// 处理树桩命中
    /// </summary>
    private void HandleStumpHit(ToolHitContext ctx)
    {
        // 只有斧头能砍树桩
        if (ctx.toolType != ToolType.Axe)
        {
            if (showDebugInfo)
                Debug.Log($"<color=gray>[TreeController] {gameObject.name} 树桩只能用斧头砍</color>");
            return;
        }
        
        var config = CurrentStageConfig;
        if (config == null || !config.hasStump)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[TreeController] {gameObject.name} 当前阶段没有树桩配置");
            return;
        }
        
        // 尝试消耗精力
        float energyCost = GetEnergyCost(ctx);
        if (!TryConsumeEnergy(energyCost))
        {
            if (showDebugInfo)
                Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 精力不足，无法砍树桩</color>");
            return;
        }
        
        // 计算伤害
        int damage = Mathf.Max(1, Mathf.RoundToInt(ctx.baseDamage));
        
        // ★ 调试输出
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[TreeController] {gameObject.name} 树桩砍伐信息：\n" +
                      $"  - 基础伤害：{ctx.baseDamage}\n" +
                      $"  - 实际伤害：{damage}\n" +
                      $"  - 精力消耗：{energyCost}\n" +
                      $"  - 当前树桩血量：{currentStumpHealth}/{config.stumpHealth}</color>");
        }
        
        // 扣树桩血量
        currentStumpHealth -= damage;
        
        // 播放效果
        PlayChopHitSound();
        
        if (currentStumpHealth <= 0)
        {
            // 树桩被砍完
            SpawnStumpDrops();
            DestroyTree();
            
            if (showDebugInfo)
                Debug.Log($"<color=orange>[TreeController] {gameObject.name} 树桩被砍完！</color>");
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 树桩受到 {damage} 点伤害，剩余 {currentStumpHealth}</color>");
        }
    }
    
    /// <summary>
    /// 获取斧头材料等级
    /// </summary>
    private int GetAxeTier(ToolHitContext ctx)
    {
        if (ctx.attacker != null)
        {
            var toolController = ctx.attacker.GetComponent<PlayerToolController>();
            if (toolController != null && toolController.CurrentToolData != null)
            {
                var toolData = toolController.CurrentToolData as ToolData;
                if (toolData != null)
                {
                    return toolData.GetMaterialTierValue();
                }
            }
        }
        return 0; // 默认木质
    }
    
    /// <summary>
    /// 获取精力消耗
    /// </summary>
    private float GetEnergyCost(ToolHitContext ctx)
    {
        float energyCost = 2f; // 默认
        
        if (ctx.attacker != null)
        {
            var toolController = ctx.attacker.GetComponent<PlayerToolController>();
            if (toolController != null && toolController.CurrentToolData != null)
            {
                var toolData = toolController.CurrentToolData as ToolData;
                if (toolData != null)
                {
                    energyCost = toolData.energyCost;
                }
            }
        }
        
        return energyCost;
    }
    
    /// <summary>
    /// 尝试消耗精力
    /// </summary>
    private bool TryConsumeEnergy(float energyCost)
    {
        if (EnergySystem.Instance != null)
        {
            return EnergySystem.Instance.TryConsumeEnergy(Mathf.RoundToInt(energyCost));
        }
        return true; // 如果没有精力系统，默认允许
    }
    #endregion
    
    #region 砍伐系统
    /// <summary>
    /// 砍倒树木
    /// </summary>
    public void ChopDown()
    {
        // ✅ 重置砍伐状态（通过 OcclusionManager 清除高亮）
        if (OcclusionManager.Instance != null)
        {
            OcclusionManager.Instance.ClearChoppingHighlight();
        }
        else if (occlusionTransparency != null)
        {
            occlusionTransparency.SetChoppingState(false);
        }
        
        // 播放砍倒音效
        PlayChopFellSound();
        
        // 生成掉落物
        SpawnDrops();
        
        // 获取砍树经验
        GrantWoodcuttingExperience();
        
        // 检查是否有树桩
        var config = CurrentStageConfig;
        bool hasStump = config != null && config.hasStump;
        
        if (hasStump)
        {
            // 启动倒下动画或直接转换为树桩
            if (enableFallAnimation)
            {
                StartCoroutine(FallAnimationCoroutine(lastHitPlayerDirection, lastHitPlayerFlipX));
            }
            else
            {
                FinishChopDown();
            }
        }
        else
        {
            // 没有树桩，直接销毁
            if (enableFallAnimation)
            {
                StartCoroutine(FallAndDestroyCoroutine(lastHitPlayerDirection, lastHitPlayerFlipX));
            }
            else
            {
                DestroyTree();
            }
        }
        
        if (showDebugInfo)
            Debug.Log($"<color=orange>[TreeController] {gameObject.name} 被砍倒！hasStump={hasStump}</color>");
    }
    
    /// <summary>
    /// 完成砍倒（转换为树桩）
    /// </summary>
    private void FinishChopDown()
    {
        currentState = TreeState.Stump;
        
        // 初始化树桩血量
        var config = CurrentStageConfig;
        if (config != null)
        {
            currentStumpHealth = config.stumpHealth;
        }
        
        UpdateSprite();
        
        // ★ 树桩状态需要更新碰撞体形状（从树干变为树桩）
        UpdatePolygonColliderShape();
    }
    
    /// <summary>
    /// 销毁树木
    /// </summary>
    private void DestroyTree()
    {
        // 从注册表注销
        if (ResourceNodeRegistry.Instance != null)
        {
            ResourceNodeRegistry.Instance.Unregister(gameObject.GetInstanceID());
        }
        
        // 销毁父物体（整棵树）
        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    
    #region 掉落系统
    /// <summary>
    /// 生成掉落物（砍倒树干时）
    /// </summary>
    private void SpawnDrops()
    {
        // 使用新的简化配置
        if (dropItemData == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[TreeController] {gameObject.name} 未配置掉落物品 SO");
            return;
        }
        
        // 获取当前阶段的掉落数量
        int dropAmount = 0;
        if (stageDropAmounts != null && currentStageIndex < stageDropAmounts.Length)
        {
            dropAmount = stageDropAmounts[currentStageIndex];
        }
        
        if (dropAmount <= 0) return;
        
        Vector3 dropOrigin = GetPosition();
        
        if (WorldSpawnService.Instance != null)
        {
            WorldSpawnService.Instance.SpawnMultiple(
                dropItemData,
                0, // 品质默认为0
                dropAmount,
                dropOrigin,
                dropSpreadRadius
            );
        }
        
        if (showDebugInfo)
            Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 阶段{currentStageIndex} 掉落 {dropAmount} 个 {dropItemData.itemName}</color>");
    }
    
    /// <summary>
    /// 生成树桩掉落物
    /// </summary>
    private void SpawnStumpDrops()
    {
        // 使用新的简化配置
        if (dropItemData == null) return;
        
        // 获取当前阶段的树桩掉落数量
        int dropAmount = 0;
        if (stumpDropAmounts != null && currentStageIndex < stumpDropAmounts.Length)
        {
            dropAmount = stumpDropAmounts[currentStageIndex];
        }
        
        if (dropAmount <= 0) return;
        
        Vector3 dropOrigin = GetPosition();
        
        if (WorldSpawnService.Instance != null)
        {
            WorldSpawnService.Instance.SpawnMultiple(
                dropItemData,
                0, // 品质默认为0
                dropAmount,
                dropOrigin,
                dropSpreadRadius
            );
        }
        
        if (showDebugInfo)
            Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 树桩掉落 {dropAmount} 个 {dropItemData.itemName}</color>");
    }
    #endregion
    
    #region 经验系统
    /// <summary>
    /// 获取当前阶段的砍伐经验
    /// </summary>
    public int GetChopExperience()
    {
        if (stageExperience == null || currentStageIndex >= stageExperience.Length)
        {
            return 0;
        }
        return stageExperience[currentStageIndex];
    }
    
    /// <summary>
    /// 给予砍树经验
    /// </summary>
    private void GrantWoodcuttingExperience()
    {
        int xp = GetChopExperience();
        if (xp <= 0) return;
        
        if (FarmGame.Data.SkillLevelService.Instance != null)
        {
            FarmGame.Data.SkillLevelService.Instance.AddExperience(FarmGame.Data.SkillType.Gathering, xp);
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=lime>[TreeController] {gameObject.name} 阶段{currentStageIndex} 砍伐获得 {xp} 点采集经验</color>");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[TreeController] SkillLevelService 未初始化，无法给予经验");
            }
        }
    }
    #endregion
    
    #region Sprite显示系统
    /// <summary>
    /// 更新Sprite显示
    /// ★ 注意：Stage 0（树苗）在冬季直接死亡，不会调用此方法
    /// </summary>
    public void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        Sprite targetSprite = GetCurrentSprite();
        
        if (targetSprite != null)
        {
            spriteRenderer.sprite = targetSprite;
            spriteRenderer.enabled = true;
            
            if (alignSpriteBottom)
            {
                AlignSpriteBottom();
            }
            UpdateShadowScale();
            UpdateColliderState();
        }
        else
        {
            // 无有效 Sprite 时隐藏
            spriteRenderer.enabled = false;
            UpdateShadowScale();
        }
    }
    
    /// <summary>
    /// 获取当前应该显示的Sprite
    /// </summary>
    private Sprite GetCurrentSprite()
    {
        if (SeasonManager.Instance == null) return null;
        if (spriteConfig == null) return null;
        
        var stageData = CurrentSpriteData;
        if (stageData == null) return null;
        
        var vegSeason = SeasonManager.Instance.GetCurrentVegetationSeason();
        
        // 树桩状态
        if (currentState == TreeState.Stump)
        {
            return stageData.GetStumpSprite(vegSeason);
        }
        
        // 冬季特殊处理
        if (vegSeason == SeasonManager.VegetationSeason.Winter)
        {
            return GetWinterSprite(stageData, vegSeason);
        }
        
        // 枯萎状态
        if (currentState == TreeState.Withered)
        {
            return stageData.GetWitheredSprite(vegSeason);
        }
        
        // 正常状态
        return GetNormalSprite(stageData, vegSeason);
    }
    
    /// <summary>
    /// 获取冬季Sprite
    /// ★ 注意：Stage 0（树苗）在冬季直接死亡，不会调用此方法
    /// </summary>
    private Sprite GetWinterSprite(StageSpriteData stageData, SeasonManager.VegetationSeason vegSeason)
    {
        // 冰封状态（挂冰）
        if (currentState == TreeState.Frozen || currentState == TreeState.Normal)
        {
            return stageData.normal.GetSprite(vegSeason);
        }
        
        // 融化状态 - 降级到枯萎外观
        if (currentState == TreeState.Melted)
        {
            return stageData.GetWitheredSprite(vegSeason);
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取正常状态Sprite（支持季节渐变）
    /// </summary>
    private Sprite GetNormalSprite(StageSpriteData stageData, SeasonManager.VegetationSeason vegSeason)
    {
        // 使用 SeasonSpriteSet 获取对应季节的 Sprite
        if (stageData.normal == null) return null;
        
        // 检查是否需要渐变
        float progress = SeasonManager.Instance.GetTransitionProgress();
        
        // ★ 调试输出：季节 Sprite 选择逻辑
        if (showDebugInfo)
        {
            int dayInSeason = TimeManager.Instance != null ? TimeManager.Instance.GetDay() : -1;
            var calendarSeason = SeasonManager.Instance.GetCurrentSeason();
            Debug.Log($"<color=magenta>[TreeController] {gameObject.name} 季节Sprite选择：\n" +
                      $"  - 日历季节: {calendarSeason}\n" +
                      $"  - 季节天数: {dayInSeason}\n" +
                      $"  - 植被季节: {vegSeason}\n" +
                      $"  - 渐变进度: {progress:F3}\n" +
                      $"  - spring配置: {(stageData.normal.spring != null ? stageData.normal.spring.name : "NULL")}\n" +
                      $"  - summer配置: {(stageData.normal.summer != null ? stageData.normal.summer.name : "NULL")}</color>");
        }
        
        // ★ 渐变逻辑说明：
        // - progress = 0：无渐变，100% 显示当前季节
        // - progress = 0.5：50% 树木显示下一季节
        // - progress = 1.0：渐变完成，100% 显示下一季节
        // - 渐变是不可逆的：一旦 treeSeedValue < progress，该树就显示下一季节
        
        // 如果进度为0，直接返回当前季节的Sprite（无渐变）
        if (progress <= 0f)
        {
            Sprite result = stageData.normal.GetSprite(vegSeason);
            if (showDebugInfo)
            {
                Debug.Log($"<color=lime>[TreeController] {gameObject.name} 无渐变(progress=0)，显示当前季节 {vegSeason}: {(result != null ? result.name : "NULL")}</color>");
            }
            return result;
        }
        
        // 如果进度为1，渐变完成，返回下一季节的Sprite
        if (progress >= 1f)
        {
            var nextSeason = GetNextVegetationSeason(vegSeason);
            Sprite result = stageData.normal.GetSprite(nextSeason);
            if (showDebugInfo)
            {
                Debug.Log($"<color=lime>[TreeController] {gameObject.name} 渐变完成(progress=1)，显示下一季节 {nextSeason}: {(result != null ? result.name : "NULL")}</color>");
            }
            return result;
        }
        
        // 渐变中：使用treeID生成固定随机值
        // ★ 每棵树有固定的随机种子，保证同一棵树在同一进度下始终显示相同季节
        int seed = treeID + currentStageIndex * 100;
        Random.InitState(seed);
        float treeSeedValue = Random.value;
        
        // 根据进度判断显示哪个季节
        // ★ 渐变不可逆：一旦 treeSeedValue < progress，该树就显示下一季节
        if (treeSeedValue < progress)
        {
            // 显示下一季节
            var nextSeason = GetNextVegetationSeason(vegSeason);
            Sprite result = stageData.normal.GetSprite(nextSeason);
            if (showDebugInfo)
            {
                Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 渐变中，treeSeed={treeSeedValue:F3} < progress={progress:F3}，显示下一季节 {nextSeason}: {(result != null ? result.name : "NULL")}</color>");
            }
            return result;
        }
        else
        {
            // 显示当前季节
            Sprite result = stageData.normal.GetSprite(vegSeason);
            if (showDebugInfo)
            {
                Debug.Log($"<color=cyan>[TreeController] {gameObject.name} 渐变中，treeSeed={treeSeedValue:F3} >= progress={progress:F3}，显示当前季节 {vegSeason}: {(result != null ? result.name : "NULL")}</color>");
            }
            return result;
        }
    }
    
    /// <summary>
    /// 获取下一个植被季节
    /// </summary>
    private SeasonManager.VegetationSeason GetNextVegetationSeason(SeasonManager.VegetationSeason current)
    {
        return current switch
        {
            SeasonManager.VegetationSeason.Spring => SeasonManager.VegetationSeason.Summer,
            SeasonManager.VegetationSeason.Summer => SeasonManager.VegetationSeason.EarlyFall,
            SeasonManager.VegetationSeason.EarlyFall => SeasonManager.VegetationSeason.LateFall,
            SeasonManager.VegetationSeason.LateFall => SeasonManager.VegetationSeason.Winter,
            SeasonManager.VegetationSeason.Winter => SeasonManager.VegetationSeason.Spring,
            _ => current
        };
    }
    #endregion
    
    #region Sprite对齐与碰撞体
    /// <summary>
    /// 对齐Sprite底部到父物体中心
    /// </summary>
    private void AlignSpriteBottom()
    {
        if (!alignSpriteBottom) return;
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        
        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        float spriteBottomOffset = spriteBounds.min.y;
        
        Vector3 localPos = spriteRenderer.transform.localPosition;
        localPos.y = -spriteBottomOffset;
        spriteRenderer.transform.localPosition = localPos;
    }
    
    /// <summary>
    /// 更新碰撞体状态
    /// </summary>
    private void UpdateColliderState()
    {
        var config = CurrentStageConfig;
        if (config == null) return;
        
        Collider2D[] colliders = GetComponents<Collider2D>();
        if (colliders.Length == 0) return;
        
        bool hadEnabledCollider = false;
        bool hasEnabledCollider = false;
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.enabled) hadEnabledCollider = true;
        }
        
        // 根据配置设置碰撞体状态
        bool shouldEnableCollider = config.enableCollider;
        
        // 树桩状态：保持碰撞体
        if (currentState == TreeState.Stump)
        {
            shouldEnableCollider = true;
        }
        
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = shouldEnableCollider;
            if (shouldEnableCollider) hasEnabledCollider = true;
            
            // ★ 优化：只在阶段变化时更新 PolygonCollider2D 形状
            // 不再每次 UpdateSprite 都更新，避免性能问题
        }
        
        // 更新遮挡透明
        if (occlusionTransparency != null)
        {
            bool shouldEnableOcclusion = config.enableOcclusion && currentState != TreeState.Stump;
            occlusionTransparency.SetCanBeOccluded(shouldEnableOcclusion);
        }
        
        // 如果碰撞体状态改变，通知NavGrid2D刷新
        if (hadEnabledCollider != hasEnabledCollider)
        {
            RequestNavGridRefresh();
        }
    }
    
    /// <summary>
    /// 从Sprite更新PolygonCollider2D
    /// </summary>
    private void UpdatePolygonColliderFromSprite(PolygonCollider2D poly, Sprite sprite)
    {
        if (poly == null || sprite == null) return;
        
        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount == 0)
        {
            poly.pathCount = 0;
            return;
        }
        
        poly.pathCount = shapeCount;
        
        List<Vector2> physicsShape = new List<Vector2>();
        for (int i = 0; i < shapeCount; i++)
        {
            physicsShape.Clear();
            sprite.GetPhysicsShape(i, physicsShape);
            poly.SetPath(i, physicsShape);
        }
        
        poly.offset = Vector2.zero;
    }
    
    /// <summary>
    /// 请求NavGrid2D刷新
    /// </summary>
    private void RequestNavGridRefresh()
    {
        if (IsInvoking(nameof(TriggerNavGridRefresh)))
        {
            CancelInvoke(nameof(TriggerNavGridRefresh));
        }
        Invoke(nameof(TriggerNavGridRefresh), 0.2f);
    }
    
    private void TriggerNavGridRefresh()
    {
        NavGrid2D.OnRequestGridRefresh?.Invoke();
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[TreeController] {gameObject.name} 通知NavGrid2D刷新网格</color>");
    }
    #endregion
    
    #region 影子系统
    /// <summary>
    /// 更新影子显示（包括 Sprite 切换、缩放和位置）
    /// </summary>
    private void UpdateShadowScale()
    {
        // 使用缓存的引用，如果未初始化则尝试初始化
        if (_shadowRenderer == null)
        {
            InitializeShadowCache();
            if (_shadowRenderer == null) return;
        }
        
        // 判断是否应该显示影子
        bool shouldShow = ShouldShowShadow();
        
        if (!shouldShow)
        {
            _shadowRenderer.enabled = false;
            return;
        }
        
        // 获取当前阶段的影子配置
        ShadowConfig config = GetShadowConfigForCurrentStage();
        
        if (config == null || config.scale <= 0f)
        {
            _shadowRenderer.enabled = false;
            return;
        }
        
        // 启用影子
        _shadowRenderer.enabled = true;
        
        // 切换影子 Sprite（如果配置了）
        if (config.sprite != null)
        {
            _shadowRenderer.sprite = config.sprite;
        }
        else if (_originalShadowSprite != null)
        {
            // 回退到原始 Sprite
            _shadowRenderer.sprite = _originalShadowSprite;
        }
        
        // 设置缩放
        _shadowTransform.localScale = new Vector3(config.scale, config.scale, 1f);
        
        // 对齐影子中心到父物体中心
        AlignShadowCenter();
    }
    
    /// <summary>
    /// 判断是否应该显示影子
    /// </summary>
    private bool ShouldShowShadow()
    {
        // 树桩无影子
        if (currentState == TreeState.Stump) return false;
        
        // 阶段0无影子
        if (currentStageIndex < 1) return false;
        
        return true;
    }
    
    /// <summary>
    /// 获取当前阶段的影子配置
    /// shadowConfigs 数组有5个元素，对应阶段1-5
    /// </summary>
    private ShadowConfig GetShadowConfigForCurrentStage()
    {
        // 阶段0没有影子
        if (currentStageIndex < 1) return null;
        
        // 计算在 shadowConfigs 数组中的索引（阶段1对应索引0）
        int configIndex = currentStageIndex - 1;
        
        if (shadowConfigs == null || configIndex >= shadowConfigs.Length)
        {
            return null;
        }
        
        return shadowConfigs[configIndex];
    }
    
    /// <summary>
    /// 对齐影子中心到父物体中心
    /// </summary>
    private void AlignShadowCenter()
    {
        if (_shadowRenderer == null || _shadowRenderer.sprite == null) return;
        
        Bounds shadowBounds = _shadowRenderer.sprite.bounds;
        float centerOffset = shadowBounds.center.y;
        
        Vector3 shadowPos = _shadowTransform.localPosition;
        shadowPos.y = -centerOffset;
        _shadowTransform.localPosition = shadowPos;
    }
    #endregion

    
    #region 音效系统
    private void PlayChopHitSound()
    {
        if (chopHitSound != null)
        {
            AudioSource.PlayClipAtPoint(chopHitSound, GetPosition(), soundVolume);
        }
    }
    
    private void PlayChopFellSound()
    {
        if (chopFellSound != null)
        {
            AudioSource.PlayClipAtPoint(chopFellSound, GetPosition(), soundVolume);
        }
    }
    
    private void PlayDigOutSound()
    {
        if (digOutSound != null)
        {
            AudioSource.PlayClipAtPoint(digOutSound, GetPosition(), soundVolume);
        }
    }
    
    private void PlayTierInsufficientSound()
    {
        if (tierInsufficientSound != null)
        {
            AudioSource.PlayClipAtPoint(tierInsufficientSound, GetPosition(), soundVolume);
        }
    }
    
    /// <summary>
    /// 播放斧头等级不足反馈
    /// </summary>
    private void PlayTierInsufficientFeedback(int currentTier)
    {
        // 播放金属碰撞音效
        PlayTierInsufficientSound();
        
        // TODO: 显示UI提示 "斧头等级不足"
        // 可以通过事件系统通知UI显示提示
    }
    #endregion
    
    #region 视觉效果
    /// <summary>
    /// 播放受击效果（抖动）
    /// </summary>
    private void PlayHitEffect(Vector2 hitDir)
    {
        StartCoroutine(HitShakeCoroutine(hitDir));
    }
    
    private System.Collections.IEnumerator HitShakeCoroutine(Vector2 hitDir)
    {
        if (spriteRenderer == null) yield break;
        
        Vector3 originalPos = spriteRenderer.transform.localPosition;
        float shakeDuration = 0.15f;
        float shakeAmount = 0.08f;
        float elapsed = 0f;
        
        float shakeDir = hitDir.x != 0 ? Mathf.Sign(hitDir.x) : 1f;
        
        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            float damping = 1f - progress;
            float x = Mathf.Sin(progress * Mathf.PI * 4) * shakeAmount * damping * shakeDir;
            spriteRenderer.transform.localPosition = originalPos + new Vector3(x, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        spriteRenderer.transform.localPosition = originalPos;
    }
    
    /// <summary>
    /// 生成树叶粒子
    /// </summary>
    private void SpawnLeafParticles()
    {
        var leafSpawner = GetComponent<LeafSpawner>();
        if (leafSpawner != null)
        {
            leafSpawner.SpawnLeaves(GetBounds());
        }
    }
    #endregion
    
    #region 倒下动画
    /// <summary>
    /// 倒下方向枚举
    /// </summary>
    public enum FallDirection
    {
        Right,
        Left,
        Up
    }
    
    /// <summary>
    /// 根据玩家朝向判定倒下方向
    /// Direction 参数映射：0=Down, 1=Up, 2=Side
    /// </summary>
    private FallDirection DetermineFallDirection(int playerDirection, bool playerFlipX)
    {
        switch (playerDirection)
        {
            case 0: // Down
                return playerFlipX ? FallDirection.Left : FallDirection.Right;
            case 1: // Up
                return playerFlipX ? FallDirection.Right : FallDirection.Left;
            case 2: // Side
                return FallDirection.Up;
            default:
                return FallDirection.Right;
        }
    }
    
    private float CalculateTargetAngle(FallDirection fallDir)
    {
        return fallDir switch
        {
            FallDirection.Right => -90f,
            FallDirection.Left => 90f,
            FallDirection.Up => 90f,
            _ => 0f
        };
    }
    
    /// <summary>
    /// 倒下动画协程（转换为树桩）
    /// </summary>
    private System.Collections.IEnumerator FallAnimationCoroutine(int playerDirection, bool playerFlipX)
    {
        if (spriteRenderer == null)
        {
            FinishChopDown();
            yield break;
        }
        
        FallDirection fallDir = DetermineFallDirection(playerDirection, playerFlipX);
        float targetAngle = CalculateTargetAngle(fallDir);
        
        // 保存当前 Sprite 信息
        Sprite fallingSprite = spriteRenderer.sprite;
        Vector3 originalWorldPos = spriteRenderer.transform.position;
        Vector3 originalScale = spriteRenderer.transform.localScale;
        Color originalColor = spriteRenderer.color;
        int sortingLayerID = spriteRenderer.sortingLayerID;
        int sortingOrder = spriteRenderer.sortingOrder;
        
        Bounds spriteBounds = spriteRenderer.bounds;
        Vector3 spriteBottomCenter = new Vector3(spriteBounds.center.x, spriteBounds.min.y, 0);
        float spriteHalfHeight = spriteBounds.extents.y;
        
        // 转换为树桩
        FinishChopDown();
        
        // 创建临时倒下 Sprite
        GameObject fallingTree = new GameObject("FallingTree_Temp");
        fallingTree.transform.position = originalWorldPos;
        fallingTree.transform.localScale = originalScale;
        
        SpriteRenderer fallingSR = fallingTree.AddComponent<SpriteRenderer>();
        fallingSR.sprite = fallingSprite;
        fallingSR.sortingLayerID = sortingLayerID;
        fallingSR.sortingOrder = sortingOrder - 1;
        fallingSR.color = originalColor;
        
        float elapsed = 0f;
        bool isSidefall = (fallDir == FallDirection.Left || fallDir == FallDirection.Right);
        
        while (elapsed < fallDuration)
        {
            float linearT = elapsed / fallDuration;
            float t = linearT * linearT;
            
            if (isSidefall)
            {
                float angle = targetAngle * t;
                float rad = angle * Mathf.Deg2Rad;
                
                Vector3 centerOffset = new Vector3(0, spriteHalfHeight, 0);
                Vector3 rotatedOffset = new Vector3(
                    centerOffset.x * Mathf.Cos(rad) - centerOffset.y * Mathf.Sin(rad),
                    centerOffset.x * Mathf.Sin(rad) + centerOffset.y * Mathf.Cos(rad),
                    0
                );
                
                Vector3 newCenter = spriteBottomCenter + rotatedOffset;
                fallingTree.transform.position = newCenter;
                fallingTree.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                float scaleY;
                if (t < fallUpStretchPhase)
                {
                    scaleY = Mathf.Lerp(1f, fallUpMaxStretch, t / fallUpStretchPhase);
                }
                else
                {
                    scaleY = Mathf.Lerp(fallUpMaxStretch, fallUpMinScale, (t - fallUpStretchPhase) / (1f - fallUpStretchPhase));
                }
                
                float newHalfHeight = spriteHalfHeight * scaleY;
                float newCenterY = spriteBottomCenter.y + newHalfHeight;
                
                fallingTree.transform.localScale = new Vector3(originalScale.x, originalScale.y * scaleY, originalScale.z);
                fallingTree.transform.position = new Vector3(spriteBottomCenter.x, newCenterY, 0);
            }
            
            // 淡出
            if (linearT > 0.7f)
            {
                float fadeT = (linearT - 0.7f) / 0.3f;
                Color fadeColor = originalColor;
                fadeColor.a = originalColor.a * (1f - fadeT);
                fallingSR.color = fadeColor;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(fallingTree);
    }
    
    /// <summary>
    /// 倒下并销毁协程（无树桩）
    /// </summary>
    private System.Collections.IEnumerator FallAndDestroyCoroutine(int playerDirection, bool playerFlipX)
    {
        if (spriteRenderer == null)
        {
            DestroyTree();
            yield break;
        }
        
        FallDirection fallDir = DetermineFallDirection(playerDirection, playerFlipX);
        float targetAngle = CalculateTargetAngle(fallDir);
        
        Sprite fallingSprite = spriteRenderer.sprite;
        Vector3 originalWorldPos = spriteRenderer.transform.position;
        Vector3 originalScale = spriteRenderer.transform.localScale;
        Color originalColor = spriteRenderer.color;
        int sortingLayerID = spriteRenderer.sortingLayerID;
        int sortingOrder = spriteRenderer.sortingOrder;
        
        Bounds spriteBounds = spriteRenderer.bounds;
        Vector3 spriteBottomCenter = new Vector3(spriteBounds.center.x, spriteBounds.min.y, 0);
        float spriteHalfHeight = spriteBounds.extents.y;
        
        // 隐藏原始 Sprite
        spriteRenderer.enabled = false;
        
        // 创建临时倒下 Sprite
        GameObject fallingTree = new GameObject("FallingTree_Temp");
        fallingTree.transform.position = originalWorldPos;
        fallingTree.transform.localScale = originalScale;
        
        SpriteRenderer fallingSR = fallingTree.AddComponent<SpriteRenderer>();
        fallingSR.sprite = fallingSprite;
        fallingSR.sortingLayerID = sortingLayerID;
        fallingSR.sortingOrder = sortingOrder;
        fallingSR.color = originalColor;
        
        float elapsed = 0f;
        bool isSidefall = (fallDir == FallDirection.Left || fallDir == FallDirection.Right);
        
        while (elapsed < fallDuration)
        {
            float linearT = elapsed / fallDuration;
            float t = linearT * linearT;
            
            if (isSidefall)
            {
                float angle = targetAngle * t;
                float rad = angle * Mathf.Deg2Rad;
                
                Vector3 centerOffset = new Vector3(0, spriteHalfHeight, 0);
                Vector3 rotatedOffset = new Vector3(
                    centerOffset.x * Mathf.Cos(rad) - centerOffset.y * Mathf.Sin(rad),
                    centerOffset.x * Mathf.Sin(rad) + centerOffset.y * Mathf.Cos(rad),
                    0
                );
                
                Vector3 newCenter = spriteBottomCenter + rotatedOffset;
                fallingTree.transform.position = newCenter;
                fallingTree.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                float scaleY;
                if (t < fallUpStretchPhase)
                {
                    scaleY = Mathf.Lerp(1f, fallUpMaxStretch, t / fallUpStretchPhase);
                }
                else
                {
                    scaleY = Mathf.Lerp(fallUpMaxStretch, fallUpMinScale, (t - fallUpStretchPhase) / (1f - fallUpStretchPhase));
                }
                
                float newHalfHeight = spriteHalfHeight * scaleY;
                float newCenterY = spriteBottomCenter.y + newHalfHeight;
                
                fallingTree.transform.localScale = new Vector3(originalScale.x, originalScale.y * scaleY, originalScale.z);
                fallingTree.transform.position = new Vector3(spriteBottomCenter.x, newCenterY, 0);
            }
            
            // 淡出
            if (linearT > 0.7f)
            {
                float fadeT = (linearT - 0.7f) / 0.3f;
                Color fadeColor = originalColor;
                fadeColor.a = originalColor.a * (1f - fadeT);
                fallingSR.color = fadeColor;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(fallingTree);
        DestroyTree();
    }
    #endregion
    
    #region 公共接口
    public int GetCurrentStageIndex() => currentStageIndex;
    public TreeState GetCurrentState() => currentState;
    public SeasonManager.Season GetCurrentSeason() => currentSeason;
    public int GetCurrentHealth() => currentHealth;
    public int GetCurrentStumpHealth() => currentStumpHealth;
    /// <summary>
    /// 检查是否为冰封树苗
    /// ★ 已废弃：树苗在冬季直接死亡，不再有冰封状态
    /// </summary>
    [System.Obsolete("树苗在冬季直接死亡，不再有冰封状态")]
    public bool IsFrozenSapling() => false;
    
    /// <summary>
    /// 设置枯萎状态
    /// </summary>
    public void SetWithered(bool withered)
    {
        if (withered)
        {
            currentState = TreeState.Withered;
        }
        else if (currentState == TreeState.Withered)
        {
            currentState = TreeState.Normal;
        }
        UpdateSprite();
    }
    
    /// <summary>
    /// 重置树木
    /// </summary>
    public void Reset()
    {
        currentStageIndex = 0;
        currentState = TreeState.Normal;
        isWeatherWithered = false;
        daysInCurrentStage = 0;
        
        if (TimeManager.Instance != null)
        {
            plantedDay = TimeManager.Instance.GetTotalDaysPassed();
            lastCheckDay = -1;
        }
        
        InitializeHealth();
        UpdateSprite();
    }
    #endregion
    
    #region 编辑器
    #if UNITY_EDITOR
    // ★ 运行时调试：缓存上一帧的状态值，用于检测 Inspector 中的修改
    private int _lastRuntimeStageIndex = -1;
    private TreeState _lastRuntimeState = TreeState.Normal;
    private SeasonManager.Season _lastRuntimeSeason = SeasonManager.Season.Spring;
    
    /// <summary>
    /// ★ 运行时 Inspector 调试更新
    /// 在 Update 中检测 Inspector 参数变化并立即更新显示
    /// </summary>
    private void UpdateRuntimeInspectorDebug()
    {
        // 只在编辑器运行时生效
        if (!Application.isPlaying) return;
        if (!editorPreview) return;
        
        bool needUpdate = false;
        
        // 检测阶段变化
        if (currentStageIndex != _lastRuntimeStageIndex)
        {
            _lastRuntimeStageIndex = currentStageIndex;
            InitializeHealth(); // 重新初始化血量
            needUpdate = true;
            if (showDebugInfo)
                Debug.Log($"<color=cyan>[TreeController] {gameObject.name} Inspector调试：阶段变更为 {currentStageIndex}</color>");
        }
        
        // 检测状态变化
        if (currentState != _lastRuntimeState)
        {
            _lastRuntimeState = currentState;
            needUpdate = true;
            if (showDebugInfo)
                Debug.Log($"<color=cyan>[TreeController] {gameObject.name} Inspector调试：状态变更为 {currentState}</color>");
        }
        
        // 检测季节变化（手动修改 Inspector 中的季节）
        if (currentSeason != _lastRuntimeSeason)
        {
            _lastRuntimeSeason = currentSeason;
            needUpdate = true;
            if (showDebugInfo)
                Debug.Log($"<color=cyan>[TreeController] {gameObject.name} Inspector调试：季节变更为 {currentSeason}</color>");
        }
        
        if (needUpdate)
        {
            UpdateSprite();
            UpdatePolygonColliderShape();
        }
    }
    
    private void Update()
    {
        // ★ 运行时 Inspector 调试
        UpdateRuntimeInspectorDebug();
    }
    
    private void OnValidate()
    {
        if (!editorPreview) return;
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null) return;
        }
        
        // 编辑模式下的实时预览
        if (!Application.isPlaying)
        {
            if (currentStageIndex != lastEditorStageIndex || currentState != lastEditorState)
            {
                lastEditorStageIndex = currentStageIndex;
                lastEditorState = currentState;
                UpdateSprite();
            }
        }
        else
        {
            // 运行时：初始化运行时缓存（首次）
            if (_lastRuntimeStageIndex < 0)
            {
                _lastRuntimeStageIndex = currentStageIndex;
                _lastRuntimeState = currentState;
                _lastRuntimeSeason = currentSeason;
            }
        }
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🌱 设置阶段0（树苗）")]
    private static void SetStage0(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null) tree.SetStage(0);
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🌿 设置阶段1")]
    private static void SetStage1(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null) tree.SetStage(1);
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🌲 设置阶段2")]
    private static void SetStage2(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null) tree.SetStage(2);
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🌳 设置阶段3")]
    private static void SetStage3(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null) tree.SetStage(3);
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🌴 设置阶段4")]
    private static void SetStage4(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null) tree.SetStage(4);
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🎄 设置阶段5（完全成熟）")]
    private static void SetStage5(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null) tree.SetStage(5);
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/━━━━━━━━━━━━━━━━", false, 100)]
    private static void Separator1(UnityEditor.MenuCommand command) { }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🪓 测试砍伐", false, 101)]
    private static void TestChop(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null) tree.ChopDown();
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🍂 测试枯萎", false, 102)]
    private static void TestWither(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null) tree.SetWithered(true);
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🔄 重置", false, 103)]
    private static void TestReset(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null) tree.Reset();
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/━━━━━━━━━━━━━━━━ 2", false, 200)]
    private static void Separator2(UnityEditor.MenuCommand command) { }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/📋 应用默认阶段配置", false, 201)]
    private static void ApplyDefaultConfigs(UnityEditor.MenuCommand command)
    {
        var tree = command.context as TreeController;
        if (tree != null)
        {
            var so = new UnityEditor.SerializedObject(tree);
            
            // 应用阶段配置
            var prop = so.FindProperty("stageConfigs");
            var defaults = StageConfigFactory.CreateDefaultConfigs();
            prop.arraySize = defaults.Length;
            
            for (int i = 0; i < defaults.Length; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("daysToNextStage").intValue = defaults[i].daysToNextStage;
                element.FindPropertyRelative("health").intValue = defaults[i].health;
                element.FindPropertyRelative("hasStump").boolValue = defaults[i].hasStump;
                element.FindPropertyRelative("stumpHealth").intValue = defaults[i].stumpHealth;
                element.FindPropertyRelative("enableCollider").boolValue = defaults[i].enableCollider;
                element.FindPropertyRelative("enableOcclusion").boolValue = defaults[i].enableOcclusion;
                element.FindPropertyRelative("acceptedToolType").enumValueIndex = (int)defaults[i].acceptedToolType;
            }
            
            // 应用影子配置
            var shadowProp = so.FindProperty("shadowConfigs");
            var shadowDefaults = ShadowConfig.CreateDefaultConfigs();
            shadowProp.arraySize = shadowDefaults.Length;
            
            for (int i = 0; i < shadowDefaults.Length; i++)
            {
                var element = shadowProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("sprite").objectReferenceValue = shadowDefaults[i].sprite;
                element.FindPropertyRelative("scale").floatValue = shadowDefaults[i].scale;
            }
            
            so.ApplyModifiedProperties();
            Debug.Log($"<color=green>[TreeController] 已应用默认阶段配置和影子配置</color>");
        }
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    #endif
    #endregion
}
