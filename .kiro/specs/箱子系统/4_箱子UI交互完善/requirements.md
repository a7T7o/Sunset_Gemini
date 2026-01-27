# 箱子 UI 交互完善 - 需求文档

## 介绍

本任务修复箱子 UI 系统的核心交互问题，确保 Up/Down 区域正确显示、Tab 键逻辑正确、箱子交互范围合理。

## 术语表

- **Up 区域**：箱子 UI 上半部分，显示箱子内容
- **Down 区域**：箱子 UI 下半部分，显示玩家背包内容
- **Hotbar**：快捷栏，索引 0-11，是背包第一行的映射（完全共用）
- **Sprite Bounds**：Sprite 的可见边界，用于交互检测

## 当前问题

### 问题 1：Up 区域不显示箱子内容
- **现象**：控制台输出 `收集到 0 个箱子槽位`
- **根源**：Box UI 预制体的 Up 区域子物体缺少 `InventorySlotUI` 组件
- **影响**：无法显示箱子内容，无法拖拽物品

### 问题 2：Down 区域只显示背包第二行
- **现象**：Down 区域只显示索引 12-35 的物品，第一行（0-11）不显示
- **根源**：`BoxPanelUI.RefreshInventorySlots()` 使用 `startIndex = 12`
- **用户澄清**：
  > Hotbar 是背包第一行的映射，完全共用，这是 UI 最初设计就规定的。
  > Down 应显示完整背包（0-35），当前代码 `startIndex=12` 是错误的。

### 问题 3：关闭箱子后 Tab 无法打开背包
- **现象**：打开箱子 → 按 Tab 关闭 → 再按 Tab 无法打开背包
- **根源**：`CloseBoxUI()` 后 Main/Top 未正确恢复激活状态
- **影响**：用户体验中断

### 问题 4：箱子交互只能点击 Collider
- **现象**：只有点击箱子底部（Collider 区域）才能触发交互
- **根源**：交互检测基于 Collider 而非 Sprite Bounds
- **用户期望**：点击整个箱子 Sprite 都能触发交互

### 问题 5：箱子存储实例确认
- **待确认**：ChestInventory 是否正确工作，每个箱子是否有独立存储

---

## 需求

### 需求 1：Up 区域正确显示箱子内容

**用户故事**：作为玩家，我希望打开箱子时能看到箱子里的物品。

#### 验收标准

1. WHEN 打开箱子时 THEN Up 区域 SHALL 显示箱子内所有物品
2. WHEN 箱子为空时 THEN Up 区域 SHALL 显示空槽位
3. WHEN 物品数量变化时 THEN Up 区域 SHALL 实时更新显示

#### 用户待完成项

- [ ] 为 Box UI 预制体（Box_12/24/36/48）的 Up 区域子物体添加 `InventorySlotUI` 组件

#### 代码待完成项

- [ ] 确认 `BoxPanelUI.BindChestSlotData()` 正确绑定数据
- [ ] 确认 ChestInventory 事件正确触发

---

### 需求 2：Down 区域显示完整背包

**用户故事**：作为玩家，我希望在箱子 UI 中看到我的完整背包，包括快捷栏物品。

#### 验收标准

1. WHEN 打开箱子时 THEN Down 区域 SHALL 显示背包索引 0-35 的所有物品
2. WHEN Hotbar 物品变化时 THEN Down 区域第一行 SHALL 同步更新
3. WHEN 从 Down 区域拖拽物品时 THEN 背包和 Hotbar SHALL 同步更新

#### 代码待完成项

- [ ] 修改 `BoxPanelUI.RefreshInventorySlots()` 的 `startIndex` 从 12 改为 0
- [ ] Down 区域槽位数量应与 UI 设计保持一致（默认 36 格 = 3×12，若实际 UI 设计不同，需在设计文档中更新说明）
- [ ] 说明 Down 区域 0-35 映射与 InventoryPanelUI.Up 的关系（背包镜像视图，不额外改变选择状态）

---

### 需求 3：Tab 键正确切换面板

**用户故事**：作为玩家，我希望关闭箱子后按 Tab 能正常打开背包。

#### 验收标准

1. WHEN 箱子打开时按 Tab THEN 系统 SHALL 关闭箱子 UI
2. WHEN 箱子关闭后按 Tab THEN 系统 SHALL 打开背包面板
3. WHEN 背包打开时按 Tab THEN 系统 SHALL 关闭背包面板

#### 代码待完成项

- [ ] 修复 `PackagePanelTabsUI.CloseBoxUI()` 确保 Main/Top 正确恢复
- [ ] 确认 `ShowMainAndTop()` 被正确调用

---

### 需求 4：箱子交互基于 Sprite Bounds

**用户故事**：作为玩家，我希望点击箱子的任何可见部分都能打开箱子。

#### 验收标准

1. WHEN 点击箱子 Sprite 任意位置时 THEN 系统 SHALL 触发交互
2. WHEN 点击箱子 Sprite 外部时 THEN 系统 SHALL 不触发交互
3. WHEN 箱子被遮挡时 THEN 系统 SHALL 按遮挡规则处理

#### 代码待完成项

- [ ] 修改 `GameInputManager` 或 `ChestController` 的交互检测逻辑
- [ ] 使用 `SpriteRenderer.bounds` 替代 Collider 检测

---

### 需求 5：箱子独立存储验证

**用户故事**：作为玩家，我希望每个箱子有独立的存储空间。

#### 验收标准

1. WHEN 放置多个箱子时 THEN 每个箱子 SHALL 有独立的 ChestInventory 实例
2. WHEN 向箱子 A 放入物品时 THEN 箱子 B 的内容 SHALL 不受影响
3. WHEN 关闭并重新打开箱子时 THEN 箱子内容 SHALL 保持不变

#### 代码待完成项

- [ ] 确认 `ChestController.Initialize()` 正确创建 ChestInventory
- [ ] 添加调试日志验证实例独立性

---

### 需求 6：面板打开时禁用世界输入

**用户故事**：作为玩家，我希望打开箱子或背包时，不会误触世界中的物体。

**用户原话**：
> 无论是箱子面板还是背包面板都是要确保对世界的输入是禁止的，你也要详细记录，而不是省略，虽然已经实现

#### 验收标准

1. WHEN Box UI 打开时 THEN 系统 SHALL 禁用对世界的点击（移动、攻击、工具使用）
2. WHEN PackagePanel 打开时 THEN 系统 SHALL 禁用对世界的点击
3. WHEN 任意面板打开时 THEN 滚轮切换工具 SHALL 被禁用
4. WHEN 任意面板打开时 THEN 数字键切换工具 SHALL 被禁用
5. WHEN 任意面板打开时 THEN Tab/ESC 等面板控制键 SHALL 仍然可用

#### 实现机制（已实现，需验证）

- `GameInputManager.IsAnyPanelOpen` 检查
- `EventSystem.current.IsPointerOverGameObject()` 检查
- `pointerOverUI` 标志

#### 代码待完成项

- [ ] 验证并记录现有实现的完整性
- [ ] 确认 Box UI 打开时与 PackagePanel 共用同一套屏蔽逻辑
- [ ] 验证 Box UI 打开时 `IsPanelOpen()` 返回 true

---

### 需求 7：箱子 Sprite 底部对齐与 Collider/NavGrid 同步

**用户故事**：作为玩家，我希望箱子开合时视觉位置不跳动，且路径不会穿过箱子。

**规范来源**：`maintenance-guidelines.md` 第十一章

#### 验收标准

1. WHEN 箱子开合时 THEN Sprite 底部中心 SHALL 保持在固定锚点
2. WHEN 箱子 Sprite 切换后 THEN PolygonCollider2D SHALL 同步更新形状
3. WHEN Collider 更新后 THEN NavGrid SHALL 刷新阻挡区域
4. WHEN 箱子开合后 THEN 路径规划 SHALL 不穿过箱子

#### 代码待完成项

- [ ] 在 `ChestController` 中实现 `ApplySpriteWithBottomAlign()`
- [ ] 在 `ChestController` 中实现 `UpdateColliderShape()`
- [ ] 在 `SetOpen()` 中调用完整的 Sprite→Collider→NavGrid 链路
- [ ] 确认箱子 Sprite 是否已配置 physicsShape（前置检查）

---

### 需求 8：箱子 UI 与背包 UI 的互斥状态机

**用户故事**：作为玩家，我希望箱子和背包的切换逻辑清晰，不会出现状态混乱。

#### 状态定义

| 状态 | 说明 |
|------|------|
| NoPanel | 无 UI 打开，玩家在世界中 |
| BackpackOpen | 背包面板打开（Main/Top 可见） |
| BoxOpen | 箱子 UI 打开（Main/Top 隐藏，Box UI 可见） |

#### 状态转换矩阵

| 当前状态 | 输入 | 结果状态 | 行为 |
|---------|------|---------|------|
| NoPanel | Tab | BackpackOpen | 打开背包 |
| NoPanel | 点击箱子 | BoxOpen | 打开箱子 UI |
| NoPanel | ESC | NoPanel | 打开设置（可选） |
| BackpackOpen | Tab | NoPanel | 关闭背包 |
| BackpackOpen | 点击箱子 | BoxOpen | 关闭背包，打开箱子 |
| BackpackOpen | ESC | NoPanel | 关闭背包 |
| BoxOpen | Tab | BackpackOpen | 关闭箱子，打开背包 |
| BoxOpen | 点击另一个箱子 | BoxOpen | 切换到新箱子 |
| BoxOpen | ESC | NoPanel | 关闭箱子 |

#### 验收标准

1. WHEN 从 NoPanel 按 Tab THEN 系统 SHALL 打开背包
2. WHEN 从 BackpackOpen 点击箱子 THEN 系统 SHALL 关闭背包并打开箱子
3. WHEN 从 BoxOpen 按 Tab THEN 系统 SHALL 关闭箱子并打开背包
4. WHEN 从 BoxOpen 按 ESC THEN 系统 SHALL 关闭箱子并返回 NoPanel
5. WHEN 从 BoxOpen 按 B/M/L/O THEN 系统 SHALL 关闭箱子并打开对应背包页面

#### 代码待完成项

- [ ] 在 `PackagePanelTabsUI` 中记录进入 Box 模式前的状态
- [ ] 修复 `CloseBoxUI()` 根据进入前状态决定是否恢复背包
- [ ] 在 `OpenPanel()` / `OpenOrToggle(idx)` 开头调用 `CloseBoxPanelIfOpen()`

---

## 优先级

| 需求 | 优先级 | 说明 |
|------|--------|------|
| 需求 1 | P0 | Up 区域是核心功能 |
| 需求 2 | P0 | Down 区域是核心功能 |
| 需求 3 | P0 | Tab 键是基础交互 |
| 需求 4 | P1 | 交互体验优化 |
| 需求 5 | P1 | 数据正确性验证 |
| 需求 6 | P0 | 世界输入禁用（已实现，需验证记录） |
| 需求 7 | P0 | Sprite/Collider/NavGrid 同步 |
| 需求 8 | P0 | 状态机完整性 |

---

## 验收重点（用户明确）

- ✅ 正确显示背包内容
- ✅ 箱子运行时保持存储内容（放进去的东西不会消失）
- ✅ 拖拽交互（箱子↔背包）
- ✅ 箱子和背包分别可 Sort
- ✅ 面板打开时禁用世界输入（已实现）
- ❌ **不包括**持久化存档

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/UI/Box/BoxPanelUI.cs` | 箱子 UI 面板 |
| `Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs` | 面板切换管理 |
| `Assets/YYY_Scripts/World/Placeable/ChestController.cs` | 箱子控制器 |
| `Assets/YYY_Scripts/Service/Inventory/ChestInventory.cs` | 箱子库存类 |
| `Assets/YYY_Scripts/Controller/Input/GameInputManager.cs` | 输入管理器 |
| `Assets/222_Prefabs/UI/Box_*.prefab` | 箱子 UI 预制体 |
