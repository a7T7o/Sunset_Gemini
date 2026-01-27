# 实现计划 - StoneController 优化

- [x] 1. 创建 StoneControllerEditor 自定义编辑器

  - [x] 1.1 创建 StoneControllerEditor.cs 基础框架
    - 创建 Assets/Editor/StoneControllerEditor.cs
    - 添加 [CustomEditor(typeof(StoneController))] 特性
    - 定义折叠状态变量和阶段名称数组
    - _Requirements: 1.1_

  - [x] 1.2 实现阶段配置分组绘制
    - 实现 DrawStageConfigs() 方法
    - 固定显示 4 个阶段（M1-M4）
    - 每个阶段显示血量、石料数量、是否最终阶段、下一阶段
    - _Requirements: 1.2_

  - [x] 1.3 实现当前状态分组绘制
    - 实现 DrawCurrentState() 方法
    - 显示当前阶段（下拉框）、矿物类型（下拉框）、含量指数（滑块）
    - OreIndex 滑块范围根据阶段动态调整
    - _Requirements: 1.3, 6.1-6.4_

  - [x] 1.4 实现 Sprite 配置分组绘制
    - 实现 DrawSpriteConfig() 方法
    - 显示 Sprite 渲染器引用
    - 显示 Sprite 路径前缀
    - _Requirements: 1.4_

  - [x] 1.5 实现状态预览分组绘制
    - 实现 DrawStatusPreview() 方法
    - 显示当前 Sprite 名称
    - 显示血量（当前/上限）
    - 显示预计掉落（矿物、石料）
    - 显示所需镐子等级
    - _Requirements: 5.1-5.4_

  - [x] 1.6 实现其他分组绘制
    - 实现 DrawDropConfig() 掉落配置
    - 实现 DrawSoundSettings() 音效设置
    - 实现 DrawDebugSettings() 调试设置
    - _Requirements: 1.1_

- [x] 2. 修改 StoneController 支持运行时调试

  - [x] 2.1 添加运行时参数检测
    - 添加 lastOreType、lastStage、lastOreIndex 字段
    - 在 Update() 中检测参数变化
    - 参数变化时调用 UpdateSprite()
    - _Requirements: 3.1-3.4_

  - [x] 2.2 实现 OreIndex 范围钳制
    - 添加 GetMaxOreIndex(StoneStage) 方法
    - 在 SetStage() 和 Update() 中钳制 OreIndex
    - _Requirements: 2.4, 6.1-6.4_

  - [x] 2.3 优化 UpdateSprite() 方法
    - 支持从缓存加载 Sprite
    - 添加 Sprite 不存在时的警告日志
    - _Requirements: 2.1-2.3, 3.4_

  - [x] 2.4 添加阶段切换血量重置
    - 修改 SetStage() 方法
    - 阶段变化时自动重置血量
    - _Requirements: 2.2_

- [x] 3. 最终检查
  - Unity 编译通过，无警告
  - 所有功能已实现完成
