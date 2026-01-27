# 需求文档 - 树木成长边距检测重构

## 简介

重构树木成长空间检测系统，从基于 Sprite Bounds 的检测改为基于 Collider 边界的精确边距检测。每个阶段配置独立的上下/左右边距参数，检测时遍历所有指定标签的 Collider，确保边距内无障碍物才允许成长。

## 术语表

- **TreeControllerV2**：树木控制器 V2，管理 6 阶段成长系统
- **StageConfig**：阶段配置类，定义每个成长阶段的属性
- **Collider 边界**：树木 Collider2D 的 bounds 边界（上下左右四个方向）
- **成长边距**：树木成长到下一阶段所需的最小空间（上下边距、左右边距）
- **障碍物标签**：用于检测的 Tag 列表（如 Tree、Rock、Building）
- **MaskField**：Unity 编辑器的多选下拉框控件，支持位掩码选择

## 需求

### 需求 1

**用户故事**：作为游戏设计师，我希望为每个树木阶段配置独立的成长边距参数，以便精确控制树木之间的间距。

#### 验收标准

1. WHEN 设计师在 Inspector 中展开阶段配置 THEN TreeControllerV2 SHALL 显示该阶段的上下边距和左右边距两个参数
2. WHEN 设计师修改边距参数 THEN 系统 SHALL 立即保存到 StageConfig 中
3. WHEN 阶段配置初始化 THEN 系统 SHALL 为每个阶段提供合理的默认边距值

### 需求 2

**用户故事**：作为游戏设计师，我希望使用多选下拉框配置障碍物检测标签，以便灵活指定哪些物体会阻挡树木成长。

#### 验收标准

1. WHEN 设计师在 Inspector 中查看 TreeControllerV2 THEN 系统 SHALL 显示一个 MaskField 风格的多选下拉框用于选择障碍物标签
2. WHEN 设计师点击下拉框 THEN 系统 SHALL 显示所有可用的 Unity Tag 供选择
3. WHEN 设计师选择多个标签 THEN 系统 SHALL 在下拉框中显示已选标签的摘要（如 "Tree, Rock, Building"）
4. WHEN 设计师取消选择某个标签 THEN 系统 SHALL 从检测列表中移除该标签

### 需求 3

**用户故事**：作为游戏系统，我希望基于 Collider 边界进行成长空间检测，以便准确判断树木是否有足够空间成长。

#### 验收标准

1. WHEN 树木尝试成长到下一阶段 THEN 系统 SHALL 获取当前树木 Collider 的边界中心点
2. WHEN 系统检测成长空间 THEN 系统 SHALL 分别检测上、下、左、右四个方向的边距
3. WHEN 检测某个方向 THEN 系统 SHALL 查找该方向上指定边距内是否存在任何带有指定标签的 Collider
4. WHEN 任意方向的边距内存在障碍物 Collider THEN 系统 SHALL 阻止树木成长
5. WHEN 所有方向的边距内均无障碍物 THEN 系统 SHALL 允许树木成长

### 需求 4

**用户故事**：作为游戏系统，我希望成长检测考虑遍历顺序的影响，以便前一棵树长大后能正确影响后续树木的检测。

#### 验收标准

1. WHEN 多棵树木在同一天尝试成长 THEN 系统 SHALL 按顺序逐一处理每棵树的成长检测
2. WHEN 前一棵树成功成长 THEN 系统 SHALL 使用该树成长后的新 Collider 边界进行后续检测
3. WHEN 后一棵树进行成长检测 THEN 系统 SHALL 检测到前一棵树成长后增大的 Collider

### 需求 5

**用户故事**：作为游戏设计师，我希望删除旧的成长空间检测相关参数，以便保持代码整洁。

#### 验收标准

1. WHEN 系统重构完成 THEN TreeControllerV2 SHALL 移除 growthSpaceMultiplier 参数
2. WHEN 系统重构完成 THEN TreeControllerV2 SHALL 移除 stageGrowthRadius 数组
3. WHEN 系统重构完成 THEN TreeControllerV2 SHALL 移除基于 Sprite Bounds 的检测逻辑
4. WHEN 系统重构完成 THEN TreeControllerV2 SHALL 保留 enableGrowthSpaceCheck 开关用于启用/禁用检测

