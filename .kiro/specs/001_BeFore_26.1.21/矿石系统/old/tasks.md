# Implementation Plan

- [x] 1. 创建基础数据结构和枚举

  - [x] 1.1 创建 StoneStage 枚举（M1-M4）
    - 在 `Assets/Scripts/Data/Enums/` 目录下创建
    - _Requirements: 1.1-1.6_

  - [x] 1.2 创建 OreType 枚举（None, C1, C2, C3）
    - 在 `Assets/Scripts/Data/Enums/` 目录下创建
    - _Requirements: 9.2-9.4_
  - [x] 1.3 创建 MaterialTier 枚举（Wood-Gold，0-5）
    - 在 `Assets/Scripts/Data/Enums/` 目录下创建
    - _Requirements: 8.1-8.10_

  - [x] 1.4 Write property test for MaterialTier enum values
    - **Property 6: 材料等级限制正确性**
    - **Validates: Requirements 8.1-8.10**

- [x] 2. 创建配置类和工具类
  - [x] 2.1 创建 StoneStageConfig 阶段配置类
    - 包含 health, stoneTotalCount, isFinalStage, nextStage, decreaseOreIndexOnTransition
    - _Requirements: 2.1-2.4_
  - [x] 2.2 创建 DropConfig 静态配置类
    - 包含矿物总量数组和石料总量数组
    - 实现 CalculateOreDropAmount 和 CalculateStoneDropAmount 方法
    - _Requirements: 4.1-4.5, 5.1-5.14, 6.1-6.4_
  - [x] 2.3 Write property test for DropConfig calculations
    - **Property 4: 差值掉落计算正确性**
    - **Validates: Requirements 4.1-4.3**
  - [x] 2.4 创建 MaterialTierHelper 材料等级判定工具类
    - 实现 CanMineOre 和 CanGetStone 方法
    - _Requirements: 8.1-8.10_
  - [ ] 2.5 Write property test for MaterialTierHelper
    - **Property 6: 材料等级限制正确性**
    - **Validates: Requirements 8.1-8.10**

- [x] 3. 更新 ToolData 添加材料等级字段
  - [x] 3.1 在 ToolData 中添加 materialTier 字段
    - 添加 MaterialTier 枚举字段
    - 添加 GetMaterialTierValue() 方法
    - _Requirements: 8.1-8.10_
  - [x] 3.2 在 WeaponData 中添加 materialTier 字段
    - 与 ToolData 保持一致
    - _Requirements: 8.1-8.10_

- [ ] 4. Checkpoint - Make sure all tests are passing
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. 创建 StoneController 核心组件
  - [x] 5.1 创建 StoneController 基础结构
    - 实现 IResourceNode 接口
    - 添加序列化字段：currentStage, oreType, oreIndex, isFinalStage, currentHealth
    - _Requirements: 1.1-1.6, 2.1-2.4_

  - [ ] 5.2 Write property test for stage health initialization
    - **Property 1: 阶段血量初始化一致性**
    - **Validates: Requirements 2.1-2.4**
  - [x] 5.3 实现血量和伤害系统
    - 实现 TakeDamage 方法
    - 实现溢出伤害传递逻辑
    - _Requirements: 2.5-2.6_
  - [ ] 5.4 Write property test for overflow damage
    - **Property 2: 溢出伤害传递正确性**
    - **Validates: Requirements 2.5-2.6**
  - [x] 5.5 实现阶段转换逻辑
    - 实现 HandleStageTransition 方法
    - 处理含量指数变化（M1→M2 保持，M2→M3 减1）
    - _Requirements: 3.1-3.4_
  - [ ] 5.6 Write property test for stage transition
    - **Property 3: 阶段转换含量指数规则**
    - **Validates: Requirements 3.1-3.2**

- [x] 6. 实现掉落系统
  - [x] 6.1 实现矿物掉落逻辑
    - 计算差值掉落数量
    - 检查材料等级限制
    - _Requirements: 4.1-4.5, 5.1-5.14_
  - [x] 6.2 实现石料掉落逻辑
    - 计算差值掉落数量
    - 所有镐子都能获得石料
    - _Requirements: 6.1-6.4_
  - [x] 6.3 Write property test for stone drop amounts
    - **Property 5: 石料掉落数量一致性**
    - **Validates: Requirements 6.1-6.4**
  - [x] 6.4 实现最终阶段掉落逻辑
    - M3 和 M4 的最终掉落
    - _Requirements: 1.4-1.5_
  - [ ] 6.5 Write property test for final stage destruction
    - **Property 8: 最终阶段销毁逻辑**
    - **Validates: Requirements 1.4-1.5, 3.4**

- [x] 7. 实现经验系统
  - [x] 7.1 实现经验计算和发放
    - 矿物 2 经验/个，石料 1 经验/个
    - 调用 SkillLevelService.AddExperience(SkillType.Gathering, xpAmount)
    - _Requirements: 7.1-7.4_
  - [x] 7.2 Write property test for experience calculation
    - **Property 7: 经验计算正确性**
    - **Validates: Requirements 7.1-7.4**





- [ ] 8. Checkpoint - Make sure all tests are passing
  - Ensure all tests pass, ask the user if questions arise.


- [x] 9. 实现工具交互
  - [x] 9.1 实现 CanAccept 方法
    - 只有镐子能有效挖掘
    - _Requirements: 10.1-10.2_
  - [x] 9.2 实现 OnHit 方法
    - 处理命中效果、伤害、精力消耗
    - _Requirements: 10.1-10.4_
  - [x] 9.3 实现材料等级判定
    - 检查镐子等级是否能获取矿物
    - _Requirements: 8.1-8.10_

- [x] 10. 实现 Sprite 系统
  - [x] 10.1 实现 Sprite 命名解析
    - 解析 `Stone_{OreType}_{Stage}_{OreIndex}` 格式
    - _Requirements: 9.1-9.6_
  - [ ] 10.2 Write property test for sprite name parsing
    - **Property 9: Sprite 命名解析正确性**
    - **Validates: Requirements 9.1-9.6**
  - [x] 10.3 实现 UpdateSprite 方法
    - 根据当前阶段和含量更新显示
    - _Requirements: 1.1, 1.6_

- [ ] 11. 实现音效和粒子效果
  - [ ] 11.1 添加音效配置字段
    - 挖掘音效、破碎音效
    - _Requirements: 10.3-10.4_
  - [ ] 11.2 实现粒子效果
    - 碎石粒子效果
    - _Requirements: 10.3_

- [ ] 12. 更新树木系统添加材料等级限制
  - [ ] 12.1 在 TreeControllerV2 中添加材料等级判定
    - 阶段 4 需要生铁斧或更高
    - 阶段 5 需要黄铜斧或更高
    - _Requirements: tree-system 13.1-13.6_
  - [ ] 12.2 实现等级不足时的反馈
    - 播放金属碰撞音效
    - 显示"斧头等级不足"提示
    - _Requirements: tree-system 13.5-13.6_

- [ ] 13. 注册到资源节点系统
  - [ ] 13.1 在 Start 中注册到 ResourceNodeRegistry
    - _Requirements: 10.1_
  - [ ] 13.2 在 OnDestroy 中注销
    - _Requirements: 10.1_

- [ ] 14. Final Checkpoint - Make sure all tests are passing
  - Ensure all tests pass, ask the user if questions arise.

