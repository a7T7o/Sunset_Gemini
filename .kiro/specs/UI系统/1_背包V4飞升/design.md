# 背包交互系统 V4 - 方案 D 设计文档

## 版本信息

- **版本**: v4.0 (方案 D)
- **日期**: 2026-01-06
- **状态**: 实施中

---

## 核心问题回顾

### V3 方案的致命缺陷

当 `InventorySlotUI` 同时实现 `IPointerDownHandler` 和 `IBeginDragHandler` 时：

1. **Unity EventSystem 的事件处理顺序**:
   ```
   用户按下鼠标
       ↓
   OnPointerDown（立即触发，但被拖拽系统"劫持"）
       ↓
   等待判断是点击还是拖拽
       ↓
   如果移动 → OnBeginDrag → OnDrag → OnEndDrag
   如果不移动 → OnPointerUp → OnPointerClick
   ```

2. **问题**:
   - `OnPointerDown` 和 `OnPointerUp` 会被延迟或跳过
   - 无法在按下时检测修饰键（Shift/Ctrl）
   - 无法实现长按持续拿取

---

## 方案 D：分离组件架构

### 核心思路

**职责分离 + 状态驱动的接口管理**

1. **InventorySlotUI** - 只负责显示（完全不动）
2. **InventorySlotInteraction** - 只负责交互（新组件）
3. **InventoryInteractionManager** - 只负责逻辑（大脑）

### 架构图

```
Unity EventSystem
    ↓
InventorySlotInteraction (传感器 + 状态控制)
    ↓
InventoryInteractionManager (大脑)
    ↓
数据层 (InventoryService / EquipmentService)
    ↓
InventorySlotUI (显示) ← 监听数据变化自动刷新
```

---

## 核心设计：状态驱动的接口管理

### 问题的本质

Unity EventSystem 的接口冲突是因为**同时启用了点击和拖拽接口**。

### 解决方案

**根据当前状态动态控制接口行为**：

1. **默认状态**：只响应点击，忽略拖拽
2. **检测到长按**：启用拖拽响应
3. **拖拽结束**：恢复默认状态

---

## 配置参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| dragTimeThreshold | 0.15f | 拖拽触发时间阈值（秒） |
| dragMoveThreshold | 5f | 拖拽触发移动阈值（像素） |
| ctrlPickupRate | 3.5f | Ctrl 长按拿取速度（个/秒） |
| dropCooldown | 5f | 丢弃冷却时间（秒） |

---

## 与 V3 方案的对比

| 特性 | V3 方案 | V4 方案 D |
|------|---------|-----------|
| 接口冲突 | ❌ 存在 | ✅ 完全避免 |
| 点击检测 | ❌ 被劫持 | ✅ 正常工作 |
| 修饰键检测 | ❌ 无法检测 | ✅ 正常工作 |
| 拖拽功能 | ✅ 正常 | ✅ 正常 |
| 职责分离 | ⚠️ 部分 | ✅ 完全分离 |
| 易于调试 | ⚠️ 一般 | ✅ 非常清晰 |
| 代码复杂度 | ⚠️ 中等 | ✅ 简单清晰 |

