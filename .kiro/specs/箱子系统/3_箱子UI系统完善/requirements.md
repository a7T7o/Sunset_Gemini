# 箱子 UI 系统完善 - 需求文档

## 介绍

本任务专注于完善箱子系统的 UI 需求（原需求 8-10），解决 Code Reaper 锐评中指出的严重不达标问题。核心目标是实现箱子 UI 与背包面板的完整互斥逻辑、不同容量箱子的 UI 适配、以及箱子内容的事件绑定。

## 术语表

- **BoxPanelUI**：箱子交互 UI 面板，显示箱子格子和背包格子两个区域
- **PackagePanelTabsUI**：背包面板切换管理器，管理道具/配方/地图等页面
- **ChestInventory**：箱子库存类（待实现），参考 InventoryService 设计，提供事件绑定
- **互斥显示**：BoxPanelUI 和 PackagePanel 同时只能显示一个

## 当前问题

根据 Code Reaper 锐评（`code-reaper-reviews/review-session10-chest-system-critique.md`）：

1. **需求 8（自动导航+打开UI）**：部分实现，待确认
2. **需求 9（互斥显示）**：❌ 严重不达标
   - `BoxPanelUI.ClosePackagePanel()` 只是单向关闭
   - PackagePanel 打开时没有关闭 BoxPanelUI 的逻辑
   - ESC 键关闭 BoxPanelUI 未实现
   - 背包键切换逻辑未实现
3. **需求 10（UI 布局）**：❌ 严重不达标
   - 没有根据不同容量加载不同预制体
   - `_contents` 只是 `List<ItemStack>`，没有事件绑定

## 需求

### 需求 1：面板互斥逻辑

**用户故事**：作为玩家，我希望箱子 UI 与背包面板互斥显示，以便界面清晰不混乱。

#### 验收标准

1. WHEN BoxPanelUI 打开时 THEN 系统 SHALL 确保 PackagePanel 处于隐藏状态
2. WHEN PackagePanel 打开时 THEN 系统 SHALL 确保 BoxPanelUI 处于隐藏状态
3. WHEN 玩家按下 ESC 键且 BoxPanelUI 打开时 THEN 系统 SHALL 关闭 BoxPanelUI
4. WHEN 玩家按下背包快捷键（Tab/B/M等）且 BoxPanelUI 打开时 THEN 系统 SHALL 关闭 BoxPanelUI 并打开对应的 PackagePanel 页面

### 需求 2：ChestInventory 事件系统

**用户故事**：作为开发者，我希望箱子内容有事件绑定，以便 UI 能够响应内容变化。

#### 验收标准

1. WHEN 箱子槽位内容变化时 THEN 系统 SHALL 触发 OnSlotChanged 事件
2. WHEN 箱子整体内容变化时 THEN 系统 SHALL 触发 OnInventoryChanged 事件
3. WHEN BoxPanelUI 打开时 THEN 系统 SHALL 订阅 ChestInventory 的事件
4. WHEN BoxPanelUI 关闭时 THEN 系统 SHALL 取消订阅 ChestInventory 的事件

### 需求 3：不同容量 UI 适配

**用户故事**：作为玩家，我希望不同容量的箱子显示不同的 UI 布局，以便清晰地看到所有格子。

#### 验收标准

1. WHEN 12 格箱子（1×12）打开时 THEN 系统 SHALL 显示 1 行 12 列的格子布局
2. WHEN 24 格箱子（2×12）打开时 THEN 系统 SHALL 显示 2 行 12 列的格子布局
3. WHEN 36 格箱子（3×12）打开时 THEN 系统 SHALL 显示 3 行 12 列的格子布局
4. WHEN 48 格箱子（4×12）打开时 THEN 系统 SHALL 显示 4 行 12 列的格子布局

### 需求 4：物品拖拽转移

**用户故事**：作为玩家，我希望能够在箱子和背包之间拖拽物品，以便方便地转移物品。

#### 验收标准

1. WHEN 玩家从箱子槽位拖拽物品到背包槽位时 THEN 系统 SHALL 将物品从箱子移动到背包
2. WHEN 玩家从背包槽位拖拽物品到箱子槽位时 THEN 系统 SHALL 将物品从背包移动到箱子
3. WHEN 拖拽目标槽位有相同物品时 THEN 系统 SHALL 尝试合并堆叠
4. WHEN 拖拽目标槽位有不同物品时 THEN 系统 SHALL 交换两个槽位的物品

## 优先级

| 需求 | 优先级 | 说明 |
|------|--------|------|
| 需求 1 | P0 | 互斥逻辑是基础，必须先实现 |
| 需求 2 | P0 | 事件系统是 UI 绑定的基础 |
| 需求 3 | P1 | 布局适配可以后续优化 |
| 需求 4 | P1 | 拖拽转移依赖前两个需求 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/UI/Box/BoxPanelUI.cs` | 箱子 UI 面板 |
| `Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs` | 背包面板切换管理 |
| `Assets/YYY_Scripts/World/Placeable/ChestController.cs` | 箱子控制器 |
| `Assets/YYY_Scripts/Service/Inventory/InventoryService.cs` | 背包服务（参考） |
