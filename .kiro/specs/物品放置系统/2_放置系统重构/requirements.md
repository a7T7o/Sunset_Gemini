# 需求文档 - 物品放置系统重构

## 简介

重构物品放置系统，解决当前实现中的多个问题，并实现完整的预放置 UI 系统。参考星露谷等游戏的放置机制，提供直观的放置预览和反馈。

## 术语表

- **放置系统（Placement System）**：管理物品放置逻辑的核心系统
- **预放置 UI（Placement Preview UI）**：放置前显示的预览界面，包含方框和物品预览
- **放置方框（Placement Grid）**：以格子为单位显示的放置区域指示器
- **Layer**：Unity 的 GameObject Layer，用于物理检测和渲染分层
- **Sorting Layer**：Unity 的渲染排序层
- **Order in Layer**：同一 Sorting Layer 内的渲染顺序
- **Collider**：碰撞体，用于物理检测和放置区域计算

---

## 需求 1：Layer 同步

**用户故事**：作为玩家，我希望种植的树苗能正确显示在当前层级，以便与场景中的其他物体正确交互。

### 验收标准

1. WHEN 玩家在某个 Layer 区域点击放置 THEN 放置系统 SHALL 获取该点击位置的 Layer 并应用到放置物体的所有部分
2. WHEN 树苗被放置 THEN 放置系统 SHALL 将父物体（如 M1）的 Layer 设置为点击位置的 Layer
3. WHEN 树苗被放置 THEN 放置系统 SHALL 将 Tree 子物体的 Layer 同步为父物体的 Layer
4. WHEN 树苗被放置 THEN 放置系统 SHALL 将 Shadow 子物体的 Layer 同步为父物体的 Layer

---

## 需求 2：动态排序（Order）计算

**用户故事**：作为玩家，我希望放置的物品能根据位置正确显示前后关系，以便场景看起来自然。

### 验收标准

1. WHEN 物品被放置 THEN 放置系统 SHALL 根据放置位置的 Y 坐标计算 Order in Layer
2. WHEN 计算 Order THEN 放置系统 SHALL 使用公式：Order = -Round(Y × 100) + offset
3. WHEN 树苗被放置 THEN 放置系统 SHALL 为 Tree 子物体设置计算出的 Order
4. WHEN 树苗被放置 THEN 放置系统 SHALL 为 Shadow 子物体设置 Order = Tree.Order - 1

---

## 需求 3：方块中心放置

**用户故事**：作为玩家，我希望物品放置在方块的中心位置，以便放置结果整齐有序。

### 验收标准

1. WHEN 鼠标移动到某个方块内 THEN 放置系统 SHALL 计算该方块的中心坐标作为放置位置
2. WHEN 计算方块中心 THEN 放置系统 SHALL 使用 Floor(mousePos) + 0.5 作为中心点
3. WHEN 预览显示 THEN 放置系统 SHALL 将预览物品显示在方块中心位置

---

## 需求 4：重复放置检测

**用户故事**：作为玩家，我希望系统阻止在同一位置重复放置物品，以避免物品重叠。

### 验收标准

1. WHEN 玩家尝试在已有物品的位置放置 THEN 放置系统 SHALL 阻止放置并显示红色预览
2. WHEN 检测重复放置 THEN 放置系统 SHALL 使用 Collider 重叠检测而非坐标比较
3. WHEN 检测到碰撞 THEN 放置系统 SHALL 在预览 UI 中标记被占用的格子为红色

---

## 需求 5：Layer 一致性检测

**用户故事**：作为玩家，我希望只能在当前所在层级放置物品，以保持游戏逻辑一致性。

### 验收标准

1. WHEN 玩家尝试放置 THEN 放置系统 SHALL 检测玩家当前所在的 Layer
2. WHEN 放置位置的 Layer 与玩家 Layer 不一致 THEN 放置系统 SHALL 阻止放置
3. IF 玩家在 Layer1 尝试放置到 Layer2 THEN 放置系统 SHALL 显示无效预览并阻止放置

---

## 需求 6：导航到放置点

**用户故事**：作为玩家，我希望在距离放置点较远时能自动导航过去，以便顺利完成放置。

### 验收标准

1. WHEN 放置位置超出玩家交互范围 THEN 放置系统 SHALL 触发玩家导航到放置点附近
2. WHEN 玩家到达放置点附近 THEN 放置系统 SHALL 自动执行放置操作
3. WHEN 导航过程中玩家取消 THEN 放置系统 SHALL 取消导航和放置操作

---

## 需求 7：预放置 UI 系统

**用户故事**：作为玩家，我希望看到清晰的放置预览，包括放置区域和物品预览，以便准确判断放置位置和可行性。

### 验收标准

1. WHEN 进入放置模式 THEN 放置系统 SHALL 显示预放置 UI
2. WHEN 显示预放置 UI THEN 放置系统 SHALL 显示基于物品 Collider 大小的方框
3. WHEN 方框大小计算 THEN 放置系统 SHALL 使用不小于物品 Collider 的格子数量
4. WHEN 位置可放置 THEN 放置系统 SHALL 将方框显示为绿色
5. WHEN 位置被遮挡 THEN 放置系统 SHALL 将被遮挡的格子显示为红色
6. WHEN 显示预览物品 THEN 放置系统 SHALL 将物品以半透明方式显示在方框中心
7. WHEN 方框显示 THEN 放置系统 SHALL 以单位格子（1x1）为最小单位显示

---

## 需求 8：碰撞检测与遮挡显示

**用户故事**：作为玩家，我希望看到哪些区域被遮挡无法放置，以便选择合适的放置位置。

### 验收标准

1. WHEN 预览区域与其他 Collider 重叠 THEN 放置系统 SHALL 检测重叠的格子
2. WHEN 检测到重叠 THEN 放置系统 SHALL 将重叠格子标记为红色
3. WHEN 计算遮挡区域 THEN 放置系统 SHALL 使用不小于遮挡物进入预览区域的格子数量
4. WHEN 所有格子都为绿色 THEN 放置系统 SHALL 允许放置
5. WHEN 存在红色格子 THEN 放置系统 SHALL 阻止放置

