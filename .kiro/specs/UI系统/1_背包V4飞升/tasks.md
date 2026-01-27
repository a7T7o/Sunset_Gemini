# 背包交互系统 V4 - 方案 D 任务列表

## 版本信息

- **版本**: v4.0 (方案 D)
- **日期**: 2026-01-06
- **状态**: 实施中

---

## 核心原则

**方案 D：分离组件架构 + 状态驱动的接口管理**

- InventorySlotUI - 只负责显示（完全不动）
- InventorySlotInteraction - 只负责交互（新组件）
- InventoryInteractionManager - 只负责逻辑（大脑）

---

## 实施计划

### 阶段 1：创建 Interaction 组件

- [x] 1. 创建 InventorySlotInteraction 组件


  - 实现 6 个 Unity 原生接口
  - 添加状态标志（isDragEnabled, pressTime, pressPosition）
  - 添加 Bind() 方法关联 SlotUI

### 阶段 2：创建 Manager



- [ ] 2. 创建 InventoryInteractionManager 组件
  - 实现单例模式
  - 实现公共方法（供 Interaction 调用）
  - 实现所有业务逻辑





### 阶段 3：集成到 SlotUI

- [ ] 3. 集成 Interaction 到 InventorySlotUI
  - 在 Awake() 中自动添加 Interaction 组件

- [x] 4. 集成 Interaction 到 EquipmentSlotUI

  - 在 Awake() 中自动添加 Interaction 组件

### 阶段 4：测试验证

- [ ] 5. 测试所有功能
  - 拖拽交换
  - Shift+左键 二分拿取
  - Ctrl+左键 单个拿取
  - 快速装备
  - 背包整理
  - 丢弃机制

