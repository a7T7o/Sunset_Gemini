# 放置系统 V3 重构 - 设计文档

## 概述

放置系统 V3 版本的核心变化是取消"放置范围"概念，改为"点击锁定 + 走过去放置"的交互模式。这更符合星露谷农场等游戏的放置体验。

## 架构

### 系统状态机

```
┌─────────────────────────────────────────────────────────────────────┐
│                        放置系统状态机                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────┐    手持可放置物品    ┌──────────────┐                 │
│  │   Idle   │ ──────────────────→ │   Preview    │                 │
│  │  (空闲)  │ ←────────────────── │  (预览跟随)  │                 │
│  └──────────┘    ESC/右键/切换物品  └──────────────┘                 │
│                                           │                         │
│                                           │ 点击左键（全绿）         │
│                                           ↓                         │
│                                    ┌──────────────┐                 │
│                                    │   Locked     │                 │
│                                    │  (位置锁定)  │                 │
│                                    └──────────────┘                 │
│                                           │                         │
│                                           │ 自动开始导航             │
│                                           ↓                         │
│                                    ┌──────────────┐                 │
│                                    │  Navigating  │                 │
│                                    │  (导航中)    │                 │
│                                    └──────────────┘                 │
│                                           │                         │
│                          ┌────────────────┼────────────────┐        │
│                          │                │                │        │
│                          ↓                ↓                ↓        │
│                    ┌──────────┐    ┌──────────┐    ┌──────────┐    │
│                    │ 到达触发  │    │ ESC取消  │    │ 点击新位置│    │
│                    │ → 放置   │    │ → Preview│    │ → Locked │    │
│                    └──────────┘    └──────────┘    └──────────┘    │
│                          │                                          │
│                          ↓                                          │
│                    ┌──────────┐                                     │
│                    │ Preview  │ ← 如果还有物品，继续放置模式         │
│                    │ 或 Idle  │ ← 如果物品用完，退出放置模式         │
│                    └──────────┘                                     │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 组件职责

| 组件 | 职责 |
|------|------|
| PlacementManagerV3 | 状态机管理、协调各组件 |
| PlacementPreviewV3 | 预览 UI 显示、位置锁定 |
| PlacementValidatorV3 | 格子颜色判定（只有两种红色情况） |
| PlacementNavigator | 计算导航目标点、到达检测 |
| PlacementLayerDetector | Layer 检测和同步（复用 V2） |
| PlacementGridCalculator | 格子计算（复用 V2） |

## 组件和接口

### PlacementManagerV3

```csharp
public class PlacementManagerV3 : MonoBehaviour
{
    // 状态
    public enum PlacementState
    {
        Idle,       // 空闲
        Preview,    // 预览跟随鼠标
        Locked,     // 位置锁定
        Navigating  // 导航中
    }
    
    // 公共方法
    void EnterPlacementMode(ItemData item);
    void ExitPlacementMode();
    void OnLeftClick();      // 处理左键点击
    void OnRightClick();     // 处理右键/ESC
    
    // 内部方法
    void LockPreviewPosition();
    void StartNavigation();
    void OnReachedTarget();
    void ExecutePlacement();
}
```

### PlacementPreviewV3

```csharp
public class PlacementPreviewV3 : MonoBehaviour
{
    // 状态
    bool isLocked;           // 是否锁定位置
    Vector3 lockedPosition;  // 锁定的位置
    
    // 公共方法
    void Show(ItemData item);
    void Hide();
    void UpdatePosition(Vector3 mouseWorldPos);  // 跟随鼠标
    void LockPosition();                          // 锁定当前位置
    void UnlockPosition();                        // 解锁，恢复跟随
    void UpdateCellStates(List<CellState> states);
    
    // 属性
    bool IsAllValid { get; }
    Vector3 LockedPosition { get; }
    Bounds GetPreviewBounds();  // 获取预览格子的边界
}
```

### PlacementNavigator

```csharp
public class PlacementNavigator : MonoBehaviour
{
    // 配置
    float triggerDistance = 0.1f;  // 触发放置的距离阈值
    
    // 公共方法
    Vector3 CalculateNavigationTarget(Bounds playerBounds, Bounds previewBounds);
    void StartNavigation(Vector3 target);
    void CancelNavigation();
    bool CheckReached(Bounds playerBounds, Bounds previewBounds);
    
    // 事件
    event Action OnReachedTarget;
    event Action OnNavigationCancelled;
}
```

### PlacementValidatorV3

```csharp
public class PlacementValidatorV3
{
    // 障碍物标签（包含 Player）
    string[] obstacleTags = { "Tree", "Rock", "Building", "Player" };
    
    // 主验证方法
    List<CellState> ValidateCells(Vector3 centerPosition, Vector2Int gridSize, Transform player);
    
    // 单格子验证
    bool IsCellValid(Vector3 cellCenter, Transform player);
    
    // 红色判定（只有两种情况）
    bool IsLayerMismatch(Vector3 position, Transform player);
    bool HasObstacle(Vector3 position);
}
```

## 数据模型

### PlacementState 枚举

```csharp
public enum PlacementState
{
    Idle,       // 空闲，未手持可放置物品
    Preview,    // 预览模式，预览 UI 跟随鼠标
    Locked,     // 位置锁定，等待导航开始
    Navigating  // 导航中，等待到达目标
}
```

### CellState 结构（复用）

```csharp
public struct CellState
{
    public Vector2Int gridPosition;
    public bool isValid;
    public InvalidReason reason;  // 新增：无效原因
}

public enum InvalidReason
{
    None,           // 有效
    LayerMismatch,  // Layer 不一致
    HasObstacle     // 有障碍物
}
```

## 正确性属性

*属性是在所有有效执行中都应该成立的特征，用于验证系统正确性。*

### 属性 1：红色格子只有两种情况

*对于任意*预览格子，如果显示红色，则必定满足以下条件之一：
1. 该格子位置的 Layer 与玩家脚下地面的 Layer 不一致
2. 该格子位置有障碍物（Tree、Rock、Building、Player 标签或水域）

**验证：需求 2.1, 2.2, 2.3, 2.4**

### 属性 2：距离不影响格子颜色

*对于任意*预览格子，无论玩家与该格子的距离是多少，格子颜色只由 Layer 一致性和障碍物决定。

**验证：需求 2.4**

### 属性 3：点击全绿才锁定

*对于任意*左键点击事件，当且仅当所有预览格子都是绿色时，系统才会锁定预览位置。

**验证：需求 3.1, 3.4**

### 属性 4：到达触发放置

*对于任意*导航过程，当玩家 Collider 边界与预览格子边界的距离小于触发距离且不重叠时，系统触发放置。

**验证：需求 4.1**

### 属性 5：放置位置一致性

*对于任意*放置操作，生成的物品位置必须与锁定时的预览位置完全一致。

**验证：需求 4.2**

## 错误处理

| 错误情况 | 处理方式 |
|---------|---------|
| 导航路径不可达 | 取消导航，解锁预览，显示提示 |
| 导航超时（10秒） | 取消导航，解锁预览 |
| 物品数量不足 | 退出放置模式 |
| 冬季放置树苗 | 阻止放置，显示控制台提示 |

## 测试策略

### 单元测试

1. **ValidatorV3 测试**
   - 测试 Layer 不一致时返回红色
   - 测试有障碍物时返回红色
   - 测试无障碍物且 Layer 一致时返回绿色
   - 测试距离不影响颜色

2. **Navigator 测试**
   - 测试导航目标点计算
   - 测试到达检测逻辑

### 属性测试

1. **属性 1 测试**：生成随机格子位置，验证红色格子必定满足两种条件之一
2. **属性 2 测试**：生成随机距离，验证颜色不变
3. **属性 3 测试**：生成随机格子状态，验证点击行为

