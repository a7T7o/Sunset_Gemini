using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmGame.Data;
using FarmGame.Data.Core;

/// <summary>
/// 背包槽位 UI - 基础版本
/// 只负责显示物品图标和数量
/// 实现基础的点击功能（选中槽位）
/// 与 ToolbarSlotUI 保持一致的简单设计
/// 
/// V2 新增：耐久度条显示
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text amountText;
    [SerializeField] private Image selectedOverlay;
    
    // 🔥 V2 新增：耐久度条
    private Image _durabilityBar;
    private Image _durabilityBarBg;

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
        
        // 🔥 V2 新增：创建耐久度条
        CreateDurabilityBar();
        
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
            // 隐藏耐久度条
            UpdateDurabilityBar(null);
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
            
            // 🔥 V2 新增：更新耐久度条
            // 尝试获取 InventoryItem 以读取耐久度
            InventoryItem invItem = null;
            if (inventory != null)
            {
                invItem = inventory.GetInventoryItem(index);
            }
            UpdateDurabilityBar(invItem);
        }
    }
    
    #region 耐久度条
    
    /// <summary>
    /// 创建耐久度条 UI（代码动态生成，无需美术资源）
    /// Rule: P2-1 耐久度条样式 - 距离底部 6px，贴着 4px 边框，加 1px 黑色描边
    /// </summary>
    private void CreateDurabilityBar()
    {
        // 检查是否已存在
        var existing = transform.Find("DurabilityBar");
        if (existing != null)
        {
            _durabilityBar = existing.GetComponent<Image>();
            var bgTransform = transform.Find("DurabilityBarBg");
            if (bgTransform != null) _durabilityBarBg = bgTransform.GetComponent<Image>();
            return;
        }
        
        // 🔥 P2-1：计算位置参数
        // 槽位边框 4px，耐久度条距离底部 6px
        // 使用像素偏移而非锚点百分比，确保精确定位
        float borderPx = 4f;
        float bottomPx = 6f;
        float barHeight = 4f; // 耐久度条高度
        
        // 创建背景条（深灰色 + 1px 黑色描边效果）
        var bgGo = new GameObject("DurabilityBarBg");
        bgGo.transform.SetParent(transform, false);
        _durabilityBarBg = bgGo.AddComponent<Image>();
        _durabilityBarBg.color = new Color(0.1f, 0.1f, 0.1f, 1f); // 黑色描边背景
        _durabilityBarBg.raycastTarget = false;
        
        var bgRt = (RectTransform)_durabilityBarBg.transform;
        // 使用绝对定位：左右贴着边框，底部距离 6px
        bgRt.anchorMin = new Vector2(0, 0);
        bgRt.anchorMax = new Vector2(1, 0);
        bgRt.pivot = new Vector2(0.5f, 0);
        // offsetMin.x = 左边距, offsetMin.y = 底部距离
        // offsetMax.x = -右边距, offsetMax.y = 底部距离 + 高度
        bgRt.offsetMin = new Vector2(borderPx, bottomPx - 1f); // -1 是描边
        bgRt.offsetMax = new Vector2(-borderPx, bottomPx + barHeight + 1f); // +1 是描边
        
        // 创建前景条（绿色）
        var barGo = new GameObject("DurabilityBar");
        barGo.transform.SetParent(transform, false);
        _durabilityBar = barGo.AddComponent<Image>();
        _durabilityBar.color = new Color(0.2f, 0.8f, 0.2f, 1f); // 绿色
        _durabilityBar.raycastTarget = false;
        
        var barRt = (RectTransform)_durabilityBar.transform;
        barRt.anchorMin = new Vector2(0, 0);
        barRt.anchorMax = new Vector2(1, 0);
        barRt.pivot = new Vector2(0, 0); // 左下角对齐，方便缩放
        // 前景条比背景条小 1px（描边效果）
        barRt.offsetMin = new Vector2(borderPx + 1f, bottomPx);
        barRt.offsetMax = new Vector2(-borderPx - 1f, bottomPx + barHeight);
        
        // 默认隐藏
        _durabilityBarBg.enabled = false;
        _durabilityBar.enabled = false;
    }
    
    /// <summary>
    /// 更新耐久度条显示
    /// Rule: P0-2 BoxUI 交互 - 支持从 IItemContainer 获取 InventoryItem
    /// Rule: P2-1 耐久度条样式 - 使用像素偏移控制宽度
    /// </summary>
    private void UpdateDurabilityBar(InventoryItem item)
    {
        if (_durabilityBar == null || _durabilityBarBg == null) return;
        
        // 🔥 修复：如果 item 为 null，尝试从 container 获取
        if (item == null && container != null)
        {
            // 尝试从 ChestInventoryV2 获取
            if (container is ChestInventoryV2 chestInv)
            {
                item = chestInv.GetItem(index);
            }
            // 尝试从 InventoryService 获取
            else if (container is InventoryService invService)
            {
                item = invService.GetInventoryItem(index);
            }
        }
        
        // 如果物品为空或没有耐久度，隐藏耐久度条
        if (item == null || !item.HasDurability)
        {
            _durabilityBarBg.enabled = false;
            _durabilityBar.enabled = false;
            return;
        }
        
        // 显示耐久度条
        _durabilityBarBg.enabled = true;
        _durabilityBar.enabled = true;
        
        // 计算耐久度百分比
        float percent = item.DurabilityPercent;
        
        // 🔥 P2-1：使用像素偏移控制宽度
        var rt = (RectTransform)_durabilityBar.transform;
        var bgRt = (RectTransform)_durabilityBarBg.transform;
        
        // 获取背景条的实际宽度（减去描边）
        float bgWidth = bgRt.rect.width - 2f; // 左右各 1px 描边
        float barWidth = bgWidth * percent;
        
        // 更新前景条的右边界
        // offsetMax.x 是相对于右锚点的偏移，负值表示向左收缩
        float borderPx = 4f;
        float rightOffset = -borderPx - 1f - (bgWidth - barWidth);
        rt.offsetMax = new Vector2(rightOffset, rt.offsetMax.y);
        
        // 根据耐久度百分比改变颜色
        // 100%-50%: 绿色 -> 黄色
        // 50%-0%: 黄色 -> 红色
        Color barColor;
        if (percent > 0.5f)
        {
            // 绿色到黄色
            float t = (percent - 0.5f) * 2f;
            barColor = Color.Lerp(Color.yellow, new Color(0.2f, 0.8f, 0.2f), t);
        }
        else
        {
            // 黄色到红色
            float t = percent * 2f;
            barColor = Color.Lerp(Color.red, Color.yellow, t);
        }
        _durabilityBar.color = barColor;
    }
    
    #endregion
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
