# 背包交互逻辑优化 - 设计文档

## 概述

本文档描述背包交互系统的逻辑优化设计，主要修改 `InventoryInteractionManager.cs` 中的二分拿取和放置逻辑。

## 架构

### 现有架构（不变）

```
InventoryInteractionManager (单例)
├── 状态机: Idle / HeldByShift / HeldByCtrl / Dragging
├── 拿取数据: heldItem, sourceIndex, sourceIsEquip
├── 服务引用: inventory, equipment, database
└── UI 引用: heldDisplay, panelRect, inventoryBoundsRect
```

### 修改范围

仅修改以下方法的内部逻辑：
1. `ShiftPickup()` - 二分拿取计算
2. `ContinueShiftSplit()` - 连续二分计算
3. `ExecutePlacement()` - 放置/交换逻辑

## 组件与接口

### ShiftPickup 方法（修改）

**修改前**：
```csharp
int half = slot.amount / 2;  // 向下取整，余数留源槽位
```

**修改后**：
```csharp
int handAmount = (slot.amount + 1) / 2;  // 向上取整，余数归手上
int sourceAmount = slot.amount - handAmount;
```

**数学验证**：
| 原数量 | 手上 (修改后) | 源槽位 (修改后) |
|--------|---------------|-----------------|
| 10 | 5 | 5 |
| 5 | 3 | 2 |
| 3 | 2 | 1 |
| 2 | 1 | 1 |
| 1 | 1 | 0 (清空) |

### ContinueShiftSplit 方法（修改）

**修改前**：
```csharp
int half = heldItem.amount / 2;
if (half <= 0) return;
int returnAmount = heldItem.amount - half;
heldItem.amount = half;
```

**修改后**：
```csharp
// 手上只有 1 个时不执行连续二分
if (heldItem.amount <= 1) return;

int returnAmount = heldItem.amount / 2;  // 返回给源槽位的数量（向下取整）
heldItem.amount = heldItem.amount - returnAmount;  // 手上保留（向上取整）
```

**数学验证**：
| 手上原数量 | 返回源槽位 | 手上剩余 |
|------------|------------|----------|
| 5 | 2 | 3 |
| 3 | 1 | 2 |
| 2 | 1 | 1 |
| 1 | 0 (跳过) | 1 |

### ExecutePlacement 方法（修改）

**修改前**：
```csharp
// 不同物品
if (allowSwap)
{
    // 拖拽模式：交换
    ...
}
else
{
    // Shift/Ctrl 模式：返回原位
    ReturnToSource();
    ResetState();
}
```

**修改后**：
```csharp
// 不同物品
if (allowSwap)
{
    // 拖拽模式：交换
    ...
}
else
{
    // Shift/Ctrl 模式：检查源槽位是否为空
    ItemStack sourceSlot = GetSlot(sourceIndex, sourceIsEquip);
    if (sourceSlot.IsEmpty)
    {
        // 源槽位为空，允许交换
        SetSlot(targetIndex, targetIsEquip, heldItem);
        SetSlot(sourceIndex, sourceIsEquip, target);
        heldItem = new ItemStack();
        SelectSlot(targetIndex, targetIsEquip);
        ResetState();
    }
    else
    {
        // 源槽位非空，返回原位
        ReturnToSource();
        ResetState();
    }
}
```

### SelectSlot 方法（确认现有实现）

当前代码中已有 `SelectSlot` 方法，需要确认在所有交换/放置场景中都被调用。

### ClearAllSelection 方法（新增）

```csharp
/// <summary>
/// 清除所有槽位的选中状态
/// </summary>
private void ClearAllSelection()
{
    if (inventorySlots != null)
    {
        foreach (var slot in inventorySlots)
        {
            slot?.Deselect();
        }
    }
}
```

### OnSortButtonClick 方法（修改）

```csharp
private void OnSortButtonClick()
{
    if (IsHolding) Cancel();
    
    // ★ 整理前清除所有选中状态
    ClearAllSelection();
    
    if (sortService != null)
    {
        sortService.SortInventory();
    }
}
```

### InventorySlotUI.Deselect 方法（新增）

```csharp
/// <summary>
/// 取消选中状态
/// </summary>
public void Deselect()
{
    if (toggle != null)
    {
        toggle.isOn = false;
    }
}
```

## 数据模型

### 状态判断逻辑

```
ExecutePlacement(targetIndex, targetIsEquip, allowSwap):
    1. 装备槽位限制检查（优先）
       - 如果目标是装备槽位且不匹配 → 返回原位
    
    2. 目标为空
       - 直接放置 → Idle
    
    3. 目标是相同物品
       - 尝试堆叠
       - 全部堆叠成功 → Idle
       - 部分堆叠 → 保持 Held 状态
    
    4. 目标是不同物品
       - 如果 allowSwap (拖拽模式) → 交换
       - 如果 !allowSwap (Shift/Ctrl 模式):
         - 如果源槽位为空 → 交换
         - 如果源槽位非空 → 返回原位
```

## 错误处理

### 边界情况处理

1. **单个物品的 Shift 拿取**
   - `(1+1)/2 = 1`，手上 1 个，源槽位 0 个（清空）
   - 正确处理

2. **手上 1 个时的连续二分**
   - 直接 return，不执行任何操作
   - 避免返回 0 个的无意义操作

3. **堆叠溢出**
   - 目标达到最大堆叠，手上保留溢出部分
   - 继续保持 Held 状态

4. **装备槽位限制**
   - 在交换逻辑之前检查
   - 不匹配时返回原位

## 测试策略

### 单元测试用例

| 测试场景 | 输入 | 预期输出 |
|----------|------|----------|
| Shift 拿取 10 个 | amount=10 | 手上=5, 源=5 |
| Shift 拿取 5 个 | amount=5 | 手上=3, 源=2 |
| Shift 拿取 3 个 | amount=3 | 手上=2, 源=1 |
| Shift 拿取 1 个 | amount=1 | 手上=1, 源=0 |
| 连续二分 5 个 | held=5 | 返回=2, 手上=3 |
| 连续二分 2 个 | held=2 | 返回=1, 手上=1 |
| 连续二分 1 个 | held=1 | 跳过，手上=1 |
| 源空+不同物品 | sourceEmpty=true | 执行交换 |
| 源非空+不同物品 | sourceEmpty=false | 返回原位 |

### 验收测试

1. **Shift 二分拿取**
   - 10 个物品 → Shift 点击 → 手上 5 个
   - 继续 Shift 点击源槽位 → 手上 3 个
   - 继续 Shift 点击源槽位 → 手上 2 个
   - 继续 Shift 点击源槽位 → 手上 1 个
   - 继续 Shift 点击源槽位 → 手上仍然 1 个（不变）

2. **源槽位为空时的交换**
   - 1 个苹果 → Shift 点击 → 手上 1 个，源槽位空
   - 点击有橙子的槽位 → 苹果放到橙子位置，橙子放到原苹果位置

3. **源槽位非空时不交换**
   - 10 个苹果 → Shift 点击 → 手上 5 个，源槽位 5 个
   - 点击有橙子的槽位 → 返回原位，苹果变回 10 个
