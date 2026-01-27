using UnityEngine;
using System.Collections.Generic;
using FarmGame.Data;

/// <summary>
/// 放置预览组件 V3
/// 支持位置锁定功能：点击后预览 UI 固定在目标位置，不再跟随鼠标
/// 
/// 状态：
/// - 跟随模式：预览 UI 跟随鼠标移动
/// - 锁定模式：预览 UI 固定在锁定位置
/// </summary>
public class PlacementPreviewV3 : MonoBehaviour
{
    #region 序列化字段
    
    [Header("━━━━ 预览设置 ━━━━")]
    [Tooltip("物品预览 SpriteRenderer")]
    [SerializeField] private SpriteRenderer itemPreviewRenderer;
    
    [Tooltip("格子容器")]
    [SerializeField] private Transform gridContainer;
    
    [Header("━━━━ 颜色配置 ━━━━")]
    [Tooltip("有效位置颜色（绿色）")]
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.6f);
    
    [Tooltip("无效位置颜色（红色）")]
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.6f);
    
    [Tooltip("物品预览透明度")]
    [Range(0.1f, 1f)]
    [SerializeField] private float itemPreviewAlpha = 0.8f;
    
    [Header("━━━━ 调试 ━━━━")]
    [SerializeField] private bool showDebugInfo = true; // 临时开启调试
    
    #endregion
    
    #region 私有字段
    
    private ItemData currentItem;
    private Vector2Int currentGridSize = Vector2Int.one;
    private List<PlacementGridCell> gridCells = new List<PlacementGridCell>();
    private List<PlacementGridCell> cellPool = new List<PlacementGridCell>();
    private bool isAllValid = true;
    
    // 位置锁定相关
    private bool isLocked = false;
    private Vector3 lockedPosition;
    
    #endregion
    
    #region 属性
    
    /// <summary>当前是否所有格子都有效</summary>
    public bool IsAllValid => isAllValid;
    
    /// <summary>当前预览位置</summary>
    public Vector3 PreviewPosition => transform.position;
    
    /// <summary>当前格子大小</summary>
    public Vector2Int GridSize => currentGridSize;
    
    /// <summary>是否处于锁定状态</summary>
    public bool IsLocked => isLocked;
    
    /// <summary>锁定的位置</summary>
    public Vector3 LockedPosition => lockedPosition;
    
    #endregion
    
    #region Unity 生命周期
    
    private void Awake()
    {
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlacementPreviewV3] Awake() 开始初始化</color>");
        
        // 创建物品预览渲染器
        if (itemPreviewRenderer == null)
        {
            GameObject previewObj = new GameObject("ItemPreview");
            previewObj.transform.SetParent(transform);
            previewObj.transform.localPosition = Vector3.zero;
            itemPreviewRenderer = previewObj.AddComponent<SpriteRenderer>();
            // 使用高 Sorting Order 确保预览在最上层
            itemPreviewRenderer.sortingLayerName = "Default";
            itemPreviewRenderer.sortingOrder = 32000; // 非常高的值确保在最上层
            
            if (showDebugInfo)
                Debug.Log($"<color=green>[PlacementPreviewV3] 创建 ItemPreview SpriteRenderer, Order=32000</color>");
        }
        
        // 创建格子容器
        if (gridContainer == null)
        {
            GameObject containerObj = new GameObject("GridContainer");
            containerObj.transform.SetParent(transform);
            containerObj.transform.localPosition = Vector3.zero;
            gridContainer = containerObj.transform;
            
            if (showDebugInfo)
                Debug.Log($"<color=green>[PlacementPreviewV3] 创建 GridContainer</color>");
        }
        
        // 初始隐藏
        Hide();
        
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlacementPreviewV3] Awake() 完成，初始隐藏</color>");
    }
    
    #endregion
    
    #region 公共方法 - 显示/隐藏
    
    /// <summary>
    /// 显示预览
    /// </summary>
    public void Show(ItemData item, Vector2Int gridSize)
    {
        Debug.Log($"<color=cyan>[PlacementPreviewV3] Show 被调用: {item?.itemName ?? "null"}, gridSize={gridSize}</color>");
        
        if (item == null) return;
        
        currentItem = item;
        currentGridSize = gridSize;
        isLocked = false;
        
        gameObject.SetActive(true);
        Debug.Log($"<color=cyan>[PlacementPreviewV3] gameObject.SetActive(true), activeSelf={gameObject.activeSelf}</color>");
        
        // ★ 优先使用预制体 Sprite，使预览与实际放置一致
        bool spriteSet = false;
        
        if (item.placementPrefab != null)
        {
            var prefabSR = item.placementPrefab.GetComponentInChildren<SpriteRenderer>();
            if (prefabSR != null && prefabSR.sprite != null)
            {
                itemPreviewRenderer.sprite = prefabSR.sprite;
                
                // ★ 计算预览 Sprite 位置，考虑底部对齐的影响
                // 使放置后 Collider 中心对齐到格子中心
                Vector3 spriteLocalPos = PlacementGridCalculator.GetPreviewSpriteLocalPosition(item.placementPrefab);
                itemPreviewRenderer.transform.localPosition = spriteLocalPos;
                
                Color previewColor = Color.white;
                previewColor.a = itemPreviewAlpha;
                itemPreviewRenderer.color = previewColor;
                
                spriteSet = true;
                Debug.Log($"<color=cyan>[PlacementPreviewV3] 使用预制体 Sprite: {prefabSR.sprite.name}, localPos={spriteLocalPos}</color>");
            }
        }
        
        // 回退到 icon
        if (!spriteSet && item.icon != null)
        {
            itemPreviewRenderer.sprite = item.icon;
            itemPreviewRenderer.transform.localPosition = Vector3.zero;
            Color previewColor = Color.white;
            previewColor.a = itemPreviewAlpha;
            itemPreviewRenderer.color = previewColor;
            Debug.Log($"<color=cyan>[PlacementPreviewV3] 回退使用 icon: {item.icon.name}</color>");
        }
        else if (!spriteSet)
        {
            Debug.LogWarning($"[PlacementPreviewV3] 物品 {item.itemName} 没有可用的 Sprite！");
        }
        
        // 创建格子
        CreateGridCells(gridSize);
        Debug.Log($"<color=green>[PlacementPreviewV3] 创建了 {gridCells.Count} 个格子</color>");
    }
    
    /// <summary>
    /// 显示预览（自动计算格子大小）
    /// </summary>
    public void Show(ItemData item)
    {
        Vector2Int gridSize = Vector2Int.one;
        
        if (item.placementPrefab != null)
        {
            gridSize = PlacementGridCalculator.GetRequiredGridSize(item.placementPrefab);
        }
        
        Show(item, gridSize);
    }
    
    /// <summary>
    /// 隐藏预览
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        currentItem = null;
        isLocked = false;
        
        // 回收所有格子到池
        foreach (var cell in gridCells)
        {
            cell.Hide();
            cellPool.Add(cell);
        }
        gridCells.Clear();
        
        // ✅ 清除预览遮挡检测
        if (OcclusionManager.Instance != null)
        {
            OcclusionManager.Instance.SetPreviewBounds(null);
        }
    }
    
    #endregion
    
    #region 公共方法 - 位置更新
    
    /// <summary>
    /// 更新预览位置（使用方块中心）
    /// 注意：如果处于锁定状态，此方法不会更新位置
    /// </summary>
    public void UpdatePosition(Vector3 worldPosition)
    {
        // 锁定状态下不更新位置
        if (isLocked) return;
        
        // 计算方块中心
        Vector3 cellCenter = PlacementGridCalculator.GetCellCenter(worldPosition);
        transform.position = cellCenter;
        
        // 更新格子位置
        UpdateGridCellPositions();
        
        // ✅ 更新预览遮挡检测
        NotifyOcclusionSystem();
    }
    
    /// <summary>
    /// 强制更新位置（忽略锁定状态）
    /// </summary>
    public void ForceUpdatePosition(Vector3 worldPosition)
    {
        Vector3 cellCenter = PlacementGridCalculator.GetCellCenter(worldPosition);
        transform.position = cellCenter;
        UpdateGridCellPositions();
        
        // ✅ 更新预览遮挡检测
        NotifyOcclusionSystem();
    }
    
    #endregion
    
    #region 公共方法 - 位置锁定
    
    /// <summary>
    /// 锁定当前位置
    /// 锁定后预览 UI 不再跟随鼠标
    /// </summary>
    public void LockPosition()
    {
        isLocked = true;
        lockedPosition = transform.position;
        
        if (showDebugInfo)
            Debug.Log($"<color=green>[PlacementPreviewV3] 位置已锁定: {lockedPosition}</color>");
    }
    
    /// <summary>
    /// 解锁位置
    /// 解锁后预览 UI 恢复跟随鼠标
    /// </summary>
    public void UnlockPosition()
    {
        isLocked = false;
        
        if (showDebugInfo)
            Debug.Log($"<color=yellow>[PlacementPreviewV3] 位置已解锁</color>");
    }
    
    #endregion
    
    #region 公共方法 - 格子状态
    
    /// <summary>
    /// 更新格子状态（使用 V3 格子状态）
    /// </summary>
    public void UpdateCellStates(List<CellStateV3> cellStates)
    {
        isAllValid = true;
        
        for (int i = 0; i < gridCells.Count && i < cellStates.Count; i++)
        {
            gridCells[i].SetValid(cellStates[i].isValid);
            
            if (!cellStates[i].isValid)
            {
                isAllValid = false;
            }
        }
        
        UpdateItemPreviewColor();
    }
    
    /// <summary>
    /// 设置所有格子为有效/无效
    /// </summary>
    public void SetAllCellsValid(bool valid)
    {
        isAllValid = valid;
        
        foreach (var cell in gridCells)
        {
            cell.SetValid(valid);
        }
        
        UpdateItemPreviewColor();
    }
    
    #endregion
    
    #region 公共方法 - Bounds 获取
    
    /// <summary>
    /// 获取预览格子的 Bounds
    /// 用于导航目标点计算和到达检测
    /// </summary>
    public Bounds GetPreviewBounds()
    {
        if (gridCells.Count == 0)
        {
            // 默认 1x1 格子
            return new Bounds(transform.position, Vector3.one);
        }
        
        // 计算所有格子的联合 Bounds
        Bounds bounds = new Bounds(gridCells[0].transform.position, Vector3.one);
        
        for (int i = 1; i < gridCells.Count; i++)
        {
            Bounds cellBounds = new Bounds(gridCells[i].transform.position, Vector3.one);
            bounds.Encapsulate(cellBounds);
        }
        
        return bounds;
    }
    
    /// <summary>
    /// 获取当前预览位置
    /// </summary>
    public Vector3 GetPreviewPosition()
    {
        return isLocked ? lockedPosition : transform.position;
    }
    
    /// <summary>
    /// 获取所有格子的世界坐标
    /// </summary>
    public List<Vector3> GetAllCellPositions()
    {
        var positions = new List<Vector3>();
        foreach (var cell in gridCells)
        {
            positions.Add(cell.transform.position);
        }
        return positions;
    }
    
    #endregion
    
    #region 公共方法 - Sorting Layer
    
    /// <summary>
    /// 更新预览的 Sorting Layer（与玩家一致）
    /// </summary>
    public void UpdateSortingLayer(string sortingLayerName)
    {
        if (itemPreviewRenderer != null)
        {
            itemPreviewRenderer.sortingLayerName = sortingLayerName;
            itemPreviewRenderer.sortingOrder = 32000; // 非常高的值确保在最上层
        }
        
        foreach (var cell in gridCells)
        {
            var sr = cell.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = 31999; // 比物品预览低 1
            }
        }
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 创建格子
    /// </summary>
    private void CreateGridCells(Vector2Int gridSize)
    {
        // 回收现有格子
        foreach (var cell in gridCells)
        {
            cell.Hide();
            cellPool.Add(cell);
        }
        gridCells.Clear();
        
        // 计算需要的格子数量
        int totalCells = gridSize.x * gridSize.y;
        
        // 从池中获取或创建格子
        for (int i = 0; i < totalCells; i++)
        {
            PlacementGridCell cell;
            
            if (cellPool.Count > 0)
            {
                cell = cellPool[cellPool.Count - 1];
                cellPool.RemoveAt(cellPool.Count - 1);
            }
            else
            {
                cell = PlacementGridCell.Create(gridContainer);
            }
            
            cell.SetColors(validColor, invalidColor);
            cell.Show();
            gridCells.Add(cell);
        }
        
        // 更新格子位置
        UpdateGridCellPositions();
    }
    
    /// <summary>
    /// 更新格子位置
    /// </summary>
    private void UpdateGridCellPositions()
    {
        if (gridCells.Count == 0) return;
        
        Vector3 center = transform.position;
        var cellCenters = PlacementGridCalculator.GetOccupiedCellCenters(center, currentGridSize);
        var cellIndices = PlacementGridCalculator.GetOccupiedCellIndices(center, currentGridSize);
        
        for (int i = 0; i < gridCells.Count && i < cellCenters.Count; i++)
        {
            gridCells[i].Initialize(cellIndices[i], cellCenters[i]);
        }
    }
    
    /// <summary>
    /// 更新物品预览颜色
    /// </summary>
    private void UpdateItemPreviewColor()
    {
        if (itemPreviewRenderer == null) return;
        
        Color color = isAllValid ? Color.white : new Color(1f, 0.5f, 0.5f);
        color.a = itemPreviewAlpha;
        itemPreviewRenderer.color = color;
    }
    
    /// <summary>
    /// ✅ 通知遮挡系统更新预览 Bounds
    /// </summary>
    private void NotifyOcclusionSystem()
    {
        if (OcclusionManager.Instance == null) return;
        if (!gameObject.activeSelf) return;
        
        // 计算预览的 Bounds（使用所有格子的联合 Bounds）
        Bounds previewBounds = GetPreviewBounds();
        
        // 如果有物品预览 Sprite，扩展 Bounds 以包含 Sprite
        if (itemPreviewRenderer != null && itemPreviewRenderer.sprite != null)
        {
            Bounds spriteBounds = itemPreviewRenderer.bounds;
            previewBounds.Encapsulate(spriteBounds);
        }
        
        OcclusionManager.Instance.SetPreviewBounds(previewBounds);
    }
    
    #endregion
}
