using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 跟随鼠标显示被拿起的物品
/// 独立组件，不依赖任何 SlotUI 代码
/// </summary>
public class HeldItemDisplay : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text amountText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();
        
        parentCanvas = GetComponentInParent<Canvas>();
        
        // 自动查找或创建子组件
        if (iconImage == null)
        {
            var t = transform.Find("Icon");
            if (t != null)
            {
                iconImage = t.GetComponent<Image>();
            }
            else
            {
                var go = new GameObject("Icon");
                go.transform.SetParent(transform, false);
                iconImage = go.AddComponent<Image>();
                iconImage.raycastTarget = false;
                var rt = (RectTransform)iconImage.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
        
        if (amountText == null)
        {
            var t = transform.Find("Amount");
            if (t != null)
            {
                amountText = t.GetComponent<Text>();
            }
            else
            {
                var go = new GameObject("Amount");
                go.transform.SetParent(transform, false);
                amountText = go.AddComponent<Text>();
                amountText.raycastTarget = false;
                amountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                amountText.fontSize = 18;
                amountText.fontStyle = FontStyle.BoldAndItalic;
                amountText.color = Color.black;
                amountText.alignment = TextAnchor.LowerRight;
                var rt = (RectTransform)amountText.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(21.2356f, 0f);
                rt.offsetMax = new Vector2(-3.8808f, -41.568f);
            }
        }
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 初始隐藏
        Hide();
    }
    
    void OnEnable()
    {
        // 确保有 Canvas 引用
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();
    }
    
    /// <summary>
    /// 显示物品
    /// </summary>
    public void Show(int itemId, int amount, Sprite icon)
    {
        gameObject.SetActive(true);
        
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            
            // 使用 UIItemIconScaler 保持样式一致（如果可用）
            if (icon != null)
            {
                UIItemIconScaler.SetIconWithAutoScale(iconImage, icon, null);
            }
        }
        
        UpdateAmount(amount);
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false; // 不阻挡射线
        }
        
        if (showDebugInfo)
            Debug.Log($"[HeldItemDisplay] 显示物品: itemId={itemId}, sprite={icon?.name}, amount={amount}");
    }
    
    /// <summary>
    /// 更新数量显示
    /// </summary>
    public void UpdateAmount(int amount)
    {
        if (amountText != null)
        {
            amountText.text = amount > 1 ? amount.ToString() : "";
        }
    }
    
    /// <summary>
    /// 隐藏显示
    /// </summary>
    public void Hide()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 是否正在显示
    /// </summary>
    public bool IsShowing => gameObject.activeSelf && canvasGroup != null && canvasGroup.alpha > 0;
    
    void Update()
    {
        if (!IsShowing) return;
        
        // 跟随鼠标
        FollowMouse();
    }
    
    private void FollowMouse()
    {
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;
        }
        
        if (rectTransform == null) return;
        
        // 根据 Canvas 渲染模式处理
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = Input.mousePosition;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                Input.mousePosition,
                parentCanvas.worldCamera,
                out Vector2 localPoint
            );
            rectTransform.localPosition = localPoint;
        }
    }
}
