using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data;

/// <summary>
/// 物品详情悬浮框
/// 鼠标悬浮在物品槽位上时显示物品信息
/// </summary>
public class ItemTooltip : MonoBehaviour
{
    #region 单例
    
    public static ItemTooltip Instance { get; private set; }
    
    #endregion
    
    #region 序列化字段
    
    [Header("UI 引用")]
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text priceText;
    [SerializeField] private Image qualityIcon;
    
    [Header("显示设置")]
    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);
    [SerializeField] private float showDelay = 0.3f;
    [SerializeField] private float fadeSpeed = 10f;
    
    #endregion
    
    #region 私有字段
    
    private Canvas _parentCanvas;
    private Camera _uiCamera;
    private bool _isShowing = false;
    private float _hoverTimer = 0f;
    private ItemStack _currentItem;
    private ItemDatabase _database;
    private int _currentAmount = 0;
    
    #endregion
    
    #region Unity 生命周期
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (tooltipRect == null)
            tooltipRect = GetComponent<RectTransform>();
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            _uiCamera = _parentCanvas.worldCamera;
        }
        
        // 初始隐藏
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        
        gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    void Update()
    {
        if (_isShowing)
        {
            FollowMouse();
            
            // 淡入效果
            if (canvasGroup != null && canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, fadeSpeed * Time.unscaledDeltaTime);
            }
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 设置数据库引用
    /// </summary>
    public void SetDatabase(ItemDatabase database)
    {
        _database = database;
    }
    
    /// <summary>
    /// 显示物品详情
    /// </summary>
    /// <param name="item">物品栈</param>
    /// <param name="amount">数量（用于计算总价）</param>
    public void Show(ItemStack item, int amount = 1)
    {
        if (item.IsEmpty || _database == null)
        {
            Hide();
            return;
        }
        
        var itemData = _database.GetItemByID(item.itemId);
        if (itemData == null)
        {
            Hide();
            return;
        }
        
        _currentItem = item;
        _currentAmount = amount;
        _isShowing = true;
        gameObject.SetActive(true);
        
        // 设置物品名称
        if (itemNameText != null)
        {
            itemNameText.text = itemData.itemName;
            // 根据品质设置颜色
            itemNameText.color = GetQualityColor((ItemQuality)item.quality);
        }
        
        // 设置描述
        if (descriptionText != null)
        {
            descriptionText.text = itemData.description;
        }
        
        // 设置价格（显示总价而非单价）
        if (priceText != null)
        {
            int totalPrice = itemData.GetSellPriceWithQuality((ItemQuality)item.quality) * amount;
            if (totalPrice > 0)
            {
                priceText.text = amount > 1 
                    ? $"总价值: {totalPrice} 金币 ({amount}个)"
                    : $"价值: {totalPrice} 金币";
                priceText.gameObject.SetActive(true);
            }
            else
            {
                priceText.gameObject.SetActive(false);
            }
        }
        
        // 设置品质图标
        if (qualityIcon != null)
        {
            if (item.quality > 0)
            {
                qualityIcon.color = itemData.GetQualityStarColor((ItemQuality)item.quality);
                qualityIcon.gameObject.SetActive(true);
            }
            else
            {
                qualityIcon.gameObject.SetActive(false);
            }
        }
        
        // 立即更新位置
        FollowMouse();
        
        // 重置透明度（淡入效果）
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// 隐藏详情框
    /// </summary>
    public void Hide()
    {
        _isShowing = false;
        _currentItem = ItemStack.Empty;
        _currentAmount = 0;
        gameObject.SetActive(false);
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// 跟随鼠标移动
    /// </summary>
    public void FollowMouse()
    {
        if (tooltipRect == null || _parentCanvas == null) return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvas.transform as RectTransform,
            Input.mousePosition,
            _uiCamera,
            out localPoint
        );
        
        // 计算位置，确保不超出屏幕
        Vector2 targetPos = localPoint + offset;
        
        // 获取屏幕边界
        RectTransform canvasRect = _parentCanvas.transform as RectTransform;
        float halfWidth = tooltipRect.rect.width * 0.5f;
        float halfHeight = tooltipRect.rect.height * 0.5f;
        
        // 限制在屏幕内
        float maxX = canvasRect.rect.width * 0.5f - halfWidth;
        float minX = -canvasRect.rect.width * 0.5f + halfWidth;
        float maxY = canvasRect.rect.height * 0.5f - halfHeight;
        float minY = -canvasRect.rect.height * 0.5f + halfHeight;
        
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        
        tooltipRect.anchoredPosition = targetPos;
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 获取品质对应的颜色
    /// </summary>
    private Color GetQualityColor(ItemQuality quality)
    {
        return quality switch
        {
            ItemQuality.Normal => Color.white,
            ItemQuality.Rare => new Color(0.3f, 0.7f, 1f),      // 蓝色
            ItemQuality.Epic => new Color(0.6f, 0.2f, 0.8f),    // 紫色
            ItemQuality.Legendary => new Color(1f, 0.8f, 0.2f), // 金色
            _ => Color.white
        };
    }
    
    #endregion
}
