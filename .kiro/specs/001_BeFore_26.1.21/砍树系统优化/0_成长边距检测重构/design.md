# 设计文档 - 树木成长边距检测重构

## 概述

重构树木成长空间检测系统，从基于 Sprite Bounds 的复杂检测改为基于 Collider 边界的简单边距检测。核心思路是：从树木 Collider 中心向上下左右四个方向检测，在指定边距内查找是否存在带有指定标签的 Collider，如果存在则阻止成长。

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                    TreeControllerV2                          │
├─────────────────────────────────────────────────────────────┤
│  序列化字段:                                                  │
│  - stageConfigs[] (包含边距参数)                              │
│  - growthObstacleTags[] (障碍物标签)                          │
│  - enableGrowthSpaceCheck (启用开关)                          │
├─────────────────────────────────────────────────────────────┤
│  成长检测方法:                                                │
│  - CanGrowToNextStage() → 调用边距检测                        │
│  - CheckGrowthMargin() → 四方向边距检测                       │
│  - HasObstacleInDirection() → 单方向检测                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      StageConfig                             │
├─────────────────────────────────────────────────────────────┤
│  新增字段:                                                    │
│  - verticalMargin (上下边距，共用)                            │
│  - horizontalMargin (左右边距，共用)                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              TreeControllerV2Editor (新增)                   │
├─────────────────────────────────────────────────────────────┤
│  - 绘制 growthObstacleTags 的 MaskField                      │
│  - 复用 NavGrid2DEditor.DrawTagMask() 方法                   │
└─────────────────────────────────────────────────────────────┘
```

## 组件和接口

### StageConfig 类修改

```csharp
[System.Serializable]
public class StageConfig
{
    // ... 现有字段 ...
    
    [Header("成长边距")]
    [Tooltip("上下方向的成长边距（单位：Unity 单位）")]
    [Range(0f, 5f)]
    public float verticalMargin = 0.5f;
    
    [Tooltip("左右方向的成长边距（单位：Unity 单位）")]
    [Range(0f, 5f)]
    public float horizontalMargin = 0.3f;
}
```

### TreeControllerV2 类修改

```csharp
public class TreeControllerV2 : MonoBehaviour, IResourceNode
{
    // 新增字段
    [Header("成长障碍物检测")]
    [Tooltip("阻挡成长的物体标签")]
    [SerializeField] private string[] growthObstacleTags = new string[] { "Tree", "Rock", "Building" };
    
    // 移除的字段
    // - growthSpaceMultiplier
    // - stageGrowthRadius
    
    // 新增方法
    public bool CanGrowToNextStage()
    {
        if (!enableGrowthSpaceCheck) return true;
        if (currentStageIndex >= STAGE_MAX) return false;
        
        var nextStageConfig = GetStageConfig(currentStageIndex + 1);
        if (nextStageConfig == null) return true;
        
        return CheckGrowthMargin(nextStageConfig.verticalMargin, nextStageConfig.horizontalMargin);
    }
    
    private bool CheckGrowthMargin(float verticalMargin, float horizontalMargin)
    {
        Vector2 center = GetColliderCenter();
        
        // 检测四个方向
        if (HasObstacleInDirection(center, Vector2.up, verticalMargin)) return false;
        if (HasObstacleInDirection(center, Vector2.down, verticalMargin)) return false;
        if (HasObstacleInDirection(center, Vector2.left, horizontalMargin)) return false;
        if (HasObstacleInDirection(center, Vector2.right, horizontalMargin)) return false;
        
        return true;
    }
    
    private bool HasObstacleInDirection(Vector2 center, Vector2 direction, float distance)
    {
        // 使用 Physics2D.Raycast 或 OverlapCircle 检测
        // 检查是否有指定标签的 Collider
    }
}
```

### TreeControllerV2Editor 类（新增）

```csharp
[CustomEditor(typeof(TreeControllerV2))]
public class TreeControllerV2Editor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 绘制除 growthObstacleTags 之外的所有字段
        DrawPropertiesExcluding(serializedObject, "m_Script", "growthObstacleTags");
        
        // 使用 MaskField 绘制 growthObstacleTags
        NavGrid2DEditor.DrawTagMask(
            serializedObject.FindProperty("growthObstacleTags"), 
            "Growth Obstacle Tags"
        );
        
        serializedObject.ApplyModifiedProperties();
    }
}
```

## 数据模型

### 边距检测流程

```
树木尝试成长
    │
    ▼
获取下一阶段配置
    │
    ├─ verticalMargin (上下边距)
    └─ horizontalMargin (左右边距)
    │
    ▼
获取当前 Collider 中心点
    │
    ▼
四方向检测
    │
    ├─ 上方: center + (0, verticalMargin)
    ├─ 下方: center - (0, verticalMargin)
    ├─ 左方: center - (horizontalMargin, 0)
    └─ 右方: center + (horizontalMargin, 0)
    │
    ▼
每个方向使用 Physics2D.OverlapCircle 检测
    │
    ├─ 检测半径: 0.1f (小范围检测)
    └─ 过滤: 只检查 growthObstacleTags 中的标签
    │
    ▼
任意方向有障碍物 → 返回 false (不能成长)
所有方向无障碍物 → 返回 true (可以成长)
```

### 默认边距配置

| 阶段 | 上下边距 | 左右边距 | 说明 |
|------|---------|---------|------|
| 0 (树苗) | 0.2 | 0.15 | 树苗很小，边距小 |
| 1 (小树苗) | 0.3 | 0.2 | 稍大一点 |
| 2 (中等树) | 0.5 | 0.35 | 中等大小 |
| 3 (大树) | 0.7 | 0.5 | 较大 |
| 4 (成熟树) | 0.9 | 0.65 | 更大 |
| 5 (完全成熟) | 1.1 | 0.8 | 最大 |

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. 
Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 边距检测方向完整性
*For any* 树木成长检测，系统应该检测上、下、左、右四个方向，且每个方向使用正确的边距值（上下使用 verticalMargin，左右使用 horizontalMargin）
**Validates: Requirements 3.2**

### Property 2: 障碍物阻挡成长
*For any* 成长检测，如果任意方向的边距内存在带有指定标签的 Collider，则 CanGrowToNextStage() 应返回 false
**Validates: Requirements 3.4**

### Property 3: 无障碍物允许成长
*For any* 成长检测，如果所有方向的边距内均无带有指定标签的 Collider，则 CanGrowToNextStage() 应返回 true
**Validates: Requirements 3.5**

### Property 4: Collider 边界实时更新
*For any* 树木成功成长后，其 Collider 边界应该更新为新阶段的大小，后续检测应使用更新后的边界
**Validates: Requirements 4.2, 4.3**

## 错误处理

1. **Collider 不存在**：如果树木没有 Collider，使用 Transform.position 作为中心点
2. **配置为空**：如果 StageConfig 为空，默认允许成长
3. **标签数组为空**：如果 growthObstacleTags 为空，跳过检测，允许成长

## 测试策略

### 单元测试
- 验证 StageConfig 新增字段的序列化
- 验证默认配置工厂创建的边距值
- 验证 CheckGrowthMargin 方法的四方向检测逻辑

### 集成测试
- 在场景中放置多棵树，验证成长检测是否正确
- 验证前一棵树成长后是否影响后续树木的检测
- 验证不同标签的障碍物是否被正确检测

### 属性测试框架
使用 Unity Test Framework 进行测试，配置每个测试运行 100 次迭代。

