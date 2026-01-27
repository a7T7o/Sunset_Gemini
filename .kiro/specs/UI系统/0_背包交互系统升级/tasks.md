# 实现计划 - 背包交互系统升级

## 🔴🔴🔴 状态：已回滚 - 2026-01-05 🔴🔴🔴

**原因**：实现过程中犯下多个严重错误，导致背包物品完全不显示。

**详情**：请查看 `错误反省.md`

**后续**：需要重新设计和实现，保持原有显示逻辑不变。

---

## 原任务列表（已作废，仅供参考）

- [ ] 1. 创建核心管理器和数据结构





  - [x] 1.1 创建 InventoryDragDropManager 核心类

    - 定义 InteractionState 枚举（None, Dragging, HeldByShift, HeldByCtrl）
    - 实现状态管理和配置参数
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_




  - [x] 1.2 创建 HeldItemDisplay 跟随鼠标显示组件

    - 实现 Show/Hide/UpdateAmount/FollowMouse 方法

    - 使用 UIItemIconScaler 保持图标样式一致
    - _Requirements: 2.7, 11.2, 11.3_


- [x] 2. 实现拖拽交换功能

  - [x] 2.1 扩展 InventorySlotUI 添加拖拽支持


    - 实现 IPointerDownHandler, IPointerUpHandler, IDragHandler

    - 添加长按检测（0.15秒阈值）

    - _Requirements: 1.1, 1.6_
  - [x] 2.2 实现拖拽交换逻辑

    - 实现 StartDrag 和拖拽状态管理

    - 实现松开时的交换/移动逻辑
    - _Requirements: 1.2, 1.3_
  - [x] 2.3 实现拖拽到面板外丢弃

    - 检测松开位置是否在 PackagePanel 内
    - 触发丢弃机制

    - _Requirements: 1.4, 5.6_

- [x] 3. 实现 Shift 二分拿取功能


  - [x] 3.1 实现 Shift+左键 拿起一半

    - 计算 floor(N/2) 拿起数量

    - 更新原槽位剩余数量
    - _Requirements: 2.1_

  - [x] 3.2 实现连续二分逻辑

    - 支持长按 Shift 多次点击
    - 支持松开再按 Shift 点击

    - _Requirements: 2.2, 2.3_
  - [x] 3.3 实现数量为 1 时的边界处理

    - 数量为 1 时不执行操作

    - _Requirements: 2.4_
  - [x] 3.4 实现放置逻辑


    - 点击空槽位放置

    - 点击有物品槽位返回原位
    - _Requirements: 2.5, 2.6_


- [x] 4. 实现 Ctrl 单个拿取功能

  - [x] 4.1 实现 Ctrl+左键 拿起单个

    - 拿起 1 个，原槽位减 1
    - _Requirements: 3.1_
  - [x] 4.2 实现长按持续拿取

    - 可配置速度（默认 3.5 个/秒）
    - 松开左键或 Ctrl 停止
    - _Requirements: 3.2, 3.3, 3.4_
  - [x] 4.3 实现数量为 0 时的边界处理

    - 停止拿取并清空槽位
    - _Requirements: 3.5_
  - [x] 4.4 实现放置逻辑

    - 与 Shift 模式共用放置逻辑
    - _Requirements: 3.6, 3.7_

- [x] 5. 实现键位冲突处理

  - [x] 5.1 实现 Shift+Ctrl 同时按下检测

    - 同时按下时不触发任何操作
    - _Requirements: 4.1_
  - [x] 5.2 实现状态互斥逻辑

    - 拖拽状态禁用 Shift/Ctrl
    - 拿起状态禁用拖拽
    - _Requirements: 4.2, 4.3_
  - [x] 5.3 实现无修饰键点击行为

    - 不按修饰键时不拿起物品
    - _Requirements: 4.4_

- [x] 6. 实现物品丢弃机制



  - [x] 6.1 实现点击面板外丢弃

    - 检测点击位置
    - 在玩家位置生成掉落物
    - _Requirements: 5.1_

  - [x] 6.2 实现垃圾桶丢弃


    - 检测放置到 BT_TrashCan
    - 触发丢弃

    - _Requirements: 5.2, 5.7_
  - [x] 6.3 实现丢弃冷却机制

    - 5 秒冷却时间
    - 离开范围后重新进入可拾取
    - _Requirements: 5.3, 5.4, 5.5_



- [x] 7. 实现掉落物生成冷却

  - [x] 7.1 扩展 WorldItemPickup 添加冷却支持

    - 添加 pickupCooldownEndTime 字段
    - 实现 SetPickupCooldown 和 CanBePickedUp 方法
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 7.2 修改 WorldItemPool 设置生成冷却

    - 生成时设置 1 秒冷却

    - _Requirements: 6.1_
  - [x] 7.3 修改 AutoPickupService 检查冷却

    - 拾取前检查 CanBePickedUp
    - _Requirements: 6.2_

- [x] 8. 实现背包整理功能



  - [x] 8.1 创建 InventorySortService

    - 定义排序优先级枚举
    - 实现 GetItemPriority 方法
    - _Requirements: 7.2_

  - [x] 8.2 实现排序逻辑

    - 工具 > 可放置 > 消耗品 > 其他
    - 同类按 ID 升序

    - _Requirements: 7.2, 7.4_
  - [x] 8.3 实现 BT_Sort 按钮功能

    - 只排序 Up 区域
    - 不影响 Down 区域
    - _Requirements: 7.1, 7.3_

- [x] 9. 实现快速装备功能


  - [x] 9.1 实现 Ctrl+左键 快速装备

    - 检测是否为装备类物品
    - 非装备类执行普通 Ctrl 拿取
    - _Requirements: 8.1, 8.7_
  - [x] 9.2 实现装备到空槽位

    - 移动物品到对应装备槽
    - _Requirements: 8.2_

  - [x] 9.3 实现装备交换


    - 已有装备时交换位置

    - _Requirements: 8.3_
  - [x] 9.4 实现戒指装备策略

    - 优先槽位 4，其次槽位 5
    - 替换最近未被替换的槽位

    - _Requirements: 8.4, 8.5, 8.6_


- [x] 10. 实现装备槽位限制

  - [x] 10.1 实现装备类型检查

    - 定义装备类型与槽位映射
    - _Requirements: 9.4_
  - [x] 10.2 实现放置验证

    - 非装备物品拒绝
    - 类型不匹配拒绝

    - _Requirements: 9.1, 9.2, 9.3_

- [x] 11. 实现槽位清空逻辑

  - [x] 11.1 检查并完善 InventoryService 清空逻辑

    - 数量为 0 时设置 Empty
    - 触发变更事件

    - _Requirements: 10.1, 10.3_
  - [x] 11.2 完善 UI 清空显示

    - 清除图标和数量
    - _Requirements: 10.2_


- [x] 12. 实现 Down 区域交互一致性

  - [x] 12.1 扩展 EquipmentSlotUI 支持拖拽和拿取

    - 复用 InventorySlotUI 的交互逻辑
    - _Requirements: 12.1, 12.2, 12.3_

- [x] 13. Checkpoint - 确保所有测试通过

  - 确保所有测试通过，如有问题请询问用户


- [x] 14. 更新相关工作区 memory

  - [x] 14.1 更新 UI系统 memory.md

  - [x] 14.2 更新 物品掉落拾取 memory.md

- [x] 15. 改进功能实现（2026-01-04）

  - [x] 15.1 添加 EquipmentType 枚举到 ItemEnums.cs

    - 定义装备类型：None, Helmet, Armor, Pants, Shoes, Ring, Accessory
    - _Requirements: 8.1, 9.4_

  - [x] 15.2 添加 ConsumableType 枚举到 ItemEnums.cs
    - 定义消耗品类型：None, Food, Potion, Buff
    - 用于右键使用功能判断

  - [x] 15.3 更新 ItemData 添加装备和消耗品字段
    - 添加 equipmentType 字段
    - 添加 consumableType 字段
    - _Requirements: 8.1, 8.7_

  - [x] 15.4 更新 InventoryDragDropManager 使用新的 EquipmentType
    - 修改 GetEquipmentType 方法直接读取 ItemData.equipmentType
    - _Requirements: 8.1_

  - [x] 15.5 实现堆叠合并功能
    - 拖拽相同物品到已有堆叠时尝试合并
    - 更新 TryPlaceOrSwap 方法
    - _Requirements: 1.2, 1.3_

  - [x] 15.6 添加音效系统接口
    - 添加 SoundType 枚举和 PlaySound 方法
    - 添加音效 AudioClip 字段
    - 预留 OnSoundRequested 事件

  - [x] 15.7 创建 InventorySortService 整理服务
    - 实现合并相同物品功能
    - 实现按优先级排序（工具 > 武器 > 可放置 > 种子 > 消耗品 > 材料 > 其他）
    - 同类按 ID 升序
    - _Requirements: 7.2, 7.4_

  - [x] 15.8 创建 ItemTooltip 物品详情悬浮框
    - 显示物品名称、描述、总价值
    - 根据品质显示不同颜色
    - 跟随鼠标移动

  - [x] 15.9 创建 ItemUseConfirmDialog 使用确认弹窗
    - 右键点击消耗品显示确认弹窗
    - 根据消耗品类型显示"使用"或"食用"
    - 支持 ESC 取消、Enter 确认

  - [x] 15.10 更新 InventorySlotUI 添加悬浮和右键功能
    - 实现 IPointerEnterHandler, IPointerExitHandler
    - 添加悬浮提示延迟显示
    - 添加右键使用消耗品功能
    - 添加拖拽高亮效果

  - [x] 15.11 预留外部 UI 交互接口
    - 添加 OnItemDraggedToExternal 事件
    - 添加 CheckExternalUI 和 TryPlaceToExternalUI 方法

  - [x] 15.12 参数化掉落物冷却
    - WorldItemPool 添加 defaultSpawnCooldown 字段
    - 替换硬编码的 1 秒冷却

  - [x] 15.13 优化 AutoPickupService 性能
    - 使用 Physics2D.OverlapCircle 新 API
    - 添加复用的碰撞检测缓冲区

  - [x] 15.14 更新 SO 设计规则
    - 添加 SO 工具同步规则到 so-design.md
    - 创建编辑器工具工作区

- [x] 16. 最终检查点
  - 确保所有代码编译通过
  - 更新 memory.md




