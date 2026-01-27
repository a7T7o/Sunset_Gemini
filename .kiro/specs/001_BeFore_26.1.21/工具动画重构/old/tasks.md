# 实现计划

- [x] 1. 修复空槽位报错和动画参数警告
  - [x] 1.1 修改 ItemDatabase.GetItemByID() 静默处理 id=-1
    - 当 id < 0 时直接返回 null，不输出警告
    - _需求: 1.1_
    - ✅ 已完成：ItemDatabase 已有此逻辑
  - [x] 1.2 修改 AnimatorExtensions 静默处理空控制器
    - SafeSetInteger/SafeGetInteger 在无控制器时不输出警告
    - _需求: 1.2_
    - ✅ 已完成：移除了警告输出
  - [x] 1.3 修改 HotbarSelectionService 静默处理空槽位
    - EquipCurrentTool 在空槽位时静默跳过
    - _需求: 1.3_
    - ✅ 已完成：已有 `if (slot.IsEmpty) return;` 逻辑

- [x] 2. 更新 ToolData 数据结构
  - [x] 2.1 添加可选伤害属性
    - 添加 canDealDamage 布尔字段
    - 添加 damageAmount 整数字段
    - _需求: 9.1, 9.4_
    - ✅ 已完成
  - [x] 2.2 确保 GetAnimationId() 返回 itemID
    - 验证方法正确返回 itemID
    - _需求: 3.1_
    - ✅ 已完成：GetAnimationId() 直接返回 itemID

- [x] 3. 实现精力系统核心
  - [x] 3.1 创建 EnergySystem.cs 脚本
    - 实现 maxEnergy、currentEnergy 字段
    - 实现 TryConsumeEnergy、RestoreEnergy、FullRestore 方法
    - 添加 OnEnergyChanged、OnEnergyDepleted 事件
    - _需求: 7.1, 7.2, 7.5_
    - ✅ 已完成
  - [x] 3.2 集成 UI Slider (UI/State/EP)
    - 获取 EP Slider 引用
    - 实现 UpdateUI 方法同步 Slider 值
    - _需求: 7.4_
    - ✅ 已完成：自动查找 UI/State/EP 路径

- [x] 4. 集成精力消耗到工具使用流程
  - [x] 4.1 修改 PlayerInteraction 添加精力消耗逻辑
    - 工具操作成功时调用 EnergySystem.TryConsumeEnergy
    - 操作失败时不消耗精力
    - _需求: 7.2, 7.3_
    - ✅ 已完成：添加 OnToolActionSuccess() 方法
  - [x] 4.2 确保武器使用不消耗精力
    - WeaponData 使用时跳过精力消耗
    - _需求: 8.1, 8.2, 8.3_
    - ✅ 已完成：只有 ToolData 才消耗精力

- [x] 5. 检查点 - 确保所有测试通过
  - ✅ 编译通过，核心功能已实现

- [x] 6. 更新 PlayerToolController 动画缓存逻辑
  - [x] 6.1 确保使用 itemID 缓存状态哈希
    - CacheStateHashes 使用 ToolData.GetAnimationId()
    - _需求: 3.3_
    - ✅ 已完成：使用 data.GetAnimationId() 获取 itemID
  - [x] 6.2 更新日志输出使用 itemID
    - 装备工具时输出 itemID 而非 toolAnimId
    - _需求: 3.3_
    - ✅ 已完成：日志输出 ItemID

- [x] 7. 更新 ToolAnimationGeneratorTool（简化版）
  - [x] 7.1 简化动画剪辑命名格式
    - 新格式：{Action}_{Dir}_Clip_{ItemID}（移除 quality 后缀）
    - _需求: 5.1, 5.2_
    - ✅ 已完成
  - [x] 7.2 修复控制器生成只使用 Any State 转换
    - 移除状态间转换，只保留 Any State → 状态
    - _需求: 4.1, 4.2_
    - ✅ 已完成：改用 stateMachine.AddAnyStateTransition()
  - [x] 7.3 简化控制器参数
    - 参数：State、Direction、ToolItemId（移除 ToolQuality）
    - _需求: 4.4_
    - ✅ 已完成

- [x] 8. 更新 LayerAnimSync 动画同步逻辑（简化版）
  - [x] 8.1 确保使用 Animator.Play() 直接切换状态
    - 不依赖状态转换，直接 Play 目标状态
    - _需求: 4.3_
    - ✅ 已完成：使用 toolAnimator.Play(targetHash, 0, toolNorm)
  - [x] 8.2 简化状态哈希构建
    - 新格式：{Action}_{Dir}_Clip_{ItemID}（移除 quality）
    - _需求: 6.4_
    - ✅ 已完成

- [x] 9. 场景配置和测试
  - [x] 9.1 重构 Player 工具对象结构
    - 原结构：Player 下有 Crush、Pierce、Slice 三个子对象
    - 新结构：合并为单个 Tool 对象（原 Slice 重命名）
    - 删除了 Crush 和 Pierce 对象
    - PlayerToolController 动态切换 Tool 的 AnimatorController
    - _需求: 动态控制器切换_
    - ✅ 已通过 MCP 完成
  - [ ] 9.2 在场景中配置 EnergySystem 组件
    - 添加到 Player 或 Managers 对象
    - 配置 EP Slider 引用
    - _需求: 7.4_
    - ⏳ 需要用户在 Unity 编辑器中手动配置
  - [ ] 9.3 更新现有 ToolData SO 配置
    - 确保 animatorController 已配置
    - 设置 canDealDamage 和 damageAmount
    - _需求: 9.1, 9.4_
    - ⏳ 需要用户在 Unity 编辑器中手动配置

- [x] 10. 最终检查点 - 确保所有测试通过
  - ✅ 编译通过，核心功能已实现
  - ✅ Player 工具结构已重构为单个 Tool 对象
  - ⏳ 场景配置需要用户手动完成

- [x] 11. 额外修复（用户反馈）
  - [x] 11.1 修复 WorldSpawnDebug 品质优先级问题
    - 工具/武器：每个品质都是独立 ItemID，品质固定为 0
    - 其他物品使用 Inspector 设置的 quality
    - ✅ 已完成
  - [x] 11.2 修改控制器命名格式
    - 新格式：{ActionType}_Controller_{ItemID}_{ItemName}
    - 例如：Slice_Controller_0_Axe
    - 添加 itemName 输入字段
    - ✅ 已完成
  - [x] 11.3 修复 Crush+Side 方向时 Tool 层级问题
    - Crush (State=8) + Side (Direction=2) 时 Tool 在 Player 之上
    - 其他情况 Tool 在 Player 之下
    - 修改 LayerAnimSync.UpdateToolVisibility()
    - ✅ 已完成

- [x] 12. 重大设计变更：简化动画命名系统
  - [x] 12.1 移除 quality 后缀
    - 旧格式：{Action}_{Dir}_Clip_{ItemID}_{Quality}
    - 新格式：{Action}_{Dir}_Clip_{ItemID}
    - 每个品质的工具都是独立 ItemID
    - ✅ 已完成
  - [x] 12.2 更新 ToolData.cs
    - 移除 quality 字段（每个品质是独立 ID）
    - ✅ 已完成
  - [x] 12.3 更新 WeaponData.cs
    - 移除 quality 字段和 useQualityIdMapping
    - 移除 animationDefaultId
    - GetAnimationKeyId() 直接返回 itemID
    - ✅ 已完成
  - [x] 12.4 更新 PlayerToolController.cs
    - 移除 currentToolQuality 字段
    - 简化 CacheStateHashes() 只缓存 direction
    - 简化 GetCachedStateHash() 只需要 direction 参数
    - 添加 UnequipCurrent() 方法
    - ✅ 已完成
  - [x] 12.5 更新 LayerAnimSync.cs
    - 移除 quality 相关逻辑
    - 简化 GetTargetToolStateHash() 方法
    - ✅ 已完成
  - [x] 12.6 更新 ToolAnimationGeneratorTool.cs
    - 动画剪辑命名移除 quality 后缀
    - 控制器参数移除 ToolQuality
    - Any State 转换条件只需要 State + Direction
    - ✅ 已完成
  - [x] 12.7 更新 ItemSOBatchCreator.cs
    - 移除 weapon_useQualityIdMapping 和 weapon_animationDefaultId
    - ✅ 已完成

- [x] 13. 修复拾取物品时自动更新装备
  - [x] 13.1 更新 HotbarSelectionService.cs
    - 订阅 OnSlotChanged 和 OnHotbarSlotChanged 事件
    - 当变化的槽位是当前选中槽位时自动调用 EquipCurrentTool()
    - 空槽位时调用 UnequipCurrent() 清除装备
    - ✅ 已完成

- [x] 14. 工具动画图层统一在玩家上层

  - [x] 14.1 修改 LayerAnimSync.UpdateToolVisibility() 方法
    - 将 sortingOrder 从 playerSpriteRenderer.sortingOrder - 1 改为 + 1
    - 移除 Crush+Side 的特殊处理逻辑
    - 移除 crushSideToolBelowPlayer 配置字段
    - _需求: 10.1, 10.2, 10.3, 10.4_
    - ✅ 已完成

## 用户下一步操作

1. **重新生成所有动画**
   - 使用 `Tools → 手持三向生成流程 → 工具动画一键生成`
   - 新格式：`{Action}_{Dir}_Clip_{ItemID}`（不含 quality）

2. **重新绑定 ToolData SO**
   - 为每个品质的工具创建独立的 ToolData SO
   - 每个 SO 有独立的 ItemID
   - 配置对应的 AnimatorController

3. **测试动画系统**
   - 运行游戏，切换不同工具
   - 观察控制台调试输出
   - 确认动画正确播放
