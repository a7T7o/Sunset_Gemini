# 需求文档

## 简介

本文档定义了石头/矿石系统的需求，包括石头的阶段系统、血量继承机制、掉落物系统和材料等级限制。石头系统与树木系统不同，石头不会自然生长，只能被挖掘变小，最终被完全采集。

## 术语表

- **石头阶段（StoneStage）**: 石头的大小阶段，M1（最大）到 M4（最小），均可被挖掘
- **矿物类型（OreType）**: 石头包含的矿物种类，C1=铜矿，C2=铁矿，C3=金矿，无矿物=纯石头
- **矿物含量（OreIndex）**: 石头中矿物的数量指数，0-4，0 表示无矿物
- **材料等级（MaterialTier）**: 工具的材质等级，0=木，1=石，2=生铁，3=黄铜，4=钢，5=金
- **溢出伤害（OverflowDamage）**: 当前阶段血量归零后，多余的伤害继承到下一阶段
- **最终阶段（FinalStage）**: 无法再进入下一阶段的状态，如 M3 和 M4
- **StoneController**: 管理单个石头的所有行为和状态的组件
- **阶段掉落差值**: 每个阶段掉落的矿物数量 = 当前阶段总矿物 - 下一阶段总矿物

## 需求列表

### 需求 1: 石头阶段系统

**用户故事:** 作为玩家，我希望石头有多个大小阶段，以便看到石头被逐渐挖小的过程。

#### 验收标准

1. WHEN 石头初始化为 M1 阶段 THEN StoneController SHALL 显示 M1 对应的 Sprite
2. WHEN M1 阶段石头血量归零 THEN StoneController SHALL 将石头转换为 M2 阶段并掉落差值矿物
3. WHEN M2 阶段石头血量归零 THEN StoneController SHALL 将石头转换为 M3 阶段并掉落差值矿物
4. WHEN M3 阶段石头血量归零 THEN StoneController SHALL 生成最终掉落物并销毁石头
5. WHEN M4 阶段石头血量归零 THEN StoneController SHALL 生成掉落物并销毁石头
6. WHEN 场景中放置任意阶段石头 THEN StoneController SHALL 正确初始化该阶段的状态

### 需求 2: 阶段血量系统

**用户故事:** 作为玩家，我希望不同阶段的石头有不同的血量，并且伤害可以溢出到下一阶段。

#### 验收标准

1. WHEN 石头处于 M1 阶段 THEN StoneController SHALL 将血量设置为 36
2. WHEN 石头处于 M2 阶段 THEN StoneController SHALL 将血量设置为 17
3. WHEN 石头处于 M3 阶段 THEN StoneController SHALL 将血量设置为 9
4. WHEN 石头处于 M4 阶段 THEN StoneController SHALL 将血量设置为 4
5. WHEN 当前阶段血量归零且有溢出伤害 THEN StoneController SHALL 将溢出伤害应用到下一阶段血量
6. WHEN 溢出伤害大于下一阶段血量 THEN StoneController SHALL 继续触发阶段转换直到伤害耗尽或石头销毁

### 需求 3: 矿物含量与阶段转换

**用户故事:** 作为玩家，我希望石头在阶段转换时矿物含量按规则变化，以便获得合理的矿物收益。

#### 验收标准

1. WHEN M1 阶段转换为 M2 阶段 THEN StoneController SHALL 保持相同的矿物含量指数
2. WHEN M2 阶段转换为 M3 阶段 THEN StoneController SHALL 将矿物含量指数减 1（最低为 0）
3. WHEN 矿物含量指数为 0 THEN StoneController SHALL 将石头视为纯石头（无矿物掉落）
4. WHEN M3 阶段石头被挖掘完成 THEN StoneController SHALL 不进入 M4 阶段而是直接销毁

### 需求 4: 掉落物系统（差值掉落）

**用户故事:** 作为玩家，我希望每个阶段挖掘完成时获得该阶段应得的矿物和石料。

#### 验收标准

1. WHEN M1 阶段转换为 M2 阶段 THEN StoneController SHALL 掉落 (M1矿物总量 - M2矿物总量) 个矿物
2. WHEN M2 阶段转换为 M3 阶段 THEN StoneController SHALL 掉落 (M2矿物总量 - M3矿物总量) 个矿物
3. WHEN M3 阶段被完全挖掘 THEN StoneController SHALL 掉落 M3 阶段的全部矿物
4. WHEN 任意阶段石头被挖掘完成 THEN StoneController SHALL 掉落该阶段固定数量的石料
5. WHEN 矿物含量指数为 0 THEN StoneController SHALL 只掉落石料不掉落矿物

### 需求 5: 矿物总量配置表

**用户故事:** 作为开发者，我希望有明确的各阶段矿物总量配置表。

#### 验收标准

##### M1 阶段矿物总量
1. WHEN M1_0 石头 THEN 矿物总量 SHALL 为 0
2. WHEN M1_1 石头 THEN 矿物总量 SHALL 为 3
3. WHEN M1_2 石头 THEN 矿物总量 SHALL 为 5
4. WHEN M1_3 石头 THEN 矿物总量 SHALL 为 7
5. WHEN M1_4 石头 THEN 矿物总量 SHALL 为 9

##### M2 阶段矿物总量
6. WHEN M2_0 石头 THEN 矿物总量 SHALL 为 0
7. WHEN M2_1 石头 THEN 矿物总量 SHALL 为 1
8. WHEN M2_2 石头 THEN 矿物总量 SHALL 为 3
9. WHEN M2_3 石头 THEN 矿物总量 SHALL 为 5
10. WHEN M2_4 石头 THEN 矿物总量 SHALL 为 7

##### M3 阶段矿物总量
11. WHEN M3_0 石头 THEN 矿物总量 SHALL 为 0
12. WHEN M3_1 石头 THEN 矿物总量 SHALL 为 1
13. WHEN M3_2 石头 THEN 矿物总量 SHALL 为 2
14. WHEN M3_3 石头 THEN 矿物总量 SHALL 为 3

#### 阶段转换掉落计算示例

| 起始状态 | 转换后状态 | 矿物掉落数量 | 计算方式 |
|---------|-----------|-------------|---------|
| M1_4 | M2_4 | 2 | 9 - 7 = 2 |
| M1_3 | M2_3 | 2 | 7 - 5 = 2 |
| M1_2 | M2_2 | 2 | 5 - 3 = 2 |
| M1_1 | M2_1 | 2 | 3 - 1 = 2 |
| M2_4 | M3_3 | 4 | 7 - 3 = 4（含量-1） |
| M2_3 | M3_2 | 3 | 5 - 2 = 3（含量-1） |
| M2_2 | M3_1 | 2 | 3 - 1 = 2（含量-1） |
| M2_1 | M3_0 | 1 | 1 - 0 = 1（含量-1） |
| M3_3 | 销毁 | 3 | 最终阶段全部掉落 |
| M3_2 | 销毁 | 2 | 最终阶段全部掉落 |
| M3_1 | 销毁 | 1 | 最终阶段全部掉落 |
| M3_0 | 销毁 | 0 | 无矿物 |

### 需求 6: 石料掉落数量配置

**用户故事:** 作为开发者，我希望有明确的石料掉落数量配置表。

#### 验收标准

1. WHEN M1 阶段转换为 M2 阶段 THEN StoneController SHALL 掉落 6 个石料（12 - 6 = 6）
2. WHEN M2 阶段转换为 M3 阶段 THEN StoneController SHALL 掉落 2 个石料（6 - 4 = 2）
3. WHEN M3 阶段石头被完全挖掘 THEN StoneController SHALL 掉落 4 个石料（最终阶段全部掉落）
4. WHEN M4 阶段石头被完全挖掘 THEN StoneController SHALL 掉落 2 个石料（最终阶段全部掉落）

#### 石料总量配置表

| 阶段 | 石料总量 | 阶段转换掉落 | 说明 |
|------|---------|-------------|------|
| M1 | 12 | 6 | 12 - 6 = 6 |
| M2 | 6 | 2 | 6 - 4 = 2 |
| M3 | 4 | 4 | 最终阶段全部掉落 |
| M4 | 2 | 2 | 最终阶段全部掉落 |

### 需求 7: 经验获取系统

**用户故事:** 作为玩家，我希望挖掘石头获得采集技能经验。

#### 验收标准

1. WHEN 玩家获得 1 个矿物 THEN StoneController SHALL 提供 2 点采集（Gathering）经验
2. WHEN 玩家获得 1 个石料 THEN StoneController SHALL 提供 1 点采集（Gathering）经验
3. WHEN 玩家因工具等级不足只获得石料 THEN StoneController SHALL 只提供石料经验不提供矿物经验
4. WHEN 石头阶段转换完成 THEN StoneController SHALL 调用 SkillLevelService.AddExperience(SkillType.Gathering, xpAmount)

### 需求 8: 材料等级限制（采矿）

**用户故事:** 作为玩家，我希望不同材质的镐子能采集不同等级的矿石。

#### 验收标准

1. WHEN 任何镐子命中任何石头 THEN StoneController SHALL 允许获得石料
2. WHEN 木质镐子命中含矿石头 THEN StoneController SHALL 只允许获得石料不允许获得矿物
3. WHEN 石质镐子命中铁矿(C2)或纯石头 THEN StoneController SHALL 允许获得矿物和石料
4. WHEN 石质镐子命中铜矿(C1)或金矿(C3) THEN StoneController SHALL 只允许获得石料不允许获得矿物
5. WHEN 生铁镐子命中铁矿(C2)或铜矿(C1) THEN StoneController SHALL 允许获得矿物和石料
6. WHEN 生铁镐子命中金矿(C3) THEN StoneController SHALL 只允许获得石料不允许获得矿物
7. WHEN 黄铜镐子命中铁矿(C2)或铜矿(C1) THEN StoneController SHALL 允许获得矿物和石料
8. WHEN 黄铜镐子命中金矿(C3) THEN StoneController SHALL 只允许获得石料不允许获得矿物
9. WHEN 钢质或金质镐子命中任何矿石 THEN StoneController SHALL 允许获得矿物和石料
10. WHEN 工具等级不足以获取矿物 THEN StoneController SHALL 仍然造成伤害并消耗精力

#### 镐子采集能力表

| 镐子材质 | 材料等级 | 可获取矿物 | 说明 |
|---------|---------|-----------|------|
| 木镐 | 0 | 无 | 只能获得石料 |
| 石镐 | 1 | 铁矿(C2) | 铜矿/金矿只能获得石料 |
| 生铁镐 | 2 | 铁矿(C2)、铜矿(C1) | 金矿只能获得石料 |
| 黄铜镐 | 3 | 铁矿(C2)、铜矿(C1) | 金矿只能获得石料 |
| 钢镐 | 4 | 所有矿物 | 可采集金矿(C3) |
| 金镐 | 5 | 所有矿物 | 可采集金矿(C3) |

#### 矿石所需镐子等级

| 矿石类型 | 代码 | 所需最低镐子等级 |
|---------|------|-----------------|
| 纯石头 | - | 木镐 (0) |
| 铁矿 | C2 | 石镐 (1) |
| 铜矿 | C1 | 生铁镐 (2) |
| 金矿 | C3 | 钢镐 (4) |

### 需求 9: Sprite 命名规范（既定规范）

**用户故事:** 作为开发者，我需要按照既定的 Sprite 命名规范进行适配。

#### 验收标准

1. WHEN 加载石头 Sprite THEN StoneController SHALL 使用格式 `Stone_{OreType}_{Stage}_{OreIndex}`
2. WHEN OreType 为 C1 THEN StoneController SHALL 识别为铜矿
3. WHEN OreType 为 C2 THEN StoneController SHALL 识别为铁矿
4. WHEN OreType 为 C3 THEN StoneController SHALL 识别为金矿
5. WHEN Stage 为 M1-M3 THEN StoneController SHALL 识别为可进阶矿石
6. WHEN Stage 为 M4 THEN StoneController SHALL 识别为最终阶段石头（多种样式变体）

#### Sprite 命名示例

| Sprite 名称 | 矿物类型 | 阶段 | 含量指数 | 说明 |
|------------|---------|------|---------|------|
| Stone_C1_M1_4 | 铜矿 | M1 | 4 | 最大铜矿，最高含量 |
| Stone_C2_M2_2 | 铁矿 | M2 | 2 | 中等铁矿，中等含量 |
| Stone_C3_M3_3 | 金矿 | M3 | 3 | 最小金矿，最高含量 |
| Stone_C1_M1_0 | 铜矿 | M1 | 0 | 纯石头（无矿物） |
| Stone_C2_M4_0 | - | M4 | 0 | 装饰石头样式0 |
| Stone_C2_M4_1 | - | M4 | 1 | 装饰石头样式1 |

### 需求 10: 工具交互

**用户故事:** 作为玩家，我希望只有镐子能有效挖掘石头。

#### 验收标准

1. WHEN 镐子命中石头 THEN StoneController SHALL 造成伤害并消耗精力
2. WHEN 非镐子工具命中石头 THEN StoneController SHALL 只播放抖动效果不造成伤害
3. WHEN 镐子命中石头 THEN StoneController SHALL 播放挖掘音效和碎石粒子效果
4. WHEN 石头血量归零 THEN StoneController SHALL 播放破碎音效

### 需求 11: M4 独立石头

**用户故事:** 作为玩家，我希望场景中有小型石头可以快速采集获得石料。

#### 验收标准

1. WHEN M4 石头初始化 THEN StoneController SHALL 将其标记为最终阶段
2. WHEN M4 石头被挖掘完成 THEN StoneController SHALL 掉落 12 个石料
3. WHEN M4 石头配置 THEN StoneController SHALL 支持多种样式变体（M4_0, M4_1, M4_2 等）
4. WHEN M4 石头被挖掘完成 THEN StoneController SHALL 掉落 2 个石料
5. WHEN M4 石头被挖掘完成 THEN StoneController SHALL 提供 2 点采集经验（2个石料×1经验）验）

