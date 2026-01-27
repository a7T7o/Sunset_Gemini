using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmGame.Data;

/// <summary>
/// 装备槽位 UI - 基础版本
/// 只负责显示装备图标和数量
/// 实现基础的点击功能
/// 与 InventorySlotUI 保持一致的简单设计
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text amountText;

    private EquipmentService equipment;
    private InventoryService inventory;
    private ItemDatabase database;
    private int index; // 0..5

    /// <summary>
    /// 槽位索引（供外部查询）
    /// </summary>
    public int Index => index;

    #region Unity 生命周期
    void Awake()
    {
        // ★ 关键：确保槽位本身有可以接收射线的 Image
        var bgImage = GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.raycastTarget = true;
        }
        
        if (iconImage == null)
        {
            var t = transform.Find("Icon");
            if (t != null)
            {
                iconImage = t.GetComponent<Image>();
            }
            else
            {
                // 自动创建 Icon
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
        if (amountText == null)
        {
            var t = transform.Find("Amount");
            if (t != null)
            {
                amountText = t.GetComponent<Text>();
            }
            else
            {
                // 自动创建 Amount
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
        
        // ★ 方案 D：自动添加 Interaction 组件
        var interaction = gameObject.GetComponent<InventorySlotInteraction>();
        if (interaction == null)
        {
            interaction = gameObject.AddComponent<InventorySlotInteraction>();
        }
        interaction.Bind(this, true);
    }

    void OnEnable()
    {
        if (equipment != null) equipment.OnEquipSlotChanged += HandleEquipChanged;
        Refresh();
    }

    void OnDisable()
    {
        if (equipment != null) equipment.OnEquipSlotChanged -= HandleEquipChanged;
    }
    #endregion

    #region 绑定和刷新
    public void Bind(EquipmentService equip, InventoryService inv, ItemDatabase db, int equipIndex)
    {
        equipment = equip;
        inventory = inv;
        database = db;
        index = equipIndex;
        if (isActiveAndEnabled)
        {
            OnDisable();
            OnEnable();
        }
        else
        {
            Refresh();
        }
    }

    void HandleEquipChanged(int changed)
    {
        if (changed == index) Refresh();
    }

    public void Refresh()
    {
        if (equipment == null || database == null) return;
        var s = equipment.GetEquip(index);
        if (s.IsEmpty)
        {
            if (iconImage != null) UIItemIconScaler.SetIconWithAutoScale(iconImage, null, null);
            if (amountText != null) amountText.text = "";
            return;
        }
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
    #endregion
    
    #region 点击事件
    /// <summary>
    /// 基础点击功能 - 仅用于测试
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log($"[EquipmentSlotUI] 点击装备槽位 {index}");
        }
    }
    #endregion
}
