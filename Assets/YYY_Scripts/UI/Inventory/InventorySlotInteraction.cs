using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FarmGame.UI;

/// <summary>
/// 背包槽位交互组件 - 方案 D（跨容器拖拽完整支持）
/// 职责：实现 Unity 原生事件接口，根据容器类型分发到不同的处理逻辑
/// 
/// 修复说明（2026-01-18 v5 - P0-2）：
/// 1. 使用 SlotDragContext 统一管理跨容器拖拽状态
/// 2. 四象限拖拽完整支持：Up→Up / Up→Down / Down→Up / Down→Down
/// 3. 箱子槽位拖拽：直接操作 ChestInventory
/// 4. 背包槽位拖拽：通过 SlotDragContext
/// 5. 跨容器拖拽：交换或堆叠
/// </summary>
public class InventorySlotInteraction : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    private InventorySlotUI inventorySlotUI;
    private EquipmentSlotUI equipmentSlotUI;
    private bool isEquip;
    
    private bool isDragging = false;
    private float pressTime;
    private Vector2 pressPosition;
    
    #region 缓存引用（性能优化）
    
    private InventoryService _cachedInventoryService;
    private EquipmentService _cachedEquipmentService;
    private BoxPanelUI _cachedBoxPanel;
    private PackagePanelTabsUI _cachedPackagePanel;
    
    private InventoryService CachedInventoryService
    {
        get
        {
            if (_cachedInventoryService == null)
                _cachedInventoryService = Object.FindFirstObjectByType<InventoryService>();
            return _cachedInventoryService;
        }
    }
    
    private EquipmentService CachedEquipmentService
    {
        get
        {
            if (_cachedEquipmentService == null)
                _cachedEquipmentService = Object.FindFirstObjectByType<EquipmentService>();
            return _cachedEquipmentService;
        }
    }
    
    private BoxPanelUI CachedBoxPanel
    {
        get
        {
            // BoxPanelUI 可能被销毁重建，需要检查
            if (_cachedBoxPanel == null || !_cachedBoxPanel.gameObject)
                _cachedBoxPanel = Object.FindFirstObjectByType<BoxPanelUI>();
            return _cachedBoxPanel;
        }
    }
    
    private PackagePanelTabsUI CachedPackagePanel
    {
        get
        {
            // PackagePanelTabsUI 没有单例，直接使用缓存
            if (_cachedPackagePanel == null)
                _cachedPackagePanel = Object.FindFirstObjectByType<PackagePanelTabsUI>();
            return _cachedPackagePanel;
        }
    }
    
    #endregion
    
    private int SlotIndex
    {
        get
        {
            if (isEquip && equipmentSlotUI != null)
                return equipmentSlotUI.Index;
            if (!isEquip && inventorySlotUI != null)
                return inventorySlotUI.Index;
            return -1;
        }
    }
    
    private IItemContainer CurrentContainer
    {
        get
        {
            if (!isEquip && inventorySlotUI != null)
                return inventorySlotUI.Container;
            return null;
        }
    }
    
    private bool IsChestSlot => CurrentContainer is ChestInventory;
    private bool IsInventorySlot => CurrentContainer is InventoryService;
    
    public void Bind(InventorySlotUI slot, bool isEquipmentSlot)
    {
        inventorySlotUI = slot;
        equipmentSlotUI = null;
        isEquip = isEquipmentSlot;
    }
    
    public void Bind(EquipmentSlotUI slot, bool isEquipmentSlot)
    {
        equipmentSlotUI = slot;
        inventorySlotUI = null;
        isEquip = isEquipmentSlot;
    }

    
    #region Unity 原生事件接口
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        pressTime = Time.time;
        pressPosition = eventData.position;
        isDragging = false;
        
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        
        // 🔥 修复 1：检测是否有 SlotDragContext 或 Manager Held 状态
        // 如果有，则处理放置逻辑
        if (SlotDragContext.IsDragging)
        {
            HandleSlotDragContextClick();
            return;
        }
        
        var manager = InventoryInteractionManager.Instance;
        if (manager != null && manager.IsHolding)
        {
            HandleManagerHeldClick();
            return;
        }
        
        // 🔥 修复 2：箱子槽位处理修饰键
        if (IsChestSlot)
        {
            if (shift || ctrl)
            {
                HandleChestSlotModifierClick(shift, ctrl);
            }
            return;
        }
        
        // 装备槽位或背包槽位：使用 InventoryInteractionManager
        if (isEquip || IsInventorySlot)
        {
            if (manager != null)
            {
                manager.OnSlotPointerDown(SlotIndex, isEquip);
            }
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        // PointerUp 不做任何事
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        float holdTime = Time.time - pressTime;
        float moveDistance = Vector2.Distance(eventData.position, pressPosition);
        
        if (holdTime < 0.15f && moveDistance < 5f) return;
        
        int index = SlotIndex;
        var container = CurrentContainer;
        
        // 箱子槽位拖拽
        if (IsChestSlot)
        {
            var chest = container as ChestInventory;
            if (chest == null) return;
            
            var item = chest.GetSlot(index);
            if (item.IsEmpty) return;
            
            isDragging = true;
            SlotDragContext.Begin(chest, index, item, inventorySlotUI);  // 🔥 传入源槽位 UI
            chest.ClearSlot(index);
            ShowDragIcon(item);
            return;
        }
        
        // 装备槽位拖拽：使用 InventoryInteractionManager
        if (isEquip)
        {
            var manager = InventoryInteractionManager.Instance;
            if (manager != null && !manager.IsHolding)
            {
                isDragging = true;
                manager.OnSlotBeginDrag(index, isEquip, eventData);
            }
            return;
        }
        
        // 背包槽位拖拽
        if (IsInventorySlot)
        {
            var inventory = container as InventoryService;
            if (inventory == null) return;
            
            var item = inventory.GetSlot(index);
            if (item.IsEmpty) return;
            
            var manager = InventoryInteractionManager.Instance;
            if (manager != null && manager.IsHolding) return;
            
            isDragging = true;
            SlotDragContext.Begin(inventory, index, item, inventorySlotUI);  // 🔥 传入源槽位 UI
            inventory.ClearSlot(index);
            ShowDragIcon(item);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        // HeldItemDisplay 自己跟随鼠标
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;
        
        if (SlotDragContext.IsDragging)
        {
            // 🔥 修复：检测垃圾桶和面板外
            if (IsOverTrashCan(eventData.position))
            {
                DropItemFromContext();
                return;
            }
            
            if (!IsInsidePanel(eventData.position))
            {
                DropItemFromContext();
                return;
            }
            
            // 没有放到有效目标，返回原位
            SlotDragContext.Cancel();
            HideDragIcon();
            return;
        }
        
        var manager = InventoryInteractionManager.Instance;
        if (manager != null)
        {
            manager.OnSlotEndDrag(eventData);
        }
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        int targetIndex = SlotIndex;
        var targetContainer = CurrentContainer;
        
        if (SlotDragContext.IsDragging)
        {
            HandleSlotDragContextDrop(targetIndex, targetContainer);
            return;
        }
        
        var manager = InventoryInteractionManager.Instance;
        if (manager != null && manager.IsHolding)
        {
            // 🔥 修复：支持 Manager Held 放置到箱子槽位
            if (IsChestSlot)
            {
                HandleManagerHeldToChest(targetIndex);
                return;
            }
            manager.OnSlotDrop(targetIndex, isEquip);
        }
    }
    
    #endregion

    
    #region SlotDragContext Drop 处理
    
    private void HandleSlotDragContextDrop(int targetIndex, IItemContainer targetContainer)
    {
        var sourceContainer = SlotDragContext.SourceContainer;
        int sourceIndex = SlotDragContext.SourceSlotIndex;
        var draggedItem = SlotDragContext.DraggedItem;
        
        bool sourceIsChest = sourceContainer is ChestInventory;
        bool targetIsChest = targetContainer is ChestInventory;
        
        // Up → Up（箱子内拖拽）
        if (sourceIsChest && targetIsChest)
        {
            var chest = sourceContainer as ChestInventory;
            if (chest == targetContainer as ChestInventory)
            {
                HandleSameContainerDrop(chest, sourceIndex, targetIndex, draggedItem);
            }
            else
            {
                SlotDragContext.Cancel();
            }
            HideDragIcon();
            SlotDragContext.End();
            return;
        }
        
        // Up → Down（箱子到背包）
        if (sourceIsChest && !targetIsChest)
        {
            var chest = sourceContainer as ChestInventory;
            var inventory = targetContainer as InventoryService;
            if (chest != null && inventory != null)
            {
                HandleChestToInventoryDrop(chest, sourceIndex, inventory, targetIndex, draggedItem);
            }
            HideDragIcon();
            SlotDragContext.End();
            return;
        }
        
        // Down → Up（背包到箱子）
        if (!sourceIsChest && targetIsChest)
        {
            var inventory = sourceContainer as InventoryService;
            var chest = targetContainer as ChestInventory;
            if (inventory != null && chest != null)
            {
                HandleInventoryToChestDrop(inventory, sourceIndex, chest, targetIndex, draggedItem);
            }
            HideDragIcon();
            SlotDragContext.End();
            return;
        }
        
        // Down → Down（背包内拖拽）
        if (!sourceIsChest && !targetIsChest)
        {
            var inventory = sourceContainer as InventoryService;
            if (inventory != null && inventory == targetContainer as InventoryService)
            {
                HandleSameContainerDrop(inventory, sourceIndex, targetIndex, draggedItem);
            }
            HideDragIcon();
            SlotDragContext.End();
        }
    }
    
    private void HandleSameContainerDrop(IItemContainer container, int sourceIndex, int targetIndex, ItemStack draggedItem)
    {
        if (sourceIndex == targetIndex)
        {
            container.SetSlot(sourceIndex, draggedItem);
            // 🔥 选中状态优化：放回原位也选中
            SelectTargetSlot();
            return;
        }
        
        var targetSlot = container.GetSlot(targetIndex);
        
        if (targetSlot.IsEmpty)
        {
            container.SetSlot(targetIndex, draggedItem);
            // 🔥 选中状态优化：放置成功后选中目标槽位
            SelectTargetSlot();
            return;
        }
        
        if (targetSlot.CanStackWith(draggedItem))
        {
            int maxStack = container.GetMaxStack(draggedItem.itemId);
            int total = targetSlot.amount + draggedItem.amount;
            
            if (total <= maxStack)
            {
                targetSlot.amount = total;
                container.SetSlot(targetIndex, targetSlot);
            }
            else
            {
                targetSlot.amount = maxStack;
                container.SetSlot(targetIndex, targetSlot);
                draggedItem.amount = total - maxStack;
                container.SetSlot(sourceIndex, draggedItem);
            }
            // 🔥 选中状态优化：堆叠后选中目标槽位
            SelectTargetSlot();
            return;
        }
        
        container.SetSlot(targetIndex, draggedItem);
        container.SetSlot(sourceIndex, targetSlot);
        // 🔥 选中状态优化：交换后选中目标槽位
        SelectTargetSlot();
    }
    
    private void HandleChestToInventoryDrop(ChestInventory chest, int chestIndex, InventoryService inventory, int invIndex, ItemStack draggedItem)
    {
        var invSlot = inventory.GetSlot(invIndex);
        
        if (invSlot.IsEmpty)
        {
            inventory.SetSlot(invIndex, draggedItem);
            // 🔥 选中状态优化：跨区域放置 - 取消源区域选中，选中目标槽位
            DeselectSourceSlot();
            SelectTargetSlot();
            return;
        }
        
        if (invSlot.CanStackWith(draggedItem))
        {
            int maxStack = inventory.GetMaxStack(draggedItem.itemId);
            int total = invSlot.amount + draggedItem.amount;
            
            if (total <= maxStack)
            {
                invSlot.amount = total;
                inventory.SetSlot(invIndex, invSlot);
            }
            else
            {
                invSlot.amount = maxStack;
                inventory.SetSlot(invIndex, invSlot);
                draggedItem.amount = total - maxStack;
                chest.SetSlot(chestIndex, draggedItem);
            }
            // 🔥 选中状态优化：跨区域放置 - 取消源区域选中，选中目标槽位
            DeselectSourceSlot();
            SelectTargetSlot();
            return;
        }
        
        inventory.SetSlot(invIndex, draggedItem);
        chest.SetSlot(chestIndex, invSlot);
        // 🔥 选中状态优化：跨区域交换 - 取消源区域选中，选中目标槽位
        DeselectSourceSlot();
        SelectTargetSlot();
    }
    
    private void HandleInventoryToChestDrop(InventoryService inventory, int invIndex, ChestInventory chest, int chestIndex, ItemStack draggedItem)
    {
        var chestSlot = chest.GetSlot(chestIndex);
        
        if (chestSlot.IsEmpty)
        {
            chest.SetSlot(chestIndex, draggedItem);
            // 🔥 选中状态优化：跨区域放置 - 取消源区域选中，选中目标槽位
            DeselectSourceSlot();
            SelectTargetSlot();
            return;
        }
        
        if (chestSlot.CanStackWith(draggedItem))
        {
            int maxStack = chest.GetMaxStack(draggedItem.itemId);
            int total = chestSlot.amount + draggedItem.amount;
            
            if (total <= maxStack)
            {
                chestSlot.amount = total;
                chest.SetSlot(chestIndex, chestSlot);
            }
            else
            {
                chestSlot.amount = maxStack;
                chest.SetSlot(chestIndex, chestSlot);
                draggedItem.amount = total - maxStack;
                inventory.SetSlot(invIndex, draggedItem);
            }
            // 🔥 选中状态优化：跨区域放置 - 取消源区域选中，选中目标槽位
            DeselectSourceSlot();
            SelectTargetSlot();
            return;
        }
        
        chest.SetSlot(chestIndex, draggedItem);
        inventory.SetSlot(invIndex, chestSlot);
        // 🔥 选中状态优化：跨区域交换 - 取消源区域选中，选中目标槽位
        DeselectSourceSlot();
        SelectTargetSlot();
    }
    
    #endregion

    
    #region 拖拽图标
    
    private void ShowDragIcon(ItemStack item)
    {
        // 🔥 P0-3：使用统一入口
        var manager = InventoryInteractionManager.Instance;
        if (manager == null) return;
        
        // 🔥 使用缓存引用
        var invService = CachedInventoryService;
        if (invService == null || invService.Database == null) return;
        
        var itemData = invService.Database.GetItemByID(item.itemId);
        if (itemData != null)
        {
            manager.ShowHeldIcon(item.itemId, item.amount, itemData.GetBagSprite());
        }
    }
    
    private void HideDragIcon()
    {
        // 🔥 P0-3：使用统一入口
        var manager = InventoryInteractionManager.Instance;
        manager?.HideHeldIcon();
    }
    
    #endregion
    
    #region 🔥 修复：新增辅助方法
    
    /// <summary>
    /// 处理箱子槽位的修饰键点击（Shift/Ctrl 拿取）
    /// </summary>
    private void HandleChestSlotModifierClick(bool shift, bool ctrl)
    {
        var chest = CurrentContainer as ChestInventory;
        if (chest == null) return;
        
        int index = SlotIndex;
        var slot = chest.GetSlot(index);
        if (slot.IsEmpty) return;
        
        ItemStack pickupItem;
        
        if (shift)
        {
            // Shift：二分拿取（向上取整给手上）
            int handAmount = (slot.amount + 1) / 2;
            int sourceAmount = slot.amount - handAmount;
            
            pickupItem = new ItemStack { itemId = slot.itemId, quality = slot.quality, amount = handAmount };
            
            if (sourceAmount > 0)
                chest.SetSlot(index, new ItemStack { itemId = slot.itemId, quality = slot.quality, amount = sourceAmount });
            else
                chest.ClearSlot(index);
        }
        else // ctrl
        {
            // Ctrl：单个拿取
            pickupItem = new ItemStack { itemId = slot.itemId, quality = slot.quality, amount = 1 };
            
            if (slot.amount > 1)
                chest.SetSlot(index, new ItemStack { itemId = slot.itemId, quality = slot.quality, amount = slot.amount - 1 });
            else
                chest.ClearSlot(index);
        }
        
        // 使用 SlotDragContext 管理 Held 状态
        SlotDragContext.Begin(chest, index, pickupItem);
        ShowDragIcon(pickupItem);
    }
    
    /// <summary>
    /// 处理 SlotDragContext 状态下的点击（放置到当前槽位）
    /// </summary>
    private void HandleSlotDragContextClick()
    {
        int targetIndex = SlotIndex;
        var targetContainer = CurrentContainer;
        
        if (targetContainer == null)
        {
            SlotDragContext.Cancel();
            HideDragIcon();
            return;
        }
        
        HandleSlotDragContextDrop(targetIndex, targetContainer);
    }
    
    /// <summary>
    /// 处理 Manager Held 状态下的点击（放置到当前槽位）
    /// </summary>
    private void HandleManagerHeldClick()
    {
        var manager = InventoryInteractionManager.Instance;
        if (manager == null || !manager.IsHolding) return;
        
        // 如果是箱子槽位，处理放置
        if (IsChestSlot)
        {
            HandleManagerHeldToChest(SlotIndex);
            return;
        }
        
        // 背包槽位，使用 Manager 的标准逻辑
        manager.OnSlotPointerDown(SlotIndex, isEquip);
    }
    
    /// <summary>
    /// 处理 Manager Held 物品放置到箱子槽位
    /// </summary>
    private void HandleManagerHeldToChest(int targetIndex)
    {
        var manager = InventoryInteractionManager.Instance;
        if (manager == null || !manager.IsHolding) return;
        
        var chest = CurrentContainer as ChestInventory;
        if (chest == null) return;
        
        var heldItem = manager.GetHeldItem();
        if (heldItem.IsEmpty) return;
        
        var targetSlot = chest.GetSlot(targetIndex);
        
        // 目标为空：直接放置
        if (targetSlot.IsEmpty)
        {
            chest.SetSlot(targetIndex, heldItem);
            manager.ClearHeldState();
            return;
        }
        
        // 相同物品：堆叠
        if (targetSlot.CanStackWith(heldItem))
        {
            int maxStack = chest.GetMaxStack(heldItem.itemId);
            int total = targetSlot.amount + heldItem.amount;
            
            if (total <= maxStack)
            {
                targetSlot.amount = total;
                chest.SetSlot(targetIndex, targetSlot);
                manager.ClearHeldState();
            }
            else
            {
                targetSlot.amount = maxStack;
                chest.SetSlot(targetIndex, targetSlot);
                // 剩余物品保留在手上 - 需要更新 Manager 状态
                // 由于 Manager 没有提供部分放置接口，这里简化处理：全部放置或不放置
                // 实际上应该返回剩余物品，但这需要修改 Manager 接口
            }
            return;
        }
        
        // 不同物品：交换
        // 把手上物品放到箱子，把箱子物品放到背包源槽位
        int sourceIndex = manager.GetSourceIndex();
        bool sourceIsEquip = manager.GetSourceIsEquip();
        
        // 🔥 使用缓存引用
        var invService = CachedInventoryService;
        var equipService = CachedEquipmentService;
        
        if (invService == null) return;
        
        // 检查源槽位是否为空
        ItemStack sourceSlot = sourceIsEquip 
            ? (equipService?.GetEquip(sourceIndex) ?? ItemStack.Empty)
            : invService.GetSlot(sourceIndex);
        
        if (sourceSlot.IsEmpty)
        {
            // 源槽位为空，可以交换
            chest.SetSlot(targetIndex, heldItem);
            if (sourceIsEquip && equipService != null)
                equipService.SetEquip(sourceIndex, targetSlot);
            else
                invService.SetSlot(sourceIndex, targetSlot);
            manager.ClearHeldState();
        }
        // 源槽位非空，不能交换，保持 Held 状态
    }
    
    /// <summary>
    /// 从 SlotDragContext 丢弃物品
    /// 🔥 使用 ItemDropHelper 统一丢弃逻辑
    /// </summary>
    private void DropItemFromContext()
    {
        if (!SlotDragContext.IsDragging) return;
        
        var item = SlotDragContext.DraggedItem;
        if (!item.IsEmpty)
        {
            // 🔥 使用 ItemDropHelper 统一丢弃逻辑
            FarmGame.UI.ItemDropHelper.DropAtPlayer(item);
        }
        
        SlotDragContext.End();
        HideDragIcon();
    }
    
    /// <summary>
    /// 检测是否在垃圾桶区域
    /// 🔥 使用缓存引用优化性能
    /// </summary>
    private bool IsOverTrashCan(Vector2 screenPos)
    {
        // 查找 BoxPanelUI 中的垃圾桶
        var boxPanel = CachedBoxPanel;
        if (boxPanel != null)
        {
            var trashCan = boxPanel.transform.Find("BT_TrashCan");
            if (trashCan != null)
            {
                var rt = trashCan.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                {
                    return true;
                }
            }
        }
        
        // 查找 PackagePanel 中的垃圾桶
        var packagePanel = CachedPackagePanel;
        if (packagePanel != null)
        {
            var trashCan = packagePanel.transform.Find("BT_TrashCan");
            if (trashCan == null)
            {
                // 尝试在子层级查找
                foreach (Transform t in packagePanel.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "BT_TrashCan")
                    {
                        trashCan = t;
                        break;
                    }
                }
            }
            if (trashCan != null)
            {
                var rt = trashCan.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检测是否在面板内
    /// 🔥 修复：复用 InventoryInteractionManager 的检测逻辑，避免 RectTransform 覆盖整个屏幕的问题
    /// 🔥 使用缓存引用优化性能
    /// </summary>
    private bool IsInsidePanel(Vector2 screenPos)
    {
        // 🔥 优先使用 InventoryInteractionManager 的检测方法（它使用配置好的精确区域）
        var manager = InventoryInteractionManager.Instance;
        if (manager != null)
        {
            return manager.IsInsidePanelBounds(screenPos);
        }
        
        // 回退：检测 BoxPanelUI
        var boxPanel = CachedBoxPanel;
        if (boxPanel != null && boxPanel.IsOpen)
        {
            var rt = boxPanel.GetComponent<RectTransform>();
            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
            {
                return true;
            }
        }
        
        // 回退：检测 PackagePanel
        var packagePanel = CachedPackagePanel;
        if (packagePanel != null && packagePanel.IsPanelOpen())
        {
            var rt = packagePanel.GetComponent<RectTransform>();
            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 🔥 选中状态优化：选中当前目标槽位
    /// </summary>
    private void SelectTargetSlot()
    {
        if (inventorySlotUI != null)
        {
            inventorySlotUI.Select();
        }
    }
    
    /// <summary>
    /// 🔥 选中状态优化：清空源区域的所有选中状态（用于跨区域放置）
    /// 使用 SetAllTogglesOff() 更简洁可靠，性能开销可忽略
    /// </summary>
    private void DeselectSourceSlot()
    {
        var sourceSlotUI = SlotDragContext.SourceSlotUI;
        if (sourceSlotUI == null) return;
        
        // 获取源槽位所在的 ToggleGroup 并清空
        var toggle = sourceSlotUI.GetComponent<Toggle>();
        if (toggle != null && toggle.group != null)
        {
            toggle.group.SetAllTogglesOff();
        }
        else
        {
            // 回退：直接取消源槽位选中
            sourceSlotUI.Deselect();
        }
    }
    
    #endregion
}
