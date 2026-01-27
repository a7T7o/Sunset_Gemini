# 预览与放置位置对齐修复 - 需求文档

**创建日期**: 2026-01-21  
**版本**: 1.0  
**状态**: 待审核

---

## 一、背景

用户报告放置系统存在预览位置与实际放置位置不一致的问题。具体表现为：
1. 预览绿框没有正确包裹物品的 Collider
2. 预览位置和放置后的位置有偏移，物品放置后会"跳动"
3. 格子大小计算可能不正确

---

## 二、问题分析

### 2.1 当前系统行为

| 组件 | 当前行为 | 问题 |
|------|---------|------|
| 格子大小计算 | `collider.bounds` 对未实例化预制体 | 返回错误值 |
| 预览 Sprite | 使用 `item.icon`（背包图标） | 与实际放置 Sprite 不同 |
| 预览对齐 | Sprite 中心 = 格子中心 | 与放置对齐方式不一致 |
| 放置对齐 | Sprite 底部中心 = 格子中心 | ChestController 底部对齐 |

### 2.2 问题根源

**问题 1：格子大小计算错误**

`PlacementGridCalculator.GetRequiredGridSize(GameObject prefab)` 方法：
```csharp
var collider = prefab.GetComponentInChildren<Collider2D>();
if (collider != null)
{
    return GetRequiredGridSize(collider.bounds);  // ← 问题：未实例化预制体的 bounds 不可靠
}
```

**问题 2：预览 Sprite 与实际 Sprite 不同**

`PlacementPreviewV3.Show()` 方法：
```csharp
itemPreviewRenderer.sprite = item.icon;  // ← 使用背包图标，不是预制体 Sprite
```

**问题 3：对齐方式不统一**

- 预览：Sprite 中心对齐到格子中心
- 放置：ChestController 使用底部对齐（`ApplySpriteWithBottomAlign`）

---

## 三、用户需求（明确）

根据用户描述，正确的行为应该是：

1. **预放置绿框应该包裹物品的 Collider**（不是 Sprite）
2. **格子应该是一个个 GridCell 拼起来的**
3. **预览和放置的中心应该统一**：都在预放置框的几何中心
4. **中心计算方式**：`(最左最右距离中点, 最上最下距离中点)`

---

## 四、用户故事

### US-1：格子正确包裹 Collider

**作为** 玩家  
**我希望** 预览绿框正确包裹物品的 Collider 区域  
**以便** 清楚知道物品实际占用的空间

**验收标准**：
- [ ] 格子大小基于预制体 PolygonCollider2D 的实际路径计算
- [ ] 格子数量 = Ceil(Collider 宽度) × Ceil(Collider 高度)
- [ ] 格子位置正确覆盖 Collider 区域

---

### US-2：预览与放置位置一致

**作为** 玩家  
**我希望** 预览位置和实际放置位置完全一致  
**以便** 物品放置后不会"跳动"

**验收标准**：
- [ ] 预览 Sprite 与放置后的 Sprite 位置完全一致
- [ ] Collider 中心对齐到格子几何中心
- [ ] 放置后物品不发生任何位置偏移

---

### US-3：预览使用正确的 Sprite

**作为** 玩家  
**我希望** 预览显示的是实际放置的物品外观  
**以便** 准确预知放置效果

**验收标准**：
- [ ] 预览 Sprite 来自 `placementPrefab` 中的 SpriteRenderer
- [ ] 预览 Sprite 的位置与实际放置一致

---

## 五、技术需求

### TR-1：从 PolygonCollider2D 路径计算格子大小

**需求描述**：
修改 `PlacementGridCalculator`，从 PolygonCollider2D 的 `GetPath()` 直接计算格子大小，而不是依赖 `bounds`。

**实现要点**：
```csharp
public static Vector2Int GetRequiredGridSizeFromPrefab(GameObject prefab)
{
    var polyCollider = prefab.GetComponentInChildren<PolygonCollider2D>();
    if (polyCollider != null && polyCollider.pathCount > 0)
    {
        return GetGridSizeFromPolygonCollider(polyCollider);
    }
    // 回退逻辑...
}

private static Vector2Int GetGridSizeFromPolygonCollider(PolygonCollider2D collider)
{
    float minX = float.MaxValue, maxX = float.MinValue;
    float minY = float.MaxValue, maxY = float.MinValue;
    
    for (int i = 0; i < collider.pathCount; i++)
    {
        Vector2[] path = collider.GetPath(i);
        foreach (var point in path)
        {
            Vector2 worldPoint = point + collider.offset;
            minX = Mathf.Min(minX, worldPoint.x);
            maxX = Mathf.Max(maxX, worldPoint.x);
            minY = Mathf.Min(minY, worldPoint.y);
            maxY = Mathf.Max(maxY, worldPoint.y);
        }
    }
    
    float width = maxX - minX;
    float height = maxY - minY;
    
    return new Vector2Int(
        Mathf.Max(1, Mathf.CeilToInt(width)),
        Mathf.Max(1, Mathf.CeilToInt(height))
    );
}
```

---

### TR-2：计算 Collider 本地中心

**需求描述**：
添加方法计算预制体 Collider 的本地空间几何中心。

**实现要点**：
```csharp
public static Vector2 GetColliderLocalCenter(GameObject prefab)
{
    var polyCollider = prefab.GetComponentInChildren<PolygonCollider2D>();
    if (polyCollider != null && polyCollider.pathCount > 0)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        for (int i = 0; i < polyCollider.pathCount; i++)
        {
            Vector2[] path = polyCollider.GetPath(i);
            foreach (var point in path)
            {
                Vector2 worldPoint = point + polyCollider.offset;
                minX = Mathf.Min(minX, worldPoint.x);
                maxX = Mathf.Max(maxX, worldPoint.x);
                minY = Mathf.Min(minY, worldPoint.y);
                maxY = Mathf.Max(maxY, worldPoint.y);
            }
        }
        
        return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
    }
    return Vector2.zero;
}
```

---

### TR-3：预览使用预制体 Sprite

**需求描述**：
修改 `PlacementPreviewV3.Show()`，使用预制体中的 Sprite 而不是 `item.icon`。

**实现要点**：
```csharp
public void Show(ItemData item, Vector2Int gridSize)
{
    // ...
    
    if (item.placementPrefab != null)
    {
        var prefabSR = item.placementPrefab.GetComponentInChildren<SpriteRenderer>();
        if (prefabSR != null && prefabSR.sprite != null)
        {
            itemPreviewRenderer.sprite = prefabSR.sprite;
            
            // 计算 Collider 中心偏移，调整预览 Sprite 位置
            Vector2 colliderCenter = PlacementGridCalculator.GetColliderLocalCenter(item.placementPrefab);
            itemPreviewRenderer.transform.localPosition = -colliderCenter;
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

---

### TR-4：放置位置使用 Collider 中心对齐

**需求描述**：
修改 `PlacementManagerV3.InstantiatePlacementPrefab()`，使放置位置基于 Collider 中心对齐。

**实现要点**：
```csharp
private GameObject InstantiatePlacementPrefab(Vector3 gridCenter)
{
    if (currentPlacementItem.placementPrefab == null) return null;
    
    // 计算 Collider 本地中心偏移
    Vector2 colliderLocalCenter = PlacementGridCalculator.GetColliderLocalCenter(
        currentPlacementItem.placementPrefab);
    
    // 放置位置 = 格子中心 - Collider 本地中心偏移
    // 这样 Collider 的中心就会对齐到格子中心
    Vector3 placementPosition = gridCenter - (Vector3)colliderLocalCenter;
    
    return Instantiate(currentPlacementItem.placementPrefab, placementPosition, Quaternion.identity);
}
```

---

### TR-5：调整 ChestController 对齐机制

**需求描述**：
评估并调整 ChestController 的底部对齐机制，使其与新的 Collider 中心对齐方案兼容。

**选项**：
- **选项 A**：移除底部对齐，完全使用 Collider 中心对齐
- **选项 B**：保留底部对齐用于 Sprite 切换，但初始放置使用 Collider 中心

**建议**：选项 B，因为底部对齐主要用于 Sprite 状态切换（开/关），不影响初始放置。

---

## 六、验收清单

### 功能验收

- [ ] 箱子预览格子正确包裹 Collider（1x1 格子）
- [ ] 树苗预览格子正确包裹 Collider
- [ ] 预览 Sprite 与放置后 Sprite 一致
- [ ] 预览位置与放置位置完全一致（无跳动）
- [ ] Collider 中心对齐到格子几何中心

### 边界情况验收

- [ ] 大型物品（多格子）预览正确
- [ ] 没有 PolygonCollider2D 的物品回退正常
- [ ] 没有 placementPrefab 的物品回退正常

### 回归测试

- [ ] 树苗放置功能正常
- [ ] 箱子放置功能正常
- [ ] 放置后 NavGrid 刷新正常
- [ ] 放置后物品交互正常

---

## 七、相关文件

| 文件 | 修改内容 |
|------|---------|
| `PlacementGridCalculator.cs` | 添加从 PolygonCollider2D 计算格子大小和中心的方法 |
| `PlacementPreviewV3.cs` | 修改 Show() 使用预制体 Sprite |
| `PlacementManagerV3.cs` | 修改 InstantiatePlacementPrefab() 使用 Collider 中心对齐 |
| `ChestController.cs` | 评估底部对齐机制的兼容性 |

---

## 八、箱子预制体 Collider 数据参考

从 `Box_1.prefab` 分析：

```yaml
PolygonCollider2D:
  m_Points:
    m_Paths:
      - - {x: -0.5, y: -0.625}
        - {x: 0.5, y: -0.625}
        - {x: 0.5, y: 0.03125}
        - {x: -0.5, y: 0.03125}

SpriteRenderer:
  m_Size: {x: 1, y: 1.25}
```

**计算结果**：
- Collider 宽度：1.0（-0.5 到 0.5）
- Collider 高度：0.65625（-0.625 到 0.03125）
- Collider 中心：(0, -0.296875)
- 格子大小：1x1（向上取整）

---

**文档维护者**: Kiro  
**最后更新**: 2026-01-21
