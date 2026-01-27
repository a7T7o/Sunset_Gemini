# 遮挡与导航系统 - 设计文档

**版本**: v3.0  
**日期**: 2025-12-22  
**状态**: 待审核

---

## 一、概述

本设计文档描述遮挡透明系统和导航系统的全面优化方案，包括：
1. 导航系统终极优化 - 解决卡顿鬼畜问题
2. 砍伐树木高亮显示 - 视觉反馈增强
3. 智能树林边缘遮挡 - 精确的遮挡判定

---

## 二、架构设计

### 2.1 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                      导航与遮挡系统                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │  NavGrid2D      │    │ OcclusionManager │                │
│  │  ─────────────  │    │  ─────────────── │                │
│  │  • A* 寻路      │    │  • 遮挡检测      │                │
│  │  • 网格构建     │    │  • 树林边界计算   │                │
│  │  • 障碍物检测   │    │  • 砍伐高亮管理   │                │
│  └────────┬────────┘    └────────┬────────┘                │
│           │                      │                          │
│           ▼                      ▼                          │
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │PlayerAutoNavigator│  │OcclusionTransparency│             │
│  │  ─────────────  │    │  ─────────────── │                │
│  │  • 路径执行     │    │  • 透明度控制    │                │
│  │  • 卡顿检测     │    │  • 砍伐状态      │                │
│  │  • 智能脱困     │    │  • 像素采样      │                │
│  │  • 碰撞预警     │    │                  │                │
│  └─────────────────┘    └─────────────────┘                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 组件职责

| 组件 | 职责 |
|------|------|
| NavGrid2D | 网格构建、A* 寻路、障碍物检测 |
| PlayerAutoNavigator | 路径执行、卡顿检测、智能脱困、碰撞预警 |
| OcclusionManager | 遮挡检测、树林边界计算、砍伐高亮管理 |
| OcclusionTransparency | 单个物体的透明度控制、砍伐状态 |

---

## 三、导航系统设计

### 3.1 核心问题分析

当前导航系统的问题根源：
1. **卡顿检测过于敏感**：位置变化阈值太小，正常减速也被判定为卡顿
2. **脱困方向单一**：只尝试重新规划路径，不尝试物理脱困
3. **碰撞预警不足**：检测到障碍物时才调整，没有提前预判

### 3.2 终极优化方案

结合历史版本的最优部分，设计新的导航方案：

#### 3.2.1 多层防卡顿系统

```
┌─────────────────────────────────────────────────────────┐
│                    防卡顿系统                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  第一层：碰撞预警（预防）                                │
│  ├─ 多点前瞻采样（近0.15m、中0.35m、远0.6m）            │
│  ├─ 渐进权重排斥力（距离越近权重越高）                   │
│  └─ 最大偏转角度限制（60度）                            │
│                                                         │
│  第二层：卡顿检测（发现）                                │
│  ├─ 位置历史记录（最近10个位置）                        │
│  ├─ 平均移动距离计算                                    │
│  └─ 卡顿阈值判定（< 0.3m/检测周期）                     │
│                                                         │
│  第三层：智能脱困（处理）                                │
│  ├─ 障碍物法线方向推出                                  │
│  ├─ 多方向尝试（±15°, ±30°, ±45°, ±60°, ±75°）        │
│  └─ 最大重试次数限制（3次）                             │
│                                                         │
│  第四层：优雅降级（兜底）                                │
│  ├─ 取消导航                                            │
│  └─ 通知玩家                                            │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

#### 3.2.2 碰撞预警算法

```csharp
// 多点前瞻采样
float[] aheadDistances = runWhileNavigating 
    ? new float[] { 0.15f, 0.35f, 0.6f }   // 跑步时前瞻更远
    : new float[] { 0.1f, 0.25f, 0.45f };  // 行走时前瞻较近

foreach (float ahead in aheadDistances)
{
    Vector2 probe = pos + desiredDir * ahead;
    var hits = Physics2D.OverlapCircleAll(probe, clearance);
    
    // 距离越近的采样点权重越高
    float weight = 1f / (ahead + 0.1f);
    
    // 计算排斥力
    foreach (var hit in hits)
    {
        if (IsObstacle(hit))
        {
            Vector2 away = probe - hit.ClosestPoint(probe);
            float repulseStrength = 1f / (away.magnitude * away.magnitude + 0.1f);
            totalRepulse += away.normalized * repulseStrength * weight;
        }
    }
}

// 限制最大偏转角度
float angle = Vector2.SignedAngle(desiredDir, corrected);
if (Mathf.Abs(angle) > 60f)
{
    corrected = RotateVector(desiredDir, Mathf.Sign(angle) * 60f);
}
```

#### 3.2.3 智能脱困算法

```csharp
private bool TryFindEscapePoint(Vector2 start, out Vector2 escape)
{
    // 1. 收集周围所有障碍物的推出方向
    Vector2 pushDirection = Vector2.zero;
    var hits = Physics2D.OverlapCircleAll(start, playerRadius + 0.02f);
    
    foreach (var hit in hits)
    {
        if (!IsObstacle(hit)) continue;
        
        Vector2 closest = hit.ClosestPoint(start);
        Vector2 away = start - closest;
        if (away.sqrMagnitude < 1e-4f)
        {
            away = start - (Vector2)hit.bounds.center;
        }
        pushDirection += away.normalized;
    }
    
    if (pushDirection == Vector2.zero) return false;
    
    // 2. 沿推出方向寻找安全点
    Vector2 dirOut = pushDirection.normalized;
    for (float d = 0.1f; d <= 3f; d += 0.2f)
    {
        Vector2 candidate = start + dirOut * d;
        if (IsPositionSafe(candidate))
        {
            escape = candidate;
            return true;
        }
    }
    
    return false;
}
```

### 3.3 参数配置

| 参数 | 默认值 | 说明 |
|------|--------|------|
| STUCK_DISTANCE_THRESHOLD | 0.3f | 卡顿判定阈值（米） |
| STUCK_CHECK_COUNT | 3 | 连续几次位置不变视为卡顿 |
| STUCK_CHECK_INTERVAL | 0.2f | 卡顿检测间隔（秒） |
| MAX_STUCK_RETRIES | 3 | 最大脱困重试次数 |
| MAX_DEFLECTION_ANGLE | 60f | 最大偏转角度（度） |

---

## 四、砍伐高亮系统设计

### 4.1 状态管理

```
┌─────────────────────────────────────────────────────────┐
│                  砍伐高亮状态机                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌─────────┐    砍伐开始    ┌─────────┐                │
│  │  正常   │ ─────────────▶ │  高亮   │                │
│  │  状态   │                │  状态   │                │
│  └─────────┘                └────┬────┘                │
│       ▲                          │                      │
│       │                          │                      │
│       │    3秒超时/树倒下        │                      │
│       └──────────────────────────┘                      │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 4.2 透明度计算

```csharp
// 砍伐高亮透明度 = 遮挡透明度 + 0.5（更不透明，更明显）
float finalAlpha = occludedAlpha;
if (isBeingChopped && isOccluding)
{
    finalAlpha = Mathf.Min(1f, occludedAlpha + 0.5f);
}
targetAlpha = isOccluding ? finalAlpha : 1f;
```

### 4.3 单一高亮保证

```csharp
// OcclusionManager 中管理当前高亮的树
private OcclusionTransparency currentChoppingTree;
private float lastChopTime;

public void SetChoppingTree(OcclusionTransparency tree)
{
    // 清除之前的高亮
    if (currentChoppingTree != null && currentChoppingTree != tree)
    {
        currentChoppingTree.SetChoppingState(false);
    }
    
    // 设置新的高亮
    currentChoppingTree = tree;
    if (tree != null)
    {
        tree.SetChoppingState(true, 0.5f);
        lastChopTime = Time.time;
    }
}

void Update()
{
    // 3秒超时自动清除
    if (currentChoppingTree != null && Time.time - lastChopTime > 3f)
    {
        currentChoppingTree.SetChoppingState(false);
        currentChoppingTree = null;
    }
}
```

---

## 五、树林边缘遮挡系统设计

### 5.1 核心概念

```
┌─────────────────────────────────────────────────────────┐
│                    树林边界示意图                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│         🌲    🌲    🌲    🌲    🌲                      │
│       ╱                            ╲                    │
│      🌲                              🌲   ← 边界树木    │
│     │                                │                  │
│     🌲    ┌──────────────────┐      🌲                  │
│     │    │                  │      │                   │
│     🌲    │   树林内部区域   │      🌲                  │
│     │    │                  │      │                   │
│     🌲    └──────────────────┘      🌲                  │
│      ╲                            ╱                     │
│       🌲    🌲    🌲    🌲    🌲                        │
│                                                         │
│  玩家在边界外被上方树遮挡 → 只透明该树                   │
│  玩家在边界内被任意树遮挡 → 整片树林透明                 │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 5.2 边界计算算法

使用凸包算法计算树林边界：

```csharp
/// <summary>
/// 计算树林边界（凸包）
/// </summary>
private List<Vector2> CalculateForestBoundary(HashSet<OcclusionTransparency> forest)
{
    // 1. 收集所有树木的 Collider 边界点
    List<Vector2> points = new List<Vector2>();
    foreach (var tree in forest)
    {
        Bounds bounds = tree.GetColliderBounds();
        // 添加四个角点
        points.Add(new Vector2(bounds.min.x, bounds.min.y));
        points.Add(new Vector2(bounds.max.x, bounds.min.y));
        points.Add(new Vector2(bounds.min.x, bounds.max.y));
        points.Add(new Vector2(bounds.max.x, bounds.max.y));
    }
    
    // 2. 计算凸包
    return ConvexHull(points);
}

/// <summary>
/// 判断点是否在凸包内部
/// </summary>
private bool IsPointInsideConvexHull(Vector2 point, List<Vector2> hull)
{
    int n = hull.Count;
    for (int i = 0; i < n; i++)
    {
        Vector2 a = hull[i];
        Vector2 b = hull[(i + 1) % n];
        
        // 叉积判断点在边的哪一侧
        float cross = (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
        if (cross < 0) return false;  // 点在边的右侧，不在凸包内
    }
    return true;
}
```

### 5.3 遮挡判定逻辑

```csharp
private void HandleForestOcclusion(OcclusionTransparency occludingTree, Vector2 playerPos)
{
    // 1. 查找连通的树林
    HashSet<OcclusionTransparency> forest = FindConnectedForest(occludingTree);
    
    // 2. 计算树林边界
    List<Vector2> boundary = CalculateForestBoundary(forest);
    
    // 3. 判断玩家是否在边界内
    bool playerInsideForest = IsPointInsideConvexHull(playerPos, boundary);
    
    // 4. 判断遮挡树木的位置（相对于玩家）
    Vector2 treePos = occludingTree.transform.position;
    bool isTopOcclusion = treePos.y > playerPos.y;  // 树在玩家上方
    
    // 5. 决定透明策略
    if (playerInsideForest)
    {
        // 玩家在树林内部 → 整片树林透明
        SetForestTransparent(forest, true);
    }
    else if (isTopOcclusion)
    {
        // 玩家在边界外，被上方/左侧/右侧的树遮挡 → 只透明该树
        occludingTree.SetOccluding(true);
        // 其他树保持不透明
    }
    else
    {
        // 玩家被下方的树遮挡，可能正在进入树林
        // 需要进一步判断玩家是否即将进入
        if (IsPlayerEnteringForest(playerPos, boundary))
        {
            SetForestTransparent(forest, true);
        }
        else
        {
            occludingTree.SetOccluding(true);
        }
    }
}
```

### 5.4 边缘判定优化

```csharp
/// <summary>
/// 判断遮挡树木是否是边界树（最外圈）
/// </summary>
private bool IsBoundaryTree(OcclusionTransparency tree, List<Vector2> boundary)
{
    Vector2 treePos = tree.transform.position;
    
    // 检查树木位置是否在边界线上或非常接近边界
    float minDistance = float.MaxValue;
    for (int i = 0; i < boundary.Count; i++)
    {
        Vector2 a = boundary[i];
        Vector2 b = boundary[(i + 1) % boundary.Count];
        
        float dist = PointToLineDistance(treePos, a, b);
        minDistance = Mathf.Min(minDistance, dist);
    }
    
    return minDistance < 1f;  // 距离边界小于1米视为边界树
}

/// <summary>
/// 判断遮挡方向（上/下/左/右）
/// </summary>
private OcclusionDirection GetOcclusionDirection(Vector2 playerPos, Vector2 treePos)
{
    Vector2 delta = treePos - playerPos;
    
    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
    {
        return delta.x > 0 ? OcclusionDirection.Right : OcclusionDirection.Left;
    }
    else
    {
        return delta.y > 0 ? OcclusionDirection.Top : OcclusionDirection.Bottom;
    }
}
```

---

## 六、数据模型

### 6.1 导航状态

```csharp
public class NavigationState
{
    public bool IsActive { get; set; }
    public Vector3 TargetPoint { get; set; }
    public List<Vector2> Path { get; set; }
    public int PathIndex { get; set; }
    public int StuckRetryCount { get; set; }
    public List<Vector2> RecentPositions { get; set; }
}
```

### 6.2 砍伐高亮状态

```csharp
public class ChoppingHighlightState
{
    public OcclusionTransparency CurrentTree { get; set; }
    public float LastChopTime { get; set; }
    public float HighlightAlphaOffset { get; set; }  // 0.5
    public float TimeoutDuration { get; set; }       // 3秒
}
```

### 6.3 树林边界数据

```csharp
public class ForestBoundaryData
{
    public HashSet<OcclusionTransparency> Trees { get; set; }
    public List<Vector2> ConvexHull { get; set; }
    public Bounds BoundingBox { get; set; }
}
```

---

## 七、Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 卡顿检测与脱困

*For any* 玩家位置序列，如果连续 3 次位置变化小于阈值（0.3m），系统应该触发脱困逻辑，且脱困方向应该是远离障碍物的方向。

**Validates: Requirements 1.1, 1.2**

### Property 2: 脱困重试限制

*For any* 卡顿场景，系统最多尝试脱困 3 次，超过后应该取消导航。

**Validates: Requirements 1.3**

### Property 3: 碰撞预警有效性

*For any* 玩家移动方向和障碍物配置，如果前方有障碍物，调整后的方向应该与原方向夹角不超过 60 度，且调整后的方向应该避开障碍物。

**Validates: Requirements 1.5, 1.6**

### Property 4: 目标点去抖

*For any* 连续的导航请求序列，如果两次请求的目标点距离小于 0.3 米，第二次请求应该被忽略。

**Validates: Requirements 1.7**

### Property 5: 砍伐高亮单一性

*For any* 砍伐操作序列，场景中处于砍伐高亮状态的树木数量应该始终 <= 1。

**Validates: Requirements 2.2, 2.3**

### Property 6: 砍伐高亮透明度

*For any* 处于砍伐高亮状态的树木，其透明度应该等于 `occludedAlpha + 0.5`（不超过 1.0）。

**Validates: Requirements 2.1**

### Property 7: 边缘遮挡单树透明

*For any* 玩家位置在树林边界外部且被边界树遮挡的情况，只有遮挡的那棵树应该变透明，其他树保持不透明。

**Validates: Requirements 3.1, 3.2, 3.3**

### Property 8: 内部遮挡整林透明

*For any* 玩家位置在树林边界内部的情况，整片树林的所有树木都应该变透明。

**Validates: Requirements 3.4, 3.5**

---

## 八、错误处理

### 8.1 导航错误

| 错误情况 | 处理方式 |
|---------|---------|
| 起点在障碍物内 | 尝试智能脱困，失败则取消导航 |
| 终点不可达 | 寻找最近可达点 |
| 路径被阻断 | 重新规划路径 |
| 多次脱困失败 | 取消导航，通知玩家 |

### 8.2 遮挡错误

| 错误情况 | 处理方式 |
|---------|---------|
| 纹理不可读 | 回退到 Bounds 检测 |
| 树林边界计算失败 | 使用简单的 Bounds 合并 |
| 凸包计算失败 | 使用 AABB 边界 |

---

## 九、测试策略

### 9.1 单元测试

- 凸包算法正确性
- 点在凸包内判定
- 卡顿检测逻辑
- 透明度计算

### 9.2 属性测试

使用 NUnit + FsCheck 进行属性测试：

- 生成随机的障碍物配置和玩家位置
- 验证导航系统的各项属性
- 验证遮挡系统的各项属性

### 9.3 集成测试

- 完整的导航流程测试
- 完整的遮挡流程测试
- 砍伐高亮流程测试

---

## 十、相关文件

| 文件 | 修改内容 |
|------|---------|
| `Assets/Scripts/Service/Player/PlayerAutoNavigator.cs` | 导航优化 |
| `Assets/Scripts/Service/Navigation/NavGrid2D.cs` | 网格寻路优化 |
| `Assets/Scripts/Service/Rendering/OcclusionManager.cs` | 树林边缘检测、砍伐高亮管理 |
| `Assets/Scripts/Service/Rendering/OcclusionTransparency.cs` | 砍伐高亮状态 |
| `Assets/Scripts/Utils/ConvexHullCalculator.cs` | 凸包计算工具（新建） |
