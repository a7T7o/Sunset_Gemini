# SO 字段设计分析

**创建日期**: 2026-01-27  
**关联工作区**: `.kiro/specs/SO设计系统/0_字段优化重构/`

---

## 一、当前 ItemData 基类字段清单

### 基础识别信息
| 字段 | 类型 | 说明 | 应该在哪些 SO 显示 |
|------|------|------|-------------------|
| itemID | int | 物品唯一ID | ✅ 所有 |
| itemName | string | 物品名称 | ✅ 所有 |
| description | string | 物品描述 | ✅ 所有 |
| category | ItemCategory | 物品大类 | ✅ 所有 |

### 视觉资源
| 字段 | 类型 | 说明 | 应该在哪些 SO 显示 |
|------|------|------|-------------------|
| icon | Sprite | 基础图标 | ✅ 所有 |
| bagSprite | Sprite | 背包显示图标 | ✅ 所有 |
| rotateBagIcon | bool | 背包图标是否旋转 | ✅ 所有 |
| worldPrefab | GameObject | 世界预制体 | ✅ 所有 |

### 经济属性
| 字段 | 类型 | 说明 | 应该在哪些 SO 显示 |
|------|------|------|-------------------|
| buyPrice | int | 购买价格 | ✅ 所有 |
| sellPrice | int | 出售价格 | ✅ 所有 |

### 堆叠属性
| 字段 | 类型 | 说明 | 应该在哪些 SO 显示 |
|------|------|------|-------------------|
| maxStackSize | int | 最大堆叠数量 | ✅ 所有 |

### 显示尺寸配置
| 字段 | 类型 | 说明 | 应该在哪些 SO 显示 |
|------|------|------|-------------------|
| useCustomBagDisplaySize | bool | 启用自定义背包尺寸 | ✅ 所有 |
| bagDisplayPixelSize | int | 背包图标像素尺寸 | ✅ 所有 |
| bagDisplayOffset | Vector2 | 背包图标偏移 | ✅ 所有 |
| useCustomDisplaySize | bool | 启用自定义世界尺寸 | ✅ 所有 |
| displayPixelSize | int | 世界物品像素尺寸 | ✅ 所有 |
| worldDisplayOffset | Vector2 | 世界物品偏移 | ✅ 所有 |

### 功能标记
| 字段 | 类型 | 说明 | 应该在哪些 SO 显示 |
|------|------|------|-------------------|
| canBeDiscarded | bool | 是否可丢弃 | ✅ 所有 |
| isQuestItem | bool | 是否任务物品 | ✅ 所有 |

### 🔴 放置配置（问题区域）
| 字段 | 类型 | 说明 | 当前显示 | 应该显示 |
|------|------|------|---------|---------|
| isPlaceable | bool | 是否可放置 | ✅ 所有 | ❌ 只在 PlaceableItemData |
| placementType | PlacementType | 放置类型 | ✅ 所有 | ❌ 只在 PlaceableItemData |
| placementPrefab | GameObject | 放置预制体 | ✅ 所有 | ❌ 只在 PlaceableItemData |
| buildingSize | Vector2Int | 建筑尺寸 | ✅ 所有 | ❌ 只在 PlaceableItemData |

### 🔴 装备配置（问题区域）
| 字段 | 类型 | 说明 | 当前显示 | 应该显示 |
|------|------|------|---------|---------|
| equipmentType | EquipmentType | 装备类型 | ✅ 所有 | ❌ 只在 ArmorData, AccessoryData |

### 🔴 消耗品配置（问题区域）
| 字段 | 类型 | 说明 | 当前显示 | 应该显示 |
|------|------|------|---------|---------|
| consumableType | ConsumableType | 消耗品类型 | ✅ 所有 | ❌ 只在 FoodData, PotionData |

---

## 二、各子类 SO 当前显示的字段

### ToolData（工具）
**应该显示**：
- 基础识别信息 ✅
- 视觉资源 ✅
- 经济属性 ✅
- 堆叠属性 ✅
- 显示尺寸配置 ✅
- 功能标记 ✅
- 工具专属属性 ✅

**不应该显示但当前显示**：
- 🔴 放置配置（isPlaceable, placementType, placementPrefab, buildingSize）
- 🔴 装备配置（equipmentType）
- 🔴 消耗品配置（consumableType）

---

### WeaponData（武器）
**应该显示**：
- 基础识别信息 ✅
- 视觉资源 ✅
- 经济属性 ✅
- 堆叠属性 ✅
- 显示尺寸配置 ✅
- 功能标记 ✅
- 武器专属属性 ✅

**不应该显示但当前显示**：
- 🔴 放置配置
- 🔴 装备配置
- 🔴 消耗品配置

---

### SeedData（种子）
**应该显示**：
- 基础识别信息 ✅
- 视觉资源 ✅
- 经济属性 ✅
- 堆叠属性 ✅
- 显示尺寸配置 ✅
- 功能标记 ✅
- 种子专属属性 ✅

**不应该显示但当前显示**：
- 🔴 放置配置
- 🔴 装备配置
- 🔴 消耗品配置

---

### CropData（作物）
**应该显示**：
- 基础识别信息 ✅
- 视觉资源 ✅
- 经济属性 ✅
- 堆叠属性 ✅
- 显示尺寸配置 ✅
- 功能标记 ✅
- 作物专属属性 ✅

**不应该显示但当前显示**：
- 🔴 放置配置
- 🔴 装备配置
- 🔴 消耗品配置

---

### FoodData（食物）
**应该显示**：
- 基础识别信息 ✅
- 视觉资源 ✅
- 经济属性 ✅
- 堆叠属性 ✅
- 显示尺寸配置 ✅
- 功能标记 ✅
- 食物专属属性 ✅
- 消耗品配置 ✅（食物是消耗品）

**不应该显示但当前显示**：
- 🔴 放置配置
- 🔴 装备配置

---

### MaterialData（材料）
**应该显示**：
- 基础识别信息 ✅
- 视觉资源 ✅
- 经济属性 ✅
- 堆叠属性 ✅
- 显示尺寸配置 ✅
- 功能标记 ✅
- 材料专属属性 ✅

**不应该显示但当前显示**：
- 🔴 放置配置
- 🔴 装备配置
- 🔴 消耗品配置

---

### SaplingData（树苗）- 可放置物品
**应该显示**：
- 基础识别信息 ✅
- 视觉资源 ✅
- 经济属性 ✅
- 堆叠属性 ✅
- 显示尺寸配置 ✅
- 功能标记 ✅
- 放置配置 ✅（树苗是可放置物品）
- 树苗专属属性 ✅

**不应该显示但当前显示**：
- 🔴 装备配置
- 🔴 消耗品配置

---

## 三、问题总结

### 问题 1：基类字段过于臃肿
ItemData 基类包含了所有子类可能需要的字段，导致：
- 每个子类都显示不相关的配置区域
- Inspector 界面混乱
- 开发者容易误配置

### 问题 2：字段归属不清晰
- 放置配置应该只在 PlaceableItemData 及其子类显示
- 装备配置应该只在 ArmorData, AccessoryData 显示
- 消耗品配置应该只在 FoodData, PotionData 显示

### 问题 3：缺少中间层抽象
当前继承结构：
```
ItemData (基类)
├── ToolData
├── WeaponData
├── SeedData
├── CropData
├── FoodData
├── MaterialData
└── SaplingData
```

应该的继承结构：
```
ItemData (基类) - 只包含通用字段
├── ToolData
├── WeaponData
├── SeedData
├── CropData
├── MaterialData
├── ConsumableItemData (中间层) - 包含消耗品配置
│   ├── FoodData
│   └── PotionData
├── EquippableItemData (中间层) - 包含装备配置
│   ├── ArmorData
│   └── AccessoryData
└── PlaceableItemData (中间层) - 包含放置配置
    ├── SaplingData
    ├── StorageData (箱子)
    └── WorkstationData (工作台)
```

---

## 四、修复方案

### 方案 A：使用 HideInInspector（临时方案）

在子类中隐藏不需要的字段：

```csharp
// ToolData.cs
public class ToolData : ItemData
{
    // 隐藏不需要的基类字段
    [HideInInspector] public new bool isPlaceable;
    [HideInInspector] public new PlacementType placementType;
    [HideInInspector] public new GameObject placementPrefab;
    [HideInInspector] public new Vector2Int buildingSize;
    [HideInInspector] public new EquipmentType equipmentType;
    [HideInInspector] public new ConsumableType consumableType;
    
    // ... 工具专属属性
}
```

**优点**：
- 快速实现
- 不影响现有数据

**缺点**：
- 字段仍然存在，只是隐藏
- 需要在每个子类中重复声明
- 不是根本解决方案

---

### 方案 B：重构继承结构（推荐方案）

1. 从 ItemData 中移除特定类型字段
2. 创建中间层抽象类
3. 在需要的子类中添加字段

**步骤**：

1. **创建 ConsumableItemData**：
```csharp
public abstract class ConsumableItemData : ItemData
{
    [Header("=== 消耗品配置 ===")]
    public ConsumableType consumableType = ConsumableType.None;
}
```

2. **创建 PlaceableItemData**：
```csharp
public abstract class PlaceableItemData : ItemData
{
    [Header("=== 放置配置 ===")]
    public bool isPlaceable = true;
    public PlacementType placementType = PlacementType.None;
    public GameObject placementPrefab;
    public Vector2Int buildingSize = Vector2Int.one;
}
```

3. **修改 FoodData**：
```csharp
public class FoodData : ConsumableItemData
{
    // 食物专属属性
}
```

4. **修改 SaplingData**：
```csharp
public class SaplingData : PlaceableItemData
{
    // 树苗专属属性
}
```

5. **从 ItemData 中移除字段**：
```csharp
public class ItemData : ScriptableObject
{
    // 移除：isPlaceable, placementType, placementPrefab, buildingSize
    // 移除：equipmentType
    // 移除：consumableType
}
```

**优点**：
- 根本解决问题
- 继承结构清晰
- 符合面向对象设计原则

**缺点**：
- 需要修改大量代码
- 可能影响现有 SO 资产
- 需要更新所有使用这些字段的代码

---

### 方案 C：使用 CustomEditor（折中方案）

为每个子类创建自定义 Editor，控制字段显示：

```csharp
[CustomEditor(typeof(ToolData))]
public class ToolDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 只绘制需要的字段
        DrawPropertiesExcluding(serializedObject, 
            "isPlaceable", "placementType", "placementPrefab", "buildingSize",
            "equipmentType", "consumableType");
        
        serializedObject.ApplyModifiedProperties();
    }
}
```

**优点**：
- 不修改数据结构
- 不影响现有 SO 资产
- 可以逐步实施

**缺点**：
- 需要为每个子类创建 Editor
- 字段仍然存在，只是不显示
- 维护成本较高

---

## 五、推荐实施顺序

1. **立即**：使用方案 A（HideInInspector）临时解决显示问题
2. **短期**：使用方案 C（CustomEditor）改善 Inspector 体验
3. **长期**：使用方案 B（重构继承结构）根本解决问题

---

## 六、与 SO 设计系统工作区的关系

此分析与 `.kiro/specs/SO设计系统/0_字段优化重构/` 工作区的需求文档一致。

建议：
1. 在 SO 设计系统工作区完成完整的重构
2. 农田系统工作区只做临时修复（方案 A）
3. 避免重复工作
