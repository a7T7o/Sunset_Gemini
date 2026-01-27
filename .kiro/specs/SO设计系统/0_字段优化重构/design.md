# SO 字段优化重构 - 设计文档

## 概述

本设计旨在通过移除基类中的特定类型字段（equipmentType 和 consumableType），并将它们移动到需要的子类中，来优化 ScriptableObject 系统的字段结构，提升代码的清晰度和可维护性。

---

## 架构设计

### 当前架构（问题）

```
ItemData（基类）
├── equipmentType (EquipmentType)     ← 问题：所有子类都继承
├── consumableType (ConsumableType)   ← 问题：所有子类都继承
├── ... 其他通用字段
│
├── ToolData
│   └── 不需要 equipmentType 和 consumableType
│
├── WeaponData
│   └── 不需要 equipmentType 和 consumableType
│
├── SeedData
│   └── 不需要 equipmentType 和 consumableType
│
├── CropData
│   └── 不需要 equipmentType 和 consumableType
│
├── FoodData
│   └── 需要 consumableType，不需要 equipmentType
│
├── MaterialData
│   └── 不需要 equipmentType 和 consumableType
│
├── PotionData
│   └── 需要 consumableType，不需要 equipmentType
│
└── KeyLockData
    └── 不需要 equipmentType 和 consumableType
```

**问题统计**：
- equipmentType：8 个类型中，0 个需要（0%）
- consumableType：8 个类型中，2 个需要（25%）

### 优化后架构（解决方案）

```
ItemData（基类）
├── ... 通用字段（移除 equipmentType 和 consumableType）
│
├── ToolData
│   └── 无额外装备/消耗品字段
│
├── WeaponData
│   └── 无额外装备/消耗品字段
│
├── SeedData
│   └── 无额外装备/消耗品字段
│
├── CropData
│   └── 无额外装备/消耗品字段
│
├── FoodData
│   └── + consumableType (ConsumableType)  ← 新增
│
├── MaterialData
│   └── 无额外装备/消耗品字段
│
├── PotionData
│   └── + consumableType (ConsumableType)  ← 新增
│
└── KeyLockData
    └── 无额外装备/消耗品字段
```

---

## 组件设计

### 1. ItemData（基类）

**修改内容**：
- 移除 `equipmentType` 字段
- 移除 `consumableType` 字段
- 标注 `bagSprite` 为 `[Obsolete]`

**保留字段**：
```csharp
// 基础识别
public int itemID;
public string itemName;
public string description;
public ItemCategory category;

// 视觉资源
public Sprite icon;
[Obsolete("bagSprite 已废弃，背包图标统一使用 icon + 45° 旋转")]
public Sprite bagSprite;
public bool rotateBagIcon = true;
public GameObject worldPrefab;

// 经济属性
public int buyPrice;
public int sellPrice;

// 堆叠属性
public int maxStackSize = 1;

// 显示尺寸配置
public bool useCustomBagDisplaySize = false;
public int bagDisplayPixelSize = 32;
public Vector2 bagDisplayOffset = Vector2.zero;
public bool useCustomDisplaySize = false;
public int displayPixelSize = 32;
public Vector2 worldDisplayOffset = Vector2.zero;

// 功能标记
public bool canBeDiscarded = true;
public bool isQuestItem = false;

// 放置配置
public bool isPlaceable = false;
public PlacementType placementType = PlacementType.Ground;
public GameObject placementPrefab;
public Vector2Int buildingSize = Vector2Int.one;
```

**新增方法**：
```csharp
/// <summary>
/// 检查物品是否为消耗品（需要子类重写）
/// </summary>
public virtual bool IsConsumable() => false;

/// <summary>
/// 获取消耗品类型（需要子类重写）
/// </summary>
public virtual ConsumableType GetConsumableType() => ConsumableType.None;
```

---

### 2. FoodData（食物）

**新增字段**：
```csharp
[Header("=== 消耗品配置 ===")]
[Tooltip("消耗品类型")]
public ConsumableType consumableType = ConsumableType.Food;
```

**重写方法**：
```csharp
public override bool IsConsumable() => true;
public override ConsumableType GetConsumableType() => consumableType;
```

**完整字段列表**：
```csharp
// 继承自 ItemData 的所有通用字段

[Header("=== 消耗品配置 ===")]
public ConsumableType consumableType = ConsumableType.Food;

[Header("=== 食物属性 ===")]
public int energyRestore;
public int healthRestore;
public float consumeTime = 1.5f;

[Header("=== Buff 效果 ===")]
public BuffType buffType = BuffType.None;
public float buffValue;
public float buffDuration;

[Header("=== 制作配置 ===")]
public int recipeID;
```

---

### 3. PotionData（药水）

**新增字段**：
```csharp
[Header("=== 消耗品配置 ===")]
[Tooltip("消耗品类型")]
public ConsumableType consumableType = ConsumableType.Potion;
```

**重写方法**：
```csharp
public override bool IsConsumable() => true;
public override ConsumableType GetConsumableType() => consumableType;
```

**完整字段列表**：
```csharp
// 继承自 ItemData 的所有通用字段

[Header("=== 消耗品配置 ===")]
public ConsumableType consumableType = ConsumableType.Potion;

[Header("=== 药水属性 ===")]
public int healthRestore;
public int energyRestore;
public float useTime = 1.0f;

[Header("=== Buff 效果 ===")]
public BuffType buffType = BuffType.None;
public float buffValue;
public float buffDuration;

[Header("=== 制作配置 ===")]
public int recipeID;

[Header("=== 特效与音效 ===")]
public GameObject useEffectPrefab;
public AudioClip useSound;
```

---

### 4. 其他 SO 类型

**ToolData、WeaponData、SeedData、CropData、MaterialData、KeyLockData**：
- 不需要任何修改
- 自动失去 equipmentType 和 consumableType 字段（因为基类移除了）

---

## 数据模型

### ConsumableType 枚举

```csharp
public enum ConsumableType
{
    None = 0,       // 非消耗品
    Food = 1,       // 食物
    Potion = 2,     // 药水
    Buff = 3        // 纯 Buff 物品（预留）
}
```

### EquipmentType 枚举（保留但不使用）

```csharp
public enum EquipmentType
{
    None = 0,       // 非装备
    Helmet = 1,     // 头盔
    Armor = 2,      // 盔甲
    Pants = 3,      // 裤子
    Shoes = 4,      // 鞋子
    Ring = 5,       // 戒指
    Accessory = 6   // 饰品（预留）
}
```

**说明**：EquipmentType 枚举保留是为了未来可能的装备系统扩展，但当前不在任何 SO 类中使用。

---

## 接口设计

### ItemData 虚方法

```csharp
/// <summary>
/// 检查物品是否为消耗品
/// </summary>
/// <returns>true 如果是消耗品</returns>
public virtual bool IsConsumable() => false;

/// <summary>
/// 获取消耗品类型
/// </summary>
/// <returns>消耗品类型枚举</returns>
public virtual ConsumableType GetConsumableType() => ConsumableType.None;
```

**使用示例**：
```csharp
// 检查物品是否可以右键使用
if (itemData.IsConsumable())
{
    ConsumableType type = itemData.GetConsumableType();
    switch (type)
    {
        case ConsumableType.Food:
            // 执行食用逻辑
            break;
        case ConsumableType.Potion:
            // 执行使用药水逻辑
            break;
    }
}
```

---

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### 属性 1：字段可见性正确性

*对于任意* SO 类型，Inspector 中显示的字段应该只包含该类型实际需要的字段，不应显示无关字段

**验证：需求 1.1, 1.2, 1.3, 1.4, 1.5**

**测试方法**：
- 打开各种类型的 SO
- 检查 Inspector 中的字段列表
- 确认只显示相关字段

### 属性 2：消耗品类型一致性

*对于任意* FoodData 或 PotionData，调用 `IsConsumable()` 应返回 true，且 `GetConsumableType()` 应返回正确的类型

**验证：需求 2.1, 2.2**

**测试方法**：
- 创建 FoodData 和 PotionData 实例
- 调用 `IsConsumable()` 和 `GetConsumableType()`
- 验证返回值正确

### 属性 3：非消耗品类型一致性

*对于任意* 非 FoodData/PotionData 的 SO，调用 `IsConsumable()` 应返回 false

**验证：需求 2.3, 2.4**

**测试方法**：
- 创建 SeedData、MaterialData 等实例
- 调用 `IsConsumable()`
- 验证返回 false

### 属性 4：批量生成正确性

*对于任意* SO 类型，使用批量生成工具创建的 SO 应该只包含该类型需要的字段，且字段值正确

**验证：需求 3.1, 3.2, 3.3, 3.4**

**测试方法**：
- 使用批量生成工具创建各种类型的 SO
- 检查生成的 SO 的字段
- 验证字段值正确

### 属性 5：批量修改安全性

*对于任意* 选中的 SO 集合，批量修改工具应该只显示所有 SO 共有的可修改字段

**验证：需求 4.1, 4.2, 4.3**

**测试方法**：
- 选中不同类型的 SO
- 打开批量修改工具
- 验证只显示共有字段

### 属性 6：功能兼容性

*对于任意* 现有的游戏功能（右键使用、快速装备等），在字段重构后应继续正常工作

**验证：需求 5.1, 5.2, 5.3, 5.4, 5.5**

**测试方法**：
- 执行各种游戏操作
- 验证功能正常
- 检查控制台无错误

### 属性 7：废弃字段标注

*对于任意* 使用 bagSprite 字段的代码，编译器应显示警告信息

**验证：需求 6.1, 6.2**

**测试方法**：
- 在代码中使用 bagSprite
- 编译项目
- 验证显示警告

### 属性 8：字段清理完整性

*对于任意* 不需要 equipmentType 或 consumableType 的 SO，运行清理工具后这些字段应被重置为默认值

**验证：需求 8.2, 8.3**

**测试方法**：
- 运行字段清理工具
- 检查所有 SO
- 验证无效字段被清理

---

## 错误处理

### 1. 空引用处理

**场景**：代码尝试访问不存在的 consumableType 字段

**处理**：
```csharp
// ❌ 错误做法：直接访问字段
ConsumableType type = itemData.consumableType; // 编译错误

// ✅ 正确做法：使用虚方法
if (itemData.IsConsumable())
{
    ConsumableType type = itemData.GetConsumableType();
    // 使用 type
}
```

### 2. 类型转换处理

**场景**：需要访问特定子类的字段

**处理**：
```csharp
// ✅ 正确做法：类型检查和转换
if (itemData is FoodData foodData)
{
    ConsumableType type = foodData.consumableType;
    // 使用 type
}
```

### 3. 批量工具错误处理

**场景**：批量修改时遇到不兼容的 SO

**处理**：
```csharp
// 检查 SO 类型
if (so is FoodData || so is PotionData)
{
    // 可以修改 consumableType
}
else
{
    Debug.LogWarning($"[BatchModifier] {so.name} 不支持 consumableType 字段");
    continue;
}
```

---

## 测试策略

### 单元测试

**测试 ItemData 虚方法**：
```csharp
[Test]
public void ItemData_IsConsumable_ReturnsFalse()
{
    var itemData = ScriptableObject.CreateInstance<ItemData>();
    Assert.IsFalse(itemData.IsConsumable());
}

[Test]
public void FoodData_IsConsumable_ReturnsTrue()
{
    var foodData = ScriptableObject.CreateInstance<FoodData>();
    Assert.IsTrue(foodData.IsConsumable());
}

[Test]
public void FoodData_GetConsumableType_ReturnsFood()
{
    var foodData = ScriptableObject.CreateInstance<FoodData>();
    foodData.consumableType = ConsumableType.Food;
    Assert.AreEqual(ConsumableType.Food, foodData.GetConsumableType());
}
```

### 集成测试

**测试批量生成工具**：
1. 创建测试数据
2. 运行批量生成工具
3. 验证生成的 SO 字段正确
4. 验证数据库同步成功

**测试批量修改工具**：
1. 创建测试 SO
2. 选中多个 SO
3. 运行批量修改工具
4. 验证修改成功
5. 验证数据库同步成功

**测试字段清理工具**：
1. 创建包含无效字段的测试 SO
2. 运行清理工具
3. 验证无效字段被清理
4. 验证有效字段保持不变

### 手动测试

**测试 Inspector 显示**：
1. 打开各种类型的 SO
2. 检查 Inspector 中的字段
3. 验证只显示相关字段

**测试游戏功能**：
1. 启动游戏
2. 测试右键使用食物
3. 测试右键使用药水
4. 测试右键使用种子
5. 验证所有功能正常

---

## 性能考虑

### 虚方法调用开销

**影响**：虚方法调用比直接字段访问略慢

**优化**：
- 虚方法调用开销极小（纳秒级）
- 只在需要时调用（如右键点击）
- 不在 Update 循环中调用

### 批量工具性能

**目标**：
- 批量修改 100 个 SO：< 5 秒
- 字段清理全部 SO：< 10 秒

**优化**：
- 使用 `AssetDatabase.StartAssetEditing()` 和 `StopAssetEditing()`
- 批量保存而非逐个保存
- 使用进度条显示进度

---

## 安全性考虑

### 数据完整性

**风险**：字段移除可能导致现有 SO 数据丢失

**缓解**：
- Unity 会自动忽略不存在的字段
- 不会导致 SO 损坏
- 可以通过版本控制回滚

### 代码兼容性

**风险**：现有代码可能直接访问 equipmentType 或 consumableType

**缓解**：
- 提供虚方法作为替代
- 编译时会报错，强制修复
- 提供清晰的迁移指南

---

## 部署策略

### 阶段 1：代码修改

1. 修改 ItemData.cs（移除字段，添加虚方法）
2. 修改 FoodData.cs（添加 consumableType）
3. 修改 PotionData.cs（添加 consumableType）
4. 修改所有使用这些字段的代码

### 阶段 2：工具更新

1. 更新 Tool_BatchItemSOGenerator.cs
2. 更新 Tool_BatchItemSOModifier.cs
3. 创建字段清理工具

### 阶段 3：数据迁移

1. 运行字段清理工具
2. 验证所有 SO 正常
3. 测试游戏功能

### 阶段 4：文档更新

1. 更新 SO 参数设计文档到 v2.0
2. 更新 so-design.md steering 规则
3. 更新工作区 memory.md

---

## 相关文件

### 核心数据类

| 文件 | 修改内容 |
|------|---------|
| `Assets/YYY_Scripts/Data/Items/ItemData.cs` | 移除 equipmentType 和 consumableType，添加虚方法 |
| `Assets/YYY_Scripts/Data/Items/FoodData.cs` | 添加 consumableType 字段 |
| `Assets/YYY_Scripts/Data/Items/PotionData.cs` | 添加 consumableType 字段 |

### 编辑器工具

| 文件 | 修改内容 |
|------|---------|
| `Assets/Editor/Tool_BatchItemSOGenerator.cs` | 更新生成逻辑 |
| `Assets/Editor/Tool_BatchItemSOModifier.cs` | 更新修改逻辑 |
| `Assets/Editor/Tool_SOFieldCleaner.cs` | 新建清理工具 |

### 游戏逻辑

| 文件 | 修改内容 |
|------|---------|
| `Assets/YYY_Scripts/Service/Inventory/InventoryService.cs` | 更新右键使用逻辑 |
| `Assets/YYY_Scripts/UI/Inventory/ItemSlotUI.cs` | 更新右键点击处理 |

### 文档

| 文件 | 修改内容 |
|------|---------|
| `Docx/设计/SO/SO参数设计.md` | 更新到 v2.0 |
| `.kiro/steering/so-design.md` | 更新字段说明 |
| `.kiro/specs/SO设计系统/memory.md` | 记录本次重构 |

---

*设计文档结束*
