# 背包交互系统 V3 - 任务列表

## 版本信息

- **版本**: v3.0
- **日期**: 2026-01-06
- **状态**: ✅ 代码实现完成，等待测试

---

## 实施计划

- [x] 1. 清理战场 - 删除 V2 的错误代码
  - [x] 1.1 删除 InventoryInteractionManager 中的 Update + Raycast 代码
    - ✅ 删除了所有 Raycast 检测逻辑
    - ✅ 删除了 `GetSlotUnderMouse()` 方法
    - ✅ 删除了 `raycastResults` 缓存字段
    - ✅ 保留状态机枚举、物品操作逻辑、数据层交互
    - _Requirements: 设计文档 V3 - 删除的内容_

  - [x] 1.2 添加 Manager 单例模式
    - ✅ 添加 `public static InventoryInteractionManager Instance { get; private set; }`
    - ✅ 在 `Awake()` 中设置 `Instance = this`
    - ✅ 在 `OnDestroy()` 中清理 `Instance = null`
    - _Requirements: 设计文档 V3 - SlotUI 需要访问 Manager_

- [ ] 2. 验证基础连接 - 确保 Unity 事件系统正常工作
  - [ ] 2.1 在 Unity 中测试点击槽位
    - 运行游戏，点击背包格子
    - 确认控制台输出 `[HeldItemDisplay] 显示物品` 日志
    - _Requirements: 设计文档 V3 - 第二步验证基础连接_

  - [ ] 2.2 验证 Toggle 组件不干扰事件
    - 确认 Toggle 的 `Interactable` 为 true
    - 确认槽位上的 Image 组件 `Raycast Target` 为 true
    - 点击格子后 Toggle 选中状态正常切换
    - _Requirements: 设计文档 V3 - 与 Toggle 的和谐共存_

- [ ] 3. Checkpoint - 基础连接验证
  - 确保点击槽位能触发事件（不再有 "Raycast 命中 0 个对象" 问题）
  - 如果不能触发，检查 Toggle/Image 的 Raycast 设置
  - **这一步通了，后面才有的谈**

- [x] 4. 实现 InventorySlotUI 完整接口
  - [x] 4.1 实现 IPointerDownHandler ✅
  - [x] 4.2 实现 IPointerUpHandler ✅
  - [x] 4.3 实现 IBeginDragHandler ✅
  - [x] 4.4 实现 IDragHandler ✅
  - [x] 4.5 实现 IEndDragHandler ✅
  - [x] 4.6 实现 IDropHandler ✅

- [x] 5. 实现 EquipmentSlotUI 完整接口
  - [x] 5.1 实现所有 6 个接口（与 InventorySlotUI 相同）✅
    - 所有调用传递 `isEquip = true`

- [x] 6. Checkpoint - 接口实现验证
  - ✅ 编译通过，无错误无警告

- [x] 7. 实现 Manager 公共方法 - 基础框架
  - [x] 7.1 添加公共方法签名 ✅
    - `OnSlotPointerDown`, `OnSlotPointerUp`
    - `OnSlotBeginDrag`, `OnSlotDrag`, `OnSlotEndDrag`
    - `OnSlotDrop`
  - [x] 7.2 添加内部状态变量 ✅

- [x] 8. 实现 OnSlotPointerDown 逻辑
  - [x] 8.1 实现修饰键检测 ✅
  - [x] 8.2 实现 Shift+左键 二分拿取 ✅
  - [x] 8.3 实现 Ctrl+左键 单个拿取 ✅
  - [x] 8.4 实现普通按下（无修饰键）✅

- [x] 9. 实现 OnSlotBeginDrag 逻辑
  - [x] 9.1 实现拖拽开始判定 ✅
  - [x] 9.2 实现 StartDrag 方法 ✅

- [x] 10. 实现 OnSlotDrag 逻辑
  - [x] 10.1 HeldItemDisplay 自己在 Update 中跟随鼠标 ✅

- [x] 11. 实现 OnSlotDrop 逻辑
  - [x] 11.1 记录放置目标 ✅

- [x] 12. 实现 OnSlotEndDrag 逻辑
  - [x] 12.1 实现拖拽结束判定 ✅
  - [x] 12.2 实现交换/堆叠逻辑 ✅
  - [x] 12.3 实现面板外丢弃 ✅

- [x] 13. 实现 OnSlotPointerUp 逻辑
  - [x] 13.1 实现 HeldByShift/HeldByCtrl 状态下的放置 ✅
  - [x] 13.2 实现 Shift 连续二分 ✅
  - [x] 13.3 实现 Ctrl 长按持续拿取（协程）✅

- [ ] 14. Checkpoint - 核心功能验证（用户测试）
  - [ ] 测试拖拽交换
  - [ ] 测试 Shift+左键 二分拿取
  - [ ] 测试 Ctrl+左键 单个拿取
  - [ ] 测试面板外丢弃

- [x] 15. 实现快速装备功能
  - [x] 15.1 实现 QuickEquip 方法 ✅
  - [x] 15.2 实现戒指装备策略 ✅
  - [x] 15.3 实现装备槽位限制检查 ✅

- [x] 16. 实现取消和返回功能
  - [x] 16.1 实现 ESC 取消 ✅
  - [x] 16.2 实现 ReturnToSource 方法 ✅

- [x] 17. 实现 HeldItemDisplay 跟随鼠标
  - [x] 17.1 确保 HeldItemDisplay 正确显示 ✅
  - [x] 17.2 实现跟随鼠标（在 Update 中）✅

- [ ] 18. Checkpoint - 完整功能验证（用户测试）
  - [ ] 测试所有交互功能
  - [ ] 测试快速装备
  - [ ] 测试 ESC 取消
  - [ ] 测试 HeldItemDisplay 显示

- [ ] 19. 清理和优化
  - [ ] 19.1 删除调试日志（测试通过后）
  - [ ] 19.2 代码审查

- [x] 20. 更新文档
  - [x] 20.1 更新 memory.md ✅
  - [ ] 20.2 更新验收指南（测试通过后）

- [ ] 21. 最终 Checkpoint - 确保所有测试通过
  - [ ] 运行完整的功能测试
  - [ ] 确保编译通过，无错误无警告
  - [ ] 用户最终验收

---

## 任务依赖关系

```
1 (清理战场)
    ↓
2 (验证基础连接)
    ↓
3 (Checkpoint)
    ↓
4 (InventorySlotUI 接口) ←→ 5 (EquipmentSlotUI 接口)
    ↓
6 (Checkpoint)
    ↓
7 (Manager 公共方法框架)
    ↓
8 (OnSlotPointerDown) → 9 (OnSlotBeginDrag) → 10 (OnSlotDrag)
    ↓                                              ↓
13 (OnSlotPointerUp) ← 11 (OnSlotDrop) ← 12 (OnSlotEndDrag)
    ↓
14 (Checkpoint)
    ↓
15 (快速装备) ←→ 16 (取消和返回) ←→ 17 (HeldItemDisplay)
    ↓
18 (Checkpoint)
    ↓
19 (清理优化) → 20 (更新文档) → 21 (最终 Checkpoint)
```

---

## 相关文件

| 文件 | 操作 |
|------|------|
| `Assets/YYY_Scripts/UI/Inventory/InventoryInteractionManager.cs` | 重写 |
| `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs` | 添加接口 |
| `Assets/YYY_Scripts/UI/Inventory/EquipmentSlotUI.cs` | 添加接口 |
| `Assets/YYY_Scripts/UI/Inventory/HeldItemDisplay.cs` | 保留/微调 |
| `.kiro/specs/UI系统/0_背包交互系统升级/memory.md` | 更新 |
| `.kiro/specs/UI系统/0_背包交互系统升级/验收指南.md` | 更新 |
