# Implementation Plan: 技能等级系统

## 任务列表

- [x] 1. 创建基础数据结构

  - [x] 1.1 创建 SkillType 枚举
    - 在 Assets/Scripts/Data/Enums/ 创建 SkillType.cs
    - 定义 Combat, Gathering, Crafting, Cooking, Fishing（5大类）
    - _Requirements: 1.1_
  
  - [x] 1.2 更新 SkillType 枚举为新的5大类
    - 将原有的 Farming/Mining/Woodcutting 合并为 Gathering
    - Combat=0, Gathering=1, Crafting=2, Cooking=3, Fishing=4
    - _Requirements: 1.1_
  - [x] 1.2 创建 SkillData 数据结构
    - 在 Assets/Scripts/Data/ 创建 SkillData.cs




    - 包含 skillType, level, currentExperience 字段
    - 实现 GetExperienceToNextLevel() 方法
    - 实现 GetProgress() 方法

    - _Requirements: 1.2, 1.3_

- [x] 2. 创建 SkillLevelService 服务
  - [x] 2.1 创建服务基础结构

    - 在 Assets/Scripts/Service/ 创建 SkillLevelService.cs
    - 实现单例模式
    - 初始化5种技能数据
    - _Requirements: 1.1_

  - [x] 2.2 实现 AddExperience 方法
    - 添加经验到指定技能
    - 检查并处理升级
    - 触发 OnExperienceGained 事件
    - _Requirements: 2.1-2.5_
  - [x] 2.3 实现等级查询方法
    - GetLevel(skillType)
    - GetExperience(skillType)
    - GetExperienceToNextLevel(skillType)
    - _Requirements: 1.2, 1.3_
  - [x] 2.4 实现升级逻辑




    - 检查经验是否达到阈值
    - 处理溢出经验

    - 触发 OnLevelUp 事件
    - 播放升级音效
    - _Requirements: 4.1, 4.2, 4.3_
  - [ ]* 2.5 编写属性测试：经验累积正确性
    - **Property 1: 经验累积正确性**
    - **Validates: Requirements 2.1-2.5, 4.1**

  - [ ]* 2.6 编写属性测试：等级提升正确性
    - **Property 2: 等级提升正确性**
    - **Validates: Requirements 4.1, 4.2**

- [x] 3. 更新 TreeControllerV2 添加经验获取
  - [x] 3.1 添加经验配置字段
    - 添加 stageExperience 数组 [0, 0, 2, 4, 6, 20]
    - 实现 GetChopExperience() 方法
    - _Requirements: 3.1-3.6_
  - [x] 3.2 更新 ChopDown 方法使用 Gathering 技能
    - 调用 SkillLevelService.AddExperience(SkillType.Gathering, xp)
    - 将原有的 Woodcutting 改为 Gathering
    - 只在树木被砍倒时给予经验
    - _Requirements: 3.1-3.6_
  - [ ]* 3.3 编写属性测试：砍树经验配置正确性
    - **Property 5: 砍树经验配置正确性**
    - **Validates: Requirements 3.1, 3.2**

- [x] 3.5 创建 StoneController 经验获取
  - 矿物 2 经验/个，石料 1 经验/个
  - 调用 SkillLevelService.AddExperience(SkillType.Gathering, xp)
  - _Requirements: 4.1-4.4_

- [ ] 4. Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户

- [x] 5. 与 CraftingService 集成
  - [x] 5.1 添加 SkillLevelService 依赖
    - 在 CraftingService 中注入 SkillLevelService 引用
    - _Requirements: 5.1_
  - [x] 5.2 订阅 OnLevelUp 事件
    - 等级提升时检查配方解锁
    - 触发 OnRecipeUnlocked 事件
    - _Requirements: 5.2, 5.3_

- [ ] 6. 实现数据持久化（可选）
  - [ ]* 6.1 实现保存功能
    - 将技能数据序列化为 JSON
    - 保存到 PlayerPrefs 或文件
    - _Requirements: 6.1_
  - [ ]* 6.2 实现加载功能
    - 从存档恢复技能数据
    - 处理存档不存在的情况
    - _Requirements: 6.2, 6.3_

- [ ] 7. 实现 UI 显示（可选）
  - [ ]* 7.1 创建 SkillPanel UI
    - 显示5种技能的等级和经验条
    - _Requirements: 7.1_
  - [ ]* 7.2 实现经验条动画
    - 获得经验时更新进度条
    - _Requirements: 7.2_
  - [ ]* 7.3 实现升级动画
    - 升级时播放特效
    - _Requirements: 7.3_

- [ ] 8. Final Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户
