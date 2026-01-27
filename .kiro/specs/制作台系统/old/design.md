# Design Document: 配方与制作台系统

## Overview

本设计文档描述配方与制作台系统的架构和实现方案。该系统从 ui-system 和 so-design-system 中抽离，专门管理：
- 不同类型工作台的配方过滤
- 配方解锁机制（等级解锁、隐藏配方）
- 配方显示逻辑（透明度、排序、条件检查）

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      制作台交互层                            │
├─────────────────────────────────────────────────────────────┤
│  玩家与工作台交互 → 打开 CraftingPanel → 显示配方列表        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   CraftingService (已存在)                   │
│  - SetStation(station)                                      │
│  - GetAvailableRecipes() → 过滤 + 排序                      │
│  - CanCraft(recipe) → 材料 + 等级检查                       │
│  - IsRecipeUnlocked(recipe) → 解锁状态检查                  │
└─────────────────────────────────────────────────────────────┘
         │                 │                 │
         ▼                 ▼                 ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────┐
│ ItemDatabase    │ │ InventoryService│ │ SkillLevelService   │
│ - allRecipes    │ │ - GetSlot()     │ │ - GetLevel(type)    │
│ - GetRecipeBy   │ │ - HasItem()     │ │ - OnLevelChanged    │
│   Station()     │ │                 │ │                     │
└─────────────────┘ └─────────────────┘ └─────────────────────┘
```

## Components and Interfaces

### 1. CraftingStation 枚举扩展

```csharp
public enum CraftingStation
{
    None = 0,           // 无需工作台（手工制作）
    Workbench = 1,      // 工作台（武器、金属物品、工具）
    Furnace = 2,        // 熔炉（烧矿、冶炼金属锭）
    AlchemyTable = 3,   // 制药台（药品制作）
    Grill = 4,          // 烧烤架（烤肉、烧烤食物）
    CookingPot = 5      // 烹饪锅（煮食、汤类）
}
```

### 2. RecipeData SO 扩展字段

```csharp
[Header("=== 工作台配置 ===")]
[Tooltip("所属工作台类型")]
public CraftingStation craftingStation = CraftingStation.Workbench;

[Header("=== 解锁条件 ===")]
[Tooltip("解锁所需技能类型")]
public SkillType requiredSkillType = SkillType.Farming;

[Tooltip("解锁所需技能等级")]
public int requiredSkillLevel = 1;

[Tooltip("是否为隐藏配方（非等级解锁）")]
public bool isHiddenRecipe = false;

[Header("=== 运行时状态 ===")]
[NonSerialized]
public bool isUnlocked = false;
```

### 3. CraftingService 扩展方法

```csharp
public class CraftingService : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private ItemDatabase database;
    [SerializeField] private InventoryService inventory;
    [SerializeField] private SkillLevelService skillService;
    
    /// <summary>
    /// 获取当前工作台可用配方（已过滤、已排序）
    /// </summary>
    public List<RecipeData> GetAvailableRecipes()
    {
        var recipes = database.GetRecipesByStation(currentStation);
        
        // 过滤：排除隐藏配方
        recipes = recipes.Where(r => !r.isHiddenRecipe || r.isUnlocked).ToList();
        
        // 过滤：只显示已解锁或可通过升级解锁的配方
        recipes = recipes.Where(r => IsRecipeVisible(r)).ToList();
        
        // 排序：按配方 ID 升序
        recipes = recipes.OrderBy(r => r.recipeID).ToList();
        
        return recipes;
    }
    
    /// <summary>
    /// 检查配方是否应该显示
    /// </summary>
    private bool IsRecipeVisible(RecipeData recipe)
    {
        // 已解锁 → 显示
        if (recipe.isUnlocked) return true;
        
        // 隐藏配方且未解锁 → 不显示
        if (recipe.isHiddenRecipe) return false;
        
        // 可通过升级解锁 → 显示（降低透明度）
        return true;
    }
    
    /// <summary>
    /// 检查配方解锁状态
    /// </summary>
    public bool IsRecipeUnlocked(RecipeData recipe)
    {
        // 默认解锁
        if (recipe.unlockedByDefault) return true;
        
        // 已标记解锁
        if (recipe.isUnlocked) return true;
        
        // 检查等级条件
        if (skillService != null)
        {
            int playerLevel = skillService.GetLevel(recipe.requiredSkillType);
            return playerLevel >= recipe.requiredSkillLevel;
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查是否可以制作（材料 + 等级）
    /// </summary>
    public bool CanCraft(RecipeData recipe)
    {
        // 检查解锁状态
        if (!IsRecipeUnlocked(recipe)) return false;
        
        // 检查材料
        foreach (var material in recipe.materials)
        {
            int owned = GetMaterialCount(material.itemId);
            if (owned < material.amount) return false;
        }
        
        return true;
    }
}
```

### 4. RecipeSlotUI 透明度逻辑

```csharp
public class RecipeSlotUI : MonoBehaviour
{
    [Header("透明度设置")]
    [SerializeField] private float fullAlpha = 1.0f;      // 可制作
    [SerializeField] private float dimmedAlpha = 0.5f;    // 条件不足
    
    /// <summary>
    /// 更新显示状态
    /// </summary>
    public void UpdateDisplay(RecipeData recipe, CraftingService service)
    {
        bool isUnlocked = service.IsRecipeUnlocked(recipe);
        bool canCraft = service.CanCraft(recipe);
        
        // 设置透明度
        float alpha = canCraft ? fullAlpha : dimmedAlpha;
        SetIconAlpha(alpha);
        
        // 显示等级要求（如果未解锁）
        if (!isUnlocked)
        {
            ShowLevelRequirement(recipe.requiredSkillLevel, recipe.requiredSkillType);
        }
        else
        {
            HideLevelRequirement();
        }
        
        // 设置交互状态
        button.interactable = canCraft;
    }
}
```

## Data Models

### 配方显示状态

```csharp
public enum RecipeDisplayState
{
    Craftable,      // 可制作（透明度 1.0）
    MaterialLack,   // 材料不足（透明度 0.5）
    LevelLock,      // 等级不足（透明度 0.5 + 等级提示）
    Hidden          // 隐藏（不显示）
}
```

### 配方显示信息

```csharp
public struct RecipeDisplayInfo
{
    public RecipeData recipe;
    public RecipeDisplayState state;
    public float alpha;
    public string lockMessage;  // "需要等级 X" 或 null
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 工作台配方过滤正确性
*For any* CraftingStation 类型，GetAvailableRecipes() 返回的所有配方的 craftingStation 字段都等于该设施类型
**Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6**

### Property 2: 隐藏配方不显示
*For any* 配方，如果 isHiddenRecipe 为 true 且 isUnlocked 为 false，则该配方不应出现在 GetAvailableRecipes() 返回列表中
**Validates: Requirements 2.3**

### Property 3: 配方排序正确性
*For any* GetAvailableRecipes() 返回的配方列表，配方应按 recipeID 升序排列
**Validates: Requirements 2.5**

### Property 4: 透明度与条件一致性
*For any* 配方，如果 CanCraft() 返回 true，则透明度应为 1.0；否则透明度应为 0.5
**Validates: Requirements 3.1, 3.2, 3.3**

### Property 5: 等级解锁正确性
*For any* 配方和玩家等级，如果玩家等级 >= requiredSkillLevel，则 IsRecipeUnlocked() 返回 true
**Validates: Requirements 2.2, 3.3**

## Error Handling

### 数据库未初始化
- 检查 database 引用
- 返回空列表，输出警告日志

### 技能服务未初始化
- 检查 skillService 引用
- 默认所有配方为已解锁状态

### 配方数据不完整
- 检查 craftingStation 字段
- 默认为 Workbench

## Testing Strategy

### 单元测试

1. **工作台过滤测试**
   - 测试各工作台类型返回正确的配方子集
   - 测试空配方列表处理

2. **解锁状态测试**
   - 测试等级满足时返回 true
   - 测试等级不足时返回 false
   - 测试隐藏配方逻辑

3. **排序测试**
   - 测试配方按 ID 升序排列

### 属性测试

1. **Property 1: 工作台配方过滤正确性**
   - 生成随机配方列表和工作台类型
   - 验证过滤结果

2. **Property 4: 透明度与条件一致性**
   - 生成随机背包状态和配方
   - 验证透明度设置

## File Structure

```
Assets/Scripts/
├── Data/
│   └── Enums/
│       └── CraftingStation.cs        # 工作台枚举（需扩展）
├── Service/
│   └── Crafting/
│       └── CraftingService.cs        # 制作服务（需更新）
└── UI/
    └── Crafting/
        └── RecipeSlotUI.cs           # 配方槽位（需更新）

Assets/Data/
└── Recipes/
    └── *.asset                       # 配方 SO（需更新字段）
```

## UI Flow

### 配方显示流程

```
玩家与工作台交互
        │
        ▼
CraftingPanel.Open(station)
        │
        ▼
CraftingService.SetStation(station)
        │
        ▼
GetAvailableRecipes()
        │
        ├─ 过滤：排除隐藏配方
        ├─ 过滤：只显示可见配方
        └─ 排序：按 ID 升序
        │
        ▼
foreach (recipe in recipes)
        │
        ├─ IsRecipeUnlocked(recipe)?
        │   ├─ true → 检查材料
        │   │   ├─ 材料足够 → alpha=1.0
        │   │   └─ 材料不足 → alpha=0.5
        │   └─ false → alpha=0.5 + "需要等级 X"
        │
        └─ RecipeSlotUI.Setup(recipe, displayInfo)
```

### 配方解锁流程

```
SkillLevelService.OnLevelChanged
        │
        ▼
CraftingService.OnSkillLevelChanged(skillType, newLevel)
        │
        ▼
foreach (recipe in allRecipes)
        │
        ├─ recipe.requiredSkillType == skillType?
        │   └─ newLevel >= recipe.requiredSkillLevel?
        │       └─ recipe.isUnlocked = true
        │
        └─ OnRecipeUnlocked?.Invoke(recipe)
        │
        ▼
CraftingPanel.RefreshRecipeList()
```
