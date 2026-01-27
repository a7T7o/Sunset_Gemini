# 需求文档 - 放置系统与背包联动

## 简介

本需求文档定义放置系统与背包系统的联动功能，确保放置成功后正确扣除背包物品，并在放置过程中正确处理各种中断情况。

## 术语表

- **PlacementManagerV3**: 放置管理器 V3 版本，负责物品放置的核心逻辑
- **InventoryService**: 背包服务，管理背包数据和物品操作
- **HotbarSelectionService**: 快捷栏选择服务，管理当前手持物品
- **PlacementSnapshot**: 放置快照，记录点击锁定时的物品信息
- **中断条件**: 导致放置过程取消的各种输入或事件

## 需求

### 需求 1

**用户故事**: 作为玩家，我希望放置物品成功后背包中的物品数量自动减少，这样我不需要手动管理物品数量。

#### 验收标准

1. WHEN 放置操作成功执行（物品实例化完成）THEN PlacementManagerV3 SHALL 调用 InventoryService.RemoveFromSlot 扣除 1 个物品
2. WHEN 放置操作失败（实例化失败或验证失败）THEN PlacementManagerV3 SHALL 保持背包物品数量不变
3. WHEN 扣除后槽位物品数量变为 0 THEN PlacementManagerV3 SHALL 自动退出放置模式

### 需求 2

**用户故事**: 作为玩家，我希望在点击确认放置位置时系统记录当前物品信息，这样即使后续发生切换也能正确处理。

#### 验收标准

1. WHEN 玩家左键点击且所有格子有效 THEN PlacementManagerV3 SHALL 创建 PlacementSnapshot 记录 itemId、quality、slotIndex
2. WHEN 放置执行时 THEN PlacementManagerV3 SHALL 使用 PlacementSnapshot 中的数据而非当前手持物品数据
3. WHEN PlacementSnapshot 中的 slotIndex 对应的物品已变化 THEN PlacementManagerV3 SHALL 取消放置并返回 Preview 状态

### 需求 3

**用户故事**: 作为玩家，我希望在放置过程中切换手持物品时自动取消当前放置，这样不会出现数据错乱。

#### 验收标准

1. WHEN 处于 Locked 或 Navigating 状态且 HotbarSelectionService.OnSelectedChanged 触发 THEN PlacementManagerV3 SHALL 取消导航并返回 Idle 状态
2. WHEN 手持物品切换后 THEN PlacementManagerV3 SHALL 清除 PlacementSnapshot
3. WHEN 切换到非可放置物品 THEN PlacementManagerV3 SHALL 退出放置模式

### 需求 4

**用户故事**: 作为玩家，我希望打开背包时自动取消当前放置过程，这样可以安全地管理物品。

#### 验收标准

1. WHEN 处于 Locked 或 Navigating 状态且背包面板打开 THEN PlacementManagerV3 SHALL 取消导航并返回 Idle 状态
2. WHEN 背包面板关闭后 THEN PlacementManagerV3 SHALL 允许重新进入放置模式（如果手持可放置物品）

### 需求 5

**用户故事**: 作为玩家，我希望在放置过程中再次左键点击时重新开始放置流程，而不是叠加多个放置任务。

#### 验收标准

1. WHEN 处于 Navigating 状态且玩家再次左键点击有效位置 THEN PlacementManagerV3 SHALL 取消当前导航并重新锁定新位置
2. WHEN 处于 Navigating 状态且玩家再次左键点击无效位置 THEN PlacementManagerV3 SHALL 忽略该点击
3. WHEN 重新锁定位置时 THEN PlacementManagerV3 SHALL 更新 PlacementSnapshot 为新位置对应的物品数据

### 需求 6

**用户故事**: 作为玩家，我希望在放置过程中受到攻击时自动取消放置，这样可以专注于战斗。

#### 验收标准

1. WHEN 处于 Locked 或 Navigating 状态且玩家受击 THEN PlacementManagerV3 SHALL 取消导航并返回 Preview 状态
2. WHEN 受击中断后 THEN PlacementManagerV3 SHALL 保持放置模式（如果手持物品未变化）

### 需求 7

**用户故事**: 作为玩家，我希望在放置过程中进行其他交互时自动取消放置，这样交互不会冲突。

#### 验收标准

1. WHEN 处于 Locked 或 Navigating 状态且玩家开始新的自动导航 THEN PlacementManagerV3 SHALL 取消当前放置导航
2. WHEN 处于 Locked 或 Navigating 状态且玩家进行 NPC 交互 THEN PlacementManagerV3 SHALL 取消放置并返回 Preview 状态
3. WHEN 处于 Locked 或 Navigating 状态且玩家使用工具 THEN PlacementManagerV3 SHALL 取消放置并返回 Preview 状态

### 需求 8

**用户故事**: 作为玩家，我希望放置成功后如果还有剩余物品能继续放置，这样可以连续放置多个物品。

#### 验收标准

1. WHEN 放置成功且槽位剩余数量 > 0 THEN PlacementManagerV3 SHALL 返回 Preview 状态继续放置
2. WHEN 放置成功且槽位剩余数量 = 0 THEN PlacementManagerV3 SHALL 退出放置模式
3. WHEN 继续放置时 THEN PlacementManagerV3 SHALL 解锁预览位置并跟随鼠标

