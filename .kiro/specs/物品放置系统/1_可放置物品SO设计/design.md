# 设计文档 - 可放置物品 SO 设计

## 概述

本设计文档描述可放置物品 ScriptableObject 的架构设计，包括类继承结构、字段定义、编辑器扩展等。

---

## 架构设计

### 类继承结构

```
ItemData (基类)
└── PlaceableItemData (可放置物品基类) [新增]
    ├── SaplingData (树苗) [优化]
    ├── WorkstationData (工作台) [新增]
    ├── StorageData (存储容器) [新增]
    ├── InteractiveDisplayData (交互展示) [新增]
    └── SimpleEventData (简单事件) [新增]
```

### 设计原则

1. **单一预制体原则**：每个可放置物品只需配置一个预制体
2. **自动推断原则**：系统自动处理季节、样式等运行时逻辑
3. **隐藏冗余字段**：子类自动隐藏父类中不需要的字段
4. **统一交互接口**：所有可放置物品实现统一的交互接口

---

## 组件与接口

### IPlaceableItem 接口

```csharp
public interface IPlaceableItem
{
    PlacementType GetPlacementType();
    GameObject GetPlacementPrefab();
    bool CanPlaceAt(Vector3 position);
    void OnPlaced(Vector3 position, GameObject instance);
    void OnRemoved(GameObject instance);
}
```

### IInteractable 接口（已有，复用）

```csharp
public interface IInteractable
{
    void OnInteract(PlayerController player);
    string GetInteractionPrompt();
}
```

---

## 数据模型

### PlaceableItemData（可放置物品基类）

| 字段 | 类型 | 说明 |
|------|------|------|
| placementPrefab | GameObject | 放置预制体（必填） |
| placementOffset | Vector3 | 放置位置偏移 |
| canRotate | bool | 是否可旋转放置 |
| rotationStep | float | 旋转步进角度（默认 90） |

**自动设置**：
- `isPlaceable = true`（在 OnValidate 中）
- `placementType` 由子类指定

**隐藏字段**（使用 CustomEditor）：
- ItemData 中的 `isPlaceable`
- ItemData 中的 `placementType`
- ItemData 中的 `buildingSize`

### SaplingData（树苗）优化

| 字段 | 类型 | 说明 |
|------|------|------|
| treePrefab | GameObject | 树木预制体（唯一需要填写的字段） |
| plantingExp | int | 种植经验值 |
| heldSprite | Sprite | 手持显示 Sprite（可选） |

**移除字段**：
- ~~plantableSeason~~（改为运行时获取当前季节）
- ~~placementPrefab~~（自动设置为 treePrefab）

**自动逻辑**：
- 放置时自动获取当前季节
- 冬季阻止放置并输出提示
- 早夏/晚秋等过渡季节随机选择样式

### WorkstationData（工作台）

| 字段 | 类型 | 说明 |
|------|------|------|
| workstationType | WorkstationType | 工作台类型枚举 |
| recipeListRef | RecipeList | 可制作配方列表 |
| craftTimeMultiplier | float | 制作时间倍率 |
| requiresFuel | bool | 是否需要燃料 |
| fuelSlotCount | int | 燃料槽数量 |
| acceptedFuelTypes | ItemCategory[] | 可接受的燃料类型 |

**WorkstationType 枚举**：
```csharp
public enum WorkstationType
{
    Heating = 0,        // 取暖设施（壁炉、火盆等）
    Smelting = 1,       // 冶炼设施（熔炉、高炉等）
    Pharmacy = 2,       // 制药设施（炼药台、蒸馏器等）
    Sawmill = 3,        // 锯木设施（劈柴架、锯木台等）
    Cooking = 4,        // 烹饪设施（厨房、烤炉等）
    Crafting = 5        // 装备和工具制作设施（工作台、铁砧等）
}
```

### StorageData（存储容器）

| 字段 | 类型 | 说明 |
|------|------|------|
| storageCapacity | int | 存储容量（格子数） |
| storageRows | int | 显示行数 |
| storageCols | int | 显示列数 |
| allowedCategories | ItemCategory[] | 允许存放的物品类型（空=全部） |
| isLockable | bool | 是否可上锁 |
| defaultLocked | bool | 默认是否上锁 |

### InteractiveDisplayData（交互展示）

| 字段 | 类型 | 说明 |
|------|------|------|
| displayTitle | string | 显示标题 |
| displayContent | string | 显示内容（支持多行） |
| displayImage | Sprite | 显示图片（可选） |
| displayDuration | float | 显示时长（0=手动关闭） |
| closeOnInteract | bool | 再次交互是否关闭 |

### SimpleEventData（简单事件）

| 字段 | 类型 | 说明 |
|------|------|------|
| eventType | SimpleEventType | 事件类型 |
| eventParameter | string | 事件参数（JSON 或简单值） |
| isOneTime | bool | 是否一次性触发 |
| cooldownTime | float | 冷却时间（秒） |
| triggerSound | AudioClip | 触发音效 |

**SimpleEventType 枚举**：
```csharp
public enum SimpleEventType
{
    PlaySound = 0,      // 播放音效
    PlayAnimation = 1,  // 播放动画
    Teleport = 2,       // 传送
    Unlock = 3,         // 解锁
    GiveItem = 4,       // 给予物品
    TriggerQuest = 5,   // 触发任务
    ShowMessage = 6     // 显示消息
}
```

---

## 错误处理

### 冬季种植阻止

```csharp
// PlacementManager.TryPlaceSapling()
if (SeasonManager.Instance.CurrentSeason == Season.Winter)
{
    Debug.Log("[PlacementManager] 冬天无法种植树木");
    // TODO: 显示 UI 提示
    OnPlacementFailed?.Invoke(PlacementInvalidReason.WrongSeason);
    return false;
}
```

### 季节样式选择

```csharp
// SaplingData.GetSeasonalStyle()
public int GetSeasonalStyleIndex()
{
    var season = SeasonManager.Instance.CurrentSeason;
    var vegSeason = SeasonManager.Instance.CurrentVegetationSeason;
    
    // 过渡季节随机选择
    if (vegSeason == VegetationSeason.LateSpringEarlySummer)
    {
        return Random.value > 0.5f ? 0 : 1; // 春或夏
    }
    // ... 其他过渡季节处理
    
    return (int)season;
}
```

---

## 测试策略

### 单元测试

1. **树苗季节测试**：验证不同季节下的样式选择
2. **冬季阻止测试**：验证冬季无法放置树苗
3. **工作台配方测试**：验证配方列表正确加载

### 集成测试

1. **放置流程测试**：完整的放置→交互→移除流程
2. **存储容器测试**：物品存取功能
3. **事件触发测试**：各类事件正确触发

---

## 正确性属性

*属性是系统应满足的通用规则，用于验证实现的正确性。*

### 属性 1：可放置物品自动标记

**对于任意** PlaceableItemData 子类实例，其 `isPlaceable` 属性 SHALL 始终为 true
**验证**: Requirements 2.2

### 属性 2：冬季种植阻止

**对于任意** 树苗放置请求，当当前季节为冬季时，系统 SHALL 返回失败并输出提示
**验证**: Requirements 1.4

### 属性 3：季节样式一致性

**对于任意** 树苗放置，放置后的树木样式 SHALL 与当前季节或过渡季节匹配
**验证**: Requirements 1.2, 1.3

### 属性 4：存储容量限制

**对于任意** 存储容器，存储的物品数量 SHALL 不超过配置的 storageCapacity
**验证**: Requirements 4.2

### 属性 5：一次性事件不重复

**对于任意** 标记为一次性的简单事件，触发后 SHALL 不再响应交互
**验证**: Requirements 6.2
