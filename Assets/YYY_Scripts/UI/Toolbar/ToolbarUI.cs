using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data;

public class ToolbarUI : MonoBehaviour
{
    [SerializeField] private InventoryService inventory;
    [SerializeField] private ItemDatabase database;
    [SerializeField] private Transform gridParent; // 包含12个 Bar_00_TG
    [SerializeField] private HotbarSelectionService selection;

    private readonly List<ToolbarSlotUI> slots = new List<ToolbarSlotUI>(InventoryService.HotbarWidth);
    private readonly List<Toggle> toggles = new List<Toggle>(InventoryService.HotbarWidth);

    void Awake()
    {
        if (gridParent == null) gridParent = transform;
    }

    void Start()
    {
        Build();
        // 初始同步一次选中高亮
        if (selection != null) HandleSelectedChanged(selection.selectedIndex);
    }

    public void Build()
    {
        slots.Clear();
        toggles.Clear();
        int index = 0;
        foreach (Transform child in gridParent)
        {
            if (index >= InventoryService.HotbarWidth) break;
            var slot = child.GetComponent<ToolbarSlotUI>();
            if (slot == null) slot = child.gameObject.AddComponent<ToolbarSlotUI>();
            slot.Bind(inventory, database, selection, index);
            slots.Add(slot);
            var tg = child.GetComponent<Toggle>();
            if (tg != null) toggles.Add(tg);
            index++;
        }
        // 若子物体不足12个，不报错，按现有数量绑定
        // 事件将在 OnEnable 中统一注册，避免重复
    }

    void OnEnable()
    {
        if (selection != null)
        {
            selection.OnSelectedChanged -= HandleSelectedChanged;
            selection.OnSelectedChanged += HandleSelectedChanged;
        }
    }

    void OnDisable()
    {
        if (selection != null)
        {
            selection.OnSelectedChanged -= HandleSelectedChanged;
        }
    }

    public void ForceRefresh()
    {
        foreach (var s in slots) s.Refresh();
    }

    void HandleSelectedChanged(int idx)
    {
        // 仅让每个Slot更新覆盖层，不再强制改 Toggle.isOn，避免白块/双触发
        foreach (var s in slots) s.RefreshSelection();
    }
}
