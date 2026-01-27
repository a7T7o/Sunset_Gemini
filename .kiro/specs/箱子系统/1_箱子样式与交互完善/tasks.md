# 实现计划

## 第一部分：数据结构扩展

- [x] 1. 新增 ChestOrigin 枚举和 KeyData 数据类

  - [x] 1.1 在 PlacementEnums.cs 中添加 ChestOrigin 枚举
    - PlayerCrafted = 0（玩家制作）
    - WorldSpawned = 1（野外生成）
    - _Requirements: 7.1, 7.2_

  - [x] 1.2 创建 KeyLockData.cs 数据类（复用已有）
    - 继承 ItemData
    - 添加 keyLockType 字段（Key/Lock）
    - 添加 unlockChance 字段（0-1 浮点数）
    - _Requirements: 11.1, 11.2_

  - [x] 1.3 修改 StorageData.cs 添加 baseUnlockChance 字段
    - 添加 baseUnlockChance 字段（0-1 浮点数，默认 0.6）
    - _Requirements: 12.1_

- [x] 2. 扩展 ChestController 来源与归属系统

  - [x] 2.1 添加 origin 和 hasBeenLocked 字段
    - origin: ChestOrigin（默认 PlayerCrafted）
    - hasBeenLocked: bool（默认 false）
    - _Requirements: 7.1, 7.2_

  - [x] 2.2 实现 CanBeMinedOrMoved 方法
    - 野外上锁箱子（hasBeenLocked=true）返回 false
    - 其他情况返回 true
    - _Requirements: 8.1, 8.2, 8.3_

  - [x] 2.3 实现 TryLockByPlayer 方法
    - 检查 hasBeenLocked，已上过锁返回 AlreadyLocked
    - 设置 isLocked=true, hasBeenLocked=true
    - 野外箱子上锁后 ownership 变为 Player
    - _Requirements: 9.1, 9.4, 7.3_

  - [x] 2.4 实现 TryUnlockWithKey 方法
    - 检查 isLocked 和 ownership
    - 计算开锁概率（钥匙概率 + 箱子概率）
    - 成功返回 Success，失败返回 MaterialMismatch
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

  - [ ]* 2.5 编写属性测试：野外上锁箱子不可挖取
    - **Property 3: 野外上锁箱子不可挖取**
    - **Validates: Requirements 8.1, 8.2**

  - [ ]* 2.6 编写属性测试：上过锁的箱子不能再次上锁
    - **Property 4: 上过锁的箱子不能再次上锁**
    - **Validates: Requirements 7.5, 9.4**

## 第二部分：Sprite 状态管理

- [ ] 3. 扩展 ChestController Sprite 管理

  - [ ] 3.1 确认4个 Sprite 字段已存在
    - spriteUnlockedClosed, spriteUnlockedOpen, spriteLockedClosed, spriteLockedOpen
    - _Requirements: 6.1_

  - [ ] 3.2 实现 GetCurrentSprite 方法
    - 根据 isLocked 和 _isOpen 返回对应 Sprite
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [ ] 3.3 实现 UpdateSprite 方法
    - 获取当前状态对应的 Sprite 并应用到 SpriteRenderer
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [ ] 3.4 实现 SetOpen 方法
    - 设置 _isOpen 状态并调用 UpdateSprite
    - _Requirements: 5.3, 5.4_

  - [ ] 3.5 添加 OnValidate 验证
    - 检查4个 Sprite 字段是否为空，为空则输出警告
    - _Requirements: 6.2_

  - [ ]* 3.6 编写属性测试：状态到 Sprite 映射
    - **Property 1: 状态到 Sprite 的映射一致性**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4**

## 第三部分：抖动和推动效果

- [ ] 4. 实现抖动效果

  - [ ] 4.1 实现 PlayShakeEffect 方法
    - 启动抖动协程
    - _Requirements: 2.1, 2.4_

  - [ ] 4.2 实现 ShakeCoroutine 协程
    - 在 shakeDuration 时间内随机偏移位置
    - 结束后恢复原位置
    - _Requirements: 2.1, 2.4_

- [ ] 5. 改进推动动画

  - [ ] 5.1 修改 PushCoroutine 实现多次跳跃
    - 3次跳跃，越跳越短（0.5, 0.3, 0.2）
    - 越跳越矮（1.0, 0.6, 0.3）
    - _Requirements: 3.2_

## 第四部分：IResourceNode 接口和 OnHit

- [ ] 6. 实现 IResourceNode 接口

  - [ ] 6.1 确认 IResourceNode 接口实现
    - ResourceTag 返回 "Chest"
    - IsDepleted 返回 false
    - _Requirements: 4.1, 4.2_

  - [ ] 6.2 修改 CanAccept 方法
    - 返回 true（所有工具都能触发抖动）
    - _Requirements: 4.4_

  - [ ] 6.3 确认 GetBounds 和 GetColliderBounds 方法
    - 返回 SpriteRenderer 或 Collider 的边界
    - _Requirements: 4.3_

  - [ ] 6.4 确认 Start 中注册到 ResourceNodeRegistry
    - _Requirements: 4.1_

  - [ ] 6.5 确认 OnDestroy 中从 ResourceNodeRegistry 注销
    - _Requirements: 4.2_

- [ ] 7. 重构 OnHit 方法

  - [ ] 7.1 修改 OnHit 方法集成来源归属系统
    - 始终播放抖动效果
    - 检查 CanBeMinedOrMoved()，不可挖取则直接返回
    - 镐子攻击有物品箱子：推动
    - 镐子攻击空箱子：造成伤害
    - _Requirements: 2.1, 2.4, 3.1, 8.1_

  - [ ]* 7.2 编写属性测试：工具类型决定伤害
    - **Property 2: 工具类型决定伤害**
    - **Validates: Requirements 2.1, 2.4**

- [ ] 8. Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户。

## 第五部分：UI 状态同步

- [ ] 9. 更新 BoxPanelUI 状态同步

  - [ ] 9.1 修改 BoxPanelUI.Open 方法
    - 调用 ChestController.SetOpen(true)
    - _Requirements: 5.1_

  - [ ] 9.2 修改 BoxPanelUI.Close 方法
    - 调用 ChestController.SetOpen(false)
    - _Requirements: 5.2_

## 第六部分：批量生成工具更新

- [x] 10. 更新批量生成物品 SO 工具

  - [x] 10.1 在 ItemSOType 枚举中添加 KeyData 类型
    - KeyData = 60（或其他合适的值）
    - _Requirements: 13.1_

  - [x] 10.2 添加 KeyData 的映射配置
    - SubTypeNames: "钥匙"
    - SubTypeStartIDs: 100（01XX 范围）
    - SubTypeOutputFolders: "Assets/111_Data/Items/Keys"
    - _Requirements: 13.5_

  - [x] 10.3 实现 DrawKeySettings 方法
    - keyMaterial 下拉框（ToolMaterial 枚举）
    - unlockChance 滑动条（0-1 范围）
    - _Requirements: 13.2, 13.3_

  - [x] 10.4 实现 CreateKeyData 方法
    - 创建 KeyLockData SO 并设置属性
    - 根据材质自动设置默认开锁概率
    - _Requirements: 13.4_

  - [x] 10.5 更新 CategoryToSubTypes 映射
    - 将 KeyData 添加到 ToolEquipment 大类
    - _Requirements: 13.1_

## 第七部分：初始化和收尾

- [ ] 11. 更新初始化逻辑

  - [ ] 11.1 在 Awake 中缓存 SpriteRenderer
    - _Requirements: 1.1_

  - [ ] 11.2 在 Start 中调用 UpdateSprite 初始化显示
    - _Requirements: 1.1_

  - [ ] 11.3 添加 Initialize 重载支持设置 origin
    - Initialize(StorageData, ChestOrigin, ChestOwnership)
    - _Requirements: 7.1, 7.2_

- [ ] 12. Final Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户。
