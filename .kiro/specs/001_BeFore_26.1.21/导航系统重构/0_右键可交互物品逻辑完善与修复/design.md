# 右键可交互物品逻辑完善与修复 - 设计文档

**创建日期**: 2026-01-19

---

## 一、问题根因

`FollowTarget()` 方法在调用 `BuildPath()` 之前没有同步更新 `targetPoint`。

```csharp
// 问题代码
public void FollowTarget(Transform t, float stopRadius, System.Action onArrived)
{
    targetTransform = t;  // ← 只设置 targetTransform
    // ← 缺少：targetPoint = t.position;
    BuildPath();  // ← BuildPath 使用旧的 targetPoint
}
```

---

## 二、修复方案

### 方案 A：在 FollowTarget 中同步 targetPoint（采用）

在 `FollowTarget()` 中添加一行代码：

```csharp
targetTransform = t;
targetPoint = t.position;  // ← 添加这一行
```

**修改位置**: `PlayerAutoNavigator.cs` 第 117 行附近

---

## 三、影响范围

- 仅修改 `FollowTarget()` 方法
- 不影响 `SetDestination()` 和其他导航逻辑
- 不影响 `Update()` 中的 `targetPoint` 更新逻辑

---

## 四、验证方法

1. 右键点击目标 A → 角色立即走向 A
2. 连续点击不同目标 → 角色始终走向最后点击的目标
