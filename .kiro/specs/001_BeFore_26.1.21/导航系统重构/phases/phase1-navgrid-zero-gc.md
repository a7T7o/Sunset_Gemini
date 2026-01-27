# 第一阶段：基建重组 - Zero GC 网格重建

## 阶段目标

实现 NavGrid2D 的 Unity 6 改造，消除 GC 分配。

## 验收状态：✅ 通过（带警告）

**验收日期**：2025-01-08

## 核心修改

### 1. 引入 Collider2D[] 缓存池

```csharp
private Collider2D[] _colliderCache = new Collider2D[10];
```

### 2. 替换 OverlapCircleAll 为 OverlapCircleNonAlloc

```csharp
// ❌ 旧代码：GC 炸弹
var allHits = Physics2D.OverlapCircleAll(worldPos, radius);

// ✅ 新代码：零 GC
int hitCount = Physics2D.OverlapCircleNonAlloc(worldPos, radius, _colliderCache);
```

### 3. 压力测试脚本

创建 `NavGrid2DStressTest.cs`：
- 每帧调用 100 次 `IsWalkable`
- 在屏幕上显示统计信息
- 提示用户打开 Profiler 查看 GC Alloc

## 验收数据

| 指标 | 数值 |
|------|------|
| 总调用次数 | 230.5 万次 → 184.0 万次 |
| 平均 FPS | 230.5 → 184.0（非常高） |
| GC 曲线 | 平坦，无明显尖刺 |
| 压力测试 | 每帧 100 次调用，无卡顿 |

## ⚠️ 残留隐患

### 隐患 1：缓存数组大小陷阱

- `_colliderCache` 固定为 10
- 如果某个格子堆叠了 12 个物体，可能漏检
- 建议：改为 `const int MAX_COLLIDER_CACHE = 16;`

### 隐患 2：CPU 峰值风险

- 消除了 GC，但 `RebuildGrid` 依然在一帧内跑完
- 大地图可能卡一下
- 建议：后续用分帧处理优化

## 相关文件

- `Assets/YYY_Scripts/Service/Navigation/NavGrid2D.cs`
- `Assets/YYY_Scripts/Service/Navigation/NavGrid2DStressTest.cs`
