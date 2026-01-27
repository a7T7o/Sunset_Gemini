# Implementation Plan: 配方与制作台系统

## 任务列表

- [x] 1. 扩展 RecipeData SO 字段

  - [x] 1.1 添加 craftingStation 字段（CraftingStation 枚举）
    - 在 RecipeData.cs 中添加工作台类型字段（已有 requiredStation）
    - 默认值为 Workbench
    - _Requirements: 4.1_
  - [x] 1.2 添加解锁条件字段
    - 添加 requiredSkillType (SkillType 枚举)
    - 添加 requiredSkillLevel (int)
    - 添加 isHiddenRecipe (bool)
    - 添加 isUnlocked (运行时状态)
    - _Requirements: 4.2, 4.3, 4.4, 4.5_



- [x] 2. 扩展 CraftingStation 枚举




  - [x] 2.1 添加新的工作台类型



    - Furnace（熔炉）
    - AlchemyTable（制药台）

    - Grill（烧烤架）
    - CookingPot（烹饪锅）

    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 3. 更新 CraftingService 配方过滤逻辑

  - [x] 3.1 实现工作台类型过滤
    - GetAvailableRecipes() 只返回当前工作台的配方
    - _Requirements: 1.6_
  - [x] 3.2 实现隐藏配方过滤
    - 隐藏配方且未解锁 → 不显示
    - _Requirements: 2.3_
  - [x] 3.3 实现配方排序
    - 按配方 ID 升序排列
    - _Requirements: 2.5_
  - [x]* 3.4 编写属性测试：工作台配方过滤正确性
    - **Property 1: 工作台配方过滤正确性**
    - **Validates: Requirements 1.1-1.6**


- [x] 4. 更新 CraftingService 解锁检查逻辑

  - [x] 4.1 实现 IsRecipeUnlocked 方法
    - 检查 unlockedByDefault
    - 检查 isUnlocked 运行时状态
    - 检查技能等级条件
    - _Requirements: 2.1, 2.2_
  - [x] 4.2 添加 SkillLevelService 依赖
    - 注入 SkillLevelService 引用
    - 订阅 OnLevelChanged 事件
    - _Requirements: 5.1, 5.2_
  - [ ]* 4.3 编写属性测试：等级解锁正确性
    - **Property 5: 等级解锁正确性**
    - **Validates: Requirements 2.2, 3.3**

- [x] 5. 更新 RecipeSlotUI 透明度逻辑
  - [x] 5.1 实现透明度计算
    - 可制作 → alpha=1.0
    - 材料不足 → alpha=0.5
    - 等级不足 → alpha=0.5
    - _Requirements: 3.1, 3.2, 3.3_
  - [x] 5.2 实现等级要求提示
    - 显示"需要等级 X"文本
    - _Requirements: 3.3_
  - [x] 5.3 实现制作按钮禁用
    - 条件不足时禁用按钮
    - _Requirements: 3.4_
  - [ ]* 5.4 编写属性测试：透明度与条件一致性
    - **Property 4: 透明度与条件一致性**
    - **Validates: Requirements 3.1, 3.2, 3.3**

- [x] 6. 实现配方解锁事件
  - [x] 6.1 添加 OnRecipeUnlocked 事件
    - 在 CraftingService 中定义事件
    - 等级提升时检查并触发
    - _Requirements: 5.3_
  - [x] 6.2 CraftingPanel 订阅解锁事件
    - 配方解锁时刷新列表
    - _Requirements: 5.4_

- [x] 7. 更新批量配方创建工具
  - [x] 7.1 添加工作台类型选择
    - 在 Tool_BatchRecipeCreator 中添加 CraftingStation 下拉框
    - _Requirements: 4.1_
  - [x] 7.2 添加解锁条件输入
    - 添加技能类型和等级输入
    - 添加隐藏配方勾选框
    - _Requirements: 4.2, 4.3, 4.4_

- [ ] 8. Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户

- [ ] 9. 更新现有配方 SO 数据
  - [ ] 9.1 为现有配方设置工作台类型
    - 根据配方类型分配到对应工作台
    - _Requirements: 1.1-1.5_
  - [ ] 9.2 为现有配方设置解锁条件
    - 设置技能类型和等级要求
    - _Requirements: 4.2, 4.3_

- [ ] 10. Final Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户
