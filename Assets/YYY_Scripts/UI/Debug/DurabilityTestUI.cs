using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data.Core;

/// <summary>
/// 耐久度测试 UI
/// 
/// 功能：
/// - 给当前选中的工具添加耐久度
/// - 消耗当前工具的耐久度
/// - 显示当前工具的耐久度状态
/// 
/// 使用方法：
/// 1. 在场景中创建一个空 GameObject
/// 2. 添加此组件
/// 3. 运行游戏后会自动创建 UI
/// 
/// 设计尺寸：1920x1080，显示在左下角（SaveLoad 面板右边）
/// </summary>
public class DurabilityTestUI : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private int defaultMaxDurability = 100;
    [SerializeField] private int durabilityConsumeAmount = 10;
    [SerializeField] private KeyCode consumeKey = KeyCode.U;
    
    [Header("UI 位置（左下角偏移）")]
    [SerializeField] private Vector2 panelOffset = new Vector2(350, 80); // 距离左下角的偏移，在 SaveLoad 右边
    
    private Canvas _canvas;
    private Text _statusText;
    private InventoryService _inventory;
    private HotbarSelectionService _hotbarSelection;
    private bool _uiCreated = false;
    
    // 面板尺寸
    private const float PanelWidth = 230f;
    private const float PanelHeight = 160f;
    private const float ButtonWidth = 210f;
    private const float ButtonHeight = 30f;
    
    void Start()
    {
        _inventory = FindFirstObjectByType<InventoryService>();
        _hotbarSelection = FindFirstObjectByType<HotbarSelectionService>();
        CreateDebugUI();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(consumeKey))
        {
            ConsumeDurability();
        }
        
        // 每帧更新状态显示
        UpdateStatusDisplay();
    }
    
    private void CreateDebugUI()
    {
        if (_uiCreated) return;
        
        // 创建 Canvas
        var canvasGo = new GameObject("DurabilityTestCanvas");
        canvasGo.transform.SetParent(transform);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9998;
        
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGo.AddComponent<GraphicRaycaster>();
        
        // 创建面板背景 - 锚点设为左下角
        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(_canvas.transform, false);
        var panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0, 0); // 左下角
        panelRt.anchorMax = new Vector2(0, 0);
        panelRt.pivot = new Vector2(0, 0); // pivot 也在左下角
        panelRt.anchoredPosition = panelOffset; // 从左下角偏移
        panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        
        // 标题
        CreateText(panelGo.transform, "耐久度测试 (调试)", new Vector2(PanelWidth/2, PanelHeight - 20), 14, TextAnchor.MiddleCenter);
        
        // 添加耐久度按钮
        CreateButton(panelGo.transform, "给工具添加耐久度", new Vector2(10, PanelHeight - 55), new Vector2(ButtonWidth, ButtonHeight), () => AddDurabilityToCurrentTool());
        
        // 消耗耐久度按钮
        CreateButton(panelGo.transform, $"消耗耐久 ({consumeKey})", new Vector2(10, PanelHeight - 90), new Vector2(ButtonWidth, ButtonHeight), () => ConsumeDurability());
        
        // 状态文本
        _statusText = CreateText(panelGo.transform, "当前工具: 无", new Vector2(PanelWidth/2, 40), 11, TextAnchor.MiddleCenter);
        
        // 提示
        CreateText(panelGo.transform, $"按 {consumeKey} 消耗耐久度", new Vector2(PanelWidth/2, 15), 10, TextAnchor.MiddleCenter);
        
        _uiCreated = true;
        Debug.Log("[DurabilityTestUI] 调试 UI 已创建");
    }
    
    private Text CreateText(Transform parent, string content, Vector2 position, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(210, 25);
        
        return text;
    }
    
    private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, System.Action onClick)
    {
        var go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        
        var image = go.AddComponent<Image>();
        image.color = new Color(0.3f, 0.5f, 0.3f, 1f);
        
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());
        
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        
        // 按钮文字
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        
        return button;
    }
    
    private int GetCurrentSlotIndex()
    {
        if (_hotbarSelection != null)
        {
            return _hotbarSelection.selectedIndex;
        }
        return 0;
    }
    
    private void AddDurabilityToCurrentTool()
    {
        if (_inventory == null)
        {
            Debug.LogWarning("[DurabilityTestUI] InventoryService 未找到");
            return;
        }
        
        int slotIndex = GetCurrentSlotIndex();
        var item = _inventory.GetInventoryItem(slotIndex);
        
        if (item == null || item.IsEmpty)
        {
            Debug.Log("[DurabilityTestUI] 当前槽位为空，无法添加耐久度");
            return;
        }
        
        // 设置耐久度
        item.SetDurability(defaultMaxDurability, defaultMaxDurability);
        
        // 触发 UI 刷新
        _inventory.RefreshSlot(slotIndex);
        
        Debug.Log($"[DurabilityTestUI] 已为槽位 {slotIndex} 的物品添加耐久度: {defaultMaxDurability}/{defaultMaxDurability}");
    }
    
    private void ConsumeDurability()
    {
        if (_inventory == null)
        {
            Debug.LogWarning("[DurabilityTestUI] InventoryService 未找到");
            return;
        }
        
        int slotIndex = GetCurrentSlotIndex();
        var item = _inventory.GetInventoryItem(slotIndex);
        
        if (item == null || item.IsEmpty)
        {
            Debug.Log("[DurabilityTestUI] 当前槽位为空");
            return;
        }
        
        if (!item.HasDurability)
        {
            Debug.Log("[DurabilityTestUI] 当前物品没有耐久度，请先点击'添加耐久度'按钮");
            return;
        }
        
        // 消耗耐久度
        bool broken = item.UseDurability(durabilityConsumeAmount);
        
        // 触发 UI 刷新
        _inventory.RefreshSlot(slotIndex);
        
        if (broken)
        {
            Debug.Log($"[DurabilityTestUI] 物品已损坏！");
        }
        else
        {
            Debug.Log($"[DurabilityTestUI] 消耗耐久度 {durabilityConsumeAmount}，剩余: {item.CurrentDurability}/{item.MaxDurability}");
        }
    }
    
    private void UpdateStatusDisplay()
    {
        if (_statusText == null || _inventory == null) return;
        
        int slotIndex = GetCurrentSlotIndex();
        var item = _inventory.GetInventoryItem(slotIndex);
        
        if (item == null || item.IsEmpty)
        {
            _statusText.text = $"槽位 {slotIndex}: 空";
        }
        else if (!item.HasDurability)
        {
            _statusText.text = $"槽位 {slotIndex}: 物品ID={item.ItemId} (无耐久度)";
        }
        else
        {
            float percent = item.DurabilityPercent * 100f;
            _statusText.text = $"槽位 {slotIndex}: {item.CurrentDurability}/{item.MaxDurability} ({percent:F0}%)";
        }
    }
}
