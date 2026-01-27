# Requirements Document

## Introduction

本需求文档定义了 TreeControllerV2 Inspector UI 重构的需求。目标是将当前使用可变长度数组的 UI 交互改为固定展示的方式，使配置更加清晰、直观，并修正数据结构中的错误设计。

## Glossary

- **TreeControllerV2**: 树木控制器脚本，管理树木的6阶段成长系统
- **StageConfig**: 阶段配置类，定义每个成长阶段的属性（成长天数、血量、掉落表等）
- **StageSpriteData**: 阶段 Sprite 数据类，定义每个阶段的各种状态 Sprite
- **ShadowConfig**: 影子配置类，定义每个阶段的影子 Sprite 和缩放
- **Inspector**: Unity 编辑器中显示组件属性的面板
- **CustomEditor**: Unity 自定义编辑器脚本，用于自定义 Inspector 显示

## Requirements

### Requirement 1

**User Story:** As a 开发者, I want StageConfigs 以固定6个阶段的方式展示, so that 我可以直观地看到和编辑每个阶段的配置而不需要手动管理数组大小。

#### Acceptance Criteria

1. WHEN Inspector 显示 StageConfigs 时 THEN TreeControllerV2 Inspector SHALL 以 Stage 0 到 Stage 5 的固定标签展示6个阶段配置
2. WHEN 用户查看 StageConfigs 时 THEN TreeControllerV2 Inspector SHALL 禁止添加或删除阶段配置元素
3. WHEN 用户展开某个阶段配置时 THEN TreeControllerV2 Inspector SHALL 显示该阶段的所有可编辑字段（成长天数、血量、树桩设置、碰撞与遮挡、掉落设置、工具类型）

### Requirement 2

**User Story:** As a 开发者, I want 阶段0-2的树桩设置被移除, so that 数据结构准确反映游戏设计（只有阶段3-5有树桩）。

#### Acceptance Criteria

1. WHEN Inspector 显示阶段0的配置时 THEN TreeControllerV2 Inspector SHALL 隐藏树桩设置字段（hasStump、stumpHealth、stumpDropTable）
2. WHEN Inspector 显示阶段1的配置时 THEN TreeControllerV2 Inspector SHALL 隐藏树桩设置字段
3. WHEN Inspector 显示阶段2的配置时 THEN TreeControllerV2 Inspector SHALL 隐藏树桩设置字段
4. WHEN Inspector 显示阶段3-5的配置时 THEN TreeControllerV2 Inspector SHALL 显示树桩设置字段

### Requirement 3

**User Story:** As a 开发者, I want SpriteConfig 中的 winterMelted 字段被移除, so that 数据结构准确反映游戏设计（冬季阶段0直接死亡，不存在融化状态）。

#### Acceptance Criteria

1. WHEN StageSpriteData 类被定义时 THEN StageSpriteData SHALL 不包含 winterMelted 字段
2. WHEN Inspector 显示任何阶段的 Sprite 配置时 THEN TreeControllerV2 Inspector SHALL 不显示 Winter Melted 选项

### Requirement 4

**User Story:** As a 开发者, I want 阶段0的 SpriteConfig 不显示枯萎和树桩部分, so that UI 只显示该阶段实际需要的配置。

#### Acceptance Criteria

1. WHEN Inspector 显示阶段0的 Sprite 配置时 THEN TreeControllerV2 Inspector SHALL 隐藏枯萎状态字段（hasWitheredState、witheredSummer、witheredFall）
2. WHEN Inspector 显示阶段0的 Sprite 配置时 THEN TreeControllerV2 Inspector SHALL 隐藏树桩状态字段（hasStump、stumpSpringSummer、stumpFall、stumpWinter）

### Requirement 5

**User Story:** As a 开发者, I want ShadowConfigs 以固定5个阶段的方式展示, so that 我可以直观地看到和编辑阶段1-5的影子配置。

#### Acceptance Criteria

1. WHEN Inspector 显示 ShadowConfigs 时 THEN TreeControllerV2 Inspector SHALL 以 Stage 1 到 Stage 5 的固定标签展示5个影子配置
2. WHEN 用户查看 ShadowConfigs 时 THEN TreeControllerV2 Inspector SHALL 禁止添加或删除影子配置元素
3. WHEN 用户展开某个影子配置时 THEN TreeControllerV2 Inspector SHALL 显示 Sprite 和 Scale 字段

### Requirement 6

**User Story:** As a 开发者, I want StageExperience 以固定6个阶段的方式展示, so that 我可以直观地看到和编辑每个阶段的砍伐经验。

#### Acceptance Criteria

1. WHEN Inspector 显示 StageExperience 时 THEN TreeControllerV2 Inspector SHALL 以 Stage 0 到 Stage 5 的固定标签展示6个经验值
2. WHEN 用户查看 StageExperience 时 THEN TreeControllerV2 Inspector SHALL 禁止添加或删除经验值元素

### Requirement 7

**User Story:** As a 开发者, I want 阶段0-2的 SpriteConfig 不显示树桩部分, so that UI 只显示该阶段实际需要的配置。

#### Acceptance Criteria

1. WHEN Inspector 显示阶段1的 Sprite 配置时 THEN TreeControllerV2 Inspector SHALL 隐藏树桩状态字段
2. WHEN Inspector 显示阶段2的 Sprite 配置时 THEN TreeControllerV2 Inspector SHALL 隐藏树桩状态字段
3. WHEN Inspector 显示阶段3-5的 Sprite 配置时 THEN TreeControllerV2 Inspector SHALL 显示树桩状态字段

### Requirement 8

**User Story:** As a 开发者, I want 阶段0的工具类型固定为锄头且不可编辑, so that 数据结构准确反映游戏设计（树苗只能用锄头挖出）。

#### Acceptance Criteria

1. WHEN Inspector 显示阶段0的配置时 THEN TreeControllerV2 Inspector SHALL 将工具类型显示为 Hoe 且设为只读
2. WHEN Inspector 显示阶段1-5的配置时 THEN TreeControllerV2 Inspector SHALL 允许编辑工具类型字段
