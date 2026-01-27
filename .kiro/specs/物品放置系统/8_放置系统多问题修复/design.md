# 放置系统多问题修复 - 设计文档

**版本**: 2.0  
**创建日期**: 2026-01-21  
**最后更新**: 2026-01-22  
**状态**: 🔴 需要重新设计

---

## 一、问题反思

### 1.1 之前设计的错误

| 错误 | 说明 |
|------|------|
| P1 修复不完整 | 只对 SaplingData 做了特殊处理，箱子等其他物品仍无法检测无碰撞体物品 |
| P2 错误结论 | 声称"验证通过"，但实际未分析 `GetOccupiedCellCenters()` 的计算逻辑 |
| 缺乏全局视角 | 只关注单一物品类型，未考虑所有可放置物品的通用检测需求 |

### 1.2 正确的设计思路

**核心原则**：所有可放置物品都需要检测所有已放置物品，不论是否有碰撞体。

---

## 二、P1 修复：统一障碍物检测

### 2.1 问题根因

**当前代码**（`PlacementValidatorV3.HasObstacle()`）：
```csharp
public bool HasObstacle(Vector3 cellCenter)
{
    Vector2 boxSize = new Vector2(0.9f, 0.9f);
    Collider2D[] hits = Physics2D.OverlapBoxAll(cellCenter, boxSize, 0f);
    
    foreach (var hit in hits)
    {
        if (HasAnyTag(hit.transform, obstacleTags))
            return true;
    }
    return false;
}
```

**问题**：只检测有碰撞体的物体，无法检测：
- 树苗（Stage 0，无碰撞体）
- 其他可能没有碰撞体的已放置物品

### 2.2 修复方案

**方案 A：在 `HasObstacle()` 中添加无碰撞体物品检测**

```csharp
public bool HasObstacle(Vector3 cellCenter)
{
    // 1. 原有的碰撞体检测
    Vector2 boxSize = new Vector2(0.9f, 0.9f);
    Collider2D[] hits = Physics2D.OverlapBoxAll(cellCenter, boxSize, 0f);
    foreach (var hit in hits)
    {
        if (HasAnyTag(hit.transform, obstacleTags))
            return true;
    }
    
    // 2. 新增：检测无碰撞体的树苗（Stage 0）
    if (HasTreeAtPosition(cellCenter, 0.5f))
        return true;
    
    // 3. 新增：检测无碰撞体的箱子（如果有的话）
    if (HasChestAtPosition(cellCenter, 0.5f))
        return true;
    
    return false;
}

/// <summary>
/// 检测指定位置是否有箱子
/// </summary>
public bool HasChestAtPosition(Vector3 center, float checkRadius)
{
    // 方法1：使用 Physics2D 检测有碰撞体的箱子
    Collider2D[] hits = Physics2D.OverlapCircleAll(center, checkRadius);
    foreach (var hit in hits)
    {
        var chestController = hit.GetComponentInParent<ChestController>();
        if (chestController != null)
            return true;
    }
    
    // 方法2：遍历场景中所有 ChestController（如果箱子可能没有碰撞体）
    var allChests = Object.FindObjectsByType<ChestController>(FindObjectsSortMode.None);
    foreach (var chest in allChests)
    {
        Vector3 chestPos = chest.transform.position;
        float distance = Vector2.Distance(
            new Vector2(center.x, center.y),
            new Vector2(chestPos.x, chestPos.y)
        );
        
        if (distance < checkRadius)
            return true;
    }
    
    return false;
}
```

**方案 B：引入 PlacedItemRegistry（更优雅但更复杂）**

创建一个统一的注册表，所有放置的物品都注册到这个表中：

```csharp
public class PlacedItemRegistry : MonoBehaviour
{
    public static PlacedItemRegistry Instance { get; private set; }
    
    private Dictionary<Vector2Int, List<IPlacedItem>> placedItems = new();
    
    public void Register(IPlacedItem item, Vector2Int cellIndex) { ... }
    public void Unregister(IPlacedItem item) { ... }
    public bool HasItemAt(Vector2Int cellIndex) { ... }
    public List<IPlacedItem> GetItemsAt(Vector2Int cellIndex) { ... }
}
```

**推荐方案**：先使用方案 A（快速修复），后续可以考虑方案 B（架构优化）。

### 2.3 修改 UpdatePreview()

之前的修改只对 SaplingData 做了特殊处理，现在需要让所有物品都使用统一的检测逻辑：

```csharp
private void UpdatePreview()
{
    // ... 获取鼠标位置、更新预览位置 ...
    
    // 验证格子状态
    Vector3 previewPos = placementPreview.GetPreviewPosition();
    Vector2Int gridSize = placementPreview.GridSize;
    
    // ★ 统一使用 ValidateCells()，但 HasObstacle() 已经增强
    currentCellStates = validator.ValidateCells(previewPos, gridSize, playerTransform);
    
    // ★ 树苗额外检测（季节、耕地、边距）
    if (currentPlacementItem is SaplingData saplingData)
    {
        // 如果基础验证通过，再做树苗专用检测
        if (validator.AreAllCellsValid(currentCellStates))
        {
            var saplingState = validator.ValidateSaplingSpecificRules(saplingData, previewPos, playerTransform);
            if (!saplingState.isValid)
            {
                currentCellStates = new List<CellStateV3> { saplingState };
            }
        }
    }
    
    placementPreview.UpdateCellStates(currentCellStates);
}
```

---

## 三、P2 修复：格子中心计算

### 3.1 问题根因

**当前代码**（`PlacementGridCalculator.GetOccupiedCellCenters()`）：
```csharp
float startX = center.x - (gridSize.x - 1) * 0.5f;
float startY = center.y - (gridSize.y - 1) * 0.5f;

for (int x = 0; x < gridSize.x; x++)
{
    for (int y = 0; y < gridSize.y; y++)
    {
        Vector3 cellCenter = new Vector3(
            startX + x,  // ← 问题在这里！
            startY + y,
            center.z
        );
        cells.Add(cellCenter);
    }
}
```

**问题分析**：
- 假设 `center = (1.5, 2.5)`（鼠标所在格子的中心）
- 对于 1x1 物品：`startX = 1.5 - 0 = 1.5`，格子中心 = (1.5, 2.5) ✅
- 对于 2x1 物品：`startX = 1.5 - 0.5 = 1.0`，两个格子中心 = (1.0, 2.5) 和 (2.0, 2.5) ❌

**正确的格子中心应该是**：
- 方案 A：(0.5, 2.5) 和 (1.5, 2.5) - 以鼠标所在格子为右侧格子
- 方案 B：(1.5, 2.5) 和 (2.5, 2.5) - 以鼠标所在格子为左侧格子

### 3.2 修复方案

**核心思路**：确保所有格子中心都在整数+0.5的位置。

```csharp
public static List<Vector3> GetOccupiedCellCenters(Vector3 center, Vector2Int gridSize)
{
    var cells = new List<Vector3>();
    
    // 计算鼠标所在格子的索引
    int centerCellX = Mathf.FloorToInt(center.x);
    int centerCellY = Mathf.FloorToInt(center.y);
    
    // 计算起始格子索引（使格子以鼠标所在格子为中心/锚点）
    // 对于 1x1：startX = centerCellX
    // 对于 2x1：startX = centerCellX - 0 = centerCellX（以鼠标所在格子为左侧格子）
    // 对于 3x1：startX = centerCellX - 1（以鼠标所在格子为中间格子）
    int startX = centerCellX - (gridSize.x - 1) / 2;
    int startY = centerCellY - (gridSize.y - 1) / 2;
    
    for (int x = 0; x < gridSize.x; x++)
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            // ★ 格子中心 = 格子索引 + 0.5
            Vector3 cellCenter = new Vector3(
                startX + x + 0.5f,
                startY + y + 0.5f,
                center.z
            );
            cells.Add(cellCenter);
        }
    }
    
    return cells;
}
```

**验证**：
- 假设 `center = (1.5, 2.5)`，`gridSize = (2, 1)`
- `centerCellX = 1`, `centerCellY = 2`
- `startX = 1 - 0 = 1`, `startY = 2 - 0 = 2`
- 格子中心 = (1.5, 2.5) 和 (2.5, 2.5) ✅

### 3.3 同步修改 GetOccupiedCellIndices()

```csharp
public static List<Vector2Int> GetOccupiedCellIndices(Vector3 center, Vector2Int gridSize)
{
    var indices = new List<Vector2Int>();
    
    // 计算鼠标所在格子的索引
    int centerCellX = Mathf.FloorToInt(center.x);
    int centerCellY = Mathf.FloorToInt(center.y);
    
    // 计算起始格子索引
    int startX = centerCellX - (gridSize.x - 1) / 2;
    int startY = centerCellY - (gridSize.y - 1) / 2;
    
    for (int x = 0; x < gridSize.x; x++)
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            indices.Add(new Vector2Int(startX + x, startY + y));
        }
    }
    
    return indices;
}
```

---

## 四、代码修改清单

### 4.1 PlacementValidatorV3.cs

| 方法 | 修改内容 |
|------|---------|
| `HasObstacle()` | 添加无碰撞体物品检测（树苗、箱子） |
| 新增 `HasChestAtPosition()` | 检测指定位置是否有箱子 |
| 新增 `ValidateSaplingSpecificRules()` | 树苗专用规则（季节、耕地、边距），从 `ValidateSaplingPlacement()` 拆分 |

### 4.2 PlacementGridCalculator.cs

| 方法 | 修改内容 |
|------|---------|
| `GetOccupiedCellCenters()` | 修复格子中心计算，确保在整数+0.5位置 |
| `GetOccupiedCellIndices()` | 同步修改，保持一致 |

### 4.3 PlacementManagerV3.cs

| 方法 | 修改内容 |
|------|---------|
| `UpdatePreview()` | 调整验证逻辑，统一使用增强后的 `ValidateCells()` |

---

## 五、测试场景

### 5.1 P1 测试：障碍物检测

| 场景 | 预期结果 |
|------|---------|
| 在空地放置树苗 | 预览框绿色，可放置 |
| 在已有树苗（Stage 0）位置放置树苗 | 预览框红色，不可放置 |
| 在已有树苗（Stage 0）位置放置箱子 | 预览框红色，不可放置 |
| 在已有箱子位置放置树苗 | 预览框红色，不可放置 |
| 在已有箱子位置放置箱子 | 预览框红色，不可放置 |
| 在已有树木（Stage 1+）位置放置任何物品 | 预览框红色，不可放置 |

### 5.2 P2 测试：格子对齐

| 场景 | 预期结果 |
|------|---------|
| 放置 1x1 物品（树苗） | 预览格子中心在 (n+0.5, m+0.5) |
| 放置 2x1 物品（大箱子） | 两个预览格子中心在 (n+0.5, m+0.5) 和 (n+1.5, m+0.5) |
| 1x1 和 2x1 物品相邻放置 | 格子边界完全对齐 |
| 预览与放置位置对比 | Collider 中心与预览格子几何中心一致 |

---

## 六、风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| `FindObjectsByType` 性能问题 | 中 | 只在验证时调用，不在 Update 中频繁调用 |
| 格子计算修改影响现有放置物品 | 低 | 只影响新放置的物品，已放置物品不受影响 |
| 箱子检测可能遗漏某些情况 | 低 | 箱子通常有碰撞体，Physics2D 检测应该能覆盖 |

---

## 七、版本历史

| 版本 | 日期 | 变更内容 |
|------|------|---------|
| 1.0 | 2026-01-21 | 初始版本（存在设计缺陷） |
| 2.0 | 2026-01-22 | 重新设计，修正 P1 和 P2 的修复方案 |

---

**文档维护者**: Kiro  
**最后更新**: 2026-01-22
