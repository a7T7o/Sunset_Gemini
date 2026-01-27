# 物品显示尺寸统一系统 - 设计文档

## 概述

本系统为所有物品提供统一的显示尺寸控制机制。核心思路是在 ItemData SO 中添加可选的像素尺寸限定参数，用于控制世界物品（World Prefab）的显示大小。背包/工具栏/配方界面保持现有的自适应填充逻辑，但修复旋转后边界计算问题。

### 设计原则

1. **可选配置** - 显示尺寸参数是可选的，不影响现有物品
2. **场景分离** - 世界物品使用 displayPixelSize，UI 使用自适应填充
3. **同步更新** - Sprite 尺寸变化时阴影同步更新
4. **向后兼容** - 未配置的物品保持现有行为

## 架构设计

### 数据流

```
┌─────────────────────────────────────────────────────────────────┐
│                        ItemData SO                               │
├─────────────────────────────────────────────────────────────────┤
│  useCustomDisplaySize: bool    │ 是否启用自定义显示尺寸          │
│  displayPixelSize: int         │ 像素尺寸限定（8-128）           │
└─────────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          │                   │                   │
          ▼                   ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ World Prefab    │  │ WorldItemPickup │  │ UI 显示         │
│ 生成器          │  │ 运行时          │  │ (背包/工具栏)   │
├─────────────────┤  ├─────────────────┤  ├─────────────────┤
│ ★ 使用          │  │ ★ 使用          │  │ ✗ 不使用        │
│ displayPixelSize│  │ displayPixelSize│  │ displayPixelSize│
│                 │  │                 │  │                 │
│ 计算缩放比例    │  │ 计算缩放比例    │  │ 自适应填充      │
│ 应用到 Sprite   │  │ 应用到 Sprite   │  │ 旋转后边界计算  │
│ 同步阴影        │  │ 同步阴影        │  │ 2px 内边距      │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

### 缩放计算公式

```
给定：
- Sprite 原始尺寸：W × H 像素
- 目标像素方框：N × N 像素
- PPU (Pixels Per Unit)：16

计算：
1. 缩放比例 = min(N / W, N / H)
2. 世界单位缩放 = 缩放比例 × (原始世界尺寸)
3. 最终 localScale = 缩放比例
```

## 组件和接口

### 1. ItemData 修改

**文件**: `Assets/YYY_Scripts/Data/Items/ItemData.cs`

```csharp
[Header("=== 显示尺寸配置 ===")]
[Tooltip("是否启用自定义显示尺寸（用于世界物品）")]
public bool useCustomDisplaySize = false;

[Tooltip("像素尺寸限定（8-128），物品 Sprite 将等比例缩放至适配此方框")]
[Range(8, 128)]
public int displayPixelSize = 32;

/// <summary>
/// 获取世界物品的缩放比例
/// </summary>
public float GetWorldDisplayScale()
{
    if (!useCustomDisplaySize) return 1f;
    
    if (icon == null) return 1f;
    
    float spriteWidth = icon.rect.width;
    float spriteHeight = icon.rect.height;
    float maxDimension = Mathf.Max(spriteWidth, spriteHeight);
    
    if (maxDimension <= 0) return 1f;
    
    return Mathf.Clamp(displayPixelSize, 8, 128) / maxDimension;
}
```

### 2. UIItemIconScaler 修改

**文件**: `Assets/YYY_Scripts/UI/Utility/UIItemIconScaler.cs`

修复旋转后边界计算，使用旋转后的实际宽高：

```csharp
// 修改后的核心逻辑
private const float PADDING = 2f;  // 内边距

public static void SetIconWithAutoScale(Image image, Sprite sprite)
{
    // ... 现有代码 ...
    
    // ★ 修复：使用旋转后的边界作为 RectTransform 尺寸
    // 旋转后边界框尺寸
    float rotatedWidthInPixels = spriteWidthInPixels * cos + spriteHeightInPixels * sin;
    float rotatedHeightInPixels = spriteWidthInPixels * sin + spriteHeightInPixels * cos;
    
    // 显示区域减去内边距
    float availableArea = DISPLAY_AREA - PADDING * 2;  // 56 - 4 = 52
    
    // 计算缩放比例（让旋转后边界框适配可用区域）
    float scaleX = availableArea / rotatedWidthInPixels;
    float scaleY = availableArea / rotatedHeightInPixels;
    float scale = Mathf.Min(scaleX, scaleY);
    
    // ★ 关键修复：RectTransform 尺寸应该是旋转后的边界尺寸
    float finalWidth = rotatedWidthInPixels * scale;
    float finalHeight = rotatedHeightInPixels * scale;
    
    rt.sizeDelta = new Vector2(finalWidth, finalHeight);
}
```

### 3. WorldPrefabGeneratorTool 修改

**文件**: `Assets/Editor/WorldPrefabGeneratorTool.cs`

在生成预制体时读取并应用 displayPixelSize：

```csharp
private void GeneratePrefab(ItemData itemData)
{
    // ... 现有代码 ...
    
    // ★ 新增：计算显示尺寸缩放
    float displayScale = itemData.GetWorldDisplayScale();
    
    // 应用到 Sprite 子物体
    spriteObj.transform.localScale = Vector3.one * displayScale;
    
    // 阴影同步缩放
    shadowObj.transform.localScale = new Vector3(
        scaleX * displayScale, 
        scaleY * displayScale, 
        1f
    );
}
```

### 4. WorldItemPickup 修改

**文件**: `Assets/YYY_Scripts/World/WorldItemPickup.cs`

在初始化时应用显示尺寸：

```csharp
public void ApplyDisplaySize()
{
    if (linkedItemData == null || !linkedItemData.useCustomDisplaySize) return;
    
    float scale = linkedItemData.GetWorldDisplayScale();
    
    // 应用到 Sprite
    if (spriteRenderer != null)
    {
        spriteRenderer.transform.localScale = Vector3.one * scale;
    }
    
    // 同步阴影
    var shadow = transform.Find("Shadow");
    if (shadow != null)
    {
        shadow.localScale *= scale;
    }
}
```

### 5. Tool_BatchItemSOGenerator 修改

**文件**: `Assets/Editor/Tool_BatchItemSOGenerator.cs`

添加显示尺寸配置选项：

```csharp
// 新增字段
private bool batchUseCustomDisplaySize = false;
private int batchDisplayPixelSize = 32;

// 在 OnGUI 中添加 UI
EditorGUILayout.LabelField("显示尺寸配置", EditorStyles.boldLabel);
batchUseCustomDisplaySize = EditorGUILayout.Toggle("启用自定义显示尺寸", batchUseCustomDisplaySize);
if (batchUseCustomDisplaySize)
{
    batchDisplayPixelSize = EditorGUILayout.IntSlider("像素尺寸", batchDisplayPixelSize, 8, 128);
}

// 在创建 SO 时应用
itemData.useCustomDisplaySize = batchUseCustomDisplaySize;
itemData.displayPixelSize = batchDisplayPixelSize;
```

## 数据模型

### 显示尺寸参数

| 参数 | 类型 | 范围 | 默认值 | 说明 |
|------|------|------|--------|------|
| useCustomDisplaySize | bool | - | false | 是否启用自定义显示尺寸 |
| displayPixelSize | int | 8-128 | 32 | 像素尺寸限定方框边长 |

### 缩放计算示例

| Sprite 尺寸 | displayPixelSize | 缩放比例 | 结果尺寸 |
|-------------|------------------|----------|----------|
| 16×16 | 32 | 2.0 | 32×32 |
| 32×32 | 32 | 1.0 | 32×32 |
| 48×32 | 32 | 0.67 | 32×21 |
| 16×24 | 48 | 2.0 | 32×48 |

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### 属性 1：世界物品缩放边界约束
*对于任意* Sprite 尺寸和任意 displayPixelSize 值，缩放后的 Sprite 最大边长应等于 displayPixelSize
**验证: 需求 1.3, 2.2, 3.2**

### 属性 2：阴影同步缩放
*对于任意* 世界物品，当 Sprite 缩放比例为 S 时，阴影的缩放比例也应为 S
**验证: 需求 2.4, 3.3**

### 属性 3：旋转后边界计算正确性
*对于任意* Sprite 尺寸 W×H 和旋转角度 θ，计算的旋转后边界应等于 |W×cos(θ)| + |H×sin(θ)| 和 |W×sin(θ)| + |H×cos(θ)|
**验证: 需求 4.1, 4.2, 4.3**

### 属性 4：UI 内边距约束
*对于任意* UI 槽位中的图标，旋转后的边界框应不超过槽位尺寸减去 4 像素（两边各 2 像素内边距）
**验证: 需求 4.4, 5.4**

### 属性 5：显示尺寸范围约束
*对于任意* displayPixelSize 输入值，实际使用的值应在 8-128 范围内
**验证: 需求 7.1, 7.2**

## 错误处理

| 场景 | 处理方式 |
|------|----------|
| icon 为空 | GetWorldDisplayScale() 返回 1.0 |
| displayPixelSize 为 0 或负数 | 使用默认值 32 |
| displayPixelSize 超出范围 | Clamp 到 8-128 范围 |
| Sprite 尺寸为 0 | 返回缩放比例 1.0 |

## 测试策略

### 单元测试
- GetWorldDisplayScale() 缩放计算正确性
- 旋转后边界计算正确性
- 参数范围验证

### 属性测试
使用 NUnit 进行属性测试，每个测试运行 100 次迭代：
- **Feature: item-display-size, Property 1: 世界物品缩放边界约束**
- **Feature: item-display-size, Property 3: 旋转后边界计算正确性**
- **Feature: item-display-size, Property 5: 显示尺寸范围约束**

### 手动测试
- 在 Inspector 中配置 displayPixelSize，验证 World Prefab 生成结果
- 验证背包/工具栏中图标显示正确，无裁剪或过多空白
- 验证运行时掉落物品尺寸与预制体一致
