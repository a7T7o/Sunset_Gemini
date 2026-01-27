using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmGame.Data;

/// <summary>
/// 背包槽位 UI - 基础版本
/// 只负责显示物品图标和数量
/// 实现基础的点击功能（选中槽位）
/// 与 ToolbarSlotUI 保持一致的简单设计
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text amountText;
    [SerializeField] private Image selectedOverlay;

    // 🔥 新增：支持 IItemContainer 接口
    private IItemContainer container;
    private InventoryService inventory;
    private EquipmentService equipment;
    private ItemDatabase database;
    private int index;
    private bool isHotbar;

    /// <summary>
    /// 槽位索引（供外部查询）
    /// </summary>
    public int Index => index;

    /// <summary>
    /// 当前绑定的容器（供外部查询）
    /// </summary>
    public IItemContainer Container => container;

    #region Unity 生命周期
    void Awake()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();

        // ★ 与 ToolbarSlotUI 保持一致：查找或创建 Icon
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
                iconImage.enabled = false;
            }
        }

        // ★ 与 ToolbarSlotUI 保持一致：查找或创建 Amount
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
                amountText.text = "";
                var rt = (RectTransform)amountText.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(21.2356f, 0f);
                rt.offsetMax = new Vector2(-3.8808f, -41.568f);
            }
        }

        if (selectedOverlay == null)
        {
            var t = transform.Find("Selected");
            if (t != null) selectedOverlay = t.GetComponent<Image>();
        }
        
        // ★ 方案 D：自动添加 Interaction 组件
        // 注意：完全不修改 Toggle 的任何配置，保留用户原有设计
        var interaction = gameObject.GetComponent<InventorySlotInteraction>();
        if (interaction == null)
        {
            interaction = gameObject.AddComponent<InventorySlotInteraction>();
        }
        interaction.Bind(this, false);
    }

    void OnEnable()
    {
        // 🔥 修复 Ⅱ：只订阅事件，不自动刷新
        // 刷新由外部调用 Bind/BindContainer 时触发
        if (container != null)
        {
            container.OnSlotChanged += OnSlotChanged;
        }
        else if (inventory != null)
        {
            inventory.OnSlotChanged += OnSlotChanged;
        }
        // 移除 Refresh()，避免使用旧绑定数据
    }

    void OnDisable()
    {
        if (container != null)
        {
            container.OnSlotChanged -= OnSlotChanged;
        }
        else if (inventory != null)
        {
            inventory.OnSlotChanged -= OnSlotChanged;
        }
    }
    #endregion

    #region 绑定和刷新

    /// <summary>
    /// 绑定到 InventoryService（原有方法，保持兼容）
    /// </summary>
    public void Bind(InventoryService inv, EquipmentService equip, ItemDatabase db, int slotIndex, bool hotbar)
    {
        // 清理旧绑定
        UnbindEvents();

        container = inv; // InventoryService 实现了 IItemContainer
        inventory = inv;
        equipment = equip;
        database = db;
        index = slotIndex;
        isHotbar = hotbar;

        if (isActiveAndEnabled)
        {
            if (inventory != null)
            {
                inventory.OnSlotChanged += OnSlotChanged;
            }
            Refresh();
        }
    }

    /// <summary>
    /// 🔥 新增：绑定到 IItemContainer（支持 ChestInventory）
    /// </summary>
    public void BindContainer(IItemContainer cont, int slotIndex)
    {
        // 清理旧绑定
        UnbindEvents();

        // 🔥 修复 Ⅰ：强制清空显示，避免显示旧数据
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        if (amountText != null)
        {
            amountText.text = "";
        }

        container = cont;
        inventory = cont as InventoryService; // 如果是 InventoryService，保留引用
        equipment = null;
        database = cont?.Database;
        index = slotIndex;
        isHotbar = false;

        if (isActiveAndEnabled)
        {
            if (container != null)
            {
                container.OnSlotChanged += OnSlotChanged;
            }
            Refresh();
        }
    }

    /// <summary>
    /// 清理事件绑定
    /// </summary>
    private void UnbindEvents()
    {
        if (container != null)
        {
            container.OnSlotChanged -= OnSlotChanged;
        }
        else if (inventory != null)
        {
            inventory.OnSlotChanged -= OnSlotChanged;
        }
    }

    void OnSlotChanged(int idx)
    {
        if (idx == index) Refresh();
    }

    public void Refresh()
    {
        if (container == null || database == null)
        {
            return;
        }

        var s = container.GetSlot(index);
        
        if (s.IsEmpty)
        {
            if (iconImage != null) UIItemIconScaler.SetIconWithAutoScale(iconImage, null, null);
            if (amountText != null) amountText.text = "";
        }
        else
        {
            var data = database.GetItemByID(s.itemId);
            if (iconImage != null)
            {
                UIItemIconScaler.SetIconWithAutoScale(iconImage, data?.GetBagSprite(), data);
            }
            if (amountText != null)
            {
                amountText.text = s.amount > 1 ? s.amount.ToString() : "";
            }
        }
    }
    #endregion
    
    #region 点击事件
    /// <summary>
    /// 基础点击功能 - 仅用于测试和选中槽位
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 🔥 P1：移除高频调用的日志输出（符合日志规范）
            // Toggle 会自动管理选中状态，不需要手动切换
        }
    }
    
    /// <summary>
    /// 选中此槽位（设置 Toggle.isOn = true）
    /// </summary>
    public void Select()
    {
        if (toggle != null)
        {
            toggle.isOn = true;
        }
    }
    
    /// <summary>
    /// 取消选中此槽位（设置 Toggle.isOn = false）
    /// </summary>
    public void Deselect()
    {
        if (toggle != null)
        {
            toggle.isOn = false;
        }
    }
    #endregion
}
