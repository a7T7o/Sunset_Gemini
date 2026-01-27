# 需求文档 - 物品放置系统 V1

## 简介

物品放置系统允许玩家将可放置物品（树苗、装饰物、建筑等）放置到游戏世界中。系统参考星露谷农场的放置机制，提供网格对齐、有效性检测、放置预览等功能。核心目标是实现统一的放置管理器，支持不同类型物品的放置规则，并通过事件系统与其他模块（如树木系统、农田系统）进行解耦通信。

## 术语表

- **PlacementManager**：放置管理器，统一管理所有物品的放置逻辑
- **PlacementPreview**：放置预览组件，显示物品放置位置和有效性
- **SaplingData**：树苗数据 SO，继承自 ItemData，定义树苗特有属性
- **PlacementType**：放置类型枚举，区分不同放置规则（树苗、装饰物、建筑等）
- **PlacementValidator**：放置验证器，检测放置位置是否有效
- **成长边距**：树苗放置时需要预留的空间，来自 StageConfig.stage0 的 verticalMargin 和 horizontalMargin
- **整数坐标**：树苗放置位置必须对齐到整数网格（如 (3, 5) 而非 (3.2, 5.7)）
- **放置事件**：物品放置成功后广播的事件，供其他系统订阅

## 需求

### 需求 1

**用户故事**：作为玩家，我希望能够将手持的可放置物品放置到世界中，以便在农场中种植树木或放置装饰物。

#### 验收标准

1. WHEN 玩家手持可放置物品 THEN PlacementManager SHALL 激活放置模式并显示放置预览
2. WHEN 玩家移动鼠标 THEN PlacementPreview SHALL 跟随鼠标位置更新预览位置
3. WHEN 玩家左键点击有效位置 THEN PlacementManager SHALL 在该位置实例化对应预制体
4. WHEN 玩家左键点击无效位置 THEN PlacementManager SHALL 播放错误提示音效并保持物品在手中
5. WHEN 放置成功 THEN PlacementManager SHALL 从玩家背包中扣除一个该物品
6. WHEN 玩家切换到非放置物品或空手 THEN PlacementManager SHALL 退出放置模式并隐藏预览

### 需求 2

**用户故事**：作为游戏设计师，我希望在 ItemData 中配置物品是否可放置及其放置规则，以便灵活控制不同物品的放置行为。

#### 验收标准

1. WHEN 设计师在 Inspector 中编辑 ItemData THEN 系统 SHALL 显示"是否可放置"开关
2. WHEN 启用可放置 THEN 系统 SHALL 显示放置类型下拉框（树苗、装饰物、建筑等）
3. WHEN 启用可放置 THEN 系统 SHALL 显示放置预制体字段用于指定实例化的预制体
4. WHEN 放置类型为树苗 THEN 系统 SHALL 显示关联的树木预制体字段
5. WHEN 放置类型为建筑 THEN 系统 SHALL 显示建筑尺寸（宽×高）字段

### 需求 3

**用户故事**：作为玩家，我希望放置预览能够清晰显示放置位置是否有效，以便我知道在哪里可以放置物品。

#### 验收标准

1. WHEN 放置位置有效 THEN PlacementPreview SHALL 显示绿色半透明预览
2. WHEN 放置位置无效 THEN PlacementPreview SHALL 显示红色半透明预览
3. WHEN 放置类型为树苗 THEN PlacementPreview SHALL 显示成长边距范围指示器
4. WHEN 放置位置超出玩家交互范围 THEN PlacementPreview SHALL 显示灰色预览表示距离过远
5. WHEN 鼠标悬停在 UI 上 THEN PlacementPreview SHALL 隐藏预览

### 需求 4

**用户故事**：作为游戏系统，我希望树苗放置遵循特殊规则，以便确保树木有足够的成长空间。

#### 验收标准

1. WHEN 放置树苗 THEN PlacementManager SHALL 将放置位置对齐到最近的整数坐标
2. WHEN 检测树苗放置有效性 THEN PlacementValidator SHALL 读取目标树木预制体的 StageConfig[0] 成长边距参数
3. WHEN 成长边距范围内存在带有障碍物标签的 Collider THEN PlacementValidator SHALL 判定位置无效
4. WHEN 成长边距范围内存在其他树木 THEN PlacementValidator SHALL 判定位置无效
5. WHEN 放置位置在水域或不可通行区域 THEN PlacementValidator SHALL 判定位置无效
6. WHEN 树苗放置成功 THEN PlacementManager SHALL 实例化树木预制体并设置为阶段 0（树苗）

### 需求 5

**用户故事**：作为游戏系统，我希望放置成功后广播事件，以便其他系统（如树木管理、成就系统）能够响应放置行为。

#### 验收标准

1. WHEN 任意物品放置成功 THEN PlacementManager SHALL 广播 OnItemPlaced 事件
2. WHEN 树苗放置成功 THEN PlacementManager SHALL 广播 OnSaplingPlanted 事件
3. WHEN 事件广播 THEN 事件数据 SHALL 包含放置位置、物品数据、实例化的 GameObject
4. WHEN TreeControllerV2 接收到 OnSaplingPlanted 事件 THEN TreeControllerV2 SHALL 初始化树木状态

### 需求 6

**用户故事**：作为玩家，我希望放置操作有良好的视觉和音效反馈，以便获得满意的游戏体验。

#### 验收标准

1. WHEN 放置成功 THEN 系统 SHALL 播放放置成功音效
2. WHEN 放置失败 THEN 系统 SHALL 播放放置失败音效
3. WHEN 放置成功 THEN 系统 SHALL 在放置位置播放放置粒子效果
4. WHEN 树苗放置成功 THEN 系统 SHALL 播放种植特效（如泥土飞溅）

### 需求 7

**用户故事**：作为游戏系统，我希望放置系统与现有输入系统集成，以便使用统一的输入管理。

#### 验收标准

1. WHEN 玩家按下左键 THEN GameInputManager SHALL 检测当前是否处于放置模式
2. WHEN 处于放置模式且点击世界空间 THEN GameInputManager SHALL 调用 PlacementManager.TryPlace()
3. WHEN 点击位置在 UI 上 THEN GameInputManager SHALL 忽略放置操作
4. WHEN 玩家按下右键或 ESC THEN PlacementManager SHALL 取消当前放置操作

### 需求 8

**用户故事**：作为游戏设计师，我希望创建专门的树苗数据类型，以便配置树苗特有的属性。

#### 验收标准

1. WHEN 设计师创建新的树苗 SO THEN 系统 SHALL 提供 SaplingData 类型（继承 ItemData）
2. WHEN 编辑 SaplingData THEN 系统 SHALL 显示关联的树木预制体字段
3. WHEN 编辑 SaplingData THEN 系统 SHALL 显示适合种植的季节字段
4. WHEN 在冬季尝试种植树苗 THEN PlacementValidator SHALL 判定位置无效并显示季节提示
5. WHEN SaplingData 验证 THEN 系统 SHALL 检查树木预制体是否包含 TreeControllerV2 组件

### 需求 9

**用户故事**：作为游戏系统，我希望放置系统支持撤销最近的放置操作，以便玩家纠正错误放置。

#### 验收标准

1. WHEN 玩家按下撤销快捷键（如 Ctrl+Z） THEN PlacementManager SHALL 撤销最近一次放置
2. WHEN 撤销放置 THEN PlacementManager SHALL 销毁已放置的 GameObject
3. WHEN 撤销放置 THEN PlacementManager SHALL 将物品返还到玩家背包
4. WHEN 撤销放置 THEN PlacementManager SHALL 播放撤销音效
5. WHEN 放置后超过 5 秒 THEN PlacementManager SHALL 禁止撤销该次放置

### 需求 10

**用户故事**：作为游戏系统，我希望放置系统与农田系统协调，以便在耕地上种植作物时使用农田系统而非放置系统。

#### 验收标准

1. WHEN 玩家手持农作物种子且点击耕地 THEN 系统 SHALL 调用 FarmingSystem 而非 PlacementManager
2. WHEN 玩家手持树苗且点击耕地 THEN PlacementValidator SHALL 判定位置无效
3. WHEN 玩家手持装饰物且点击耕地 THEN PlacementValidator SHALL 判定位置无效
4. WHEN 检测放置位置 THEN PlacementValidator SHALL 查询 FarmingSystem 判断是否为耕地
