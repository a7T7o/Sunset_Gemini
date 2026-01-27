# 设计文档 - 物品放置系统

## 概述

物品放置系统是一个统一的放置管理框架，允许玩家将可放置物品（树苗、装饰物、建筑等）放置到游戏世界中。系统采用事件驱动架构，与现有的背包系统、输入系统、树木系统进行解耦集成。

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                      GameInputManager                        │
│                    (输入检测与分发)                           │
└─────────────────────┬───────────────────────────────────────┘
                      │ 左键点击
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    PlacementManager                          │
│              (放置管理器 - 核心控制器)                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ 放置模式    │  │ 放置验证    │  │ 事件广播            │  │
│  │ 状态管理    │  │ PlacementV. │  │ OnItemPlaced        │  │
│  └─────────────┘  └─────────────┘  │ OnSaplingPlanted    │  │
│                                     └─────────────────────┘  │
└─────────────────────┬───────────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
┌───────────┐  ┌───────────┐  ┌───────────────┐
│ Placement │  │ Inventory │  │ TreeController│
│ Preview   │  │ Service   │  │ V2            │
│ (预览UI)  │  │ (背包扣除)│  │ (树木初始化) │
└───────────┘  └───────────┘  └───────────────┘
```

## 组件和接口

### 1. PlacementManager（放置管理器）

**职责**：统一管理放置模式、验证、执行和事件广播

```csharp
public class PlacementManager : MonoBehaviour
{
    // 单例
    public static PlacementManager Instance { get; private set; }
    
    // 状态
    public bool IsPlacementMode { get; private set; }
    public ItemData CurrentPlacementItem { get; private set; }
    
    // 事件
    public static event Action<PlacementEventData> OnItemPlaced;
    public static event Action<SaplingPlantedEventData> OnSaplingPlanted;
    
    // 核心方法
    public void EnterPlacementMode(ItemData item);
    public void ExitPlacementMode();
    public bool TryPlace(Vector3 worldPosition);
    public bool CanPlaceAt(Vector3 worldPosition);
    public void UndoLastPlacement();
}
```

### 2. PlacementValidator（放置验证器）

**职责**：验证放置位置是否有效

```csharp
public class PlacementValidator
{
    // 验证方法
    public PlacementValidationResult Validate(ItemData item, Vector3 position);
    public bool IsValidSaplingPosition(SaplingData sapling, Vector3 position);
    public bool IsWithinPlayerRange(Vector3 position);
    public bool IsOnFarmland(Vector3 position);
    public bool HasObstacleInMargin(Vector3 position, float vMargin, float hMargin, string[] tags);
}

public struct PlacementValidationResult
{
    public bool IsValid;
    public PlacementInvalidReason Reason;
    public string Message;
}

public enum PlacementInvalidReason
{
    None,
    OutOfRange,
    ObstacleBlocking,
    OnFarmland,
    OnWater,
    InsufficientSpace,
    WrongSeason,
    InvalidTerrain
}
```

### 3. PlacementPreview（放置预览）

**职责**：显示放置预览和有效性指示

```csharp
public class PlacementPreview : MonoBehaviour
{
    // 预览状态
    public void Show(ItemData item);
    public void Hide();
    public void UpdatePosition(Vector3 worldPosition);
    public void SetValid(bool isValid);
    public void SetOutOfRange(bool outOfRange);
    
    // 树苗边距指示器
    public void ShowMarginIndicator(float vMargin, float hMargin);
    public void HideMarginIndicator();
}
```

### 4. SaplingData（树苗数据）

**职责**：定义树苗特有属性

```csharp
[CreateAssetMenu(fileName = "Sapling_New", menuName = "Farm/Items/Sapling", order = 2)]
public class SaplingData : ItemData
{
    [Header("=== 树苗专属属性 ===")]
    public GameObject treePrefab;           // 关联的树木预制体
    public Season plantableSeason;          // 可种植季节（AllSeason = 全季节）
    public int plantingExp;                 // 种植经验值
    
    // 验证
    protected override void OnValidate();
}
```

### 5. ItemData 扩展

**职责**：在基类中添加放置相关字段

```csharp
// 在 ItemData.cs 中添加
[Header("=== 放置配置 ===")]
public bool isPlaceable = false;
public PlacementType placementType = PlacementType.None;
public GameObject placementPrefab;

// 建筑专用
public Vector2Int buildingSize = Vector2Int.one;
```

### 6. PlacementType 枚举

```csharp
public enum PlacementType
{
    None = 0,
    Sapling = 1,      // 树苗（整数坐标，成长边距检测）
    Decoration = 2,   // 装饰物（自由放置）
    Building = 3,     // 建筑（多格子占用）
    Furniture = 4     // 家具（室内放置）
}
```

## 数据模型

### PlacementEventData（放置事件数据）

```csharp
public struct PlacementEventData
{
    public Vector3 Position;
    public ItemData ItemData;
    public GameObject PlacedObject;
    public PlacementType PlacementType;
    public float Timestamp;
}
```

### SaplingPlantedEventData（树苗种植事件数据）

```csharp
public struct SaplingPlantedEventData
{
    public Vector3 Position;
    public SaplingData SaplingData;
    public GameObject TreeObject;
    public TreeControllerV2 TreeController;
    public float Timestamp;
}
```

### PlacementHistoryEntry（放置历史记录）

```csharp
public class PlacementHistoryEntry
{
    public PlacementEventData EventData;
    public float PlacementTime;
    public bool CanUndo => Time.time - PlacementTime < 5f;
}
```

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 整数坐标对齐
*For any* 浮点坐标输入，树苗放置位置应对齐到最近的整数坐标
**Validates: Requirements 4.1**

### Property 2: 放置后背包扣除
*For any* 成功的放置操作，玩家背包中该物品数量应减少 1
**Validates: Requirements 1.5**

### Property 3: 障碍物检测
*For any* 放置位置，如果成长边距范围内存在障碍物 Collider，则放置应被拒绝
**Validates: Requirements 4.3, 4.4**

### Property 4: 事件广播完整性
*For any* 成功的放置操作，广播的事件数据应包含正确的位置、物品数据和实例化对象
**Validates: Requirements 5.1, 5.2, 5.3**

### Property 5: 树苗初始化阶段
*For any* 成功的树苗放置，实例化的树木应处于阶段 0（树苗）
**Validates: Requirements 4.6**

### Property 6: 撤销时间限制
*For any* 放置操作，超过 5 秒后撤销应被拒绝
**Validates: Requirements 9.5**

### Property 7: 农田系统协调
*For any* 放置位置，如果位于耕地上，树苗和装饰物放置应被拒绝
**Validates: Requirements 10.2, 10.3**

## 错误处理

| 错误场景 | 处理方式 |
|---------|---------|
| 放置位置超出范围 | 显示灰色预览，点击无效 |
| 障碍物阻挡 | 显示红色预览，播放错误音效 |
| 背包物品不足 | 退出放置模式 |
| 预制体缺失 | 记录错误日志，跳过放置 |
| 树木预制体无 TreeControllerV2 | OnValidate 警告 |

## 测试策略

### 单元测试
- PlacementValidator.Validate() 各种场景
- 整数坐标对齐算法
- 事件数据构造

### 属性测试
使用 NUnit + 自定义生成器：
- 随机坐标的整数对齐
- 随机障碍物配置的检测
- 随机放置序列的背包扣除

### 集成测试
- PlacementManager + InventoryService 集成
- PlacementManager + TreeControllerV2 集成
- 输入系统集成

## 文件结构

```
Assets/YYY_Scripts/
├── Service/
│   └── Placement/
│       ├── PlacementManager.cs
│       ├── PlacementValidator.cs
│       └── PlacementPreview.cs
├── Data/
│   ├── Items/
│   │   └── SaplingData.cs
│   └── Enums/
│       └── PlacementEnums.cs
└── Events/
    └── PlacementEvents.cs
```
