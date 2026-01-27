using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmGame.Data;
using FarmGame.UI;

public class ToolbarSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text amountText;
    [SerializeField] private Image selectedOverlay;
    [SerializeField] private Toggle toggle;

    private InventoryService inventory;
    private ItemDatabase database;
    private HotbarSelectionService selection;
    private int index; // 0..11

    void Awake()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        if (iconImage == null)
        {
            var t = transform.Find("Icon");
            if (t) iconImage = t.GetComponent<Image>();
            else
            {
                var go = new GameObject("Icon");
                go.transform.SetParent(transform, false);
                iconImage = go.AddComponent<Image>();
                iconImage.raycastTarget = false;
                var rt = (RectTransform)iconImage.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                iconImage.enabled = false;
            }
        }
        if (amountText == null)
        {
            var t = transform.Find("Amount");
            if (t) 
            {
                amountText = t.GetComponent<Text>();
            }
            else
            {
                // ★ 自动创建 Amount（与 InventorySlotUI 保持一致）
                var go = new GameObject("Amount");
                go.transform.SetParent(transform, false);
                amountText = go.AddComponent<Text>();
                amountText.raycastTarget = false;
                
                // 字体设置
                amountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                amountText.fontSize = 18;
                amountText.fontStyle = FontStyle.BoldAndItalic;
                amountText.color = Color.black;
                amountText.alignment = TextAnchor.LowerRight;
                amountText.text = "";
                
                var rt = (RectTransform)amountText.transform;
                // 自定义锚点（全拉伸）
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                // ★ 用户指定参数：左21.2356，顶部41.568，右3.8808，底部0
                rt.offsetMin = new Vector2(21.2356f, 0f);      // left, bottom
                rt.offsetMax = new Vector2(-3.8808f, -41.568f); // -right, -top
            }
        }
        if (selectedOverlay == null)
        {
            var t = transform.Find("Selected");
            if (t) selectedOverlay = t.GetComponent<Image>();
        }

        // 让 Toggle 不影响底图着色：targetGraphic设为null，transition=None，避免Toggle自身视觉反馈
        // 选中红框完全由 selectedOverlay 控制
        if (toggle != null)
        {
            toggle.targetGraphic = null;  // 不让Toggle着色任何图像
            toggle.transition = Selectable.Transition.None;
#if UNITY_2021_2_OR_NEWER
            toggle.SetIsOnWithoutNotify(false);
#else
            toggle.isOn = false;
#endif
        }
    }

    void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnHotbarSlotChanged += HandleHotbarChanged;
            inventory.OnSlotChanged += HandleAnySlotChanged;
        }
        if (selection != null)
        {
            selection.OnSelectedChanged -= HandleSelectionChanged;
            selection.OnSelectedChanged += HandleSelectionChanged;
        }
        
        // 注册 Toggle 的 OnValueChanged 事件，用于锁定状态下拦截
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
        
        Refresh();
        RefreshSelection();
    }

    void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnHotbarSlotChanged -= HandleHotbarChanged;
            inventory.OnSlotChanged -= HandleAnySlotChanged;
        }
        if (selection != null)
        {
            selection.OnSelectedChanged -= HandleSelectionChanged;
        }
        
        // 移除 Toggle 事件监听
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
    
    /// <summary>
    /// Toggle 值变化时的处理
    /// 在锁定状态下立即恢复状态，阻止视觉变化
    /// </summary>
    private void OnToggleValueChanged(bool isOn)
    {
        var lockManager = ToolActionLockManager.Instance;
        if (lockManager != null && lockManager.IsLocked)
        {
            // 锁定状态：立即恢复到正确的选中状态
            ForceRestoreToggleState();
        }
    }

    public void Bind(InventoryService inv, ItemDatabase db, HotbarSelectionService sel, int hotbarIndex)
    {
        inventory = inv;
        database = db;
        selection = sel;
        index = hotbarIndex;
        if (isActiveAndEnabled)
        {
            OnDisable();
            OnEnable();
        }
        else
        {
            Refresh();
            RefreshSelection();
        }
    }

    void HandleHotbarChanged(int changedIndex)
    {
        if (changedIndex == index) Refresh();
    }

    void HandleAnySlotChanged(int changedIndex)
    {
        if (changedIndex == index) Refresh();
    }

    void HandleSelectionChanged(int idx)
    {
        RefreshSelection();
    }

    public void Refresh()
    {
        if (inventory == null || database == null) return;
        var s = inventory.GetSlot(index);
        if (s.IsEmpty)
        {
            // 使用统一的缩放适配工具清除图标
            if (iconImage) UIItemIconScaler.SetIconWithAutoScale(iconImage, null, null);
            if (amountText) amountText.text = "";
            return;
        }
        var data = database.GetItemByID(s.itemId);
        // ✅ 使用统一的缩放适配工具设置图标（支持自定义旋转和尺寸）
        if (iconImage)
        {
            UIItemIconScaler.SetIconWithAutoScale(iconImage, data != null ? data.GetBagSprite() : null, data);
        }
        if (amountText)
        {
            amountText.text = s.amount > 1 ? s.amount.ToString() : "";
        }
    }

    public void RefreshSelection()
    {
        bool sel = selection != null && selection.selectedIndex == index;
        // 1) 更新选中红框（overlay）
        if (selectedOverlay) selectedOverlay.enabled = sel;
        // 2) 更新Toggle.isOn状态以配合ToggleGroup联动（但不触发事件）
        if (toggle != null)
        {
#if UNITY_2021_2_OR_NEWER
            toggle.SetIsOnWithoutNotify(sel);
#else
            bool prev = toggle.isOn;
            toggle.isOn = sel;
            // 低版本无SetIsOnWithoutNotify，需要手动避免事件（但ToggleGroup会处理）
#endif
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // ★ 检查是否有面板打开（背包/箱子）- 被遮挡时不响应
            var packageTabs = FindFirstObjectByType<PackagePanelTabsUI>();
            if (packageTabs != null && packageTabs.IsPanelOpen()) 
            {
                ForceRestoreToggleState();
                return;
            }
            if (BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen) 
            {
                ForceRestoreToggleState();
                return;
            }
            
            // 检查是否处于工具动作锁定状态
            var lockManager = ToolActionLockManager.Instance;
            if (lockManager != null && lockManager.IsLocked)
            {
                // 锁定状态：缓存输入而非立即切换
                lockManager.CacheHotbarInput(index);
                
                // 重要：强制恢复 Toggle 状态，防止视觉变化
                // Toggle 的 OnValueChanged 可能已经触发，需要立即恢复
                ForceRestoreToggleState();
                return;
            }
            
            selection?.SelectIndex(index);
            RefreshSelection();
        }
    }
    
    /// <summary>
    /// 强制恢复 Toggle 状态到当前选中的槽位
    /// 用于锁定状态下阻止视觉变化
    /// </summary>
    private void ForceRestoreToggleState()
    {
        if (toggle == null || selection == null) return;
        
        bool shouldBeSelected = selection.selectedIndex == index;
        
        // 使用 SetIsOnWithoutNotify 避免触发事件
#if UNITY_2021_2_OR_NEWER
        toggle.SetIsOnWithoutNotify(shouldBeSelected);
#else
        toggle.isOn = shouldBeSelected;
#endif
        
        // 同时更新选中覆盖层
        if (selectedOverlay != null)
            selectedOverlay.enabled = shouldBeSelected;
    }
}
