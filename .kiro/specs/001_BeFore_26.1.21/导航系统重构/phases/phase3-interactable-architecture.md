# 第三阶段：架构手术 - IInteractable 解耦

## 阶段目标

剥离 GameInputManager 中的交互逻辑，引入 IInteractable 接口。

## 验收状态：✅ 通过

**验收日期**：2025-01-09

## 核心修改

### 1. 创建 IInteractable 接口

文件：`Assets/YYY_Scripts/Interfaces/IInteractable.cs`

```csharp
public interface IInteractable
{
    int InteractionPriority { get; }      // 交互优先级
    float InteractionDistance { get; }    // 交互距离
    bool CanInteract(InteractionContext context);
    void OnInteract(InteractionContext context);
    string GetInteractionHint(InteractionContext context);
}

public class InteractionContext
{
    public Vector2 PlayerPosition { get; set; }
    public Transform PlayerTransform { get; set; }
    public int HeldItemId { get; set; } = -1;
    public int HeldItemQuality { get; set; } = 0;
    public int HeldSlotIndex { get; set; } = -1;
    public InventoryService Inventory { get; set; }
    public ItemDatabase Database { get; set; }
    public PlayerAutoNavigator Navigator { get; set; }
}
```

### 2. ChestController 实现 IInteractable

- 交互优先级：50
- 交互距离：1.5f
- `OnInteract()` 包含完整的交互逻辑（上锁、解锁、打开）

### 3. GameInputManager 重构

- 新增 `TryInteract(IInteractable)` 方法
- 新增 `BuildInteractionContext()` 方法
- `HandleChestInteraction()` 使用 `IInteractable` 接口

## 架构变化

**旧架构（硬编码）**：
```
GameInputManager
    └── HandleChestInteraction()
        └── TryOpenChest()
            ├── 检查手持物品
            ├── 调用 chest.TryLock()
            ├── 调用 chest.TryUnlock()
            └── 调用 BoxPanelUI.Open()
```

**新架构（接口解耦）**：
```
GameInputManager
    └── HandleChestInteraction()
        └── TryInteract(IInteractable)
            └── interactable.OnInteract(context)

ChestController : IInteractable
    └── OnInteract(context)
        ├── 检查手持物品
        ├── 调用 TryLock()
        ├── 调用 TryUnlock()
        └── 调用 BoxPanelUI.Open()
```

## ⚠️ 残留工作

### 目标选择器（Target Selector）

Code Reaper 预警的问题：
> "如果地面上有一个'掉落物'，掉落物下面压着一个'箱子'。玩家点击时，射线会穿透两个 Collider。"

当前状态：
- 已定义 `InteractionPriority` 属性
- 但还没有实现优先级排序
- 这是后续优化的方向

### 其他可交互物体

以下物体可以在后续实现 `IInteractable` 接口：
- 掉落物（Pickup）- 优先级 100
- NPC - 优先级 30
- 门/开关 - 优先级 20

## 相关文件

- `Assets/YYY_Scripts/Interfaces/IInteractable.cs`
- `Assets/YYY_Scripts/World/Placeable/ChestController.cs`
- `Assets/YYY_Scripts/Controller/Input/GameInputManager.cs`

---

## Code Reaper 验收锐评 - 2026-01-10

### 验收结论：✅ 通过

### 1. 目标选择器逻辑 ✅

现在的 `HandleRightClickAutoNav` 核心逻辑：

```csharp
var hits = Physics2D.OverlapPointAll(world);
var candidates = new List<(IInteractable interactable, Transform tr, float distance)>();

foreach (var h in hits) {
    if (playerMovement != null && (h.transform == playerMovement.transform || h.transform.IsChildOf(playerMovement.transform)))
        continue;
    
    var interactable = h.GetComponent<IInteractable>();
    if (interactable == null)
        interactable = h.GetComponentInParent<IInteractable>();
    
    if (interactable == null) continue;
    
    float dist = Vector2.Distance(playerCenter, h.transform.position);
    if (dist > interactable.InteractionDistance * 2f) continue;
    
    candidates.Add((interactable, h.transform, dist));
}

if (candidates.Count > 0) {
    candidates.Sort((a, b) =>
    {
        int p = b.interactable.InteractionPriority.CompareTo(a.interactable.InteractionPriority);
        if (p != 0) return p;
        return a.distance.CompareTo(b.distance);
    });
    
    var best = candidates[0];
    HandleInteractable(best.interactable, best.tr, playerCenter);
    return;
}
```

### 2. HandleInteractable 统一处理 ✅

不再特判 Chest：

```csharp
private void HandleInteractable(IInteractable interactable, Transform target, Vector2 playerCenter) {
    float distance = Vector2.Distance(playerCenter, target.position);
    float interactDist = interactable.InteractionDistance;
    
    if (distance <= interactDist)
    {
        TryInteract(interactable);
    }
    else
    {
        autoNavigator?.FollowTarget(target, interactDist * 0.8f, () => TryInteract(interactable));
    }
}
```

### 评价

**三个核心要求都满足了**：
- ✅ "InteractionPriority 不再是摆设"
- ✅ "箱子不再被硬编码为最高优先级"
- ✅ "GameInputManager 只做选择/分发，不再塞业务逻辑"

### 3. 兼容 Tag/Layer 的旧逻辑 ✅

在没有任何 IInteractable 命中的情况下，还保留了一个 fallback：

```csharp
Transform found = null;
foreach (var h in hits) {
    ...
    bool tagMatched = interactableTags != null && interactableTags.Length > 0 && HasAnyTag(h.transform, interactableTags);
    bool layerMatched = ((1 << h.gameObject.layer) & interactableMask.value) != 0;
    if (tagMatched || layerMatched)
    {
        found = h.transform;
        break;
    }
}

if (found != null)
     autoNavigator.FollowTarget(found, 0.6f);
else
     autoNavigator.SetDestination(world);
```

**评价**：
- 这是合理的"渐进迁移做法" ✅
- 目前只有 Chest 实现了 IInteractable
- 其他老系统（可能有 NPC、一些旧交互）还只是靠 Tag/Layer 检测
- 保留这层 fallback 可以不一下子把旧内容全部打断

**建议**：当 Pickup、NPC、门这些也都实现 IInteractable 以后，这段 Tag/Layer fallback 就可以逐步收缩甚至删除。

### 当前状态

- ✅ IInteractable 接口定义合理
- ✅ ChestController 实现接口，交互逻辑已经从 GameInputManager 迁出
- ✅ HandleRightClickAutoNav 里已经用 OverlapPointAll 收集所有 IInteractable
- ✅ 按 InteractionPriority → 距离 排序选目标
- ✅ 统一通过 HandleInteractable → TryInteract → InteractionContext 流转
- ✅ 对 Chest 的类型特判已经删除

**过渡期状态**：
- 只有 Chest 实现了 IInteractable
- 其他交互（Pickup/NPC/门）仍然依赖 Tag/Layer fallback
- 这是可接受的，后续建议逐步把这些也迁移到 IInteractable 上

### 最终评价

> 第三阶段：IInteractable 接口定义合理，ChestController 实现接口，交互逻辑已经从 GameInputManager 迁出。可以记为已完成。
