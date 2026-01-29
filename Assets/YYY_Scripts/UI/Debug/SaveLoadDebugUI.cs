using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data.Core;

/// <summary>
/// 存档/读档调试 UI
/// 
/// 功能：
/// - 显示保存/加载按钮
/// - 显示当前存档状态
/// - 按 F5 保存，F9 加载
/// 
/// 使用方法：
/// 1. 在场景中创建一个空 GameObject
/// 2. 添加此组件
/// 3. 运行游戏后会自动创建 UI
/// 
/// 设计尺寸：1920x1080，显示在左下角
/// </summary>
public class SaveLoadDebugUI : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private string defaultSlotName = "slot1";
    [SerializeField] private KeyCode saveKey = KeyCode.F5;
    [SerializeField] private KeyCode loadKey = KeyCode.F9;
    
    [Header("UI 位置（左下角偏移）")]
    [SerializeField] private Vector2 panelOffset = new Vector2(120, 80); // 距离左下角的偏移
    
    private Canvas _canvas;
    private Text _statusText;
    private bool _uiCreated = false;
    
    // 面板尺寸
    private const float PanelWidth = 220f;
    private const float PanelHeight = 130f;
    private const float ButtonWidth = 95f;
    private const float ButtonHeight = 30f;
    
    void Start()
    {
        CreateDebugUI();
    }
    
    void Update()
    {
        // 快捷键
        if (Input.GetKeyDown(saveKey))
        {
            DoSave();
        }
        else if (Input.GetKeyDown(loadKey))
        {
            DoLoad();
        }
    }
    
    private void CreateDebugUI()
    {
        if (_uiCreated) return;
        
        // 创建 Canvas
        var canvasGo = new GameObject("SaveLoadDebugCanvas");
        canvasGo.transform.SetParent(transform);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999; // 最顶层
        
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
        CreateText(panelGo.transform, "存档系统 (调试)", new Vector2(PanelWidth/2, PanelHeight - 20), 14, TextAnchor.MiddleCenter);
        
        // 保存按钮
        CreateButton(panelGo.transform, $"保存 ({saveKey})", new Vector2(10, PanelHeight - 55), new Vector2(ButtonWidth, ButtonHeight), () => DoSave());
        
        // 加载按钮
        CreateButton(panelGo.transform, $"加载 ({loadKey})", new Vector2(115, PanelHeight - 55), new Vector2(ButtonWidth, ButtonHeight), () => DoLoad());
        
        // 状态文本
        _statusText = CreateText(panelGo.transform, "状态: 就绪", new Vector2(PanelWidth/2, 45), 12, TextAnchor.MiddleCenter);
        
        // 提示文本
        CreateText(panelGo.transform, $"快捷键: {saveKey}=保存, {loadKey}=加载", new Vector2(PanelWidth/2, 15), 10, TextAnchor.MiddleCenter);
        
        _uiCreated = true;
        Debug.Log("[SaveLoadDebugUI] 调试 UI 已创建");
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
        rt.sizeDelta = new Vector2(200, 25);
        
        return text;
    }
    
    private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, System.Action onClick)
    {
        var go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        
        var image = go.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        
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
    
    private void DoSave()
    {
        if (SaveManager.Instance == null)
        {
            UpdateStatus("错误: SaveManager 未找到");
            return;
        }
        
        bool success = SaveManager.Instance.SaveGame(defaultSlotName);
        UpdateStatus(success ? $"已保存到 {defaultSlotName}" : "保存失败!");
    }
    
    private void DoLoad()
    {
        if (SaveManager.Instance == null)
        {
            UpdateStatus("错误: SaveManager 未找到");
            return;
        }
        
        if (!SaveManager.Instance.SaveExists(defaultSlotName))
        {
            UpdateStatus($"存档 {defaultSlotName} 不存在");
            return;
        }
        
        bool success = SaveManager.Instance.LoadGame(defaultSlotName);
        UpdateStatus(success ? $"已加载 {defaultSlotName}" : "加载失败!");
    }
    
    private void UpdateStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = $"状态: {message}";
        }
        Debug.Log($"[SaveLoadDebugUI] {message}");
    }
}
