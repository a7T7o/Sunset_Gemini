# 背包图标旋转 - 开发记忆

## 模块概述
背包/工具栏图标显示系统，支持可配置的旋转、自定义尺寸和位置偏移。

## 当前状态
- **完成度**: 100%
- **最后更新**: 2025-12-31
- **状态**: 已完成

## 会话记录

### 会话 1 - 2025-12-31

**用户需求**:
> 在SO部分的视觉资源部分增加一个选项在bag sprite边上，若勾选则为icon的旋转，不勾选则不旋转，然后再来到显示尺寸配置部分，也要有一个选项给bag sprite，自定义显示尺寸大小，并与下面的display 也就是world prefab的尺寸部分分离开来

**完成任务**:
1. 在 ItemData 中添加 `rotateBagIcon` 字段（默认 true）
2. 在 ItemData 中添加 `useCustomBagDisplaySize` 和 `bagDisplayPixelSize` 字段
3. 修改 UIItemIconScaler.SetIconWithAutoScale() 支持从 ItemData 读取配置
4. 更新 InventorySlotUI、ToolbarSlotUI、EquipmentSlotUI 传入 ItemData

**修改文件**:
- `Assets/YYY_Scripts/Data/Items/ItemData.cs` - 添加 rotateBagIcon、useCustomBagDisplaySize、bagDisplayPixelSize 字段和 GetBagDisplayPixelSize() 方法
- `Assets/YYY_Scripts/UI/Utility/UIItemIconScaler.cs` - 修改 SetIconWithAutoScale() 支持可配置旋转和尺寸
- `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs` - 传入 ItemData 参数
- `Assets/YYY_Scripts/UI/Toolbar/ToolbarSlotUI.cs` - 传入 ItemData 参数
- `Assets/YYY_Scripts/UI/Inventory/EquipmentSlotUI.cs` - 传入 ItemData 参数

---

### 会话 2 - 2025-12-31

**用户需求**:
> 在显示尺寸配置加个小分类，一个是背包显示，一个是世界显示，背包显示里面再加一个上下左右的偏移量设置，世界显示也要一个

**完成任务**:
1. 重构显示尺寸配置区域，分为"背包显示"和"世界显示"两个子分类
2. 添加 `bagDisplayOffset` (Vector2) 背包图标位置偏移
3. 添加 `worldDisplayOffset` (Vector2) 世界物品位置偏移
4. 更新 UIItemIconScaler 应用背包偏移量

**修改文件**:
- `Assets/YYY_Scripts/Data/Items/ItemData.cs` - 添加分类 Header 和偏移量字段
- `Assets/YYY_Scripts/UI/Utility/UIItemIconScaler.cs` - 应用 bagDisplayOffset

**解决方案**:
显示尺寸配置区域结构：
```
=== 显示尺寸配置 ===
--- 背包显示 ---
  - useCustomBagDisplaySize (bool)
  - bagDisplayPixelSize (int, 8-128)
  - bagDisplayOffset (Vector2, 像素)
--- 世界显示 ---
  - useCustomDisplaySize (bool)
  - displayPixelSize (int, 8-128)
  - worldDisplayOffset (Vector2, 单位)
```

**遗留问题**:
- worldDisplayOffset 需要在 World Prefab 生成工具和运行时掉落物中应用（当前仅定义字段）

---

### 会话 3 - 2025-12-31

**用户需求**:
> Amount 的 RectTransform 参数要按照截图设置：左21.2356，顶部41.568，右3.8808，底部0

**完成任务**:
1. 更新 InventorySlotUI 的 Amount RectTransform 参数
2. 更新 ToolbarSlotUI 添加自动创建 Amount 逻辑（与 InventorySlotUI 保持一致）
3. 更新 steering/ui.md 中的标准配置

**修改文件**:
- `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs` - 更新 Amount RectTransform 参数
- `Assets/YYY_Scripts/UI/Toolbar/ToolbarSlotUI.cs` - 添加自动创建 Amount 逻辑
- `.kiro/steering/ui.md` - 更新物品堆叠数量显示标准配置

**解决方案**:
Amount RectTransform 参数：
```csharp
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.pivot = new Vector2(0.5f, 0.5f);
rt.offsetMin = new Vector2(21.2356f, 0f);      // left, bottom
rt.offsetMax = new Vector2(-3.8808f, -41.568f); // -right, -top
```

**遗留问题**:
- 无

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| rotateBagIcon 默认 true | 保持与现有行为一致，大多数物品需要旋转 | 2025-12-31 |
| bagDisplayPixelSize 默认 52 | 与原有 DEFAULT_AVAILABLE_AREA 一致 | 2025-12-31 |
| 分离背包和世界物品尺寸配置 | 两者用途不同，需要独立控制 | 2025-12-31 |
| 使用 Header 分类 | Inspector 中更清晰的视觉分组 | 2025-12-31 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Data/Items/ItemData.cs` | 物品数据基类，包含旋转、尺寸和偏移配置 |
| `Assets/YYY_Scripts/UI/Utility/UIItemIconScaler.cs` | 图标缩放适配工具 |
| `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs` | 背包槽位 UI |
| `Assets/YYY_Scripts/UI/Toolbar/ToolbarSlotUI.cs` | 工具栏槽位 UI |
| `Assets/YYY_Scripts/UI/Inventory/EquipmentSlotUI.cs` | 装备槽位 UI |

## Inspector 字段说明

### 视觉资源部分
| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| icon | Sprite | - | 基础图标 |
| bagSprite | Sprite | - | 背包显示图标（为空时使用 icon） |
| rotateBagIcon | bool | true | 背包图标是否旋转45度 |
| worldPrefab | GameObject | - | 世界预制体 |

### 显示尺寸配置 - 背包显示
| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| useCustomBagDisplaySize | bool | false | 是否启用自定义背包图标尺寸 |
| bagDisplayPixelSize | int | 52 | 背包图标像素尺寸（8-128） |
| bagDisplayOffset | Vector2 | (0,0) | 背包图标位置偏移（像素） |

### 显示尺寸配置 - 世界显示
| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| useCustomDisplaySize | bool | false | 是否启用自定义世界物品尺寸 |
| displayPixelSize | int | 32 | 世界物品像素尺寸（8-128） |
| worldDisplayOffset | Vector2 | (0,0) | 世界物品位置偏移（单位） |
