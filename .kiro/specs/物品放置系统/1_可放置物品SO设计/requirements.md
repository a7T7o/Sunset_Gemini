# 需求文档 - 可放置物品 SO 设计

## 介绍

本文档定义可放置物品 ScriptableObject 的设计需求，包括树苗 SO 优化和新增多种可放置物品类型。

## 术语表

- **PlaceableItemData**: 可放置物品数据基类
- **SaplingData**: 树苗数据类（继承 PlaceableItemData）
- **WorkstationData**: 工作台数据类（工作台、熔炉、劈柴架等）
- **StorageData**: 存储容器数据类（箱子等）
- **InteractiveDisplayData**: 交互展示数据类（显示 UI 提示）
- **SimpleEventData**: 简单事件数据类（触发事件）
- **TreeControllerV2**: 树木控制器组件

---

## 需求 1：树苗 SO 优化

**用户故事**: 作为策划，我希望树苗 SO 只需要填写一个预制体，系统自动处理季节样式，减少配置工作量。

### 验收标准

1. WHEN 创建树苗 SO THEN 系统 SHALL 只显示一个 Tree Prefab 字段供用户填写
2. WHEN 放置树苗时 THEN 系统 SHALL 自动根据当前季节选择合适的树苗样式
3. WHEN 当前季节为早夏 THEN 系统 SHALL 随机选择春季或夏季样式并满足预设比例
4. WHEN 当前季节为冬季 THEN 系统 SHALL 阻止放置并输出控制台提示"冬天无法种植树木"
5. WHEN 显示树苗 SO Inspector THEN 系统 SHALL 隐藏通用放置配置字段（Is Placeable、Placement Type、Placement Prefab、Building Size）

---

## 需求 2：可放置物品基类设计

**用户故事**: 作为开发者，我希望有一个统一的可放置物品基类，便于扩展不同类型的可放置物品。

### 验收标准

1. WHEN 创建可放置物品 SO THEN 系统 SHALL 提供 PlaceableItemData 基类
2. WHEN PlaceableItemData 初始化 THEN 系统 SHALL 自动设置 isPlaceable = true
3. WHEN 子类继承 PlaceableItemData THEN 系统 SHALL 自动隐藏父类的放置配置字段

---

## 需求 3：工作台类型（Workstation）

**用户故事**: 作为玩家，我希望放置工作台、熔炉、劈柴架等可制作物品的设施。

### 验收标准

1. WHEN 创建工作台 SO THEN 系统 SHALL 提供 WorkstationData 类型
2. WHEN 配置工作台 SO THEN 系统 SHALL 包含以下字段：
   - 工作台类型枚举（Workbench、Furnace、ChoppingBlock 等）
   - 可制作配方列表引用
   - 制作时间倍率
   - 燃料消耗配置（仅熔炉类）
3. WHEN 玩家交互工作台 THEN 系统 SHALL 打开对应的制作 UI

---

## 需求 4：存储容器类型（Storage）

**用户故事**: 作为玩家，我希望放置箱子等可存储物品的容器。

### 验收标准

1. WHEN 创建存储容器 SO THEN 系统 SHALL 提供 StorageData 类型
2. WHEN 配置存储容器 SO THEN 系统 SHALL 包含以下字段：
   - 存储容量（格子数）
   - 存储类型限制（可选，如只能存放矿石）
   - 是否可上锁
3. WHEN 玩家交互存储容器 THEN 系统 SHALL 打开存储 UI

---

## 需求 5：交互展示类型（Interactive Display）

**用户故事**: 作为玩家，我希望与某些物品交互后显示 UI 提示信息。

### 验收标准

1. WHEN 创建交互展示 SO THEN 系统 SHALL 提供 InteractiveDisplayData 类型
2. WHEN 配置交互展示 SO THEN 系统 SHALL 包含以下字段：
   - 显示标题
   - 显示内容（支持多行）
   - 显示图片（可选）
   - 显示时长（0 = 手动关闭）
3. WHEN 玩家交互 THEN 系统 SHALL 显示配置的 UI 提示

---

## 需求 6：简单事件类型（Simple Event）

**用户故事**: 作为玩家，我希望与某些物品交互后触发简单事件。

### 验收标准

1. WHEN 创建简单事件 SO THEN 系统 SHALL 提供 SimpleEventData 类型
2. WHEN 配置简单事件 SO THEN 系统 SHALL 包含以下字段：
   - 事件类型枚举（播放音效、触发动画、传送、解锁等）
   - 事件参数（根据类型不同）
   - 是否一次性触发
   - 冷却时间
3. WHEN 玩家交互 THEN 系统 SHALL 触发配置的事件

---

## 需求 7：放置类型枚举扩展

**用户故事**: 作为开发者，我希望放置类型枚举能覆盖所有可放置物品类型。

### 验收标准

1. WHEN 定义 PlacementType 枚举 THEN 系统 SHALL 包含以下值：
   - None = 0
   - Sapling = 1（树苗）
   - Decoration = 2（装饰物）
   - Building = 3（建筑）
   - Furniture = 4（家具）
   - Workstation = 5（工作台）
   - Storage = 6（存储容器）
   - InteractiveDisplay = 7（交互展示）
   - SimpleEvent = 8（简单事件）

---

## 需求 8：Create 菜单组织

**用户故事**: 作为策划，我希望在 Unity 编辑器中方便地创建各种可放置物品 SO。

### 验收标准

1. WHEN 右键 Project 窗口 THEN 系统 SHALL 在 Create → Farm → Placeable 下显示所有可放置物品类型
2. WHEN 创建菜单结构 THEN 系统 SHALL 按以下组织：
   ```
   Create → Farm → Placeable →
     ├── Sapling（树苗）
     ├── Workstation（工作台）
     ├── Storage（存储容器）
     ├── Interactive Display（交互展示）
     └── Simple Event（简单事件）
   ```
