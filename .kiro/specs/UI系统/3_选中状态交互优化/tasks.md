# 选中状态交互优化 - 任务列表

## 概述

根据需求和设计文档，实现选中状态交互优化。

---

## 任务列表

- [x] 1. 修复 Ctrl+长按拿取数量显示 Bug
  - 修改 `InventoryInteractionManager.ContinueCtrlPickup()`
  - 在 `break` 前添加 `ShowHeld()` 调用
  - _Requirements: 3.1, 3.2, 3.3_

- [x] 2. 实现放置后选中目标槽位
  - [x] 2.1 修改 `InventorySlotInteraction.HandleSlotDragContextDrop()`
    - 在放置成功后调用目标槽位的 `Select()`
    - _Requirements: 2.1, 2.2_
  - [x] 2.2 修改 `InventorySlotInteraction.HandleSameContainerDrop()`
    - 同容器放置后选中目标槽位
    - _Requirements: 2.1_
  - [x] 2.3 修改 `InventorySlotInteraction.HandleChestToInventoryDrop()`
    - 跨容器放置后选中目标槽位
    - _Requirements: 2.2_
  - [x] 2.4 修改 `InventorySlotInteraction.HandleInventoryToChestDrop()`
    - 跨容器放置后选中目标槽位
    - _Requirements: 2.2_

- [x] 3. 实现跨区域放置时取消原区域选中
  - [x] 3.1 扩展 `SlotDragContext` 记录源槽位 UI 引用
    - 添加 `SourceSlotUI` 属性
    - _Requirements: 2.2_
  - [x] 3.2 修改 `InventorySlotInteraction.OnBeginDrag()`
    - 在 `SlotDragContext.Begin()` 时传入源槽位 UI
    - _Requirements: 2.2_
  - [x] 3.3 在跨区域放置时调用源槽位 `Deselect()`
    - 新增 `DeselectSourceSlot()` 辅助方法
    - _Requirements: 2.2_

- [x] 4. 确认 Toggle 基础行为正常
  - Toggle 组件自动管理选中/取消选中
  - ToggleGroup 自动管理互斥
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 5. Checkpoint - 编译验证
  - Unity 编译通过，无新增错误
  - 3 个无关警告（NavGrid2D、ItemTooltip）

- [x] 6. 创建验收指南
  - 创建 `验收指南.md` 文档

---

**创建日期**: 2026-01-20
**完成日期**: 2026-01-20
**状态**: 已完成
