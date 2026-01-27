# Implementation Plan

## 1. 核心框架搭建

- [x] 1.1 创建 ResourceNodeRegistry 单例
  - 创建 `Assets/Scripts/Combat/ResourceNodeRegistry.cs`
  - 实现 Register/Unregister/GetNodesInRange 方法
  - 使用 Dictionary 存储节点，支持快速查找
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 1.2 更新 IResourceNode 接口
  - 修改 `Assets/Scripts/Interfaces/IResourceNode.cs`
  - 添加 GetBounds()、ResourceTag 属性
  - 更新 CanAccept 和 OnHit 方法签名
  - _Requirements: 1.3, 8.1_

- [x] 1.3 创建 ToolResourceConfig 配置类
  - 创建 `Assets/Scripts/Data/ToolResourceConfig.cs`
  - 定义工具-资源交互配置结构
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 8.2_

## 2. 命中检测系统

- [x] 2.1 重写 PlayerToolHitEmitter
  - 修改 `Assets/Scripts/Combat/PlayerToolHitEmitter.cs`
  - 实现帧索引检测（frame 3-4）
  - 实现 Sprite Bounds 相交检测
  - 移除 LayerMask 依赖，改用 ResourceNodeRegistry
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 2.2 实现扇形-Bounds 相交算法
  - 计算扇形范围（origin, forward, angle, reach）
  - 检查 Bounds 是否与扇形相交
  - 支持不同方向（Down/Side/Up + FlipX）
  - _Requirements: 1.1, 1.2_

- [x] 2.3 集成工具-资源交互配置
  - 在 PlayerToolHitEmitter 中添加配置数组
  - 根据配置决定是否扣血/消耗精力/播放特效
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

## 3. TreeController 扩展

- [x] 3.1 实现 IResourceNode 接口
  - 更新 TreeController 实现新接口
  - GetBounds() 返回 Tree 子物体的 SpriteRenderer.bounds
  - ResourceTag 返回 "Tree"
  - _Requirements: 1.2, 8.1_

- [x] 3.2 注册到 ResourceNodeRegistry
  - 在 Start 中注册
  - 在 OnDestroy 中注销
  - _Requirements: 2.1, 2.2_

- [x] 3.3 实现 OnHit 方法
  - 根据 ToolHitContext.toolType 判断是否扣血
  - 斧头：扣血 + 抖动 + 树叶 + 精力
  - 其他：仅抖动
  - _Requirements: 3.1, 3.2, 4.3_

- [x] 3.4 完善血量系统
  - 根据成长阶段初始化血量
  - 血量归零时转换为 Stump 状态
  - 触发掉落物生成
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

## 4. 树叶飘落效果

- [x] 4.1 创建 LeafFallEffect 组件
  - 创建 `Assets/Scripts/Effects/LeafFallEffect.cs`
  - 管理单个树叶的飘落行为
  - 支持随机漂移和旋转
  - 到达目标高度后淡出
  - _Requirements: 6.2, 6.3_

- [x] 4.2 创建 LeafSpawner 组件
  - 创建 `Assets/Scripts/Effects/LeafSpawner.cs`
  - 从 Sprite 数组随机选择树叶
  - 在树木顶部生成 3-5 片树叶
  - 使用对象池管理树叶实例
  - _Requirements: 6.1, 6.4, 6.5_

- [x] 4.3 集成到 TreeController
  - 添加 LeafSpawner 引用
  - 在 OnHit 中调用 SpawnLeaves()
  - 配置树叶 Sprite 数组
  - _Requirements: 5.2, 6.1_

## 5. 受击表现完善

- [x] 5.1 优化抖动效果
  - 调整抖动参数（时长、幅度）
  - 支持不同强度的抖动
  - _Requirements: 5.1_

- [x] 5.2 添加音效支持
  - 在 TreeController 添加音效字段
  - 砍击音效、砍倒音效
  - 使用 AudioSource.PlayClipAtPoint
  - _Requirements: 5.3, 5.4, 5.5_

## 6. 精力系统集成

- [x] 6.1 创建精力消耗接口
  - 定义 IStaminaConsumer 接口或使用现有系统
  - PlayerToolHitEmitter 在成功命中后调用
  - _Requirements: 7.1, 7.2, 7.3_

- [x] 6.2 精力不足检查
  - 在工具使用前检查精力
  - 精力不足时阻止动作并显示提示
  - _Requirements: 7.4_

## 7. Checkpoint - 基础功能测试

- [x] 7. Checkpoint - 确保基础砍树功能正常
  - ✅ 代码编译通过（0 错误）
  - ✅ ResourceNodeRegistry 单例已在场景中
  - ⏳ 需要用户手动测试验证砍树功能

## 8. 全局规则更新

- [x] 8.1 更新 steering 规则
  - 添加项目层级设计规则到 `.kiro/steering/`
  - 记录树木层级结构和碰撞体用途
  - _Requirements: 项目规范_

## 9. 树木倒下动画

- [x] 9.1 实现倒下方向判定
  - 根据玩家朝向（Direction）和翻转状态（FlipX）判定倒下方向
  - 实现 `DetermineFallDirection()` 方法
  - 倒下方向枚举：Right, Left, Up
  - _Requirements: 9.1, 9.2, 9.3_

- [x] 9.2 实现轴心点计算
  - 统一使用 Collider 中心作为轴心点
  - 实现 `CalculatePivotPoint()` 方法
  - _Requirements: 9.4_

- [x] 9.3 实现倒下动画协程
  - 创建临时的倒下 Sprite（纯视觉，无碰撞）
  - 绕轴心点旋转 90°
  - 使用 t² 曲线（先慢后快，模拟重力）
  - 最后 30% 时间淡出
  - 动画时长 1.5 秒
  - _Requirements: 9.5_

- [x] 9.4 实现树桩与动画分离
  - 树桩在被砍到的那一刻就立着（原位置）
  - 倒下的树木 Sprite 是纯视觉效果
  - 动画结束后销毁临时 Sprite
  - _Requirements: 9.1, 9.2, 9.3_

- [x] 9.5 添加调试输出
  - 输出玩家朝向、翻转状态、倒下方向、轴心点、旋转角度
  - 便于验证判定逻辑
  - _Requirements: 调试_

## 10. 命中检测调试绘制

- [x] 10.1 修复扇形绘制
  - 以玩家 Collider 中心为起点
  - 朝向玩家面对的方向
  - 绘制扇形边界线和弧线
  - _Requirements: 10.1, 10.2_

- [x] 10.2 添加命中点和方向箭头
  - 红色点显示在树木上的实际命中位置
  - 黄色四向箭头指向树木倒下的方向
  - _Requirements: 10.3, 10.4_

## 11. Checkpoint - 倒下动画测试

- [x] 11. Checkpoint - 确保倒下动画正常



  - 验证各方向倒下动画效果
  - 验证树桩位置正确
  - 验证动画不影响物理
  - Ensure all tests pass, ask the user if questions arise.

## 12. 扩展性准备

- [x] 12.1 创建 RockController 模板
  - ✅ 已存在 `Assets/Scripts/Controller/RockController.cs`
  - ✅ 继承自 ResourceNode 基类
  - ✅ 实现了矿石类型、等级、受损Sprite、重生等功能
  - _Requirements: 8.1_

- [x] 12.2 创建通用 ResourceNode 基类
  - ✅ 已存在 `Assets/Scripts/World/ResourceNode.cs`
  - ✅ 提供了 TakeDamage、Reset、SpawnDrops 等通用方法
  - ✅ RockController 已继承此基类
  - _Requirements: 8.1, 8.2_

## 13. Final Checkpoint

- [x] 13. Final Checkpoint - 完整功能验证
  - ✅ 所有核心代码已实现
  - ✅ 编译通过（0 错误）
  - ✅ 倒下动画各方向测试通过
  - ✅ 命中检测调试绘制正确
  - ✅ RockController 和 ResourceNode 基类已存在
  - Ensure all tests pass, ask the user if questions arise.
  - 命中检测调试绘制正确
  - Ensure all tests pass, ask the user if questions arise.

## 14. 砍树经验获取（与 skill-level-system 关联）

- [x] 14.1 添加经验配置字段
  - 在 TreeControllerV2 添加 stageExperience 数组
  - 配置值：[0, 0, 2, 4, 6, 20]（阶段0-5）
  - 实现 GetChopExperience() 方法
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_

- [x] 14.2 在 ChopDown 中添加经验获取
  - 调用 SkillLevelService.AddExperience(SkillType.Gathering, xp)
  - 使用 Gathering 技能（采集）而非 Woodcutting
  - 只在树木被砍倒时给予经验（不是每次命中）
  - _Requirements: 11.7_

> ✅ **已完成**：此任务已与 `skill-level-system` 工作区的 SkillLevelService 集成

## 15. 材料等级限制（与 stone-system 关联）

- [x] 15.1 添加材料等级判定方法
  - 实现 CanChopWithAxeTier(int axeMaterialTier) 方法
  - 阶段 0-3：任何斧头都能砍
  - 阶段 4：需要生铁斧（等级2）或更高
  - 阶段 5：需要黄铜斧（等级3）或更高
  - _Requirements: 12.1-12.4_

- [x] 15.2 更新 OnHit 方法添加材料等级检查
  - 在处理命中前检查斧头材料等级
  - 等级不足时：播放金属碰撞音效，显示提示，但仍消耗精力和耐久
  - _Requirements: 12.5-12.7_

- [x] 15.3 添加等级不足反馈
  - 添加金属碰撞音效字段
  - 实现 ShowAxeTierInsufficientHint() 方法
  - _Requirements: 12.7_

> ✅ **已完成**：材料等级系统已实现，详见 `Docx/分类/全局/材料等级设计.md`

## 16. 精确命中检测修复（Bug修复）

- [x] 16.1 扩展 IResourceNode 接口
  - 添加 `GetColliderBounds()` 方法
  - 返回 Collider bounds，无 Collider 时回退到 Sprite bounds
  - _Requirements: 13.1, 13.4_

- [x] 16.2 更新 TreeControllerV2 实现 GetColliderBounds
  - 获取树木的 Collider2D 组件
  - 返回 Collider.bounds
  - 无 Collider 时回退到 SpriteRenderer.bounds
  - _Requirements: 13.1, 13.3, 13.4_

- [x] 16.3 实现位置验证方法
  - 在 PlayerToolHitEmitter 添加 `ValidatePositionRelation()` 方法
  - 根据玩家朝向验证相对位置
  - 支持 0.1 单位容差值
  - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6_

- [x] 16.4 重写 TryHit 方法实现三重验证
  - 使用 GetColliderBounds() 替代 GetBounds()
  - 添加位置验证步骤
  - 实现单目标选择（Collider 中心距离最近）
  - _Requirements: 13.1, 13.2, 14.5, 15.1, 15.2, 15.4_

- [x] 16.5 更新调试输出
  - 输出使用的 bounds 类型（Collider/Sprite）
  - 输出位置验证结果
  - 输出候选节点数量和选中节点
  - _Requirements: 调试_

## 17. Checkpoint - 精确命中检测测试

- [x] 17. Checkpoint - 确保精确命中检测正常
  - ✅ 编译通过（0 错误）
  - ✅ IResourceNode 接口已扩展
  - ✅ TreeControllerV2、TreeController、StoneController 已实现 GetColliderBounds
  - ✅ PlayerToolHitEmitter 已实现三重验证
  - ⏳ 需要用户手动测试验证命中检测功能

### ✅ 已完成的代码文件

| 文件 | 状态 | 说明 |
|------|------|------|
| `ResourceNodeRegistry.cs` | ✅ | 资源节点注册表单例 |
| `PlayerToolHitEmitter.cs` | ✅ | 帧索引命中检测 |
| `IResourceNode.cs` | ✅ | 资源节点接口 |
| `ToolResourceConfig.cs` | ✅ | 工具-资源交互配置 |
| `TreeController.cs` | ✅ | 树木控制器（已扩展 IResourceNode） |
| `LeafSpawner.cs` | ✅ | 树叶生成器 |
| `LeafFallEffect.cs` | ✅ | 树叶飘落效果 |
| `ToolEvents.cs` | ✅ | 工具事件总线 |

### ⏳ 待用户配置

1. 场景中添加 `ResourceNodeRegistry` 组件（MANAGERS 下）
2. 玩家 GameObject 添加 `PlayerToolHitEmitter` 组件
3. 树木预制体 Tree 子物体添加 `LeafSpawner` 组件（可选）
4. 配置音效资源（可选）
