using UnityEngine;
using System.Collections.Generic;
using FarmGame.Data;

/// <summary>
/// 放置预览组件 V2
/// 显示物品放置位置和有效性指示（格子方框 + 物品预览）
/// </summary>
public class PlacementPreviewV2 : MonoBehaviour
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
    
    #endregion
    
    #region 私有字段
    
    private ItemData currentItem;
    private Vector2Int currentGridSize = Vector2Int.one;
    private List<PlacementGridCell> gridCells = new List<PlacementGridCell>();
    private List<PlacementGridCell> cellPool = new List<PlacementGridCell>();
    private bool isAllValid = true;
    
    #endregion
    
    #region 属性
    
    /// <summary>当前是否所有格子都有效</summary>
    public bool IsAllValid => isAllValid;
    
    /// <summary>当前预览位置</summary>
    public Vector3 PreviewPosition => transform.position;
    
    /// <summary>当前格子大小</summary>
    public Vector2Int GridSize => currentGridSize;
    
    #endregion
    
    #region Unity 生命周期
    
    private void Awake()
    {
        Debug.Log($"<color=cyan>[PlacementPreviewV2] Awake() 开始初始化</color>");
        
        // 创建物品预览渲染器
        if (itemPreviewRenderer == null)
        {
            GameObject previewObj = new GameObject("ItemPreview");
            previewObj.transform.SetParent(transform);
            previewObj.transform.localPosition = Vector3.zero;
            itemPreviewRenderer = previewObj.AddComponent<SpriteRenderer>();
            // 使用玩家所在的 Sorting Layer，Order 设置为很高的值确保在最上层
            // 默认使用 "Default"，运行时会根据玩家位置动态更新
            itemPreviewRenderer.sortingLayerName = "Default";
            itemPreviewRenderer.sortingOrder = 32000; // 非常高的值确保在最上层
            Debug.Log($"<color=green>[PlacementPreviewV2] 创建 ItemPreview SpriteRenderer, Order=32000</color>");
        }
        
        // 创建格子容器
        if (gridContainer == null)
        {
            GameObject containerObj = new GameObject("GridContainer");
            containerObj.transform.SetParent(transform);
            containerObj.transform.localPosition = Vector3.zero;
            gridContainer = containerObj.transform;
            Debug.Log($"<color=green>[PlacementPreviewV2] 创建 GridContainer</color>");
        }
        
        // 初始隐藏
        Hide();
        Debug.Log($"<color=cyan>[PlacementPreviewV2] Awake() 完成，初始隐藏</color>");
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 显示预览
    /// </summary>
    /// <param name="item">要放置的物品</param>
    /// <param name="gridSize">格子大小</param>
    public void Show(ItemData item, Vector2Int gridSize)
    {
        Debug.Log($"<color=cyan>[PlacementPreviewV2] Show 被调用: {item?.itemName ?? "null"}, gridSize={gridSize}</color>");
        
        if (item == null) return;
        
        currentItem = item;
        currentGridSize = gridSize;
        
        gameObject.SetActive(true);
        Debug.Log($"<color=cyan>[PlacementPreviewV2] gameObject.SetActive(true), activeSelf={gameObject.activeSelf}</color>");
        
        // 设置物品预览 Sprite
        if (item.icon != null)
        {
            itemPreviewRenderer.sprite = item.icon;
            Color previewColor = Color.white;
            previewColor.a = itemPreviewAlpha;
            itemPreviewRenderer.color = previewColor;
            Debug.Log($"<color=cyan>[PlacementPreviewV2] 设置预览 Sprite: {item.icon.name}</color>");
        }
        else
        {
            Debug.LogWarning($"[PlacementPreviewV2] 物品 {item.itemName} 没有 icon！");
        }
        
        // 创建格子
        CreateGridCells(gridSize);
        Debug.Log($"<color=green>[PlacementPreviewV2] 创建了 {gridCells.Count} 个格子</color>");
    }
    
    /// <summary>
    /// 显示预览（自动计算格子大小）
    /// </summary>
    public void Show(ItemData item)
    {
        Vector2Int gridSize = Vector2Int.one;
        
        // 尝试从预制体获取格子大小
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
        
        // 回收所有格子到池
        foreach (var cell in gridCells)
        {
            cell.Hide();
            cellPool.Add(cell);
        }
        gridCells.Clear();
    }
    
    /// <summary>
    /// 更新预览位置（使用方块中心）
    /// </summary>
    /// <param name="worldPosition">鼠标世界坐标</param>
    public void UpdatePosition(Vector3 worldPosition)
    {
        // 计算方块中心
        Vector3 cellCenter = PlacementGridCalculator.GetCellCenter(worldPosition);
        transform.position = cellCenter;
        
        // 更新格子位置
        UpdateGridCellPositions();
    }
    
    /// <summary>
    /// 更新预览的 Sorting Layer（与玩家一致）
    /// </summary>
    /// <param name="sortingLayerName">Sorting Layer 名称</param>
    public void UpdateSortingLayer(string sortingLayerName)
    {
        if (itemPreviewRenderer != null)
        {
            itemPreviewRenderer.sortingLayerName = sortingLayerName;
        }
        
        foreach (var cell in gridCells)
        {
            var sr = cell.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = sortingLayerName;
            }
        }
    }
    
    /// <summary>
    /// 更新格子状态
    /// </summary>
    /// <param name="cellStates">每个格子的状态</param>
    public void UpdateCellStates(List<CellState> cellStates)
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
        
        // 更新物品预览颜色
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
    
    /// <summary>
    /// 获取当前预览位置
    /// </summary>
    public Vector3 GetPreviewPosition()
    {
        return transform.position;
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
    
    #endregion
}

/// <summary>
/// 格子状态
/// </summary>
public struct CellState
{
    public Vector2Int gridPosition;
    public bool isValid;
    
    public CellState(Vector2Int position, bool valid)
    {
        gridPosition = position;
        isValid = valid;
    }
}
