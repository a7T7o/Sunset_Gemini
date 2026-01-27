# 箱子 UI 锐评修复 V3 - 需求文档

## 文档说明

本文档基于用户验收反馈和 Code Reaper 锐评，修复 5 个严重问题。

---

## 问题清单

### 问题 1：远处箱子不需要导航就能打开

**用户描述**：
> 点击 A 箱子后导航到 A 箱子并打开后关闭，再点击较远处的 B 箱子，可以直接打开 B 箱子并不需要导航

**根因分析**：
- `HandleInteractable` 中的 `distance <= interactDist` 判断可能通过了
- 或者 `interactable.InteractionDistance` 设置过大

### 问题 2：背包区域 Sort 在箱子 UI 无效

**用户描述**：
> 背包区域 sort 在箱子 UI 无法实现，但是在背包 UI 可以实现

**根因分析**：
- `BoxPanelUI.SubscribeToChest()` 只订阅了箱子的事件
- 没有订阅 `InventoryService.OnInventoryChanged`
- 背包整理完了，BoxPanelUI 根本不知道

### 问题 3：长按左键拖动显示被删

**用户描述**：
> 长按左键的拿起拖动显示你删了，触发了拿起但是只会让当前格子的物品消失，图标不会跟随鼠标

**根因分析**：
- `InventoryInteractionManager.HandleIdleClick()` 无修饰键分支被留空
- 必须拖拽（BeginDrag）才会拿起
- 单纯的点击被忽略了

### 问题 4：垃圾桶失效

**用户描述**：
> 垃圾桶全部失效，无论是 props 还是箱子的，都是不能丢弃了

**根因分析**：
- `BoxPanelUI.OnTrashCanClicked()` 是空方法
- 没有调用 `InventoryInteractionManager.OnTrashCanClick()`
- SlotDragContext 模式下也没有丢弃逻辑

### 问题 5：长按左键拿起状态失效

**用户描述**：
> 只有长按左键交互触发了拿起但是只会让当前格子的物品消失，图标不会跟随鼠标

**根因分析**：
- 同问题 3，`HandleIdleClick` 无修饰键分支被留空

---

## 修复需求

### 需求 1：修复 Down 区 Sort

**验收标准**：
1. WHEN 点击 Sort Down 按钮时 THEN BoxPanelUI SHALL 刷新背包槽位显示
2. WHEN InventoryService.Sort() 触发 OnInventoryChanged 事件时 THEN BoxPanelUI SHALL 响应并刷新

### 需求 2：修复垃圾桶

**验收标准**：
1. WHEN 背包物品在手上时点击垃圾桶 THEN 系统 SHALL 调用 InventoryInteractionManager.OnTrashCanClick()
2. WHEN 箱子物品在手上时点击垃圾桶 THEN 系统 SHALL 丢弃物品并清空 SlotDragContext

### 需求 3：修复点击拿起

**验收标准**：
1. WHEN 无修饰键点击非空槽位时 THEN 系统 SHALL 拿起物品并显示拖拽图标
2. WHEN 物品被拿起时 THEN HeldItemDisplay SHALL 显示并跟随鼠标

### 需求 4：修复导航距离

**验收标准**：
1. WHEN 玩家与箱子距离超过 InteractionDistance 时 THEN 系统 SHALL 先导航再交互
2. WHEN 玩家与箱子距离在 InteractionDistance 内时 THEN 系统 SHALL 直接交互

---

## 修复顺序（按锐评指令）

1. 修复 Down 区 Sort（最简单，立竿见影）
2. 修复垃圾桶（接上 Manager 的逻辑）
3. 修复点击拿起（恢复 HandleIdleClick 的默认分支）
4. 检查导航距离

---

## 相关文件

| 文件 | 修改内容 |
|------|---------|
| `BoxPanelUI.cs` | 订阅 InventoryService.OnInventoryChanged，修复垃圾桶 |
| `InventoryInteractionManager.cs` | 恢复 HandleIdleClick 无修饰键分支 |
| `GameInputManager.cs` | 检查导航距离判断 |
