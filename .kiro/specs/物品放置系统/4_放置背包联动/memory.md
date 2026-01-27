# 4_放置背包联动 - 子工作区记忆

## 概述
- **创建日期**: 2026-01-05
- **状态**: ✅ 已完成
- **目标**: 实现放置系统与背包的联动（扣除物品、中断处理、连续放置）

## 会话记录

### 会话 1 - 2026-01-05（设计与实现）

**用户需求**:
> 对于放置物品的成功的时候你需要对当前手持的可放置物品数量减一，你根本没有这个逻辑，对背包内容进行同步，但是这一步你需要严谨处理，首先是严谨选择触发时机，你需要确保放置完成了之后才会触发减一，并且对于减一的处理为了避免在放置过程中切换到其他物品，所以在放置过程中如果出现手持物品切换为其他物体则你的放置就直接中断并且取消导航等内容...

**完成任务**:
1. ✅ 实现 PlacementSnapshot 快照机制
2. ✅ 实现 CreateSnapshot() 和 ValidateSnapshot() 方法
3. ✅ 实现 CheckInterruptConditions() 和 HandleInterrupt() 方法
4. ✅ 订阅 HotbarSelectionService.OnSelectedChanged 事件
5. ✅ 修改 ExecutePlacement() 使用快照数据
6. ✅ 修改 DeductItemFromInventory() 和 HasMoreItems() 使用快照数据
7. ✅ 创建验收指南

**核心实现**:

### PlacementSnapshot 快照机制
```csharp
private struct PlacementSnapshot
{
    public int itemId;           // 物品 ID
    public int quality;          // 物品品质
    public int slotIndex;        // 背包槽位索引
    public Vector3 lockedPosition; // 锁定的放置位置
    public bool isValid;         // 快照是否有效
}
```

### 中断检测
- 背包打开时中断
- 手持物品切换时中断（通过事件订阅）
- 快照失效时中断（物品被移动/消耗）

### 背包扣除
- 只在 ExecutePlacement() 成功实例化后才扣除
- 使用快照中的 slotIndex 精确扣除
- 扣除失败时销毁已实例化的物品

---

### 会话 2 - 2026-01-07（事件触发顺序问题修复）

**问题**:
放置时减一然后物体消失，放置又需要调用这个物体的信息导致报错

**根本原因**:
`DeductItemFromInventory()` 触发 `OnSlotChanged` 事件 → `HotbarSelectionService` 调用 `ExitPlacementMode()` → `currentPlacementItem = null`

**修复**:
1. ✅ 添加 `isExecutingPlacement` 标志位
2. ✅ 在扣除物品之前保存所有需要的数据
3. ✅ 在回调中检查标志，执行中时忽略回调
4. ✅ 订阅 `InventoryService.OnSlotChanged` 事件

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 使用快照机制 | 避免放置过程中物品切换导致数据错乱 | 2026-01-05 |
| 订阅 OnSelectedChanged 事件 | 解耦检测，不需要每帧轮询 | 2026-01-05 |
| 添加 isExecutingPlacement 标志 | 防止事件回调在执行中清空数据 | 2026-01-07 |
