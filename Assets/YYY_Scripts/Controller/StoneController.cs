using UnityEngine;
using System.Collections.Generic;
using FarmGame.Combat;
using FarmGame.Data;
using FarmGame.Utils;

/// <summary>
/// 石头/矿石控制器
/// 
/// 核心特性：
/// - 4阶段系统（M1-M4），只能被挖掘变小，不会生长
/// - 溢出伤害机制：伤害可以跨阶段传递
/// - 差值掉落：每个阶段掉落当前与下一阶段的矿物/石料差值
/// - 材料等级限制：不同镐子能获取不同矿物，但所有镐子都能获得石料
/// - Sprite 底部中心对齐：所有阶段的 Sprite 底部中心与父物体位置对齐
/// - Collider 自动同步：从 Sprite 的 Custom Physics Shape 更新 PolygonCollider2D
/// 
/// Sprite命名规范：Stone_{OreType}_{Stage}_{OreIndex}
/// 例如：Stone_C1_M1_4（铜矿，M1阶段，含量4）
/// </summary>
public class StoneController : MonoBehaviour, IResourceNode
{
    #region 序列化字段 - 阶段配置
    [Header("━━━━ 阶段配置 ━━━━")]
    [Tooltip("4个阶段的配置")]
    [SerializeField] private StoneStageConfig[] stageConfigs = new StoneStageConfig[]
    {
        // M1：最大阶段
        new StoneStageConfig
        {
            health = 36,
            stoneTotalCount = 12,
            isFinalStage = false,
            nextStage = StoneStage.M2,
            decreaseOreIndexOnTransition = false
        },
        // M2：中等阶段
        new StoneStageConfig
        {
            health = 17,
            stoneTotalCount = 6,
            isFinalStage = false,
            nextStage = StoneStage.M3,
            decreaseOreIndexOnTransition = true
        },
        // M3：最小阶段（最终阶段）
        new StoneStageConfig
        {
            health = 9,
            stoneTotalCount = 2,  // 与 M4 一致
            isFinalStage = true,
            nextStage = StoneStage.M3,
            decreaseOreIndexOnTransition = false
        },
        // M4：装饰石头（最终阶段）
        new StoneStageConfig
        {
            health = 4,
            stoneTotalCount = 2,
            isFinalStage = true,
            nextStage = StoneStage.M4,
            decreaseOreIndexOnTransition = false
        }
    };
    
    [Tooltip("当前阶段")]
    [SerializeField] private StoneStage currentStage = StoneStage.M1;
    
    [Tooltip("矿物类型")]
    [SerializeField] private OreType oreType = OreType.None;
    
    [Tooltip("矿物含量指数（0-4）")]
    [Range(0, 4)]
    [SerializeField] private int oreIndex = 0;
    #endregion
    
    #region 序列化字段 - 血量
    [Header("━━━━ 血量状态 ━━━━")]
    [Tooltip("当前血量")]
    [SerializeField] private int currentHealth = 36;
    #endregion
    
    #region 序列化字段 - Sprite配置
    [Header("━━━━ Sprite配置 ━━━━")]
    [Tooltip("SpriteRenderer组件")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Tooltip("PolygonCollider2D组件（用于从 Sprite 的 Custom Physics Shape 同步）")]
    [SerializeField] private PolygonCollider2D polygonCollider;
    
    [Tooltip("Sprite资源文件夹（拖入包含所有Stone Sprite的文件夹）")]
    [SerializeField] private UnityEngine.Object spriteFolder;
    
    [Tooltip("Sprite资源路径前缀（从文件夹自动获取，也可手动填写）")]
    [SerializeField] private string spritePathPrefix = "Sprites/Props/Materials/Stone/";
    #endregion
    
    #region 序列化字段 - 视觉效果配置
    [Header("━━━━ 视觉效果 ━━━━")]
    [Tooltip("阶段变化时的粒子效果预制体")]
    [SerializeField] private GameObject stageChangeParticlePrefab;
    
    [Tooltip("石块碎片颜色")]
    [SerializeField] private Color debrisColor = new Color(0.6f, 0.5f, 0.4f, 1f);
    
    [Tooltip("阶段变化时是否播放缩放动画")]
    [SerializeField] private bool playScaleAnimation = true;
    #endregion
    
    #region 序列化字段 - 掉落配置
    [Header("━━━━ 掉落配置 ━━━━")]
    [Tooltip("铜矿掉落物品")]
    [SerializeField] private ItemData copperOreItem;
    
    [Tooltip("铁矿掉落物品")]
    [SerializeField] private ItemData ironOreItem;
    
    [Tooltip("金矿掉落物品")]
    [SerializeField] private ItemData goldOreItem;
    
    [Tooltip("石料掉落物品")]
    [SerializeField] private ItemData stoneItem;
    
    [Tooltip("掉落物散布半径")]
    [Range(0.5f, 2f)]
    [SerializeField] private float dropSpreadRadius = 1f;
    #endregion
    
    #region 序列化字段 - 音效
    [Header("━━━━ 音效设置 ━━━━")]
    [Tooltip("挖掘音效")]
    [SerializeField] private AudioClip mineHitSound;
    
    [Tooltip("破碎音效")]
    [SerializeField] private AudioClip breakSound;
    
    [Tooltip("等级不足音效")]
    [SerializeField] private AudioClip tierInsufficientSound;
    
    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.8f;
    #endregion
    
    #region 序列化字段 - 调试
    [Header("━━━━ 调试 ━━━━")]
    [SerializeField] private bool showDebugInfo = false;
    #endregion
    
    #region 私有字段
    private bool isDepleted = false;
    private int lastHitPickaxeTier = 0;
    private bool lastHitCanGetOre = false;
    
    // 运行时调试：用于检测 Inspector 参数变化
    private OreType lastOreType;
    private StoneStage lastStage;
    private int lastOreIndex;
    #endregion
    
    #region 属性
    /// <summary>当前阶段配置</summary>
    public StoneStageConfig CurrentStageConfig => GetStageConfig(currentStage);
    
    /// <summary>是否为最终阶段</summary>
    public bool IsFinalStage => CurrentStageConfig?.isFinalStage ?? true;
    #endregion
    
    #region Unity生命周期
    private void Awake()
    {
        // 初始化阶段配置
        if (stageConfigs == null || stageConfigs.Length != 4)
        {
            stageConfigs = StoneStageConfigFactory.CreateDefaultConfigs();
        }
    }
    
    private void Start()
    {
        Debug.Log($"<color=magenta>[StoneController] ★★★ Start() 开始初始化: {gameObject.name} ★★★</color>");
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"[StoneController] {gameObject.name} 缺少SpriteRenderer组件！");
            enabled = false;
            return;
        }
        
        // 初始化血量
        InitializeHealth();
        
        // 钳制 OreIndex 到有效范围
        ClampOreIndex();
        
        // 更新显示
        UpdateSprite();
        
        // 初始化运行时调试状态
        lastOreType = oreType;
        lastStage = currentStage;
        lastOreIndex = oreIndex;
        
        // 注册到资源节点注册表
        if (ResourceNodeRegistry.Instance != null)
        {
            ResourceNodeRegistry.Instance.Register(this, gameObject.GetInstanceID());
            Debug.Log($"<color=lime>[StoneController] ✓ 已注册到 ResourceNodeRegistry: {gameObject.name} (InstanceID={gameObject.GetInstanceID()})</color>");
        }
        else
        {
            Debug.LogError($"<color=red>[StoneController] ★★★ 错误：ResourceNodeRegistry.Instance 为空！无法注册 {gameObject.name} ★★★</color>");
        }
        
        Debug.Log($"<color=magenta>[StoneController] 初始化完成: {gameObject.name}</color>");
        Debug.Log($"<color=magenta>  - 矿物类型: {oreType}</color>");
        Debug.Log($"<color=magenta>  - 当前阶段: {currentStage}</color>");
        Debug.Log($"<color=magenta>  - 含量指数: {oreIndex}</color>");
        Debug.Log($"<color=magenta>  - 当前血量: {currentHealth}</color>");
    }
    
    private void Update()
    {
        // 运行时调试：检测 Inspector 参数变化
        UpdateRuntimeInspectorDebug();
    }
    
    private void OnDestroy()
    {
        if (ResourceNodeRegistry.Instance != null)
        {
            ResourceNodeRegistry.Instance.Unregister(gameObject.GetInstanceID());
        }
    }
    #endregion
    
    #region 初始化
    /// <summary>
    /// 初始化血量（根据当前阶段和矿物含量）
    /// M3 阶段 oreIndex=0（无矿物）时血量为 4，与 M4 一致
    /// </summary>
    private void InitializeHealth()
    {
        var config = CurrentStageConfig;
        if (config != null)
        {
            // M3 阶段且无矿物（oreIndex=0）时，血量与 M4 一致（4）
            if (currentStage == StoneStage.M3 && oreIndex == 0)
            {
                currentHealth = 4;  // 与 M4 装饰石头血量一致
            }
            else
            {
                currentHealth = config.health;
            }
        }
    }
    
    /// <summary>
    /// 获取指定阶段的配置
    /// </summary>
    private StoneStageConfig GetStageConfig(StoneStage stage)
    {
        int index = (int)stage;
        if (stageConfigs == null || index < 0 || index >= stageConfigs.Length)
        {
            return null;
        }
        return stageConfigs[index];
    }
    
    /// <summary>
    /// 获取指定阶段的最大 OreIndex
    /// </summary>
    private int GetMaxOreIndex(StoneStage stage)
    {
        return stage switch
        {
            StoneStage.M1 => 4,
            StoneStage.M2 => 4,
            StoneStage.M3 => 3,
            StoneStage.M4 => 7,
            _ => 4
        };
    }
    
    /// <summary>
    /// 钳制 OreIndex 到当前阶段的有效范围
    /// </summary>
    private void ClampOreIndex()
    {
        int maxIndex = GetMaxOreIndex(currentStage);
        oreIndex = Mathf.Clamp(oreIndex, 0, maxIndex);
    }
    
    /// <summary>
    /// 运行时调试：检测 Inspector 参数变化并实时更新
    /// </summary>
    private void UpdateRuntimeInspectorDebug()
    {
        bool changed = false;
        
        // 检测阶段变化
        if (lastStage != currentStage)
        {
            if (showDebugInfo)
                Debug.Log($"<color=cyan>[StoneController] 阶段变化: {lastStage} → {currentStage}</color>");
            
            lastStage = currentStage;
            
            // 阶段变化时重置血量
            InitializeHealth();
            
            // 钳制 OreIndex 到新阶段的有效范围
            ClampOreIndex();
            
            changed = true;
        }
        
        // 检测矿物类型变化
        if (lastOreType != oreType)
        {
            if (showDebugInfo)
                Debug.Log($"<color=cyan>[StoneController] 矿物类型变化: {lastOreType} → {oreType}</color>");
            
            lastOreType = oreType;
            changed = true;
        }
        
        // 检测含量指数变化
        if (lastOreIndex != oreIndex)
        {
            // 钳制到有效范围
            ClampOreIndex();
            
            if (showDebugInfo)
                Debug.Log($"<color=cyan>[StoneController] 含量指数变化: {lastOreIndex} → {oreIndex}</color>");
            
            lastOreIndex = oreIndex;
            changed = true;
        }
        
        // 参数变化时更新 Sprite
        if (changed)
        {
            UpdateSprite();
        }
    }
    #endregion
    
    #region IResourceNode 接口实现
    public string ResourceTag => "Rock";
    
    public bool IsDepleted => isDepleted;
    
    /// <summary>
    /// 检查是否接受此工具类型（只有镐子能有效挖掘）
    /// </summary>
    public bool CanAccept(ToolHitContext ctx)
    {
        Debug.Log($"<color=cyan>[StoneController] CanAccept 被调用: {gameObject.name}</color>");
        Debug.Log($"<color=cyan>  - isDepleted: {isDepleted}</color>");
        Debug.Log($"<color=cyan>  - ctx.toolType: {ctx.toolType}</color>");
        Debug.Log($"<color=cyan>  - 期望类型: {ToolType.Pickaxe}</color>");
        
        if (isDepleted)
        {
            Debug.Log($"<color=yellow>[StoneController] 已耗尽，拒绝接受</color>");
            return false;
        }
        
        bool canAccept = ctx.toolType == ToolType.Pickaxe;
        Debug.Log($"<color=cyan>[StoneController] CanAccept 结果: {canAccept}</color>");
        return canAccept;
    }
    
    /// <summary>
    /// 处理命中效果
    /// </summary>
    public void OnHit(ToolHitContext ctx)
    {
        if (isDepleted) return;
        
        // 只有镐子能有效挖掘
        if (ctx.toolType != ToolType.Pickaxe)
        {
            PlayShakeEffect();
            if (showDebugInfo)
                Debug.Log($"<color=gray>[StoneController] {gameObject.name} 被非镐子工具击中（工具类型={ctx.toolType}），只抖动</color>");
            return;
        }
        
        // 获取镐子材料等级
        int pickaxeTier = GetPickaxeTier(ctx);
        lastHitPickaxeTier = pickaxeTier;
        
        // 检查是否能获取矿物
        lastHitCanGetOre = MaterialTierHelper.CanMineOre(pickaxeTier, oreType);
        
        // ★★★ 详细调试输出 ★★★
        Debug.Log($"<color=cyan>[StoneController] ═══════════════════════════════════</color>");
        Debug.Log($"<color=cyan>[StoneController] 挖掘命中: {gameObject.name}</color>");
        Debug.Log($"<color=cyan>[StoneController] 矿石信息:</color>");
        Debug.Log($"<color=cyan>  - 矿物类型: {oreType} ({MaterialTierHelper.GetOreTypeName(oreType)})</color>");
        Debug.Log($"<color=cyan>  - 当前阶段: {currentStage}</color>");
        Debug.Log($"<color=cyan>  - 含量指数: {oreIndex}</color>");
        Debug.Log($"<color=cyan>  - 当前血量: {currentHealth}/{CurrentStageConfig?.health ?? 0}</color>");
        Debug.Log($"<color=cyan>[StoneController] 镐子信息:</color>");
        Debug.Log($"<color=cyan>  - 镐子等级: {pickaxeTier} ({MaterialTierHelper.GetTierName(pickaxeTier)})</color>");
        Debug.Log($"<color=cyan>  - 所需等级: {MaterialTierHelper.GetRequiredPickaxeTier(oreType)} ({MaterialTierHelper.GetTierName(MaterialTierHelper.GetRequiredPickaxeTier(oreType))})</color>");
        Debug.Log($"<color=cyan>  - 能否获取矿物: {lastHitCanGetOre} (pickaxeTier={pickaxeTier} >= required={MaterialTierHelper.GetRequiredPickaxeTier(oreType)})</color>");
        Debug.Log($"<color=cyan>[StoneController] ═══════════════════════════════════</color>");
        
        // 尝试消耗精力
        float energyCost = GetEnergyCost(ctx);
        if (!TryConsumeEnergy(energyCost))
        {
            PlayShakeEffect();
            Debug.Log($"<color=yellow>[StoneController] {gameObject.name} 精力不足，无法挖掘</color>");
            return;
        }
        
        // 计算伤害
        int damage = Mathf.Max(1, Mathf.RoundToInt(ctx.baseDamage));
        
        // 播放挖掘音效
        PlayMineHitSound();
        
        // 扣血
        TakeDamage(damage);
        
        Debug.Log($"<color=yellow>[StoneController] {gameObject.name} 受到 {damage} 点伤害，剩余血量 {currentHealth}</color>");
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
    
    #region 伤害系统
    /// <summary>
    /// 处理伤害（含溢出）
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            int overflow = -currentHealth;
            HandleStageTransition(overflow);
        }
        else
        {
            PlayShakeEffect();
        }
    }
    
    /// <summary>
    /// 处理阶段转换（含溢出伤害）
    /// </summary>
    private void HandleStageTransition(int overflowDamage)
    {
        var config = CurrentStageConfig;
        if (config == null)
        {
            Debug.Log($"<color=red>[StoneController] {gameObject.name} 配置为空，直接销毁</color>");
            DestroyStone();
            return;
        }
        
        Debug.Log($"<color=orange>[StoneController] ═══════════════════════════════════</color>");
        Debug.Log($"<color=orange>[StoneController] 阶段转换开始: {gameObject.name}</color>");
        Debug.Log($"<color=orange>  - 当前阶段: {currentStage}</color>");
        Debug.Log($"<color=orange>  - 是否最终阶段: {config.isFinalStage}</color>");
        Debug.Log($"<color=orange>  - 溢出伤害: {overflowDamage}</color>");
        Debug.Log($"<color=orange>  - lastHitCanGetOre: {lastHitCanGetOre}</color>");
        Debug.Log($"<color=orange>  - lastHitPickaxeTier: {lastHitPickaxeTier}</color>");
        
        // 最终阶段：直接销毁
        if (config.isFinalStage)
        {
            Debug.Log($"<color=orange>[StoneController] 最终阶段，准备掉落并销毁</color>");
            SpawnFinalDrops();
            GrantExperience(true);
            DestroyStone();
            return;
        }
        
        // 计算新的含量指数
        int newOreIndex = config.decreaseOreIndexOnTransition 
            ? Mathf.Max(0, oreIndex - 1) 
            : oreIndex;
        
        Debug.Log($"<color=orange>[StoneController] 阶段转换掉落计算:</color>");
        Debug.Log($"<color=orange>  - 当前含量指数: {oreIndex}</color>");
        Debug.Log($"<color=orange>  - 新含量指数: {newOreIndex}</color>");
        Debug.Log($"<color=orange>  - 下一阶段: {config.nextStage}</color>");
        
        // 计算并掉落差值矿物（如果镐子等级足够）
        if (lastHitCanGetOre)
        {
            int oreDrop = StoneDropConfig.CalculateOreDropAmount(
                currentStage, oreIndex, 
                config.nextStage, newOreIndex
            );
            
            Debug.Log($"<color=lime>[StoneController] 矿物掉落计算:</color>");
            Debug.Log($"<color=lime>  - 计算结果: {oreDrop} 个矿物</color>");
            Debug.Log($"<color=lime>  - 矿物类型: {oreType}</color>");
            
            if (oreDrop > 0)
            {
                SpawnOreDrops(oreDrop);
            }
            else
            {
                Debug.Log($"<color=yellow>[StoneController] 矿物掉落数量为0，不生成掉落物</color>");
            }
        }
        else
        {
            Debug.Log($"<color=yellow>[StoneController] 镐子等级不足，无法获取矿物（只能获得石料）</color>");
        }
        
        // 计算并掉落差值石料（所有镐子都能获得）
        int stoneDrop = StoneDropConfig.CalculateStoneDropAmount(currentStage, config.nextStage);
        Debug.Log($"<color=lime>[StoneController] 石料掉落计算: {stoneDrop} 个石料</color>");
        
        if (stoneDrop > 0)
        {
            SpawnStoneDrops(stoneDrop);
        }
        
        // 给予经验
        GrantExperience(false);
        
        // 播放破碎音效
        PlayBreakSound();
        
        // 播放阶段变化视觉效果（粒子 + 缩放动画）
        PlayStageChangeEffect();
        
        // 转换到下一阶段
        StoneStage previousStage = currentStage;
        currentStage = config.nextStage;
        oreIndex = newOreIndex;
        
        // 初始化新阶段血量
        InitializeHealth();
        
        // 更新 Sprite（包含底部对齐和 Collider 同步）
        UpdateSprite();;
        
        Debug.Log($"<color=orange>[StoneController] 阶段转换完成: {previousStage} → {currentStage}，新含量指数 {oreIndex}</color>");
        Debug.Log($"<color=orange>[StoneController] ═══════════════════════════════════</color>");
        
        // 应用溢出伤害
        if (overflowDamage > 0)
        {
            Debug.Log($"<color=orange>[StoneController] 应用溢出伤害: {overflowDamage}</color>");
            TakeDamage(overflowDamage);
        }
    }
    #endregion
    
    #region 掉落系统
    /// <summary>
    /// 生成矿物掉落
    /// </summary>
    private void SpawnOreDrops(int amount)
    {
        Debug.Log($"<color=lime>[StoneController] SpawnOreDrops 被调用，数量: {amount}</color>");
        
        if (amount <= 0)
        {
            Debug.Log($"<color=yellow>[StoneController] 矿物数量 <= 0，跳过生成</color>");
            return;
        }
        
        ItemData oreItem = GetOreItem();
        if (oreItem == null)
        {
            Debug.Log($"<color=red>[StoneController] ★★★ 错误：矿物 ItemData 为空！★★★</color>");
            Debug.Log($"<color=red>  - 矿物类型: {oreType}</color>");
            Debug.Log($"<color=red>  - copperOreItem: {(copperOreItem != null ? copperOreItem.itemName : "NULL")}</color>");
            Debug.Log($"<color=red>  - ironOreItem: {(ironOreItem != null ? ironOreItem.itemName : "NULL")}</color>");
            Debug.Log($"<color=red>  - goldOreItem: {(goldOreItem != null ? goldOreItem.itemName : "NULL")}</color>");
            return;
        }
        
        Vector3 dropOrigin = GetPosition();
        
        Debug.Log($"<color=lime>[StoneController] 准备生成矿物掉落:</color>");
        Debug.Log($"<color=lime>  - 物品: {oreItem.itemName} (ID={oreItem.itemID})</color>");
        Debug.Log($"<color=lime>  - 数量: {amount}</color>");
        Debug.Log($"<color=lime>  - 位置: {dropOrigin}</color>");
        Debug.Log($"<color=lime>  - WorldSpawnService: {(WorldSpawnService.Instance != null ? "存在" : "NULL")}</color>");
        
        if (WorldSpawnService.Instance != null)
        {
            WorldSpawnService.Instance.SpawnMultiple(
                oreItem,
                0, // 品质
                amount,
                dropOrigin,
                dropSpreadRadius
            );
            Debug.Log($"<color=lime>[StoneController] ✓ 矿物掉落已生成: {amount} 个 {oreItem.itemName}</color>");
        }
        else
        {
            Debug.Log($"<color=red>[StoneController] ★★★ 错误：WorldSpawnService.Instance 为空！★★★</color>");
        }
    }
    
    /// <summary>
    /// 生成石料掉落
    /// </summary>
    private void SpawnStoneDrops(int amount)
    {
        Debug.Log($"<color=lime>[StoneController] SpawnStoneDrops 被调用，数量: {amount}</color>");
        
        if (amount <= 0)
        {
            Debug.Log($"<color=yellow>[StoneController] 石料数量 <= 0，跳过生成</color>");
            return;
        }
        
        if (stoneItem == null)
        {
            Debug.Log($"<color=red>[StoneController] ★★★ 错误：stoneItem 为空！请在 Inspector 中配置石料掉落物品 ★★★</color>");
            return;
        }
        
        Vector3 dropOrigin = GetPosition();
        
        Debug.Log($"<color=lime>[StoneController] 准备生成石料掉落:</color>");
        Debug.Log($"<color=lime>  - 物品: {stoneItem.itemName} (ID={stoneItem.itemID})</color>");
        Debug.Log($"<color=lime>  - 数量: {amount}</color>");
        Debug.Log($"<color=lime>  - 位置: {dropOrigin}</color>");
        Debug.Log($"<color=lime>  - WorldSpawnService: {(WorldSpawnService.Instance != null ? "存在" : "NULL")}</color>");
        
        if (WorldSpawnService.Instance != null)
        {
            WorldSpawnService.Instance.SpawnMultiple(
                stoneItem,
                0, // 品质
                amount,
                dropOrigin,
                dropSpreadRadius
            );
            Debug.Log($"<color=lime>[StoneController] ✓ 石料掉落已生成: {amount} 个 {stoneItem.itemName}</color>");
        }
        else
        {
            Debug.Log($"<color=red>[StoneController] ★★★ 错误：WorldSpawnService.Instance 为空！★★★</color>");
        }
    }
    
    /// <summary>
    /// 生成最终阶段掉落（全部掉落）
    /// </summary>
    private void SpawnFinalDrops()
    {
        Debug.Log($"<color=magenta>[StoneController] ═══════════════════════════════════</color>");
        Debug.Log($"<color=magenta>[StoneController] 最终阶段掉落: {gameObject.name}</color>");
        Debug.Log($"<color=magenta>  - 当前阶段: {currentStage}</color>");
        Debug.Log($"<color=magenta>  - 矿物类型: {oreType}</color>");
        Debug.Log($"<color=magenta>  - 含量指数: {oreIndex}</color>");
        Debug.Log($"<color=magenta>  - lastHitCanGetOre: {lastHitCanGetOre}</color>");
        
        // 掉落矿物（如果镐子等级足够）
        if (lastHitCanGetOre)
        {
            int oreDrop = StoneDropConfig.CalculateFinalOreDropAmount(currentStage, oreIndex);
            Debug.Log($"<color=magenta>[StoneController] 最终矿物掉落计算: {oreDrop} 个</color>");
            
            if (oreDrop > 0)
            {
                SpawnOreDrops(oreDrop);
            }
            else
            {
                Debug.Log($"<color=yellow>[StoneController] 最终矿物掉落数量为0</color>");
            }
        }
        else
        {
            Debug.Log($"<color=yellow>[StoneController] 镐子等级不足，最终阶段也无法获取矿物</color>");
        }
        
        // 掉落石料（所有镐子都能获得）
        int stoneDrop = StoneDropConfig.CalculateFinalStoneDropAmount(currentStage);
        Debug.Log($"<color=magenta>[StoneController] 最终石料掉落计算: {stoneDrop} 个</color>");
        
        if (stoneDrop > 0)
        {
            SpawnStoneDrops(stoneDrop);
        }
        
        // 播放破碎音效
        PlayBreakSound();
        
        Debug.Log($"<color=magenta>[StoneController] ═══════════════════════════════════</color>");
    }
    
    /// <summary>
    /// 获取对应矿物类型的掉落物品
    /// </summary>
    private ItemData GetOreItem()
    {
        return oreType switch
        {
            OreType.C1 => copperOreItem,
            OreType.C2 => ironOreItem,
            OreType.C3 => goldOreItem,
            _ => null
        };
    }
    #endregion
    
    #region 经验系统
    /// <summary>
    /// 给予采集经验
    /// </summary>
    /// <param name="isFinal">是否为最终阶段</param>
    private void GrantExperience(bool isFinal)
    {
        int oreCount = 0;
        int stoneCount = 0;
        
        if (isFinal)
        {
            // 最终阶段：计算全部掉落
            if (lastHitCanGetOre)
            {
                oreCount = StoneDropConfig.CalculateFinalOreDropAmount(currentStage, oreIndex);
            }
            stoneCount = StoneDropConfig.CalculateFinalStoneDropAmount(currentStage);
        }
        else
        {
            // 阶段转换：计算差值掉落
            var config = CurrentStageConfig;
            if (config != null)
            {
                int newOreIndex = config.decreaseOreIndexOnTransition 
                    ? Mathf.Max(0, oreIndex - 1) 
                    : oreIndex;
                
                if (lastHitCanGetOre)
                {
                    oreCount = StoneDropConfig.CalculateOreDropAmount(
                        currentStage, oreIndex, 
                        config.nextStage, newOreIndex
                    );
                }
                stoneCount = StoneDropConfig.CalculateStoneDropAmount(currentStage, config.nextStage);
            }
        }
        
        int totalXP = StoneDropConfig.CalculateExperience(oreCount, stoneCount);
        
        if (totalXP > 0 && SkillLevelService.Instance != null)
        {
            SkillLevelService.Instance.AddExperience(SkillType.Gathering, totalXP);
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=lime>[StoneController] {gameObject.name} 获得 {totalXP} 点采集经验（矿物{oreCount}×2 + 石料{stoneCount}×1）</color>");
            }
        }
    }
    #endregion
    
    #region 工具辅助方法
    /// <summary>
    /// 获取镐子材料等级
    /// </summary>
    private int GetPickaxeTier(ToolHitContext ctx)
    {
        if (ctx.attacker != null)
        {
            var toolController = ctx.attacker.GetComponent<PlayerToolController>();
            if (toolController != null && toolController.CurrentToolData != null)
            {
                var toolData = toolController.CurrentToolData as ToolData;
                if (toolData != null)
                {
                    int tier = toolData.GetMaterialTierValue();
                    Debug.Log($"<color=cyan>[StoneController] 获取镐子等级: {tier} ({MaterialTierHelper.GetTierName(tier)}) - 工具: {toolData.itemName}</color>");
                    return tier;
                }
                else
                {
                    Debug.Log($"<color=yellow>[StoneController] CurrentToolData 不是 ToolData 类型</color>");
                }
            }
            else
            {
                Debug.Log($"<color=yellow>[StoneController] toolController={toolController != null}, CurrentToolData={toolController?.CurrentToolData != null}</color>");
            }
        }
        else
        {
            Debug.Log($"<color=yellow>[StoneController] ctx.attacker 为空</color>");
        }
        
        Debug.Log($"<color=yellow>[StoneController] 无法获取镐子等级，使用默认值 0 (木质)</color>");
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
    
    #region Sprite系统
    /// <summary>
    /// 更新Sprite显示（包含底部对齐和Collider同步）
    /// </summary>
    public void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        string spriteName = GetSpriteName();
        Sprite sprite = null;
        
#if UNITY_EDITOR
        // 编辑器模式：使用 AssetDatabase 加载
        sprite = LoadSpriteInEditor(spriteName);
#else
        // 运行时：尝试从 Resources 加载
        string fullPath = spritePathPrefix + spriteName;
        sprite = Resources.Load<Sprite>(fullPath);
        
        // 如果找不到，尝试加载带 _0 后缀的版本
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>(fullPath + "_0");
        }
#endif
        
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            
            // 对齐 Sprite 底部中心到父物体位置
            AlignSpriteBottomCenter();
            
            // 同步 Collider
            SyncColliderFromSprite();
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning($"[StoneController] 找不到Sprite: {spriteName}");
        }
    }
    
    /// <summary>
    /// 对齐 Sprite 底部中心到父物体位置
    /// 确保所有阶段的石头底部中心都在同一位置
    /// </summary>
    private void AlignSpriteBottomCenter()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        
        // 获取 Sprite 的 bounds（本地坐标）
        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        
        // 计算底部中心的偏移量
        // Sprite 的 pivot 决定了 bounds.center 相对于 transform.position 的位置
        // 我们需要让 Sprite 的底部中心与父物体位置对齐
        float bottomY = spriteBounds.min.y;
        float centerX = spriteBounds.center.x;
        
        // 设置本地位置，使底部中心对齐到 (0, 0)
        spriteRenderer.transform.localPosition = new Vector3(-centerX, -bottomY, 0);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[StoneController] Sprite 底部对齐: localPos = {spriteRenderer.transform.localPosition}</color>");
        }
    }
    
    /// <summary>
    /// 从 Sprite 的 Custom Physics Shape 同步 PolygonCollider2D
    /// 注意：PolygonCollider2D 和 SpriteRenderer 在同一个物体上
    /// 当我们移动 transform 来对齐 Sprite 底部时，Collider 会自动跟着移动
    /// 所以路径点不需要额外偏移
    /// </summary>
    private void SyncColliderFromSprite()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        
        // 自动查找 PolygonCollider2D - 应该在 SpriteRenderer 同一个物体上
        if (polygonCollider == null)
        {
            polygonCollider = spriteRenderer.GetComponent<PolygonCollider2D>();
        }
        
        // 如果 SpriteRenderer 物体上没有，尝试在当前物体上找
        if (polygonCollider == null)
        {
            polygonCollider = GetComponent<PolygonCollider2D>();
        }
        
        if (polygonCollider == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[StoneController] 没有找到 PolygonCollider2D，跳过 Collider 同步");
            return;
        }
        
        Sprite sprite = spriteRenderer.sprite;
        int shapeCount = sprite.GetPhysicsShapeCount();
        
        if (shapeCount == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[StoneController] Sprite {sprite.name} 没有 Custom Physics Shape");
            return;
        }
        
        // 设置路径数量
        polygonCollider.pathCount = shapeCount;
        
        // 复制每个路径（不需要偏移，因为 Collider 和 SpriteRenderer 在同一个物体上）
        List<Vector2> path = new List<Vector2>();
        
        for (int i = 0; i < shapeCount; i++)
        {
            path.Clear();
            sprite.GetPhysicsShape(i, path);
            polygonCollider.SetPath(i, path);
        }
        
        // 重置 offset
        polygonCollider.offset = Vector2.zero;
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[StoneController] Collider 已同步: {shapeCount} 个路径</color>");
        }
        
        // 如果有 CompositeCollider2D，触发重新生成
        if (transform.parent != null)
        {
            var composite = transform.parent.GetComponent<CompositeCollider2D>();
            if (composite != null)
            {
                composite.GenerateGeometry();
            }
        }
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 编辑器中加载 Sprite（使用 AssetDatabase）
    /// </summary>
    private Sprite LoadSpriteInEditor(string spriteName)
    {
        if (spriteFolder == null) return null;
        
        string folderPath = UnityEditor.AssetDatabase.GetAssetPath(spriteFolder);
        if (string.IsNullOrEmpty(folderPath)) return null;
        
        // 搜索匹配的 Sprite
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:Sprite {spriteName}", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            
            // 尝试加载子资源（Multiple Sprite 模式）
            UnityEngine.Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var subAsset in subAssets)
            {
                if (subAsset is Sprite sprite)
                {
                    // 检查名称匹配（支持带 _0 后缀）
                    string normalizedName = GetNormalizedSpriteName(sprite.name);
                    if (normalizedName == spriteName || sprite.name == spriteName)
                    {
                        return sprite;
                    }
                }
            }
        }
        
        // 如果精确搜索失败，尝试遍历所有 Sprite
        guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var subAsset in subAssets)
            {
                if (subAsset is Sprite sprite)
                {
                    string normalizedName = GetNormalizedSpriteName(sprite.name);
                    if (normalizedName == spriteName || sprite.name == spriteName)
                    {
                        return sprite;
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取规范化的 Sprite 名称（去掉 Unity 切片后缀）
    /// </summary>
    private string GetNormalizedSpriteName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        
        string[] parts = name.Split('_');
        
        // 如果是 5 个部分且第一个是 Stone，去掉最后一个（切片后缀）
        if (parts.Length == 5 && parts[0] == "Stone")
        {
            return $"{parts[0]}_{parts[1]}_{parts[2]}_{parts[3]}";
        }
        
        return name;
    }
#endif
    
    /// <summary>
    /// 获取当前状态的Sprite名称
    /// 格式：Stone_{OreType}_{Stage}_{OreIndex}
    /// </summary>
    private string GetSpriteName()
    {
        string oreTypeStr = oreType == OreType.None ? "C0" : oreType.ToString();
        string stageStr = currentStage.ToString();
        return $"Stone_{oreTypeStr}_{stageStr}_{oreIndex}";
    }
    
    /// <summary>
    /// 从Sprite名称解析状态
    /// </summary>
    public static bool TryParseSpriteName(string spriteName, out OreType oreType, out StoneStage stage, out int oreIndex)
    {
        oreType = OreType.None;
        stage = StoneStage.M1;
        oreIndex = 0;
        
        if (string.IsNullOrEmpty(spriteName)) return false;
        
        // 格式：Stone_{OreType}_{Stage}_{OreIndex}
        string[] parts = spriteName.Split('_');
        if (parts.Length < 4 || parts[0] != "Stone") return false;
        
        // 解析矿物类型
        string oreStr = parts[1];
        if (oreStr == "C0" || oreStr == "None")
            oreType = OreType.None;
        else if (System.Enum.TryParse(oreStr, out OreType parsedOre))
            oreType = parsedOre;
        else
            return false;
        
        // 解析阶段
        if (!System.Enum.TryParse(parts[2], out stage))
            return false;
        
        // 解析含量指数
        if (!int.TryParse(parts[3], out oreIndex))
            return false;
        
        return true;
    }
    #endregion
    
    #region 音效系统
    private void PlayMineHitSound()
    {
        if (mineHitSound != null)
        {
            AudioSource.PlayClipAtPoint(mineHitSound, GetPosition(), soundVolume);
        }
    }
    
    private void PlayBreakSound()
    {
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, GetPosition(), soundVolume);
        }
    }
    
    private void PlayTierInsufficientSound()
    {
        if (tierInsufficientSound != null)
        {
            AudioSource.PlayClipAtPoint(tierInsufficientSound, GetPosition(), soundVolume);
        }
    }
    #endregion
    
    #region 视觉效果
    /// <summary>
    /// 播放抖动效果
    /// </summary>
    private void PlayShakeEffect()
    {
        StartCoroutine(ShakeCoroutine());
    }
    
    /// <summary>
    /// 播放阶段变化效果（缩放动画 + 粒子）
    /// </summary>
    private void PlayStageChangeEffect()
    {
        // 播放粒子效果
        SpawnDebrisParticles();
        
        // 播放缩放动画
        if (playScaleAnimation)
        {
            StartCoroutine(StageChangeScaleCoroutine());
        }
    }
    
    /// <summary>
    /// 生成石块碎片粒子
    /// </summary>
    private void SpawnDebrisParticles()
    {
        Vector3 spawnPos = GetPosition();
        
        // 如果有预制体，使用预制体
        if (stageChangeParticlePrefab != null)
        {
            var particle = Instantiate(stageChangeParticlePrefab, spawnPos, Quaternion.identity);
            Destroy(particle, 2f);
            return;
        }
        
        // 否则使用简单的碎片效果
        StartCoroutine(SimpleDebrisCoroutine(spawnPos));
    }
    
    /// <summary>
    /// 简单的碎片效果（不依赖预制体）
    /// </summary>
    private System.Collections.IEnumerator SimpleDebrisCoroutine(Vector3 origin)
    {
        // 创建临时的碎片精灵
        int debrisCount = Random.Range(4, 8);
        List<GameObject> debris = new List<GameObject>();
        
        for (int i = 0; i < debrisCount; i++)
        {
            var debrisObj = new GameObject($"StoneDebris_{i}");
            debrisObj.transform.position = origin + new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.5f),
                0
            );
            
            var sr = debrisObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDebrisSprite();
            sr.color = debrisColor;
            sr.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
            sr.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 0;
            
            debris.Add(debrisObj);
        }
        
        // 动画：碎片飞散
        float duration = 0.5f;
        float elapsed = 0f;
        
        Vector3[] velocities = new Vector3[debrisCount];
        for (int i = 0; i < debrisCount; i++)
        {
            float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
            float speed = Random.Range(1.5f, 3f);
            velocities[i] = new Vector3(Mathf.Cos(angle) * speed * (Random.value > 0.5f ? 1 : -1), Mathf.Sin(angle) * speed, 0);
        }
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float gravity = 5f;
            
            for (int i = 0; i < debris.Count; i++)
            {
                if (debris[i] != null)
                {
                    // 应用速度和重力
                    velocities[i].y -= gravity * Time.deltaTime;
                    debris[i].transform.position += velocities[i] * Time.deltaTime;
                    
                    // 淡出
                    var sr = debris[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 1f - t;
                        sr.color = c;
                    }
                    
                    // 缩小
                    debris[i].transform.localScale = Vector3.one * (1f - t * 0.5f) * 0.3f;
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 清理
        foreach (var d in debris)
        {
            if (d != null) Destroy(d);
        }
    }
    
    /// <summary>
    /// 创建简单的碎片 Sprite（1x1 白色像素）
    /// </summary>
    private Sprite CreateDebrisSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16);
    }
    
    /// <summary>
    /// 阶段变化时的缩放动画
    /// </summary>
    private System.Collections.IEnumerator StageChangeScaleCoroutine()
    {
        if (spriteRenderer == null) yield break;
        
        Transform target = spriteRenderer.transform;
        Vector3 originalScale = target.localScale;
        
        // 先缩小
        float shrinkDuration = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < shrinkDuration)
        {
            float t = elapsed / shrinkDuration;
            target.localScale = originalScale * (1f - t * 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 等待 Sprite 更新（在调用处已经更新了）
        yield return null;
        
        // 弹回
        float bounceDuration = 0.15f;
        elapsed = 0f;
        Vector3 newScale = target.localScale;
        
        while (elapsed < bounceDuration)
        {
            float t = elapsed / bounceDuration;
            // 使用弹性曲线
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            target.localScale = originalScale * bounce;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        target.localScale = originalScale;
    }
    
    private System.Collections.IEnumerator ShakeCoroutine()
    {
        if (spriteRenderer == null) yield break;
        
        Vector3 originalPos = spriteRenderer.transform.localPosition;
        float shakeDuration = 0.15f;
        float shakeAmount = 0.05f;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            float damping = 1f - progress;
            float x = Mathf.Sin(progress * Mathf.PI * 4) * shakeAmount * damping;
            spriteRenderer.transform.localPosition = originalPos + new Vector3(x, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        spriteRenderer.transform.localPosition = originalPos;
    }
    #endregion
    
    #region 销毁
    /// <summary>
    /// 销毁石头
    /// </summary>
    private void DestroyStone()
    {
        isDepleted = true;
        
        // 从注册表注销
        if (ResourceNodeRegistry.Instance != null)
        {
            ResourceNodeRegistry.Instance.Unregister(gameObject.GetInstanceID());
        }
        
        // 销毁父物体（整个石头）
        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        if (showDebugInfo)
            Debug.Log($"<color=orange>[StoneController] {gameObject.name} 被完全挖掘！</color>");
    }
    #endregion
    
    #region 公共接口
    public StoneStage GetCurrentStage() => currentStage;
    public OreType GetOreType() => oreType;
    public int GetOreIndex() => oreIndex;
    public int GetCurrentHealth() => currentHealth;
    public UnityEngine.Object GetSpriteFolder() => spriteFolder;
    public string GetSpritePathPrefix() => spritePathPrefix;
    
    /// <summary>
    /// 设置 Sprite 路径前缀（由编辑器调用）
    /// </summary>
    public void SetSpritePathPrefix(string prefix)
    {
        spritePathPrefix = prefix;
    }
    
    /// <summary>
    /// 设置阶段（用于调试或初始化）
    /// </summary>
    public void SetStage(StoneStage stage, OreType type, int index)
    {
        currentStage = stage;
        oreType = type;
        
        // 使用阶段特定的最大值钳制
        int maxIndex = GetMaxOreIndex(stage);
        oreIndex = Mathf.Clamp(index, 0, maxIndex);
        
        // 更新运行时调试状态
        lastStage = currentStage;
        lastOreType = oreType;
        lastOreIndex = oreIndex;
        
        InitializeHealth();
        UpdateSprite();
    }
    #endregion
    
    #region 编辑器
    #if UNITY_EDITOR
    /// <summary>
    /// 编辑器中参数变化时更新预览
    /// </summary>
    private void OnValidate()
    {
        // 钳制 OreIndex 到有效范围
        int maxIndex = GetMaxOreIndex(currentStage);
        oreIndex = Mathf.Clamp(oreIndex, 0, maxIndex);
        
        // 编辑模式下更新 Sprite 预览
        if (!Application.isPlaying && spriteRenderer != null)
        {
            UpdateSprite();
        }
    }
    
    [ContextMenu("调试 - 设置为M1_C1_4（大铜矿）")]
    private void DEBUG_SetM1C1()
    {
        SetStage(StoneStage.M1, OreType.C1, 4);
    }
    
    [ContextMenu("调试 - 设置为M1_C2_3（大铁矿）")]
    private void DEBUG_SetM1C2()
    {
        SetStage(StoneStage.M1, OreType.C2, 3);
    }
    
    [ContextMenu("调试 - 设置为M1_C3_2（大金矿）")]
    private void DEBUG_SetM1C3()
    {
        SetStage(StoneStage.M1, OreType.C3, 2);
    }
    
    [ContextMenu("调试 - 设置为M4（装饰石头）")]
    private void DEBUG_SetM4()
    {
        SetStage(StoneStage.M4, OreType.None, 0);
    }
    
    [ContextMenu("调试 - 造成10点伤害")]
    private void DEBUG_TakeDamage10()
    {
        lastHitCanGetOre = true;
        lastHitPickaxeTier = 5;
        TakeDamage(10);
    }
    
    [ContextMenu("调试 - 造成50点伤害（测试溢出）")]
    private void DEBUG_TakeDamage50()
    {
        lastHitCanGetOre = true;
        lastHitPickaxeTier = 5;
        TakeDamage(50);
    }
    #endif
    #endregion
}
