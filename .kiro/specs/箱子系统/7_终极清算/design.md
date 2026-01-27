# 设计文档 - 箱子系统终极清算

## 概述

本文档描述如何实现 `requirements_2.md` 中定义的四大需求（A/B/C/D）。

---

## 架构

### 核心组件

| 组件 | 职责 |
|------|------|
| PlayerAutoNavigator | 导航状态机，管理路径寻找和到达判定 |
| GameInputManager | 输入处理，统一交互入口 |
| BoxPanelUI | 箱子 UI，管理 Up/Down 区域 |
| InventoryInteractionManager | 背包交互，管理 Held 状态 |
| InventorySlotInteraction | 槽位交互，处理拖拽事件 |
| HeldItemDisplay | Held 图标显示 |

### 数据流

```
右键点击 → GameInputManager.HandleInteractable()
         → PlayerAutoNavigator.FollowTarget(target, stopRadius, onArrived)
         → 导航中...
         → 到达 → CompleteArrival() → onArrived() → TryInteract()
         → 取消/卡顿 → ForceCancel() → 不触发回调
```

---

## 需求 A 实现：导航与交互

### A-1/A-2 解决方案：navToken 机制

**问题根因分析**：

当前 `Cancel()` 方法的实现：
```csharp
public void Cancel()
{
    bool wasActive = active;
    var callback = _onArrivedCallback;
    // ... 重置状态 ...
    
    // ★ 问题：只要 wasActive 为 true 就执行回调
    if (wasActive && callback != null)
    {
        callback.Invoke();
    }
}
```

这导致以下场景都会执行回调：
1. **卡顿检测失败**（`CheckAndHandleStuck` 中调用 `Cancel()`）
2. **路径重建失败**（`BuildPath` 后 `path.Count == 0` 时调用 `Cancel()`）
3. **正常到达**（`ExecuteNavigation` 中调用 `Cancel()`）

用户反馈"没有移动也出现 bug"，说明问题不只是 `HandleMovement` 调用 `Cancel()`，而是 `Cancel()` 本身的逻辑有问题。

**解决方案**：引入 `navToken` 隔离不同导航请求，并拆分到达和取消逻辑。

```csharp
// PlayerAutoNavigator.cs
private int _navToken = 0;
private int _currentToken = 0;

public void FollowTarget(Transform t, float stopRadius, System.Action onArrived)
{
    // 先强制取消旧导航（不触发回调）
    ForceCancel();
    
    _navToken++;  // 每次新导航递增 token
    _currentToken = _navToken;
    _onArrivedCallback = onArrived;
    
    targetTransform = t;
    followStopRadius = Mathf.Max(0.1f, stopRadius);
    active = true;
    // ... 其他逻辑
}

/// <summary>
/// 正常到达时调用（唯一允许执行回调的入口）
/// </summary>
private void CompleteArrival()
{
    // 只有 token 匹配才执行回调
    if (_currentToken == _navToken && _onArrivedCallback != null)
    {
        var callback = _onArrivedCallback;
        ResetNavigationState();
        callback.Invoke();
    }
    else
    {
        ResetNavigationState();
    }
}

/// <summary>
/// 取消导航（不触发回调）- 用于卡顿、路径失败等
/// </summary>
public void Cancel()
{
    ResetNavigationState();
    // ★ 不执行回调
}

/// <summary>
/// 强制取消导航（不触发回调）- 用于切换目标、玩家移动打断
/// </summary>
public void ForceCancel()
{
    _navToken++;  // 使旧 token 失效
    ResetNavigationState();
}

private void ResetNavigationState()
{
    active = false;
    targetTransform = null;
    path.Clear();
    pathIndex = 0;
    stuckRetryCount = 0;
    runWhileNavigating = false;
    _onArrivedCallback = null;
    if (movement != null) movement.SetMovementInput(Vector2.zero, false);
}
```

### 需要修改的调用点

| 文件 | 方法 | 当前调用 | 修改为 | 原因 |
|------|------|---------|--------|------|
| PlayerAutoNavigator | CheckAndHandleStuck | Cancel() | Cancel()（不变） | Cancel 不再执行回调 |
| PlayerAutoNavigator | ExecuteNavigation 到达分支 | Cancel() | CompleteArrival() | 正常到达，需要执行回调 |
| PlayerAutoNavigator | MoveDirectly 到达分支 | Cancel() | CompleteArrival() | 正常到达，需要执行回调 |
| PlayerAutoNavigator | BuildPath 失败后 | Cancel() | Cancel()（不变） | 路径失败，不执行回调 |
| GameInputManager | HandleMovement | Cancel() | ForceCancel() | 玩家移动打断，不执行回调 |
| GameInputManager | HandleInteractable 开始新导航前 | 无 | ForceCancel() | 切换目标，使旧 token 失效 |

### A-3 解决方案：统一入口 + 距离复核

**修改 `GameInputManager.TryInteract()`**：

```csharp
private void TryInteract(IInteractable interactable, Transform target)
{
    if (interactable == null || target == null) return;
    
    // ★ 距离复核：确保玩家真的在交互距离内
    Vector2 playerPos = GetPlayerCenter();
    Vector2 targetPos = GetTargetAnchor(target);  // 底部中心
    float distance = Vector2.Distance(playerPos, targetPos);
    float interactDist = interactable.GetInteractionDistance();
    
    if (distance > interactDist * 1.2f)  // 允许 20% 容差
    {
        // ★ 去重：同类警告只首次打印
        LogWarningOnce("DistanceTooFar", $"[GameInputManager] 距离过远，取消交互: {distance:F2} > {interactDist:F2}");
        return;
    }
    
    var context = BuildInteractionContext();
    if (!interactable.CanInteract(context)) return;
    
    interactable.OnInteract(context);
}

/// <summary>
/// 获取目标锚点（底部中心）
/// </summary>
private Vector2 GetTargetAnchor(Transform target)
{
    var collider = target.GetComponent<Collider2D>();
    if (collider != null)
    {
        // 底部中心：(center.x, min.y)
        return new Vector2(collider.bounds.center.x, collider.bounds.min.y);
    }
    return target.position;
}
```

---

## 需求 B 实现：日志治理

### B-1/B-2/B-3 解决方案：开关控制

**修改所有相关组件**：

```csharp
// BoxPanelUI.cs
[Header("Debug")]
[SerializeField] private bool showDebugInfo = false;  // ★ 默认 false

private void BindChestSlotData(int index, ItemStack stack)
{
    // ★ 删除逐格日志，或改为开关控制
    // Debug.Log($"[BoxPanelUI] Up[{index}] -> ...");  // 删除
    
    if (showDebugInfo)
    {
        Debug.Log($"[BoxPanelUI] BindChestSlotData: index={index}");
    }
}
```

### B-4 解决方案：删除逐格日志

**需要删除/注释的日志**：

| 文件 | 行号 | 日志内容 |
|------|------|---------|
| BoxPanelUI.cs | ~285 | `[BoxPanelUI] Up[{index}] -> ChestInventory, item=...` |
| GameInputManager.cs | ~385 | `[GameInputManager] HandleInteractable: ...` |
| InventoryInteractionManager.cs | 多处 | 高频 Debug.Log |
| UIItemIconScaler.cs | 多处 | UpdateScale 日志 |

---

## 需求 C 实现：排序与刷新

### C-1 解决方案：SortDown 与背包一致

**核心理解**：
- Hotbar 是背包的一部分，不是独立的
- Down 区镜像全部 36 个槽位（0..35）
- 排序对全部 36 个槽位执行，包括 Hotbar 区域
- Hotbar UI 只是引用背包第一行（0..11）进行展示

**修改 `BoxPanelUI.OnSortDownClicked()`**：

```csharp
private void OnSortDownClicked()
{
    // ★ 使用 InventorySortService，与背包 UI 完全一致
    // 对全部 36 个槽位（0..35）执行排序
    var sortService = FindFirstObjectByType<InventorySortService>();
    if (sortService != null)
    {
        sortService.SortInventory();
    }
    else
    {
        Debug.LogWarning("[BoxPanelUI] InventorySortService 未找到");
    }
    
    RefreshDownSlots();
}
```

**`InventorySortService.SortInventory()` 已有实现**（无需修改）：
- 收集 0..35 全部槽位的物品
- 合并相同物品（id + quality 相同）
- 按优先级排序（工具 > 武器 > 可放置 > 种子 > 消耗品 > 材料 > 其他）
- 放回 0..35 槽位

### C-2/C-3 解决方案：事件订阅

**确保 BoxPanelUI 订阅库存变化事件**：

```csharp
private void OnEnable()
{
    if (_inventoryService != null)
    {
        _inventoryService.OnInventoryChanged += RefreshDownSlots;
    }
    if (_currentChest?.Inventory != null)
    {
        _currentChest.Inventory.OnInventoryChanged += RefreshUpSlots;
    }
}

private void OnDisable()
{
    if (_inventoryService != null)
    {
        _inventoryService.OnInventoryChanged -= RefreshDownSlots;
    }
    if (_currentChest?.Inventory != null)
    {
        _currentChest.Inventory.OnInventoryChanged -= RefreshUpSlots;
    }
}
```

---

## 需求 D 实现：Held 状态

### D-1 解决方案：触发模型（已实现）

当前代码已正确实现：
- `OnPointerDown`：只选中，不拿起
- `OnBeginDrag`：长按 0.15s 或移动 5px 才进入 Held

### D-2 解决方案：统一图标入口

**问题分析**：

当前 `InventorySlotInteraction.cs` 中的图标显示逻辑：
```csharp
private void ShowDragIcon(ItemStack item)
{
    // ★ 问题：直接 FindFirstObjectByType，绕过了 Manager
    var heldDisplay = Object.FindFirstObjectByType<HeldItemDisplay>();
    // ...
}
```

这导致箱子路径和背包路径的图标显示逻辑不一致。

**解决方案**：

1. **在 `InventoryInteractionManager` 添加公共接口**：

```csharp
// InventoryInteractionManager.cs
/// <summary>
/// 显示 Held 图标（统一入口）
/// </summary>
public void ShowHeldIcon(int itemId, int amount, Sprite icon)
{
    heldDisplay?.Show(itemId, amount, icon);
}

/// <summary>
/// 隐藏 Held 图标（统一入口）
/// </summary>
public void HideHeldIcon()
{
    heldDisplay?.Hide();
}
```

2. **修改 `InventorySlotInteraction` 使用统一入口**：

```csharp
// InventorySlotInteraction.cs
private void ShowDragIcon(ItemStack item)
{
    var manager = InventoryInteractionManager.Instance;
    if (manager == null) return;
    
    var invService = Object.FindFirstObjectByType<InventoryService>();
    if (invService?.Database == null) return;
    
    var itemData = invService.Database.GetItemByID(item.itemId);
    if (itemData != null)
    {
        manager.ShowHeldIcon(item.itemId, item.amount, itemData.GetBagSprite());
    }
}

private void HideDragIcon()
{
    var manager = InventoryInteractionManager.Instance;
    manager?.HideHeldIcon();
}
```

### D-2a 解决方案：隐藏时机

**需要在以下时机调用 `HideHeldIcon()`**：

| 时机 | 调用位置 |
|------|---------|
| EndDrag | `InventorySlotInteraction.OnEndDrag()` |
| Cancel | `InventoryInteractionManager.Cancel()` |
| 丢弃成功 | `InventoryInteractionManager.DropItem()` 后 |
| UI 关闭 | `BoxPanelUI.Close()` |
| 导航开始 | `GameInputManager.HandleInteractable()` 开始导航前 |

**修改 `GameInputManager.HandleInteractable()`**：

```csharp
private void HandleInteractable(IInteractable interactable, Transform target)
{
    // ★ 导航开始前隐藏 Held 图标
    var manager = InventoryInteractionManager.Instance;
    if (manager != null && manager.IsHolding)
    {
        manager.Cancel();  // 取消会自动隐藏图标
    }
    
    // ... 导航逻辑
}
```

---

## 正确性属性

*正确性属性是对系统行为的形式化描述，用于验证实现是否符合需求。*

### Property 1: 导航到达才交互

**For any** 导航请求，只有当玩家真实到达目标且距离 ≤ 交互距离时，才会触发交互回调。

**Validates: Requirements A-1**

### Property 2: 导航切换隔离

**For any** 导航切换（从目标 A 切换到目标 B），旧目标 A 的回调永远不会被执行。

**Validates: Requirements A-2**

### Property 3: 日志预算

**For any** 完整的箱子交互流程（打开→操作→关闭），默认日志条数 ≤ 15。

**Validates: Requirements B-1**

### Property 4: 排序一致性

**For any** SortDown 操作，结果与背包 UI 的排序结果完全一致。

**Validates: Requirements C-1**

### Property 5: Held 图标统一

**For any** 进入 Held 状态的操作（无论来自背包还是箱子），图标显示行为完全一致。

**Validates: Requirements D-2**

---

## 错误处理

| 场景 | 处理方式 |
|------|---------|
| 导航卡顿 3 次 | ForceCancel()，不触发回调 |
| 玩家手动取消 | ForceCancel()，不触发回调 |
| 距离检查失败 | 不执行交互，输出警告日志 |
| 排序服务不存在 | 回退到 InventoryService.Sort() |

---

## 测试策略

### 单元测试

- navToken 递增和匹配逻辑
- ForceCancel 不触发回调
- 距离检查边界值

### 集成测试

- 导航切换场景（A→B→C）
- 卡顿取消场景
- 排序一致性验证

### 手动验收

按 `requirements_2.md` 中的验收清单逐项验证。
