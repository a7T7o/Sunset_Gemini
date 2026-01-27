using UnityEngine;

/// <summary>
/// 动态排序 - 适用于多层级2D游戏（升级版 v2.0）
/// 支持精灵底部计算、自动适配Pivot、自动处理Shadow子物体
/// </summary>
public class DynamicSortingOrder : MonoBehaviour
{
    [Header("排序设置")]
    [Tooltip("Y坐标缩放倍数，数值越大排序越精确")]
    public int sortingOrderMultiplier = 100;
    
    [Tooltip("排序偏移值，用于微调显示优先级")]
    public int sortingOrderOffset = 0;
    
    [Header("计算方式")]
    [Tooltip("使用边界计算（优先Collider，回退Sprite）")]
    public bool useSpriteBounds = true;
    
    [Tooltip("底部偏移（正值=往上，负值=往下）用于微调逻辑底部位置")]
    public float bottomOffset = 0f;
    
    [Header("Shadow处理")]
    [Tooltip("自动处理名为Shadow的子物体")]
    public bool autoHandleShadow = true;
    
    [Tooltip("Shadow的Order偏移（负数表示在本体下面）")]
    public int shadowOrderOffset = -1;
    
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = false;  // ❌ 默认关闭调试
    
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer shadowRenderer;
    private string currentSortingLayer = "";
    private int lastCalculatedOrder = 0;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] 未找到SpriteRenderer组件！");
            enabled = false;
            return;
        }
        
        currentSortingLayer = spriteRenderer.sortingLayerName;
        
        // 查找Shadow子物体
        if (autoHandleShadow)
        {
            Transform shadowTransform = transform.Find("Shadow");
            if (shadowTransform != null)
            {
                shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
                if (showDebugInfo && shadowRenderer != null)
                    Debug.Log($"[{gameObject.name}] 找到Shadow子物体，将自动处理其Order");
            }
        }
        
        if (showDebugInfo)
        {
            Collider2D col = GetComponent<Collider2D>();
            Debug.Log($"[{gameObject.name}] 动态排序初始化\n" +
                     $"- 有Collider: {col != null}\n" +
                     $"- 使用边界计算: {useSpriteBounds}");
        }
    }
    
    void LateUpdate()
    {
        if (spriteRenderer == null) return;
        
        // 计算用于排序的Y坐标
        float sortingY;
        
        // ✅ 优先使用Collider2D底部（最准确！）
        Collider2D collider = GetComponent<Collider2D>();
        
        if (collider != null)
        {
            // 使用Collider底部 = 物理边界的最低点 = 玩家实际可交互位置
            sortingY = collider.bounds.min.y + bottomOffset;
        }
        else if (useSpriteBounds && spriteRenderer.sprite != null)
        {
            // 回退：使用Sprite底部
            sortingY = spriteRenderer.bounds.min.y + bottomOffset;
        }
        else
        {
            // Fallback：使用Transform位置
            sortingY = transform.position.y + bottomOffset;
        }
        
        // 计算Order：Y越小（越下面）→ Order越大 → 显示在前面
        int calculatedOrder = -Mathf.RoundToInt(sortingY * sortingOrderMultiplier) + sortingOrderOffset;
        
        // 🔍 详细调试输出（每秒输出一次）
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Collider2D col = GetComponent<Collider2D>();
            
            string debugMsg = $"<color=yellow>[{gameObject.name}] 动态排序</color>\n" +
                             $"  Transform.Y = {transform.position.y:F3}\n";
            
            if (col != null)
                debugMsg += $"  Collider.min.y = {col.bounds.min.y:F3} ✅用这个\n";
            else if (spriteRenderer.sprite != null)
                debugMsg += $"  Sprite.min.y = {spriteRenderer.bounds.min.y:F3} ✅用这个\n";
            else
                debugMsg += $"  ⚠️ 无Collider/Sprite，用Transform\n";
            
            debugMsg += $"  → SortingY = {sortingY:F3}\n" +
                       $"  → 计算 = -Round({sortingY:F3} × {sortingOrderMultiplier}) + {sortingOrderOffset}\n" +
                       $"  → Order = {calculatedOrder}";
            
            Debug.Log(debugMsg);
        }
        
        // 只在数值变化时更新（优化性能）
        if (calculatedOrder != lastCalculatedOrder || spriteRenderer.sortingLayerName != currentSortingLayer)
        {
            spriteRenderer.sortingOrder = calculatedOrder;
            lastCalculatedOrder = calculatedOrder;
            
            // 自动处理Shadow子物体
            if (shadowRenderer != null)
            {
                // Shadow跟随父物体的Sorting Layer（重要！多层游戏必须跟随）
                if (shadowRenderer.sortingLayerName != spriteRenderer.sortingLayerName)
                {
                    shadowRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
                }
                
                // Shadow的Order = 父物体Order + shadowOrderOffset
                // shadowOrderOffset通常是-1，所以Shadow永远在父物体下面
                shadowRenderer.sortingOrder = calculatedOrder + shadowOrderOffset;
            }
            
            // 检测Sorting Layer是否被外部改变（如楼梯触发器）
            if (spriteRenderer.sortingLayerName != currentSortingLayer)
            {
                currentSortingLayer = spriteRenderer.sortingLayerName;
                
                if (showDebugInfo)
                    Debug.Log($"[{gameObject.name}] Layer切换: {currentSortingLayer}, Order: {calculatedOrder}");
            }
        }
    }
    
    /// <summary>
    /// 获取当前计算出的排序值（用于调试）
    /// </summary>
    public int GetCurrentSortingOrder() => lastCalculatedOrder;
    
    /// <summary>
    /// 获取当前所在的Sorting Layer
    /// </summary>
    public string GetCurrentSortingLayer() => currentSortingLayer;
    
}

