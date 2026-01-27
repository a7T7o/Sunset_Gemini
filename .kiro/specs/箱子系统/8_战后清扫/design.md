# 箱子系统战后清扫 - 设计文档

**创建日期**: 2026-01-19

---

## 一、问题根因

### 问题 1：垃圾桶拖拽

```csharp
// InventorySlotInteraction.OnEndDrag
public void OnEndDrag(PointerEventData eventData)
{
    if (SlotDragContext.IsDragging)
    {
        SlotDragContext.Cancel();  // ← 直接返回原位，没有检测垃圾桶
        HideDragIcon();
        return;
    }
}
```

### 问题 2：Up 区域修饰键

```csharp
// InventorySlotInteraction.OnPointerDown
public void OnPointerDown(PointerEventData eventData)
{
    if (IsChestSlot) return;  // ← 直接返回，没有处理修饰键
}
```

### 问题 3：Down→Up 放置

```csharp
// InventorySlotInteraction.OnDrop
if (manager != null && manager.IsHolding)
{
    if (IsChestSlot)
    {
        Debug.LogWarning("背包到箱子拖拽暂不支持");  // ← 只输出警告
        return;
    }
}
```

---

## 二、修复方案

### 2.1 垃圾桶拖拽修复

在 `OnEndDrag` 中添加垃圾桶检测：

```csharp
public void OnEndDrag(PointerEventData eventData)
{
    if (SlotDragContext.IsDragging)
    {
        // 检测是否在垃圾桶区域
        if (IsOverTrashCan(eventData.position))
        {
            DropItemFromContext();
            return;
        }
        
        // 检测是否在面板外
        if (!IsInsidePanel(eventData.position))
        {
            DropItemFromContext();
            return;
        }
        
        SlotDragContext.Cancel();
        HideDragIcon();
        return;
    }
}
```

### 2.2 Up 区域修饰键修复

在 `OnPointerDown` 中处理箱子槽位的修饰键：

```csharp
public void OnPointerDown(PointerEventData eventData)
{
    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    
    // 箱子槽位：处理修饰键
    if (IsChestSlot)
    {
        if (shift || ctrl)
        {
            HandleChestSlotModifierClick(shift, ctrl);
        }
        return;
    }
}
```

### 2.3 Down→Up 放置修复

在 `OnDrop` 中处理 Manager Held 状态到箱子槽位：

```csharp
if (manager != null && manager.IsHolding)
{
    if (IsChestSlot)
    {
        HandleManagerHeldToChest(targetIndex);
        return;
    }
}
```

---

## 三、新增方法

### InventorySlotInteraction

```csharp
// 处理箱子槽位的修饰键点击
private void HandleChestSlotModifierClick(bool shift, bool ctrl);

// 处理 Manager Held 状态放置到箱子
private void HandleManagerHeldToChest(int targetIndex);

// 从 SlotDragContext 丢弃物品
private void DropItemFromContext();

// 检测是否在垃圾桶区域
private bool IsOverTrashCan(Vector2 screenPos);

// 检测是否在面板内
private bool IsInsidePanel(Vector2 screenPos);
```

### InventoryInteractionManager

```csharp
// 获取当前 Held 物品（供外部使用）
public ItemStack GetHeldItem();

// 清空 Held 状态（供外部使用）
public void ClearHeldItem();
```

---

## 四、状态流转

### 修饰键拿起 → 放置到箱子

1. Down 区域 Shift+左键 → `InventoryInteractionManager.HeldByShift`
2. 点击 Up 区域槽位 → `OnPointerDown` 检测到 `manager.IsHolding`
3. 调用 `HandleManagerHeldToChest()` → 放置到箱子
4. 清空 Manager Held 状态

### 箱子槽位修饰键拿起 → 放置

1. Up 区域 Shift+左键 → `SlotDragContext.Begin()`
2. 点击其他槽位 → `OnPointerDown` 检测到 `SlotDragContext.IsDragging`
3. 调用 `HandleSlotDragContextDrop()` → 放置
4. 清空 SlotDragContext

---

## 五、边界情况

1. **Held 状态点击箱子槽位**：放置到箱子
2. **SlotDragContext 状态点击背包槽位**：放置到背包
3. **两种状态同时存在**：理论上不可能，但需要防御性处理
