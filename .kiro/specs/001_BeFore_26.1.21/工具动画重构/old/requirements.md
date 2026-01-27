# 需求文档

## 简介

本文档定义了工具动画系统重构的需求，旨在解决以下问题：
1. 物品 ID 为 -1 时的报错（-1 表示空槽位，应静默处理）
2. 物品 SO 基础属性的重新规划（只有农作物、食物、药品才有 baseQuality）
3. 工具动画系统的简化和统一（使用 itemID + quality 进行动画匹配）
4. 动画控制器连线问题（应只用 Any State → 状态，不需要状态间连线）
5. 动画生成工具的命名问题（正确使用 itemID 命名）

## 术语表

- **ToolData**: 工具数据 ScriptableObject，存储工具的属性和动画配置
- **itemID**: 物品唯一 ID，用于动画状态名命名
- **quality**: 品质等级（0-5），同一工具不同品质使用相同 itemID，通过 quality 区分
- **AnimActionType**: 动画动作类型枚举（Slice/Crush/Pierce/Fish/Watering）
- **LayerAnimSync**: 图层动画同步器，负责玩家与工具动画的帧同步
- **PlayerToolController**: 玩家工具控制器，负责工具切换和动画参数设置
- **ItemStack**: 背包槽位数据结构，itemId=-1 表示空槽位
- **动画状态名格式**: `{Action}_{Direction}_Clip_{itemID}_{quality}`，例如 `Slice_Down_Clip_0_0`

## 需求

### 需求 1：静默处理空槽位

**用户故事:** 作为开发者，我希望系统能静默处理空槽位（itemId=-1），这样控制台不会被不必要的警告淹没。

#### 验收标准

1. 当 ItemDatabase 收到 itemId=-1 的查询时，ItemDatabase 应返回 null 且不输出任何警告
2. 当 AnimatorExtensions 在没有控制器的情况下收到参数设置请求时，AnimatorExtensions 应跳过操作且不输出警告
3. 当 HotbarSelectionService 尝试从空槽位装备时，HotbarSelectionService 应静默跳过装备操作

### 需求 2：品质属性重新规划

**用户故事:** 作为游戏设计师，我希望只有可收获的农作物、食物和药品才有基础品质属性，这样物品系统能准确反映游戏机制。

#### 验收标准

1. 当创建 CropData 时，CropData 应包含 baseQuality 字段用于收获品质判定
2. 当创建 FoodData 时，FoodData 应包含 baseQuality 字段（继承自原料品质）
3. 当创建 PotionData 时，PotionData 应包含 baseQuality 字段用于效果缩放
4. 当创建 ToolData 时，ToolData 不应包含 baseQuality 字段
5. 当创建 WeaponData 时，WeaponData 不应包含 baseQuality 字段
6. 当创建 SeedData 时，SeedData 不应包含 baseQuality 字段

### 需求 3：工具动画配置简化

**用户故事:** 作为开发者，我希望工具动画配置简单且一致，这样添加新工具只需最少的设置。

#### 验收标准

1. 当配置 ToolData 时，ToolData 应使用 itemID 作为动画状态匹配的主要标识符
2. 当构建动画状态名时，系统应遵循格式 `{ActionType}_{Direction}_Clip_{itemID}_{quality}`
3. 当 PlayerToolController 装备工具时，PlayerToolController 应从 ToolData 读取 itemID 并据此缓存状态哈希
4. 当 LayerAnimSync 同步动画时，LayerAnimSync 应使用基于 itemID 和 quality 的缓存状态哈希
5. 当创建 ToolData 时，ToolData 应暴露 animatorController、animationFrameCount 和 animActionType 字段
6. 当同一工具有多个品质版本时，所有品质版本应使用相同的 itemID，通过 quality 参数区分

### 需求 4：动画控制器连线修复

**用户故事:** 作为开发者，我希望动画控制器只使用 Any State 转换，这样控制器图表清晰且工具切换由代码处理。

#### 验收标准

1. 当 ToolAnimationGeneratorTool 创建控制器时，工具应只创建从 Any State 到每个动画状态的转换
2. 当 ToolAnimationGeneratorTool 创建控制器时，工具不应创建动画状态之间的转换
3. 当 LayerAnimSync 切换工具动画时，LayerAnimSync 应直接使用 Animator.Play() 而不是依赖状态转换
4. 当生成控制器时，控制器应包含参数：State、Direction、ToolQuality

### 需求 5：动画生成工具命名修复

**用户故事:** 作为开发者，我希望动画生成工具正确使用 itemID 命名，这样生成的动画符合预期的命名规范。

#### 验收标准

1. 当 ToolAnimationGeneratorTool 生成动画剪辑时，工具应在剪辑名中使用配置的 itemID
2. 当 ToolAnimationGeneratorTool 生成控制器时，工具应使用格式 `{ActionType}_{Direction}_Clip_{itemID}_{quality}` 命名状态
3. 当不同工具共享相同动作类型时，工具应使用不同的 itemID 值以避免命名冲突
4. 当工具扫描现有动画时，工具应正确从文件名解析 itemID

### 需求 6：参数命名一致性

**用户故事:** 作为开发者，我希望动画系统中的参数命名一致，这样所有组件使用相同的参数名。

#### 验收标准

1. 当 PlayerToolController 设置动画器参数时，PlayerToolController 应使用 "ToolQuality" 表示品质值
2. 当 LayerAnimSync 读取动画器参数时，LayerAnimSync 应使用 "ToolQuality" 表示品质值
3. 当 ToolAnimationGeneratorTool 创建控制器参数时，工具应创建 "ToolQuality" 参数
4. 当系统构建状态哈希时，系统应使用 itemID（不是 toolAnimId）作为状态名的 ID 部分


### 需求 7：精力系统实现

**用户故事:** 作为玩家，我希望使用工具时消耗精力，这样游戏有资源管理的策略性。

#### 验收标准

1. 当玩家初始化时，玩家应拥有 150 点最大精力值
2. 当工具操作成功时（砍树、挖矿、浇水、耕种等），系统应消耗对应的精力值
3. 当工具操作失败时（如对空地使用斧头），系统不应消耗精力
4. 当精力值变化时，UI 中的 EP Slider 应同步更新
5. 当精力耗尽时，玩家应无法使用消耗精力的工具

### 需求 8：武器不消耗精力

**用户故事:** 作为玩家，我希望使用武器战斗时不消耗精力，这样战斗体验更流畅。

#### 验收标准

1. 当使用 WeaponData 类型的物品时，系统不应消耗精力
2. 当武器攻击命中敌人时，系统不应消耗精力
3. 当武器攻击未命中时，系统不应消耗精力

### 需求 9：工具可选伤害属性

**用户故事:** 作为游戏设计师，我希望工具可以配置是否造成伤害，这样斧头可以用来攻击敌人。

#### 验收标准

1. 当创建 ToolData 时，ToolData 应包含 canDealDamage 布尔字段
2. 当 canDealDamage 为 true 时，工具应能对敌人造成伤害
3. 当 canDealDamage 为 false 时（如水壶），工具不应对敌人造成伤害
4. 当工具造成伤害时，伤害值应使用 ToolData 中配置的 damageAmount

### 需求 10：工具动画图层统一在玩家上层

**用户故事:** 作为游戏设计师，我希望所有工具动画都显示在玩家角色的上层，这样工具的视觉效果更加突出和一致。

#### 验收标准

1. 当 LayerAnimSync 更新工具可见性时，工具的 sortingOrder 应始终大于玩家的 sortingOrder
2. 当工具处于任何动作状态（Slice/Pierce/Crush/Fish/Watering）时，工具应显示在玩家上层
3. 当工具处于任何方向（Down/Up/Side）时，工具应显示在玩家上层
4. 当工具显示时，工具的 sortingLayerID 应与玩家的 sortingLayerID 保持一致
