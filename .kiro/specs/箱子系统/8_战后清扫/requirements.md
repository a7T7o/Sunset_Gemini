# 箱子系统战后清扫 - 需求文档

**创建日期**: 2026-01-19
**优先级**: P0（严重 Bug）

---

## 一、问题描述

### 问题 1：垃圾桶拖拽无效
- **现象**：长按拖拽物品到垃圾桶无法丢弃，带修饰键单击可以
- **根因**：`OnEndDrag` 中 `SlotDragContext.IsDragging` 为 true 时直接 `Cancel()` 返回原位，没有检测垃圾桶

### 问题 2：Up 区域 Shift/Ctrl 无效
- **现象**：Shift/Ctrl+左键无法对 Up 区域（箱子槽位）进行二分/单拿
- **根因**：`OnPointerDown` 中 `if (IsChestSlot) return;` 直接返回，没有处理箱子槽位的修饰键逻辑

### 问题 3：Down 物品无法放到 Up
- **现象**：Down 区域拿起的物品无法放置到 Up 区域
- **根因**：`OnDrop` 中 `manager.IsHolding` 时对箱子槽位只输出警告，没有实际处理

---

## 二、用户故事

### US-1：垃圾桶拖拽丢弃
**作为** 玩家
**我希望** 拖拽物品到垃圾桶时能丢弃物品
**以便** 我能快速清理不需要的物品

**验收标准**:
- [ ] 从 Up 区域拖拽物品到垃圾桶 → 物品丢弃
- [ ] 从 Down 区域拖拽物品到垃圾桶 → 物品丢弃
- [ ] 拖拽到面板外 → 物品丢弃

### US-2：Up 区域修饰键交互
**作为** 玩家
**我希望** 在 Up 区域使用 Shift/Ctrl+左键进行二分/单拿
**以便** 我能精确控制从箱子拿取的物品数量

**验收标准**:
- [ ] Shift+左键 Up 区域槽位 → 二分拿取
- [ ] Ctrl+左键 Up 区域槽位 → 单个拿取
- [ ] 拿起后点击其他槽位 → 放置物品

### US-3：跨区域放置
**作为** 玩家
**我希望** 从 Down 区域拿起的物品能放到 Up 区域
**以便** 我能将背包物品存入箱子

**验收标准**:
- [ ] Down 区域 Shift 拿起 → 点击 Up 区域空槽位 → 放置成功
- [ ] Down 区域 Ctrl 拿起 → 点击 Up 区域空槽位 → 放置成功
- [ ] Down 区域拿起 → 点击 Up 区域同物品槽位 → 堆叠成功

---

## 三、修复方案

### 方案 A：扩展 InventoryInteractionManager（采用）

**核心思路**：让 `InventoryInteractionManager` 支持任意 `IItemContainer`

**改动点**：
1. 添加 `IItemContainer sourceContainer` 字段
2. 新增 `OnSlotPointerDownWithContainer()` 方法
3. 修改 `GetSlot/SetSlot/ClearSlot` 支持任意容器
4. `InventorySlotInteraction.OnPointerDown` 调用新方法

---

## 四、影响范围

- `InventoryInteractionManager.cs` - 扩展支持 IItemContainer
- `InventorySlotInteraction.cs` - 修改 OnPointerDown 和 OnEndDrag
- `BoxPanelUI.cs` - 无需修改

---

## 五、测试计划

### 测试矩阵

| 操作 | Up→Up | Up→Down | Down→Up | Down→Down | 垃圾桶 |
|------|-------|---------|---------|-----------|--------|
| 拖拽 | ✓ | ✓ | ✓ | ✓ | ✓ |
| Shift | ✓ | ✓ | ✓ | ✓ | ✓ |
| Ctrl | ✓ | ✓ | ✓ | ✓ | ✓ |
