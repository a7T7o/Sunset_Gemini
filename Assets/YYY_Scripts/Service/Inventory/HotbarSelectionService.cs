using System;
using UnityEngine;
using FarmGame.Data;

public class HotbarSelectionService : MonoBehaviour
{
    public int selectedIndex = 0; // 0..11
    public event Action<int> OnSelectedChanged;

    [Header("装备系统引用")]
    [SerializeField] private PlayerToolController playerToolController;
    [SerializeField] private InventoryService inventory;

    private ItemDatabase database;

    void Awake()
    {
        // 自动查找引用
        if (playerToolController == null)
            playerToolController = FindFirstObjectByType<PlayerToolController>();
        if (inventory == null)
            inventory = FindFirstObjectByType<InventoryService>();
        
        // 从 InventoryService 获取 database 引用(ItemDatabase 是 ScriptableObject,不能用 Find)
        if (inventory != null)
            database = inventory.Database;
    }

    void OnEnable()
    {
        // 订阅背包变化事件，当物品变化时检查是否需要更新装备
        if (inventory != null)
        {
            inventory.OnSlotChanged += OnSlotChanged;
            inventory.OnHotbarSlotChanged += OnHotbarSlotChanged;
        }
    }

    void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnSlotChanged -= OnSlotChanged;
            inventory.OnHotbarSlotChanged -= OnHotbarSlotChanged;
        }
    }

    void Start()
    {
        // 游戏开始时装备当前选中槽位的工具
        EquipCurrentTool();
    }

    /// <summary>
    /// 当背包槽位变化时检查是否需要更新当前装备
    /// 处理场景：拾取物品到当前选中槽位时自动装备
    /// </summary>
    private void OnSlotChanged(int slotIndex)
    {
        // 只有当变化的槽位是当前选中的槽位时才更新装备
        if (slotIndex == selectedIndex)
        {
            EquipCurrentTool();
        }
    }

    /// <summary>
    /// 当快捷栏槽位变化时检查是否需要更新当前装备
    /// </summary>
    private void OnHotbarSlotChanged(int hotbarIndex)
    {
        // 只有当变化的快捷栏槽位是当前选中的槽位时才更新装备
        if (hotbarIndex == selectedIndex)
        {
            EquipCurrentTool();
        }
    }

    public void SelectIndex(int index)
    {
        int clamped = Mathf.Clamp(index, 0, InventoryService.HotbarWidth - 1);
        if (clamped == selectedIndex) return;
        selectedIndex = clamped;
        
        // 选中变化时立即装备工具
        EquipCurrentTool();
        
        OnSelectedChanged?.Invoke(selectedIndex);
    }

    public void SelectNext()
    {
        int next = (selectedIndex + 1) % InventoryService.HotbarWidth;
        SelectIndex(next);
    }

    public void SelectPrev()
    {
        int prev = (selectedIndex - 1 + InventoryService.HotbarWidth) % InventoryService.HotbarWidth;
        SelectIndex(prev);
    }

    /// <summary>
    /// 装备当前选中槽位的工具
    /// </summary>
    private void EquipCurrentTool()
    {
        if (playerToolController == null || inventory == null || database == null)
            return;

        var slot = inventory.GetSlot(selectedIndex);
        
        // 空槽位时清除当前装备并退出放置模式
        if (slot.IsEmpty)
        {
            playerToolController.UnequipCurrent();
            ExitPlacementModeIfActive();
            return;
        }

        var itemData = database.GetItemByID(slot.itemId);
        if (itemData == null) return;

        // ★ 检查是否是可放置物品
        Debug.Log($"<color=cyan>[HotbarSelectionService] 选中物品: {itemData.itemName}, isPlaceable={itemData.isPlaceable}, placementType={itemData.placementType}</color>");
        
        if (itemData.isPlaceable)
        {
            // 进入放置模式
            playerToolController.UnequipCurrent();
            // 优先使用 V3 → V2 → V1
            Debug.Log($"<color=yellow>[HotbarSelectionService] PlacementManagerV3.Instance={PlacementManagerV3.Instance != null}, PlacementManagerV2.Instance={PlacementManagerV2.Instance != null}, PlacementManager.Instance={PlacementManager.Instance != null}</color>");
            
            if (PlacementManagerV3.Instance != null)
            {
                Debug.Log($"<color=green>[HotbarSelectionService] 调用 PlacementManagerV3.EnterPlacementMode</color>");
                PlacementManagerV3.Instance.EnterPlacementMode(itemData, slot.quality);
            }
            else if (PlacementManagerV2.Instance != null)
            {
                Debug.Log($"<color=green>[HotbarSelectionService] 调用 PlacementManagerV2.EnterPlacementMode</color>");
                PlacementManagerV2.Instance.EnterPlacementMode(itemData, slot.quality);
            }
            else if (PlacementManager.Instance != null)
            {
                Debug.Log($"<color=green>[HotbarSelectionService] 调用 PlacementManager.EnterPlacementMode</color>");
                PlacementManager.Instance.EnterPlacementMode(itemData, slot.quality);
            }
            else
            {
                Debug.LogError("[HotbarSelectionService] 没有找到任何 PlacementManager！");
            }
            return;
        }

        // 非放置物品，退出放置模式
        ExitPlacementModeIfActive();

        // 每个品质的工具都是独立 ID，直接使用 itemID 匹配动画
        if (itemData is ToolData toolData)
            playerToolController.EquipToolData(toolData);
        else if (itemData is WeaponData weaponData)
            playerToolController.EquipWeaponData(weaponData);
    }

    /// <summary>
    /// 如果处于放置模式则退出
    /// </summary>
    private void ExitPlacementModeIfActive()
    {
        // 优先检查 V3 → V2 → V1
        if (PlacementManagerV3.Instance != null && PlacementManagerV3.Instance.IsPlacementMode)
        {
            PlacementManagerV3.Instance.ExitPlacementMode();
            return;
        }
        if (PlacementManagerV2.Instance != null && PlacementManagerV2.Instance.IsPlacementMode)
        {
            PlacementManagerV2.Instance.ExitPlacementMode();
            return;
        }
        if (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacementMode)
        {
            PlacementManager.Instance.ExitPlacementMode();
        }
    }
}
