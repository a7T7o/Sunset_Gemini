# 箱子系统战后清扫 - 任务列表

**创建日期**: 2026-01-19

---

## 任务列表

- [x] T1: 修复垃圾桶拖拽
- [x] T2: 修复 Up 区域修饰键
- [x] T3: 修复 Down→Up 放置
- [x] T4: 添加 Manager 公共接口
- [x] T5: 创建验收指南

---

## T1: 修复垃圾桶拖拽

**文件**: `Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

**修改内容**: 
- 在 `OnEndDrag` 中添加垃圾桶和面板外检测
- 添加 `DropItemFromContext()` 方法
- 添加 `IsOverTrashCan()` 和 `IsInsidePanel()` 方法

**状态**: ✅ 完成

---

## T2: 修复 Up 区域修饰键

**文件**: `Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

**修改内容**: 
- 修改 `OnPointerDown` 处理箱子槽位的修饰键
- 添加 `HandleChestSlotModifierClick()` 方法

**状态**: ✅ 完成

---

## T3: 修复 Down→Up 放置

**文件**: `Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`

**修改内容**: 
- 修改 `OnDrop` 处理 Manager Held 到箱子
- 添加 `HandleManagerHeldToChest()` 方法

**状态**: ✅ 完成

---

## T4: 添加 Manager 公共接口

**文件**: `Assets/YYY_Scripts/UI/Inventory/InventoryInteractionManager.cs`

**修改内容**: 
- 添加 `GetHeldItem()` 方法
- 添加 `ClearHeldItem()` 方法

**状态**: ✅ 完成

---

## T5: 创建验收指南

**文件**: `.kiro/specs/箱子系统/8_战后清扫/验收指南.md`

**状态**: ✅ 完成
