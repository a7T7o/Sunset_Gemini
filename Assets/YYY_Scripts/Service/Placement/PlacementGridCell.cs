using UnityEngine;

/// <summary>
/// 放置格子单元
/// 显示单个 1x1 格子的预览状态（绿色/红色）
/// </summary>
public class PlacementGridCell : MonoBehaviour
{
    #region 序列化字段
    
    [Header("━━━━ 渲染设置 ━━━━")]
    [Tooltip("格子 SpriteRenderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("━━━━ 颜色配置 ━━━━")]
    [Tooltip("有效位置颜色（绿色）")]
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.4f);
    
    [Tooltip("无效位置颜色（红色）")]
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.4f);
    
    #endregion
    
    #region 私有字段
    
    private bool isValid = true;
    private Vector2Int cellIndex;
    
    #endregion
    
    #region 属性
    
    /// <summary>当前是否有效</summary>
    public bool IsValid => isValid;
    
    /// <summary>格子索引</summary>
    public Vector2Int CellIndex => cellIndex;
    
    #endregion
    
    #region Unity 生命周期
    
    private void Awake()
    {
        // 确保有 SpriteRenderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
        
        // 设置排序层 - 使用高 Order 确保在最上层
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = 31999; // 比物品预览低 1
        
        // 创建默认 Sprite（如果没有）
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateGridSprite();
        }
        
        // 设置默认大小为 1x1
        transform.localScale = Vector3.one;
        
        Debug.Log($"<color=green>[PlacementGridCell] 格子创建完成, Order=31999</color>");
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 初始化格子
    /// </summary>
    /// <param name="index">格子索引</param>
    /// <param name="worldPosition">世界坐标</param>
    public void Initialize(Vector2Int index, Vector3 worldPosition)
    {
        cellIndex = index;
        transform.position = worldPosition;
        SetValid(true);
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 设置格子状态
    /// </summary>
    /// <param name="valid">true=绿色（可放置），false=红色（被遮挡）</param>
    public void SetValid(bool valid)
    {
        isValid = valid;
        UpdateColor();
    }
    
    /// <summary>
    /// 设置格子位置
    /// </summary>
    public void SetPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }
    
    /// <summary>
    /// 设置颜色配置
    /// </summary>
    public void SetColors(Color valid, Color invalid)
    {
        validColor = valid;
        invalidColor = invalid;
        UpdateColor();
    }
    
    /// <summary>
    /// 隐藏格子
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 显示格子
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 更新颜色
    /// </summary>
    private void UpdateColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isValid ? validColor : invalidColor;
        }
    }
    
    /// <summary>
    /// 创建格子 Sprite（1x1 方框）
    /// </summary>
    private Sprite CreateGridSprite()
    {
        int size = 32;
        int borderWidth = 2;
        
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;
        
        Color[] colors = new Color[size * size];
        Color fillColor = Color.white;
        Color borderColor = new Color(1f, 1f, 1f, 0.8f);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 边框
                bool isBorder = x < borderWidth || x >= size - borderWidth ||
                               y < borderWidth || y >= size - borderWidth;
                
                if (isBorder)
                {
                    colors[y * size + x] = borderColor;
                }
                else
                {
                    // 内部填充（半透明）
                    colors[y * size + x] = new Color(1f, 1f, 1f, 0.3f);
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        // 创建 Sprite，PPU = size 使其为 1x1 单位
        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
    }
    
    #endregion
    
    #region 静态工厂方法
    
    /// <summary>
    /// 创建格子实例
    /// </summary>
    public static PlacementGridCell Create(Transform parent = null)
    {
        GameObject cellObj = new GameObject("GridCell");
        if (parent != null)
        {
            cellObj.transform.SetParent(parent);
        }
        
        var cell = cellObj.AddComponent<PlacementGridCell>();
        return cell;
    }
    
    #endregion
}
