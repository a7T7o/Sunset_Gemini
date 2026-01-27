# 箱子系统 - 掉落与自动放置

## 文档说明
对应需求 3：箱子掉落后自动放置逻辑

---

## 核心设计

**箱子不走通用掉落物管线，而是走自动放置逻辑。**

### 当前实现（ChestDropHandler.cs）

```csharp
public static bool HandleChestDrop(StorageData storageData, Vector3 dropPosition, ChestOwnership ownership)
{
    // 1. 查找空位（螺旋搜索）
    Vector3? emptyPos = FindEmptyPosition(dropPosition, MaxSearchRadius);
    
    // 2. 在空位实例化箱子预制体
    return SpawnPlacedChest(storageData, emptyPos.Value, ownership);
}
```

### 实现状态

| 功能 | 状态 | 说明 |
|------|------|------|
| 螺旋搜索算法 | ✅ 已实现 | `FindEmptyPosition()` |
| 碰撞检测 | ✅ 已实现 | `Physics2D.OverlapCircleAll` |
| 直接实例化箱子 | ✅ 已实现 | `SpawnPlacedChest()` |
| 掉落动画（1s） | ❓ 待确认 | 由 WorldSpawnService 托管？ |
| 禁止变为可拾取 | ✅ 设计正确 | 直接生成箱子，不走 ItemPickup |

---

## 待确认问题

1. **掉落动画**：`DropAnimationDuration = 1f` 常量存在，但动画由谁播放？
2. **WorldSpawnService 集成**：是否正确调用 ChestDropHandler？

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/World/Placeable/ChestDropHandler.cs` | 箱子掉落处理器 |
| `Assets/YYY_Scripts/Service/WorldSpawnService.cs` | 世界生成服务 |
