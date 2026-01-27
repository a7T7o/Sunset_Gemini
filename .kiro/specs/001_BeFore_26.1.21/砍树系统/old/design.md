# 设计文档

## 概述

工具-资源交互系统是一个可扩展的框架，用于处理玩家工具与世界资源的交互。核心设计原则：
1. **零改动现有预制体** - 使用精灵边界检测，不依赖额外碰撞体
2. **帧索引触发** - 利用现有动画系统的帧同步机制
3. **工具-资源矩阵** - 不同工具对不同资源有不同效果
4. **可扩展架构** - 通过接口和配置支持新工具/资源类型

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                      命中发射器                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  帧索引     │  │  扇形计算   │  │   边界相交检测      │  │
│  │  检测       │→ │  (60°锥形) │→ │                     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      资源节点注册表                          │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  字典存储所有已注册的资源节点                        │    │
│  │  - 注册(node)                                       │    │
│  │  - 注销(node)                                       │    │
│  │  - 获取范围内节点(position, radius)                 │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      资源节点接口                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ 能否接受()  │  │  命中处理() │  │    获取边界()       │  │
│  │ (工具类型)  │  │ (应用伤害)  │  │ (精灵边界)          │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
         树木控制器      矿石控制器       农田格子
         (实现接口)      (未来)           (未来)
```

## 组件和接口

### IResourceNode 接口

```csharp
namespace FarmGame.Combat
{
    public interface IResourceNode
    {
        /// <summary>检查是否接受此工具类型（用于判断是否扣血）</summary>
        bool CanAccept(ToolHitContext ctx);
        
        /// <summary>处理命中（抖动、扣血、特效等）</summary>
        void OnHit(ToolHitContext ctx);
        
        /// <summary>获取检测边界（精灵边界）</summary>
        Bounds GetBounds();
        
        /// <summary>获取资源位置（树根位置）</summary>
        Vector3 GetPosition();
        
        /// <summary>资源是否已耗尽</summary>
        bool IsDepleted { get; }
        
        /// <summary>资源类型标识</summary>
        string ResourceTag { get; }
    }
}
```

### ToolHitContext 命中上下文结构体

```csharp
public struct ToolHitContext
{
    public int toolItemId;        // 工具物品ID
    public int toolQuality;       // 工具品质
    public ToolType toolType;     // 工具类型枚举
    public int materialTier;      // 材料等级（0=木, 1=石, 2=生铁, 3=黄铜, 4=钢, 5=金）
    public int actionState;       // 动画状态（挥砍=6, 挖掘=8）
    public Vector2 hitPoint;      // 命中点
    public Vector2 hitDir;        // 命中方向
    public GameObject attacker;   // 攻击者
    public float baseDamage;      // 基础伤害
    public int frameIndex;        // 命中帧索引
}
```

### ResourceNodeRegistry 资源注册表单例

```csharp
public class ResourceNodeRegistry : MonoBehaviour
{
    public static ResourceNodeRegistry Instance { get; private set; }
    
    private Dictionary<int, IResourceNode> _nodes = new();
    
    public void Register(IResourceNode node);      // 注册节点
    public void Unregister(IResourceNode node);    // 注销节点
    public List<IResourceNode> GetNodesInRange(Vector2 center, float radius);  // 获取范围内节点
}
```

### PlayerToolHitEmitter 命中发射器组件

```csharp
public class PlayerToolHitEmitter : MonoBehaviour
{
    [Header("命中检测设置")]
    public float wedgeAngleDeg = 60f;      // 扇形角度
    public float defaultReach = 1.5f;       // 默认命中半径
    public int hitWindowStart = 3;          // 命中窗口起始帧
    public int hitWindowEnd = 4;            // 命中窗口结束帧
    
    [Header("工具-资源配置")]
    public ToolResourceConfig[] interactions;  // 交互配置
    
    private void TryHit();                  // 执行命中检测
    private bool CheckBoundsIntersection(...);  // 检查边界相交
}
```

## 数据模型

### 工具-资源交互配置

```csharp
[System.Serializable]
public class ToolResourceConfig
{
    public ToolType toolType;           // 工具类型
    public string resourceTag;          // 资源标签（Tree, Rock）
    public bool dealsDamage;            // 是否造成伤害
    public bool consumesStamina;        // 是否消耗精力
    public bool playsShake;             // 是否播放抖动
    public bool spawnsParticles;        // 是否生成粒子
}
```

### 默认交互矩阵

| 工具类型 | 资源标签 | 造成伤害 | 消耗精力 | 播放抖动 | 生成粒子 |
|----------|----------|----------|----------|----------|----------|
| 斧头 | 树木 | ✓ | ✓ | ✓ | ✓ (树叶) |
| 斧头 | 矿石 | ✗ | ✗ | ✓ | ✗ |
| 镐子 | 树木 | ✗ | ✗ | ✓ | ✗ |
| 镐子 | 矿石 | ✓ | ✓ | ✓ | ✓ (碎石) |
| 镰刀 | 树木 | ✗ | ✗ | ✓ | ✗ |
| 锄头 | 树木 | ✗ | ✗ | ✗ | ✗ |

## 正确性属性

*属性是指在系统所有有效执行中都应保持为真的特征或行为——本质上是关于系统应该做什么的形式化陈述。属性是人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1: 命中窗口一致性
*对于任何*工具动画，命中检测只应在帧索引处于 [命中窗口起始帧, 命中窗口结束帧] 范围内时发生
**验证: 需求 1.1, 1.4**

### 属性 2: 精灵边界检测准确性
*对于任何*资源节点，命中检测应使用 Tree 子物体的 SpriteRenderer.bounds，而非父物体的碰撞体
**验证: 需求 1.2**

### 属性 3: 工具-资源匹配规则
*对于任何*工具-资源交互，只有当工具类型与资源所需工具匹配时才应造成伤害
**验证: 需求 3.1, 3.2, 3.3, 3.4**

### 属性 4: 精力消耗条件
*对于任何*工具动作，只有当工具类型与资源匹配且命中成功时才应消耗精力
**验证: 需求 3.5, 7.1, 7.2, 7.3**

### 属性 5: 血量归零触发状态转换
*对于任何*血量归零的树木，树木应转换为树桩状态并生成掉落物
**验证: 需求 4.4, 4.5**

### 属性 6: 树叶粒子生成位置
*对于任何*生成的树叶粒子，初始位置应在树木精灵边界的顶部30%区域内
**验证: 需求 6.1**

### 属性 7: 树叶粒子消失条件
*对于任何*树叶粒子，当其Y坐标到达或低于树根Y坐标时应淡出消失
**验证: 需求 6.3**

### 属性 8: Collider Bounds 检测一致性
*对于任何*命中检测，系统应使用树木 Collider 的 bounds 进行扇形相交检测，而非 Sprite 的 bounds；无 Collider 时回退到 Sprite bounds
**验证: 需求 13.1, 13.2, 13.3, 13.4**

### 属性 9: 玩家位置与朝向验证
*对于任何*命中检测，只有当玩家位置与朝向符合逻辑时才判定命中：朝下时玩家在树上方，朝上时玩家在树下方，朝左/右时玩家在树的对应侧
**验证: 需求 14.1, 14.2, 14.3, 14.4, 14.5**

### 属性 10: 单目标命中选择
*对于任何*命中范围内有多棵树木的情况，系统应只命中通过所有验证且 Collider 中心距离玩家 Collider 中心最近的一棵
**验证: 需求 15.1, 15.2, 15.4**

## 错误处理

1. **资源注册表未初始化**：命中发射器在启动时检查，若为空则禁用自身并输出警告
2. **SpriteRenderer 为空**：TreeController.GetBounds() 返回零大小边界，跳过检测
3. **精力不足**：显示 UI 提示，阻止工具动作播放
4. **掉落表为空**：砍倒时跳过掉落物生成，输出警告

## 树木倒下动画设计（待审核）

### 设计原则

1. **基于玩家朝向和挥砍方向判定**：不是简单的"与砍伐方向相反"
2. **以 Collider 边缘为轴心**：像真实的树倒下
3. **先慢后快**：模拟重力加速（t² 曲线）
4. **树桩在原位置**：动画结束后恢复原位置再转换

### 用户示例分析

> "当人物朝下，树在人下方，斧头从人物左上方逆时针劈砍，那么肯定是树木的左侧受击，然后树木从左往右倒下，以其collider的右部中心点为轴旋转90度，先慢后快地倒下"

**分解**：
- 玩家朝向：Down（朝下）
- 挥砍方向：从左上方逆时针劈砍
- 受击侧：树木左侧
- 倒下方向：从左往右（向右倒）
- 轴心点：Collider 右部中心点
- 旋转角度：90°
- 加速曲线：先慢后快

### 倒下方向判定表（用户修正版）

| 玩家朝向 | FlipX | 挥砍方向描述 | 倒下方向 | 轴心点位置 | 旋转角度 |
|---------|-------|-------------|---------|-----------|---------|
| Down (0) | false (0) | 左上贴近竖直位置逆时针挥砍半圆 | 向右倒 | Collider 中心 | -90° |
| Down (0) | true (1) | 右上角顺时针挥砍 | 向左倒 | Collider 中心 | +90° |
| Side (1) | false (0，朝右) | 从左侧水平位置逆时针挥砍半圆 | 向上倒 | Collider 中心 | +90° |
| Side (1) | true (1，朝左) | 从右侧顺时针半圆 | 向上倒 | Collider 中心 | +90° |
| Up (2) | false (0) | 从右下贴近竖直方向逆时针半圆 | 向左倒 | Collider 中心 | +90° |
| Up (2) | true (1) | 左下顺时针半圆 | 向右倒 | Collider 中心 | -90° |

**重要说明**：
- FlipX=false 记为 0，FlipX=true 记为 1
- 轴心点统一使用 Collider 中心
- 树桩在被砍到的那一刻就立着，树木 Sprite 单独有倒下动画
- 倒下的树木不会推动玩家或其他物体

### 轴心点计算

**统一使用 Collider 中心作为轴心点**：

```csharp
// 轴心点统一为 Collider 中心
Vector3 pivotPoint = treeCollider.bounds.center;
```

### 倒下方向枚举

```csharp
public enum FallDirection
{
    Right,    // 向右倒（Down0, Up1）
    Left,     // 向左倒（Down1, Up0）
    Up        // 向上倒（Side0, Side1）
}
```

### 树桩与倒下动画分离

**重要设计**：
- 树桩在被砍到的那一刻就立着（原位置）
- 树木 Sprite 单独播放倒下动画
- 倒下的树木不会推动玩家或其他物体
- 动画只是视觉效果，不影响物理

### 核心代码结构

```csharp
// 1. 判定倒下方向（用户修正版）
private FallDirection DetermineFallDirection(int playerDirection, bool playerFlipX)
{
    switch (playerDirection)
    {
        case 0: // Down
            // Down0: 左上逆时针 → 向右倒
            // Down1: 右上顺时针 → 向左倒
            return playerFlipX ? FallDirection.Left : FallDirection.Right;
        case 1: // Side
            // Side0: 左侧逆时针 → 向上倒
            // Side1: 右侧顺时针 → 向上倒
            return FallDirection.Up;
        case 2: // Up
            // Up0: 右下逆时针 → 向左倒
            // Up1: 左下顺时针 → 向右倒
            return playerFlipX ? FallDirection.Right : FallDirection.Left;
        default:
            return FallDirection.Right;
    }
}

// 2. 计算轴心点（统一使用 Collider 中心）
private Vector3 CalculatePivotPoint(Bounds colBounds)
{
    return colBounds.center;
}

// 3. 计算旋转角度
private float CalculateTargetAngle(FallDirection fallDir)
{
    return fallDir switch
    {
        FallDirection.Right => -90f,   // 顺时针向右倒
        FallDirection.Left => 90f,     // 逆时针向左倒
        FallDirection.Up => 90f,       // 逆时针向上倒
        _ => 0f
    };
}

// 4. 倒下动画协程（树桩与动画分离版）
private IEnumerator FallAnimationCoroutine(FallDirection fallDir, Vector3 pivotWorld)
{
    float elapsed = 0f;
    float duration = 1.5f; // 动画慢一点
    
    // 立即转换为树桩状态（树桩在原位置）
    ConvertToStump();
    
    // 创建临时的倒下 Sprite（不影响物理）
    GameObject fallingTree = CreateFallingTreeSprite();
    SpriteRenderer fallingSR = fallingTree.GetComponent<SpriteRenderer>();
    
    Vector3 originalPos = fallingTree.transform.position;
    Color originalColor = fallingSR.color;
    
    // 目标旋转角度
    float targetAngle = CalculateTargetAngle(fallDir);
    
    while (elapsed < duration)
    {
        float t = elapsed / duration;
        float easedT = t * t; // 先慢后快（重力加速）
        
        // 当前旋转角度
        float currentAngle = targetAngle * easedT;
        
        // 绕轴心旋转（只是视觉效果）
        fallingTree.transform.rotation = Quaternion.Euler(0, 0, currentAngle);
        
        // 淡出（最后 30%）
        if (t > 0.7f)
        {
            float fadeT = (t - 0.7f) / 0.3f;
            fallingSR.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f - fadeT);
        }
        
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    // 销毁临时的倒下 Sprite
    Destroy(fallingTree);
}

// 创建临时的倒下树木 Sprite（纯视觉，无碰撞）
private GameObject CreateFallingTreeSprite()
{
    GameObject fallingTree = new GameObject("FallingTree");
    fallingTree.transform.position = spriteRenderer.transform.position;
    
    SpriteRenderer sr = fallingTree.AddComponent<SpriteRenderer>();
    sr.sprite = spriteRenderer.sprite;
    sr.sortingLayerID = spriteRenderer.sortingLayerID;
    sr.sortingOrder = spriteRenderer.sortingOrder - 1; // 在树桩后面
    
    return fallingTree;
}
```

### 调试输出

在实现前，先添加调试输出验证判定逻辑：

```csharp
Debug.Log($"[TreeController] 倒下判定:");
Debug.Log($"  玩家朝向: {playerDirection} ({GetDirectionName(playerDirection)})");
Debug.Log($"  玩家翻转: {playerFlipX} ({(playerFlipX ? 1 : 0)})");
Debug.Log($"  倒下方向: {fallDirection}");
Debug.Log($"  轴心点: {pivotPoint}");
Debug.Log($"  旋转角度: {targetAngle}°");

private string GetDirectionName(int dir) => dir switch
{
    0 => "Down",
    1 => "Side",
    2 => "Up",
    _ => "Unknown"
};
```

### 动画参数

| 参数 | 值 | 说明 |
|------|-----|------|
| 旋转角度 | 90° | 从竖直到水平 |
| 动画时长 | 1.5 秒 | 慢一点，更自然 |
| 加速曲线 | t² | 先慢后快，模拟重力 |
| 淡出开始 | 70% | 最后 30% 时间淡出 |

### 重要约束

1. **树桩立即生成**：被砍到的那一刻树桩就立着
2. **动画纯视觉**：倒下的树木 Sprite 不影响物理
3. **不推动物体**：倒下的树木不会推动玩家或其他物体

---

## 命中检测设计

### 核心规则

**只有玩家朝向前方指定角度指定距离内的树木才会被砍到，途径的不会被砍到**

### 命中检测参数

| 参数 | 值 | 说明 |
|------|-----|------|
| wedgeAngleDeg | 60° | 扇形角度 |
| defaultReach | 1.5 | 命中距离（米） |
| hitWindowStart | 3 | 命中窗口起始帧 |
| hitWindowEnd | 4 | 命中窗口结束帧 |
| positionTolerance | 0.1 | 位置验证容差值 |

### 精确命中检测流程（Bug修复版）

```
┌─────────────────────────────────────────────────────────────┐
│                    精确命中检测流程                          │
├─────────────────────────────────────────────────────────────┤
│  1. 获取范围内所有资源节点                                   │
│                    ↓                                        │
│  2. 对每个节点执行三重验证：                                 │
│     ┌─────────────────────────────────────────────────┐     │
│     │ A. Collider Bounds 扇形检测                      │     │
│     │    - 使用 Collider.bounds 而非 Sprite.bounds    │     │
│     │    - 计算 Collider 最近点到玩家的距离和角度      │     │
│     │    - 无 Collider 时回退到 Sprite.bounds         │     │
│     └─────────────────────────────────────────────────┘     │
│                    ↓                                        │
│     ┌─────────────────────────────────────────────────┐     │
│     │ B. 玩家朝向与相对位置验证                        │     │
│     │    - 朝下: playerY > collider.max.y - tolerance │     │
│     │    - 朝上: playerY < collider.min.y + tolerance │     │
│     │    - 朝右: playerX < collider.min.x + tolerance │     │
│     │    - 朝左: playerX > collider.max.x - tolerance │     │
│     └─────────────────────────────────────────────────┘     │
│                    ↓                                        │
│  3. 从通过验证的节点中选择最近的一个                         │
│     - 使用 Collider 中心到 Collider 中心的距离              │
│                    ↓                                        │
│  4. 只命中选中的单个目标                                    │
└─────────────────────────────────────────────────────────────┘
```

### IResourceNode 接口扩展

```csharp
namespace FarmGame.Combat
{
    public interface IResourceNode
    {
        // ... 现有方法 ...
        
        /// <summary>获取碰撞体边界（用于精确命中检测）</summary>
        /// <returns>Collider bounds，无 Collider 时返回 Sprite bounds</returns>
        Bounds GetColliderBounds();
    }
}
```

### 精确命中检测逻辑

```csharp
private void TryHit()
{
    Vector2 playerCenter = GetPlayerCenter();
    Vector2 forward = GetFacingDirection();
    int direction = GetPlayerDirection();
    bool flipX = GetPlayerFlipX();
    
    var nodes = ResourceNodeRegistry.Instance.GetNodesInRange(playerCenter, defaultReach);
    
    // 筛选通过所有验证的候选节点
    List<(IResourceNode node, float distance)> validCandidates = new();
    
    foreach (var node in nodes)
    {
        if (node == null || node.IsDepleted) continue;
        
        // ✅ 使用 Collider bounds 而非 Sprite bounds
        Bounds colliderBounds = node.GetColliderBounds();
        
        // A. 扇形检测（使用 Collider bounds）
        if (!CheckWedgeBoundsIntersection(playerCenter, forward, defaultReach, wedgeAngleDeg, colliderBounds))
            continue;
        
        // B. 位置验证
        if (!ValidatePositionRelation(playerCenter, direction, flipX, colliderBounds))
            continue;
        
        // 计算 Collider 中心距离
        float distance = Vector2.Distance(playerCenter, colliderBounds.center);
        validCandidates.Add((node, distance));
    }
    
    // ✅ 只命中最近的一个
    if (validCandidates.Count > 0)
    {
        validCandidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        var closest = validCandidates[0].node;
        
        var ctx = BuildContext();
        ctx.hitPoint = closest.GetColliderBounds().ClosestPoint(playerCenter);
        ctx.hitDir = ((Vector2)closest.GetColliderBounds().center - playerCenter).normalized;
        
        closest.OnHit(ctx);
    }
}

/// <summary>
/// 验证玩家位置与朝向是否符合逻辑
/// </summary>
private bool ValidatePositionRelation(Vector2 playerCenter, int direction, bool flipX, Bounds colliderBounds)
{
    const float tolerance = 0.1f;
    
    switch (direction)
    {
        case 0: // Down - 玩家必须在树上方
            return playerCenter.y > colliderBounds.max.y - tolerance;
            
        case 1: // Up - 玩家必须在树下方
            return playerCenter.y < colliderBounds.min.y + tolerance;
            
        case 2: // Side
            if (flipX) // 朝左 - 玩家必须在树右侧
                return playerCenter.x > colliderBounds.max.x - tolerance;
            else // 朝右 - 玩家必须在树左侧
                return playerCenter.x < colliderBounds.min.x + tolerance;
            
        default:
            return false;
    }
}

/// <summary>
/// 检查扇形与 Collider 边界是否相交
/// </summary>
private bool CheckWedgeBoundsIntersection(Vector2 origin, Vector2 forward, float reach, float angleDeg, Bounds bounds)
{
    // 使用 Collider bounds 的最近点
    Vector2 closestPoint = bounds.ClosestPoint(origin);
    
    // 检查距离
    float distSqr = (closestPoint - origin).sqrMagnitude;
    if (distSqr > reach * reach) return false;
    
    // 检查角度
    Vector2 toClosest = (closestPoint - origin).normalized;
    float angle = Vector2.Angle(forward, toClosest);
    if (angle > angleDeg * 0.5f) return false;
    
    return true;
}
```

### 位置验证规则图示

```
朝下 (Direction=0)              朝上 (Direction=1)
    玩家 ●                           ┌───┐
         ↓                           │树木│
    ┌───┐                            └───┘
    │树木│                               ↑
    └───┘                            玩家 ●
    
朝右 (Direction=2, FlipX=false)  朝左 (Direction=2, FlipX=true)
    玩家 ● →  ┌───┐                  ┌───┐  ← ● 玩家
              │树木│                  │树木│
              └───┘                  └───┘
```

---

## 调试绘制设计

### 需求

1. **扇形**：以玩家 Collider 中心为起点，朝向玩家面对的方向
2. **红色点**：显示在树木上的实际命中位置（Bounds.ClosestPoint 返回的点）
3. **箭头**：四向（上下左右），指向树木倒下的方向

### 绘制内容

```csharp
private void OnDrawGizmosSelected()
{
    // 1. 获取玩家 Collider 中心（最高优先级规则！）
    Vector2 origin = playerCollider.bounds.center;
    
    // 2. 获取玩家朝向
    Vector2 facingDir = GetFacingDirection();
    
    // 3. 绘制扇形
    Gizmos.color = Color.green;
    // 绘制扇形边界线
    float halfAngle = wedgeAngleDeg * 0.5f * Mathf.Deg2Rad;
    Vector2 leftEdge = RotateVector(facingDir, halfAngle) * defaultReach;
    Vector2 rightEdge = RotateVector(facingDir, -halfAngle) * defaultReach;
    Gizmos.DrawLine(origin, origin + leftEdge);
    Gizmos.DrawLine(origin, origin + rightEdge);
    // 绘制弧线
    DrawArc(origin, facingDir, wedgeAngleDeg, defaultReach);
    
    // 4. 绘制玩家朝向箭头（蓝色粗箭头）
    Gizmos.color = Color.blue;
    DrawArrow(origin, origin + facingDir * 0.5f, 0.1f);
    
    // 5. 绘制命中点（红色球）
    foreach (var hitPoint in _lastHitPoints)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(hitPoint, 0.1f);
    }
    
    // 6. 绘制倒下方向箭头（黄色，四向）
    for (int i = 0; i < _lastHitPoints.Count; i++)
    {
        Gizmos.color = Color.yellow;
        Vector2 fallDir = GetSimplifiedFallDirection(_lastFallDirections[i]);
        DrawArrow(_lastHitPoints[i], _lastHitPoints[i] + fallDir * 0.5f, 0.08f);
    }
}

// 简化为四向（用户修正版）
private Vector2 GetSimplifiedFallDirection(FallDirection fallDir)
{
    return fallDir switch
    {
        FallDirection.Right => Vector2.right,  // 向右倒
        FallDirection.Left => Vector2.left,    // 向左倒
        FallDirection.Up => Vector2.up,        // 向上倒
        _ => Vector2.zero
    };
}
```

---

## 测试策略

### 单元测试
- 命中上下文构建正确性
- 边界相交算法准确性
- 工具-资源匹配逻辑
- 倒下方向判定逻辑

### 集成测试
- 完整砍树流程（命中→扣血→抖动→树叶→砍倒→掉落）
- 非匹配工具交互（只抖动不扣血）
- 精力消耗验证
- 倒下动画各方向测试

### 手动测试
- 不同方向砍树的命中范围
- 树叶飘落视觉效果
- 音效播放时机
- 倒下动画方向和效果验证

