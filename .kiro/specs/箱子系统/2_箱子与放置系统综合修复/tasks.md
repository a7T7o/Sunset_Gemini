# 实现计划

## 第一部分：Sprite 状态管理（工作区 1 未完成）

- [ ] 1. 实现 ChestController Sprite 管理

  - [ ] 1.1 确认 4 个 Sprite 字段已存在
    - spriteUnlockedClosed, spriteUnlockedOpen, spriteLockedClosed, spriteLockedOpen
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [ ] 1.2 实现 GetCurrentSprite 方法
    - 根据 isLocked 和 _isOpen 返回对应 Sprite
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [ ] 1.3 实现 UpdateSprite 方法
    - 获取当前状态对应的 Sprite 并应用到 SpriteRenderer
    - 调用 UpdateColliderFromSprite() 更新碰撞体
    - _Requirements: 1.5, 2.2_

  - [ ] 1.4 实现 SetOpen 方法
    - 设置 _isOpen 状态并调用 UpdateSprite
    - _Requirements: 1.5, 6.3_

  - [ ] 1.5 添加 OnValidate 验证
    - 检查 4 个 Sprite 字段是否为空，为空则输出警告
    - _Requirements: 1.6_

  - [ ]* 1.6 编写属性测试：状态到 Sprite 映射
    - **Property 1: 状态到 Sprite 的映射一致性**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4**

## 第二部分：PolygonCollider2D 更新（新问题）

- [ ] 2. 实现碰撞体从 Sprite 更新

  - [ ] 2.1 实现 UpdateColliderFromSprite 方法
    - 参考 TreeControllerV2.UpdateColliderFromSprite()
    - 从 Sprite.GetPhysicsShape() 获取形状
    - 设置 _polygonCollider.SetPath()
    - _Requirements: 2.1, 2.2, 2.3_

  - [ ] 2.2 在 Awake 中缓存 PolygonCollider2D 引用
    - _Requirements: 2.1, 8.1_

  - [ ] 2.3 在 UpdateSprite 中调用 UpdateColliderFromSprite
    - _Requirements: 2.2_

  - [ ] 2.4 添加 Sprite 无 Custom Physics Shape 的错误处理
    - 保持原有碰撞体不变，输出警告日志
    - _Requirements: 2.4_

  - [ ]* 2.5 编写属性测试：碰撞体与 Sprite 同步
    - **Property 2: 碰撞体与 Sprite 同步**
    - **Validates: Requirements 2.1, 2.2**

## 第三部分：抖动和推动效果（工作区 1 未完成）+ 安全机制

- [ ] 3. 实现抖动效果

  - [ ] 3.1 实现 PlayShakeEffect 方法
    - 启动抖动协程
    - _Requirements: 3.1, 3.5_

  - [ ] 3.2 实现 ShakeCoroutine 协程
    - 在 shakeDuration 时间内随机偏移位置
    - 结束后恢复原位置
    - _Requirements: 3.2_

- [ ] 4. 改进推动动画 + 安全机制

  - [ ] 4.1 添加 _isPushing 标志位
    - 🔴 防止协程重入导致"无限升空" Bug
    - 在推动过程中拒绝新的推动请求
    - _Requirements: 3.3, 3.4_
    - _参考：锐评与反思.md - 问题 2_

  - [ ] 4.2 实现 TryPush 方法的碰撞检测
    - 🔴 使用 Physics2D.BoxCast 检测目标位置是否有障碍物
    - 如果有障碍物，只播放抖动，不执行推动
    - _Requirements: 3.3, 3.4_
    - _参考：锐评与反思.md - 问题 1（第二轮）_

  - [ ] 4.3 修改 PushCoroutine 实现多次跳跃
    - 3 次跳跃，越跳越短（0.5, 0.3, 0.2）
    - 越跳越矮（1.0, 0.6, 0.3）
    - 在协程开始时设置 _isPushing = true
    - 在协程结束时设置 _isPushing = false
    - _Requirements: 3.3, 3.4_

## 第四部分：IResourceNode 接口（工作区 1 未完成）

- [ ] 5. 实现 IResourceNode 接口

  - [ ] 5.1 确认 IResourceNode 接口实现
    - ResourceTag 返回 "Chest"
    - IsDepleted 返回 false
    - _Requirements: 4.1, 4.3, 4.4_

  - [ ] 5.2 修改 CanAccept 方法
    - 返回 true（所有工具都能触发抖动）
    - _Requirements: 4.5_

  - [ ] 5.3 确认 GetBounds 和 GetColliderBounds 方法
    - 返回 SpriteRenderer 或 Collider 的边界
    - _Requirements: 4.6_

  - [ ] 5.4 确认 Start 中注册到 ResourceNodeRegistry
    - _Requirements: 4.1, 13.3_

  - [ ] 5.5 确认 OnDestroy 中从 ResourceNodeRegistry 注销
    - _Requirements: 4.2, 13.4_

## 第五部分：OnHit 方法重构（工作区 1 未完成）+ 野外箱子清理后路

- [ ] 6. 重构 OnHit 方法

  - [ ] 6.1 修改 OnHit 方法集成来源归属系统
    - 始终播放抖动效果
    - 检查 CanBeMinedOrMoved()，不可挖取则直接返回
    - 镐子攻击有物品箱子：推动
    - 镐子攻击空箱子：造成伤害
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [ ] 6.2 添加 allowDestroyEmptyWorldChest 开关
    - 🟡 在 CanBeMinedOrMoved() 中添加后路逻辑
    - 如果开关启用且箱子为空且已开锁，允许清理野外箱子
    - 默认值为 false，严格遵守原始需求
    - _参考：锐评与反思.md - 问题 2（第二轮）_

  - [ ]* 6.3 编写属性测试：工具类型决定伤害
    - **Property 3: 工具类型决定伤害**
    - **Validates: Requirements 5.5**

- [ ] 7. Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户。

## 第六部分：UI 状态同步（工作区 1 未完成）- 事件驱动架构

- [ ] 8. 更新 UI 状态同步为事件驱动架构

  - [ ] 8.1 在 ChestController 中添加状态变化事件
    - 定义 `public event Action<bool> OnOpenStateChanged`
    - 在 SetOpen() 方法中触发事件
    - 添加重复状态检查（避免重复触发）
    - _Requirements: 6.1, 6.2_
    - _参考：锐评与反思.md - 问题 3（第一轮）_

  - [ ] 8.2 修改 BoxPanelUI 使用事件订阅机制
    - Open() 方法：取消订阅旧箱子，订阅新箱子事件
    - Close() 方法：取消订阅事件
    - 实现 OnChestStateChanged(bool) 回调方法
    - 调用 SetOpen() 触发状态变化（而非直接修改状态）
    - _Requirements: 6.1, 6.2_
    - _参考：锐评与反思.md - 问题 3（第一轮）_

## 第七部分：批量生成工具更新（工作区 1 未完成）

- [ ] 9. 更新批量生成物品 SO 工具

  - [ ] 9.1 确认 ItemSOType 枚举中已添加 KeyData 和 LockData 类型
    - _Requirements: 7.1_

  - [ ] 9.2 确认 KeyData 和 LockData 的映射配置
    - SubTypeNames: "钥匙", "锁"
    - SubTypeStartIDs: 100, 110（01XX 范围）
    - SubTypeOutputFolders: "Assets/111_Data/Items/Keys", "Assets/111_Data/Items/Locks"
    - _Requirements: 7.6, 7.7_

  - [ ] 9.3 确认 DrawKeySettings 方法已实现
    - keyMaterial 下拉框（ToolMaterial 枚举）
    - unlockChance 滑动条（0-1 范围）
    - _Requirements: 7.2, 7.3_

  - [ ] 9.4 确认 DrawLockSettings 方法已实现
    - lockMaterial 下拉框（ToolMaterial 枚举）
    - _Requirements: 7.3_

  - [ ] 9.5 确认 CreateKeyData 方法已实现
    - 创建 KeyLockData SO 并设置属性
    - 根据材质自动设置默认开锁概率
    - _Requirements: 7.4_

  - [ ] 9.6 确认 CreateLockData 方法已实现
    - 创建 KeyLockData SO 并设置属性
    - unlockChance 固定为 0
    - _Requirements: 7.5_

  - [ ] 9.7 确认 CategoryToSubTypes 映射已更新
    - 将 KeyData 和 LockData 添加到 ToolEquipment 大类
    - _Requirements: 7.1_

## 第八部分：初始化逻辑完善（工作区 1 未完成）

- [ ] 10. 更新初始化逻辑

  - [ ] 10.1 在 Awake 中缓存 SpriteRenderer 和 PolygonCollider2D
    - _Requirements: 8.1_

  - [ ] 10.2 在 Start 中调用 UpdateSprite 初始化显示
    - _Requirements: 8.2_

  - [ ] 10.3 添加 Initialize 重载支持设置 origin
    - Initialize(StorageData, ChestOrigin, ChestOwnership)
    - _Requirements: 8.3, 8.4, 8.5_

## 第九部分：放置系统修复（新问题）+ 物理安全机制

- [ ] 11. 修复放置系统向左/向下方向问题 + 安全机制

  - [ ] 11.1 修改 PlacementNavigator.CalculateNavigationTarget()
    - 返回放置点中心而非边界最近点
    - _Requirements: 9.1, 9.2, 9.5_

  - [ ] 11.2 添加 placementTriggerDistance 参数
    - 触发放置的距离阈值（建议 1.5f）
    - _Requirements: 9.5_

  - [ ] 11.3 修改 CheckReached() 方法
    - 检测与放置点中心的距离
    - 距离小于阈值时触发放置
    - _Requirements: 9.5_

  - [ ] 11.4 添加放置成功检测
    - 放置成功后立即停止导航
    - 而非等待导航完成
    - _Requirements: 9.3_

  - [ ] 11.5 添加导航超时和卡死检测
    - 🟡 实现 IsNavigationStuck() 方法
    - 检测导航超时（建议 10 秒）
    - 检测路径是否有效
    - 超时或卡死时自动取消放置
    - _Requirements: 9.4_
    - _参考：锐评与反思.md - 问题 4（第二轮）_

  - [ ] 11.6 添加放置时的物理保护机制
    - 🔴 在 PlaceObject() 中添加物理安全机制
    - 方案 A：放置瞬间将箱子 Collider 设为 Trigger，延迟后恢复
    - 方案 B（备选）：使用 Physics2D.IgnoreCollision 暂时忽略玩家碰撞
    - 实现 EnableColliderAfterDelay() 协程，检测玩家是否离开
    - _参考：锐评与反思.md - 问题 1（第一轮）和终极锐评_

  - [ ]* 11.7 编写属性测试：放置方向无关性
    - **Property 4: 放置方向无关性**
    - **Validates: Requirements 9.1, 9.2**

  - [ ]* 11.8 编写属性测试：放置成功终止
    - **Property 5: 放置成功终止**
    - **Validates: Requirements 9.3**

## 第十部分：箱子丢弃机制（新问题）

- [ ] 12. 修改箱子丢弃处理

  - [ ] 12.1 修改 WorldSpawnService.SpawnWithAnimation()
    - 检测物品是否为 StorageData
    - 如果是，调用 ChestDropHandler.HandleChestDrop()
    - _Requirements: 10.1_

  - [ ] 12.2 修改 ChestDropHandler.HandleChestDrop()
    - 实例化 Box 预制体（而非 WorldItem）
    - 添加临时阴影 SpriteRenderer
    - 播放弹跳动画
    - 动画完成后移除临时阴影
    - 初始化 ChestController
    - _Requirements: 10.1, 10.2, 10.3, 10.5_

  - [ ] 12.3 实现 CreateTemporaryShadow 方法
    - 创建临时阴影 GameObject
    - 设置阴影 Sprite 和位置
    - _Requirements: 10.2_

  - [ ] 12.4 修改 PlayBounceAnimation 协程
    - 同时移动箱子和临时阴影
    - 动画完成后返回
    - _Requirements: 10.3_

  - [ ]* 12.5 编写属性测试：箱子丢弃不可拾取
    - **Property 6: 箱子丢弃不可拾取**
    - **Validates: Requirements 10.4**

## 第十一部分：箱子打开功能修复（新问题）+ 交互优化

- [ ] 13. 修复箱子打开功能 + 交互距离优化

  - [ ] 13.1 检查 GameInputManager.HandleChestInteraction()
    - 确保正确检测箱子
    - 确保正确调用 TryOpenChest()
    - _Requirements: 11.4_

  - [ ] 13.2 检查 TryOpenChest() 方法
    - 🟡 优先使用 SerializeField 引用 BoxPanelUI
    - FindFirstObjectByType 作为备选方案
    - 确保正确调用 BoxPanelUI.Open(chest)
    - _Requirements: 11.1, 11.3_
    - _参考：锐评与反思.md - 问题 5（第一轮）_

  - [ ] 13.3 实现 CalculateInteractionDistance() 方法
    - 🟡 使用 Collider.ClosestPoint() 计算精确距离
    - 支持不规则形状箱子的交互判定
    - 备选方案：使用中心点距离
    - _参考：锐评与反思.md - 问题 3（第二轮）_

  - [ ] 13.4 添加距离检测和自动导航
    - 检查玩家与箱子的距离（使用 CalculateInteractionDistance）
    - 超出交互距离时触发自动导航
    - 到达后自动打开
    - _Requirements: 11.2, 13.2_

  - [ ]* 13.5 编写属性测试：箱子打开可触发
    - **Property 7: 箱子打开可触发**
    - **Validates: Requirements 11.1, 11.2**

## 第十二部分：钥匙和锁材质等级扩展（新问题）

- [ ] 14. 扩展钥匙和锁材质等级

  - [ ] 14.1 修改 KeyLockData.cs
    - 将 material 字段类型从 ChestMaterial 改为 int
    - 添加 [Range(0, 5)] 属性限制范围
    - 添加 GetMaterialName() 方法
    - _Requirements: 12.1, 12.2, 12.3_

  - [ ] 14.2 修改 StorageData.cs
    - 将 chestMaterial 字段类型从 ChestMaterial 改为 int
    - 添加 [Range(0, 5)] 属性限制范围
    - 添加 GetMaterialName() 方法
    - _Requirements: 12.7_

  - [ ] 14.3 创建 MaterialTier 静态类
    - 定义材质等级常量（Wood=0, Stone=1, Iron=2, Copper=3, Steel=4, Gold=5）
    - 定义材质名称数组
    - 实现 GetName(int tier) 方法
    - _Requirements: 12.1_

  - [ ] 14.4 修改 ChestController.TryLock() 和 TryUnlock()
    - 使用 int 类型的材质等级进行比较
    - _Requirements: 12.4, 12.5_

  - [ ] 14.5 更新批量生成工具
    - 支持选择 0-5 的材质等级
    - _Requirements: 12.6_

  - [ ]* 14.6 编写属性测试：材质等级范围
    - **Property 8: 材质等级范围**
    - **Validates: Requirements 12.1, 12.2, 12.3, 12.7**

## 第十三部分：箱子系统与放置系统集成

- [ ] 15. 完善箱子系统集成

  - [ ] 15.1 确认箱子放置时的初始化
    - 调用 Initialize(StorageData, ChestOrigin.PlayerCrafted, ChestOwnership.Player)
    - _Requirements: 13.1_

  - [ ] 15.2 确认交互距离判断
    - 使用与放置系统相同的交互距离
    - _Requirements: 13.2_

  - [ ] 15.3 确认 ResourceNodeRegistry 注册
    - 箱子放置后注册到 ResourceNodeRegistry
    - 箱子销毁时从 ResourceNodeRegistry 注销
    - _Requirements: 13.3, 13.4_

- [ ] 16. Final Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户。
