# Requirements Document

## Introduction

本需求文档定义了 UI 系统中制作台功能的需求规范。制作台是游戏中玩家制作物品的核心界面，需要显示可用配方、检查材料、执行制作等功能。

## Glossary

- **制作台 (Crafting Panel)**: 显示配方列表和制作功能的 UI 面板
- **RecipeData**: 配方数据 SO，定义制作所需材料和产物
- **CraftingStation**: 制作设施类型枚举（烹饪锅、熔炉、工作台等）
- **InventoryService**: 背包服务，管理玩家物品存储
- **ItemDatabase**: 物品数据库，提供物品和配方数据查询
- **CraftingController**: 制作控制器，处理制作逻辑和 UI 交互

## Requirements

### Requirement 1: 制作台 UI 控制器

**User Story:** As a 玩家, I want to 在制作台界面查看所有可用配方, so that 我可以了解能制作哪些物品。

#### Acceptance Criteria

1. WHEN 玩家打开制作台面板 THEN CraftingController SHALL 从 ItemDatabase 获取当前设施类型的所有配方
2. WHEN 配方列表加载完成 THEN CraftingController SHALL 在 UI 中显示配方名称、图标和所需材料
3. WHEN 玩家选择某个配方 THEN CraftingController SHALL 高亮显示该配方并展示详细信息
4. WHEN 显示配方详情 THEN CraftingController SHALL 显示产物图标、名称、所需材料列表及玩家当前持有数量

### Requirement 2: 材料检查功能

**User Story:** As a 玩家, I want to 看到我是否有足够材料制作某个配方, so that 我可以决定是否进行制作。

#### Acceptance Criteria

1. WHEN 显示配方材料列表 THEN CraftingController SHALL 查询 InventoryService 获取玩家持有的材料数量
2. WHEN 材料数量足够 THEN CraftingController SHALL 以绿色显示该材料项
3. WHEN 材料数量不足 THEN CraftingController SHALL 以红色显示该材料项并标注缺少数量
4. WHEN 所有材料都足够 THEN CraftingController SHALL 启用制作按钮
5. WHEN 任一材料不足 THEN CraftingController SHALL 禁用制作按钮并显示提示

### Requirement 3: 制作执行功能

**User Story:** As a 玩家, I want to 点击制作按钮完成物品制作, so that 我可以获得制作的物品。

#### Acceptance Criteria

1. WHEN 玩家点击制作按钮且材料充足 THEN CraftingController SHALL 从背包扣除所需材料
2. WHEN 材料扣除成功 THEN CraftingController SHALL 将产物添加到玩家背包
3. WHEN 制作完成 THEN CraftingController SHALL 播放制作成功音效和视觉反馈
4. WHEN 背包空间不足 THEN CraftingController SHALL 显示"背包已满"提示并取消制作
5. WHEN 制作完成 THEN CraftingController SHALL 刷新材料显示状态

### Requirement 4: 制作台设施类型

**User Story:** As a 玩家, I want to 不同制作设施显示不同的配方, so that 我知道在哪里制作什么物品。

#### Acceptance Criteria

1. WHEN 玩家与烹饪锅交互 THEN CraftingController SHALL 仅显示 CraftingStation.CookingPot 的配方
2. WHEN 玩家与熔炉交互 THEN CraftingController SHALL 仅显示 CraftingStation.Furnace 的配方
3. WHEN 玩家与工作台交互 THEN CraftingController SHALL 仅显示 CraftingStation.Workbench 的配方
4. WHEN 配方需要特定设施 THEN CraftingController SHALL 在其他设施中不显示该配方

### Requirement 5: 配方解锁系统

**User Story:** As a 玩家, I want to 只看到已解锁的配方, so that 游戏有进度感和探索感。

#### Acceptance Criteria

1. WHEN 配方 unlockedByDefault 为 true THEN CraftingController SHALL 默认显示该配方
2. WHEN 配方 unlockedByDefault 为 false THEN CraftingController SHALL 检查玩家是否满足解锁条件
3. WHEN 配方未解锁 THEN CraftingController SHALL 以灰色或锁定图标显示该配方
4. WHEN 玩家等级不足 THEN CraftingController SHALL 显示"需要等级 X"提示

