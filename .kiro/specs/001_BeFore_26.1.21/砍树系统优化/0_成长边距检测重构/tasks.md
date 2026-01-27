# 实现计划 - 树木成长边距检测重构

- [x] 1. 修改 StageConfig 类，添加边距参数


  - 添加 verticalMargin 字段（上下边距）
  - 添加 horizontalMargin 字段（左右边距）
  - 更新 StageConfigFactory 默认配置
  - _Requirements: 1.1, 1.2, 1.3_



- [x] 2. 修改 TreeControllerV2，添加障碍物标签字段


  - 添加 growthObstacleTags 字符串数组字段


  - 设置默认值为 ["Tree", "Rock", "Building"]
  - _Requirements: 2.1, 2.4_



- [x] 3. 创建 TreeControllerV2Editor，实现 MaskField 下拉框


  - 创建 Editor 脚本
  - 复用 NavGrid2DEditor.DrawTagMask 方法
  - 绘制 growthObstacleTags 的多选下拉框



  - _Requirements: 2.1, 2.2, 2.3_


- [ ] 4. 重写 CanGrowToNextStage 方法
  - 获取下一阶段的边距配置

  - 调用 CheckGrowthMargin 进行检测
  - _Requirements: 3.1_


- [x] 5. 实现 CheckGrowthMargin 方法

  - 获取 Collider 中心点


  - 检测上下左右四个方向
  - 返回是否可以成长

  - _Requirements: 3.2, 3.3, 3.4, 3.5_

- [x] 6. 实现 HasObstacleInDirection 方法


  - 使用 Physics2D.OverlapCircle 检测
  - 过滤指定标签的 Collider
  - 跳过自身和子物体
  - _Requirements: 3.3, 3.4_



- [ ] 7. 删除旧的成长检测相关代码
  - 移除 growthSpaceMultiplier 字段


  - 移除 stageGrowthRadius 数组


  - 移除 CanGrowNearTree 方法
  - 移除 GetStageBounds 方法
  - 移除 CalculateOverlapRatio 方法



  - 移除 CanGrowToNextStageByPhysics 方法
  - 移除 GetMinRootDistance 方法
  - 移除 GetStageRootRadius 方法
  - 移除 GetGrowthCheckRadius 方法
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ] 8. 更新 GrowToNextStage 方法
  - 确保成长后 Collider 边界更新
  - 调用 UpdatePolygonColliderShape
  - _Requirements: 4.2, 4.3_

- [ ] 9. 编译验证和测试
  - 确保所有代码编译通过
  - 在 Unity 中测试成长检测功能
  - _Requirements: 全部_

