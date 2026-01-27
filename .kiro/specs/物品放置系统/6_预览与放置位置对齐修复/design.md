# 预览与放置位置对齐修复 - 设计文档

**创建日期**: 2026-01-21  
**版本**: 2.0（考虑底部对齐的影响）  
**状态**: 已确认

---

## 一、架构设计

### 1.1 核心概念

| 概念 | 定义 | 计算方式 |
|------|------|---------|
| **Collider Bounds** | 预制体 Collider 的本地空间边界 | 从 PolygonCollider2D.GetPath() 计算 |
| **Collider Center** | Collider Bounds 的几何中心 | (minX+maxX)/2, (minY+maxY)/2 |
| **Grid Size** | 包裹 Collider 的最小格子数量 | Ceil(width), Ceil(height) |
| **Grid Center** | 格子矩形的几何中心 | Floor(mousePos) + 0.5 |
| **Bottom Align Offset** | 底部对齐后 Sprite 的 Y 偏移 | -sprite.bounds.min.y |
| **Final Collider Center** | 底部对齐后 Collider 中心相对于物品原点的位置 | Collider Center + (0, Bottom Align Offset) |
| **Placement Position** | 预制体实例化位置 | Grid Center - Final Collider Center |

### 1.2 位置对齐原则

**核心规则**：放置后 Collider 的几何中心对齐到格子的几何中心

**关键洞察**：TreeControllerV2 和 ChestController 在 Awake/Start 时会执行底部对齐（`AlignSpriteBottom`），这会改变 Sprite 相对于物品原点的位置。我们需要**预测这个变化**，计算出放置后 Collider 中心的最终位置。

```
底部对齐偏移 = -sprite.bounds.min.y
放置后 Collider 中心 = 原始 Collider 中心 + (0, 底部对齐偏移)
放置位置 = 格子中心 - 放置后 Collider 中心
```

这样确保：
1. 放置后 Collider 中心 = 格子中心
2. 预览和放置位置完全一致
3. 格子正确包裹 Collider
4. 自动适应不同的底部对齐处理

---

## 二、组件设计

### 2.1 PlacementGridCalculator 修改

**新增方法**：

```csharp
/// <summary>
/// 计算底部对齐后 Collider 中心相对于物品原点的位置
/// 这是预览和放置都需要使用的核心计算
/// </summary>
public static Vector2 GetColliderCenterAfterBottomAlign(GameObject prefab)
{
    if (prefab == null) return Vector2.zero;
    
    var sr = prefab.GetComponentInChildren<SpriteRenderer>();
    var polyCollider = prefab.GetComponentInChildren<PolygonCollider2D>();
    
    // 如果没有 SpriteRenderer，回退到原始 Collider 中心
    if (sr == null || sr.sprite == null)
    {
        return GetColliderLocalCenter(prefab);
    }
    
    // 计算底部对齐偏移
    float bottomAlignOffset = -sr.sprite.bounds.min.y;
    
    // 获取原始 Collider 中心
    Vector2 colliderCenter = GetColliderLocalCenter(prefab);
    
    // 底部对齐后，Collider 中心相对于物品原点的位置
    return new Vector2(colliderCenter.x, colliderCenter.y + bottomAlignOffset);
}

/// <summary>
/// 计算预览 Sprite 的 localPosition
/// 使 Collider 中心对齐到格子中心
/// </summary>
public static Vector3 GetPreviewSpriteLocalPosition(GameObject prefab)
{
    Vector2 finalColliderCenter = GetColliderCenterAfterBottomAlign(prefab);
    
    // 预览 Sprite 需要向反方向偏移
    // 同时应用底部对齐效果
    var sr = prefab.GetComponentInChildren<SpriteRenderer>();
    if (sr != null && sr.sprite != null)
    {
        float bottomAlignOffset = -sr.sprite.bounds.min.y;
        return new Vector3(-finalColliderCenter.x, bottomAlignOffset - finalColliderCenter.y, 0);
    }
    
    return -(Vector3)finalColliderCenter;
}
```

**保留方法**（已实现）：

```csharp
/// <summary>
/// 从预制体的 PolygonCollider2D 路径计算格子大小
/// </summary>
public static Vector2Int GetRequiredGridSizeFromPrefab(GameObject prefab)

/// <summary>
/// 获取预制体 Collider 的本地空间几何中心（不考虑底部对齐）
/// </summary>
public static Vector2 GetColliderLocalCenter(GameObject prefab)
```

### 2.2 PlacementPreviewV3 修改

**修改 Show() 方法**：

```csharp
public void Show(ItemData item, Vector2Int gridSize)
{
    // ... 现有代码 ...
    
    if (item.placementPrefab != null)
    {
        var prefabSR = item.placementPrefab.GetComponentInChildren<SpriteRenderer>();
        if (prefabSR != null && prefabSR.sprite != null)
        {
            itemPreviewRenderer.sprite = prefabSR.sprite;
            
            // ★ 计算预览 Sprite 位置，考虑底部对齐的影响
            // 使 Collider 中心对齐到格子中心
            Vector3 spriteLocalPos = PlacementGridCalculator.GetPreviewSpriteLocalPosition(item.placementPrefab);
            itemPreviewRenderer.transform.localPosition = spriteLocalPos;
            
            Color previewColor = Color.white;
            previewColor.a = itemPreviewAlpha;
            itemPreviewRenderer.color = previewColor;
        }
    }
    else if (item.icon != null)
    {
        // 回退到 icon
        itemPreviewRenderer.sprite = item.icon;
        itemPreviewRenderer.transform.localPosition = Vector3.zero;
    }
    
    // ...
}
```

### 2.3 PlacementManagerV3 修改

**修改 InstantiatePlacementPrefab() 方法**：

```csharp
private GameObject InstantiatePlacementPrefab(Vector3 gridCenter)
{
    if (currentPlacementItem.placementPrefab == null) return null;
    
    // ★ 计算放置后 Collider 中心的位置（考虑底部对齐）
    Vector2 finalColliderCenter = PlacementGridCalculator.GetColliderCenterAfterBottomAlign(
        currentPlacementItem.placementPrefab);
    
    // 放置位置 = 格子中心 - 放置后 Collider 中心
    // 这样放置后（底部对齐执行后）Collider 中心就会在格子中心
    Vector3 placementPosition = gridCenter - (Vector3)finalColliderCenter;
    
    return Instantiate(currentPlacementItem.placementPrefab, placementPosition, Quaternion.identity);
}
```

### 2.4 ChestController / TreeControllerV2 兼容性

**无需修改**：

这两个组件的底部对齐机制保持不变。放置系统已经预测了底部对齐的影响，所以：
1. 物品实例化到计算好的位置
2. Awake/Start 中执行底部对齐
3. 最终 Collider 中心 = 格子中心

---

## 三、数据流

### 3.1 预览显示流程

```
1. EnterPlacementMode(item)
2. PlacementPreviewV3.Show(item)
   ├── 获取预制体 Sprite
   ├── 计算底部对齐偏移 = -sprite.bounds.min.y
   ├── 计算原始 Collider 中心
   ├── 计算放置后 Collider 中心 = 原始中心 + (0, 底部对齐偏移)
   ├── 设置 Sprite localPosition 使 Collider 中心对齐格子中心
   └── 创建格子 (gridSize)
3. UpdatePreview()
   ├── 计算格子中心 = Floor(mousePos) + 0.5
   ├── 设置 transform.position = 格子中心
   └── 更新格子位置
```

### 3.2 放置执行流程

```
1. ExecutePlacement()
2. position = placementPreview.LockedPosition (格子中心)
3. InstantiatePlacementPrefab(position)
   ├── 计算放置后 Collider 中心（考虑底部对齐）
   ├── placementPosition = position - finalColliderCenter
   └── Instantiate(prefab, placementPosition)
4. 物品 Awake/Start 执行底部对齐
5. 结果：Collider 中心 = 格子中心 ✓
```

### 3.3 计算示例（树苗 M1）

假设树苗预制体数据：
- Sprite bounds.min.y = -0.5
- Collider 中心 = (0, 0)

计算过程：
```
底部对齐偏移 = -(-0.5) = 0.5
放置后 Collider 中心 = (0, 0) + (0, 0.5) = (0, 0.5)
放置位置 = 格子中心 - (0, 0.5)

如果格子中心 = (5.5, 10.5)
则放置位置 = (5.5, 10.0)

放置后：
- 物品原点在 (5.5, 10.0)
- 底部对齐使 Sprite 向上移动 0.5
- Collider 中心 = (5.5, 10.0 + 0.5) = (5.5, 10.5) = 格子中心 ✓
```

---

## 四、边界情况处理

### 4.1 没有 SpriteRenderer（不执行底部对齐）

如果预制体没有 SpriteRenderer，则不会执行底部对齐，直接使用原始 Collider 中心：

```csharp
public static Vector2 GetColliderCenterAfterBottomAlign(GameObject prefab)
{
    var sr = prefab.GetComponentInChildren<SpriteRenderer>();
    
    // 没有 SpriteRenderer，回退到原始 Collider 中心
    if (sr == null || sr.sprite == null)
    {
        return GetColliderLocalCenter(prefab);
    }
    
    // ... 正常计算 ...
}
```

### 4.2 没有 PolygonCollider2D

回退到 BoxCollider2D 或默认值：

```csharp
public static Vector2 GetColliderLocalCenter(GameObject prefab)
{
    // 1. 尝试 PolygonCollider2D
    var polyCollider = prefab.GetComponentInChildren<PolygonCollider2D>();
    if (polyCollider != null && polyCollider.pathCount > 0)
        return CalculateFromPolygon(polyCollider);
    
    // 2. 尝试 BoxCollider2D
    var boxCollider = prefab.GetComponentInChildren<BoxCollider2D>();
    if (boxCollider != null)
        return boxCollider.offset;
    
    // 3. 默认返回零点
    return Vector2.zero;
}
```

### 4.3 没有 placementPrefab

回退到 item.icon，不计算偏移：

```csharp
if (item.placementPrefab != null)
{
    // 使用预制体 Sprite + 计算偏移
}
else if (item.icon != null)
{
    itemPreviewRenderer.sprite = item.icon;
    itemPreviewRenderer.transform.localPosition = Vector3.zero;
}
```

### 4.4 物品不使用底部对齐

如果某个物品的 Controller 没有底部对齐逻辑（如 `alignSpriteBottom = false`），计算结果仍然正确：
- 底部对齐偏移 = 0（因为不执行）
- 放置后 Collider 中心 = 原始 Collider 中心
- 放置位置 = 格子中心 - 原始 Collider 中心

**注意**：当前实现假设所有物品都会执行底部对齐。如果需要支持不执行底部对齐的物品，可以在 ItemData 中添加标志位。

---

## 五、验证清单

### 功能验证

- [ ] 格子大小正确反映 Collider 尺寸
- [ ] 预览 Sprite 来自预制体
- [ ] 预览位置与放置位置一致（考虑底部对齐）
- [ ] 放置后 Collider 中心对齐格子中心
- [ ] 放置后物品不跳动

### 回归验证

- [ ] 树苗放置正常（底部对齐物品）
- [ ] 箱子放置正常（底部对齐物品）
- [ ] 连续放置正常
- [ ] 导航放置正常
- [ ] NavGrid 刷新正常

---

## 六、相关文件

| 文件 | 修改类型 | 修改内容 |
|------|---------|---------|
| `PlacementGridCalculator.cs` | 新增方法 | `GetColliderCenterAfterBottomAlign()`, `GetPreviewSpriteLocalPosition()` |
| `PlacementPreviewV3.cs` | 修改 Show() | 使用新的位置计算方法 |
| `PlacementManagerV3.cs` | 修改 InstantiatePlacementPrefab() | 使用新的位置计算方法 |
| `ChestController.cs` | 无需修改 | - |
| `TreeControllerV2.cs` | 无需修改 | - |

---

**文档维护者**: Kiro  
**最后更新**: 2026-01-21
