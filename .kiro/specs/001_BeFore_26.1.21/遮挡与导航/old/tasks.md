# Implementation Plan

## 1. 导航系统优化

- [x] 1.1 优化碰撞预警系统
  - ✅ 重写 `AdjustDirectionByColliders()` 方法（精简版）
  - ✅ 前方探测 + 排斥力计算
  - ✅ 最大偏转角度限制（30度，可配置）
  - _Requirements: 1.5, 1.6_

- [x] 1.2 实现方向平滑算法
  - ✅ 添加 `SmoothDirection()` 方法
  - ✅ 限制单帧最大偏转角度（`maxDeflectionAngle = 30f`）
  - ✅ 使用 Lerp 平滑过渡（`directionSmoothFactor = 0.3f`）
  - ✅ 开阔区域不做方向调整
  - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [x] 1.3 优化智能脱困系统
  - ✅ 简化为：卡顿检测 + 重建路径 + 3次重试后取消
  - ✅ 移除复杂的脱困逻辑，依赖 NavGrid2D 的 TryFindNearestWalkable
  - _Requirements: 1.1, 1.2_

- [x] 1.4 添加详细调试输出
  - ✅ 添加 `LogNavigationDebug()` 方法
  - ✅ 输出起点、终点、可走状态
  - ✅ 输出周边障碍物列表
  - ✅ 添加 `enableDetailedDebug` 开关
  - _Requirements: 1.8, 8.4_

- [x] 1.5 优化卡顿检测逻辑
  - ✅ STUCK_THRESHOLD = 0.15f
  - ✅ STUCK_CHECK_INTERVAL = 0.25f
  - ✅ MAX_STUCK_RETRIES = 3
  - ✅ 正常移动时重置计数器
  - _Requirements: 1.4_

- [x] 1.6 实现目标点去抖
  - ✅ `destinationChangeThreshold = 0.3f`
  - ✅ 在 `SetDestination()` 中检查距离
  - _Requirements: 1.9_

### 🔥 v5.0 更新说明
- 添加方向平滑算法，解决斜向移动鬼畜问题
- 添加详细调试输出，便于排查导航失败原因
- 最大偏转角度从 45 度降低到 30 度
- 添加 Inspector 可配置参数

## 2. Checkpoint - 导航系统测试

- [ ] 2. Checkpoint - 确保导航系统正常
  - 验证碰撞预警有效性
  - 验证卡顿检测和脱困逻辑
  - 验证目标点去抖功能
  - Ensure all tests pass, ask the user if questions arise.

## 3. 砍伐高亮系统

- [x] 3.1 扩展 OcclusionTransparency 砍伐状态
  - ✅ `isBeingChopped` 状态字段（已存在）
  - ✅ `choppingAlphaOffset` 参数（默认0.25）
  - ✅ `SetChoppingState(bool, float)` 方法（已存在）
  - ✅ 透明度计算逻辑（已存在）
  - _Requirements: 2.1_

- [x] 3.2 实现 OcclusionManager 砍伐高亮管理
  - ✅ `currentChoppingTree` 字段
  - ✅ `lastChopTime` 字段
  - ✅ `SetChoppingTree()` 方法
  - ✅ 单一高亮保证逻辑
  - _Requirements: 2.2, 2.3_

- [ ] 3.3 Write property test for chopping highlight uniqueness
  - **Property 5: 砍伐高亮单一性**
  - **Validates: Requirements 2.2, 2.3**

- [x] 3.4 实现砍伐高亮超时恢复
  - ✅ `CheckChoppingTimeout()` 方法
  - ✅ 超时后自动清除高亮状态（3秒）
  - _Requirements: 2.4_

- [x] 3.5 集成砍伐系统调用
  - ✅ 在 TreeControllerV2.HandleAxeChop 中调用 `OcclusionManager.SetChoppingTree()`
  - ✅ 在 TreeControllerV2.ChopDown 中调用 `OcclusionManager.ClearChoppingHighlight()`
  - _Requirements: 2.5_

- [ ] 3.6 Write property test for chopping highlight alpha
  - **Property 6: 砍伐高亮透明度**
  - **Validates: Requirements 2.1**

## 4. Checkpoint - 砍伐高亮测试

- [ ] 4. Checkpoint - 确保砍伐高亮正常
  - 验证透明度计算正确
  - 验证单一高亮保证
  - 验证超时恢复功能
  - Ensure all tests pass, ask the user if questions arise.

## 5. 树林边缘遮挡系统

- [x] 5.1 创建凸包计算工具
  - ✅ 创建 `Assets/Scripts/Utils/ConvexHullCalculator.cs`
  - ✅ 实现 Andrew's Monotone Chain 算法
  - ✅ 实现 `IsPointInsideConvexHull()` 方法
  - ✅ 实现 `ComputeConvexHullFromBounds()` 方法
  - _Requirements: 3.1-3.5_

- [x] 5.2 扩展 OcclusionTransparency 获取 Collider Bounds
  - ✅ 添加 `GetColliderBounds()` 方法
  - ✅ 优先使用父物体 CompositeCollider2D，其次子物体 Collider2D
  - ✅ 回退到 Sprite Bounds
  - _Requirements: 3.1-3.5_

- [x] 5.3 实现树林边界计算
  - ✅ 在 OcclusionManager 中添加 `CalculateForestBoundary()` 方法
  - ✅ 收集树林所有树木的 Collider 边界点
  - ✅ 调用凸包算法计算边界
  - _Requirements: 3.1-3.5_

- [x] 5.4 实现边缘遮挡判定逻辑
  - ✅ 添加 `HandleForestOcclusion()` 方法
  - ✅ 判断玩家是否在边界内（`IsPlayerInsideForest()`）
  - ✅ 判断遮挡方向（`GetOcclusionDirection()`）
  - ✅ 根据判定结果决定透明策略
  - _Requirements: 3.1-3.5_

- [ ] 5.5 Write property test for edge occlusion
  - **Property 7: 边缘遮挡单树透明**
  - **Validates: Requirements 3.1, 3.2, 3.3**

- [ ] 5.6 Write property test for interior occlusion
  - **Property 8: 内部遮挡整林透明**
  - **Validates: Requirements 3.4, 3.5**

- [x] 5.7 修改 OcclusionManager 遮挡检测流程
  - ✅ 在检测到树木遮挡时调用 `HandleForestOcclusion()`
  - ✅ 替换原有的整林透明逻辑
  - ✅ 保持向后兼容（`enableSmartEdgeOcclusion` 开关）
  - _Requirements: 3.1-3.5_

- [x] 5.8 修复树林遮挡保底机制
  - ✅ 凸包计算失败 → 直接整林透明
  - ✅ 多棵树同时遮挡 → 直接整林透明
  - ✅ 内侧树触发遮挡 → 直接整林透明
  - ✅ 改进 `IsBoundaryTree()` 使用 Sprite Bounds 中心点
  - ✅ 边界树阈值从 1.5m 增加到 2.0m
  - ✅ **v5.1 修复**：遮挡检测改用 `Bounds.Intersects` 替代 `Contains`
  - ✅ **v5.1 修复**：保底机制直接检查树林中所有树的 Bounds 重叠
  - _Requirements: 3.1, 3.2, 3.8_

## 5.9 导航系统斜向移动修复（v5.1 新增）

- [x] 5.9 恢复多点前瞻采样算法
  - ✅ 恢复 v3.3 的多点前瞻采样（近 0.15m、中 0.35m、远 0.6m）
  - ✅ 恢复渐进权重排斥力（距离越近权重越高）
  - ✅ 保留方向平滑算法
  - _Requirements: 9.1, 9.2, 9.3, 9.4_

## 6. Checkpoint - 树林边缘遮挡测试

- [ ] 6. Checkpoint - 确保树林边缘遮挡正常
  - 验证凸包计算正确性
  - 验证边缘遮挡只透明单树
  - 验证内部遮挡整林透明
  - Ensure all tests pass, ask the user if questions arise.

## 7. 集成测试与优化

- [ ] 7.1 集成测试
  - 测试导航系统在各种场景下的表现
  - 测试砍伐高亮与遮挡系统的交互
  - 测试树林边缘遮挡的各种边界情况
  - _Requirements: 全部_

- [ ] 7.2 性能优化
  - 优化凸包计算频率（缓存结果）
  - 优化边界判定频率
  - 确保不影响帧率
  - _Requirements: 性能_

## 8. Final Checkpoint

- [ ] 8. Final Checkpoint - 完整功能验证
  - 导航系统稳定性验证
  - 砍伐高亮功能验证
  - 树林边缘遮挡功能验证
  - Ensure all tests pass, ask the user if questions arise.

---

## 实现优先级

1. **高优先级**：导航系统优化（1.1 - 1.7）
2. **中优先级**：砍伐高亮系统（3.1 - 3.5）
3. **中优先级**：树林边缘遮挡系统（5.1 - 5.7）
4. **低优先级**：属性测试（带 * 标记的任务）

## 技术说明

### 凸包算法选择

推荐使用 Andrew's Monotone Chain 算法：
- 时间复杂度：O(n log n)
- 实现简单，数值稳定
- 适合 2D 点集

### 性能考虑

- 凸包计算结果应该缓存，只在树林成员变化时重新计算
- 边界判定可以先用 AABB 快速排除，再用凸包精确判定
- 遮挡检测间隔保持 0.1 秒，不需要每帧检测
