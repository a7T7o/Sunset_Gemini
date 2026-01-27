# 实现计划 - 物品放置系统

- [x] 1. 创建基础数据结构和枚举




  - [x] 1.1 创建 PlacementEnums.cs（PlacementType、PlacementInvalidReason）


    - 定义放置类型枚举和验证失败原因枚举


    - _Requirements: 2.2, 4.3_




  - [ ] 1.2 创建 PlacementEvents.cs（事件数据结构）
    - 定义 PlacementEventData 和 SaplingPlantedEventData




    - _Requirements: 5.1, 5.2, 5.3_
  - [ ] 1.3 扩展 ItemData.cs 添加放置相关字段
    - 添加 isPlaceable、placementType、placementPrefab、buildingSize 字段
    - _Requirements: 2.1, 2.2, 2.3, 2.5_





- [ ] 2. 创建 SaplingData 树苗数据类
  - [ ] 2.1 创建 SaplingData.cs 继承 ItemData
    - 添加 treePrefab、plantableSeason、plantingExp 字段
    - 实现 OnValidate 验证树木预制体




    - _Requirements: 8.1, 8.2, 8.3, 8.5_

- [x] 3. 实现放置验证器

  - [ ] 3.1 创建 PlacementValidator.cs
    - 实现 Validate() 主验证方法
    - 实现 IsValidSaplingPosition() 树苗位置验证
    - 实现 IsWithinPlayerRange() 范围检测
    - 实现 HasObstacleInMargin() 障碍物检测

    - 实现 IsOnFarmland() 耕地检测
    - _Requirements: 4.2, 4.3, 4.4, 4.5, 10.2, 10.3, 10.4_

- [x] 4. 实现放置预览组件

  - [ ] 4.1 创建 PlacementPreview.cs
    - 实现 Show/Hide 显示控制
    - 实现 UpdatePosition() 位置更新
    - 实现 SetValid() 有效性颜色切换



    - 实现 ShowMarginIndicator() 边距指示器

    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_





- [ ] 5. 实现放置管理器核心
  - [x] 5.1 创建 PlacementManager.cs 基础结构




    - 实现单例模式
    - 实现 EnterPlacementMode/ExitPlacementMode
    - 实现放置模式状态管理

    - _Requirements: 1.1, 1.6_
  - [ ] 5.2 实现 TryPlace() 放置执行逻辑
    - 调用验证器检查位置


    - 实例化预制体
    - 扣除背包物品
    - 广播事件
    - _Requirements: 1.3, 1.4, 1.5, 5.1, 5.2_
  - [ ] 5.3 实现树苗放置特殊逻辑
    - 整数坐标对齐
    - 读取 StageConfig[0] 边距参数
    - 设置 TreeControllerV2 为阶段 0
    - _Requirements: 4.1, 4.2, 4.6_
  - [ ] 5.4 实现撤销功能
    - 维护放置历史记录
    - 实现 UndoLastPlacement()
    - 5 秒时间限制
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 6. 集成输入系统
  - [ ] 6.1 修改 GameInputManager 集成放置系统
    - 检测放置模式
    - 左键触发 TryPlace()
    - 右键/ESC 取消放置
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [ ] 7. 集成工具栏系统
  - [ ] 7.1 修改 HotbarSelectionService 检测可放置物品
    - 手持可放置物品时激活放置模式
    - 切换物品时退出放置模式
    - _Requirements: 1.1, 1.6_

- [ ] 8. 添加音效和特效
  - [ ] 8.1 在 PlacementManager 中添加音效播放
    - 放置成功音效
    - 放置失败音效
    - 撤销音效
    - _Requirements: 6.1, 6.2, 9.4_
  - [ ] 8.2 添加放置粒子效果
    - 通用放置特效
    - 树苗种植特效（泥土飞溅）
    - _Requirements: 6.3, 6.4_

- [ ] 9. 最终检查点
  - 确保所有功能正常工作
  - 确保与现有系统兼容
