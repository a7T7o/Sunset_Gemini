# Phase 4 - NavGrid 集成

## 阶段概述
修复树木成长时 NavGrid 动态刷新联动问题。

## 完成日期
2026-01-10

---

## 问题背景

Code Reaper 锐评指出：树木成长时碰撞体变大，但 NavGrid 没有被通知刷新，导致导航网格与实际碰撞体不同步。

## 修复内容

### 1. NavGrid2D.cs - 添加物理同步

```csharp
public void RebuildGrid()
{
    // 🔥 关键修复：同步物理系统的 Transform 变化
    Physics2D.SyncTransforms();
    
    if (autoDetectWorldBounds)
    {
        DetectWorldBounds();
    }
    // ...
}
```

### 2. TreeControllerV2.cs - 成长后刷新 NavGrid

```csharp
private void UpdatePolygonColliderShape()
{
    if (spriteRenderer == null || spriteRenderer.sprite == null) return;
    
    Collider2D[] colliders = GetComponents<Collider2D>();
    foreach (Collider2D collider in colliders)
    {
        if (collider is PolygonCollider2D poly)
        {
            UpdatePolygonColliderFromSprite(poly, spriteRenderer.sprite);
        }
    }
    
    // 🔥 碰撞体形状变化后，通知 NavGrid 刷新
    RequestNavGridRefresh();
}
```

### 3. ChestController.cs - 放置/推动后刷新 NavGrid

```csharp
private void Start()
{
    Initialize();
    // ...
    StartCoroutine(RequestNavGridRefreshDelayed());
}

private IEnumerator PushCoroutine(Vector3 targetPos)
{
    // ...
    transform.position = targetPos;
    _isPushing = false;
    
    // 🔥 推动完成后刷新 NavGrid
    RequestNavGridRefresh();
}
```

## 验收标准（需实机测试）

| 测试项 | 预期结果 |
|--------|---------|
| 树木成长后 NavGrid 更新 | 红格随树木阶段增长而扩大 |
| 箱子放置后 NavGrid 更新 | 箱子底下变成红色不可走区域 |
| 箱子推动后 NavGrid 更新 | 红格跟随箱子移动 |

## 实机测试记录

> 待用户验证后填写

| 测试项 | 结果 | 说明 |
|--------|------|------|
| 树木成长后 NavGrid 更新 | ⏳ 待测试 | |
| 箱子放置后 NavGrid 更新 | ⏳ 待测试 | |
| 箱子推动后 NavGrid 更新 | ⏳ 待测试 | |

## 相关锐评
详见 `code-reaper-reviews/review-session9-navgrid-desync.md`
