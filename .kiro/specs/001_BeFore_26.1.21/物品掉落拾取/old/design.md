# Design Document - 物品掉落与拾取系统优化

## Overview

本设计文档描述物品掉落与拾取系统的优化方案，包括简化掉落配置、优化掉落物分散显示、添加拾取飞向玩家动画、以及修复物品无法被拾取的问题。

## Architecture

### 组件关系

```
TreeControllerV2
├── dropItemData (ItemData)     # 掉落物品 SO
├── stageDropAmounts[6]         # 每阶段掉落数量
└── SpawnDrops()                # 生成掉落物

WorldSpawnService
├── SpawnMultipleScattered()    # 分散生成多个物品
└── GetScatteredPositions()     # 计算分散位置

WorldItemDrop
├── StartDrop()                 # 弹起动画
└── 现有弹跳逻辑

WorldItemPickup
├── FlyToPlayer()               # 飞向玩家动画
├── Collider2D                  # 物理检测（必需）
└── Tag: "Pickup"               # 拾取标签（必需）

AutoPickupService
├── 检测范围内物品
└── 触发 FlyToPlayer 动画
```

## Components and Interfaces

### 1. TreeControllerV2 掉落配置简化

移除 DropTable，改为直接配置：

```csharp
[Header("━━━━ 掉落配置 ━━━━")]
[Tooltip("掉落的物品 SO")]
[SerializeField] private ItemData dropItemData;

[Tooltip("各阶段掉落数量（阶段0-5）")]
[SerializeField] private int[] stageDropAmounts = new int[] { 0, 1, 2, 3, 5, 8 };

[Tooltip("树桩掉落数量（阶段3-5）")]
[SerializeField] private int[] stumpDropAmounts = new int[] { 0, 0, 0, 1, 2, 3 };
```

### 2. WorldSpawnService 分散生成

新增分散生成方法：

```csharp
/// <summary>
/// 在指定范围内分散生成多个物品
/// </summary>
public List<WorldItemPickup> SpawnScattered(
    ItemData data, 
    int quality, 
    int totalAmount, 
    Vector3 origin, 
    Bounds scatterBounds)
{
    // 在 bounds 范围内均匀分散生成
    // 每个物品有独立的随机位置
}
```

### 3. WorldItemPickup 飞向玩家动画

新增飞向玩家方法：

```csharp
/// <summary>
/// 飞向玩家动画
/// </summary>
public void FlyToPlayer(Transform player, System.Action onComplete)
{
    StartCoroutine(FlyToPlayerCoroutine(player, onComplete));
}

private IEnumerator FlyToPlayerCoroutine(Transform player, System.Action onComplete)
{
    float duration = 0.3f;
    Vector3 startPos = transform.position;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        // 使用缓动曲线
        t = 1f - Mathf.Pow(1f - t, 3f); // ease out cubic
        transform.position = Vector3.Lerp(startPos, player.position, t);
        yield return null;
    }
    
    onComplete?.Invoke();
}
```

### 4. AutoPickupService 触发飞向动画

修改拾取逻辑，添加背包空间检查：

```csharp
void Update()
{
    // ... 检测逻辑 ...
    
    if (pickup != null && !pickup.IsFlying)
    {
        // ★ 关键：先检查背包是否有空间
        if (!CanPickupItem(pickup))
        {
            continue; // 背包满，跳过此物品
        }
        
        // 触发飞向动画，动画完成后执行拾取
        pickup.FlyToPlayer(transform, () => {
            pickup.TryPickup(inventory);
        });
    }
}

/// <summary>
/// 检查背包是否有空间容纳指定物品
/// </summary>
private bool CanPickupItem(WorldItemPickup pickup)
{
    if (inventory == null) return false;
    return inventory.CanAddItem(pickup.itemId, pickup.quality, pickup.amount);
}
```

### 5. InventoryService 背包空间检查

新增检查方法：

```csharp
/// <summary>
/// 检查是否可以添加指定物品（不实际添加）
/// </summary>
public bool CanAddItem(int itemId, int quality, int amount)
{
    if (amount <= 0) return true;
    int remaining = amount;
    int maxStack = GetMaxStack(itemId);
    
    // 1) 检查第一行现有堆叠
    remaining = CountAvailableStackSpace(itemId, quality, remaining, 0, HotbarWidth, maxStack);
    if (remaining <= 0) return true;
    
    // 2) 检查第一行空位
    remaining = CountEmptySlots(remaining, 0, HotbarWidth, maxStack);
    if (remaining <= 0) return true;
    
    // 3) 检查其他行现有堆叠
    remaining = CountAvailableStackSpace(itemId, quality, remaining, HotbarWidth, inventorySize, maxStack);
    if (remaining <= 0) return true;
    
    // 4) 检查其他行空位
    remaining = CountEmptySlots(remaining, HotbarWidth, inventorySize, maxStack);
    
    return remaining <= 0;
}

private int CountAvailableStackSpace(int itemId, int quality, int remaining, int start, int end, int maxStack)
{
    for (int i = start; i < end && remaining > 0; i++)
    {
        var s = slots[i];
        if (!s.IsEmpty && s.itemId == itemId && s.quality == quality && s.amount < maxStack)
        {
            remaining -= (maxStack - s.amount);
        }
    }
    return Mathf.Max(0, remaining);
}

private int CountEmptySlots(int remaining, int start, int end, int maxStack)
{
    for (int i = start; i < end && remaining > 0; i++)
    {
        if (slots[i].IsEmpty)
        {
            remaining -= maxStack;
        }
    }
    return Mathf.Max(0, remaining);
}
```
```

## Data Models

### 掉落数量配置

| 阶段 | 树干掉落 | 树桩掉落 |
|------|----------|----------|
| 0    | 0        | -        |
| 1    | 1        | -        |
| 2    | 2        | -        |
| 3    | 3        | 1        |
| 4    | 5        | 2        |
| 5    | 8        | 3        |

### WorldItemPickup 必需组件

| 组件 | 用途 |
|------|------|
| Collider2D (Trigger) | 被 AutoPickupService 的 OverlapCircleAll 检测 |
| Tag: "Pickup" | 被 AutoPickupService 的标签过滤检测 |
| SpriteRenderer | 显示物品图标 |

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

本需求主要涉及游戏逻辑和动画效果，不涉及可自动化测试的正确性属性。

## Error Handling

1. **ItemData 为空**：如果 dropItemData 未配置，跳过掉落生成并输出警告
2. **Collider 缺失**：WorldPrefab 生成时自动添加 CircleCollider2D (Trigger)
3. **Tag 缺失**：WorldPrefab 生成时自动设置 Tag 为 "Pickup"

## Testing Strategy

### 手动测试

1. 砍伐不同阶段的树木，验证掉落数量正确
2. 验证掉落物分散显示，不完全重叠
3. 验证掉落物有弹起动画
4. 验证拾取时物品飞向玩家
5. 验证通过 Ctrl+左键 生成的物品可以被拾取

## 问题排查：物品无法被拾取

### 可能原因

1. **WorldPrefab 未生成**：ItemData.worldPrefab 为空，导致使用运行时创建的简单物体
2. **缺少 Collider2D**：运行时创建的物体可能没有 Collider2D
3. **Tag 不匹配**：物品的 Tag 不是 "Pickup"
4. **Layer 不匹配**：物品的 Layer 不在 AutoPickupService 的 pickupMask 中

### 解决方案

1. 使用 WorldPrefabGeneratorTool 批量生成 WorldPrefab
2. 确保 WorldPrefab 包含 CircleCollider2D (Trigger)
3. 确保 WorldPrefab 的 Tag 设置为 "Pickup"
