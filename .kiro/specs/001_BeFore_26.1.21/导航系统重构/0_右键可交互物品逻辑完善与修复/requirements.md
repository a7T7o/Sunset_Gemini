# 右键可交互物品逻辑完善与修复 - 需求文档

**创建日期**: 2026-01-19
**优先级**: P0（严重 Bug）

---

## 一、问题描述

### 用户报告

> 右键可交互物品时存储目的地但不导航，下次右键另一个可交互物品时才走向上一个目的地

### 问题表现

1. 玩家右键点击目标 A → 玩家不动
2. 玩家右键点击目标 B → 玩家走向 A
3. 玩家右键点击目标 C → 玩家走向 B
4. ...以此类推

这是一个"延迟一拍"的 Bug。

---

## 二、根因分析

### 问题代码位置

`Assets/YYY_Scripts/Service/Player/PlayerAutoNavigator.cs` 的 `FollowTarget()` 方法

### 问题根因

`FollowTarget()` 方法在调用 `BuildPath()` 之前没有同步更新 `targetPoint`，导致 `BuildPath()` 使用了旧的目标位置。

```csharp
public void FollowTarget(Transform t, float stopRadius, System.Action onArrived)
{
    ForceCancel();
    _navToken++;
    _currentToken = _navToken;
    
    targetTransform = t;  // ← 只设置 targetTransform
    // ← 缺少：targetPoint = t.position;
    followStopRadius = Mathf.Max(0.1f, stopRadius);
    _onArrivedCallback = onArrived;
    active = true;

    BuildPath();  // ← BuildPath 使用旧的 targetPoint
}

private void BuildPath()
{
    Vector2 end = targetPoint;  // ← 使用 targetPoint，不是 targetTransform.position
}
```

### 时序问题

1. `FollowTarget()` 设置 `targetTransform = t`
2. `FollowTarget()` 调用 `BuildPath()`
3. `BuildPath()` 使用 `targetPoint`（此时还是上一次的值！）
4. 下一帧 `Update()` 才会更新 `targetPoint = targetTransform.position`

### 对比 SetDestination

`SetDestination()` 方法正确地设置了 `targetPoint`：

```csharp
public void SetDestination(Vector3 worldPos)
{
    targetTransform = null;
    targetPoint = worldPos;  // ← 直接设置 targetPoint
    active = true;
    BuildPath();  // ← BuildPath 使用正确的 targetPoint
}
```

---

## 三、用户故事

### US-1：修复延迟一拍 Bug

**作为** 玩家
**我希望** 右键点击可交互物品时，角色立即走向该物品
**以便** 我能正常与物品交互

**验收标准**:
- [ ] 右键点击目标 A，角色立即走向 A
- [ ] 右键点击目标 B，角色立即走向 B（不是 A）
- [ ] 连续快速点击不同目标，角色始终走向最后点击的目标

### US-2：添加调试日志

**作为** 开发者
**我希望** 在 BuildPath 中输出目标点信息
**以便** 未来能快速定位类似问题

**验收标准**:
- [ ] `BuildPath()` 输出 `start`、`end`、`targetTransform.name`
- [ ] 日志受 `enableDetailedDebug` 开关控制

---

## 四、修复方案

### 方案 A：在 FollowTarget 中同步 targetPoint（推荐）

在 `FollowTarget()` 中添加一行代码：

```csharp
public void FollowTarget(Transform t, float stopRadius, System.Action onArrived)
{
    ForceCancel();
    
    _navToken++;
    _currentToken = _navToken;
    
    targetTransform = t;
    targetPoint = t.position;  // ← 添加这一行
    followStopRadius = Mathf.Max(0.1f, stopRadius);
    _onArrivedCallback = onArrived;
    active = true;

    // ...
    BuildPath();
}
```

**优点**:
- 修改最小
- 与 `SetDestination` 保持一致
- 不影响其他逻辑

### 方案 B：在 BuildPath 中使用 targetTransform.position

修改 `BuildPath()` 的逻辑：

```csharp
private void BuildPath()
{
    Vector2 end = targetTransform != null ? (Vector2)targetTransform.position : (Vector2)targetPoint;
}
```

**缺点**:
- 需要修改 `BuildPath()` 的多处逻辑
- 可能引入新的问题

### 推荐方案

**方案 A** - 修改最小，风险最低。

---

## 五、影响范围

### 受影响的功能

- 右键点击可交互物品（箱子、NPC、资源节点等）
- 所有通过 `FollowTarget()` 触发的导航

### 不受影响的功能

- 右键点击空地（使用 `SetDestination()`）
- WASD 移动
- 其他导航逻辑

---

## 六、测试计划

### 测试场景 1：基本导航

1. 玩家站在位置 P
2. 右键点击箱子 A（距离 > 交互距离）
3. 验证：玩家走向 A

### 测试场景 2：连续点击不同目标

1. 玩家站在位置 P
2. 右键点击箱子 A
3. 立即右键点击箱子 B
4. 验证：玩家走向 B（不是 A）

### 测试场景 3：点击空地后点击目标

1. 玩家站在位置 P
2. 右键点击空地 X
3. 玩家开始走向 X
4. 右键点击箱子 A
5. 验证：玩家改为走向 A

### 测试场景 4：在交互距离内点击

1. 玩家站在箱子 A 旁边（距离 < 交互距离）
2. 右键点击箱子 A
3. 验证：直接打开箱子 UI，不触发导航
