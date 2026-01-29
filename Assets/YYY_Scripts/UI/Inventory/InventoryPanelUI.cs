using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data;

/// <summary>
/// 背包面板 UI - 简化版，与 ToolbarUI 保持一致的设计
/// </summary>
public class InventoryPanelUI : MonoBehaviour
{
    [Header("Services & DB")]
    [SerializeField] private InventoryService inventory;
    [SerializeField] private EquipmentService equipment;
    [SerializeField] private ItemDatabase database;
    [SerializeField] private HotbarSelectionService selection;

    [Header("Layout")]
    [SerializeField] private Transform upParent;   // 36格：0..35，其中0..11为Hotbar映射
    [SerializeField] private Transform downParent; // 6格装备栏：0..5

    [Header("Limits")] 
    [SerializeField] private int upCount = 36;  // ★ 背包有 36 格（0-35）
    [SerializeField] private int downCount = 6;

    void Awake()
    {
        if (inventory == null) inventory = FindFirstObjectByType<InventoryService>();
        if (equipment == null) equipment = FindFirstObjectByType<EquipmentService>();
        // ItemDatabase 是 ScriptableObject，不能用 FindFirstObjectByType
        // 必须从 InventoryService 获取
        if (database == null && inventory != null) database = inventory.Database;
        if (selection == null) selection = FindFirstObjectByType<HotbarSelectionService>();
    }

    void Start()
    {
        BuildUpSlots();
        BuildDownSlots();
    }
    
    /// <summary>
    /// Rule: P1-1 背包刷新 - 每次面板激活时强制刷新
    /// </summary>
    void OnEnable()
    {
        // 🔥 P1-1 修复：每次面板激活时强制刷新
        // 确保从 BoxUI 切换回来时数据是最新的
        RefreshAll();
    }

    public void BuildUpSlots()
    {
        if (upParent == null)
        {
            Debug.LogError("[InventoryPanelUI] BuildUpSlots: upParent 为 null!");
            return;
        }
        if (inventory == null)
        {
            Debug.LogError("[InventoryPanelUI] BuildUpSlots: inventory 为 null!");
            return;
        }
        if (database == null)
        {
            Debug.LogError("[InventoryPanelUI] BuildUpSlots: database 为 null!");
            return;
        }
        
        int n = Mathf.Min(upCount, upParent.childCount);
        for (int i = 0; i < n; i++)
        {
            var child = upParent.GetChild(i);
            var slot = child.GetComponent<InventorySlotUI>();
            if (slot == null) slot = child.gameObject.AddComponent<InventorySlotUI>();
            bool isHotbar = i < InventoryService.HotbarWidth;
            slot.Bind(inventory, equipment, database, i, isHotbar);
        }
    }

    public void BuildDownSlots()
    {
        if (downParent == null || equipment == null) return;
        int n = Mathf.Min(downCount, downParent.childCount);
        for (int i = 0; i < n; i++)
        {
            var child = downParent.GetChild(i);
            var slot = child.GetComponent<EquipmentSlotUI>();
            if (slot == null) slot = child.gameObject.AddComponent<EquipmentSlotUI>();
            slot.Bind(equipment, inventory, database, i);
        }
    }

    // 在面板首次从未激活→激活时调用，确保所有格子已构建与绑定
    public void EnsureBuilt()
    {
        // 确保引用已初始化
        if (inventory == null) inventory = FindFirstObjectByType<InventoryService>();
        if (equipment == null) equipment = FindFirstObjectByType<EquipmentService>();
        if (database == null && inventory != null) database = inventory.Database;
        
        BuildUpSlots();
        BuildDownSlots();
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (upParent != null)
        {
            int n = Mathf.Min(upCount, upParent.childCount);
            for (int i = 0; i < n; i++)
            {
                var slot = upParent.GetChild(i).GetComponent<InventorySlotUI>();
                if (slot != null) slot.Refresh();
            }
        }
        if (downParent != null)
        {
            int n = Mathf.Min(downCount, downParent.childCount);
            for (int i = 0; i < n; i++)
            {
                var slot = downParent.GetChild(i).GetComponent<EquipmentSlotUI>();
                if (slot != null) slot.Refresh();
            }
        }
    }

    private void TryApplyHotbarSelectionToUp()
    {
        if (selection == null || upParent == null) return;
        int idx = Mathf.Clamp(selection.selectedIndex, 0, Mathf.Min(InventoryService.HotbarWidth, upParent.childCount) - 1);
        for (int i = 0; i < Mathf.Min(upCount, upParent.childCount); i++)
        {
            var tg = upParent.GetChild(i).GetComponent<Toggle>();
            if (tg != null) tg.isOn = (i == idx);
        }
    }

    private void ClearDownSelection()
    {
        if (downParent == null) return;
        for (int i = 0; i < Mathf.Min(downCount, downParent.childCount); i++)
        {
            var tg = downParent.GetChild(i).GetComponent<Toggle>();
            if (tg != null) tg.isOn = false;
        }
    }

    // 由 PackagePanelTabsUI 在"主面板从关闭→打开"时调用
    public void ResetSelectionsOnPanelOpen()
    {
        TryApplyHotbarSelectionToUp();
        ClearDownSelection();
    }
    
    /// <summary>
    /// 清空背包槽位（Up 区域）的所有选中状态
    /// 供 Sort 后调用
    /// </summary>
    public void ClearUpSelection()
    {
        if (upParent == null) return;
        
        // 方案 1：通过 ToggleGroup 清空
        var toggleGroup = upParent.GetComponent<ToggleGroup>();
        if (toggleGroup != null)
        {
            toggleGroup.SetAllTogglesOff();
            return;
        }
        
        // 方案 2：回退到遍历槽位
        int n = Mathf.Min(upCount, upParent.childCount);
        for (int i = 0; i < n; i++)
        {
            var tg = upParent.GetChild(i).GetComponent<Toggle>();
            if (tg != null) tg.isOn = false;
        }
    }
}
