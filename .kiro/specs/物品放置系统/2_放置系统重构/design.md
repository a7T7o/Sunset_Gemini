# 设计文档 - 物品放置系统重构

## 概述

重构物品放置系统，实现完整的预放置 UI、Layer 同步、动态排序、碰撞检测等功能。参考星露谷等游戏的放置机制，提供直观的"蓝图式"放置预览。

## 架构设计

### 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    PlacementManager                          │
│  - 放置模式控制                                              │
│  - 放置执行                                                  │
│  - 事件广播                                                  │
└─────────────────┬───────────────────────────────────────────┘
                  │
    ┌─────────────┼─────────────┬─────────────────┐
    ▼             ▼             ▼                 ▼
┌─────────┐ ┌──────────┐ ┌───────────┐ ┌──────────────┐
│GridCalc │ │Validator │ │PreviewUI  │ │LayerDetector │
│方块计算 │ │放置验证  │ │预览UI系统 │ │Layer检测     │
└─────────┘ └──────────┘ └───────────┘ └──────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
        ┌──────────┐   ┌──────────┐   ┌──────────┐
        │GridCell  │   │ItemPreview│  │Collision │
        │格子渲染  │   │物品预览   │  │碰撞检测  │
        └──────────┘   └──────────┘   └──────────┘
```

### 核心组件

| 组件 | 职责 |
|------|------|
| PlacementManager | 放置系统核心，管理放置模式和执行 |
| PlacementGridCalculator | 计算方块中心、格子数量 |
| PlacementValidator | 验证放置有效性（碰撞、Layer、范围） |
| PlacementPreviewUI | 预放置 UI 系统（方框 + 物品预览） |
| PlacementLayerDetector | 检测点击位置和玩家的 Layer |
| PlacementGridCell | 单个格子的渲染（绿色/红色） |

## 组件与接口

### PlacementGridCalculator

```csharp
public static class PlacementGridCalculator
{
    /// <summary>
    /// 计算方块中心坐标
    /// </summary>
    public static Vector3 GetCellCenter(Vector3 worldPosition)
    {
        return new Vector3(
            Mathf.Floor(worldPosition.x) + 0.5f,
            Mathf.Floor(worldPosition.y) + 0.5f,
            worldPosition.z
        );
    }
    
    /// <summary>
    /// 计算物品占用的格子数量（基于 Collider）
    /// </summary>
    public static Vector2Int GetRequiredGridSize(Bounds colliderBounds)
    {
        int width = Mathf.CeilToInt(colliderBounds.size.x);
        int height = Mathf.CeilToInt(colliderBounds.size.y);
        return new Vector2Int(Mathf.Max(1, width), Mathf.Max(1, height));
    }
    
    /// <summary>
    /// 获取所有占用的格子位置
    /// </summary>
    public static List<Vector2Int> GetOccupiedCells(Vector3 center, Vector2Int gridSize);
}
```

### PlacementLayerDetector

```csharp
public class PlacementLayerDetector
{
    /// <summary>
    /// 获取点击位置的 Layer（通过 Raycast 检测地面）
    /// </summary>
    public static int GetLayerAtPosition(Vector3 worldPosition);
    
    /// <summary>
    /// 获取玩家当前所在的 Layer
    /// </summary>
    public static int GetPlayerLayer(Transform playerTransform);
    
    /// <summary>
    /// 检查两个 Layer 是否一致
    /// </summary>
    public static bool IsLayerMatch(int layerA, int layerB);
}
```

### PlacementPreviewUI

```csharp
public class PlacementPreviewUI : MonoBehaviour
{
    // 格子预制体
    [SerializeField] private GameObject gridCellPrefab;
    
    // 颜色配置
    [SerializeField] private Color validColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1, 0, 0, 0.5f);
    [SerializeField] private Color previewAlpha = 0.6f;
    
    /// <summary>
    /// 显示预览（物品 + 格子）
    /// </summary>
    public void Show(ItemData item, Vector3 centerPosition);
    
    /// <summary>
    /// 更新预览位置和状态
    /// </summary>
    public void UpdatePreview(Vector3 centerPosition, List<CellState> cellStates);
    
    /// <summary>
    /// 隐藏预览
    /// </summary>
    public void Hide();
}

public struct CellState
{
    public Vector2Int gridPosition;
    public bool isValid;  // true=绿色, false=红色
}
```

### PlacementValidator 重构

```csharp
public class PlacementValidator
{
    /// <summary>
    /// 完整验证放置位置
    /// </summary>
    public PlacementValidationResult ValidateFull(
        ItemData item, 
        Vector3 centerPosition, 
        Transform playerTransform,
        out List<CellState> cellStates);
    
    /// <summary>
    /// 检测每个格子的碰撞状态
    /// </summary>
    public List<CellState> DetectCellCollisions(
        Vector3 centerPosition, 
        Vector2Int gridSize,
        string[] obstacleTags);
    
    /// <summary>
    /// 检查 Layer 一致性
    /// </summary>
    public bool ValidateLayerConsistency(
        Vector3 position, 
        Transform playerTransform);
}
```

## 数据模型

### PlacementValidationResult 扩展

```csharp
public class PlacementValidationResult
{
    public bool IsValid;
    public PlacementInvalidReason Reason;
    public string Message;
    public List<CellState> CellStates;  // 新增：每个格子的状态
    public int DetectedLayer;            // 新增：检测到的 Layer
}
```

### PlacementInvalidReason 扩展

```csharp
public enum PlacementInvalidReason
{
    None,
    OutOfRange,
    LayerMismatch,      // 新增：Layer 不一致
    CollisionDetected,  // 新增：碰撞检测到障碍物
    // ... 其他原因
}
```

## 正确性属性

*正确性属性是系统应该满足的通用规则，用于验证实现的正确性。*

### Property 1: 方块中心计算一致性
*对于任意* 世界坐标 (x, y)，计算出的方块中心应该满足：
- center.x = Floor(x) + 0.5
- center.y = Floor(y) + 0.5
**验证: 需求 3.1, 3.2**

### Property 2: Layer 同步完整性
*对于任意* 放置的树苗物体，其所有子物体（Tree、Shadow）的 Layer 应该与父物体一致
**验证: 需求 1.1, 1.2, 1.3, 1.4**

### Property 3: Order 计算公式正确性
*对于任意* 放置位置 Y 坐标，计算出的 Order 应该满足：
- Tree.Order = -Round(Y × 100) + offset
- Shadow.Order = Tree.Order - 1
**验证: 需求 2.1, 2.2, 2.3, 2.4**

### Property 4: 碰撞检测阻止放置
*对于任意* 存在 Collider 重叠的放置位置，放置操作应该被阻止
**验证: 需求 4.1, 4.2, 8.4, 8.5**

### Property 5: Layer 一致性阻止放置
*对于任意* 玩家 Layer 与放置位置 Layer 不一致的情况，放置操作应该被阻止
**验证: 需求 5.1, 5.2**

### Property 6: 格子大小计算正确性
*对于任意* 物品 Collider，计算出的格子数量应该不小于 Collider 的实际大小（向上取整）
**验证: 需求 7.2, 7.3**

### Property 7: 预览颜色状态一致性
*对于任意* 预览格子，如果该格子与障碍物 Collider 重叠则显示红色，否则显示绿色
**验证: 需求 7.4, 7.5, 8.2**

## 错误处理

| 错误场景 | 处理方式 |
|---------|---------|
| 无法检测 Layer | 使用默认 Layer (Default) |
| 物品无 Collider | 使用 1x1 格子 |
| 玩家 Transform 为空 | 跳过 Layer 检测 |
| 导航失败 | 取消放置操作 |

## 测试策略

### 单元测试
- PlacementGridCalculator 的方块中心计算
- Order 计算公式验证
- Layer 检测逻辑

### 属性测试
- Property 1: 方块中心计算（随机坐标输入）
- Property 3: Order 计算（随机 Y 坐标输入）
- Property 6: 格子大小计算（随机 Collider 大小输入）

### 集成测试
- 完整放置流程（进入模式 → 预览 → 放置）
- Layer 同步验证
- 碰撞检测验证

