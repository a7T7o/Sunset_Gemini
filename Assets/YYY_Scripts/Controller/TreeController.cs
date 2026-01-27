using UnityEngine;
using System.Collections.Generic;
using FarmGame.Combat;
using FarmGame.Data;
using FarmGame.Events;

/// <summary>
/// 成长阶段（3个）
/// </summary>
public enum GrowthStage
{
    Sapling,    // 树苗
    Small,      // 小树
    Large       // 大树
}

/// <summary>
/// 树的状态
/// </summary>额
public enum TreeState
{
    Normal,         // 正常
    Withered,       // 枯萎
    Frozen,         // 冰封（仅冬季树苗）
    Melted,         // 冰融化（冬季晴天）
    Stump           // 树桩
}

/// <summary>
/// 树木控制器 - 全新五季节系统
/// 
/// GameObject结构（关键）：
/// Tree_M1_00 (父物体) ← 位置 = 树根 = 种植点
/// ├─ Tree (本脚本所在，SpriteRenderer) ← sprite底部对齐父物体中心
/// └─ Shadow (同级兄弟，SpriteRenderer) ← 中心对齐父物体中心
/// 
/// 核心逻辑：
/// - Tree.localY = -sprite.bounds.min.y （让sprite底部在父物体中心）
/// - Shadow.localY = -shadowSprite.bounds.center.y （让Shadow中心在父物体中心）
/// 
/// 总计25个sprite：
/// - 春3 + 夏3 + 早秋3 + 晚秋3 = 12个成长
/// - 春夏树桩1 + 秋树桩1 + 冬树桩1 = 3个树桩
/// - 夏枯萎2 + 秋枯萎2 = 4个枯萎
/// - 冬挂冰3 + 冬融化2 = 5个冬季
/// </summary>
public class TreeController : MonoBehaviour, IResourceNode
{
    [System.Serializable]
    public class SeasonGrowthData
    {
        [Header("成长阶段（3个）")]
        [Tooltip("阶段0：树苗")]
        public Sprite stage0_Sapling;
        
        [Tooltip("阶段1：小树")]
        public Sprite stage1_Small;
        
        [Tooltip("阶段2：大树")]
        public Sprite stage2_Large;
    }
    
    [System.Serializable]
    public class WitherableSeasonData : SeasonGrowthData
    {
        [Header("枯萎状态")]
        [Tooltip("小树枯萎")]
        public Sprite withered_Small;
        
        [Tooltip("大树枯萎")]
        public Sprite withered_Large;
    }
    
    [System.Serializable]
    public class WinterSeasonData
    {
        [Header("冬季挂冰状态（3个阶段）")]
        [Tooltip("树苗挂冰（休眠）")]
        public Sprite frozen_Sapling;
        
        [Tooltip("小树挂冰")]
        public Sprite frozen_Small;
        
        [Tooltip("大树挂冰")]
        public Sprite frozen_Large;
        
        [Header("冬季融化状态（仅树苗）")]
        [Tooltip("树苗融化（显示萎缩状态）")]
        public Sprite melted_Sapling;
        
        [Space(10)]
        [Header("⚠️ 说明")]
        [Tooltip("• 挂冰=下雪天（1,5,11,21,26）\n• 融化=晴天（3,8,17,24,28）\n• Small/Large融化直接用秋季枯萎外观\n• 冬季不成长，春季全部恢复")]
        public bool winterExplanation = true;
    }
    
    [Header("━━━━ 春夏成长数据 ━━━━")]
    [Tooltip("春季（早春 + 晚春早夏）")]
    public SeasonGrowthData spring;
    
    [Tooltip("夏季（晚春早夏 + 晚夏早秋，可枯萎）")]
    public WitherableSeasonData summer;
    
    [Header("━━━━ 秋季成长数据（两套）━━━━")]
    [Tooltip("早秋（晚夏早秋，可枯萎）")]
    public WitherableSeasonData fall_Early;
    
    [Tooltip("晚秋（单独使用）")]
    public SeasonGrowthData fall_Late;
    
    [Header("━━━━ 冬季数据 ━━━━")]
    [Tooltip("冬季（挂冰/融化两种状态）")]
    public WinterSeasonData winter;
    
    [Header("━━━━ 树桩状态（3种）━━━━")]
    [Tooltip("春夏共用树桩")]
    public Sprite stump_SpringSummer;
    
    [Tooltip("秋季树桩")]
    public Sprite stump_Fall;
    
    [Tooltip("冬季树桩")]
    public Sprite stump_Winter;
    
    [Header("━━━━ 当前状态 ━━━━")]
    [Tooltip("树木ID（基于InstanceID，0-9999循环）")]
    [SerializeField] private int treeID = -1;
    
    [Tooltip("当前日历季节（只读，由SeasonManager控制）")]
    [SerializeField] private SeasonManager.Season currentSeason = SeasonManager.Season.Spring;
    
    [Tooltip("当前成长阶段（可调试）")]
    public GrowthStage currentStage = GrowthStage.Large;
    
    [Tooltip("当前树的状态（可调试）")]
    public TreeState currentState = TreeState.Normal;
    
    [Header("━━━━ 成长设置 ━━━━")]
    [Tooltip("是否启用自动成长（基于天数）")]
    public bool autoGrow = true;
    
    [Tooltip("树苗成长为小树需要的天数")]
    public int daysToStage1 = 2;
    
    [Tooltip("小树成长为大树需要的天数")]
    public int daysToStage2 = 3;
    
    [Tooltip("种植日期（游戏开始后的第几天，0=未种植）")]
    [SerializeField] private int plantedDay = 0;
    
    [Header("━━━━ 影子缩放（自动应用到同级Shadow）━━━━")]
    [Tooltip("⚠️ 只有小树和大树有影子，树苗和树桩无影子")]
    public bool shadowExplanation = true;
    
    [Tooltip("小树阶段的影子缩放（0.0-2.0）")]
    [Range(0f, 2f)]
    public float shadowScaleStage1 = 0.8f;
    
    [Tooltip("大树阶段的影子缩放（0.0-2.0）")]
    [Range(0f, 2f)]
    public float shadowScaleStage2 = 1.0f;
    
    [Header("━━━━ Sprite底部对齐 ━━━━")]
    [Tooltip("是否自动对齐Sprite底部到父物体位置（种植点）")]
    public bool alignSpriteBottom = true;
    
    [Header("━━━━ 砍伐设置 ━━━━")]
    [Tooltip("小树需要砍伐的次数")]
    [Range(1, 10)]
    public int chopCountSmall = 3;
    
    [Tooltip("大树需要砍伐的次数")]
    [Range(1, 20)]
    public int chopCountLarge = 7;
    
    [Tooltip("当前剩余砍伐次数")]
    [SerializeField] private int currentChopCount = 0;
    
    [Header("━━━━ 倒下动画 ━━━━")]
    [Tooltip("是否启用倒下动画")]
    [SerializeField] private bool enableFallAnimation = true;
    
    [Tooltip("倒下动画时长（秒）")]
    [Range(0.5f, 2f)]
    [SerializeField] private float fallDuration = 0.8f;
    
    [Header("向上倒参数（可调试）")]
    [Tooltip("Y轴最大拉长倍数")]
    [Range(1f, 3f)]
    [SerializeField] private float fallUpMaxStretch = 1.2f;
    
    [Tooltip("Y轴最终缩放倍数（1=不缩放）")]
    [Range(0.01f, 2f)]
    [SerializeField] private float fallUpMinScale = 1f;
    
    [Tooltip("拉长阶段占比（0-1）")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float fallUpStretchPhase = 0.4f;
    
    // 记录最后一次命中时玩家的朝向（0=Down, 1=Up, 2=Side）和 flipX
    // ✅ 修正：Direction 参数来自 PlayerAnimController.ConvertToAnimatorDirection
    private int lastHitPlayerDirection = 0;
    private bool lastHitPlayerFlipX = false;
    
    [Header("━━━━ 掉落设置 ━━━━")]
    [Tooltip("掉落表（定义砍伐后掉落的物品）")]
    [SerializeField] private FarmGame.Data.DropTable dropTable;
    
    [Header("━━━━ 音效设置 ━━━━")]
    [Tooltip("砍击音效（每次命中播放）")]
    [SerializeField] private AudioClip chopHitSound;
    
    [Tooltip("砍倒音效（树木倒下时播放）")]
    [SerializeField] private AudioClip chopFellSound;
    
    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.8f;
    
    [Header("━━━━ 调试 ━━━━")]
    [SerializeField] private bool showDebugInfo = false;
    
    [Tooltip("编辑器实时预览（Inspector修改时自动更新）")]
    public bool editorPreview = true;
    
    internal SpriteRenderer spriteRenderer;
    private OcclusionTransparency occlusionTransparency; // 遮挡透明组件引用
    private int lastCheckDay = -1;
    private bool isWeatherWithered = false; // 天气导致的枯萎（区分手动枯萎）
    private bool isFrozenSapling = false;   // 冬季冰封的树苗（春季可恢复）
    
    // 编辑器预览
    #if UNITY_EDITOR
    private GrowthStage lastEditorStage;
    private TreeState lastEditorState;
    #endif
    
    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"[TreeController] {gameObject.name} 缺少SpriteRenderer组件！（请确保Tree子物体上有SpriteRenderer）");
            enabled = false;
            return;
        }
        
        // ✅ 缓存 OcclusionTransparency 组件引用
        occlusionTransparency = GetComponent<OcclusionTransparency>();
        
        // ✅ 基于InstanceID生成树木ID（0-9999循环）
        treeID = Mathf.Abs(gameObject.GetInstanceID()) % 10000;
        
        // 初始化编辑器预览变量
        #if UNITY_EDITOR
        lastEditorStage = currentStage;
        lastEditorState = currentState;
        #endif
        
        // 订阅SeasonManager
        SeasonManager.OnSeasonChanged += OnSeasonChangedByManager;
        SeasonManager.OnVegetationSeasonChanged += OnVegetationSeasonChangedByManager;
        
        // 同步当前季节
        if (SeasonManager.Instance != null)
        {
            currentSeason = SeasonManager.Instance.GetCurrentSeason();
        }
        
        // 订阅TimeManager（成长）
        if (autoGrow)
        {
            TimeManager.OnDayChanged += OnDayChangedByTimeManager;
            
            if (plantedDay == 0 && TimeManager.Instance != null)
            {
                plantedDay = TimeManager.Instance.GetTotalDaysPassed();
            }
        }
        
        // 订阅WeatherSystem
        WeatherSystem.OnPlantsWither += OnWeatherWither;
        WeatherSystem.OnPlantsRecover += OnWeatherRecover;
        WeatherSystem.OnWinterSnow += OnWinterSnow;
        WeatherSystem.OnWinterMelt += OnWinterMelt;
        
        // 初始检查天气
        if (WeatherSystem.Instance != null && WeatherSystem.Instance.IsWithering())
        {
            OnWeatherWither();
        }
        
        // ✅ 初始化显示（持续重试直到SeasonManager就绪）
        StartCoroutine(WaitForSeasonManagerAndInitialize());
        
        // ✅ 注册到资源节点注册表
        if (ResourceNodeRegistry.Instance != null)
        {
            ResourceNodeRegistry.Instance.Register(this, gameObject.GetInstanceID());
        }
    }
    
    /// <summary>
    /// 等待SeasonManager初始化完成后再初始化显示
    /// </summary>
    private System.Collections.IEnumerator WaitForSeasonManagerAndInitialize()
    {
        int retryCount = 0;
        while (SeasonManager.Instance == null && retryCount < 100)
        {
            retryCount++;
            yield return null; // 等待一帧
        }

        if (SeasonManager.Instance == null)
        {
            Debug.LogError($"[TreeController] {transform.parent?.name}/{gameObject.name} - SeasonManager初始化超时", gameObject);
            yield break;
        }

        InitializeDisplay();
    }
    
    /// <summary>
    /// 初始化显示（确保在SeasonManager就绪后调用）
    /// </summary>
    private void InitializeDisplay()
    {
        if (SeasonManager.Instance == null)
        {
            Debug.LogError($"<color=red>❌ [{transform.parent.name}/{gameObject.name}] SeasonManager仍未初始化！</color>", gameObject);
            return;
        }
        
        // 同步当前季节（如果Start时未能同步）
        if (currentSeason == SeasonManager.Season.Spring && SeasonManager.Instance.GetCurrentSeason() != SeasonManager.Season.Spring)
        {
            currentSeason = SeasonManager.Instance.GetCurrentSeason();
        }
        
        UpdateSprite();
    }
    
    void OnDestroy()
    {
        SeasonManager.OnSeasonChanged -= OnSeasonChangedByManager;
        SeasonManager.OnVegetationSeasonChanged -= OnVegetationSeasonChangedByManager;
        TimeManager.OnDayChanged -= OnDayChangedByTimeManager;
        WeatherSystem.OnPlantsWither -= OnWeatherWither;
        WeatherSystem.OnPlantsRecover -= OnWeatherRecover;
        WeatherSystem.OnWinterSnow -= OnWinterSnow;
        WeatherSystem.OnWinterMelt -= OnWinterMelt;
        
        // ✅ 从资源节点注册表注销
        if (ResourceNodeRegistry.Instance != null)
        {
            ResourceNodeRegistry.Instance.Unregister(gameObject.GetInstanceID());
        }
    }
    
    /// <summary>
    /// VegetationSeasonManager植被季节变化回调（由全局管理器通知）
    /// </summary>
    private void OnVegetationSeasonChangedByManager()
    {
        // 植被季节由VegetationSeasonManager全局管理，这里只需更新显示
        UpdateSprite();
    }
    
    /// <summary>
    /// SeasonManager季节变化回调
    /// </summary>
    private void OnSeasonChangedByManager(SeasonManager.Season newSeason)
    {
        currentSeason = newSeason;
        
        // ✅ 春季：所有枯萎植物复苏（保持成长阶段）
        if (newSeason == SeasonManager.Season.Spring)
        {
            if (isFrozenSapling)
            {
                isFrozenSapling = false;
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=lime>[TreeController] {gameObject.name} 春季到来，冰封树苗解冻！</color>");
                }
            }
            
            // 所有枯萎状态恢复正常
            if (currentState == TreeState.Withered || currentState == TreeState.Frozen || currentState == TreeState.Melted)
            {
                currentState = TreeState.Normal;
                isWeatherWithered = false;
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=lime>[TreeController] {gameObject.name} 春季复苏！阶段保持: {currentStage}</color>");
                }
            }
        }
        
        // 冬季：树苗冰封，其他进入枯萎
        if (newSeason == SeasonManager.Season.Winter)
        {
            if (currentStage == GrowthStage.Sapling && currentState == TreeState.Normal)
            {
                isFrozenSapling = true;
                currentState = TreeState.Frozen;
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=cyan>[TreeController] {gameObject.name} 冬季到来，树苗冰封！</color>");
                }
            }
        }
    }
    
    /// <summary>
    /// TimeManager每日回调
    /// </summary>
    private void OnDayChangedByTimeManager(int year, int seasonDay, int totalDays)
    {
        // 成长检查
        if (lastCheckDay == totalDays) return;
        lastCheckDay = totalDays;
        
        if (currentState != TreeState.Normal) return;
        if (currentStage == GrowthStage.Large) return;
        
        // 冬季不成长
        if (currentSeason == SeasonManager.Season.Winter) return;
        
        // 枯萎时不成长
        if (isWeatherWithered) return;
        
        int daysSincePlanted = totalDays - plantedDay;
        int requiredDays = GetRequiredDaysForNextStage();
        
        if (daysSincePlanted >= requiredDays)
        {
            Grow();
            plantedDay = totalDays;
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=lime>[TreeController] {gameObject.name} 成长！{currentStage}</color>");
            }
        }
    }
    
    /// <summary>
    /// 天气枯萎回调
    /// </summary>
    private void OnWeatherWither()
    {
        if (currentState == TreeState.Normal)
        {
            isWeatherWithered = true;
            currentState = TreeState.Withered;
            UpdateSprite();
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=red>[TreeController] {gameObject.name} 因天气枯萎</color>");
            }
        }
    }
    
    /// <summary>
    /// 天气恢复回调
    /// </summary>
    private void OnWeatherRecover()
    {
        if (isWeatherWithered)
        {
            isWeatherWithered = false;
            currentState = TreeState.Normal;
            UpdateSprite();
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=green>[TreeController] {gameObject.name} 天气恢复</color>");
            }
        }
    }
    
    /// <summary>
    /// 冬季下雪回调（树苗休眠，挂冰）
    /// </summary>
    private void OnWinterSnow()
    {
        if (currentSeason != SeasonManager.Season.Winter) return;
        
        if (currentStage == GrowthStage.Sapling)
        {
            // 树苗冰封
            isFrozenSapling = true;
            currentState = TreeState.Frozen;
        }
        else
        {
            // Small/Large进入冰封状态
            currentState = TreeState.Frozen;
        }
        
        UpdateSprite();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[TreeController] {gameObject.name} 下雪天，进入冰封状态</color>");
        }
    }
    
    /// <summary>
    /// 冬季融化回调（大太阳，冰雪融化）
    /// </summary>
    private void OnWinterMelt()
    {
        if (currentSeason != SeasonManager.Season.Winter) return;
        
        // 进入融化状态
        currentState = TreeState.Melted;
        
        UpdateSprite();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 大太阳，冰雪融化</color>");
        }
    }
    
    /// <summary>
    /// 获取成长到下一阶段需要的天数
    /// </summary>
    private int GetRequiredDaysForNextStage()
    {
        return currentStage switch
        {
            GrowthStage.Sapling => daysToStage1,  // 树苗→小树
            GrowthStage.Small => daysToStage2,    // 小树→大树
            _ => int.MaxValue                      // 大树不再成长
        };
    }
    
    /// <summary>
    /// 更新Sprite显示
    /// </summary>
    public void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        Sprite targetSprite = GetCurrentSprite();
        var vegSeason = SeasonManager.Instance != null ? SeasonManager.Instance.GetCurrentVegetationSeason() : SeasonManager.VegetationSeason.Spring;
        
        if (targetSprite != null)
        {
            spriteRenderer.sprite = targetSprite;
            spriteRenderer.enabled = true;
            
            // ✅ 对齐sprite底部和更新Shadow
            if (alignSpriteBottom)
            {
                AlignSpriteBottom();
            }
            UpdateShadowScale();
        }
        else
        {
            // 冬季融化的树苗 → 隐藏
            if (currentSeason == SeasonManager.Season.Winter && currentStage == GrowthStage.Sapling && currentState == TreeState.Melted)
            {
                spriteRenderer.enabled = false;
                UpdateShadowScale(); // ← 也要更新Shadow
            }
            else
            {
                UpdateShadowScale(); // ← 无论如何都要更新Shadow
            }
        }
    }
    
    /// <summary>
    /// 获取当前应该显示的Sprite
    /// </summary>
    private Sprite GetCurrentSprite()
    {
        // ✅ 从SeasonManager获取当前植被季节
        if (SeasonManager.Instance == null)
        {
            // 💡 编辑器下或游戏启动初期，SeasonManager可能未初始化，这是正常的
            // 只在游戏运行且超过1秒后才报错
            if (Application.isPlaying && Time.timeSinceLevelLoad > 1f)
            {
                Debug.LogError($"<color=red>❌ [{transform.parent?.name}/{gameObject.name}] SeasonManager.Instance == null！</color>", gameObject);
            }
            return null;
        }
        
        SeasonManager.VegetationSeason vegSeason = SeasonManager.Instance.GetCurrentVegetationSeason();
        
        // 树桩状态
        if (currentState == TreeState.Stump)
        {
            return vegSeason switch
            {
                SeasonManager.VegetationSeason.Spring => stump_SpringSummer,
                SeasonManager.VegetationSeason.Summer => stump_SpringSummer,
                SeasonManager.VegetationSeason.EarlyFall => stump_Fall,
                SeasonManager.VegetationSeason.LateFall => stump_Fall,
                SeasonManager.VegetationSeason.Winter => stump_Winter,
                _ => stump_SpringSummer
            };
        }
        
        // 冬季特殊处理
        if (vegSeason == SeasonManager.VegetationSeason.Winter)
        {
            return GetWinterSprite();
        }
        
        // 枯萎状态
        if (currentState == TreeState.Withered)
        {
            return GetWitheredSprite();
        }
        
        // 正常成长状态
        return GetNormalSprite();
    }
    
    /// <summary>
    /// 获取冬季Sprite
    /// </summary>
    private Sprite GetWinterSprite()
    {
        // 冰封状态（挂冰）- 下雪天
        if (currentState == TreeState.Frozen || currentState == TreeState.Normal)
        {
            return currentStage switch
            {
                GrowthStage.Sapling => winter.frozen_Sapling,
                GrowthStage.Small => winter.frozen_Small,
                GrowthStage.Large => winter.frozen_Large,
                _ => null
            };
        }
        
        // 融化状态（晴天）- 树苗单独sprite，Small/Large用秋季枯萎
        if (currentState == TreeState.Melted)
        {
            return currentStage switch
            {
                GrowthStage.Sapling => winter.melted_Sapling, // ✅ 树苗单独融化sprite
                GrowthStage.Small => fall_Early.withered_Small, // ✅ 直接用秋季枯萎
                GrowthStage.Large => fall_Early.withered_Large, // ✅ 直接用秋季枯萎
                _ => null
            };
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取枯萎Sprite（枯萎的树也跟随季节外观）
    /// </summary>
    private Sprite GetWitheredSprite()
    {
        // 树苗不显示枯萎，直接消失
        if (currentStage == GrowthStage.Sapling) return null;
        
        // ✅ 从SeasonManager获取当前植被季节
        if (SeasonManager.Instance == null) return null;
        SeasonManager.VegetationSeason vegSeason = SeasonManager.Instance.GetCurrentVegetationSeason();
        
        // ✅ 枯萎状态跟随季节外观
        switch (vegSeason)
        {
            case SeasonManager.VegetationSeason.Spring:
                // 春季不应有枯萎（春季复苏），降级为夏季枯萎
                return currentStage switch
                {
                    GrowthStage.Small => summer.withered_Small,
                    GrowthStage.Large => summer.withered_Large,
                    _ => null
                };
                
            case SeasonManager.VegetationSeason.Summer:
                // 夏季：夏季枯萎外观
                return currentStage switch
                {
                    GrowthStage.Small => summer.withered_Small,
                    GrowthStage.Large => summer.withered_Large,
                    _ => null
                };
                
            case SeasonManager.VegetationSeason.EarlyFall:
                // 早秋：枯萎植物也按比例渐变（使用固定随机值）
                // ✅ 使用treeID生成固定随机值
                int seed = treeID + (int)currentStage * 100;
                Random.InitState(seed);
                float treeSeedValue = Random.value;
                
                // ✅ 从SeasonManager获取过渡进度
                float progress = SeasonManager.Instance.GetTransitionProgress();
                
                // 根据进度判断显示哪个季节的枯萎外观
                if (treeSeedValue < progress)
                {
                    // 显示秋季枯萎外观
                    return currentStage switch
                    {
                        GrowthStage.Small => fall_Early.withered_Small,
                        GrowthStage.Large => fall_Early.withered_Large,
                        _ => null
                    };
                }
                else
                {
                    // 显示夏季枯萎外观
                    return currentStage switch
                    {
                        GrowthStage.Small => summer.withered_Small,
                        GrowthStage.Large => summer.withered_Large,
                        _ => null
                    };
                }
                
            case SeasonManager.VegetationSeason.LateFall:
                // 晚秋：秋季枯萎外观
                return currentStage switch
                {
                    GrowthStage.Small => fall_Early.withered_Small,
                    GrowthStage.Large => fall_Early.withered_Large,
                    _ => null
                };
                
            case SeasonManager.VegetationSeason.Winter:
                // 冬季：秋季枯萎外观
                return currentStage switch
                {
                    GrowthStage.Small => fall_Early.withered_Small,
                    GrowthStage.Large => fall_Early.withered_Large,
                    _ => null
                };
                
            default:
                return null;
        }
    }
    
    /// <summary>
    /// 获取正常成长Sprite（基于渐变进度）
    /// </summary>
    private Sprite GetNormalSprite()
    {
        // ✅ 从SeasonManager获取当前植被季节
        if (SeasonManager.Instance == null) return null;
        SeasonManager.VegetationSeason vegSeason = SeasonManager.Instance.GetCurrentVegetationSeason();
        
        Sprite targetSprite = null;
        
        switch (vegSeason)
        {
            case SeasonManager.VegetationSeason.Spring:
                // 100%春季
                targetSprite = GetSeasonSprite(spring);
                break;
                
            case SeasonManager.VegetationSeason.Summer:
                // 渐变：春季 → 夏季（基于进度）
                targetSprite = GetTransitionSprite(spring, summer);
                break;
                
            case SeasonManager.VegetationSeason.EarlyFall:
                // 渐变：夏季 → 早秋（基于进度）
                targetSprite = GetTransitionSprite(summer, fall_Early);
                break;
                
            case SeasonManager.VegetationSeason.LateFall:
                // 100%晚秋
                targetSprite = GetSeasonSprite(fall_Late);
                break;
                
            case SeasonManager.VegetationSeason.Winter:
                // 冬季不应走这里，降级为晚秋
                targetSprite = GetSeasonSprite(fall_Late);
                break;
        }
        
        return targetSprite;
    }
    
    /// <summary>
    /// 获取单季节Sprite
    /// </summary>
    private Sprite GetSeasonSprite(SeasonGrowthData seasonData)
    {
        if (seasonData == null)
        {
            Debug.LogError($"<color=red>❌ [{transform.parent.name}/{gameObject.name}] GetSeasonSprite: seasonData为NULL！</color>\n" +
                          $"当前Stage: {currentStage}, State: {currentState}\n" +
                          $"这意味着对应季节的字段（spring/summer/fall等）为null！", gameObject);
            return null;
        }
        
        Sprite result = currentStage switch
        {
            GrowthStage.Sapling => seasonData.stage0_Sapling,
            GrowthStage.Small => seasonData.stage1_Small,
            GrowthStage.Large => seasonData.stage2_Large,
            _ => null
        };
        
        if (result == null)
        {
            Debug.LogError($"<color=red>❌ [{transform.parent.name}/{gameObject.name}] GetSeasonSprite: sprite为NULL！</color>\n" +
                          $"seasonData存在但stage{(int)currentStage}的sprite为null\n" +
                          $"当前Stage: {currentStage}\n" +
                          $"stage0_Sapling: {(seasonData.stage0_Sapling != null ? "✓" : "✗")}\n" +
                          $"stage1_Small: {(seasonData.stage1_Small != null ? "✓" : "✗")}\n" +
                          $"stage2_Large: {(seasonData.stage2_Large != null ? "✓" : "✗")}", gameObject);
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取过渡季节Sprite（基于进度渐变选择）
    /// </summary>
    private Sprite GetTransitionSprite(SeasonGrowthData season1, SeasonGrowthData season2)
    {
        if (season1 == null || season2 == null) return GetSeasonSprite(season1);
        
        // ✅ 使用treeID + 阶段作为随机种子
        int seed = treeID + (int)currentStage * 100;
        Random.InitState(seed);
        
        // 生成一个固定的随机值（0-1），用于判断该树属于哪个季节外观
        float treeSeedValue = Random.value;
        
        // ✅ 从SeasonManager获取过渡进度
        if (SeasonManager.Instance == null) return GetSeasonSprite(season1);
        float progress = SeasonManager.Instance.GetTransitionProgress();
        
        // 根据progress判断显示哪个季节
        // 例如：progress=0.3时，30%的树显示season2，70%显示season1
        if (treeSeedValue < progress)
        {
            // 显示season2（下一季节）
            return GetSeasonSprite(season2);
        }
        else
        {
            // 显示season1（当前季节）
            return GetSeasonSprite(season1);
        }
    }
    
    /// <summary>
    /// 对齐Sprite底部到父物体中心（树根位置）
    /// ✅ 同时更新Collider状态
    /// </summary>
    private void AlignSpriteBottom()
    {
        if (!alignSpriteBottom) return;
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        
        // ✅ 核心逻辑：让sprite底部对齐父物体中心（0,0,0）
        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        float spriteBottomOffset = spriteBounds.min.y;
        
        Vector3 localPos = spriteRenderer.transform.localPosition;
        localPos.y = -spriteBottomOffset;
        spriteRenderer.transform.localPosition = localPos;
        
        // ✅ 更新Collider状态
        UpdateColliderState();
    }
    
    /// <summary>
    /// 更新Collider状态
    /// ✅ Sapling阶段：禁用Collider + 禁用OcclusionTransparency
    /// ✅ Small/Large阶段：启用Collider + 启用OcclusionTransparency
    /// ✅ Stump阶段：禁用OcclusionTransparency
    /// </summary>
    private void UpdateColliderState()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        if (colliders.Length == 0) return;
        
        bool hadEnabledCollider = false;
        bool hasEnabledCollider = false;
        
        // 记录状态变化前的碰撞体状态
        foreach (Collider2D collider in colliders)
        {
            if (collider.enabled) hadEnabledCollider = true;
        }
        
        // ✅ 树苗阶段：禁用所有Collider + 禁用遮挡透明
        if (currentStage == GrowthStage.Sapling)
        {
            foreach (Collider2D collider in colliders)
            {
                collider.enabled = false;
            }
            
            // 禁用遮挡透明
            if (occlusionTransparency != null)
            {
                occlusionTransparency.SetCanBeOccluded(false);
            }
        }
        // ✅ 树桩阶段：启用Collider + 禁用遮挡透明
        else if (currentState == TreeState.Stump)
        {
            foreach (Collider2D collider in colliders)
            {
                collider.enabled = true;
                hasEnabledCollider = true;
                
                if (collider is PolygonCollider2D poly && spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    UpdatePolygonColliderFromSprite(poly, spriteRenderer.sprite);
                }
            }
            
            // 禁用遮挡透明
            if (occlusionTransparency != null)
            {
                occlusionTransparency.SetCanBeOccluded(false);
            }
        }
        else
        {
            // ✅ Small/Large阶段：启用Collider + 启用遮挡透明
            foreach (Collider2D collider in colliders)
            {
                collider.enabled = true;
                hasEnabledCollider = true;
                
                // ✅ 如果是PolygonCollider2D，从当前Sprite的Custom Physics Shape更新形状
                if (collider is PolygonCollider2D poly && spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    UpdatePolygonColliderFromSprite(poly, spriteRenderer.sprite);
                }
            }
            
            // 启用遮挡透明
            if (occlusionTransparency != null)
            {
                occlusionTransparency.SetCanBeOccluded(true);
            }
        }
        
        // ✅ 如果碰撞体状态改变（禁用→启用 或 启用→禁用），通知NavGrid2D刷新
        if (hadEnabledCollider != hasEnabledCollider)
        {
            RequestNavGridRefresh();
        }
    }
    
    /// <summary>
    /// 请求NavGrid2D刷新网格（延迟执行，避免重复刷新）
    /// </summary>
    private void RequestNavGridRefresh()
    {
        // 延迟0.2秒刷新，给碰撞体足够的时间更新
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
        {
            Debug.Log($"<color=cyan>[TreeController] {gameObject.name} 通知NavGrid2D刷新网格</color>");
        }
    }
    
    /// <summary>
    /// 从Sprite的Custom Physics Shape更新PolygonCollider2D
    /// </summary>
    private void UpdatePolygonColliderFromSprite(PolygonCollider2D poly, Sprite sprite)
    {
        if (poly == null || sprite == null) return;
        
        // ✅ 获取Sprite的物理形状数量
        int shapeCount = sprite.GetPhysicsShapeCount();
        
        if (shapeCount == 0)
        {
            // 如果Sprite没有Custom Physics Shape，使用默认形状（Sprite边界）
            poly.pathCount = 0; // 清空现有路径
            return;
        }
        
        // ✅ 设置path数量
        poly.pathCount = shapeCount;
        
        // ✅ 为每个shape创建路径
        List<Vector2> physicsShape = new List<Vector2>();
        for (int i = 0; i < shapeCount; i++)
        {
            physicsShape.Clear();
            sprite.GetPhysicsShape(i, physicsShape);
            poly.SetPath(i, physicsShape);
        }
        
        // ✅ 重置offset为(0,0)，让Collider完全跟随Sprite
        poly.offset = Vector2.zero;
    }
    
    /// <summary>
    /// 更新Shadow显示状态、缩放和位置
    /// ✅ Shadow中心对齐父物体中心（树根位置）
    /// </summary>
    private void UpdateShadowScale()
    {
        // Shadow和Tree是同级，都在父物体下
        if (transform.parent == null) return;
        
        Transform shadowTransform = transform.parent.Find("Shadow");
        if (shadowTransform == null) return;
        
        SpriteRenderer shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null) return;
        
        // ✅ 树苗和树桩无影子
        if (currentStage == GrowthStage.Sapling || currentState == TreeState.Stump)
        {
            shadowRenderer.enabled = false;
            return;
        }
        
        // ✅ 小树和大树启用并设置缩放
        shadowRenderer.enabled = true;
        
        float targetScale = currentStage switch
        {
            GrowthStage.Small => shadowScaleStage1,
            GrowthStage.Large => shadowScaleStage2,
            _ => shadowScaleStage2
        };
        
        shadowTransform.localScale = new Vector3(targetScale, targetScale, 1f);
        
        // ✅ Shadow中心对齐父物体中心（树根）
        // 如果Shadow sprite的pivot在中心（通常情况），直接设置为0即可
        // 如果pivot不在中心，需要根据bounds.center计算偏移
        if (shadowRenderer.sprite != null)
        {
            Bounds shadowBounds = shadowRenderer.sprite.bounds;
            
            // Shadow几何中心相对于pivot的偏移
            float centerOffset = shadowBounds.center.y;
            
            // 让Shadow几何中心对齐父物体中心
            Vector3 shadowPos = shadowTransform.localPosition;
            shadowPos.y = -centerOffset;
            shadowTransform.localPosition = shadowPos;
        }
    }
    
    /// <summary>
    /// 成长到下一阶段
    /// </summary>
    public void Grow()
    {
        if (currentStage == GrowthStage.Sapling)
        {
            currentStage = GrowthStage.Small;
        }
        else if (currentStage == GrowthStage.Small)
        {
            currentStage = GrowthStage.Large;
        }
        
        UpdateSprite();
    }
    
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
    
    #region IResourceNode 接口实现
    
    /// <summary>
    /// 资源类型标识
    /// </summary>
    public string ResourceTag => "Tree";
    
    /// <summary>
    /// 资源是否已耗尽
    /// </summary>
    public bool IsDepleted => currentState == TreeState.Stump || currentStage == GrowthStage.Sapling;
    
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
    /// 获取当前树木的阶段值（用于等级判定）
    /// GrowthStage 枚举转换为 0-5 的整数
    /// </summary>
    private int GetTreeStageValue()
    {
        // GrowthStage: Sapling=0, Small=1, Large=2
        // 但我们需要支持 0-5 的阶段系统
        // 这里直接使用枚举的整数值
        return (int)currentStage;
    }
    
    /// <summary>
    /// 检查是否接受此工具类型（用于判断是否扣血）
    /// </summary>
    public bool CanAccept(ToolHitContext ctx)
    {
        // 只有斧头能对树木造成伤害
        if (ctx.toolType != ToolType.Axe) return false;
        
        // 树桩不能再砍
        if (currentState == TreeState.Stump) return false;
        
        // 树苗不能砍
        if (currentStage == GrowthStage.Sapling) return false;
        
        // ★ 检查斧头等级是否足够
        int axeTier = GetAxeTier(ctx);
        int treeStage = GetTreeStageValue();
        if (!FarmGame.Utils.MaterialTierHelper.CanChopTree(axeTier, treeStage))
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 处理命中效果
    /// </summary>
    public void OnHit(ToolHitContext ctx)
    {
        // 树桩和树苗不响应
        if (currentState == TreeState.Stump) return;
        if (currentStage == GrowthStage.Sapling) return;
        
        // ✅ 记录玩家朝向（用于倒下动画）
        // 从 ToolHitContext 的 attacker 获取玩家的 Animator
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
        
        // 判断是否是正确的工具（斧头）
        bool isCorrectTool = CanAccept(ctx);
        
        // ✅ 计算被砍方向（从玩家朝向推断）
        // 玩家在右边砍 → 树被从右边砍 → 应该向左倒
        Vector2 chopDirection = -ctx.hitDir; // 反向就是被砍的方向
        
        if (isCorrectTool)
        {
            // ✅ 消耗精力（只有斧头砍树才消耗精力）
            float energyCost = 2f; // 默认消耗2点精力
            
            // 从 ToolData 获取精力消耗（如果有的话）
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
            
            // 尝试消耗精力
            bool hasEnergy = true;
            if (EnergySystem.Instance != null)
            {
                hasEnergy = EnergySystem.Instance.TryConsumeEnergy(Mathf.RoundToInt(energyCost));
            }
            
            if (!hasEnergy)
            {
                // 精力不足，只播放抖动效果，不扣血
                PlayHitEffect(chopDirection);
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 精力不足，无法砍伐</color>");
                }
                return;
            }
            
            // ✅ 设置砍伐状态（透明度加深，更不透明）
            if (occlusionTransparency != null)
            {
                occlusionTransparency.SetChoppingState(true, 0.25f);
            }
            
            // 斧头：扣血 + 抖动 + 树叶 + 音效
            int damage = Mathf.Max(1, Mathf.RoundToInt(ctx.baseDamage));
            bool felled = TakeDamage(damage);
            
            if (!felled)
            {
                PlayHitEffect(chopDirection);
                SpawnLeafParticles();
                PlayChopHitSound();
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=yellow>[TreeController] {gameObject.name} 受到 {damage} 点伤害，剩余 {currentChopCount} 次，消耗精力 {energyCost}</color>");
            }
        }
        else
        {
            // 检查是否是斧头但等级不足
            if (ctx.toolType == ToolType.Axe)
            {
                int axeTier = GetAxeTier(ctx);
                int treeStage = GetTreeStageValue();
                int requiredTier = FarmGame.Utils.MaterialTierHelper.GetRequiredAxeTier(treeStage);
                
                // 斧头等级不足：播放抖动 + 提示
                PlayHitEffect(chopDirection);
                
                if (showDebugInfo)
                {
                    string axeName = FarmGame.Utils.MaterialTierHelper.GetTierName(axeTier);
                    string requiredName = FarmGame.Utils.MaterialTierHelper.GetTierName(requiredTier);
                    Debug.Log($"<color=orange>[TreeController] {gameObject.name} 斧头等级不足！当前: {axeName}({axeTier}), 需要: {requiredName}({requiredTier})</color>");
                }
                
                // TODO: 可以在这里播放"叮"的音效或显示 UI 提示
            }
            else
            {
                // 其他工具：只抖动，不扣血
                PlayHitEffect(chopDirection);
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=gray>[TreeController] {gameObject.name} 被非斧头工具击中，只抖动</color>");
                }
            }
        }
    }
    
    /// <summary>
    /// 获取检测边界（Sprite Bounds）
    /// </summary>
    public Bounds GetBounds()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.bounds;
        }
        
        // 返回一个默认的小边界
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
    
    /// <summary>
    /// 获取资源节点位置（树根位置）
    /// </summary>
    public Vector3 GetPosition()
    {
        return transform.parent != null ? transform.parent.position : transform.position;
    }
    
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
        
        // 根据命中方向决定抖动方向
        float shakeDir = hitDir.x != 0 ? Mathf.Sign(hitDir.x) : 1f;
        
        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            float damping = 1f - progress; // 衰减
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
        // 如果有 LeafSpawner 组件则调用
        var leafSpawner = GetComponent<LeafSpawner>();
        if (leafSpawner != null)
        {
            leafSpawner.SpawnLeaves(GetBounds());
        }
    }
    
    /// <summary>
    /// 播放砍击音效
    /// </summary>
    private void PlayChopHitSound()
    {
        if (chopHitSound != null)
        {
            Vector3 pos = GetPosition();
            AudioSource.PlayClipAtPoint(chopHitSound, pos, soundVolume);
        }
    }
    
    /// <summary>
    /// 播放砍倒音效
    /// </summary>
    private void PlayChopFellSound()
    {
        if (chopFellSound != null)
        {
            Vector3 pos = GetPosition();
            AudioSource.PlayClipAtPoint(chopFellSound, pos, soundVolume);
        }
    }
    
    #endregion
    
    /// <summary>
    /// 对树木造成伤害（砍伐）
    /// </summary>
    /// <param name="damage">伤害值（默认1）</param>
    /// <returns>是否已砍倒</returns>
    public bool TakeDamage(int damage = 1)
    {
        if (currentState == TreeState.Stump) return true;
        if (currentStage == GrowthStage.Sapling) return true; // 树苗不能砍
        
        // 初始化砍伐次数
        if (currentChopCount <= 0)
        {
            currentChopCount = currentStage == GrowthStage.Small ? chopCountSmall : chopCountLarge;
        }
        
        currentChopCount -= damage;
        
        if (currentChopCount <= 0)
        {
            ChopDown();
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 砍伐成树桩（并生成掉落物）
    /// </summary>
    public void ChopDown()
    {
        // ✅ 重置砍伐状态
        if (occlusionTransparency != null)
        {
            occlusionTransparency.SetChoppingState(false);
        }
        
        // 播放砍倒音效
        PlayChopFellSound();
        
        // 生成掉落物品
        SpawnDrops();
        
        // ✅ 启动倒下动画或直接转换为树桩
        if (enableFallAnimation)
        {
            // 使用最后一次命中时记录的玩家朝向
            StartCoroutine(FallAnimationCoroutine(lastHitPlayerDirection, lastHitPlayerFlipX));
        }
        else
        {
            // 直接转换为树桩
            FinishChopDown();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=orange>[TreeController] {gameObject.name} 被砍倒！</color>");
        }
    }
    
    /// <summary>
    /// 完成砍倒（转换为树桩）
    /// </summary>
    private void FinishChopDown()
    {
        currentState = TreeState.Stump;
        currentChopCount = 0;
        UpdateSprite();
    }
    
    /// <summary>
    /// 倒下方向枚举
    /// </summary>
    public enum FallDirection
    {
        Right,    // 向右倒（Down0, Up1）
        Left,     // 向左倒（Down1, Up0）
        Up        // 向上倒（Side0, Side1）
    }
    
    /// <summary>
    /// 根据玩家朝向和翻转状态判定倒下方向
    /// ✅ 修正版：Direction 参数映射 0=Down, 1=Up, 2=Side
    /// 
    /// 判定表：
    /// | 玩家朝向 | FlipX | 倒下方向 |
    /// |---------|-------|---------|
    /// | Down (0) | false | 向右倒 |
    /// | Down (0) | true  | 向左倒 |
    /// | Up (1)   | false | 向左倒 |
    /// | Up (1)   | true  | 向右倒 |
    /// | Side (2) | false | 向上倒 |
    /// | Side (2) | true  | 向上倒 |
    /// </summary>
    private FallDirection DetermineFallDirection(int playerDirection, bool playerFlipX)
    {
        switch (playerDirection)
        {
            case 0: // Down
                // Down: 向右倒（flipX时向左倒）
                return playerFlipX ? FallDirection.Left : FallDirection.Right;
            case 1: // Up（不是 Side！）
                // Up: 向左倒（flipX时向右倒）
                return playerFlipX ? FallDirection.Right : FallDirection.Left;
            case 2: // Side（不是 Up！）
                // Side: 向上倒
                return FallDirection.Up;
            default:
                return FallDirection.Right;
        }
    }
    
    /// <summary>
    /// 计算旋转角度
    /// </summary>
    private float CalculateTargetAngle(FallDirection fallDir)
    {
        return fallDir switch
        {
            FallDirection.Right => -90f,   // 顺时针向右倒
            FallDirection.Left => 90f,     // 逆时针向左倒
            FallDirection.Up => 90f,       // 逆时针向上倒（透视效果）
            _ => 0f
        };
    }
    
    /// <summary>
    /// 获取方向名称（调试用）
    /// ✅ 修正：Direction 参数映射 0=Down, 1=Up, 2=Side
    /// </summary>
    private string GetDirectionName(int dir) => dir switch
    {
        0 => "Down",
        1 => "Up",      // 不是 Side！
        2 => "Side",    // 不是 Up！
        _ => "Unknown"
    };
    
    /// <summary>
    /// 倒下动画协程
    /// ✅ 修复版：树桩立即生成，倒下动画是纯视觉效果
    /// 
    /// 核心设计：
    /// 1. 树桩在被砍到的那一刻就立着（原位置）
    /// 2. 创建临时的倒下 Sprite（纯视觉，无碰撞）
    /// 3. 倒下的树木不会推动玩家或其他物体
    /// 4. 动画结束后销毁临时 Sprite
    /// </summary>
    /// <param name="playerDirection">玩家朝向（0=Down, 1=Up, 2=Side）</param>
    /// <param name="playerFlipX">玩家是否水平翻转</param>
    private System.Collections.IEnumerator FallAnimationCoroutine(int playerDirection, bool playerFlipX)
    {
        if (spriteRenderer == null) 
        {
            FinishChopDown();
            yield break;
        }
        
        // ✅ 判定倒下方向
        FallDirection fallDir = DetermineFallDirection(playerDirection, playerFlipX);
        float targetAngle = CalculateTargetAngle(fallDir);
        
        // ✅ 保存当前 Sprite 信息用于创建临时倒下效果
        Sprite fallingSprite = spriteRenderer.sprite;
        Vector3 originalWorldPos = spriteRenderer.transform.position;
        Vector3 originalScale = spriteRenderer.transform.localScale;
        Color originalColor = spriteRenderer.color;
        int sortingLayerID = spriteRenderer.sortingLayerID;
        int sortingOrder = spriteRenderer.sortingOrder;
        
        // ✅ 计算 Sprite 的底部中心位置（树根视觉位置）
        // 这是旋转的轴心点，无论如何旋转/缩放，这个点必须保持不变
        Bounds spriteBounds = spriteRenderer.bounds;
        Vector3 spriteBottomCenter = new Vector3(spriteBounds.center.x, spriteBounds.min.y, 0);
        
        // ✅ 计算 Sprite 中心到底部的偏移（用于旋转计算）
        float spriteHalfHeight = spriteBounds.extents.y;
        
        // ✅ 调试输出
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[TreeController] 倒下判定:</color>\n" +
                      $"  玩家朝向: {playerDirection} ({GetDirectionName(playerDirection)})\n" +
                      $"  玩家翻转: {playerFlipX}\n" +
                      $"  倒下方向: {fallDir}\n" +
                      $"  Sprite中心: {spriteBounds.center}\n" +
                      $"  Sprite底部中心(轴心): {spriteBottomCenter}\n" +
                      $"  Sprite半高: {spriteHalfHeight}\n" +
                      $"  旋转角度: {targetAngle}°");
        }
        
        // 转换为树桩
        FinishChopDown();
        
        // ✅ 创建临时的倒下 Sprite（纯视觉，无碰撞）
        GameObject fallingTree = new GameObject("FallingTree_Temp");
        fallingTree.transform.position = originalWorldPos;
        fallingTree.transform.localScale = originalScale;
        
        SpriteRenderer fallingSR = fallingTree.AddComponent<SpriteRenderer>();
        fallingSR.sprite = fallingSprite;
        fallingSR.sortingLayerID = sortingLayerID;
        fallingSR.sortingOrder = sortingOrder - 1; // 在树桩后面
        fallingSR.color = originalColor;
        
        // ✅ 动画参数
        float elapsed = 0f;
        float duration = fallDuration;
        
        // 判断是侧向倒还是向上倒
        bool isSidefall = (fallDir == FallDirection.Left || fallDir == FallDirection.Right);
        
        while (elapsed < duration)
        {
            // 使用 t² 实现先慢后快（模拟重力加速）
            float linearT = elapsed / duration;
            float t = linearT * linearT; // 加速曲线
            
            if (isSidefall)
            {
                // ✅ 侧向倒：绕 Sprite 底部中心旋转
                // 核心：树根位置（spriteBottomCenter）始终不变
                float angle = targetAngle * t;
                float rad = angle * Mathf.Deg2Rad;
                
                // 从底部中心到 Sprite 中心的向量（未旋转时是 (0, spriteHalfHeight)）
                Vector3 centerOffset = new Vector3(0, spriteHalfHeight, 0);
                
                // 旋转这个偏移向量
                Vector3 rotatedOffset = new Vector3(
                    centerOffset.x * Mathf.Cos(rad) - centerOffset.y * Mathf.Sin(rad),
                    centerOffset.x * Mathf.Sin(rad) + centerOffset.y * Mathf.Cos(rad),
                    0
                );
                
                // 新的 Sprite 中心位置 = 底部中心 + 旋转后的偏移
                Vector3 newCenter = spriteBottomCenter + rotatedOffset;
                
                fallingTree.transform.position = newCenter;
                fallingTree.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                // ✅ 向上倒：只做Y轴拉长然后消失（参数可在Inspector调试）
                // 核心：树根位置始终不变
                
                float scaleY;
                if (t < fallUpStretchPhase)
                {
                    // 拉长阶段：1.0 → fallUpMaxStretch
                    scaleY = Mathf.Lerp(1f, fallUpMaxStretch, t / fallUpStretchPhase);
                }
                else
                {
                    // 缩短阶段：fallUpMaxStretch → fallUpMinScale
                    scaleY = Mathf.Lerp(fallUpMaxStretch, fallUpMinScale, (t - fallUpStretchPhase) / (1f - fallUpStretchPhase));
                }
                
                // 缩放后的新半高
                float newHalfHeight = spriteHalfHeight * scaleY;
                
                // 新的 Sprite 中心 Y = 底部 Y + 新半高（保持树根不动）
                float newCenterY = spriteBottomCenter.y + newHalfHeight;
                
                // X轴保持不变
                fallingTree.transform.localScale = new Vector3(originalScale.x, originalScale.y * scaleY, originalScale.z);
                fallingTree.transform.position = new Vector3(spriteBottomCenter.x, newCenterY, 0);
            }
            
            // ✅ 淡出动画（最后 30% 开始淡出）
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
        
        // ✅ 动画结束，销毁临时 Sprite
        Destroy(fallingTree);
    }
    
    /// <summary>
    /// 生成掉落物品
    /// </summary>
    private void SpawnDrops()
    {
        if (dropTable == null) return;
        
        var drops = dropTable.GenerateDrops();
        Vector3 dropOrigin = transform.parent != null ? transform.parent.position : transform.position;
        
        foreach (var drop in drops)
        {
            if (drop.item == null) continue;
            
            if (WorldSpawnService.Instance != null)
            {
                WorldSpawnService.Instance.SpawnMultiple(
                    drop.item,
                    drop.quality,
                    drop.amount,
                    dropOrigin,
                    dropTable.spreadRadius
                );
            }
        }
    }
    
    /// <summary>
    /// 重置
    /// </summary>
    public void Reset()
    {
        currentStage = GrowthStage.Sapling;
        currentState = TreeState.Normal;
        isWeatherWithered = false;
        isFrozenSapling = false;
        
        if (TimeManager.Instance != null)
        {
            plantedDay = TimeManager.Instance.GetTotalDaysPassed();
            lastCheckDay = -1;
        }
        
        UpdateSprite();
    }
    
    #region 公共接口
    public GrowthStage GetCurrentStage() => currentStage;
    public SeasonManager.Season GetCurrentSeason() => currentSeason;
    public SeasonManager.VegetationSeason GetVegetationSeason() => SeasonManager.Instance != null ? SeasonManager.Instance.GetCurrentVegetationSeason() : SeasonManager.VegetationSeason.Spring;
    public TreeState GetCurrentState() => currentState;
    public bool IsFrozenSapling() => isFrozenSapling;
    #endregion
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        #if UNITY_EDITOR
        // ✅ 只在编辑器模式下预览，运行时不触发
        if (!editorPreview) return;
        if (Application.isPlaying) return; // 运行时完全跳过
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null) return;
        }
        
        // 编辑器预览：监听阶段和状态变化
        if (currentStage != lastEditorStage)
        {
            lastEditorStage = currentStage;
            UpdateSprite();
        }
        else if (currentState != lastEditorState)
        {
            lastEditorState = currentState;
            UpdateSprite();
        }
        #endif
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🔄 测试季节循环")]
    private static void TestSeasonCycle(UnityEditor.MenuCommand command)
    {
        TreeController tree = command.context as TreeController;
        if (tree == null) return;
        
        SeasonManager.Season nextSeason = tree.currentSeason switch
        {
            SeasonManager.Season.Spring => SeasonManager.Season.Summer,
            SeasonManager.Season.Summer => SeasonManager.Season.Autumn,
            SeasonManager.Season.Autumn => SeasonManager.Season.Winter,
            SeasonManager.Season.Winter => SeasonManager.Season.Spring,
            _ => SeasonManager.Season.Spring
        };
        
        tree.currentSeason = nextSeason;
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🌱 测试成长")]
    private static void TestGrow(UnityEditor.MenuCommand command)
    {
        TreeController tree = command.context as TreeController;
        if (tree == null) return;
        
        tree.Grow();
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🍂 测试枯萎")]
    private static void TestWither(UnityEditor.MenuCommand command)
    {
        TreeController tree = command.context as TreeController;
        if (tree == null) return;
        
        tree.SetWithered(true);
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🪓 测试砍伐")]
    private static void TestChop(UnityEditor.MenuCommand command)
    {
        TreeController tree = command.context as TreeController;
        if (tree == null) return;
        
        tree.ChopDown();
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/━━━━━━━━━━━━━━━━", false, 1000)]
    private static void Separator1(UnityEditor.MenuCommand command) { }
    
    [UnityEditor.MenuItem("CONTEXT/TreeController/🔧 立即对齐当前Sprite", false, 1001)]
    private static void AlignCurrentSprite(UnityEditor.MenuCommand command)
    {
        TreeController tree = command.context as TreeController;
        if (tree == null) return;
        
        if (tree.spriteRenderer == null)
        {
            tree.spriteRenderer = tree.GetComponentInChildren<SpriteRenderer>();
        }
        
        if (tree.spriteRenderer != null && tree.spriteRenderer.sprite != null)
        {
            // ✅ 新逻辑：让sprite底部对齐父物体中心
            Bounds spriteBounds = tree.spriteRenderer.sprite.bounds;
            float spriteBottomOffset = spriteBounds.min.y;
            
            Transform treeTransform = tree.spriteRenderer.transform;
            Vector3 localPos = treeTransform.localPosition;
            localPos.y = -spriteBottomOffset;
            treeTransform.localPosition = localPos;
            
            Debug.Log($"<color=cyan>[TreeController] {tree.gameObject.name} 已对齐Sprite (localY={localPos.y:F3})</color>");
        }
        
        UnityEditor.EditorUtility.SetDirty(tree);
    }
    #endif
}
