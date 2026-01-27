# 实现计划 - 放置系统与背包联动

## 任务列表

- [x] 1. 添加服务引用和初始化
  - 在 PlacementManagerV3 中添加 InventoryService、HotbarSelectionService、PackagePanelTabsUI 引用
  - 在 Start() 中自动查找这些服务
  - _Requirements: 1.1, 2.1_

- [x] 2. 实现 PlacementSnapshot 结构体
  - 创建 PlacementSnapshot 结构体
  - 添加 itemId、quality、slotIndex、lockedPosition、isValid 字段
  - 添加 Invalid 静态属性
  - _Requirements: 2.1, 2.2_

- [x] 3. 实现快照创建和验证
  - [x] 3.1 实现 CreateSnapshot() 方法
    - 在 LockPreviewPosition() 中调用
    - _Requirements: 2.1_
  - [x] 3.2 实现 ValidateSnapshot() 方法
    - 检查槽位物品是否与快照匹配
    - _Requirements: 2.2, 2.3_

- [x] 4. 实现中断检测和处理
  - [x] 4.1 实现 CheckInterruptConditions() 方法
    - 检测背包打开、快照失效等条件
    - _Requirements: 4.1, 7.1_
  - [x] 4.2 实现 HandleInterrupt() 方法
    - 取消导航、清除快照、返回 Preview 状态
    - _Requirements: 3.1, 4.1, 6.1_
  - [x] 4.3 在 Update() 的 Locked/Navigating 状态中调用中断检测
    - _Requirements: 3.1, 4.1, 6.1, 7.1_

- [x] 5. 实现手持物品切换中断
  - [x] 5.1 订阅 HotbarSelectionService.OnSelectedChanged 事件
    - _Requirements: 3.1_
  - [x] 5.2 实现 OnHotbarSelectionChanged() 回调
    - 中断放置并退出放置模式
    - _Requirements: 3.1, 3.2, 3.3_

- [x] 6. 修改 ExecutePlacement() 方法
  - [x] 6.1 在执行前验证快照有效性
    - 无效则取消放置
    - _Requirements: 2.3_
  - [x] 6.2 修改 DeductItemFromInventory() 使用快照数据
    - 调用 InventoryService.RemoveFromSlot(slotIndex, 1)
    - _Requirements: 1.1_
  - [x] 6.3 修改 HasMoreItems() 使用快照数据
    - 检查槽位剩余数量
    - _Requirements: 8.1, 8.2_

- [x] 7. 修改 OnLeftClick() 处理 Navigating 状态
  - 再次点击有效位置时重新锁定
  - 更新快照数据
  - _Requirements: 5.1, 5.2, 5.3_

- [x] 8. 清理和优化
  - [x] 8.1 移除旧的 TODO 注释
  - [x] 8.2 添加必要的调试日志（可通过 showDebugInfo 控制）
  - [x] 8.3 确保所有状态转换正确清理快照

- [x] 9. Checkpoint - 确保所有测试通过
  - 编译通过（0 errors, 0 warnings）
  - 待用户测试验证

## 完成状态

✅ 所有任务已完成，代码已实现并编译通过。

待用户测试验证：
- 放置成功后背包物品数量 -1
- 放置失败不扣除物品
- 手持物品切换中断放置
- 背包打开中断放置
- 连续放置功能
- 撤销功能返还物品
