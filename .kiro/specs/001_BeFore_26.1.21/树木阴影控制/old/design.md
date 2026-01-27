# 设计文档

## 概述

树木影子控制系统是对现有 TreeController 的扩展，将影子 Sprite 切换和显示控制功能集成到树木控制器中。核心设计原则：

1. **集成而非分离** - 影子控制逻辑集成到 TreeController，避免额外组件
2. **配置驱动** - 通过 Inspector 配置影子 Sprite 和缩放参数
3. **状态联动** - 影子状态与树木阶段/状态自动同步
4. **向后兼容** - 未配置影子 Sprite 时保持原有行为

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                      TreeController                          │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  现有功能                                            │    │
│  │  - 成长阶段管理 (GrowthStage)                       │    │
│  │  - 树木状态管理 (TreeState)                         │    │
│  │  - Sprite 更新 (UpdateSprite)                       │    │
│  │  - 影子缩放 (UpdateShadowScale) ← 扩展此方法        │    │
│  └─────────────────────────────────────────────────────┘    │
│                              │                               │
│                              ▼                               │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  新增功能                                            │    │
│  │  - 影子 Sprite 配置字段                             │    │
│  │  - 影子 Sprite 切换逻辑                             │    │
│  │  - 影子显示/隐藏控制                                │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Shadow 子物体                           │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  SpriteRenderer                                      │    │
│  │  - sprite: 由 TreeController 控制                   │    │
│  │  - enabled: 由 TreeController 控制                  │    │
│  │  - localScale: 由 TreeController 控制               │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## 组件和接口

### TreeController 新增字段

```csharp
[Header("━━━━ 影子 Sprite 配置 ━━━━")]
[Tooltip("小树阶段的影子 Sprite（可选，为空则使用原有 Sprite）")]
public Sprite shadowSprite_Small;

[Tooltip("大树阶段的影子 Sprite（可选，为空则使用原有 Sprite）")]
public Sprite shadowSprite_Large;

// 缓存原始影子 Sprite（用于未配置时的回退）
private Sprite _originalShadowSprite;
```

### 影子更新方法（扩展现有方法）

```csharp
/// <summary>
/// 更新 Shadow 显示状态、Sprite、缩放和位置
/// ✅ 扩展版：支持影子 Sprite 切换
/// </summary>
private void UpdateShadow()
{
    // 1. 获取 Shadow 子物体
    if (transform.parent == null) return;
    Transform shadowTransform = transform.parent.Find("Shadow");
    if (shadowTransform == null) return;
    
    SpriteRenderer shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
    if (shadowRenderer == null) return;
    
    // 2. 缓存原始 Sprite（首次调用时）
    if (_originalShadowSprite == null && shadowRenderer.sprite != null)
    {
        _originalShadowSprite = shadowRenderer.sprite;
    }
    
    // 3. 判断是否显示影子
    bool shouldShowShadow = ShouldShowShadow();
    shadowRenderer.enabled = shouldShowShadow;
    
    if (!shouldShowShadow) return;
    
    // 4. 切换影子 Sprite
    Sprite targetSprite = GetShadowSpriteForCurrentStage();
    if (targetSprite != null)
    {
        shadowRenderer.sprite = targetSprite;
    }
    
    // 5. 应用缩放
    float targetScale = GetShadowScaleForCurrentStage();
    shadowTransform.localScale = new Vector3(targetScale, targetScale, 1f);
    
    // 6. 对齐影子中心到父物体中心
    AlignShadowCenter(shadowTransform, shadowRenderer);
}

/// <summary>
/// 判断当前是否应该显示影子
/// </summary>
private bool ShouldShowShadow()
{
    // 树苗无影子
    if (currentStage == GrowthStage.Sapling) return false;
    
    // 树桩无影子
    if (currentState == TreeState.Stump) return false;
    
    // 小树和大树有影子
    return true;
}

/// <summary>
/// 获取当前阶段对应的影子 Sprite
/// </summary>
private Sprite GetShadowSpriteForCurrentStage()
{
    return currentStage switch
    {
        GrowthStage.Small => shadowSprite_Small ?? _originalShadowSprite,
        GrowthStage.Large => shadowSprite_Large ?? _originalShadowSprite,
        _ => _originalShadowSprite
    };
}

/// <summary>
/// 获取当前阶段对应的影子缩放
/// </summary>
private float GetShadowScaleForCurrentStage()
{
    return currentStage switch
    {
        GrowthStage.Small => shadowScaleStage1,
        GrowthStage.Large => shadowScaleStage2,
        _ => shadowScaleStage2
    };
}

/// <summary>
/// 对齐影子中心到父物体中心（树根位置）
/// </summary>
private void AlignShadowCenter(Transform shadowTransform, SpriteRenderer shadowRenderer)
{
    if (shadowRenderer.sprite == null) return;
    
    Bounds shadowBounds = shadowRenderer.sprite.bounds;
    float centerOffset = shadowBounds.center.y;
    
    Vector3 shadowPos = shadowTransform.localPosition;
    shadowPos.y = -centerOffset;
    shadowTransform.localPosition = shadowPos;
}
```

## 数据模型

### 影子可见性规则

| 成长阶段 | 树木状态 | 影子可见 | 影子 Sprite |
|----------|----------|----------|-------------|
| Sapling | Normal | ✗ | - |
| Sapling | Stump | ✗ | - |
| Small | Normal | ✓ | shadowSprite_Small |
| Small | Stump | ✗ | - |
| Large | Normal | ✓ | shadowSprite_Large |
| Large | Stump | ✗ | - |

### 影子缩放配置

| 成长阶段 | 默认缩放 | 配置字段 |
|----------|----------|----------|
| Small | 0.8 | shadowScaleStage1 |
| Large | 1.0 | shadowScaleStage2 |

## 正确性属性

*属性是指在系统所有有效执行中都应保持为真的特征或行为——本质上是关于系统应该做什么的形式化陈述。属性是人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1: 影子可见性与阶段/状态映射
*对于任何*树木，影子可见性应满足：Sapling 阶段隐藏、Stump 状态隐藏、Small/Large 阶段且非 Stump 状态显示
**验证: 需求 2.1, 2.2, 2.3, 2.4, 4.1, 4.2**

### 属性 2: 成长时影子 Sprite 正确切换
*对于任何*从 Sapling 成长为 Small 或从 Small 成长为 Large 的树木，影子 Sprite 应切换为对应阶段配置的 Sprite
**验证: 需求 3.1, 3.2**

### 属性 3: 影子缩放与阶段映射
*对于任何*显示影子的树木，影子缩放应等于当前阶段配置的缩放值，且 X/Y 轴缩放相等
**验证: 需求 3.3, 5.3, 5.4**

### 属性 4: 影子位置对齐
*对于任何*显示影子的树木，影子几何中心应对齐父物体中心（树根位置）
**验证: 需求 3.4**

### 属性 5: 未配置时回退到原始 Sprite
*对于任何*未配置影子 Sprite 的阶段，系统应使用 Shadow 子物体原有的 Sprite
**验证: 需求 1.3**

## 错误处理

1. **Shadow 子物体不存在**：方法提前返回，不报错（兼容无影子的树木预制体）
2. **SpriteRenderer 组件不存在**：方法提前返回，不报错
3. **影子 Sprite 配置为空**：使用原始 Sprite 作为回退
4. **原始 Sprite 也为空**：保持 SpriteRenderer 当前状态

## 测试策略

### 单元测试
- ShouldShowShadow() 方法对各种阶段/状态组合的返回值
- GetShadowSpriteForCurrentStage() 方法的 Sprite 选择逻辑
- GetShadowScaleForCurrentStage() 方法的缩放值返回

### 集成测试
- 树木成长时影子状态变化
- 树木被砍倒时影子隐藏
- 影子 Sprite 切换后位置对齐

### 手动测试
- Inspector 中配置影子 Sprite
- 运行时观察影子变化
- 不同阶段的影子外观验证

## 实现注意事项

1. **方法重命名**：将现有的 `UpdateShadowScale()` 重命名为 `UpdateShadow()`，因为功能已扩展
2. **调用时机**：在 `UpdateSprite()` 中调用，确保与树木 Sprite 同步更新
3. **性能考虑**：缓存 Shadow Transform 和 SpriteRenderer 引用，避免每次 Find
4. **编辑器预览**：支持在编辑器中实时预览影子变化

