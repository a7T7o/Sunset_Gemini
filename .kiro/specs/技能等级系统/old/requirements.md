# Requirements Document

## Introduction

本需求文档定义了技能等级系统的需求规范。该系统管理玩家的5种独立技能类别，每种技能通过不同渠道获取经验并独立升级。技能等级影响配方解锁、工具效率等游戏机制。

## Glossary

- **SkillType**: 技能类型枚举，包含5种技能类别
- **SkillData**: 技能数据结构，存储单个技能的等级和经验值
- **SkillLevelService**: 技能等级服务，管理所有技能的经验获取和等级计算
- **经验值 (Experience/XP)**: 通过游戏行为获取的数值，累积后提升等级
- **等级 (Level)**: 技能的熟练程度，影响配方解锁和效率加成
- **经验曲线 (XP Curve)**: 定义每个等级所需经验值的公式或配置

## Requirements

### Requirement 1: 技能类型定义

**User Story:** As a 玩家, I want to 有多种独立的技能类别, so that 我可以专注发展不同的游戏方向。

#### Acceptance Criteria

1. WHEN 游戏初始化 THEN SkillLevelService SHALL 创建5种独立技能：
   - **Combat（战斗）**: 打猎和打怪
   - **Gathering（采集）**: 挖矿、砍树、耕种
   - **Crafting（制作）**: 所有使用配方或在NPC处交付材料进行制作的内容
   - **Cooking（烹饪）**: 所有对食材的处理加工
   - **Fishing（钓鱼）**: 钓鱼相关活动
2. WHEN 查询技能等级 THEN SkillLevelService SHALL 返回指定技能类型的当前等级
3. WHEN 查询技能经验 THEN SkillLevelService SHALL 返回指定技能类型的当前经验值和升级所需经验
4. WHEN 技能等级变化 THEN SkillLevelService SHALL 触发 OnSkillLevelChanged 事件通知订阅者

### Requirement 2: 经验获取渠道

**User Story:** As a 玩家, I want to 通过对应的游戏行为获取技能经验, so that 我的技能等级反映我的游戏活动。

#### Acceptance Criteria

##### Combat（战斗）经验
1. WHEN 玩家击败敌人 THEN SkillLevelService SHALL 为 Combat 技能增加经验值
2. WHEN 玩家击败猎物 THEN SkillLevelService SHALL 为 Combat 技能增加经验值

##### Gathering（采集）经验
3. WHEN 玩家挖掘矿石获得矿物 THEN SkillLevelService SHALL 为 Gathering 技能增加经验值（每个矿物2点）
4. WHEN 玩家挖掘矿石获得石料 THEN SkillLevelService SHALL 为 Gathering 技能增加经验值（每个石料1点）
5. WHEN 玩家砍伐树木 THEN SkillLevelService SHALL 为 Gathering 技能增加经验值
6. WHEN 玩家收获作物 THEN SkillLevelService SHALL 为 Gathering 技能增加经验值

##### Crafting（制作）经验
7. WHEN 玩家使用配方制作物品 THEN SkillLevelService SHALL 为 Crafting 技能增加经验值
8. WHEN 玩家在NPC处交付材料制作 THEN SkillLevelService SHALL 为 Crafting 技能增加经验值

##### Cooking（烹饪）经验
9. WHEN 玩家烹饪食物 THEN SkillLevelService SHALL 为 Cooking 技能增加经验值
10. WHEN 玩家加工食材 THEN SkillLevelService SHALL 为 Cooking 技能增加经验值

##### Fishing（钓鱼）经验
11. WHEN 玩家钓到鱼 THEN SkillLevelService SHALL 为 Fishing 技能增加经验值

### Requirement 3: 采集经验配置（砍树）

**User Story:** As a 玩家, I want to 砍伐不同阶段的树木获得不同经验, so that 砍大树比砍小树更有价值。

#### Acceptance Criteria

1. WHEN 玩家砍伐阶段0树苗 THEN TreeController SHALL 不提供任何 Gathering 经验
2. WHEN 玩家砍伐阶段1幼苗 THEN TreeController SHALL 不提供任何 Gathering 经验
3. WHEN 玩家砍伐阶段2小树 THEN TreeController SHALL 提供2点 Gathering 经验
4. WHEN 玩家砍伐阶段3中树 THEN TreeController SHALL 提供4点 Gathering 经验
5. WHEN 玩家砍伐阶段4大树 THEN TreeController SHALL 提供6点 Gathering 经验
6. WHEN 玩家砍伐阶段5巨树 THEN TreeController SHALL 提供20点 Gathering 经验

### Requirement 4: 采集经验配置（挖矿）

**User Story:** As a 玩家, I want to 挖掘矿石获得经验, so that 采矿活动有成长感。

#### Acceptance Criteria

1. WHEN 玩家获得1个矿物 THEN StoneController SHALL 提供2点 Gathering 经验
2. WHEN 玩家获得1个石料 THEN StoneController SHALL 提供1点 Gathering 经验
3. WHEN 玩家因工具等级不足只获得石料 THEN StoneController SHALL 只提供石料经验不提供矿物经验
4. WHEN 石头阶段转换完成 THEN StoneController SHALL 调用 SkillLevelService.AddExperience(SkillType.Gathering, xpAmount)

### Requirement 5: 等级计算

**User Story:** As a 玩家, I want to 经验累积后自动升级, so that 我能看到技能成长。

#### Acceptance Criteria

1. WHEN 技能经验达到升级阈值 THEN SkillLevelService SHALL 自动提升该技能等级
2. WHEN 技能升级 THEN SkillLevelService SHALL 将溢出经验保留到下一级
3. WHEN 技能升级 THEN SkillLevelService SHALL 播放升级音效和视觉提示
4. WHEN 技能达到最大等级 THEN SkillLevelService SHALL 停止累积经验

### Requirement 6: 等级影响配方解锁

**User Story:** As a 玩家, I want to 技能等级提升后解锁新配方, so that 升级有实际意义。

#### Acceptance Criteria

1. WHEN 技能等级提升 THEN SkillLevelService SHALL 检查所有配方的解锁条件
2. WHEN 配方的 requiredSkillType 和 requiredSkillLevel 满足 THEN SkillLevelService SHALL 将配方标记为已解锁
3. WHEN 配方解锁 THEN SkillLevelService SHALL 触发 OnRecipeUnlocked 事件
4. WHEN 配方解锁 THEN CraftingPanel SHALL 刷新配方列表显示

### Requirement 7: 技能数据持久化

**User Story:** As a 玩家, I want to 技能等级和经验被保存, so that 下次游戏时保留进度。

#### Acceptance Criteria

1. WHEN 游戏保存 THEN SkillLevelService SHALL 将所有技能的等级和经验写入存档
2. WHEN 游戏加载 THEN SkillLevelService SHALL 从存档恢复所有技能的等级和经验
3. WHEN 存档不存在 THEN SkillLevelService SHALL 将所有技能初始化为等级1、经验0

### Requirement 8: UI 显示

**User Story:** As a 玩家, I want to 在界面上看到技能等级和经验进度, so that 我了解当前状态。

#### Acceptance Criteria

1. WHEN 打开技能面板 THEN SkillPanel SHALL 显示所有5种技能的等级和经验条
2. WHEN 获得经验 THEN SkillPanel SHALL 更新对应技能的经验条动画
3. WHEN 技能升级 THEN SkillPanel SHALL 播放升级动画效果

## 技能类型枚举

```csharp
/// <summary>
/// 技能类型（5大类）
/// </summary>
public enum SkillType
{
    Combat = 0,     // 战斗（打猎和打怪）
    Gathering = 1,  // 采集（挖矿、砍树、耕种）
    Crafting = 2,   // 制作（配方制作、NPC制作）
    Cooking = 3,    // 烹饪（食材处理加工）
    Fishing = 4     // 钓鱼
}
```

## 经验获取汇总表

| 技能类型 | 经验来源 | 说明 |
|---------|---------|------|
| Combat | 击败敌人、打猎 | 战斗相关 |
| Gathering | 挖矿、砍树、耕种、收获 | 资源采集相关 |
| Crafting | 配方制作、NPC制作 | 物品制作相关 |
| Cooking | 烹饪、食材加工 | 食物相关 |
| Fishing | 钓鱼 | 钓鱼相关 |

