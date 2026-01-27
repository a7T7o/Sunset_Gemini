# 树木系统 - 成长空间检测设计

## 文档说明
本文档从 memory.md 会话 4 迁移而来，包含成长空间检测方案设计。

---

## 核心思路

基于目标阶段 Sprite 尺寸的圆形检测：
```
检测半径 = 目标阶段 Sprite 宽度 × 系数（建议 0.4~0.5）
```

## 检测参数

| 目标阶段 | Sprite 宽度估算 | 检测系数 | 检测半径 | 说明 |
|---------|----------------|---------|---------|------|
| 0→1 | ~0.5 单位 | 0.3 | ~0.15 | 树苗很小，几乎不需要空间 |
| 1→2 | ~1.0 单位 | 0.4 | ~0.4 | 小树开始需要空间 |
| 2→3 | ~1.5 单位 | 0.5 | ~0.75 | 中等树需要更多空间 |
| 3→4 | ~2.0 单位 | 0.5 | ~1.0 | 大树需要显著空间 |
| 4→5 | ~2.5 单位 | 0.5 | ~1.25 | 成熟树需要最大空间 |

## 关键设计决策

1. **检测基准点**：使用树根位置（父物体位置）
2. **检测系数**：0.4~0.5，允许树冠 10%~20% 重叠
3. **检测对象**：只检测其他树木，不检测建筑、岩石
4. **成长失败处理**：保持当前阶段，下一天继续检测

## 配置字段

```csharp
[Header("成长空间检测")]
[SerializeField] private bool enableGrowthSpaceCheck = true;
[SerializeField] private float growthSpaceMultiplier = 0.45f;
[SerializeField] private float[] stageGrowthRadius = { 0f, 0.15f, 0.4f, 0.75f, 1.0f, 1.25f };
[SerializeField] private bool showGrowthBlockedInfo = true;
```

## 核心逻辑

1. 使用 `Physics2D.OverlapCircleAll` 检测周围碰撞体
2. 只检测其他树木（TreeControllerV2 和 TreeController）
3. 如果周围有同等或更大阶段的树木，则无法成长
4. 两棵树同时尝试成长时，InstanceID 较小的优先
5. 空间不足时保持当前阶段，天数不重置，等待空间

## 实现日期
2025-12-24
