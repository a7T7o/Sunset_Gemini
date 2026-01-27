# Implementation Plan

- [x] 1. 修改 StageSpriteData 数据结构




  - [ ] 1.1 从 StageSpriteData 类中移除 winterMelted 字段
    - 编辑 Assets/YYY_Scripts/Data/TreeSpriteData.cs


    - 删除 winterMelted 字段定义
    - _Requirements: 3.1_




  - [ ] 1.2 更新 TreeControllerV2 中使用 winterMelted 的代码
    - 编辑 Assets/YYY_Scripts/Controller/TreeControllerV2.cs

    - 移除 GetWinterSprite 方法中对 winterMelted 的引用
    - _Requirements: 3.1, 3.2_

- [x] 2. 创建 TreeControllerV2Editor 自定义编辑器


  - [ ] 2.1 创建 TreeControllerV2Editor.cs 基础框架
    - 在 Assets/Editor/ 目录创建新文件
    - 实现 OnInspectorGUI 基础结构


    - _Requirements: 1.1, 1.2_


  - [x] 2.2 实现 DrawStageConfigs 方法



    - 绘制固定6个阶段配置（Stage 0-5）
    - 阶段 0-2 隐藏树桩设置字段
    - 阶段 0 工具类型设为只读 Hoe
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 2.4, 8.1, 8.2_
  - [ ] 2.3 实现 DrawSpriteConfig 方法
    - 绘制固定6个阶段的 Sprite 配置
    - 阶段 0 隐藏枯萎和树桩部分
    - 阶段 1-2 隐藏树桩部分
    - _Requirements: 4.1, 4.2, 7.1, 7.2, 7.3_
  - [ ] 2.4 实现 DrawShadowConfigs 方法
    - 绘制固定5个影子配置（Stage 1-5）
    - _Requirements: 5.1, 5.2, 5.3_
  - [ ] 2.5 实现 DrawStageExperience 方法
    - 绘制固定6个经验值（Stage 0-5）
    - _Requirements: 6.1, 6.2_

- [ ] 3. 编译验证
  - 确保所有代码编译通过
  - 在 Unity 中验证 Inspector 显示正确
