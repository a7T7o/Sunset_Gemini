# 箱子系统 - 交互流程

## 文档说明
对应需求 4 & 5：挖取/推动/交互完整流程

---

## 需求对照表

### 需求 4：挖取行为

| 验收标准 | 实现位置 | 状态 |
|---------|---------|------|
| 只有镐子能造成伤害 | `OnHit()` 检查 `ctx.toolType != ToolType.Pickaxe` | ✅ |
| 非镐子只抖动 | `PlayShakeEffect()` | ✅ |
| 空箱子被打掉 → 掉落物 | `OnDestroyed()` → `WorldSpawnService.SpawnWithAnimation` | ✅ |
| 有物品的箱子 → 推动 | `if (!IsEmpty) TryPush(ctx.hitDir)` | ✅ |

### 需求 5：推动行为

| 验收标准 | 实现位置 | 状态 |
|---------|---------|------|
| 方向来自玩家朝向 | `TryPush(ctx.hitDir)` | ✅ |
| 目标位置碰撞检测 | `Physics2D.OverlapCircleAll(targetPos, collisionCheckRadius)` | ✅ |
| 有碰撞体 → 取消推动 | `if (!hit.isTrigger) return` | ✅ |
| 无碰撞体 → 跳动移动 | `PushCoroutine()` 带跳跃效果 | ✅ |
| 推动后刷新 NavGrid | `RequestNavGridRefresh()` | ✅ |

---

## 核心代码流程

```
OnHit(ToolHitContext ctx)
    ├── PlayShakeEffect()  // 始终抖动
    ├── 非镐子 → return
    ├── !CanBeMinedOrMoved() → return  // 野外上锁箱子
    ├── !IsEmpty → TryPush(ctx.hitDir)  // 有物品 → 推动
    └── IsEmpty → 扣血 → OnDestroyed()  // 空箱子 → 破坏
```

---

## 安全机制

| 机制 | 实现 | 状态 |
|------|------|------|
| 推动重入保护 | `_isPushing` 标志位 | ✅ |
| 推动碰撞检测 | `OverlapCircleAll` | ✅ |
| NavGrid 同步 | `RequestNavGridRefresh()` | ✅ |

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/World/Placeable/ChestController.cs` | 箱子控制器 |
