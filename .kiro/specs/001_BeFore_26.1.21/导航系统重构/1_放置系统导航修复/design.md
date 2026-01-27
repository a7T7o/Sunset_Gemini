# 设计文档：放置系统导航修复

## 概述

本设计修复 PlacementNavigator 的距离计算问题，采用与右键可交互物品导航相同的 ClosestPoint 统一方案。

## 问题分析

### 当前实现问题

1. **导航目标点计算**：`CalculateNavigationTarget()` 基于玩家当前位置计算目标点，导致目标点可能在格子侧面
2. **到达判断**：`CheckReached()` 使用 Bounds 距离计算，与 PlayerAutoNavigator 的到达判断不一致
3. **结果**：PlayerAutoNavigator 认为"到达目标"，但 PlacementNavigator 认为"未到达触发距离"

### 修复方案

统一使用"玩家中心到预览格子边缘的最近点"计算距离：

```
距离 = Vector3.Distance(playerCenter, previewBounds.ClosestPoint(playerCenter))
```

## 架构

### 修改文件

- `Assets/YYY_Scripts/Service/Placement/PlacementNavigator.cs`

### 核心修改

#### 1. 新增 CalculateDistanceToPreview() 方法

```csharp
/// <summary>
/// 计算玩家中心到预览格子边缘的距离（ClosestPoint 方式）
/// </summary>
private float CalculateDistanceToPreview(Vector3 playerCenter, Bounds previewBounds)
{
    Vector3 closestPoint = previewBounds.ClosestPoint(playerCenter);
    return Vector3.Distance(playerCenter, closestPoint);
}
```

#### 2. 修改 CalculateNavigationTarget()

改为返回预览格子边缘的最近点（基于玩家中心），不再向外偏移：

```csharp
public Vector3 CalculateNavigationTarget(Bounds playerBounds, Bounds previewBounds)
{
    Vector3 playerCenter = playerBounds.center;
    
    // 直接返回预览格子边缘的最近点
    Vector3 closestPoint = previewBounds.ClosestPoint(playerCenter);
    
    // 如果玩家中心在预览 Bounds 内部，找到最近的边界点
    if (previewBounds.Contains(playerCenter))
    {
        // ... 处理内部情况 ...
    }
    
    return closestPoint;
}
```

#### 3. 修改 CheckReached() 和 IsAlreadyNearTarget()

改为使用 ClosestPoint 距离计算：

```csharp
public bool CheckReached()
{
    if (playerCollider == null) return false;
    
    Vector3 playerCenter = playerCollider.bounds.center;
    float distance = CalculateDistanceToPreview(playerCenter, targetBounds);
    
    return distance <= triggerDistance;
}
```

## 正确性属性

### 属性 1：ClosestPoint 距离一致性

*对于任意*玩家位置和预览格子，导航目标点计算和到达判断使用相同的距离计算方式（ClosestPoint）。

**验证: 需求 1.1, 1.2**

### 属性 2：方向无关性

*对于任意*接近方向（水平、垂直、对角线），当玩家中心到预览格子边缘的距离小于 triggerDistance 时，系统应触发放置。

**验证: 需求 2.1, 2.2, 2.3, 2.4, 2.5**

## 测试策略

### 手动测试

1. 从左侧水平接近预览格子，验证放置触发
2. 从右侧水平接近预览格子，验证放置触发
3. 从上方垂直接近预览格子，验证放置触发
4. 从下方垂直接近预览格子，验证放置触发
5. 从对角线方向接近预览格子，验证放置触发
