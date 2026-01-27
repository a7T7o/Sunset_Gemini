# 配方与制作台系统 - 开发记忆

## 模块概述

配方与制作台系统，从 ui-system 和 so-design-system 中抽离出来的独立模块。管理游戏中所有的制作配方，包括：
- 不同类型的工作台（熔炉、制药台、烧烤架、工作台、烹饪锅）
- 配方的解锁机制（等级解锁、隐藏配方）
- 制作界面的显示逻辑（透明度、排序、条件检查）

## 当前状态

- **完成度**: 95%（核心功能已完成）
- **最后更新**: 2025-12-21
- **状态**: ✅ 核心功能完成，待用户配置配方数据

## 核心设计要点

### 工作台类型
| 工作台 | 枚举值 | 用途 |
|--------|--------|------|
| 熔炉 | Furnace | 烧矿、冶炼金属锭 |
| 制药台 | AlchemyTable | 制作药品 |
| 烧烤架 | Grill | 烤肉、烧烤食物 |
| 工作台 | Workbench | 武器、金属物品、工具 |
| 烹饪锅 | CookingPot | 煮食、汤类 |

### 配方显示规则
1. **已解锁 + 材料充足** → 透明度 1.0，可制作
2. **已解锁 + 材料不足** → 透明度 0.5，不可制作
3. **未解锁但可升级解锁** → 透明度 0.5，显示"需要等级 X"
4. **隐藏配方** → 不加载到 UI

### 配方排序
- 按配方 ID 升序排列

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 工作台专属配方 | 增加游戏深度，不同设施有不同用途 | 2025-12-20 |
| 等级+材料双条件 | 透明度统一表示"条件不足" | 2025-12-20 |
| 隐藏配方不加载 | 减少 UI 复杂度，保持探索感 | 2025-12-20 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Service/Crafting/CraftingService.cs` | 制作服务（已存在） |
| `Assets/Scripts/UI/Crafting/CraftingPanel.cs` | 制作面板（已存在） |
| `Assets/Scripts/UI/Crafting/RecipeSlotUI.cs` | 配方槽位（已存在） |
| `Assets/Scripts/Data/RecipeData.cs` | 配方数据 SO（需扩展） |

## 会话记录

### 会话 1 - 2025-12-20

**用户需求**:
> 把配方和制作台的部分单独抽离出来分出一个独立工作区
> 1、配方的SO需要有明确的工作台区域
> 2、配方处理只显示已解锁和即将升级解锁的配方
> 3、材料足够透明度拉满，不足或等级不足降低透明度

**完成任务**:
1. ✅ 创建 crafting-station-system 工作区
2. ✅ 编写 requirements.md 需求文档
3. ✅ 创建 memory.md 记忆文档

**遗留问题**:
- [x] 编写 design.md 设计文档
- [x] 编写 tasks.md 任务列表
- [ ] 扩展 RecipeData SO 添加工作台字段
- [ ] 更新 CraftingPanel 显示逻辑

---

### 会话 2 - 2025-12-20

**用户需求**:
> 把配方和制作台的部分单独抽离出来分出一个独立工作区
> 配方的SO需要有明确的工作台区域
> 配方处理只显示已解锁和即将升级解锁的配方
> 材料足够透明度拉满，不足或等级不足降低透明度

**完成任务**:
1. ✅ 编写 design.md 设计文档
2. ✅ 编写 tasks.md 任务列表

**设计要点**:
- CraftingStation 枚举扩展（Furnace, AlchemyTable, Grill, CookingPot）
- RecipeData SO 扩展字段（craftingStation, requiredSkillType, requiredSkillLevel, isHiddenRecipe）
- CraftingService 配方过滤逻辑（工作台类型、隐藏配方、排序）
- RecipeSlotUI 透明度逻辑（可制作=1.0, 条件不足=0.5）

**遗留问题**:
- [ ] 开始执行任务列表

---

### 会话 3 - 2025-12-20

**用户需求**:
> 开始实现配方与制作台系统

**完成任务**:
1. ✅ 扩展 CraftingStation 枚举（添加 AlchemyTable、Grill）
2. ✅ 扩展 RecipeData SO 字段（requiredSkillType、requiredSkillLevel、isHiddenRecipe、isUnlocked）
3. ✅ 更新 CraftingService 配方过滤逻辑（工作台类型、隐藏配方、排序）
4. ✅ 更新 CraftingService 解锁检查逻辑（使用 SkillLevelService）
5. ✅ 更新 RecipeSlotUI 透明度逻辑（可制作=1.0, 条件不足=0.5）

**修改文件**:
- `Assets/Scripts/Data/Enums/ItemEnums.cs` - 扩展 CraftingStation 枚举
- `Assets/Scripts/Data/Recipes/RecipeData.cs` - 添加解锁条件字段
- `Assets/Scripts/Service/Crafting/CraftingService.cs` - 更新过滤和解锁逻辑
- `Assets/Scripts/UI/Crafting/RecipeSlotUI.cs` - 更新透明度逻辑

**遗留问题**:
- [ ] 实现配方解锁事件
- [ ] 更新批量配方创建工具
- [ ] 更新现有配方 SO 数据

---

### 会话 4 - 2025-12-20

**用户需求**:
> 继续完成 crafting-station-system 的剩余任务

**完成任务**:
1. ✅ 实现配方解锁事件（OnRecipeUnlocked）
2. ✅ CraftingService 订阅 SkillLevelService.OnLevelUp 事件
3. ✅ CraftingPanel 订阅 OnRecipeUnlocked 事件
4. ✅ 更新批量配方创建工具（添加技能类型、技能等级、隐藏配方字段）
5. ✅ 更新 CraftingPanel.GetStationName() 添加新工作台类型

**修改文件**:
- `Assets/Scripts/Service/Crafting/CraftingService.cs` - 添加 OnRecipeUnlocked 事件、OnSkillLevelUp 回调
- `Assets/Scripts/UI/Crafting/CraftingPanel.cs` - 订阅 OnRecipeUnlocked、添加新工作台名称
- `Assets/Editor/Tool_BatchRecipeCreator.cs` - 添加技能解锁条件字段

**遗留问题**:
- [ ] 更新现有配方 SO 数据（任务 9）
- [ ] 最终测试（任务 10）



---

### 会话 5 - 2025-12-21

**用户需求**:
> 完成12.21任务清单中P0内除了1.4 UI的所有任务

**完成任务**:
1. ✅ 确认 RecipeData 已有 craftingStation 字段（requiredStation）
2. ✅ 确认 RecipeData 已有解锁条件字段（requiredSkillType, requiredSkillLevel, isHiddenRecipe, isUnlocked）
3. ✅ 确认 CraftingService 已实现隐藏配方过滤
4. ✅ 确认 CraftingService 已实现配方排序（按 recipeID 升序）
5. ✅ 确认 CraftingService 已实现 IsRecipeUnlocked 方法
6. ✅ 确认 CraftingService 已订阅 SkillLevelService.OnLevelUp 事件
7. ✅ 确认 RecipeSlotUI 已实现透明度计算（可制作=1.0, 条件不足=0.5）
8. ✅ 确认 RecipeSlotUI 已实现等级要求提示
9. ✅ 确认 RecipeSlotUI 已实现制作按钮禁用
10. ✅ 修复 GetSkillName 使用新的5大类技能枚举

**修改文件**:
- `Assets/Scripts/Service/Crafting/CraftingService.cs` - 修复 GetSkillName 使用新技能枚举
- `Assets/Scripts/UI/Crafting/RecipeSlotUI.cs` - 修复 GetSkillName 使用新技能枚举
- `Assets/Scripts/Data/Recipes/RecipeData.cs` - 修复默认技能类型为 Crafting

**遗留问题**:
- [ ] 用户需要为现有配方 SO 设置工作台类型和解锁条件（任务 9）
- [ ] 属性测试待实现（可选）
