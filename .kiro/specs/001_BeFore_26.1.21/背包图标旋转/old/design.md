# 背包图标旋转显示系统 - 设计文档

## 概述

本系统通过 UI 层面的 Transform 旋转实现背包/工具栏/装备栏中物品图标的 45 度旋转显示效果。核心思路是在 `UIItemIconScaler` 中统一处理旋转逻辑，所有使用该工具的 UI 组件自动获得旋转效果。

### 设计原则

1. **单一职责** - 旋转逻辑集中在 `UIItemIconScaler` 中
2. **向后兼容** - 保留 `bagSprite` 字段但不再使用
3. **像素完整** - 使用 Transform 旋转而非图片处理
4. **统一风格** - 背包图标与世界物品视觉一致

## 架构设计

### 修改前后对比

**修改前**:
```
ItemData.icon → GetBagSprite() → UIItemIconScaler.SetIconWithAutoScale() → Image.sprite
                    ↓
            bagSprite (如果有)
```

**修改后**:
```
ItemData.icon → GetBagSprite() → UIItemIconScaler.SetIconWithAutoScale() → Image.sprite
                    ↓                           ↓
               返回 icon              RectTransform.rotation = 45°
                                              ↓
                                    缩放适配旋转后边界框
```

### 核心架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    UI 图标显示系统                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐                                       │
│  │    ItemData      │                                       │
│  │  ┌────────────┐  │                                       │
│  │  │   icon     │──┼──────────────────────┐                │
│  │  └────────────┘  │                      │                │
│  │  ┌────────────┐  │                      │                │
│  │  │ bagSprite  │  │ (废弃，不再使用)      │                │
│  │  └────────────┘  │                      │                │
│  └──────────────────┘                      │                │
│                                            ▼                │
│                              ┌──────────────────────┐       │
│                              │  UIItemIconScaler    │       │
│                              │  ┌────────────────┐  │       │
│                              │  │ ICON_ROTATION  │  │       │
│                              │  │    = 45°       │  │       │
│                              │  └────────────────┘  │       │
│                              │  ┌────────────────┐  │       │
│                              │  │ 计算旋转后     │  │       │
│                              │  │ 边界框尺寸     │  │       │
│                              │  └────────────────┘  │       │
│                              │  ┌────────────────┐  │       │
│                              │  │ 应用旋转和缩放 │  │       │
│                              │  └────────────────┘  │       │
│                              └──────────┬───────────┘       │
│                                         │                   │
│           ┌─────────────────────────────┼─────────────────┐ │
│           │                             │                 │ │
│           ▼                             ▼                 ▼ │
│  ┌─────────────────┐   ┌─────────────────┐   ┌───────────┐ │
│  │ InventorySlotUI │   │  ToolbarSlotUI  │   │EquipSlotUI│ │
│  │  Image.sprite   │   │  Image.sprite   │   │Image.sprite│ │
│  │  rotation=45°   │   │  rotation=45°   │   │rotation=45°│ │
│  └─────────────────┘   └─────────────────┘   └───────────┘ │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## 组件和接口

### UIItemIconScaler（修改）

**文件**: `Assets/YYY_Scripts/UI/Utility/UIItemIconScaler.cs`

**新增常量**:
```csharp
private const float ICON_ROTATION_Z = 45f;  // 图标 Z 轴旋转角度
```

**修改方法**: `SetIconWithAutoScale(Image image, Sprite sprite)`

核心逻辑变更：
1. 计算旋转后的边界框尺寸
2. 根据旋转后边界框计算缩放比例
3. 应用旋转到 RectTransform

### ItemData（保持不变）

**文件**: `Assets/YYY_Scripts/Data/Items/ItemData.cs`

`GetBagSprite()` 方法保持不变，继续返回 `bagSprite ?? icon`。
旋转逻辑由 UI 层处理，数据层不需要修改。

### WorldPrefabGeneratorTool（微调）

**文件**: `Assets/Editor/WorldPrefabGeneratorTool.cs`

确保不设置 `bagSprite` 字段（当前已经是 null）。

### Tool_BatchItemSOGenerator（微调）

**文件**: `Assets/Editor/Tool_BatchItemSOGenerator.cs`

确保不设置 `bagSprite` 字段。

## 数据模型

### 旋转后边界框计算

对于一个 `width × height` 的矩形，旋转 θ 角度后的边界框尺寸：

```
rotatedWidth = |width × cos(θ)| + |height × sin(θ)|
rotatedHeight = |width × sin(θ)| + |height × cos(θ)|
```

对于 45° 旋转（cos(45°) = sin(45°) ≈ 0.707）：
- 正方形 16×16 → 旋转后约 22.6×22.6
- 需要缩放约 0.707 倍才能适配原尺寸

### 缩放计算

```csharp
// 计算旋转后边界框
float rotRad = ICON_ROTATION_Z * Mathf.Deg2Rad;
float cos = Mathf.Abs(Mathf.Cos(rotRad));
float sin = Mathf.Abs(Mathf.Sin(rotRad));
float rotatedWidth = spriteWidth * cos + spriteHeight * sin;
float rotatedHeight = spriteWidth * sin + spriteHeight * cos;

// 计算缩放比例（让旋转后边界框适配显示区域）
float scaleX = displayArea / rotatedWidth;
float scaleY = displayArea / rotatedHeight;
float scale = Mathf.Min(scaleX, scaleY);
```

## 正确性属性

*正确性属性是系统在所有有效执行中都应保持为真的特征或行为——本质上是关于系统应该做什么的形式化陈述。属性是人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1：旋转角度一致性
*对于任意* UI 槽位（背包、工具栏、装备栏），图标旋转角度应精确为 Z 轴 45 度。
**验证: 需求 1.1, 1.2, 1.3, 3.3**

### 属性 2：边界框不超出
*对于任意* 旋转后的图标，旋转后的边界框应适配槽位的显示区域（56×56 像素）。
**验证: 需求 4.2, 4.3**

### 属性 3：像素完整性
*对于任意* 图标 sprite，旋转应通过 Transform 旋转应用，而非图片处理，保持像素完整性。
**验证: 需求 1.4**

### 属性 4：回退逻辑一致性
*对于任意* bagSprite 为空的 ItemData，GetBagSprite() 应返回 icon sprite。
**验证: 需求 2.1, 2.2, 2.4**

## 错误处理

1. **空 Sprite 处理** - 如果 sprite 为 null，禁用 Image 组件，不应用旋转
2. **零尺寸 Sprite** - 如果 sprite 尺寸为 0，跳过缩放计算
3. **非法旋转角度** - 旋转角度应在 0-90 度范围内

## 测试策略

### 单元测试

1. **边界框计算测试** - 验证不同尺寸 sprite 的旋转后边界框计算正确
2. **缩放比例测试** - 验证缩放后图标不超出显示区域
3. **回退逻辑测试** - 验证 bagSprite 为空时正确返回 icon

### 属性测试

使用 NUnit 进行属性测试：

1. **属性 1 测试** - 验证所有 UI 槽位的旋转角度一致
2. **属性 2 测试** - 生成随机尺寸 sprite，验证旋转后边界框不超出
3. **属性 4 测试** - 随机设置 bagSprite 为 null 或有效值，验证回退逻辑

