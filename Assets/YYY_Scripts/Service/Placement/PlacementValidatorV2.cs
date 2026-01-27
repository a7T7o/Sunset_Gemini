using UnityEngine;
using System.Collections.Generic;
using FarmGame.Data;
using FarmGame.Events;

/// <summary>
/// 放置验证器 V2
/// 负责验证放置位置是否有效，包括格子碰撞检测和 Layer 一致性检测
/// </summary>
public class PlacementValidatorV2
{
    #region 配置参数
    
    /// <summary>玩家交互范围</summary>
    private float playerInteractionRange = 3f;
    
    /// <summary>障碍物检测标签</summary>
    private string[] obstacleTags = new string[] { "Tree", "Rock", "Building" };
    
    /// <summary>水域检测层</summary>
    private LayerMask waterLayer;
    
    /// <summary>是否启用 Layer 检测</summary>
    private bool enableLayerCheck = true;
    
    /// <summary>调试模式</summary>
    private bool showDebugInfo = false;
    
    #endregion
    
    #region 构造函数
    
    public PlacementValidatorV2(float interactionRange = 3f)
    {
        playerInteractionRange = interactionRange;
        waterLayer = LayerMask.GetMask("Water");
    }
    
    #endregion
    
    #region 主验证方法
    
    /// <summary>
    /// 完整验证放置位置
    /// </summary>
    /// <param name="item">要放置的物品</param>
    /// <param name="centerPosition">放置中心位置（方块中心）</param>
    /// <param name="playerTransform">玩家 Transform</param>
    /// <param name="cellStates">输出：每个格子的状态</param>
    /// <returns>验证结果</returns>
    public PlacementValidationResult ValidateFull(
        ItemData item,
        Vector3 centerPosition,
        Transform playerTransform,
        out List<CellState> cellStates)
    {
        cellStates = new List<CellState>();
        
        if (item == null)
            return PlacementValidationResult.Invalid(PlacementInvalidReason.InvalidTerrain, "物品数据为空");
        
        if (!item.isPlaceable)
            return PlacementValidationResult.Invalid(PlacementInvalidReason.InvalidTerrain, "该物品不可放置");
        
        // 1. 检查玩家范围
        if (!IsWithinPlayerRange(centerPosition, playerTransform))
            return PlacementValidationResult.Invalid(PlacementInvalidReason.OutOfRange, "超出放置范围");
        
        // 2. 检查 Layer 一致性
        int detectedLayer = PlacementLayerDetector.GetLayerAtPosition(centerPosition);
        if (enableLayerCheck && !PlacementLayerDetector.IsPlacementLayerValid(centerPosition, playerTransform))
        {
            return PlacementValidationResult.WithCellStates(
                false,
                PlacementInvalidReason.LayerMismatch,
                "不能在不同层级放置物品",
                cellStates,
                detectedLayer
            );
        }
        
        // 3. 获取格子大小
        Vector2Int gridSize = Vector2Int.one;
        if (item.placementPrefab != null)
        {
            gridSize = PlacementGridCalculator.GetRequiredGridSize(item.placementPrefab);
        }
        
        // 4. 检测每个格子的碰撞状态
        cellStates = DetectCellCollisions(centerPosition, gridSize, obstacleTags);
        
        // 5. 检查是否有任何格子被遮挡
        bool hasInvalidCell = false;
        foreach (var state in cellStates)
        {
            if (!state.isValid)
            {
                hasInvalidCell = true;
                break;
            }
        }
        
        if (hasInvalidCell)
        {
            return PlacementValidationResult.WithCellStates(
                false,
                PlacementInvalidReason.CollisionDetected,
                "放置区域有障碍物",
                cellStates,
                detectedLayer
            );
        }
        
        // 6. 根据放置类型进行特定验证
        var typeResult = ValidateByType(item, centerPosition);
        if (!typeResult.IsValid)
        {
            typeResult.CellStates = cellStates;
            typeResult.DetectedLayer = detectedLayer;
            return typeResult;
        }
        
        // 验证通过
        return PlacementValidationResult.WithCellStates(
            true,
            PlacementInvalidReason.None,
            string.Empty,
            cellStates,
            detectedLayer
        );
    }
    
    /// <summary>
    /// 简化验证（兼容旧接口）
    /// </summary>
    public PlacementValidationResult Validate(ItemData item, Vector3 position, Transform playerTransform)
    {
        List<CellState> cellStates;
        Vector3 centerPosition = PlacementGridCalculator.GetCellCenter(position);
        return ValidateFull(item, centerPosition, playerTransform, out cellStates);
    }
    
    #endregion
    
    #region 格子碰撞检测
    
    /// <summary>
    /// 检测每个格子的碰撞状态
    /// </summary>
    /// <param name="centerPosition">放置中心位置</param>
    /// <param name="gridSize">格子大小</param>
    /// <param name="tags">障碍物标签</param>
    /// <returns>每个格子的状态列表</returns>
    public List<CellState> DetectCellCollisions(Vector3 centerPosition, Vector2Int gridSize, string[] tags)
    {
        var cellStates = new List<CellState>();
        var cellCenters = PlacementGridCalculator.GetOccupiedCellCenters(centerPosition, gridSize);
        var cellIndices = PlacementGridCalculator.GetOccupiedCellIndices(centerPosition, gridSize);
        
        for (int i = 0; i < cellCenters.Count; i++)
        {
            Vector3 cellCenter = cellCenters[i];
            Vector2Int cellIndex = cellIndices[i];
            
            // 检测该格子是否有障碍物
            bool isValid = !HasObstacleInCell(cellCenter, tags);
            
            // 检测是否在水域
            if (isValid && IsOnWater(cellCenter))
            {
                isValid = false;
            }
            
            cellStates.Add(new CellState(cellIndex, isValid));
            
            if (showDebugInfo && !isValid)
            {
                Debug.Log($"<color=red>[PlacementValidatorV2] 格子 {cellIndex} 被遮挡</color>");
            }
        }
        
        return cellStates;
    }
    
    /// <summary>
    /// 检测单个格子是否有障碍物
    /// </summary>
    private bool HasObstacleInCell(Vector3 cellCenter, string[] tags)
    {
        if (tags == null || tags.Length == 0) return false;
        
        // 使用 OverlapBox 检测整个格子区域
        Vector2 boxSize = new Vector2(0.9f, 0.9f); // 略小于 1x1，避免边缘误检
        Collider2D[] hits = Physics2D.OverlapBoxAll(cellCenter, boxSize, 0f);
        
        foreach (var hit in hits)
        {
            if (HasAnyTag(hit.transform, tags))
            {
                return true;
            }
        }
        
        return false;
    }
    
    #endregion
    
    #region 类型特定验证
    
    /// <summary>
    /// 根据放置类型进行特定验证
    /// </summary>
    private PlacementValidationResult ValidateByType(ItemData item, Vector3 position)
    {
        switch (item.placementType)
        {
            case PlacementType.Sapling:
                return ValidateSaplingPlacement(item as SaplingData, position);
            
            case PlacementType.Building:
                return ValidateBuildingPlacement(item, position);
            
            default:
                return PlacementValidationResult.Valid();
        }
    }
    
    /// <summary>
    /// 验证树苗放置
    /// </summary>
    private PlacementValidationResult ValidateSaplingPlacement(SaplingData sapling, Vector3 position)
    {
        if (sapling == null)
            return PlacementValidationResult.Invalid(PlacementInvalidReason.InvalidTerrain, "树苗数据为空");
        
        // 检查冬季
        if (sapling.IsWinter())
            return PlacementValidationResult.Invalid(PlacementInvalidReason.WrongSeason, "冬天无法种植树木");
        
        // 检查是否在耕地上
        if (IsOnFarmland(position))
            return PlacementValidationResult.Invalid(PlacementInvalidReason.OnFarmland, "不能在耕地上种植树苗");
        
        // 获取成长边距参数
        float vMargin, hMargin;
        if (!sapling.GetStage0Margins(out vMargin, out hMargin))
        {
            vMargin = 0.2f;
            hMargin = 0.15f;
        }
        
        // 检查成长边距内是否有其他树木
        if (HasTreeInMargin(position, vMargin, hMargin))
            return PlacementValidationResult.Invalid(PlacementInvalidReason.TreeTooClose, "距离其他树木太近");
        
        return PlacementValidationResult.Valid();
    }
    
    /// <summary>
    /// 验证建筑放置
    /// </summary>
    private PlacementValidationResult ValidateBuildingPlacement(ItemData item, Vector3 position)
    {
        // 建筑验证逻辑
        return PlacementValidationResult.Valid();
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 检查是否在玩家交互范围内
    /// </summary>
    public bool IsWithinPlayerRange(Vector3 position, Transform playerTransform)
    {
        if (playerTransform == null) return false;
        
        var playerCollider = playerTransform.GetComponent<Collider2D>();
        Vector3 playerCenter = playerCollider != null
            ? playerCollider.bounds.center
            : playerTransform.position;
        
        float distance = Vector3.Distance(position, playerCenter);
        return distance <= playerInteractionRange;
    }
    
    /// <summary>
    /// 检查是否在耕地上
    /// </summary>
    public bool IsOnFarmland(Vector3 position)
    {
        // TODO: 与 FarmingSystem 集成
        return false;
    }
    
    /// <summary>
    /// 检查是否在水域
    /// </summary>
    public bool IsOnWater(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapPoint(position, waterLayer);
        return hit != null;
    }
    
    /// <summary>
    /// 检查边距内是否有其他树木
    /// </summary>
    public bool HasTreeInMargin(Vector3 center, float vMargin, float hMargin)
    {
        float maxMargin = Mathf.Max(vMargin, hMargin);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, maxMargin);
        
        foreach (var hit in hits)
        {
            var treeController = hit.GetComponentInParent<TreeControllerV2>();
            if (treeController != null)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查 Transform 或其父级是否有指定标签
    /// </summary>
    private bool HasAnyTag(Transform t, string[] tags)
    {
        Transform current = t;
        while (current != null)
        {
            foreach (var tag in tags)
            {
                if (current.CompareTag(tag))
                    return true;
            }
            current = current.parent;
        }
        return false;
    }
    
    /// <summary>
    /// 对齐到方块中心
    /// </summary>
    public Vector3 AlignToGrid(Vector3 position)
    {
        return PlacementGridCalculator.GetCellCenter(position);
    }
    
    #endregion
    
    #region 配置方法
    
    public void SetInteractionRange(float range)
    {
        playerInteractionRange = range;
    }
    
    public void SetObstacleTags(string[] tags)
    {
        obstacleTags = tags;
    }
    
    public void SetEnableLayerCheck(bool enable)
    {
        enableLayerCheck = enable;
    }
    
    public void SetDebugMode(bool debug)
    {
        showDebugInfo = debug;
    }
    
    #endregion
}
