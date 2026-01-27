# 箱子 UI 交互完善 V2 - 任务列表

## 概述

本任务列表基于 Code Reaper Review Session 11 的锐评及强制修正指令。

**预计总时间**：约 3-4 小时（不含回归测试）

---

## 任务

### 0. UI 互斥与导航修复（P0-1 新增）

**预计时间**：30 分钟

- [ ] 0.1 修改 GameInputManager 移除对 Box 的直接关闭分支
  - _文件_：`Assets/YYY_Scripts/Controller/Input/GameInputManager.cs`

- [ ] 0.2 修改 PackagePanelTabsUI.OpenOrToggle() 统一关闭 Box
  - _文件_：`Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs`

- [ ] 0.3 修改 PackagePanelTabsUI.OpenPanel() 统一关闭 Box
  - _文件_：`Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs`

- [ ] 0.4 添加 CloseBoxPanelIfOpen() 辅助方法
  - _文件_：`Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs`

- [ ] 0.5 添加 RestoreMainAndTop() 辅助方法
  - _文件_：`Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs`

- [ ] 0.6 修复 IsAnyPanelOpen 属性
  - _文件_：`Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs`

- [ ] 0.7 验证 UI 互斥与导航


---

### 1. 修复 _database 初始化问题（P0-3）

**预计时间**：15 分钟

- [ ] 1.1 修改 BoxPanelUI.Open() 方法添加防御性检查
  - 优先级 1：从 chest.Inventory.Database 获取
  - 优先级 2：从 _inventoryService.Database 获取
  - 优先级 3：尝试查找 InventoryService 并获取
  - _文件_：`Assets/YYY_Scripts/UI/Box/BoxPanelUI.cs`

- [ ] 1.2 验证 Up 槽位绑定成功

---

### 2. 实现跨容器拖拽 SlotDragContext（P0-2 完整实现）

**预计时间**：60 分钟

- [ ] 2.1 新建 SlotDragContext.cs 静态服务类
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/SlotDragContext.cs`

- [ ] 2.2 添加容器类型判断属性到 InventorySlotInteraction
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

- [ ] 2.3 修改 OnBeginDrag() 方法 - 箱子槽位
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

- [ ] 2.4 修改 OnBeginDrag() 方法 - 背包槽位
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

- [ ] 2.5 修改 OnDrop() 方法 - 目标是箱子槽位
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

- [ ] 2.6 实现 HandleDropToChest() 方法
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

- [ ] 2.7 修改 OnDrop() 方法 - 目标是背包槽位
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

- [ ] 2.8 实现 HandleDropToInventory() 方法
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

- [ ] 2.9 修改 OnEndDrag() 方法
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

- [ ] 2.10 验证跨容器拖拽


---

### 3. 实施日志规范

**预计时间**：15 分钟

- [ ] 3.1 修改 UIItemIconScaler.cs 添加日志开关
  - _文件_：`Assets/YYY_Scripts/UI/Utils/UIItemIconScaler.cs`

- [ ] 3.2 修改 BoxPanelUI.cs 添加错误日志去重
  - _文件_：`Assets/YYY_Scripts/UI/Box/BoxPanelUI.cs`

- [ ] 3.3 修改 InventorySlotUI.cs 移除成功日志
  - _文件_：`Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs`

- [ ] 3.4 验证日志数量

---

### 4. 添加 Sort 方法的事件触发（P0-4）

**预计时间**：10 分钟

- [ ] 4.1 修改 ChestInventory.Sort() 添加事件触发
  - _文件_：`Assets/YYY_Scripts/Service/Inventory/ChestInventory.cs`

- [ ] 4.2 修改 InventoryService.Sort() 添加事件触发
  - _文件_：`Assets/YYY_Scripts/Service/Inventory/InventoryService.cs`

- [ ] 4.3 验证 Sort 功能


---

### 5. 完整验证

**预计时间**：30 分钟

- [ ] 5.1 UI 互斥与导航验证（P0-1）
  - BoxOpen → Tab：背包页面出现、Box 关闭
  - BoxOpen → B/M/L/O：对应背包页面出现、Box 关闭
  - NoPanel → 右键远处箱子：导航到位后开 UI
  - BoxOpen → 右键另一个远处箱子：导航到新箱子并切换箱子 UI

- [ ] 5.2 _database 初始化验证（P0-3）
  - BindChestSlotData() 成功调用
  - Up 槽位的 Container 属性指向 ChestInventory
  - Up 槽位显示箱子内容

- [ ] 5.3 跨容器拖拽验证（P0-2）
  - Up→Up 拖拽：箱子内物品交换/合并
  - Down→Up 拖拽：物品从背包进入箱子
  - Up→Down 拖拽：物品从箱子进入背包
  - Down→Down 拖拽：背包内物品交换/合并

- [ ] 5.4 Sort 功能验证（P0-4）
  - Sort Up 按钮对箱子排序生效
  - Sort Down 按钮对背包排序生效

- [ ] 5.5 日志验证
  - 完整流程日志数量 ≤ 15 行

- [ ] 5.6 创建验收指南文档
  - _文件_：`.kiro/specs/箱子系统/5_箱子UI交互完善V2/验收指南.md`

---

## 相关文档

- 需求文档：`.kiro/specs/箱子系统/5_箱子UI交互完善V2/requirements.md`
- 设计文档：`.kiro/specs/箱子系统/5_箱子UI交互完善V2/design.md`
- 锐评文档：`.kiro/specs/箱子系统/4_箱子UI交互完善/code-reaper-reviews/review-session11-up-mirror-critique.md`
