using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 放置网格计算器
/// 负责计算方块中心、格子数量和占用格子位置
/// </summary>
public static class PlacementGridCalculator
{
    #region 方块中心计算
    
    /// <summary>
    /// 计算方块中心坐标
    /// 鼠标所在方块的中心 = Floor(pos) + 0.5
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>方块中心坐标</returns>
    public static Vector3 GetCellCenter(Vector3 worldPosition)
    {
        return new Vector3(
            Mathf.Floor(worldPosition.x) + 0.5f,
            Mathf.Floor(worldPosition.y) + 0.5f,
            worldPosition.z
        );
    }
    
    /// <summary>
    /// 计算方块中心坐标（2D版本）
    /// </summary>
    public static Vector2 GetCellCenter(Vector2 worldPosition)
    {
        return new Vector2(
            Mathf.Floor(worldPosition.x) + 0.5f,
            Mathf.Floor(worldPosition.y) + 0.5f
        );
    }
    
    /// <summary>
    /// 获取坐标所在的格子索引
    /// </summary>
    public static Vector2Int GetCellIndex(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x),
            Mathf.FloorToInt(worldPosition.y)
        );
    }
    
    #endregion
    
    #region 格子大小计算
    
    /// <summary>
    /// 计算物品占用的格子数量（基于 Collider）
    /// 使用向上取整，确保格子能完全包裹 Collider
    /// </summary>
    /// <param name="colliderBounds">Collider 的边界</param>
    /// <returns>格子数量 (宽, 高)</returns>
    public static Vector2Int GetRequiredGridSize(Bounds colliderBounds)
    {
        int width = Mathf.CeilToInt(colliderBounds.size.x);
        int height = Mathf.CeilToInt(colliderBounds.size.y);
        
        // 最小为 1x1
        return new Vector2Int(
            Mathf.Max(1, width),
            Mathf.Max(1, height)
        );
    }
    
    /// <summary>
    /// 计算物品占用的格子数量（基于 Collider2D）
    /// </summary>
    public static Vector2Int GetRequiredGridSize(Collider2D collider)
    {
        if (collider == null)
            return Vector2Int.one;
        
        return GetRequiredGridSize(collider.bounds);
    }
    
    /// <summary>
    /// 计算物品占用的格子数量（基于预制体）
    /// 修改：优先使用 GetRequiredGridSizeFromPrefab 从 Collider 路径计算
    /// </summary>
    public static Vector2Int GetRequiredGridSize(GameObject prefab)
    {
        // 使用新方法从 Collider 路径计算，避免 bounds 在未实例化预制体上的问题
        return GetRequiredGridSizeFromPrefab(prefab);
    }
    
    #endregion
    
    #region 占用格子计算
    
    /// <summary>
    /// 获取所有占用的格子位置（世界坐标）
    /// 修复：确保所有格子中心都在整数+0.5的位置
    /// </summary>
    /// <param name="center">放置中心点（鼠标所在格子的中心）</param>
    /// <param name="gridSize">格子数量</param>
    /// <returns>所有格子的中心坐标列表</returns>
    public static List<Vector3> GetOccupiedCellCenters(Vector3 center, Vector2Int gridSize)
    {
        var cells = new List<Vector3>();
        
        // 计算鼠标所在格子的索引
        int centerCellX = Mathf.FloorToInt(center.x);
        int centerCellY = Mathf.FloorToInt(center.y);
        
        // 计算起始格子索引（使格子以鼠标所在格子为锚点）
        // 对于 1x1：startX = centerCellX
        // 对于 2x1：startX = centerCellX（以鼠标所在格子为左侧格子）
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
    
    /// <summary>
    /// 获取所有占用的格子索引
    /// 修复：与 GetOccupiedCellCenters() 保持一致的计算逻辑
    /// </summary>
    /// <param name="center">放置中心点（鼠标所在格子的中心）</param>
    /// <param name="gridSize">格子数量</param>
    /// <returns>所有格子的索引列表</returns>
    public static List<Vector2Int> GetOccupiedCellIndices(Vector3 center, Vector2Int gridSize)
    {
        var indices = new List<Vector2Int>();
        
        // 计算鼠标所在格子的索引
        int centerCellX = Mathf.FloorToInt(center.x);
        int centerCellY = Mathf.FloorToInt(center.y);
        
        // 计算起始格子索引（与 GetOccupiedCellCenters 保持一致）
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
    
    #endregion
    
    #region PolygonCollider2D 计算方法
    
    /// <summary>
    /// 从 PolygonCollider2D 计算本地空间边界
    /// </summary>
    /// <param name="collider">PolygonCollider2D 组件</param>
    /// <returns>本地空间的最小和最大点</returns>
    private static (Vector2 min, Vector2 max) GetPolygonColliderLocalBounds(PolygonCollider2D collider)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        for (int i = 0; i < collider.pathCount; i++)
        {
            Vector2[] path = collider.GetPath(i);
            foreach (var point in path)
            {
                // 考虑 offset
                Vector2 localPoint = point + collider.offset;
                minX = Mathf.Min(minX, localPoint.x);
                maxX = Mathf.Max(maxX, localPoint.x);
                minY = Mathf.Min(minY, localPoint.y);
                maxY = Mathf.Max(maxY, localPoint.y);
            }
        }
        
        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }
    
    /// <summary>
    /// 从 PolygonCollider2D 计算格子大小
    /// </summary>
    private static Vector2Int GetGridSizeFromPolygonCollider(PolygonCollider2D collider)
    {
        var (min, max) = GetPolygonColliderLocalBounds(collider);
        
        float width = max.x - min.x;
        float height = max.y - min.y;
        
        return new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(width)),
            Mathf.Max(1, Mathf.CeilToInt(height))
        );
    }
    
    /// <summary>
    /// 从 BoxCollider2D 计算格子大小
    /// </summary>
    private static Vector2Int GetGridSizeFromBoxCollider(BoxCollider2D collider)
    {
        return new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(collider.size.x)),
            Mathf.Max(1, Mathf.CeilToInt(collider.size.y))
        );
    }
    
    /// <summary>
    /// 从预制体的 Collider 计算格子大小（正确处理本地空间）
    /// 优先使用 PolygonCollider2D，其次 BoxCollider2D，最后 Sprite
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <returns>格子数量 (宽, 高)</returns>
    public static Vector2Int GetRequiredGridSizeFromPrefab(GameObject prefab)
    {
        if (prefab == null) return Vector2Int.one;
        
        // 1. 尝试 PolygonCollider2D
        var polyCollider = prefab.GetComponentInChildren<PolygonCollider2D>();
        if (polyCollider != null && polyCollider.pathCount > 0)
        {
            return GetGridSizeFromPolygonCollider(polyCollider);
        }
        
        // 2. 尝试 BoxCollider2D
        var boxCollider = prefab.GetComponentInChildren<BoxCollider2D>();
        if (boxCollider != null)
        {
            return GetGridSizeFromBoxCollider(boxCollider);
        }
        
        // 3. 回退到 Sprite
        var spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            return new Vector2Int(
                Mathf.Max(1, Mathf.CeilToInt(spriteBounds.size.x)),
                Mathf.Max(1, Mathf.CeilToInt(spriteBounds.size.y))
            );
        }
        
        // 4. 默认 1x1
        return Vector2Int.one;
    }
    
    /// <summary>
    /// 获取预制体 Collider 的本地空间几何中心
    /// 这个中心点将作为放置时的锚点
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <returns>Collider 的本地空间几何中心</returns>
    public static Vector2 GetColliderLocalCenter(GameObject prefab)
    {
        if (prefab == null) return Vector2.zero;
        
        // 1. 尝试 PolygonCollider2D
        var polyCollider = prefab.GetComponentInChildren<PolygonCollider2D>();
        if (polyCollider != null && polyCollider.pathCount > 0)
        {
            var (min, max) = GetPolygonColliderLocalBounds(polyCollider);
            return new Vector2((min.x + max.x) / 2f, (min.y + max.y) / 2f);
        }
        
        // 2. 尝试 BoxCollider2D
        var boxCollider = prefab.GetComponentInChildren<BoxCollider2D>();
        if (boxCollider != null)
        {
            return boxCollider.offset;
        }
        
        // 3. 默认返回零点
        return Vector2.zero;
    }
    
    /// <summary>
    /// 计算底部对齐后 Collider 中心相对于物品原点的位置
    /// 这是预览和放置都需要使用的核心计算
    /// 
    /// 原理：TreeController 和 ChestController 在 Awake/Start 时会执行底部对齐
    /// 底部对齐会将 Sprite 的 localPosition.y 设置为 -sprite.bounds.min.y
    /// 这会改变 Collider 相对于物品原点的位置
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <returns>底部对齐后 Collider 中心相对于物品原点的位置</returns>
    public static Vector2 GetColliderCenterAfterBottomAlign(GameObject prefab)
    {
        if (prefab == null) return Vector2.zero;
        
        var sr = prefab.GetComponentInChildren<SpriteRenderer>();
        
        // 如果没有 SpriteRenderer，不会执行底部对齐，回退到原始 Collider 中心
        if (sr == null || sr.sprite == null)
        {
            return GetColliderLocalCenter(prefab);
        }
        
        // 计算底部对齐偏移：-sprite.bounds.min.y
        float bottomAlignOffset = -sr.sprite.bounds.min.y;
        
        // 获取原始 Collider 中心
        Vector2 colliderCenter = GetColliderLocalCenter(prefab);
        
        // 底部对齐后，Collider 中心相对于物品原点的位置
        // Y 坐标需要加上底部对齐偏移
        return new Vector2(colliderCenter.x, colliderCenter.y + bottomAlignOffset);
    }
    
    /// <summary>
    /// 计算预览 Sprite 的 localPosition
    /// 使放置后 Collider 中心对齐到格子中心
    /// 
    /// 注意：此方法假设 PlacementPreviewV3.transform.position 是鼠标所在格子的中心
    /// 对于多格子物品，需要额外计算格子几何中心的偏移
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <returns>预览 Sprite 应该设置的 localPosition</returns>
    public static Vector3 GetPreviewSpriteLocalPosition(GameObject prefab)
    {
        if (prefab == null) return Vector3.zero;
        
        // 获取格子大小
        Vector2Int gridSize = GetRequiredGridSizeFromPrefab(prefab);
        
        // 计算格子几何中心相对于鼠标所在格子中心的偏移
        // 对于 1x1：偏移 = (0, 0)
        // 对于 2x1：偏移 = (0.5, 0) - 因为格子从鼠标所在格子向右扩展
        // 对于 3x1：偏移 = (0, 0) - 因为格子以鼠标所在格子为中心
        // 公式：偏移 = ((gridSize - 1) - (gridSize - 1) / 2 * 2) * 0.5
        // 简化：对于偶数宽度，偏移 = 0.5；对于奇数宽度，偏移 = 0
        float gridCenterOffsetX = (gridSize.x % 2 == 0) ? 0.5f : 0f;
        float gridCenterOffsetY = (gridSize.y % 2 == 0) ? 0.5f : 0f;
        
        var sr = prefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            // 没有 Sprite，使用原始 Collider 中心的反向偏移
            Vector2 colliderCenter = GetColliderLocalCenter(prefab);
            return new Vector3(
                gridCenterOffsetX - colliderCenter.x,
                gridCenterOffsetY - colliderCenter.y,
                0
            );
        }
        
        // 计算底部对齐偏移
        float bottomAlignOffset = -sr.sprite.bounds.min.y;
        
        // 计算放置后 Collider 中心
        Vector2 finalColliderCenter = GetColliderCenterAfterBottomAlign(prefab);
        
        // 预览 Sprite 的 localPosition：
        // 1. 应用格子几何中心偏移（多格子物品需要）
        // 2. 应用底部对齐效果（Y 方向偏移）
        // 3. 使 Collider 中心对齐到格子几何中心（反向偏移）
        return new Vector3(
            gridCenterOffsetX - finalColliderCenter.x,
            gridCenterOffsetY + bottomAlignOffset - finalColliderCenter.y,
            0
        );
    }
    
    #endregion
    
    #region 放置位置计算
    
    /// <summary>
    /// 计算实际放置位置
    /// 使放置后 Collider 中心对齐到格子几何中心
    /// 
    /// 核心等式：放置后 Collider 几何中心 = 格子几何中心
    /// </summary>
    /// <param name="mouseGridCenter">鼠标所在格子的中心（PlacementPreviewV3.LockedPosition）</param>
    /// <param name="prefab">预制体</param>
    /// <returns>实际放置位置（预制体根物体的 position）</returns>
    public static Vector3 GetPlacementPosition(Vector3 mouseGridCenter, GameObject prefab)
    {
        if (prefab == null) return mouseGridCenter;
        
        // 获取格子大小
        Vector2Int gridSize = GetRequiredGridSizeFromPrefab(prefab);
        
        // 计算格子几何中心相对于鼠标所在格子中心的偏移
        float gridCenterOffsetX = (gridSize.x % 2 == 0) ? 0.5f : 0f;
        float gridCenterOffsetY = (gridSize.y % 2 == 0) ? 0.5f : 0f;
        
        // 格子几何中心的世界坐标
        Vector3 gridGeometricCenter = new Vector3(
            mouseGridCenter.x + gridCenterOffsetX,
            mouseGridCenter.y + gridCenterOffsetY,
            mouseGridCenter.z
        );
        
        // 计算放置后 Collider 中心相对于物品原点的位置
        Vector2 finalColliderCenter = GetColliderCenterAfterBottomAlign(prefab);
        
        // 放置位置 = 格子几何中心 - 放置后 Collider 中心
        // 这样放置后（底部对齐执行后）Collider 中心就会在格子几何中心
        return new Vector3(
            gridGeometricCenter.x - finalColliderCenter.x,
            gridGeometricCenter.y - finalColliderCenter.y,
            gridGeometricCenter.z
        );
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 格子索引转世界坐标（格子中心）
    /// </summary>
    public static Vector3 CellIndexToWorldCenter(Vector2Int cellIndex, float z = 0f)
    {
        return new Vector3(
            cellIndex.x + 0.5f,
            cellIndex.y + 0.5f,
            z
        );
    }
    
    /// <summary>
    /// 检查两个格子是否相邻
    /// </summary>
    public static bool AreCellsAdjacent(Vector2Int cellA, Vector2Int cellB)
    {
        int dx = Mathf.Abs(cellA.x - cellB.x);
        int dy = Mathf.Abs(cellA.y - cellB.y);
        return (dx <= 1 && dy <= 1) && (dx + dy > 0);
    }
    
    /// <summary>
    /// 计算两个格子之间的曼哈顿距离
    /// </summary>
    public static int GetManhattanDistance(Vector2Int cellA, Vector2Int cellB)
    {
        return Mathf.Abs(cellA.x - cellB.x) + Mathf.Abs(cellA.y - cellB.y);
    }
    
    #endregion
}
