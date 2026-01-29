using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PackagePanelTabsUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject panelRoot;         // PackagePanel 根
    [SerializeField] private Transform topParent;           // 6_Top
    [SerializeField] private Transform pagesParent;         // Main 下的 0_Props,1_Recipes,...

    [Header("Box UI")]
    [Tooltip("Box UI 的父容器（位于 PackagePanel 内部）")]
    [SerializeField] private Transform boxUIRoot;
    
    // 当前活跃的 Box UI 实例
    private GameObject _activeBoxUI;
    
    // 🔥 C9：记录进入 Box 模式前背包是否打开
    private bool _wasBackpackOpenBeforeBox = false;

    private readonly Dictionary<int, Toggle> topToggles = new Dictionary<int, Toggle>();
    private readonly Dictionary<int, GameObject> pages = new Dictionary<int, GameObject>();
    private int currentIndex = -1;
    private bool suppressToggleCallbacks = false;
    private bool initialStateApplied = false;

    void Awake()
    {
        TryAutoLocate();
        Collect();
        ApplyInitialState();
        WireToggles();
    }

    public void SetRoots(GameObject root, Transform top, Transform pagesRoot)
    {
        panelRoot = root; topParent = top; pagesParent = pagesRoot; Collect(); ApplyInitialState(); WireToggles();
    }

    void Collect()
    {
        topToggles.Clear();
        pages.Clear();
        if (topParent != null)
        {
            foreach (Transform t in topParent)
            {
                if (t.name.StartsWith("Top_"))
                {
                    if (int.TryParse(t.name.Substring(4), out int idx))
                    {
                        var tg = t.GetComponent<Toggle>();
                        if (tg != null) topToggles[idx] = tg;
                    }
                }
            }
        }
        if (pagesParent != null)
        {
            foreach (Transform t in pagesParent)
            {
                if (t.name.Length > 2 && char.IsDigit(t.name[0]) && t.name[1] == '_')
                {
                    int idx = t.name[0] - '0';
                    pages[idx] = t.gameObject;
                }
            }
        }
    }

    void TryAutoLocate()
    {
        if (panelRoot == null)
        {
            panelRoot = LocatePanelRoot();
        }
        if (topParent == null && panelRoot != null)
        {
            topParent = FindChildByName(panelRoot.transform, "6_Top");
            if (topParent == null) topParent = FindChildContains(panelRoot.transform, "Top");
        }
        if (pagesParent == null && panelRoot != null)
        {
            // 优先找名为 Main 的节点；找不到就根据 0_Props 的父来推断
            pagesParent = FindChildByName(panelRoot.transform, "Main");
            if (pagesParent == null)
            {
                var props = FindChildByName(panelRoot.transform, "0_Props");
                if (props != null) pagesParent = props.parent;
            }
        }
    }

    Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    Transform FindChildContains(Transform root, string part)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains(part)) return t;
        }
        return null;
    }

    GameObject LocatePanelRoot()
    {
        // 优先在自身及父级中寻找名称包含 "PackagePanel" 的对象
        Transform walker = transform;
        while (walker != null)
        {
            if (walker.name.IndexOf("PackagePanel", StringComparison.OrdinalIgnoreCase) >= 0)
                return walker.gameObject;
            walker = walker.parent;
        }

        // 退而求其次：在场景根节点下遍历查找
        if (gameObject.scene.IsValid())
        {
            var roots = gameObject.scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.IndexOf("PackagePanel", StringComparison.OrdinalIgnoreCase) >= 0)
                        return t.gameObject;
                }
            }
        }

        // 最后兜底：若仍未找到，则使用父物体，若无父则自身
        return transform.parent != null ? transform.parent.gameObject : gameObject;
    }

    void WireToggles()
    {
        // 给Top的Toggle绑定点击事件，使鼠标点击和键盘快捷键逻辑统一
        foreach (var kv in topToggles)
        {
            var idx = kv.Key;
            var tg = kv.Value;
            if (tg == null) continue;
            // 先移除旧监听避免重复
            tg.onValueChanged.RemoveAllListeners();
            // 绑定：Toggle被选中时调用OpenOrToggle
            tg.onValueChanged.AddListener(isOn => HandleToggleChanged(idx, isOn));
        }
    }

    // 供外部在运行时激活UI后调用，确保自动定位与绑定完成
    public void EnsureReady()
    {
        TryAutoLocate();
        Collect();
        WireToggles();
        if (panelRoot != null && !panelRoot.activeSelf)
        {
            ApplyInitialState();
        }
    }

    public void ShowPanel(bool visible)
    {
        if (visible) OpenPanel(); else ClosePanel();
    }

    public void TogglePanel()
    {
        if (panelRoot == null) return;
        bool now = !panelRoot.activeSelf;
        ShowPanel(now);
    }

    public bool IsPanelOpen()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    public void ShowPage(int idx)
    {
        EnsureCollected();
        // 若找不到该idx，尝试按名称部分匹配
        if (panelRoot == null)
        {
            Debug.LogError("[PackagePanelTabsUI] ShowPage called but panelRoot is null!");
            return;
        }
        if (!pages.ContainsKey(idx)) return;

        SetVisiblePage(idx);
    }

    // 快捷方法
    public void OpenProps()      { OpenOrToggle(0); }
    public void OpenRecipes()    { OpenOrToggle(1); }
    public void OpenEx()         { OpenOrToggle(2); }
    public void OpenMap()        { OpenOrToggle(3); }
    public void OpenRelations()  { OpenOrToggle(4); }
    public void OpenSettings()   { OpenOrToggle(5); }

    private void OpenOrToggle(int idx)
    {
        EnsureCollected();
        
        // 🔥 C2/C9：打开背包前先关闭 Box UI（互斥逻辑）
        CloseBoxPanelIfOpen();
        
        bool isOpen = panelRoot != null && panelRoot.activeSelf;
        if (!isOpen)
        {
            OpenPanel();
            SetVisiblePage(idx);
            return;
        }
        if (currentIndex == idx)
        {
            ClosePanel();
            return;
        }
        
        // ★ 切换页面时取消拿取状态
        CancelInteractionIfNeeded();
        
        SetVisiblePage(idx);
    }

    void SetToggleSelection(int idx)
    {
        suppressToggleCallbacks = true;
        foreach (var kv in topToggles)
        {
            var tg = kv.Value;
            if (tg == null) continue;
#if UNITY_2021_2_OR_NEWER
            tg.SetIsOnWithoutNotify(idx >= 0 && kv.Key == idx);
#else
            tg.isOn = idx >= 0 && kv.Key == idx;
#endif
        }
        suppressToggleCallbacks = false;
    }

    void HandleToggleChanged(int idx, bool isOn)
    {
        if (suppressToggleCallbacks) return;
        if (isOn)
        {
            // 鼠标点击Toggle：只负责打开面板和切换页面，不关闭
            bool isOpen = panelRoot != null && panelRoot.activeSelf;
            if (!isOpen)
            {
                OpenPanel();
            }
            SetVisiblePage(idx);
            return;
        }
        // Toggle被取消选中时不做任何操作，因为会有其他Toggle被选中
    }

    void SetVisiblePage(int idx)
    {
        if (!pages.ContainsKey(idx)) return;

        foreach (var kv in pages)
        {
            if (kv.Value != null) kv.Value.SetActive(kv.Key == idx);
        }

        SetToggleSelection(idx);
        currentIndex = idx;
    }

    void SetVisiblePageInactive()
    {
        foreach (var kv in pages)
        {
            if (kv.Value != null) kv.Value.SetActive(false);
        }
        currentIndex = -1;
        SetToggleSelection(-1);
    }

    void OpenPanel()
    {
        if (panelRoot == null || panelRoot.activeSelf) return;
        
        // 🔥 互斥逻辑：打开 PackagePanel 前先关闭 BoxPanelUI
        CloseBoxPanelIfOpen();
        
        panelRoot.SetActive(true);
        
        // 🔥 C10：确保 Main/Top 可见（修复 ESC 关闭箱子后 Tab 打开背包时 Main/Top 未激活的问题）
        ShowMainAndTop();
        
        OnPanelJustOpened();
    }
    
    /// <summary>
    /// 🔥 关闭箱子面板（互斥逻辑）
    /// 🔥 C2/C9：打开背包时调用，关闭 Box 后打开背包
    /// </summary>
    private void CloseBoxPanelIfOpen()
    {
        if (FarmGame.UI.BoxPanelUI.ActiveInstance != null && FarmGame.UI.BoxPanelUI.ActiveInstance.IsOpen)
        {
            FarmGame.UI.BoxPanelUI.ActiveInstance.Close();
        }
        
        // 同时清理本地引用并恢复 Main/Top
        if (_activeBoxUI != null)
        {
            Destroy(_activeBoxUI);
            _activeBoxUI = null;
            ShowMainAndTop();
        }
        
        _wasBackpackOpenBeforeBox = false;
    }

    #region Box UI 管理

    /// <summary>
    /// 🔥 修正 Ⅱ：确保 PackagePanel 为 Box 模式打开时也执行初始化
    /// </summary>
    private void EnsurePanelOpenForBox()
    {
        if (panelRoot == null) return;
        if (!panelRoot.activeSelf)
        {
            panelRoot.SetActive(true);
            OnPanelJustOpened(); // 🔥 关键：确保 InventoryPanelUI.EnsureBuilt() 被调用
        }
    }

    /// <summary>
    /// 打开 Box UI（在 PackagePanel 内部实例化）
    /// </summary>
    /// <param name="boxUIPrefab">Box UI 预制体</param>
    /// <returns>实例化的 BoxPanelUI 组件</returns>
    public FarmGame.UI.BoxPanelUI OpenBoxUI(GameObject boxUIPrefab)
    {
        if (boxUIPrefab == null)
        {
            Debug.LogError("[PackagePanelTabsUI] boxUIPrefab 为空！");
            return null;
        }

        // 🔥 C9：记录进入 Box 模式前背包是否打开
        _wasBackpackOpenBeforeBox = IsBackpackVisible();

        // 关闭之前的 Box UI
        CloseBoxUIInternal();

        // 🔥 修正 Ⅱ：使用对称逻辑确保 PackagePanel 打开并初始化
        EnsurePanelOpenForBox();

        // 隐藏 Main 和 Top（背包区域）
        HideMainAndTop();

        // 确定父容器
        Transform parent = boxUIRoot;
        if (parent == null)
        {
            // 如果没有配置 boxUIRoot，尝试自动查找或使用 panelRoot
            parent = FindChildByName(panelRoot.transform, "BoxUIRoot");
            if (parent == null)
            {
                parent = panelRoot.transform;
            }
        }

        // 实例化 Box UI
        _activeBoxUI = Instantiate(boxUIPrefab, parent);
        _activeBoxUI.transform.SetAsLastSibling();

        var boxPanelUI = _activeBoxUI.GetComponent<FarmGame.UI.BoxPanelUI>();
        if (boxPanelUI == null)
        {
            Debug.LogError($"[PackagePanelTabsUI] Box UI 预制体 {boxUIPrefab.name} 缺少 BoxPanelUI 组件！");
            Destroy(_activeBoxUI);
            _activeBoxUI = null;
            return null;
        }

        Debug.Log($"[PackagePanelTabsUI] 打开 Box UI: {boxUIPrefab.name}, panelRoot.active={panelRoot.activeSelf}, wasBackpackOpen={_wasBackpackOpenBeforeBox}");
        return boxPanelUI;
    }

    /// <summary>
    /// 关闭 Box UI（内部方法，不处理后续状态）
    /// </summary>
    private void CloseBoxUIInternal()
    {
        if (_activeBoxUI != null)
        {
            var boxPanelUI = _activeBoxUI.GetComponent<FarmGame.UI.BoxPanelUI>();
            if (boxPanelUI != null && boxPanelUI.IsOpen)
            {
                boxPanelUI.Close();
            }
            Destroy(_activeBoxUI);
            _activeBoxUI = null;
        }
    }

    /// <summary>
    /// 关闭 Box UI（公共方法）
    /// 🔥 C9：区分 Tab 触发和 ESC 触发的关闭行为
    /// 🔥 C10：ESC 关闭时也要恢复 Main/Top 状态，避免后续打开背包时显示空白
    /// </summary>
    /// <param name="openBackpackAfter">是否在关闭后打开背包（Tab 触发时为 true）</param>
    public void CloseBoxUI(bool openBackpackAfter = false)
    {
        if (_activeBoxUI == null) return;
        
        CloseBoxUIInternal();

        if (openBackpackAfter)
        {
            // Tab 触发：关闭 Box 后打开背包
            ShowMainAndTop();
            if (currentIndex < 0) currentIndex = 0;
            SetVisiblePage(currentIndex);
            Debug.Log($"[PackagePanelTabsUI] CloseBoxUI: Tab 触发，打开背包页面 {currentIndex}");
        }
        else
        {
            // 🔥 C10：ESC 触发时，先恢复 Main/Top 状态，再关闭面板
            // 这样后续打开背包时 Main/Top 就是正确的状态
            ShowMainAndTop();
            ClosePanel();
            Debug.Log("[PackagePanelTabsUI] CloseBoxUI: ESC 触发，返回 NoPanel");
        }
        
        _wasBackpackOpenBeforeBox = false;
    }

    /// <summary>
    /// 隐藏 Main 和 Top（背包区域）
    /// </summary>
    private void HideMainAndTop()
    {
        if (topParent != null)
        {
            topParent.gameObject.SetActive(false);
        }
        if (pagesParent != null)
        {
            pagesParent.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 显示 Main 和 Top（背包区域）
    /// </summary>
    private void ShowMainAndTop()
    {
        if (topParent != null)
        {
            topParent.gameObject.SetActive(true);
        }
        if (pagesParent != null)
        {
            pagesParent.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 是否有 Box UI 打开
    /// </summary>
    public bool IsBoxUIOpen()
    {
        return _activeBoxUI != null && _activeBoxUI.activeSelf;
    }

    /// <summary>
    /// 🔥 C9：背包区域（Main/Top）是否可见
    /// </summary>
    private bool IsBackpackVisible()
    {
        return panelRoot != null && panelRoot.activeSelf 
            && topParent != null && topParent.gameObject.activeSelf
            && pagesParent != null && pagesParent.gameObject.activeSelf;
    }

    #endregion

    void ClosePanel()
    {
        if (panelRoot == null || !panelRoot.activeSelf) return;
        
        // ★ 关闭面板时处理手持物品（物品归位逻辑）
        ReturnHeldItemsBeforeClose();
        
        panelRoot.SetActive(false);
        SetVisiblePageInactive();
    }
    
    /// <summary>
    /// 🔥 P1+-1：关闭前处理手持物品（物品归位逻辑）
    /// </summary>
    private void ReturnHeldItemsBeforeClose()
    {
        var interactionManager = InventoryInteractionManager.Instance;
        if (interactionManager != null && interactionManager.IsHolding)
        {
            Debug.Log($"<color=yellow>[PackagePanelTabsUI] 关闭面板前归位手持物品</color>");
            interactionManager.ReturnHeldItemToInventory();
        }
        
        // 同时处理 SlotDragContext（箱子物品）
        if (SlotDragContext.IsDragging)
        {
            Debug.Log($"<color=yellow>[PackagePanelTabsUI] 关闭面板前归位箱子物品</color>");
            SlotDragContext.Cancel();
        }
    }
    
    /// <summary>
    /// 取消背包交互状态（如果正在拿取物品）
    /// </summary>
    private void CancelInteractionIfNeeded()
    {
        var interactionManager = InventoryInteractionManager.Instance;
        if (interactionManager != null && interactionManager.IsHolding)
        {
            Debug.Log($"<color=yellow>[PackagePanelTabsUI] 取消背包交互状态</color>");
            interactionManager.Cancel();
        }
    }

    void ApplyInitialState()
    {
        if (initialStateApplied) return;
        initialStateApplied = true;
        SetVisiblePageInactive();
        if (panelRoot != null && panelRoot.activeSelf)
        {
            panelRoot.SetActive(false);
        }
    }

    void EnsureCollected()
    {
        if (topParent == null || pagesParent == null) TryAutoLocate();
        if (topToggles.Count == 0 || pages.Count == 0) Collect();
    }

    void OnPanelJustOpened()
    {
        // 主面板从关闭到打开：重置道具栏与装备栏选择（满足“关闭再打开才映射Toolbar选择”的需求）
        var invPanel = panelRoot != null ? panelRoot.GetComponentInChildren<InventoryPanelUI>(true) : null;
        if (invPanel != null)
        {
            invPanel.EnsureBuilt();
            invPanel.ResetSelectionsOnPanelOpen();
        }
    }

    // 备用：通过名称片段打开（以防层级命名不同）
    public void OpenByName(string pageNamePart)
    {
        EnsureCollected();
        int idx = -1;
        foreach (var kv in pages)
        {
            if (kv.Value != null && kv.Value.name.IndexOf(pageNamePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
            { idx = kv.Key; break; }
        }
        if (idx >= 0) { if (!IsPanelOpen()) ShowPanel(true); ShowPage(idx); }
    }
}
