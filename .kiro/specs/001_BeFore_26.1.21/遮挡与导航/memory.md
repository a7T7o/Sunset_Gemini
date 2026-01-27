# 遮挡与导航系统 - 开发记忆

## 模块概述
遮挡透明系统和导航系统的优化，核心目标是解决遮挡检测不精确、命中检测起点错误、导航卡顿等问题。

## 当前状态
- **完成度**: 95%
- **最后更新**: 2025-12-22
- **状态**: v5.2 导航优化已实现，待用户测试

## v5.2 核心改进总结

### 1. 斜向移动朝向固定
```csharp
// GetFacingDirection() - 斜向移动时固定朝向为左或右
if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y)) return moveDir;  // 水平移动
if (Mathf.Abs(moveDir.y) > Mathf.Abs(moveDir.x) * 1.5f) return moveDir;  // 垂直移动
return moveDir.x > 0 ? Vector2.right : Vector2.left;  // 斜向移动 → 固定左/右
```

### 2. 路径平滑处理
- `SmoothPath()` - 构建路径后移除冗余点
- `TrySkipWaypoints()` - 运行时跳过可直达的中间点
- `HasLineOfSight()` - 视线检测（多点采样）

### 3. 详细调试输出
- 在 Inspector 中启用 `enableDetailedDebug`
- 卡顿时输出完整诊断报告：玩家位置、目标位置、路径信息、网格信息、周边障碍物、调试日志历史

### 4. PlayerMovement 朝向参数
```csharp
// 新增重载方法
public void SetMovementInput(Vector2 input, bool isShifted, Vector2? facingDirection)
```

## 会话记录

### 会话 7 - 2025-12-22

**用户需求**:
> 1、斜着走还是会鬼畜转向，摇摆脑袋，斜着走的时候就让朝向固定位左或者右
> 2、路线规划肯定也有问题路线肯定是崎岖的
> 3、需要在卡顿出现时进一步用更深入的控制台输出来获取导航失败的时候的周边物体的情况

**完成任务**:
1. **斜向移动朝向固定** - 斜向移动时固定朝向为左或右，避免摇头
2. **路径平滑处理** - 添加视线优化，移除崎岖路径中的冗余点
3. **详细调试输出** - 卡顿时输出完整诊断报告

**修改文件**:
- `Assets/YYY_Scripts/Service/Player/PlayerAutoNavigator.cs` - v5.2 终极优化版
- `Assets/YYY_Scripts/Service/Player/PlayerMovement.cs` - 添加朝向参数重载

**解决方案**:

#### 1. 斜向移动朝向固定
**问题**：斜向移动时角色频繁转向，摇头
**修复方案**：
```csharp
private Vector2 GetFacingDirection(Vector2 moveDir)
{
    // 如果主要是水平或垂直移动，使用移动方向
    if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y)) return moveDir;
    if (Mathf.Abs(moveDir.y) > Mathf.Abs(moveDir.x) * 1.5f) return moveDir;
    
    // 斜向移动时，固定朝向为左或右
    return moveDir.x > 0 ? Vector2.right : Vector2.left;
}
```

#### 2. 路径平滑处理
**问题**：A* 路径崎岖，导致频繁转向
**修复方案**：
- 添加 `SmoothPath()` 方法，使用视线检测移除冗余路径点
- 添加 `TrySkipWaypoints()` 方法，运行时跳过可直达的中间点
- 添加 `HasLineOfSight()` 方法，检测两点之间是否有障碍物

#### 3. 详细调试输出
**问题**：卡顿时缺少诊断信息
**修复方案**：
- 添加 `LogFullDebugInfo()` 方法，输出完整诊断报告
- 包含：玩家位置、目标位置、路径信息、网格信息、周边障碍物、调试日志历史、卡顿信息
- 使用 `debugLogs` 列表记录寻路过程中的关键事件

**遗留问题**:
- [ ] 用户测试验证修复效果

---

### 会话 6 - 2025-12-22

**用户需求**:
> 继续上次对话，修复遮挡系统和导航系统的问题

**完成任务**:
1. **修复遮挡系统** - 将遮挡检测从 `Contains(playerCenterPos)` 改为 `Intersects(playerBounds)`
2. **修复保底机制** - 改进 `HandleForestOcclusion()` 直接检查树林中所有树的 Bounds 重叠
3. **恢复导航多点前瞻** - 恢复 v3.3 的多点前瞻采样算法

**修改文件**:
- `Assets/YYY_Scripts/Service/Rendering/OcclusionManager.cs` - 修复遮挡检测和保底机制
- `Assets/YYY_Scripts/Service/Player/PlayerAutoNavigator.cs` - 恢复多点前瞻采样

**解决方案**:

#### 1. 遮挡系统修复（v5.1）
**问题根因**：
- 原逻辑使用 `occluderBounds.Contains(playerCenterPos)` 检测遮挡
- 这要求玩家中心点必须在树的 Bounds 内部
- 外侧树的 Bounds 可能不包含玩家中心点，导致外侧树永远不会被检测到
- 保底机制 `currentlyOccluding.Count >= 2` 永远不会触发

**修复方案**：
```csharp
// 原逻辑（错误）
isOccluding = occluderBounds.Contains(playerCenterPos);

// 新逻辑（正确）
isOccluding = occluderBounds.Intersects(playerBounds);
```

**保底机制改进**：
- 不再依赖 `currentlyOccluding`（因为之前的检测可能漏掉外侧树）
- 直接遍历树林中的所有树，检查 Bounds 重叠
- `HandleForestOcclusion()` 新增 `playerBounds` 参数

#### 2. 导航系统修复（v5.1）
**问题根因**：
- v5.0 移除了 v3.3 的多点前瞻采样
- 单点探测不够平滑，导致斜向移动鬼畜

**修复方案**：
- 恢复多点前瞻采样（近 0.15m、中 0.35m、远 0.6m）
- 恢复渐进权重排斥力（距离越近权重越高）
- 保留方向平滑算法

**遗留问题**:
- [ ] 用户测试验证修复效果

---

### 会话 5 - 2025-12-22

**用户需求**:
> 1、树林遮挡有部分情况不能检测正确，内侧树触发隐藏时应该触发整林透明（保底机制）
> 2、导航斜向移动鬼畜，需要优化
> 3、树木根部附近导航失败，需要详细调试输出

**完成任务**:
1. **更新需求文档** - 添加需求 8（动态障碍物导航同步）和需求 9（斜向移动平滑优化）
2. **修复需求 3** - 智能树林遮挡的保底机制逻辑
3. **更新技术方案** - 添加方向平滑算法和动态障碍物同步流程

**修改文件**:
- `.kiro/specs/occlusion-navigation/requirements.md` - 更新需求文档 v4.0

**待实现的修复**:

#### 1. 遮挡系统修复（OcclusionManager.cs）✅ 已完成
**问题**：内侧树触发遮挡时未整林透明
**修复方案**：
- 改进 `IsBoundaryTree()` 判定逻辑，使用 Sprite Bounds 中心点
- 在 `HandleForestOcclusion()` 中增加保底机制
- 凸包计算失败 → 直接整林透明
- 多棵树同时遮挡 → 直接整林透明
- 内侧树触发 → 直接整林透明
- 边界树阈值从 1.5m 增加到 2.0m

#### 2. 导航系统修复（PlayerAutoNavigator.cs）✅ 已完成
**问题**：斜向移动鬼畜、树木根部导航失败
**修复方案**：
- 添加方向平滑算法（`SmoothDirection()`）
- 限制单帧最大偏转角度为 30 度（可配置）
- 使用 Lerp 平滑过渡方向
- 添加详细调试输出（`LogNavigationDebug()`）
- 开阔区域不做方向调整

#### 3. 导航网格同步（NavGrid2D.cs + TreeController.cs）✅ 已验证
**问题**：树木成长后导航网格未及时更新
**验证结果**：
- TreeController 的 `RequestNavGridRefresh()` 已正常工作
- 碰撞体状态变化时会触发 `NavGrid2D.OnRequestGridRefresh`
- 延迟 0.2 秒刷新，避免频繁调用

**遗留问题**:
- [ ] 用户测试验证修复效果

---

### 会话 3 - 2025-12-22

**用户需求**:
> 联合 NavGrid2D 脚本一起进行完善，结合所有项目经验和专业思维做一个最优的最简洁高效的导航功能，要无bug的导航

**完成任务**:
1. **导航系统 v4.0 重构** - 大幅精简 PlayerAutoNavigator
2. **砍伐高亮系统** - OcclusionManager 添加砍伐高亮管理
3. **凸包计算工具** - 创建 ConvexHullCalculator.cs

**修改文件**:
- `Assets/YYY_Scripts/Service/Player/PlayerAutoNavigator.cs` - 从 1000+ 行精简到 ~250 行
- `Assets/YYY_Scripts/Service/Rendering/OcclusionManager.cs` - 添加砍伐高亮管理
- `Assets/Scripts/Utils/ConvexHullCalculator.cs` - 新建凸包计算工具

**解决方案**:

#### 1. 导航系统 v4.0 重构
**设计原则**：
- 简单就是稳定 - 移除所有实验性功能
- 信任 A* 路径 - 不做过度的路径优化
- 快速失败 - 卡住就取消，不无限重试

**移除的功能**：
- 混合导航（enableHybridNavigation）
- 连续空间导航（CalculateContinuousNavigation）
- 复杂路径合并（MergePathWithOld）
- 视线优化（OptimizePathByLineOfSight）
- 多点前瞻采样（改为简单的单点探测）

**保留的核心功能**：
- A* 路径执行
- 简单卡顿检测（3次重试后取消）
- 目标点去抖（0.3m）
- 碰撞微调（最大偏转45度）

#### 2. 砍伐高亮系统
- `OcclusionManager.SetChoppingTree()` - 设置当前砍伐的树
- `OcclusionManager.ClearChoppingHighlight()` - 清除高亮
- `OcclusionManager.CheckChoppingTimeout()` - 3秒超时自动清除
- 确保同时只有一棵树处于砍伐高亮状态

#### 3. 凸包计算工具
- Andrew's Monotone Chain 算法
- `ComputeConvexHull()` - 计算点集凸包
- `IsPointInsideConvexHull()` - 判断点是否在凸包内
- `ComputeConvexHullFromBounds()` - 从 Bounds 列表生成凸包

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 使用 Bounds.Intersects 检测遮挡 | Contains 只检测中心点，外侧树无法触发 | 2025-12-22 |
| 保底机制直接检查 Bounds 重叠 | 不依赖 currentlyOccluding，更可靠 | 2025-12-22 |
| 恢复多点前瞻采样 | v5.0 单点探测导致斜向移动鬼畜 | 2025-12-22 |
| 使用像素采样而非 PolygonCollider2D | 碰撞体只覆盖树根，不是整个树冠 | 2025-12-18 |
| 添加回退机制 | 纹理可能未设置为可读，需要兼容 | 2025-12-18 |
| 保持树林连通判定不变 | 该功能已经稳定，无需修改 | 2025-12-18 |
| 使用凸包计算树林边界 | 凸包算法简单高效，适合 2D 场景 | 2025-12-22 |
| 导航系统精简化 | 简单就是稳定，移除实验性功能 | 2025-12-22 |
| 方向平滑限制30度 | 避免斜向移动鬼畜，同时保持响应性 | 2025-12-22 |
| 边界树阈值2.0m | 更宽松的判定，减少误判内侧树 | 2025-12-22 |
| 保底机制优先级调整 | 先检查凸包有效性，再检查多树遮挡，最后检查边界树 | 2025-12-22 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Service/Rendering/OcclusionTransparency.cs` | 遮挡透明组件（含像素采样） |
| `Assets/YYY_Scripts/Service/Rendering/OcclusionManager.cs` | 遮挡管理器（含边缘遮挡、保底机制） |
| `Assets/YYY_Scripts/Service/Player/PlayerAutoNavigator.cs` | 自动导航（v5.0 方向平滑版） |
| `Assets/YYY_Scripts/Service/Navigation/NavGrid2D.cs` | A* 寻路网格 |
| `Assets/YYY_Scripts/Controller/TreeController.cs` | 树木控制器（碰撞体变化通知） |
| `Assets/Scripts/Utils/ConvexHullCalculator.cs` | 凸包计算工具 |
