# 需求文档

## 简介

本文档定义了树木影子控制系统的需求。该系统将影子控制功能集成到现有的 TreeController 中，实现影子 Sprite 的切换、显示/隐藏控制，以响应树木阶段变化和状态变化（如变成树桩）。

## 术语表

- **TreeController**: 树木控制器，管理树木的成长阶段、状态和显示
- **Shadow**: 树木的影子子物体，位于树木父物体下与 Tree 同级
- **GrowthStage**: 成长阶段枚举（Sapling/Small/Large）
- **TreeState**: 树木状态枚举（Normal/Withered/Frozen/Melted/Stump）
- **影子 Sprite**: 用于显示树木影子的精灵图

## 需求列表

### 需求 1: 影子 Sprite 配置

**用户故事:** 作为开发者，我希望能在 Inspector 中配置不同阶段的影子 Sprite，以便树木在不同成长阶段显示对应的影子。

#### 验收标准

1. WHEN TreeController 在 Inspector 中显示时，系统应提供小树阶段影子 Sprite 配置字段
2. WHEN TreeController 在 Inspector 中显示时，系统应提供大树阶段影子 Sprite 配置字段
3. WHEN 影子 Sprite 字段为空时，系统应保持使用 Shadow 子物体原有的 Sprite
4. WHEN 配置了影子 Sprite 时，系统应在阶段切换时自动应用对应的 Sprite

### 需求 2: 影子显示/隐藏控制

**用户故事:** 作为玩家，我希望树苗和树桩没有影子，只有小树和大树有影子，以便视觉效果更加真实。

#### 验收标准

1. WHEN 树木处于 Sapling（树苗）阶段时，系统应隐藏影子
2. WHEN 树木处于 Small（小树）阶段时，系统应显示影子
3. WHEN 树木处于 Large（大树）阶段时，系统应显示影子
4. WHEN 树木变成 Stump（树桩）状态时，系统应隐藏影子
5. WHEN 树木从树桩恢复为正常状态时，系统应根据当前阶段决定是否显示影子

### 需求 3: 影子 Sprite 切换

**用户故事:** 作为玩家，我希望树木成长时影子也随之变化，以便影子与树木外观匹配。

#### 验收标准

1. WHEN 树木从 Sapling 成长为 Small 时，系统应显示影子并应用小树影子 Sprite
2. WHEN 树木从 Small 成长为 Large 时，系统应切换为大树影子 Sprite
3. WHEN 影子 Sprite 切换时，系统应同时更新影子的缩放比例
4. WHEN 影子 Sprite 切换时，系统应保持影子中心对齐父物体中心（树根位置）

### 需求 4: 影子与树木状态联动

**用户故事:** 作为玩家，我希望树木被砍倒变成树桩时影子消失，以便视觉效果一致。

#### 验收标准

1. WHEN 树木被砍倒变成树桩时，系统应立即隐藏影子
2. WHEN 树木状态从 Normal 变为 Stump 时，系统应禁用影子的 SpriteRenderer
3. WHEN 树木在倒下动画播放期间，系统应保持影子隐藏
4. WHEN 树木状态变化时，系统应在 UpdateSprite() 调用中同步更新影子状态

### 需求 5: 影子缩放配置

**用户故事:** 作为开发者，我希望能配置不同阶段影子的缩放比例，以便影子大小与树木匹配。

#### 验收标准

1. WHEN TreeController 在 Inspector 中显示时，系统应提供小树阶段影子缩放配置（默认0.8）
2. WHEN TreeController 在 Inspector 中显示时，系统应提供大树阶段影子缩放配置（默认1.0）
3. WHEN 影子显示时，系统应根据当前阶段应用对应的缩放值
4. WHEN 缩放值改变时，系统应同时更新 X 和 Y 轴的缩放（保持比例）

