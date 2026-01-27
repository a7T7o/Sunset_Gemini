# 背包交互逻辑优化 - 任务列表

## 实现计划

- [x] 1. 修改 ShiftPickup 方法




  - 将 `half = amount / 2` 改为 `handAmount = (amount + 1) / 2`
  - 处理单个物品时源槽位清空的情况
  - _需求: 1.1, 1.2_

- [x] 2. 修改 ContinueShiftSplit 方法


  - 添加 `if (heldItem.amount <= 1) return;` 终止条件
  - 将返回数量改为向下取整，手上保留向上取整
  - _需求: 1.3_

- [x] 3. 修改 ExecutePlacement 方法


  - 在不同物品分支中添加源槽位检查
  - 源槽位为空时执行交换
  - 源槽位非空时返回原位
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2_

- [x] 4. 添加调试日志

  - 在关键逻辑点添加日志输出
  - 便于验证计算结果

- [x] 6. 确认 SelectSlot 在所有交换场景中被调用


  - 检查 ExecutePlacement 中的所有分支
  - 确保拖拽交换、Shift/Ctrl 放置、堆叠都调用 SelectSlot
  - _需求: 4.1, 4.2, 4.3_

- [x] 7. 实现 Sort 后清除选中状态


  - 在 InventorySlotUI 添加 Deselect() 方法
  - 在 InventoryInteractionManager 添加 ClearAllSelection() 方法
  - 在 OnSortButtonClick 中调用 ClearAllSelection()
  - _需求: 5.1, 5.2_

- [x] 8. 更新交互状态表

  - 更新 `.kiro/specs/UI系统/0_背包交互系统升级/交互状态表.md`
  - 反映新的交换逻辑

- [x] 9. 更新工作区记忆

  - 更新 `.kiro/specs/UI系统/1_背包V4飞升/memory.md`
  - 记录本次修改

- [x] 10. Checkpoint - 验证所有功能

  - 确保所有测试通过，询问用户是否有问题
