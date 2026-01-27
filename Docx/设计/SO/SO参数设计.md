# ScriptableObject 参数设计规范

**版本**: v1.1  
**日期**: 2025-12-17  
**状态**: ✅ 已完成

**更新记录**:
- v1.1: WeaponData 添加动画配置字段（animatorController, animationFrameCount, animActionType），与 ToolData 保持一致

---

## 概述

本项目使用 ScriptableObject (SO) 作为物品数据的存储方式。所有物品数据类继承自 `ItemData` 基类，位于 `Assets/Scripts/Data/Items/` 目录下。

---

## ID 分配规范

```
0XXX: 工具和武器
    00XX: 农业工具（锄头、水壶、镰刀、钓鱼竿）
    01XX: 采集工具（镐子、斧头）
    02XX: 武器（剑、弓、法杖）

1XXX: 种植类
    10XX: 种子
    11XX: 作物
    12XX: 树苗（Sapling）
    13XX: 建筑材料
    14XX: 钥匙和锁
        1420-1499: 钥匙/锁（KeyLockData）

2XXX: 动物产品
    20XX: 畜牧产品
    21XX: 肉类
    22XX: 水产

3XXX: 矿物和材料
    30XX: 矿石（未加工）
    31XX: 锭（加工后）
    32XX: 自然材料（木、石）
    33XX: 怪物掉落

4XXX: 消耗品
    40XX: 药水

5XXX: 食品
    50XX: 简单料理
    51XX: 高级料理

6XXX: 家具
7XXX: 特殊物品
```
    51XX: 高级料理

6XXX: 家具
7XXX: 特殊物品
```

---

## 品质系统设计

### ⚠️ 重要：品质 ≠ 等级

本项目中：
- **品质 (Quality)** 是运行时属性，通过 UI 星星显示，影响售价
- **工具/武器没有"等级"属性**，不同材质的工具是独立的 ItemID

### 品质枚举 (ItemQuality)

| 品质 | 值 | 星星颜色 | 价格倍率 | 说明 |
|------|-----|---------|---------|------|
| Normal | 0 | 无星星 | ×1.0 | 普通/正常，大部分物品默认品质 |
| Rare | 1 | 蓝色 | ×1.25 | 稀有 |
| Epic | 2 | 紫色 | ×2.0 | 罕见 |
| Legendary | 3 | 金色 | ×3.25 | 猎奇 |

> ⚠️ **价格计算后向上取整** (`Mathf.CeilToInt`)

### 品质的应用场景

- **作物收获**：随机判定品质，影响售价
- **动画系统**：工具动画通过 `{ActionType}_{Direction}_Clip_{itemId}_{quality}` 命名
- **UI 显示**：Normal 无星星，其他品质在物品槽左下角显示对应颜色星星
- **材料/建材**：通常为 Normal，表示"正常"而非"低品质"

---

## SO 类型详解

### 1. ItemData（基类）

所有物品的共同属性。

| 字段 | 类型 | 说明 |
|------|------|------|
| itemID | int | 唯一ID（0-9999） |
| itemName | string | 物品名称 |
| description | string | 物品描述 |
| category | ItemCategory | 物品大类 |
| icon | Sprite | 基础图标 |
| bagSprite | Sprite | 背包显示图标（可选，为空时使用 icon） |
| worldPrefab | GameObject | 世界预制体 |
| buyPrice | int | 购买价格（0=不可购买） |
| sellPrice | int | 出售价格 |
| maxStackSize | int | 最大堆叠数（1=不可堆叠） |
| canBeDiscarded | bool | 是否可丢弃 |
| isQuestItem | bool | 是否是任务物品 |

---

### 2. ToolData（工具）

继承 ItemData，用于锄头、斧头、镐子等。

| 字段 | 类型 | 说明 |
|------|------|------|
| toolType | ToolType | 工具类型（Hoe/Axe/Pickaxe 等） |
| energyCost | int | 使用消耗精力（1-20） |
| effectRadius | int | 作用范围（1=单格） |
| efficiencyMultiplier | float | 效率加成（1.0=基础） |
| hasDurability | bool | 是否有耐久度 |
| maxDurability | int | 最大耐久度 |
| canDealDamage | bool | 是否可造成伤害 |
| damageAmount | int | 伤害值 |
| animatorController | RuntimeAnimatorController | 工具动画控制器 |
| animationFrameCount | int | 动画帧数（用于帧同步） |
| animActionType | AnimActionType | 动画动作类型 |
| useSound | AudioClip | 使用音效 |

**⚠️ 注意**：
- 工具**没有等级属性**
- 不同材质的工具（铜斧、铁斧、金斧）是**独立的 ItemID**
- 品质通过后缀命名区分（如动画 `Slice_Down_Clip_0_0`）

---

### 3. WeaponData（武器）

继承 ItemData，用于剑、弓、法杖等战斗装备。

| 字段 | 类型 | 说明 |
|------|------|------|
| weaponType | WeaponType | 武器类型（Sword/Bow/Staff） |
| attackPower | int | 攻击力（1-200） |
| attackSpeed | float | 攻击速度（0.3-3.0，越小越快） |
| criticalChance | float | 暴击率（0-100%） |
| criticalDamageMultiplier | float | 暴击伤害倍率（1.5-3.0） |
| attackRange | float | 攻击范围 |
| knockbackForce | float | 击退力度 |
| energyCostPerAttack | int | 每次攻击消耗精力 |
| hasDurability | bool | 是否有耐久度 |
| maxDurability | int | 最大耐久度 |
| animatorController | RuntimeAnimatorController | 武器动画控制器 |
| animationFrameCount | int | 动画帧数（用于帧同步） |
| animActionType | AnimActionType | 动画动作类型（默认 Pierce） |
| attackSound | AudioClip | 攻击音效 |
| hitSound | AudioClip | 击中音效 |

**⚠️ 注意**：
- 武器**没有等级属性**
- 不同材质的武器是**独立的 ItemID**
- 武器动画配置与工具一致，支持帧同步系统
- 品质通过后缀命名区分（如动画 `Pierce_Down_Clip_200_0`）

---

### 4. SeedData（种子）

继承 ItemData，用于可种植的种子。

| 字段 | 类型 | 说明 |
|------|------|------|
| growthDays | int | 生长所需天数（1-28） |
| season | Season | 适合种植的季节 |
| harvestCropID | int | 收获的作物ID（11XX） |
| harvestAmountRange | Vector2Int | 收获数量范围 |
| isReHarvestable | bool | 是否可重复收获 |
| reHarvestDays | int | 重复收获间隔天数 |
| maxHarvestCount | int | 最大收获次数（0=无限） |
| growthStageSprites | Sprite[] | 生长阶段图 |
| needsTrellis | bool | 是否需要支架 |
| needsWatering | bool | 是否需要浇水 |
| plantingExp | int | 种植经验值 |
| harvestingExp | int | 收获经验值 |

**ID 范围**：10XX

---

### 5. CropData（作物）

继承 ItemData，用于收获的农作物。

| 字段 | 类型 | 说明 |
|------|------|------|
| seedID | int | 对应的种子ID（10XX） |
| harvestExp | int | 收获经验值 |
| canBeCrafted | bool | 是否可制作成食物 |
| usedInRecipes | string | 用于哪些配方（显示用） |

**ID 范围**：11XX

**品质说明**：作物收获时随机判定品质，品质影响售价但不影响外观。

---

### 6. FoodData（食物）

继承 ItemData，用于可食用的料理。

| 字段 | 类型 | 说明 |
|------|------|------|
| energyRestore | int | 恢复精力值 |
| healthRestore | int | 恢复HP值 |
| consumeTime | float | 食用动画时长（秒） |
| buffType | BuffType | Buff类型 |
| buffValue | float | Buff数值 |
| buffDuration | float | Buff持续时间（秒，0=永久） |
| recipeID | int | 对应的配方ID |

**ID 范围**：50XX（简单料理）、51XX（高级料理）

**堆叠建议**：最多 20 个

---

### 7. MaterialData（材料）

继承 ItemData，用于矿石、木材、怪物掉落等。

| 字段 | 类型 | 说明 |
|------|------|------|
| materialSubType | MaterialSubType | 材料子类型 |
| sourceDescription | string | 来源描述 |
| canBeSmelt | bool | 是否可熔炼（仅矿石） |
| smeltResultID | int | 熔炼产物ID（31XX） |
| smeltTime | int | 熔炼时间（游戏小时） |
| craftingUse | string | 用途说明 |

**ID 范围**：
- 30XX：矿石
- 31XX：锭
- 32XX：自然材料
- 33XX：怪物掉落

---

### 8. PotionData（药水）

继承 ItemData，用于HP药水、精力药水等。

| 字段 | 类型 | 说明 |
|------|------|------|
| healthRestore | int | 恢复HP值 |
| energyRestore | int | 恢复精力值 |
| useTime | float | 使用时间（秒） |
| buffType | BuffType | Buff类型 |
| buffValue | float | Buff数值 |
| buffDuration | float | Buff持续时间（秒） |
| recipeID | int | 制作配方ID |
| useEffectPrefab | GameObject | 使用时的粒子效果 |
| useSound | AudioClip | 使用音效 |

**ID 范围**：40XX

---

## 枚举定义

### ToolType（工具类型）

```csharp
public enum ToolType
{
    None,
    Hoe,            // 锄头
    WateringCan,    // 水壶
    Sickle,         // 镰刀
    FishingRod,     // 钓鱼竿
    Pickaxe,        // 镐子
    Axe,            // 斧头
    Weapon          // 武器（在WeaponType中详细定义）
}
```

### WeaponType（武器类型）

```csharp
public enum WeaponType
{
    Sword,          // 剑
    Bow,            // 弓
    Staff,          // 法杖
    Spear,          // 矛（预留）
    Hammer          // 锤（预留）
}
```

### AnimActionType（动画动作类型）

```csharp
public enum AnimActionType
{
    Slice = 6,      // 挥砍（斧头、镰刀）
    Pierce = 7,     // 刺出（长剑）
    Crush = 8,      // 挖掘（镐子、锄头）
    Fish = 9,       // 钓鱼（鱼竿）
    Watering = 10   // 浇水（洒水壶）
}
```

### 工具/武器类型到动画映射

| 类型 | AnimActionType | 说明 |
|---------|---------------|------|
| Axe | Slice (6) | 挥砍 |
| Sickle | Slice (6) | 挥砍 |
| Pickaxe | Crush (8) | 挖掘 |
| Hoe | Crush (8) | 挖掘 |
| FishingRod | Fish (9) | 钓鱼 |
| WateringCan | Watering (10) | 浇水 |
| Sword | Pierce (7) | 刺出 |
| Bow | Pierce (7) | 刺出（预留） |
| Staff | Pierce (7) | 刺出（预留） |

---

## 动画命名规范

### 工具/武器动画

```
{ActionType}_{Direction}_Clip_{itemId}_{quality}

示例：
- Slice_Down_Clip_0_0      （ID=0的斧头，品质0，向下）
- Crush_Side_Clip_10_2     （ID=10的镐子，品质2，侧面）
- Pierce_Down_Clip_200_0   （ID=200的剑，品质0，向下）
```

### 动画控制器

```
{ActionType}_Controller_{itemId}_{itemName}.controller

示例：
- Slice_Controller_0_Axe.controller
- Crush_Controller_10_Pickaxe.controller
- Pierce_Controller_200_Sword.controller
```

---

## 文件命名规范

### SO 资产文件

```
{TypePrefix}_{itemID}_{itemName}.asset

示例：
- Tool_0_Axe_0.asset
- Tool_6_Pickaxe_0.asset
- Weapon_200_Sword_0.asset
- Seed_1000_TomatoSeed.asset
- Crop_1100_Tomato.asset
- Food_5000_FriedEgg.asset
- Material_3000_CopperOre.asset
- Potion_4000_HealthPotion.asset
```

> ⚠️ **注意**：ID 不需要补零，直接使用数字即可（如 `Tool_0_Axe_0` 而非 `Tool_0000_Axe_0`）

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Data/Items/ItemData.cs` | 基类 |
| `Assets/Scripts/Data/Items/ToolData.cs` | 工具数据 |
| `Assets/Scripts/Data/Items/WeaponData.cs` | 武器数据 |
| `Assets/Scripts/Data/Items/SeedData.cs` | 种子数据 |
| `Assets/Scripts/Data/Items/CropData.cs` | 作物数据 |
| `Assets/Scripts/Data/Items/FoodData.cs` | 食物数据 |
| `Assets/Scripts/Data/Items/MaterialData.cs` | 材料数据 |
| `Assets/Scripts/Data/Items/PotionData.cs` | 药水数据 |
| `Assets/Scripts/Data/Enums/ItemEnums.cs` | 枚举定义 |

---

*文档结束*
