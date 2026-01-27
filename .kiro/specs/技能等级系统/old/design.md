# Design Document: 技能等级系统

## Overview

本设计文档描述技能等级系统的架构和实现方案。该系统管理玩家的5种独立技能类别，每种技能通过不同渠道获取经验并独立升级。技能等级影响配方解锁、工具效率等游戏机制。

**5大技能类别**：
- **Combat（战斗）** - 打猎和打怪
- **Gathering（采集）** - 挖矿、砍树、耕种
- **Crafting（制作）** - 配方制作、NPC制作
- **Cooking（烹饪）** - 食材处理加工
- **Fishing（钓鱼）** - 钓鱼相关活动

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      经验获取层                              │
├─────────────────┬─────────────────┬─────────────────────────┤
│ TreeController  │ StoneController │ CropController          │
│ (Gathering)     │ (Gathering)     │ (Gathering)             │
├─────────────────┼─────────────────┼─────────────────────────┤
│ EnemyController │ CraftingService │ CookingService          │
│ (Combat)        │ (Crafting)      │ (Cooking)               │
├─────────────────┴─────────────────┴─────────────────────────┤
│ FishingController (Fishing)                                  │
└────────┬────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│                   SkillLevelService                          │
│  - AddExperience(skillType, amount)                         │
│  - GetLevel(skillType)                                      │
│  - GetExperience(skillType)                                 │
│  - GetExperienceToNextLevel(skillType)                      │
│  - Events: OnExperienceGained, OnLevelUp                    │
└─────────────────────────────────────────────────────────────┘
         │                 │
         ▼                 ▼
┌─────────────────┐ ┌─────────────────────────────────────────┐
│ SkillData[]     │ │ CraftingService                         │
│ - 5种技能数据   │ │ - 检查配方解锁条件                      │
│ - 等级、经验    │ │ - 订阅 OnLevelUp 事件                   │
└─────────────────┘ └─────────────────────────────────────────┘
```

## Components and Interfaces

### 1. SkillType 枚举（更新后）

```csharp
namespace FarmGame.Data
{
    /// <summary>
    /// 技能类型（5大类）
    /// 注意：原有的 Farming/Mining/Woodcutting 已合并为 Gathering
    /// </summary>
    public enum SkillType
    {
        Combat = 0,     // 战斗（打猎和打怪）
        Gathering = 1,  // 采集（挖矿、砍树、耕种、收获）
        Crafting = 2,   // 制作（配方制作、NPC制作）
        Cooking = 3,    // 烹饪（食材处理加工）
        Fishing = 4     // 钓鱼
    }
}
```

### 经验获取渠道汇总

| 技能类型 | 经验来源 | 说明 |
|---------|---------|------|
| Combat | 击败敌人、打猎 | 战斗相关 |
| Gathering | 挖矿、砍树、耕种、收获 | 资源采集相关 |
| Crafting | 配方制作、NPC制作 | 物品制作相关 |
| Cooking | 烹饪、食材加工 | 食物相关 |
| Fishing | 钓鱼 | 钓鱼相关 |

### 2. SkillData 数据结构

```csharp
[System.Serializable]
public class SkillData
{
    public SkillType skillType;
    public int level = 1;
    public int currentExperience = 0;
    
    /// <summary>
    /// 获取升级所需经验（可配置曲线）
    /// </summary>
    public int GetExperienceToNextLevel()
    {
        // 简单公式：100 * level^1.5
        return Mathf.RoundToInt(100 * Mathf.Pow(level, 1.5f));
    }
    
    /// <summary>
    /// 获取当前等级进度（0-1）
    /// </summary>
    public float GetProgress()
    {
        return (float)currentExperience / GetExperienceToNextLevel();
    }
}
```

### 3. SkillLevelService 服务

```csharp
public class SkillLevelService : MonoBehaviour
{
    public static SkillLevelService Instance { get; private set; }
    
    [Header("技能数据")]
    [SerializeField] private SkillData[] skills = new SkillData[5];
    
    [Header("配置")]
    [SerializeField] private int maxLevel = 10;
    
    [Header("音效")]
    [SerializeField] private AudioClip levelUpSound;
    
    // 事件
    public event Action<SkillType, int> OnExperienceGained;  // (技能类型, 获得经验)
    public event Action<SkillType, int> OnLevelUp;           // (技能类型, 新等级)
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeSkills();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 初始化技能数据
    /// </summary>
    private void InitializeSkills()
    {
        if (skills == null || skills.Length != 5)
        {
            skills = new SkillData[5];
            for (int i = 0; i < 5; i++)
            {
                skills[i] = new SkillData
                {
                    skillType = (SkillType)i,
                    level = 1,
                    currentExperience = 0
                };
            }
        }
    }
    
    /// <summary>
    /// 添加经验
    /// </summary>
    public void AddExperience(SkillType skillType, int amount)
    {
        if (amount <= 0) return;
        
        var skill = GetSkillData(skillType);
        if (skill == null) return;
        
        // 已达最大等级
        if (skill.level >= maxLevel) return;
        
        skill.currentExperience += amount;
        OnExperienceGained?.Invoke(skillType, amount);
        
        // 检查升级
        while (skill.currentExperience >= skill.GetExperienceToNextLevel() && skill.level < maxLevel)
        {
            skill.currentExperience -= skill.GetExperienceToNextLevel();
            skill.level++;
            
            PlayLevelUpSound();
            OnLevelUp?.Invoke(skillType, skill.level);
            
            Debug.Log($"<color=lime>[SkillLevelService] {skillType} 升级到 {skill.level}！</color>");
        }
    }
    
    /// <summary>
    /// 获取技能等级
    /// </summary>
    public int GetLevel(SkillType skillType)
    {
        var skill = GetSkillData(skillType);
        return skill?.level ?? 1;
    }
    
    /// <summary>
    /// 获取技能当前经验
    /// </summary>
    public int GetExperience(SkillType skillType)
    {
        var skill = GetSkillData(skillType);
        return skill?.currentExperience ?? 0;
    }
    
    /// <summary>
    /// 获取升级所需经验
    /// </summary>
    public int GetExperienceToNextLevel(SkillType skillType)
    {
        var skill = GetSkillData(skillType);
        return skill?.GetExperienceToNextLevel() ?? 100;
    }
    
    /// <summary>
    /// 获取技能数据
    /// </summary>
    private SkillData GetSkillData(SkillType skillType)
    {
        int index = (int)skillType;
        if (index >= 0 && index < skills.Length)
        {
            return skills[index];
        }
        return null;
    }
    
    /// <summary>
    /// 播放升级音效
    /// </summary>
    private void PlayLevelUpSound()
    {
        if (levelUpSound != null)
        {
            AudioSource.PlayClipAtPoint(levelUpSound, Camera.main.transform.position);
        }
    }
}
```

### 4. TreeController 经验配置（使用 Gathering 技能）

```csharp
// 在 TreeControllerV2 中添加
[Header("━━━━ 经验配置 ━━━━")]
[Tooltip("各阶段砍伐经验（阶段0-5）")]
[SerializeField] private int[] stageExperience = new int[] { 0, 0, 2, 4, 6, 20 };

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
/// 砍倒树木时调用
/// </summary>
private void OnTreeFelled()
{
    int xp = GetChopExperience();
    if (xp > 0 && SkillLevelService.Instance != null)
    {
        // 使用 Gathering 技能（采集）- 砍树属于采集
        SkillLevelService.Instance.AddExperience(SkillType.Gathering, xp);
    }
}
```

### 5. StoneController 经验配置（使用 Gathering 技能）

```csharp
// 在 StoneController 中
/// <summary>
/// 给予采集经验
/// </summary>
private void GrantGatheringExperience(int oreCount, int stoneCount)
{
    // 矿物 2 经验/个，石料 1 经验/个
    int totalXP = oreCount * 2 + stoneCount * 1;
    
    if (totalXP > 0 && SkillLevelService.Instance != null)
    {
        // 使用 Gathering 技能（采集）- 挖矿属于采集
        SkillLevelService.Instance.AddExperience(SkillType.Gathering, totalXP);
    }
}
```



## Data Models

### 经验获取事件

```csharp
public struct ExperienceGainedEvent
{
    public SkillType skillType;
    public int amount;
    public Vector3 position;  // 用于显示浮动文字
}
```

### 升级事件

```csharp
public struct LevelUpEvent
{
    public SkillType skillType;
    public int oldLevel;
    public int newLevel;
}
```

### 砍树经验配置表

| 树木阶段 | 经验值 | 技能类型 | 说明 |
|----------|--------|---------|------|
| 阶段 0 | 0 | Gathering | 树苗，不提供经验 |
| 阶段 1 | 0 | Gathering | 幼苗，不提供经验 |
| 阶段 2 | 2 | Gathering | 小树 |
| 阶段 3 | 4 | Gathering | 中树 |
| 阶段 4 | 6 | Gathering | 大树 |
| 阶段 5 | 20 | Gathering | 巨树，高经验奖励 |

### 挖矿经验配置表

| 资源类型 | 经验值 | 技能类型 | 说明 |
|----------|--------|---------|------|
| 矿物 | 2/个 | Gathering | 每获得1个矿物 |
| 石料 | 1/个 | Gathering | 每获得1个石料 |

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 经验累积正确性
*For any* 技能和经验增量，AddExperience 后的 currentExperience 应等于之前的值加上增量（减去升级消耗）
**Validates: Requirements 2.1-2.5, 4.1**

### Property 2: 等级提升正确性
*For any* 技能，当 currentExperience >= GetExperienceToNextLevel() 时，等级应自动提升
**Validates: Requirements 4.1, 4.2**

### Property 3: 溢出经验保留
*For any* 升级操作，溢出的经验应保留到下一级
**Validates: Requirements 4.2**

### Property 4: 最大等级限制
*For any* 技能，等级不应超过 maxLevel
**Validates: Requirements 4.4**

### Property 5: 砍树经验配置正确性
*For any* 阶段0或阶段1的树木，GetChopExperience() 应返回 0
**Validates: Requirements 3.1, 3.2**

### Property 6: 砍树经验阶段递增
*For any* 阶段2-5的树木，阶段越高经验越多（2<4<6<20）
**Validates: Requirements 3.3, 3.4, 3.5, 3.6**

## Error Handling

### 技能服务未初始化
- 检查 Instance 引用
- 返回默认值（等级1，经验0）

### 无效技能类型
- 检查枚举范围
- 返回 null 或默认值

### 经验为负数
- 检查 amount <= 0
- 直接返回，不处理

## Testing Strategy

### 单元测试

1. **经验累积测试**
   - 测试添加经验后数值正确
   - 测试升级后经验溢出处理

2. **等级提升测试**
   - 测试达到阈值时自动升级
   - 测试最大等级限制

3. **砍树经验测试**
   - 测试各阶段返回正确经验值
   - 测试阶段0-1返回0

### 属性测试

1. **Property 1: 经验累积正确性**
   - 生成随机经验增量
   - 验证累积结果

2. **Property 5: 砍树经验配置正确性**
   - 测试阶段0-1返回0

## File Structure

```
Assets/Scripts/
├── Data/
│   ├── Enums/
│   │   └── SkillType.cs              # 技能类型枚举（新建）
│   └── SkillData.cs                  # 技能数据结构（新建）
├── Service/
│   └── SkillLevelService.cs          # 技能等级服务（新建）
└── Controller/
    └── TreeControllerV2.cs           # 树木控制器（更新）
```

## Integration Points

### 与 CraftingService 集成

```csharp
// CraftingService.cs
private void Start()
{
    if (SkillLevelService.Instance != null)
    {
        SkillLevelService.Instance.OnLevelUp += OnSkillLevelUp;
    }
}

private void OnSkillLevelUp(SkillType skillType, int newLevel)
{
    // 检查是否有新配方解锁
    CheckRecipeUnlocks(skillType, newLevel);
}
```

### 与 TreeController 集成

```csharp
// TreeControllerV2.cs - ChopDown 方法中
private void ChopDown()
{
    // ... 现有代码 ...
    
    // 添加经验
    int xp = GetChopExperience();
    if (xp > 0 && SkillLevelService.Instance != null)
    {
        SkillLevelService.Instance.AddExperience(SkillType.Woodcutting, xp);
    }
    
    // ... 现有代码 ...
}
```

## UI Integration (Future)

### SkillPanel 设计

```
┌─────────────────────────────────────────────────────────────┐
│  📊 技能面板                                        [✖]    │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 🌾 种植  Lv.3                                          │ │
│ │ [████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░] 45%   │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ ⛏️ 挖矿  Lv.2                                          │ │
│ │ [████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░] 20%   │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 🪓 砍树  Lv.5                                          │ │
│ │ [████████████████████████░░░░░░░░░░░░░░░░░░░░░░] 60%   │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 🎣 钓鱼  Lv.1                                          │ │
│ │ [██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░] 5%    │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ ⚔️ 战斗  Lv.4                                          │ │
│ │ [████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░] 50%   │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```
