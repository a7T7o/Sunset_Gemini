---
inclusion: manual
priority: P1
keywords: [树木, 树林, 砍树, TreeController, 树桩, 生长阶段]
lastUpdated: 2025-01-09
---

# 树木系统规范

## 树木层级结构

> **层级结构详见 `layers.md` 中的"树木层级结构"章节，此处不再赘述。**

## TreeController 功能

### 生长阶段
- Sapling（树苗）：无碰撞
- Small（小树）：启用碰撞
- Large（大树）：启用碰撞

### 树木状态
- Normal：正常
- Withered：枯萎
- Frozen：冰冻
- Melted：融化
- Stump：树桩

### 季节变化
- 4 季 × 3 阶段 = 12 种外观
- 季节过渡渐变（基于随机种子的分布式变化）

### 碰撞体管理
- 从 Sprite 的 Custom Physics Shape 自动更新 PolygonCollider2D
- 树苗无碰撞，小树/大树启用碰撞
- 碰撞体状态变化时触发导航网格刷新

## 导航网格同步

### 触发条件
```csharp
// 检测碰撞体状态变化
if (hadEnabledCollider != hasEnabledCollider)
{
    RequestNavGridRefresh();  // 延迟 0.2 秒刷新
}
```

### 刷新机制
```csharp
private void RequestNavGridRefresh()
{
    CancelInvoke(nameof(DoNavGridRefresh));
    Invoke(nameof(DoNavGridRefresh), 0.2f);
}

private void DoNavGridRefresh()
{
    NavGrid2D.OnRequestGridRefresh?.Invoke();
}
```

## Sprite 底部对齐

### AlignSpriteBottom 方法
- 自动计算 sprite bounds
- 对齐父物体中心（树根位置）
- 阴影缩放与位置同步

## 遮挡透明配置

### 组件配置
- ❌ 不需要 DynamicSortingOrder（静态物体）
- ✅ 需要 PolygonCollider2D
- ✅ 需要 OcclusionTransparency
- ✅ 自动校准会处理 Order

### 标签配置
- 使用 `Tree` 标签（单数形式）
- Manager 过滤标签：`Trees`, `Buildings`, `Rocks`

## 批量添加组件

### 执行流程
1. 父物体添加 `Rigidbody2D` (Static)
2. Tree 子物体设置 `compositeOperation = Merge`
3. 父物体添加 `CompositeCollider2D` (Trigger)
4. 调用 `composite.GenerateGeometry()` 强制刷新
5. 删除父物体空 `SpriteRenderer`

### 编辑器工具
- 菜单：`Tools → 🌳 批量添加遮挡组件`
- 菜单：`Tools → 🔧 校准所有静态物体Order`

## Order 计算（双层结构）

```csharp
private float CalculateSortingY(SpriteRenderer sr)
{
    // 双层结构检测
    Transform parent = sr.transform.parent;
    if (parent != null && parent.GetComponent<SpriteRenderer>() == null)
    {
        // 父物体无SR → 用父物体Y（种植点）
        return parent.position.y + bottomOffset;
    }
    
    // 常规：Collider > Sprite > Transform
}
```

## 调试工具

### TreeController Inspector
- `showDebugInfo = true`：显示刷新日志

### 可视化
- Scene 视图中可见绿色连通区域
- 遮挡时显示红色方框


---

## 6阶段系统核心规则（游戏数值）

> 以下内容从 `tree-system.md` 合并而来，包含关键的游戏策划数值。

### 阶段配置（必须遵守）

| 阶段 | 天数 | 血量 | 碰撞 | 遮挡 | 树桩 | 工具 |
|------|------|------|------|------|------|------|
| 0 | 1 | 0 | ✗ | ✗ | ✗ | 锄头 |
| 1 | 2 | 4 | ✗ | ✗ | ✗ | 斧头 |
| 2 | 2 | 9 | ✓ | ✓ | ✗ | 斧头 |
| 3 | 4 | 17 | ✓ | ✓ | ✓ | 斧头 |
| 4 | 5 | 28 | ✓ | ✓ | ✓ | 斧头 |
| 5 | - | 40 | ✓ | ✓ | ✓ | 斧头 |

### 工具交互规则

1. **阶段0（树苗）只能用锄头挖出**
   - 斧头命中阶段0：无效果
   - 锄头命中阶段0：移除树苗并生成掉落物

2. **阶段1-5只能用斧头砍**
   - 斧头命中：造成伤害
   - 锄头命中：只播放抖动效果

3. **树桩不能再交互**

### 树桩生成规则

- 阶段0-2：砍倒后直接销毁，不留树桩
- 阶段3-5：砍倒后显示对应阶段的树桩

### 碰撞体与遮挡规则（数值层面）

- 阶段0-1：禁用碰撞体和遮挡透明
- 阶段2-5：启用碰撞体和遮挡透明
- 树桩：保持碰撞体，禁用遮挡透明

## 文档同步规则

当修改树木系统相关代码后，必须更新：
1. `.kiro/specs/tree-system/memory.md` - 记录会话内容
2. `Docx/分类/树木/000_树木系统完整文档.md` - 同步设计变更

## 相关规格文档

- `.kiro/specs/tree-system/` - 树木系统规格
- `.kiro/specs/tree-chopping-system/` - 树木砍伐系统规格
