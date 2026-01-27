# 放置系统 V3 重构 - 任务清单

## 实现计划

- [x] 1. 创建 PlacementValidatorV3 - 简化红色判定逻辑
  - [x] 1.1 移除"放置范围"检测逻辑
  - [x] 1.2 实现只有两种红色情况的判定
  - [x] 1.3 添加 Player 到障碍物标签列表
  - [x] 1.4 添加 InvalidReason 枚举到 CellState
  - _需求: 2.1, 2.2, 2.3, 2.4_

- [ ]* 1.5 编写 ValidatorV3 单元测试（可选）
  - **属性 1: 红色格子只有两种情况**
  - **验证: 需求 2.1, 2.2, 2.3**

- [x] 2. 创建 PlacementNavigator - 导航和到达检测
  - [x] 2.1 实现导航目标点计算（玩家到预览边界最近点）
  - [x] 2.2 实现到达检测逻辑（Collider 边界接近但不重叠）
  - [x] 2.3 集成 PlayerAutoNavigator
  - [x] 2.4 添加导航取消功能
  - _需求: 3.2, 3.3, 4.1, 5.1, 5.2_

- [ ]* 2.5 编写 Navigator 单元测试（可选）
  - **属性 4: 到达触发放置**
  - **验证: 需求 4.1**

- [x] 3. 更新 PlacementPreviewV3 - 添加位置锁定功能
  - [x] 3.1 添加 isLocked 状态和 lockedPosition 字段
  - [x] 3.2 实现 LockPosition() 和 UnlockPosition() 方法
  - [x] 3.3 修改 UpdatePosition() 在锁定时不更新位置
  - [x] 3.4 添加 GetPreviewBounds() 方法
  - _需求: 3.1, 5.2_

- [x] 4. 创建 PlacementManagerV3 - 状态机管理
  - [x] 4.1 实现 PlacementState 状态机
  - [x] 4.2 实现 Preview 状态逻辑（预览跟随鼠标）
  - [x] 4.3 实现 Locked 状态逻辑（位置锁定）
  - [x] 4.4 实现 Navigating 状态逻辑（导航中）
  - [x] 4.5 实现状态转换逻辑
  - [x] 4.6 修复类结构错误（类定义过早闭合）
  - _需求: 1.1, 1.2, 1.3, 3.1, 3.4, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3_

- [x] 5. 实现放置执行逻辑
  - [x] 5.1 实现 ExecutePlacement() 方法
  - [x] 5.2 实现 Layer 同步（复用 V2 逻辑）
  - [x] 5.3 实现 Sorting Order 计算（复用 V2 逻辑）
  - [x] 5.4 实现背包物品扣除（预留接口）
  - _需求: 4.2, 4.3, 4.4, 6.1, 6.2, 6.3_

- [ ]* 5.5 编写放置执行单元测试（可选）
  - **属性 5: 放置位置一致性**
  - **验证: 需求 4.2**

- [ ] 6. 实现树苗特殊逻辑
  - [x] 6.1 冬季检测和提示（已在 PlacementValidatorV3 实现）
  - [ ] 6.2 耕地检测（待实现）
  - [ ] 6.3 成长边距检测（待实现）
  - _需求: 7.1, 7.2, 7.3_

- [ ] 7. 集成到输入系统
  - [ ] 7.1 修改 GameInputManager 调用 V3 版本
  - [ ] 7.2 修改 HotbarSelectionService 调用 V3 版本
  - _需求: 1.1, 3.1_

- [x] 8. 编译检查 - ✅ 所有文件编译通过
  - [x] PlacementValidatorV3.cs - 0 errors
  - [x] PlacementNavigator.cs - 0 errors
  - [x] PlacementPreviewV3.cs - 0 errors
  - [x] PlacementManagerV3.cs - 0 errors

- [ ] 9. 清理和文档
  - [ ] 9.1 更新 memory.md
  - [ ] 9.2 更新使用指南

## 当前状态

**完成度**: 约 85%
**最后更新**: 2026-01-04

### 已完成
- ✅ 核心组件全部创建完成
- ✅ 状态机逻辑实现
- ✅ 导航和到达检测
- ✅ 预览 UI 锁定功能
- ✅ 放置执行逻辑
- ✅ 编译通过

### 待完成
- ⏳ 集成到输入系统（GameInputManager、HotbarSelectionService）
- ⏳ 树苗特殊逻辑（耕地检测、成长边距检测）
- ⏳ 更新使用指南

