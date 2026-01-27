# 需求文档 - 箱子系统终极清算

## 引言

本文档是箱子系统的终极清算需求，经过 6 轮修复后仍存在严重问题，现进行彻底分析和修复。

**历史背景**：
- 第 4 子工作区：箱子 UI 交互完善
- 第 5 子工作区：箱子 UI 交互完善 V2
- 第 6 子工作区：锐评修复 V3（包含全盘分析报告）
- 本次：第 7 子工作区 - 终极清算

## 术语表

- **Box_System**: 箱子系统，包括 ChestController、ChestInventory、BoxPanelUI 等组件
- **Navigator**: PlayerAutoNavigator，负责右键点击自动导航
- **PackagePanel**: 背包面板，包含 Main（背包区域）和 Top（Tab 栏）
- **BoxPanelUI**: 箱子 UI 面板，在 PackagePanel 内部实例化
- **Held_State**: 拿起状态，玩家手上持有物品的状态
- **SlotDragContext**: 跨容器拖拽上下文，记录拖拽源和目标
- **HeldItemDisplay**: 拿起物品显示组件，跟随鼠标显示被拿起的物品
- **InventoryInteractionManager**: 背包交互管理器，处理物品拖拽和放置

---

## 🔴 用户报告的问题（原文）

> 1. 右键导航逻辑混乱 - 先点B箱子走过去打开关闭，再右键A箱子会直接原地打开A内容，再右键C箱子会走向A但打开C内容
> 2. 控制台输出过多（227条），`[BoxPanelUI] Up[35] -> ChestInventory, item=-1x0` 类型日志未删除
> 3. SortDown/SortUp 功能不正确
> 4. 左键长按的Held状态不出现

---

## 问题根源分析（代码级别）

### 🔴 问题1：右键导航到箱子的逻辑混乱（最严重）

**现象描述：**
- 先右键点击B箱子，走过去打开后关闭
- 再右键A箱子，会直接原地打开A箱子内容（不走过去）
- 再关闭右键C箱子，玩家走向A但打开的是C箱子内容

**根本原因分析（结合全盘分析报告 V2）：**

问题不是简单的闭包捕获，而是**多重问题叠加**：

#### 原因 1：`Cancel()` 在卡顿取消时也执行回调

查看 `PlayerAutoNavigator.cs` 的 `Cancel()` 方法：

```csharp
public void Cancel()
{
    bool wasActive = active;
    var callback = _onArrivedCallback;
    
    // ...
    
    // 如果是正常到达（不是被打断），执行回调
    if (wasActive && callback != null)
    {
        callback.Invoke();  // ← 卡顿取消也会执行回调！
    }
}
```

注释说"如果是正常到达"，但实际上**卡顿取消也会执行回调**！

当导航卡顿 3 次后取消时，会执行 `TryInteract(interactable)`，但此时玩家可能还在原地，距离目标箱子很远。

#### 原因 2：`TryInteract()` 没有距离检查

```csharp
private void TryInteract(IInteractable interactable)
{
    if (interactable == null) return;
    
    var context = BuildInteractionContext();
    
    if (!interactable.CanInteract(context))
    {
        Debug.Log($"[GameInputManager] 当前无法交互");
        return;
    }
    
    interactable.OnInteract(context);  // ← 没有距离检查！
}
```

`TryInteract()` 只检查 `CanInteract()`，但 `ChestController.CanInteract()` 可能没有距离检查，导致远距离也能打开箱子。

#### 原因 3：箱子位置被 NavGrid 标记为不可走

日志显示 `终点可走: False`，这说明箱子的位置被 NavGrid 标记为障碍物。导航系统找到替代终点，但玩家无法到达，导致卡顿取消。

#### 原因 4：闭包捕获的 interactable 在导航切换时未清理

当玩家在导航到 B 的过程中右键点击 C：
1. `FollowTarget(C, ...)` 被调用
2. 旧的回调 `() => TryInteract(B)` 被新的回调 `() => TryInteract(C)` 覆盖
3. 但如果 `Cancel()` 被调用（卡顿或手动取消），可能执行错误的回调

#### 原因 5：`ChestController.CanInteract()` 缺少距离检查

需要确认 `ChestController.CanInteract()` 是否有距离检查。如果没有，远距离也能通过 `CanInteract()` 检查，导致远距离打开箱子。

**修复方向：**
1. `Cancel()` 方法需要区分"正常到达"和"卡顿取消"，只有正常到达才执行回调
2. `TryInteract()` 需要添加距离检查
3. 导航切换时需要清理旧的回调
4. 考虑在 `ChestController.CanInteract()` 中添加距离检查

---

### 🟡 问题2：控制台输出过多（227条）

**现象描述：**
- 打开箱子后控制台输出 227 条日志
- 大量 `[BoxPanelUI] Up[35] -> ChestInventory, item=-1x0` 类型日志

**根本原因分析：**

#### 原因 1：`BoxPanelUI.cs` 中的日志未受开关控制

```csharp
// BoxPanelUI.cs 第285行附近
Debug.Log($"[BoxPanelUI] Up[{index}] -> ChestInventory, item={stack.itemId}x{stack.amount}");
```

这行日志在每次绑定槽位时都会输出，36个槽位就是36条日志。

#### 原因 2：`GameInputManager.cs` 中的日志未受开关控制

```csharp
// GameInputManager.cs 第385行附近
Debug.Log($"[GameInputManager] HandleInteractable: ...");
```

#### 原因 3：`InventoryInteractionManager.cs` 中的日志未受开关控制

多处 `Debug.Log` 调用未使用 `showDebugInfo` 开关。

**修复方向：**
1. 所有 `Debug.Log` 必须使用 `showDebugInfo` 开关控制
2. 高频函数（如 `Refresh()`、`BindSlotData()`）中禁止输出详细日志
3. 遵循 `debug-logging-standards.md` 规范

---

### 🟡 问题3：SortDown/SortUp 功能不正确

**现象描述：**
- 用户报告 Sort 功能不正确（具体表现未详细描述）

**代码分析：**

查看 `BoxPanelUI.cs` 中的 Sort 按钮绑定：

```csharp
// SortUp 按钮 → 整理箱子
_sortUpButton.onClick.AddListener(() => {
    _currentChest?.Inventory?.Sort();
    RefreshUpSlots();
});

// SortDown 按钮 → 整理背包
_sortDownButton.onClick.AddListener(() => {
    _inventoryService?.Sort();
    RefreshDownSlots();
});
```

**可能的问题：**

1. **`ChestInventory.Sort()` 实现问题**：需要确认是否正确实现
2. **`InventoryService.Sort()` 只排序 12-35**：不含 Hotbar（0-11），这是设计如此
3. **UI 刷新问题**：`RefreshUpSlots()` 和 `RefreshDownSlots()` 可能没有正确刷新
4. **事件触发问题**：Sort 后可能没有触发 `OnInventoryChanged` 事件

**修复方向：**
1. 确认 `ChestInventory.Sort()` 和 `InventoryService.Sort()` 的实现
2. 确认 Sort 后是否触发 `OnInventoryChanged` 事件
3. 确认 `RefreshUpSlots()` 和 `RefreshDownSlots()` 是否正确刷新 UI

---

### 🟡 问题4：左键长按的 Held 状态不出现

**现象描述：**
- 用户报告左键长按后 Held 状态不出现

**代码分析：**

查看 `InventorySlotInteraction.cs` 的拖拽逻辑：

```csharp
public void OnBeginDrag(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;
    if (_slotUI.Container == null) return;
    
    var stack = _slotUI.Container.GetItem(_slotUI.SlotIndex);
    if (stack.itemId < 0) return;  // 空槽位不能拖拽
    
    // 进入拖拽状态
    _manager.BeginDrag(_slotUI, eventData);
}
```

**可能的问题：**

1. **Unity 拖拽阈值**：Unity 默认需要移动一定距离才触发 `OnBeginDrag`
2. **`Container` 为 null**：如果 `_slotUI.Container` 为 null，拖拽会被忽略
3. **空槽位检查**：如果 `stack.itemId < 0`，拖拽会被忽略
4. **`_manager` 为 null**：如果 `InventoryInteractionManager` 未正确初始化

**修复方向：**
1. 确认 `InventorySlotUI.Container` 是否正确绑定
2. 确认 `InventoryInteractionManager` 是否正确初始化
3. 添加调试日志确认拖拽流程是否被触发

---

## 需求

### 需求 1：右键导航到箱子功能正确

**用户故事：** 作为玩家，我想要右键点击箱子时，玩家能正确导航到箱子并打开正确的箱子，以便我能与正确的箱子交互。

#### 验收标准

1. WHEN 玩家右键点击远处箱子A THEN Box_System SHALL 导航玩家到箱子A附近并打开箱子A的内容
2. WHEN 玩家关闭箱子A后右键点击远处箱子B THEN Box_System SHALL 导航玩家到箱子B附近并打开箱子B的内容
3. WHEN 玩家在导航到箱子B的过程中右键点击箱子C THEN Box_System SHALL 取消对B的导航并开始导航到C，到达后打开C的内容
4. WHEN 玩家已经在箱子附近时右键点击该箱子 THEN Box_System SHALL 直接打开该箱子而不是其他箱子
5. IF 导航因卡顿被取消 THEN Box_System SHALL 不执行任何箱子交互
6. IF 导航被玩家手动取消（如按下移动键） THEN Box_System SHALL 不执行任何箱子交互

---

### 需求 2：控制台输出干净

**用户故事：** 作为开发者，我想要控制台只输出必要的信息，以便我能快速定位问题。

#### 验收标准

1. WHEN 玩家进行正常箱子交互操作（打开→操作→关闭） THEN Box_System SHALL 输出不超过 15 条日志
2. THE Box_System SHALL 删除所有 `[BoxPanelUI] Up[X] -> ChestInventory, item=-1x0` 类型的日志
3. THE Box_System SHALL 将所有 `Debug.Log` 调用改为受 `showDebugInfo` 开关控制
4. THE Box_System SHALL 遵循 `debug-logging-standards.md` 规范
5. WHERE 高频函数（如 `Refresh()`、`BindSlotData()`） THEN Box_System SHALL 禁止输出详细日志

---

### 需求 3：箱子排序功能正确

**用户故事：** 作为玩家，我想要点击排序按钮时，箱子/背包内容能正确排序并立即显示，以便我能快速整理物品。

#### 验收标准

1. WHEN 玩家点击 SortUp 按钮 THEN Box_System SHALL 按物品ID升序排列箱子内容
2. WHEN 玩家点击 SortDown 按钮 THEN Box_System SHALL 按物品ID升序排列背包内容（不含 Hotbar）
3. WHEN 排序完成 THEN Box_System SHALL 立即刷新 UI 显示，无需手动刷新
4. THE 排序结果 SHALL 将空槽位移动到末尾
5. THE 排序结果 SHALL 合并相同物品的堆叠

---

### 需求 4：左键拖拽拿起功能正确

**用户故事：** 作为玩家，我想要左键拖拽物品槽位时，能拿起物品并看到物品跟随鼠标，以便我能移动物品到其他槽位。

#### 验收标准

1. WHEN 玩家左键按下非空槽位并拖动 THEN Box_System SHALL 进入 Held_State
2. WHILE 处于 Held_State THEN Box_System SHALL 在鼠标位置显示物品图标（HeldItemDisplay）
3. WHEN 玩家拖拽到目标槽位并释放 THEN Box_System SHALL 将物品放置到目标槽位
4. IF 目标槽位有相同物品且可堆叠 THEN Box_System SHALL 合并物品
5. IF 目标槽位有不同物品 THEN Box_System SHALL 交换两个物品
6. WHEN 玩家拖拽到面板外并释放 THEN Box_System SHALL 丢弃物品到玩家脚下

---

## 修复方案概述

### 方案 1：修复右键导航逻辑

1. **修改 `PlayerAutoNavigator.Cancel()`**：
   - 添加参数区分"正常到达"和"卡顿/手动取消"
   - 只有正常到达才执行回调

2. **修改 `GameInputManager.TryInteract()`**：
   - 添加距离检查，超出交互距离不执行交互

3. **修改 `GameInputManager.HandleInteractable()`**：
   - 在开始新导航前，清理旧的回调状态

### 方案 2：清理日志输出

1. **`BoxPanelUI.cs`**：
   - 删除或注释所有 `Debug.Log` 调用
   - 保留 `Debug.LogError` 和 `Debug.LogWarning`

2. **`GameInputManager.cs`**：
   - 将所有 `Debug.Log` 改为受 `showDebugInfo` 开关控制

3. **`InventoryInteractionManager.cs`**：
   - 将所有 `Debug.Log` 改为受 `showDebugInfo` 开关控制

### 方案 3：修复 Sort 功能

1. 确认 `ChestInventory.Sort()` 实现
2. 确认 Sort 后触发 `OnInventoryChanged` 事件
3. 确认 UI 刷新逻辑

### 方案 4：修复拖拽功能

1. 添加调试日志确认拖拽流程
2. 确认 `Container` 绑定正确
3. 确认 `InventoryInteractionManager` 初始化正确

---

## 验收清单

### 右键导航验收
- [ ] 右键点击远处箱子A，玩家走过去并打开A的内容
- [ ] 关闭A，右键点击远处箱子B，玩家走过去并打开B的内容
- [ ] 导航到B的过程中右键点击C，玩家改为走向C并打开C的内容
- [ ] 玩家在箱子附近，右键直接打开正确的箱子
- [ ] 导航卡顿取消后，不打开任何箱子
- [ ] 玩家手动取消导航后，不打开任何箱子

### 日志输出验收
- [ ] 打开箱子 UI：≤ 3 行日志
- [ ] 点击槽位：≤ 2 行日志
- [ ] 拖拽物品：≤ 3 行日志
- [ ] Sort 操作：≤ 2 行日志
- [ ] 关闭箱子 UI：≤ 1 行日志
- [ ] 无 `item=-1x0` 类型的日志
- [ ] 一次完整交互流程（打开→操作→关闭）日志数量 ≤ 15 行

### Sort 功能验收
- [ ] SortUp 正确排序箱子内容（按物品ID升序）
- [ ] SortDown 正确排序背包内容（按物品ID升序，不含 Hotbar）
- [ ] 排序后 UI 立即刷新
- [ ] 空槽位移动到末尾
- [ ] 相同物品堆叠合并

### 拖拽功能验收
- [ ] 左键按下非空槽位并拖动，进入 Held 状态
- [ ] Held 状态下物品图标跟随鼠标
- [ ] 拖拽到空槽位释放，物品移动到目标槽位
- [ ] 拖拽到相同物品槽位释放，物品合并
- [ ] 拖拽到不同物品槽位释放，物品交换
- [ ] 拖拽到面板外释放，物品丢弃到玩家脚下

