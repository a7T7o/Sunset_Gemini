# Requirements Document

## Introduction

本需求文档定义了配方与制作台系统的需求规范。该系统管理游戏中所有的制作配方，包括不同类型的工作台（熔炉、制药台、烧烤架、工作台等），配方的解锁机制，以及制作界面的显示逻辑。

## Glossary

- **RecipeData**: 配方数据 SO，定义制作所需材料、产物和工作台类型
- **CraftingStation**: 制作设施类型枚举，定义配方在哪个工作台制作
- **制作台 (Crafting Station)**: 游戏世界中的可交互物体，玩家靠近后可打开制作界面
- **配方解锁 (Recipe Unlock)**: 配方从隐藏状态变为可见/可制作状态的机制
- **等级解锁 (Level Unlock)**: 通过技能等级提升解锁的配方
- **隐藏配方 (Hidden Recipe)**: 通过特殊方式解锁的配方，不在等级解锁列表中显示
- **RecipeSlotUI**: 配方槽位 UI 预制体，显示单个配方的图标和状态

## Requirements

### Requirement 1: 工作台类型分类

**User Story:** As a 玩家, I want to 在不同的工作台制作不同类型的物品, so that 游戏有合理的制作逻辑和探索感。

#### Acceptance Criteria

1. WHEN 玩家与熔炉交互 THEN CraftingPanel SHALL 仅显示 CraftingStation.Furnace 类型的配方（烧矿、冶炼）
2. WHEN 玩家与制药台交互 THEN CraftingPanel SHALL 仅显示 CraftingStation.AlchemyTable 类型的配方（药品制作）
3. WHEN 玩家与烧烤架交互 THEN CraftingPanel SHALL 仅显示 CraftingStation.Grill 类型的配方（烤肉、烧烤食物）
4. WHEN 玩家与工作台交互 THEN CraftingPanel SHALL 仅显示 CraftingStation.Workbench 类型的配方（武器、金属物品、工具）
5. WHEN 玩家与烹饪锅交互 THEN CraftingPanel SHALL 仅显示 CraftingStation.CookingPot 类型的配方（煮食、汤类）
6. WHEN 配方的 craftingStation 字段与当前工作台不匹配 THEN CraftingPanel SHALL 不显示该配方

### Requirement 2: 配方解锁与显示机制

**User Story:** As a 玩家, I want to 只看到已解锁和即将解锁的配方, so that 界面清晰且有进度感。

#### Acceptance Criteria

1. WHEN 配方已解锁 THEN CraftingPanel SHALL 以正常透明度显示该配方图标
2. WHEN 配方未解锁但可通过升级解锁 THEN CraftingPanel SHALL 以降低透明度显示该配方并标注所需等级
3. WHEN 配方为隐藏配方（非等级解锁）THEN CraftingPanel SHALL 不加载该配方到 UI 列表
4. WHEN 配方解锁状态变化 THEN CraftingPanel SHALL 更新 RecipeData 的 isUnlocked 字段并刷新 UI
5. WHEN 显示配方列表 THEN CraftingPanel SHALL 按配方 ID 升序排列所有配方

### Requirement 3: 材料条件检查

**User Story:** As a 玩家, I want to 清楚看到我是否有足够材料制作, so that 我可以快速判断能否制作。

#### Acceptance Criteria

1. WHEN 玩家背包材料满足配方所有需求 THEN RecipeSlotUI SHALL 以完全不透明（alpha=1.0）显示配方图标
2. WHEN 玩家背包材料不满足任一需求 THEN RecipeSlotUI SHALL 以降低透明度（alpha=0.5）显示配方图标
3. WHEN 配方需要的等级高于玩家当前等级 THEN RecipeSlotUI SHALL 以降低透明度显示并标注"需要等级 X"
4. WHEN 材料不足或等级不足 THEN RecipeSlotUI SHALL 禁用制作按钮

### Requirement 4: RecipeData SO 扩展

**User Story:** As a 开发者, I want to 配方 SO 包含工作台类型和解锁条件, so that 系统能正确过滤和显示配方。

#### Acceptance Criteria

1. WHEN 创建 RecipeData THEN RecipeData SHALL 包含 craftingStation 字段指定所属工作台
2. WHEN 创建 RecipeData THEN RecipeData SHALL 包含 requiredSkillType 字段指定解锁所需的技能类型
3. WHEN 创建 RecipeData THEN RecipeData SHALL 包含 requiredSkillLevel 字段指定解锁所需的技能等级
4. WHEN 创建 RecipeData THEN RecipeData SHALL 包含 isHiddenRecipe 字段标记是否为隐藏配方
5. WHEN 创建 RecipeData THEN RecipeData SHALL 包含 isUnlocked 运行时字段记录解锁状态

### Requirement 5: 配方槽位 UI 预制体

**User Story:** As a 开发者, I want to 有标准化的配方槽位预制体, so that 可以动态生成配方列表。

#### Acceptance Criteria

1. WHEN 实例化 RecipeSlotUI 预制体 THEN RecipeSlotUI SHALL 自动绑定图标、名称、等级提示等 UI 元素
2. WHEN 调用 SetRecipe(RecipeData) THEN RecipeSlotUI SHALL 更新显示内容并计算透明度
3. WHEN 配方可制作 THEN RecipeSlotUI SHALL 响应点击事件并触发制作流程
4. WHEN 配方不可制作 THEN RecipeSlotUI SHALL 显示不可制作的视觉反馈

### Requirement 6: 制作执行

**User Story:** As a 玩家, I want to 点击配方后执行制作, so that 我可以获得制作的物品。

#### Acceptance Criteria

1. WHEN 玩家点击可制作的配方 THEN CraftingService SHALL 从背包扣除所需材料
2. WHEN 材料扣除成功 THEN CraftingService SHALL 将产物添加到玩家背包
3. WHEN 制作完成 THEN CraftingService SHALL 播放制作成功音效和视觉反馈
4. WHEN 背包空间不足 THEN CraftingService SHALL 显示"背包已满"提示并取消制作
5. WHEN 制作完成 THEN CraftingPanel SHALL 刷新所有配方的材料状态显示

