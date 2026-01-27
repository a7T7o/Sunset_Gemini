# 实现计划 - 箱子系统终极清算

## 概述

按 A→C→D→B 顺序执行，每完成一项提交"变更点 + 验收结果"。

---

## 执行顺序说明

| 优先级 | 需求 | 原因 |
|--------|------|------|
| P0-1 | A 导航与交互 | 根因修复，影响最大 |
| P0-2 | C 排序与刷新 | 功能修复，用户可见 |
| P0-3 | D Held 状态 | 功能修复，用户可见 |
| P1 | B 日志治理 | 体验优化，最后处理 |

---

## P0-1：需求 A - 导航与交互

### 任务 1：修复 PlayerAutoNavigator 导航状态机

- [x] 1.1 添加 navToken 字段
  - 添加 `private int _navToken = 0;`
  - 添加 `private int _currentToken = 0;`
  - _Requirements: A-1, A-2_

- [x] 1.2 修改 FollowTarget() 方法
  - 开始时调用 `ForceCancel()` 取消旧导航
  - 递增 `_navToken++`
  - 保存 `_currentToken = _navToken`
  - _Requirements: A-2_

- [x] 1.3 新增 CompleteArrival() 方法
  - 只有 `_currentToken == _navToken` 时才执行回调
  - 替换 `ExecuteNavigation` 和 `MoveDirectly` 中的到达分支
  - _Requirements: A-1_

- [x] 1.4 修改 Cancel() 不执行回调
  - 移除 `if (wasActive && callback != null) callback.Invoke();`
  - 只调用 `ResetNavigationState()`
  - _Requirements: A-1_

- [x] 1.5 修改 ForceCancel() 使 token 失效
  - 添加 `_navToken++;` 使旧回调失效
  - _Requirements: A-2_

- [x] 1.6 新增 ResetNavigationState() 方法
  - 提取公共重置逻辑
  - _Requirements: A-1_

### 任务 2：修复 GameInputManager 交互入口

- [x] 2.1 HandleInteractable 开始新导航前调用 ForceCancel
  - 在 `FollowTarget` 前调用 `autoNavigator.ForceCancel()`
  - _Requirements: A-2, A-3_

- [x] 2.2 TryInteract 添加距离复核
  - 获取玩家位置（Collider 中心）
  - 获取目标锚点（Collider 底部中心）
  - 检查距离 ≤ 交互距离 * 1.2
  - 超出则不执行交互，输出警告（去重）
  - _Requirements: A-1_

- [x] 2.3 新增 GetTargetAnchor() 方法
  - 返回 `(collider.bounds.center.x, bounds.min.y)`
  - 无 Collider 时退化为 `Transform.position`
  - _Requirements: A-1_

- [x] 2.4 HandleMovement 使用 ForceCancel
  - 将 `autoNavigator.Cancel()` 改为 `autoNavigator.ForceCancel()`
  - _Requirements: A-1_

### 任务 3：检查点 - 验证需求 A

- [ ] 3.1 测试：右键远处 A → 导航到 A 后开 A
- [ ] 3.2 测试：关闭 A → 右键远处 B → 导航到 B 后开 B
- [ ] 3.3 测试：导航去 B 途中右键 C → 只开 C
- [ ] 3.4 测试：近距离右键箱子 → 不导航，直接开
- [ ] 3.5 测试：卡顿取消后 → 不打开任何箱子
- [ ] 3.6 测试：玩家手动移动打断 → 不打开任何箱子

---

## P0-2：需求 C - 排序与刷新

### 任务 4：修复 BoxPanelUI 排序

- [x] 4.1 SortDown 使用 InventorySortService
  - 修改 `OnSortDownClicked()` 调用 `InventorySortService.SortInventory()`
  - 与背包 UI 排序逻辑完全一致（全部 36 个槽位）
  - _Requirements: C-1_

- [x] 4.2 确保排序后触发事件
  - 检查 `InventorySortService.SortInventory()` 是否触发 `OnInventoryChanged`
  - 如果没有，手动触发
  - _Requirements: C-3_

- [x] 4.3 确保 BoxPanelUI 订阅事件
  - `OnEnable` 订阅 `_inventoryService.OnInventoryChanged`
  - `OnDisable` 取消订阅
  - _Requirements: C-3_

- [x] 4.4 确保 UI 即时刷新
  - 排序后调用 `RefreshDownSlots()`
  - _Requirements: C-2_

### 任务 5：检查点 - 验证需求 C

- [ ] 5.1 测试：Box UI → SortDown 后 Down 区重排
- [ ] 5.2 测试：SortDown 结果与背包面板一致
- [ ] 5.3 测试：SortUp 重排 Up 区
- [ ] 5.4 测试：操作后 UI 立即刷新

---

## P0-3：需求 D - Held 状态

### 任务 6：统一 Held 图标入口

- [x] 6.1 InventoryInteractionManager 添加公共接口
  - 添加 `public void ShowHeldIcon(int itemId, int amount, Sprite icon)`
  - 添加 `public void HideHeldIcon()`
  - _Requirements: D-2_

- [x] 6.2 InventorySlotInteraction 使用统一入口
  - `ShowDragIcon` 改为调用 `manager.ShowHeldIcon()`
  - `HideDragIcon` 改为调用 `manager.HideHeldIcon()`
  - _Requirements: D-2_

### 任务 7：补全隐藏时机

- [x] 7.1 EndDrag 隐藏
  - 确保 `OnEndDrag` 调用 `HideDragIcon()`
  - _Requirements: D-2a_

- [x] 7.2 Cancel 隐藏
  - 确保 `InventoryInteractionManager.Cancel()` 调用 `heldDisplay?.Hide()`
  - _Requirements: D-2a_

- [x] 7.3 UI 关闭隐藏
  - `BoxPanelUI.Close()` 调用 `manager.HideHeldIcon()`
  - _Requirements: D-2a_

- [x] 7.4 导航开始隐藏
  - `GameInputManager.HandleInteractable()` 开始导航前取消 Held 状态
  - _Requirements: D-2a_

### 任务 8：检查点 - 验证需求 D

- [ ] 8.1 测试：单击仅选中
- [ ] 8.2 测试：长按拖拽进入 Held，图标出现
- [ ] 8.3 测试：图标跟随鼠标
- [ ] 8.4 测试：四象限拖拽全部可用
- [ ] 8.5 测试：丢弃后图标消失
- [ ] 8.6 测试：关闭 UI 后图标消失

---

## P1：需求 B - 日志治理

### 任务 9：清理 BoxPanelUI 日志

- [x] 9.1 删除逐格绑定日志
  - 删除 `[BoxPanelUI] Up[{index}] -> ...` 日志
  - _Requirements: B-4_

- [x] 9.2 添加 showDebugInfo 开关
  - 添加 `[SerializeField] private bool showDebugInfo = false;`
  - 关键日志受开关控制
  - _Requirements: B-3_

### 任务 10：清理 GameInputManager 日志

- [x] 10.1 高频日志改为开关控制
  - `HandleInteractable` 日志受开关控制
  - 默认 `showDebugInfo = false`
  - _Requirements: B-2, B-3_

- [x] 10.2 添加日志去重
  - 同类 Warning/Error 只首次打印
  - 使用 `HashSet<string>` 记录已打印的警告类型
  - _Requirements: B-1_

### 任务 11：清理 InventoryInteractionManager 日志

- [x] 11.1 高频日志改为开关控制
  - 删除或注释高频 `Debug.Log`
  - _Requirements: B-2_

### 任务 12：清理 UIItemIconScaler 日志

- [x] 12.1 UpdateScale 日志改为开关控制
  - 默认不输出
  - _Requirements: B-2_

### 任务 13：检查点 - 验证需求 B

- [ ] 13.1 测试：打开 ≤3 条日志
- [ ] 13.2 测试：点击 ≤2 条日志
- [ ] 13.3 测试：拖拽 ≤3 条日志
- [ ] 13.4 测试：排序 ≤2 条日志
- [ ] 13.5 测试：关闭 ≤1 条日志
- [ ] 13.6 测试：完整流程 ≤15 条日志
- [ ] 13.7 测试：无逐格绑定噪音

---

## 最终验收

- [ ] 14. 全面验收
  - [ ] 14.1 按 requirements_2.md 验收清单逐项验证
  - [ ] 14.2 更新 memory.md

---

## 注意事项

1. **所有 `showDebugInfo` 默认值必须为 `false`**
2. **修改 `Cancel()` 时注意不破坏其他调用方**
3. **排序逻辑以背包 UI 为基准，不要自创规则**
4. **Hotbar 是背包的一部分，排序对全部 36 个槽位执行**
5. **玩家位置 = Collider 中心，目标锚点 = Collider 底部中心**
6. **Warning/Error 需要去重，同类只首次打印**

---

## 完成状态

**代码修改已完成**：
- ✅ P0-1：导航与交互（navToken 机制、CompleteArrival、距离复核）
- ✅ P0-2：排序与刷新（SortDown 使用 InventorySortService）
- ✅ P0-3：Held 状态（统一图标入口、隐藏时机）
- ✅ P1：日志治理（删除高频日志、添加开关控制、警告去重）

**待用户验收**：
- 任务 3：导航与交互测试
- 任务 5：排序与刷新测试
- 任务 8：Held 状态测试
- 任务 13：日志数量测试
