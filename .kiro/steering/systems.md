---
inclusion: manual
priority: P1
keywords: [时间, 季节, 导航, 遮挡, NavGrid, TimeManager, Order]
lastUpdated: 2025-01-09
---

# 核心系统设计规范

## 时间与季节系统

### TimeManager
- 事件订阅方式：`TimeManager.OnDayChanged += OnDayChanged;`（静态事件）
- 事件签名：
  - `OnDayChanged(int year, int day, int totalDays)`
  - `OnHourChanged(int hour)`
  - `OnMinuteChanged(int minute)`
- 获取时间：`TimeManager.GetDay()`, `TimeManager.GetHour()`, `TimeManager.GetSeason()`

### SeasonManager
- 四季循环：Spring → Summer → Autumn → Winter
- 植被季节细分（5个阶段）：早春、春末夏初、夏末秋初、晚秋、冬季
- 季节枚举对应：Spring=0, Summer=1, Autumn=2, Winter=3

## 遮挡透明系统

### 核心逻辑
- 玩家在物体下方（PlayerY > ObjectY）+ Sprite Bounds 重叠 → 物体透明
- 使用 `Bounds.Intersects` 进行精确检测（性能比 OverlapPoint 提升 20 倍）

### 组件职责
- `OcclusionManager`：集中管理全局透明参数、检测逻辑、调试输出
- `OcclusionTransparency`：挂在可遮挡物体上，OnEnable 读取一次 Manager 参数

### 树林连通判定（双重条件）
```csharp
// 满足任一条件即为连通
if (rootDistance <= rootConnectionDistance) return true;  // 树根距离近
if (overlapRatio >= 0.15f) return true;                 // 树冠重叠显著
```

### 关键参数
- `rootConnectionDistance = 2.5f`：树根连通距离
- `maxForestSearchDepth = 50`：最大搜索深度
- `maxForestSearchRadius = 15f`：最大搜索半径

## 导航系统

### NavGrid2D
- A* 寻路算法，八方向、严格对角线检测
- 多点障碍物检测：中心+四角五点采样，防止穿墙
- 自动世界边界检测：基于 Tilemap 和场景物体

### 世界边界检测
- 扫描所有 Tilemap 的联合边界
- 层级过滤：只包含 `LAYER 1/2/3` 下的对象
- 排除玩家和临时物体（Clone、Pickup）
- 边界扩展：`boundsPadding = 5`

### 动态障碍物同步
- 全局事件：`NavGrid2D.OnRequestGridRefresh?.Invoke()`
- TreeController 检测碰撞体状态变化时触发
- 延迟 0.2 秒刷新，避免频繁调用

### 性能参考
| 场景大小 | 格子数量 | 重建时间 |
|---------|---------|---------|
| 50x50 | 10000 | ~50ms |
| 100x80 | 32000 | ~150ms |
| 200x200 | 160000 | ~800ms |

## Order 计算与自动校准

### 统一标准
```
优先级：
1. Collider2D.bounds.min.y + bottomOffset
2. Sprite.bounds.min.y + bottomOffset  
3. Transform.position.y + bottomOffset

Order = -Round(sortingY × 100) + 0
```

### 自动校准系统
- 文件：`StaticObjectOrderAutoCalibrator.cs`
- 触发时机：进入 Play 模式前自动执行
- 手动触发：`Tools → 🔧 校准所有静态物体Order`
- 跳过：有 DynamicSortingOrder 的物体、Order < -9990 的特殊标记物体
- Shadow 处理：Order = 父Order + (-1)

## 云朵阴影系统

### 方案 A：精灵云影（推荐）
- 使用 Multiply 材质的云影精灵
- 随机缩放/位置/速度横向移动
- Sorting Layer：`CloudShadow`（在世界物体之上、UI 之下）

### CloudShadowManager 参数
- `Intensity`：云影透明度 (0-1)
- `Density`：云影密度
- `ScaleRange`：随机缩放范围
- `Speed/Direction`：移动速度和方向
- `WeatherGate`：天气联动开关

### 天气联动
- Sunny：开启，Intensity=0.25~0.4
- PartlyCloudy：低强度/低密度
- Overcast/Rain/Snow：关闭
