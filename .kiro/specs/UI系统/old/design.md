# Design Document: 制作台 UI 系统

## Overview

本设计文档描述制作台 UI 系统的架构和实现方案。制作台是玩家制作物品的核心界面，需要：
- 显示当前设施可用的配方列表
- 检查玩家是否有足够材料
- 执行制作并更新背包

设计参考 `InventoryService` 的服务模式，采用事件驱动架构。

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      UI Layer                                │
├─────────────────┬─────────────────┬─────────────────────────┤
│ CraftingPanel   │ RecipeSlotUI    │ MaterialItemUI          │
│ (面板容器)      │ (配方槽位)      │ (材料项显示)            │
└────────┬────────┴────────┬────────┴────────┬────────────────┘
         │                 │                 │
         ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────┐
│                   CraftingService (新增)                     │
│  - GetRecipesForStation(station)                            │
│  - CanCraft(recipe)                                         │
│  - Craft(recipe)                                            │
│  - Events: OnCraftSuccess, OnCraftFailed                    │
└─────────────────────────────────────────────────────────────┘
         │                 │
         ▼                 ▼
┌─────────────────┐ ┌─────────────────────────────────────────┐
│ ItemDatabase    │ │ InventoryService                        │
│ - allRecipes    │ │ - GetSlot(index)                        │
│ - GetRecipeBy   │ │ - AddItem(id, quality, amount)          │
│   Station()     │ │ - RemoveFromSlot(index, amount)         │
└─────────────────┘ └─────────────────────────────────────────┘
```

## Components and Interfaces

### 1. CraftingService (新增)

制作服务，处理制作逻辑，学习 InventoryService 的设计模式。

```csharp
public class CraftingService : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private ItemDatabase database;
    [SerializeField] private InventoryService inventory;
    
    // 当前设施类型
    private CraftingStation currentStation;
    
    // 事件
    public event Action<RecipeData> OnCraftSuccess;
    public event Action<RecipeData, string> OnCraftFailed;
    public event Action OnRecipeListChanged;
    
    // 核心方法
    public void SetStation(CraftingStation station);
    public List<RecipeData> GetAvailableRecipes();
    public bool CanCraft(RecipeData recipe);
    public CraftResult TryCraft(RecipeData recipe);
    public int GetMaterialCount(int itemId);
    public bool IsRecipeUnlocked(RecipeData recipe);
}

public struct CraftResult
{
    public bool success;
    public string message;
    public int resultItemId;
    public int resultAmount;
}
```

### 2. CraftingPanel (新增)

制作台 UI 面板，管理配方列表和制作交互。

```csharp
public class CraftingPanel : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeSlotPrefab;
    [SerializeField] private RecipeDetailPanel detailPanel;
    [SerializeField] private Button craftButton;
    [SerializeField] private Text craftButtonText;
    
    [Header("服务")]
    [SerializeField] private CraftingService craftingService;
    
    // 当前选中的配方
    private RecipeData selectedRecipe;
    private List<RecipeSlotUI> recipeSlots = new List<RecipeSlotUI>();
    
    // 核心方法
    public void Open(CraftingStation station);
    public void Close();
    public void SelectRecipe(RecipeData recipe);
    public void OnCraftButtonClick();
    private void RefreshRecipeList();
    private void RefreshMaterialStatus();
}
```

### 3. RecipeSlotUI (新增)

单个配方槽位 UI 组件。

```csharp
public class RecipeSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Image lockOverlay;
    [SerializeField] private Button button;
    
    private RecipeData recipe;
    private CraftingPanel panel;
    
    public void Setup(RecipeData recipe, CraftingPanel panel, bool unlocked);
    public void SetSelected(bool selected);
    private void OnClick();
}
```

### 4. RecipeDetailPanel (新增)

配方详情面板，显示材料列表和制作信息。

```csharp
public class RecipeDetailPanel : MonoBehaviour
{
    [SerializeField] private Image resultIcon;
    [SerializeField] private Text resultName;
    [SerializeField] private Text resultAmount;
    [SerializeField] private Transform materialListContainer;
    [SerializeField] private GameObject materialItemPrefab;
    [SerializeField] private Text descriptionText;
    
    public void ShowRecipe(RecipeData recipe, CraftingService service);
    public void Clear();
    private void RefreshMaterials(RecipeData recipe, CraftingService service);
}
```

### 5. MaterialItemUI (新增)

材料项 UI 组件，显示单个材料的需求和持有状态。

```csharp
public class MaterialItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text amountText;  // 格式: "持有/需要"
    [SerializeField] private Image statusIcon; // 绿勾/红叉
    
    [Header("颜色")]
    [SerializeField] private Color sufficientColor = Color.green;
    [SerializeField] private Color insufficientColor = Color.red;
    
    public void Setup(int itemId, int required, int owned, ItemDatabase database);
}
```

## Data Models

### 制作结果

```csharp
public struct CraftResult
{
    public bool success;           // 是否成功
    public string message;         // 结果消息
    public int resultItemId;       // 产物 ID
    public int resultAmount;       // 产物数量
    public FailReason failReason;  // 失败原因
}

public enum FailReason
{
    None,
    InsufficientMaterials,  // 材料不足
    InventoryFull,          // 背包已满
    RecipeLocked,           // 配方未解锁
    LevelTooLow             // 等级不足
}
```

### 材料状态

```csharp
public struct MaterialStatus
{
    public int itemId;
    public string itemName;
    public Sprite icon;
    public int required;    // 需要数量
    public int owned;       // 持有数量
    public bool sufficient; // 是否足够
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 设施配方过滤正确性
*For any* CraftingStation 类型，GetAvailableRecipes() 返回的所有配方的 requiredStation 字段都等于该设施类型
**Validates: Requirements 1.1, 4.1, 4.2, 4.3**

### Property 2: 材料检查一致性
*For any* 配方和背包状态，CanCraft() 返回 true 当且仅当背包中每种材料的数量都大于等于配方要求
**Validates: Requirements 2.1, 2.4**

### Property 3: 制作材料扣除正确性
*For any* 成功的制作操作，制作后背包中每种材料的数量等于制作前数量减去配方要求数量
**Validates: Requirements 3.1**

### Property 4: 制作产物添加正确性
*For any* 成功的制作操作，制作后背包中产物的数量增加了配方定义的 resultAmount
**Validates: Requirements 3.2**

### Property 5: 解锁状态过滤正确性
*For any* 配方，如果 unlockedByDefault 为 true，则 IsRecipeUnlocked() 返回 true
**Validates: Requirements 5.1**

### Property 6: 等级检查正确性
*For any* 配方和玩家等级，如果玩家等级小于 requiredLevel，则 IsRecipeUnlocked() 返回 false
**Validates: Requirements 5.2**

## Error Handling

### 材料不足
- CanCraft() 返回 false
- TryCraft() 返回 FailReason.InsufficientMaterials
- UI 显示红色材料项和禁用的制作按钮

### 背包已满
- TryCraft() 在扣除材料前检查背包空间
- 空间不足时返回 FailReason.InventoryFull
- 不扣除任何材料，保持原状态

### 配方未解锁
- IsRecipeUnlocked() 返回 false
- UI 显示锁定图标
- 点击时显示解锁条件提示

### 数据库未初始化
- 检查 database 和 inventory 引用
- 缺失时在控制台输出错误日志
- 返回空列表或失败结果

## Testing Strategy

### 单元测试

使用 Unity Test Framework (NUnit)：

1. **设施过滤测试**
   - 测试各设施类型返回正确的配方子集
   - 测试空配方列表处理

2. **材料检查测试**
   - 测试材料充足时返回 true
   - 测试材料不足时返回 false
   - 测试边界情况（刚好足够）

3. **制作逻辑测试**
   - 测试成功制作后的背包状态
   - 测试失败时背包不变

### 属性测试

1. **Property 1: 设施配方过滤正确性**
   - 生成随机配方列表和设施类型
   - 验证过滤结果

2. **Property 2: 材料检查一致性**
   - 生成随机背包状态和配方
   - 验证 CanCraft 结果与手动计算一致

3. **Property 3 & 4: 制作操作正确性**
   - 生成随机可制作场景
   - 验证制作前后背包变化

## File Structure

```
Assets/Scripts/
├── Service/
│   └── Crafting/
│       └── CraftingService.cs        # 新增：制作服务
├── UI/
│   └── Crafting/
│       ├── CraftingPanel.cs          # 新增：制作面板
│       ├── RecipeSlotUI.cs           # 新增：配方槽位
│       ├── RecipeDetailPanel.cs      # 新增：配方详情
│       └── MaterialItemUI.cs         # 新增：材料项

Assets/Prefabs/
└── UI/
    └── Crafting/
        ├── CraftingPanel.prefab      # 新增：制作面板预制体
        ├── RecipeSlot.prefab         # 新增：配方槽位预制体
        └── MaterialItem.prefab       # 新增：材料项预制体
```

## UI Design

### 制作台面板布局

```
┌─────────────────────────────────────────────────────────────┐
│  🔨 工作台                                          [✖]    │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────────────┐ ┌─────────────────────────────────┐ │
│ │ 📜 配方列表         │ │ 📋 配方详情                     │ │
│ │ ┌─────────────────┐ │ │ ┌─────────────────────────────┐ │ │
│ │ │ [图] 铜锭       │ │ │ │      [产物图标]             │ │ │
│ │ │ [图] 铁锭       │ │ │ │      铜锭 x1                │ │ │
│ │ │ [图] 金锭       │ │ │ └─────────────────────────────┘ │ │
│ │ │ [🔒] 秘银锭     │ │ │                                 │ │
│ │ │                 │ │ │ 所需材料：                      │ │
│ │ │                 │ │ │ ┌─────────────────────────────┐ │ │
│ │ │                 │ │ │ │ [图] 铜矿石  5/5  ✓        │ │ │
│ │ │                 │ │ │ │ [图] 煤炭    2/3  ✗        │ │ │
│ │ │                 │ │ │ └─────────────────────────────┘ │ │
│ │ │                 │ │ │                                 │ │
│ │ │                 │ │ │ 制作时间：5 秒                  │ │
│ │ │                 │ │ │ 获得经验：10                    │ │
│ │ └─────────────────┘ │ │                                 │ │
│ └─────────────────────┘ │ ┌─────────────────────────────┐ │ │
│                         │ │      [🔨 制作]              │ │ │
│                         │ └─────────────────────────────┘ │ │
│                         └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 交互流程

```
玩家与制作设施交互
        │
        ▼
CraftingPanel.Open(station)
        │
        ▼
CraftingService.SetStation(station)
        │
        ▼
GetAvailableRecipes() → 过滤配方
        │
        ▼
RefreshRecipeList() → 生成 RecipeSlotUI
        │
        ▼
玩家点击配方槽位
        │
        ▼
SelectRecipe(recipe)
        │
        ▼
RecipeDetailPanel.ShowRecipe()
        │
        ▼
RefreshMaterialStatus() → 检查材料
        │
        ▼
玩家点击制作按钮
        │
        ▼
CraftingService.TryCraft(recipe)
        │
        ├─ 成功 → OnCraftSuccess → 刷新 UI
        │
        └─ 失败 → OnCraftFailed → 显示提示
```

