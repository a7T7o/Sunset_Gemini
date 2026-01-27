# 设计文档

## 概述

实现 Cinemachine 摄像头死区与场景边界的自动同步。系统在场景加载时自动检测边界，并将边界数据同步到 Cinemachine Position Composer 的 Dead Zone 参数。

## 架构

### 组件关系

```
┌─────────────────────────────────────────────────────────────┐
│                    CameraDeadZoneSync                       │
│  (挂载在 CinemachineCamera 同级或子物体)                      │
├─────────────────────────────────────────────────────────────┤
│  - 检测场景边界（复用 NavGrid2D 的逻辑）                       │
│  - 计算 Dead Zone 参数                                       │
│  - 同步到 CinemachinePositionComposer                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              CinemachinePositionComposer                    │
│  (Cinemachine 3.x 内置组件)                                  │
├─────────────────────────────────────────────────────────────┤
│  - Dead Zone Width/Height                                   │
│  - Screen Position X/Y                                      │
│  - Camera Distance                                          │
└─────────────────────────────────────────────────────────────┘
```

### 数据流

```
场景加载 → 检测 Tilemap 边界 → 计算可视区域 → 计算 Dead Zone → 应用到 Composer
```

## 组件与接口

### CameraDeadZoneSync

```csharp
public class CameraDeadZoneSync : MonoBehaviour
{
    [Header("边界检测")]
    [SerializeField] private bool autoDetectBounds = true;
    [SerializeField] private string[] worldLayerNames = { "LAYER 1", "LAYER 2", "LAYER 3" };
    [SerializeField] private float boundsPadding = 1f;
    
    [Header("手动边界（当 autoDetectBounds = false）")]
    [SerializeField] private Bounds manualBounds;
    
    [Header("引用")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    
    // 公共属性
    public Bounds CurrentBounds { get; private set; }
    
    // 公共方法
    public void RefreshBounds();
    public void ApplyDeadZone();
}
```

## 数据模型

### 边界数据

| 字段 | 类型 | 说明 |
|------|------|------|
| worldBounds | Bounds | 世界边界（从 Tilemap 检测） |
| cameraBounds | Bounds | 摄像头可移动边界（worldBounds - 半屏） |
| deadZoneSize | Vector2 | 计算出的死区大小 |

### Dead Zone 计算公式

```
摄像头可视宽度 = orthographicSize * 2 * aspect
摄像头可视高度 = orthographicSize * 2

// 摄像头中心可移动范围
cameraMoveRangeX = worldBounds.size.x - 摄像头可视宽度
cameraMoveRangeY = worldBounds.size.y - 摄像头可视高度

// Dead Zone 是相对于屏幕的比例 (0-1)
// 当玩家在 Dead Zone 内移动时，摄像头不跟随
// Dead Zone 越大，摄像头跟随越"懒"
```

## 正确性属性

*属性是系统应该满足的通用规则，用于验证实现的正确性。*

### 属性 1：边界检测完整性

*对于任意* 包含 Tilemap 的场景，检测到的边界 SHALL 包含所有 Tilemap 的联合边界
**验证：需求 2.1**

### 属性 2：摄像头约束有效性

*对于任意* 玩家位置，摄像头中心 SHALL 始终保持在 cameraBounds 内
**验证：需求 1.3**

### 属性 3：场景切换同步

*对于任意* 场景切换事件，系统 SHALL 在新场景加载后重新计算并应用 Dead Zone
**验证：需求 1.2**

## 错误处理

| 错误情况 | 处理方式 |
|---------|---------|
| 未找到 CinemachineCamera | 日志警告，禁用功能 |
| 未找到 PositionComposer | 日志警告，禁用功能 |
| 场景中无 Tilemap | 使用默认边界或手动边界 |
| 边界过小（小于摄像头可视区域） | 禁用 Dead Zone，摄像头固定 |

## 测试策略

### 单元测试

1. 边界检测逻辑测试
2. Dead Zone 计算公式测试

### 集成测试

1. 场景加载后自动同步测试
2. 场景切换同步测试
3. 运行时边界变化测试

### 手动测试

1. 玩家移动到场景边缘，验证摄像头不越界
2. 切换场景，验证 Dead Zone 自动更新
