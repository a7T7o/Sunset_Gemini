# 需求文档 - 箱子系统终极清算

## 引言

本文档是箱子系统的终极清算需求，旨在彻底修复所有遗留问题，确保系统稳定可用。

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

## 问题根源分析（代码级别）

### 问题1：右键导航到箱子的逻辑混乱

**现象描述：**
- 先右键点击B箱子，走过去打开后关闭
- 再右键A箱子，会直接原地打开A箱子内容（不走过去）
- 再关闭右键C箱子，玩家走向A箱子，但打开的是C箱子内容

**根本原因：闭包捕获导致的状态混乱**

查看 `GameInputManager.cs` 的 `HandleInteractable` 方法：

```csharp
private void HandleInteractable(IInteractable interactable, Transform target, Vector2 playerCenter)
{
    if (distance <= interactDist)
    {
        TryInteract(interactable);
    }
    else
    {
        autoNavigator.FollowTarget(target, interactDist * 0.8f, () =>
        {
            TryInteract(interactable);  // ★ 问题：interactable 是闭包捕获的！
        });
    }
}
```

- 当玩家右键点击箱子A，`interactable` 被闭包捕获
- 如果在导航过程中右键点击箱子C，新的导航会覆盖旧的
- 但旧的回调仍然持有对箱子A的引用
- 导航完成后，可能执行错误的回调

### 问题2：控制台输出过多（227条）

**根本原因：**
- `BoxPanelUI.cs` 第285行：`Debug.Log($"[BoxPanelUI] Up[{index}] -> ChestInventory, item={stack.itemId}x{stack.amount}");`
- `GameInputManager.cs` 第385行：`Debug.Log($"[GameInputManager] HandleInteractable: ...`
- `InventoryInteractionManager.cs` 多处 `Debug.Log`

### 问题3：SortUp/SortDown 按钮

**代码分析：**
- `SortUp` 按钮 → 调用 `_currentChest.Inventory.Sort()` → 整理箱子
- `SortDown` 按钮 → 调用 `_inventoryService.Sort()` → 整理背包
- 功能应该是正确的，需要验证 UI 刷新是否正常

### 问题4：左键拖拽的 Held 状态

**代码分析：**
`InventorySlotInteraction.cs` 的 `OnBeginDrag` 方法需要长按 0.15 秒或移动 5 像素才能触发。
这是 Unity 的标准拖拽行为，应该是正确的。

---

## 需求

### 需求 1：右键导航到箱子功能正确

**用户故事：** 作为玩家，我想要右键点击箱子时，玩家能正确导航到箱子并打开正确的箱子，以便我能与正确的箱子交互。

#### 验收标准

1. WHEN 玩家右键点击远处箱子A THEN Box_System SHALL 导航玩家到箱子A附近并打开箱子A的内容
2. WHEN 玩家关闭箱子A后右键点击远处箱子B THEN Box_System SHALL 导航玩家到箱子B附近并打开箱子B的内容
3. WHEN 玩家在导航到箱子B的过程中右键点击箱子C THEN Box_System SHALL 取消对B的导航并开始导航到C
4. WHEN 玩家已经在箱子附近时右键点击该箱子 THEN Box_System SHALL 直接打开该箱子而不是其他箱子
5. IF 导航被取消或中断 THEN Box_System SHALL 清理所有待处理的箱子交互状态

### 需求 2：控制台输出干净

**用户故事：** 作为开发者，我想要控制台只输出必要的信息，以便我能快速定位问题。

#### 验收标准

1. WHEN 玩家进行正常箱子交互操作 THEN Box_System SHALL 输出不超过5条日志
2. THE Box_System SHALL 删除所有 `[BoxPanelUI] Up[X] -> ChestInventory, item=-1x0` 类型的日志
3. THE Box_System SHALL 只保留 `Debug.LogError` 和 `Debug.LogWarning` 级别的日志
4. WHERE 需要调试日志 THEN Box_System SHALL 使用 `showDebugInfo` 开关控制输出

### 需求 3：箱子排序功能正确

**用户故事：** 作为玩家，我想要点击排序按钮时，箱子内容能正确排序，以便我能快速整理箱子。

#### 验收标准

1. WHEN 玩家点击 SortUp 按钮 THEN Box_System SHALL 按物品ID升序排列箱子内容
2. WHEN 玩家点击 SortDown 按钮 THEN Box_System SHALL 按物品ID升序排列背包内容
3. WHEN 排序完成 THEN Box_System SHALL 立即刷新UI显示
4. THE 排序逻辑 SHALL 与 `InventoryService.Sort` 保持一致

### 需求 4：左键拖拽拿起功能正确

**用户故事：** 作为玩家，我想要左键拖拽物品槽位时，能拿起物品，以便我能移动物品到其他槽位。

#### 验收标准

1. WHEN 玩家左键按下并拖动非空槽位 THEN Box_System SHALL 进入 Held_State
2. WHILE 处于 Held_State THEN Box_System SHALL 在鼠标位置显示物品图标
3. WHEN 玩家拖拽到目标槽位并释放 THEN Box_System SHALL 将物品放置到目标槽位
4. IF 目标槽位有物品且可堆叠 THEN Box_System SHALL 合并物品
5. IF 目标槽位有不同物品 THEN Box_System SHALL 交换两个物品
6. WHEN 玩家拖拽到面板外并释放 THEN Box_System SHALL 丢弃物品到玩家脚下

---

## 验收清单

- [ ] 右键导航到箱子A，打开A的内容
- [ ] 关闭A，右键导航到B，打开B的内容
- [ ] 导航中切换目标，正确切换到新目标
- [ ] 玩家在箱子附近，右键直接打开正确的箱子
- [ ] 控制台输出不超过5条
- [ ] 无 `item=-1x0` 类型的日志
- [ ] SortUp 正确排序箱子内容
- [ ] SortDown 正确排序背包内容
- [ ] 左键拖拽进入拿起状态
- [ ] 拿起状态显示物品图标跟随鼠标
- [ ] 放下物品到目标槽位
- [ ] 拖拽到面板外丢弃物品
