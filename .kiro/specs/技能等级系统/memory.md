# 技能等级系统 - 开发记忆

## 模块概述

技能等级系统，管理玩家的5种独立技能类别：
- **Combat（战斗）** - 打猎和打怪获取经验
- **Gathering（采集）** - 挖矿、砍树、耕种获取经验
- **Crafting（制作）** - 配方制作、NPC制作获取经验
- **Cooking（烹饪）** - 食材处理加工获取经验
- **Fishing（钓鱼）** - 钓鱼获取经验

技能等级影响配方解锁、工具效率等游戏机制。

## 当前状态

- **完成度**: 95%（核心功能已完成）
- **最后更新**: 2025-12-21
- **状态**: ✅ 核心功能完成

## 会话记录

### 会话 1-7 - 2025-12-20~21（历史记录）

**完成任务**:
1. ✅ 创建 skill-level-system 工作区
2. ✅ 编写 requirements.md 需求文档
3. ✅ 编写 design.md 设计文档
4. ✅ 编写 tasks.md 任务列表
5. ✅ 创建 SkillType 枚举（5大类：Combat/Gathering/Crafting/Cooking/Fishing）
6. ✅ 创建 SkillData 数据结构
7. ✅ 创建 SkillLevelService 服务
8. ✅ 更新 TreeControllerV2 添加经验配置和获取逻辑（使用 Gathering 技能）
9. ✅ CraftingService 集成
10. ✅ 创建 StoneController 经验获取（矿物2经验，石料1经验）

---

### 会话 8 - 2025-12-21

**用户需求**:
> 检查当前完成情况，更新任务清单

**完成任务**:
1. ✅ 确认 SkillType 枚举已更新为5大类
2. ✅ 确认 TreeControllerV2 已使用 Gathering 技能
3. ✅ 确认 StoneController 已创建并使用 Gathering 技能
4. ✅ 更新 tasks.md 标记已完成任务
5. ✅ 更新 memory.md 记录完成状态

**遗留问题**:
- [ ]* 属性测试待实现（可选）
- [ ]* UI 显示待实现（可选）
- [ ]* 数据持久化待实现（可选）

## 核心设计要点

### 技能类型枚举（更新后）
```csharp
public enum SkillType
{
    Combat = 0,     // 战斗（打猎和打怪）
    Gathering = 1,  // 采集（挖矿、砍树、耕种）
    Crafting = 2,   // 制作（配方制作、NPC制作）
    Cooking = 3,    // 烹饪（食材处理加工）
    Fishing = 4     // 钓鱼
}
```

### 采集经验配置（砍树）
| 树木阶段 | 经验值 | 说明 |
|----------|--------|------|
| 阶段 0 | 0 | 树苗，不提供经验 |
| 阶段 1 | 0 | 幼苗，不提供经验 |
| 阶段 2 | 2 | 小树 |
| 阶段 3 | 4 | 中树 |
| 阶段 4 | 6 | 大树 |
| 阶段 5 | 20 | 巨树，高经验奖励 |

### 采集经验配置（挖矿）
| 资源类型 | 经验值 | 说明 |
|----------|--------|------|
| 矿物 | 2/个 | 每获得1个矿物 |
| 石料 | 1/个 | 每获得1个石料 |

### 经验获取渠道
| 技能 | 获取方式 |
|------|----------|
| Combat | 击败敌人、打猎 |
| Gathering | 挖矿、砍树、耕种、收获 |
| Crafting | 配方制作、NPC制作 |
| Cooking | 烹饪、食材加工 |
| Fishing | 钓鱼 |

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 5种独立技能 | 参考星露谷，增加游戏深度 | 2025-12-20 |
| 技能分类调整 | 挖矿和砍树合并为采集技能 | 2025-12-20 |
| 阶段0-1不给经验 | 树苗太小，砍伐无意义 | 2025-12-20 |
| 阶段5给20经验 | 巨树稀有，高奖励激励玩家 | 2025-12-20 |
| 矿物2经验/石料1经验 | 矿物更有价值 | 2025-12-20 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Data/Enums/SkillType.cs` | 技能类型枚举（需更新） |
| `Assets/Scripts/Data/SkillData.cs` | 技能数据结构 |
| `Assets/Scripts/Service/SkillLevelService.cs` | 技能等级服务 |
| `Assets/Scripts/Controller/TreeControllerV2.cs` | 树木控制器 |
| `Assets/Scripts/Controller/StoneController.cs` | 石头控制器（待创建） |

## 会话记录

### 会话 1-4 - 2025-12-20（历史记录）

**完成任务**:
1. ✅ 创建 skill-level-system 工作区
2. ✅ 编写 requirements.md 需求文档
3. ✅ 编写 design.md 设计文档
4. ✅ 编写 tasks.md 任务列表
5. ✅ 创建 SkillType 枚举
6. ✅ 创建 SkillData 数据结构
7. ✅ 创建 SkillLevelService 服务
8. ✅ 更新 TreeControllerV2 添加经验配置和获取逻辑
9. ✅ CraftingService 集成

---

### 会话 5 - 2025-12-20

**用户需求**:
> 技能经验分为五类：战斗（打猎和打怪）、采集（挖矿砍树耕种）、制作（所有使用配方或者在NPC处交付材料进行制作的内容）、烹饪（所有对食材的处理加工都算）、钓鱼

**完成任务**:
1. ✅ 更新 requirements.md 技能分类
2. ✅ 更新 memory.md 记录新的技能分类

**修改文件**:
- `.kiro/specs/skill-level-system/requirements.md` - 更新技能分类为5大类
- `.kiro/specs/skill-level-system/memory.md` - 更新记忆文档

**遗留问题**:
- [ ] 更新 SkillType.cs 枚举（Farming/Mining/Woodcutting → Combat/Gathering/Crafting/Cooking/Fishing）
- [ ] 更新 SkillLevelService.cs 适配新枚举
- [ ] 更新 TreeControllerV2.cs 使用 Gathering 技能
- [ ] 创建 StoneController.cs 使用 Gathering 技能

---

### 会话 6 - 2025-12-21

**用户需求**:
> 更新各工作区的 design.md 和 tasks.md，与已更新的 memory.md 和 requirements.md 同步

**完成任务**:
1. ✅ 更新 design.md 架构图，添加5大技能类别说明
2. ✅ 更新 SkillType 枚举说明（Combat/Gathering/Crafting/Cooking/Fishing）
3. ✅ 添加经验获取渠道汇总表
4. ✅ 更新 TreeController 和 StoneController 经验配置示例
5. ✅ 更新 tasks.md 任务列表

**修改文件**:
- `.kiro/specs/skill-level-system/design.md` - 更新架构和技能分类
- `.kiro/specs/skill-level-system/tasks.md` - 更新任务列表

**遗留问题**:
- [ ] 更新 SkillType.cs 枚举为新的5大类
- [ ] 更新 SkillLevelService.cs 适配新枚举
- [ ] 更新 TreeControllerV2.cs 使用 Gathering 技能
- [ ] 创建 StoneController.cs 使用 Gathering 技能



---

### 会话 7 - 2025-12-21

**用户需求**:
> 完成12.21任务清单中P0内除了1.4 UI的所有任务

**完成任务**:
1. ✅ 更新 SkillType.cs 枚举为新的5大类（Combat/Gathering/Crafting/Cooking/Fishing）
2. ✅ 添加废弃别名（Farming/Mining/Woodcutting → Gathering）保持向后兼容
3. ✅ 更新 SkillData.GetSkillName() 支持新技能类型
4. ✅ 更新 TreeControllerV2.GrantWoodcuttingExperience() 使用 Gathering 技能
5. ✅ 创建 StoneController.cs 使用 Gathering 技能
6. ✅ 修复所有使用废弃 SkillType 的代码

**修改文件**:
- `Assets/Scripts/Data/Enums/SkillType.cs` - 更新为5大类技能
- `Assets/Scripts/Data/SkillData.cs` - 更新技能名称
- `Assets/Scripts/Controller/TreeControllerV2.cs` - 使用 Gathering 技能
- `Assets/Scripts/Controller/StoneController.cs` - 新建，使用 Gathering 技能
- `Assets/Scripts/Service/SkillLevelService.cs` - 更新调试方法
- `Assets/Scripts/Service/Crafting/CraftingService.cs` - 修复废弃调用
- `Assets/Scripts/UI/Crafting/RecipeSlotUI.cs` - 修复废弃调用
- `Assets/Scripts/Data/Recipes/RecipeData.cs` - 修复废弃调用
- `Assets/Editor/Tool_BatchRecipeCreator.cs` - 修复废弃调用

**遗留问题**:
- [ ] 属性测试待实现（可选）
- [ ] UI 显示待实现（可选）
- [ ] 数据持久化待实现（可选）
