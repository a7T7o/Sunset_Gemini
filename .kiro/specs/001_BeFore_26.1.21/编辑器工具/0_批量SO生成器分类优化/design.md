# 设计文档 - 批量 SO 生成器分类优化

## 概述

本设计文档描述批量物品 SO 生成器的分类系统重构，将扁平的物品类型按钮改为大类+小类的层级结构，同时新增对可放置物品类型（工作台、存储、交互展示、简单事件）的支持。

---

## 架构设计

### 枚举定义

```csharp
/// <summary>
/// 物品大类 - 顶级分类
/// </summary>
public enum ItemMainCategory
{
    ToolEquipment = 0,  // 工具装备
    Planting = 1,       // 种植类
    Placeable = 2,      // 可放置
    Consumable = 3,     // 消耗品
    Material = 4,       // 材料
    Other = 5           // 其他
}

/// <summary>
/// 物品 SO 类型 - 对应实际数据类（扩展版）
/// </summary>
public enum ItemSOType
{
    // 工具装备
    ToolData = 0,
    WeaponData = 1,
    
    // 种植类
    SeedData = 10,
    CropData = 11,
    
    // 可放置
    SaplingData = 20,
    WorkstationData = 21,
    StorageData = 22,
    InteractiveDisplayData = 23,
    SimpleEventData = 24,
    
    // 消耗品
    FoodData = 30,
    PotionData = 31,
    
    // 材料
    MaterialData = 40,
    
    // 其他
    ItemData = 50,
    FurnitureData = 51,
    SpecialData = 52
}
```

### 大类到小类映射

| 大类 | 小类列表 | 颜色 |
|------|----------|------|
| 工具装备 | ToolData, WeaponData | 橙色 |
| 种植类 | SeedData, CropData | 绿色 |
| 可放置 | SaplingData, WorkstationData, StorageData, InteractiveDisplayData, SimpleEventData | 青色 |
| 消耗品 | FoodData, PotionData | 粉色 |
| 材料 | MaterialData（含子类：矿石/锭/自然/怪物） | 紫色 |
| 其他 | ItemData, FurnitureData, SpecialData | 灰色 |

### ID 范围映射

| 小类 | 起始 ID | ID 范围说明 |
|------|---------|-------------|
| ToolData | 0 | 00XX 农具, 01XX 采集 |
| WeaponData | 200 | 02XX 武器 |
| SeedData | 1000 | 10XX 种子 |
| CropData | 1100 | 11XX 作物 |
| SaplingData | 1200 | 12XX 树苗 |
| WorkstationData | 1300 | 13XX 工作台 |
| StorageData | 1400 | 14XX 存储容器 |
| InteractiveDisplayData | 1500 | 15XX 交互展示 |
| SimpleEventData | 1600 | 16XX 简单事件 |
| FoodData | 5000 | 50XX 简单, 51XX 高级 |
| PotionData | 4000 | 40XX 药水 |
| MaterialData | 3200 | 30XX-33XX 根据子类 |
| ItemData | 0 | 通用 |
| FurnitureData | 6000 | 6XXX 家具 |
| SpecialData | 7000 | 7XXX 特殊 |

---

## 组件与接口

### CategoryMapping 静态类

```csharp
public static class CategoryMapping
{
    // 获取大类下的所有小类
    public static ItemSOType[] GetSubTypes(ItemMainCategory category);
    
    // 获取小类的起始 ID
    public static int GetStartID(ItemSOType type);
    
    // 获取小类的输出路径
    public static string GetOutputFolder(ItemSOType type);
    
    // 获取小类的显示名称
    public static string GetDisplayName(ItemSOType type);
    
    // 获取大类的显示名称
    public static string GetCategoryName(ItemMainCategory category);
    
    // 获取大类的颜色
    public static Color GetCategoryColor(ItemMainCategory category);
}
```

---

## 数据模型

### 新增 SO 类型字段

#### WorkstationData 专属设置

| 字段 | 类型 | 说明 |
|------|------|------|
| workstationType | WorkstationType | 工作台类型 |
| requiresFuel | bool | 是否需要燃料 |
| fuelSlotCount | int | 燃料槽数量 |

#### StorageData 专属设置

| 字段 | 类型 | 说明 |
|------|------|------|
| storageCapacity | int | 存储容量 |
| isLockable | bool | 是否可上锁 |

#### InteractiveDisplayData 专属设置

| 字段 | 类型 | 说明 |
|------|------|------|
| displayTitle | string | 显示标题 |
| displayContent | string | 显示内容 |
| displayDuration | float | 显示时长 |

#### SimpleEventData 专属设置

| 字段 | 类型 | 说明 |
|------|------|------|
| eventType | SimpleEventType | 事件类型 |
| isOneTime | bool | 是否一次性 |
| cooldownTime | float | 冷却时间 |

---

## UI 布局设计

### 物品类型区域

```
┌─────────────────────────────────────────────────────────────┐
│ 📋 物品类型                                                  │
├─────────────────────────────────────────────────────────────┤
│ 大类：                                                       │
│ ┌────────┬────────┬────────┬────────┬────────┬────────┐    │
│ │工具装备│ 种植类 │ 可放置 │ 消耗品 │  材料  │  其他  │    │
│ └────────┴────────┴────────┴────────┴────────┴────────┘    │
│                                                              │
│ 小类：                                                       │
│ ┌────────┬────────┬────────┬────────┬────────┐             │
│ │  树苗  │ 工作台 │  存储  │交互展示│简单事件│             │
│ └────────┴────────┴────────┴────────┴────────┘             │
│                                                              │
│ ℹ️ 可放置 > 工作台 - 工作台、熔炉、制作设施等               │
│    ID 范围：13XX                                             │
└─────────────────────────────────────────────────────────────┘
```

### 按钮样式

- 选中的大类：使用对应颜色高亮
- 未选中的大类：白色背景
- 选中的小类：使用大类颜色高亮
- 未选中的小类：浅灰色背景

---

## 错误处理

### 类型不存在处理

```csharp
// 如果 SO 类型对应的数据类不存在，显示警告
if (!TypeExists(soType))
{
    EditorGUILayout.HelpBox($"⚠️ {GetDisplayName(soType)} 数据类尚未实现", MessageType.Warning);
    GUI.enabled = false;
}
```

### 预制体缺失处理

```csharp
// 可放置物品必须有预制体
if (IsPlaceableType(soType) && placementPrefab == null)
{
    EditorGUILayout.HelpBox("⚠️ 可放置物品需要设置放置预制体", MessageType.Warning);
}
```

---

## 测试策略

### 单元测试

1. **映射关系测试**：验证大类到小类的映射正确
2. **ID 范围测试**：验证每个小类的起始 ID 正确
3. **路径生成测试**：验证输出路径格式正确

### 集成测试

1. **SO 生成测试**：验证各类型 SO 能正确生成
2. **设置持久化测试**：验证关闭重开后设置恢复

---

## 正确性属性

*属性是系统应满足的通用规则，用于验证实现的正确性。*

### 属性 1：大类小类映射完整性

**对于任意** ItemMainCategory 值，GetSubTypes() 返回的数组 SHALL 非空且所有元素有效
**验证**: Requirements 2.1-2.6

### 属性 2：ID 范围唯一性

**对于任意** 两个不同的 ItemSOType，其起始 ID SHALL 不重叠（除非是同一大类的子类型）
**验证**: Requirements 3.1-3.12

### 属性 3：输出路径有效性

**对于任意** ItemSOType，GetOutputFolder() 返回的路径 SHALL 以 "Assets/" 开头且格式有效
**验证**: Requirements 4.1-4.5

### 属性 4：设置持久化一致性

**对于任意** 保存的大类和小类组合，重新加载后 SHALL 恢复到相同的选择状态
**验证**: Requirements 7.1-7.3

