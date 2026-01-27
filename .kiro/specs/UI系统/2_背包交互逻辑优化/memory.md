# 背包交互逻辑优化 - 开发记忆

## 模块概述

优化背包交互系统的 Shift 二分拿取和 Shift/Ctrl 放置逻辑，使修饰键操作更加灵活便捷。

## 当前状态

- **完成度**: 100%
- **最后更新**: 2026-01-07
- **状态**: ✅ 已完成所有实现任务

---

## 会话 2 - 2026-01-07（实现）

**用户需求**:
> 继续完成所有任务

**完成任务**:
1. ✅ 修改 ShiftPickup 方法（余数归手上）
2. ✅ 修改 ContinueShiftSplit 方法（手上 1 个时跳过）
3. ✅ 修改 ExecutePlacement 方法（源空时允许交换）
4. ✅ 添加调试日志
5. ✅ 确认 SelectSlot 在所有交换场景中被调用
6. ✅ 实现 Sort 后清除选中状态
   - 在 InventorySlotUI 添加 Deselect() 方法
   - 在 InventoryInteractionManager 添加 ClearAllSelection() 方法
   - 在 OnSortButtonClick 中调用 ClearAllSelection()

**修改文件**:
- `Assets/YYY_Scripts/UI/Inventory/InventoryInteractionManager.cs`
  - ShiftPickup：`handAmount = (amount + 1) / 2`
  - ContinueShiftSplit：添加 `if (heldItem.amount <= 1) return;`
  - ExecutePlacement：源槽位为空时允许交换
  - OnSortButtonClick：整理后清除所有选中状态
  - ClearAllSelection：新增方法
- `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs`
  - Deselect：新增方法

**解决方案**:

### 1. Shift 二分拿取余数归手上

```csharp
// 向上取整：余数归手上
int handAmount = (slot.amount + 1) / 2;
int sourceAmount = slot.amount - handAmount;
```

### 2. 连续二分终止

```csharp
// 手上只有 1 个时，不执行二分
if (heldItem.amount <= 1) return;
```

### 3. 源槽位为空时允许交换

```csharp
// Shift/Ctrl 模式：检查源槽位是否为空
ItemStack source = GetSlot(sourceIndex, sourceIsEquip);

if (source.IsEmpty)
{
    // 源槽位为空：允许交换
    SetSlot(targetIndex, targetIsEquip, heldItem);
    SetSlot(sourceIndex, sourceIsEquip, target);
    SelectSlot(targetIndex, targetIsEquip);
}
else
{
    // 源槽位非空：返回原位
    ReturnToSource();
}
```

### 4. Sort 后清除选中状态

```csharp
private void ClearAllSelection()
{
    foreach (var slot in inventorySlots)
    {
        slot?.Deselect();
    }
}
```

**遗留问题**:
- 无

---

## 会话 1 - 2026-01-07（需求分析与设计）

**用户需求**:
> 1. Shift 二分拿取余数归手上，最坏情况手上至少有 1 个
> 2. Shift/Ctrl 拿起后源槽位为空时，允许交换不同物品
> 3. 修饰键是为了方便玩家，不是限制玩家
> 4. 交换后选中目标槽位
> 5. Sort 后清除所有选中状态

**需求分析**:

### 需求 1：Shift 二分拿取

**当前逻辑**：`half = amount / 2`（向下取整，余数留源槽位）
- 10 → 手上 5，源 5
- 5 → 手上 2，源 3 ❌

**期望逻辑**：`handAmount = (amount + 1) / 2`（向上取整，余数归手上）
- 10 → 手上 5，源 5
- 5 → 手上 3，源 2 ✅
- 3 → 手上 2，源 1 ✅
- 2 → 手上 1，源 1 ✅
- 1 → 手上 1，源 0 ✅（源槽位清空）

### 需求 2：源槽位为空时允许交换

**当前逻辑**：Shift/Ctrl 拿起后点击不同物品 → 返回原位

**期望逻辑**：
- 源槽位为空 → 允许交换
- 源槽位非空 → 返回原位

### 边界情况分析

1. **连续二分终止**：手上只有 1 个时跳过（不返回 0 个）
2. **堆叠溢出**：目标达到最大，手上保留溢出部分
3. **装备槽位限制**：限制检查优先于交换逻辑

**完成任务**:
1. ✅ 创建需求文档 `requirements.md`（5 个需求）
2. ✅ 创建设计文档 `design.md`
3. ✅ 创建任务列表 `tasks.md`（10 个任务）
4. ✅ 创建工作区记忆 `memory.md`

**需求汇总**:
| 需求 | 说明 |
|------|------|
| 需求 1 | Shift 二分拿取余数归手上 |
| 需求 2 | 源槽位为空时允许交换 |
| 需求 3 | Ctrl 拿取全部后的状态处理 |
| 需求 4 | 交换后选中目标槽位 |
| 需求 5 | Sort 后清除选中状态 |

**下一步**:
- [ ] 执行任务列表中的实现任务

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 余数归手上 | 玩家更容易控制拿取数量 | 2026-01-07 |
| 源空时允许交换 | 修饰键应该方便玩家而非限制 | 2026-01-07 |
| 手上 1 个时跳过连续二分 | 避免无意义的返回 0 个操作 | 2026-01-07 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/UI/Inventory/InventoryInteractionManager.cs` | 核心修改文件 |
| `.kiro/specs/UI系统/0_背包交互系统升级/交互状态表.md` | 需要更新 |
| `.kiro/specs/UI系统/1_背包V4飞升/memory.md` | 需要更新 |
