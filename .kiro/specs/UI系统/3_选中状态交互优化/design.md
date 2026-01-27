# 选中状态交互优化 - 设计文档

## 概述

本设计文档描述如何优化背包和箱子 UI 的选中状态交互逻辑，包括修复 Bug 和完善选中状态绑定。

---

## 架构设计

### 现有架构

```
InventorySlotUI (槽位 UI)
├── Toggle 组件 (管理选中状态)
├── Select() / Deselect() 方法
└── InventorySlotInteraction (交互组件)
    ├── OnPointerDown / OnBeginDrag / OnDrop 等事件
    └── 调用 InventoryInteractionManager 或 SlotDragContext

InventoryInteractionManager (交互管理器)
├── 状态机: Idle / HeldByShift / HeldByCtrl / Dragging
├── ExecutePlacement() - 放置逻辑
├── SelectSlot() - 选中槽位（仅背包）
└── ContinueCtrlPickup() - Ctrl 长按协程

SlotDragContext (拖拽上下文)
├── 跨容器拖拽状态管理
└── HandleSlotDragContextDrop() - 处理放置
```

### 修改点

1. **InventoryInteractionManager.ContinueCtrlPickup()**
   - 在 `break` 前添加 `ShowHeld()` 调用

2. **InventoryInteractionManager.SelectSlot()**
   - 扩展支持箱子槽位选中

3. **InventorySlotInteraction.HandleSlotDragContextDrop()**
   - 放置成功后调用选中逻辑

4. **新增：DeselectSourceSlot()**
   - 跨区域放置时取消原区域选中

---

## 详细设计

### 修复 1：Ctrl+长按拿取数量显示 Bug

**问题代码**：
```csharp
private IEnumerator ContinueCtrlPickup()
{
    while (true)
    {
        // ...
        heldItem.amount++;
        if (src.amount > 1)
            SetSlot(...);
        else
        {
            ClearSlot(...);
            break;  // 🔥 问题：break 后没有 ShowHeld()
        }
        ShowHeld();
    }
}
```

**修复方案**：
```csharp
private IEnumerator ContinueCtrlPickup()
{
    while (true)
    {
        // ...
        heldItem.amount++;
        if (src.amount > 1)
            SetSlot(...);
        else
        {
            ClearSlot(...);
            ShowHeld();  // 🔥 修复：break 前调用 ShowHeld()
            break;
        }
        ShowHeld();
    }
}
```

### 修复 2：扩展 SelectSlot 支持箱子槽位

**现有代码**：
```csharp
private void SelectSlot(int index, bool isEquip)
{
    if (!isEquip && inventorySlots != null && index >= 0 && index < inventorySlots.Length)
    {
        inventorySlots[index]?.Select();
    }
}
```

**问题**：只处理背包槽位，没有处理箱子槽位。

**修复方案**：
由于箱子槽位在 `BoxPanelUI` 中管理，需要通过 `InventorySlotInteraction` 直接调用槽位的 `Select()` 方法。

在 `InventorySlotInteraction` 的放置处理中添加选中逻辑：
```csharp
// 放置成功后选中目标槽位
if (inventorySlotUI != null)
{
    inventorySlotUI.Select();
}
```

### 修复 3：跨区域放置时取消原区域选中

**设计思路**：
- 在 `SlotDragContext` 中记录源槽位的 `InventorySlotUI` 引用
- 放置成功后，如果是跨区域，调用源槽位的 `Deselect()`

**实现方案**：
在 `InventorySlotInteraction.HandleSlotDragContextDrop()` 中：
```csharp
// 跨区域放置时取消原区域选中
if (sourceIsChest != targetIsChest)
{
    // 需要取消源槽位选中
    // 通过 SlotDragContext 获取源槽位 UI 并调用 Deselect()
}

// 选中目标槽位
if (inventorySlotUI != null)
{
    inventorySlotUI.Select();
}
```

### 修复 4：InventoryInteractionManager 放置后选中

**现有代码**：
`ExecutePlacement()` 中已有 `SelectSlot()` 调用，但只处理背包槽位。

**修复方案**：
保持现有逻辑，因为 `InventoryInteractionManager` 主要处理背包槽位。
箱子槽位的选中由 `InventorySlotInteraction` 处理。

---

## 选中状态流程图

```
用户操作
    │
    ├─ 左键单击（无修饰键）
    │   └─ Toggle 自动处理选中/取消选中
    │
    ├─ 拖拽放置
    │   ├─ 同区域
    │   │   └─ 目标槽位 Select()（ToggleGroup 自动取消其他）
    │   └─ 跨区域
    │       ├─ 源槽位 Deselect()
    │       └─ 目标槽位 Select()
    │
    ├─ Shift 拿取后点击放置
    │   └─ 目标槽位 Select()
    │
    └─ Ctrl 拿取后点击放置
        └─ 目标槽位 Select()
```

---

## 测试策略

### 单元测试

1. **Ctrl+长按拿取数量**
   - 测试 10 个物品拿干净后手上显示 10 个
   - 测试放置后槽位显示 10 个

2. **选中状态绑定**
   - 测试同区域拖拽后目标槽位选中
   - 测试跨区域拖拽后源槽位取消选中、目标槽位选中

### 集成测试

1. 背包内拖拽物品，验证选中状态
2. 箱子内拖拽物品，验证选中状态
3. 背包到箱子拖拽，验证选中状态
4. 箱子到背包拖拽，验证选中状态
5. Shift/Ctrl 拿取后放置，验证选中状态

---

## 风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| Toggle 行为被干扰 | 选中状态异常 | 只在必要时调用 Select()，不干扰 Toggle 自身逻辑 |
| 跨区域选中状态不一致 | 两个区域都有选中 | 明确在跨区域时调用源槽位 Deselect() |
| 性能影响 | 无 | 只在放置时调用，不是高频操作 |

---

**创建日期**: 2026-01-20
**状态**: 设计完成
