# 设计文档

## 概述

石头/矿石系统是一个可采集资源系统，管理游戏中的石头和矿石资源。核心设计原则：
1. **阶段递减** - 石头只能被挖掘变小，不会生长
2. **溢出伤害** - 伤害可以跨阶段传递
3. **差值掉落** - 每个阶段掉落当前与下一阶段的矿物差值
4. **材料等级限制** - 不同镐子能获取不同矿物，但所有镐子都能获得石料

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                      StoneController                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  阶段系统   │  │  血量系统   │  │   掉落系统          │  │
│  │  (M1-M4)   │  │  (溢出伤害) │  │  (差值计算)         │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
         阶段配置         材料等级判定      经验系统
         (血量/掉落)      (镐子vs矿石)     (Gathering)
```

## 组件和接口

### StoneStage 枚举

```csharp
/// <summary>
/// 石头阶段（4个）
/// </summary>
public enum StoneStage
{
    M1 = 0,  // 最大，血量36
    M2 = 1,  // 中等，血量17
    M3 = 2,  // 最小可进阶，血量9，最终阶段
    M4 = 3   // 装饰石头，血量4，最终阶段
}
```

### OreType 枚举

```csharp
/// <summary>
/// 矿物类型
/// </summary>
public enum OreType
{
    None = 0,   // 纯石头（无矿物）
    C1 = 1,     // 铜矿
    C2 = 2,     // 铁矿
    C3 = 3      // 金矿
}
```

### StoneStageConfig 阶段配置

```csharp
[System.Serializable]
public class StoneStageConfig
{
    [Header("血量设置")]
    [Tooltip("该阶段的血量")]
    public int health;
    
    [Header("掉落设置")]
    [Tooltip("该阶段的石料总量（用于计算差值掉落）")]
    public int stoneTotalCount;
    
    [Header("阶段设置")]
    [Tooltip("是否为最终阶段（M3和M4为true）")]
    public bool isFinalStage;
    
    [Tooltip("下一阶段（非最终阶段有效）")]
    public StoneStage nextStage;
    
    [Tooltip("转换到下一阶段时含量指数是否减1")]
    public bool decreaseOreIndexOnTransition;
}
```

### 默认阶段配置表

| 阶段 | 血量 | 石料总量 | 石料掉落 | 最终阶段 | 下一阶段 | 含量减少 |
|------|------|---------|---------|---------|---------|---------|
| M1 | 36 | 12 | 6 | ✗ | M2 | ✗ |
| M2 | 17 | 6 | 2 | ✗ | M3 | ✓ |
| M3 | 9 | 4 | 4 | ✓ | - | - |
| M4 | 4 | 2 | 2 | ✓ | - | - |

> **石料掉落计算**：每个阶段掉落 = 当前阶段石料总量 - 下一阶段石料总量
> - M1→M2：12 - 6 = 6 个石料
> - M2→M3：6 - 4 = 2 个石料
> - M3 销毁：4 个石料（最终阶段全部掉落）
> - M4 销毁：2 个石料（最终阶段全部掉落）

### OreDropConfig 矿物和石料掉落配置

```csharp
/// <summary>
/// 矿物和石料掉落配置表
/// </summary>
public static class DropConfig
{
    // M1 阶段矿物总量
    public static readonly int[] M1_OreTotals = { 0, 3, 5, 7, 9 };
    
    // M2 阶段矿物总量
    public static readonly int[] M2_OreTotals = { 0, 1, 3, 5, 7 };
    
    // M3 阶段矿物总量
    public static readonly int[] M3_OreTotals = { 0, 1, 2, 3 };
    
    // 各阶段石料总量
    public static readonly int[] StoneTotals = { 12, 6, 4, 2 }; // M1, M2, M3, M4
    
    /// <summary>
    /// 计算阶段转换时的矿物掉落数量
    /// </summary>
    public static int CalculateOreDropAmount(StoneStage fromStage, int oreIndex, StoneStage toStage, int newOreIndex)
    {
        int currentTotal = GetOreTotal(fromStage, oreIndex);
        int nextTotal = GetOreTotal(toStage, newOreIndex);
        return currentTotal - nextTotal;
    }
    
    /// <summary>
    /// 计算阶段转换时的石料掉落数量
    /// </summary>
    public static int CalculateStoneDropAmount(StoneStage fromStage, StoneStage toStage)
    {
        int currentTotal = GetStoneTotal(fromStage);
        int nextTotal = GetStoneTotal(toStage);
        return currentTotal - nextTotal;
    }
    
    /// <summary>
    /// 获取指定阶段和含量的矿物总量
    /// </summary>
    public static int GetOreTotal(StoneStage stage, int oreIndex)
    {
        switch (stage)
        {
            case StoneStage.M1:
                return oreIndex < M1_OreTotals.Length ? M1_OreTotals[oreIndex] : 0;
            case StoneStage.M2:
                return oreIndex < M2_OreTotals.Length ? M2_OreTotals[oreIndex] : 0;
            case StoneStage.M3:
                return oreIndex < M3_OreTotals.Length ? M3_OreTotals[oreIndex] : 0;
            default:
                return 0;
        }
    }
    
    /// <summary>
    /// 获取指定阶段的石料总量
    /// </summary>
    public static int GetStoneTotal(StoneStage stage)
    {
        int index = (int)stage;
        return index < StoneTotals.Length ? StoneTotals[index] : 0;
    }
}
```

### MaterialTierHelper 材料等级判定

```csharp
/// <summary>
/// 材料等级判定工具
/// </summary>
public static class MaterialTierHelper
{
    /// <summary>
    /// 检查镐子是否能获取指定矿石的矿物
    /// 注意：所有镐子都能获得石料
    /// </summary>
    public static bool CanMineOre(int pickaxeTier, OreType oreType)
    {
        // 纯石头或无矿物：返回 true
        if (oreType == OreType.None) return true;
        
        switch (oreType)
        {
            case OreType.C2: // 铁矿
                return pickaxeTier >= 1; // 石镐或更高
                
            case OreType.C1: // 铜矿
                return pickaxeTier >= 2; // 生铁镐或更高
                
            case OreType.C3: // 金矿
                return pickaxeTier >= 4; // 钢镐或更高
                
            default:
                return true;
        }
    }
    
    /// <summary>
    /// 所有镐子都能获得石料
    /// </summary>
    public static bool CanGetStone(int pickaxeTier) => true;
}
```

## 数据模型

### StoneController 核心字段

```csharp
public class StoneController : MonoBehaviour, IResourceNode
{
    #region 序列化字段 - 阶段配置
    [Header("━━━━ 阶段配置 ━━━━")]
    [Tooltip("当前阶段")]
    [SerializeField] private StoneStage currentStage = StoneStage.M1;
    
    [Tooltip("矿物类型")]
    [SerializeField] private OreType oreType = OreType.None;
    
    [Tooltip("矿物含量指数（0-4）")]
    [Range(0, 4)]
    [SerializeField] private int oreIndex = 0;
    
    [Tooltip("是否为最终阶段")]
    [SerializeField] private bool isFinalStage = false;
    #endregion
    
    #region 序列化字段 - 血量
    [Header("━━━━ 血量状态 ━━━━")]
    [Tooltip("当前血量")]
    [SerializeField] private int currentHealth = 36;
    #endregion
    
    #region 序列化字段 - Sprite配置
    [Header("━━━━ Sprite配置 ━━━━")]
    [Tooltip("各阶段各含量的Sprite（自动从命名规范加载）")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    #endregion
    
    #region 序列化字段 - 掉落配置
    [Header("━━━━ 掉落配置 ━━━━")]
    [Tooltip("矿物掉落物品")]
    [SerializeField] private ItemData oreDropItem;
    
    [Tooltip("石料掉落物品")]
    [SerializeField] private ItemData stoneDropItem;
    #endregion
}
```

### 阶段转换逻辑

```csharp
/// <summary>
/// 处理阶段转换（含溢出伤害）
/// </summary>
private void HandleStageTransition(int overflowDamage)
{
    var config = GetStageConfig(currentStage);
    
    // 最终阶段：直接销毁
    if (config.isFinalStage)
    {
        SpawnFinalDrops();
        DestroyStone();
        return;
    }
    
    // 计算新的含量指数
    int newOreIndex = config.decreaseOreIndexOnTransition 
        ? Mathf.Max(0, oreIndex - 1) 
        : oreIndex;
    
    // 计算并掉落差值矿物
    int oreDrop = DropConfig.CalculateOreDropAmount(
        currentStage, oreIndex, 
        config.nextStage, newOreIndex
    );
    
    // 掉落矿物（如果镐子等级足够）
    if (canGetOre && oreDrop > 0)
    {
        SpawnOreDrops(oreDrop);
        GrantOreExperience(oreDrop);
    }
    
    // 计算并掉落差值石料
    int stoneDrop = DropConfig.CalculateStoneDropAmount(currentStage, config.nextStage);
    SpawnStoneDrops(stoneDrop);
    GrantStoneExperience(stoneDrop);
    
    // 转换到下一阶段
    currentStage = config.nextStage;
    oreIndex = newOreIndex;
    
    // 初始化新阶段血量
    var newConfig = GetStageConfig(currentStage);
    currentHealth = newConfig.health;
    
    // 检查新阶段是否为最终阶段
    isFinalStage = newConfig.isFinalStage;
    
    // 更新 Sprite
    UpdateSprite();
    
    // 应用溢出伤害
    if (overflowDamage > 0)
    {
        TakeDamage(overflowDamage);
    }
}
```

### 伤害处理逻辑

```csharp
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
        PlayHitEffect();
    }
}
```

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. 
Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 阶段血量初始化一致性
*For any* 石头阶段，初始化后的血量应等于该阶段配置的 health 值（M1=36, M2=17, M3=9, M4=4）
**Validates: Requirements 2.1-2.4**

### Property 2: 溢出伤害传递正确性
*For any* 伤害值大于当前血量的情况，溢出伤害应正确传递到下一阶段
**Validates: Requirements 2.5-2.6**

### Property 3: 阶段转换含量指数规则
*For any* M1→M2 转换，含量指数保持不变；*For any* M2→M3 转换，含量指数减1（最低为0）
**Validates: Requirements 3.1-3.2**

### Property 4: 差值掉落计算正确性
*For any* 阶段转换，掉落的矿物数量应等于当前阶段矿物总量减去下一阶段矿物总量
**Validates: Requirements 4.1-4.3**

### Property 5: 石料掉落数量一致性
*For any* 阶段转换，掉落的石料数量应等于当前阶段石料总量减去下一阶段石料总量（M1→M2=6, M2→M3=2, M3销毁=4, M4销毁=2）
**Validates: Requirements 6.1-6.4**

### Property 6: 材料等级限制正确性
*For any* 镐子和矿石组合，矿物获取应遵循材料等级限制表，但石料始终可获得
**Validates: Requirements 8.1-8.10**

### Property 7: 经验计算正确性
*For any* 掉落物，经验值应等于矿物数量×2 + 石料数量×1
**Validates: Requirements 7.1-7.4**

### Property 8: 最终阶段销毁逻辑
*For any* M3 或 M4 阶段血量归零，石头应被销毁而非进入下一阶段
**Validates: Requirements 1.4-1.5, 3.4**

### Property 9: Sprite 命名解析正确性
*For any* Sprite 名称 `Stone_{OreType}_{Stage}_{OreIndex}`，解析结果应正确对应矿物类型、阶段和含量
**Validates: Requirements 9.1-9.6**

## 错误处理

1. **阶段配置缺失**：使用默认配置
2. **Sprite 缺失**：输出警告，使用占位 Sprite
3. **掉落物品未配置**：跳过掉落，输出警告
4. **精力不足**：显示 UI 提示，阻止工具动作

## 测试策略

### 单元测试
- 阶段血量初始化正确性
- 溢出伤害计算正确性
- 差值掉落计算正确性
- 材料等级判定逻辑

### 属性测试
- 使用 NUnit + FsCheck 进行属性测试
- 生成随机的阶段、含量、伤害值组合
- 验证所有正确性属性

### 集成测试
- 完整挖掘流程（M1→M2→M3→销毁）
- 溢出伤害跨阶段传递
- 材料等级限制验证

### 手动测试
- 各阶段视觉效果
- 挖掘音效和粒子
- 掉落物生成位置

