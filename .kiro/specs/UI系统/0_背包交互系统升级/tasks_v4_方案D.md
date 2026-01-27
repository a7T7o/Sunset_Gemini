# 背包交互系统 V4 - 方案 D 任务列表

## 版本信息

- **版本**: v4.0 (方案 D)
- **日期**: 2026-01-06
- **状态**: 待实施

---

## 核心原则

**方案 D：分离组件架构 + 状态驱动的接口管理**

- InventorySlotUI - 只负责显示（完全不动）
- InventorySlotInteraction - 只负责交互（新组件）
- InventoryInteractionManager - 只负责逻辑（大脑）

---

## 实施计划

### 阶段 1：创建 Interaction 组件

- [ ] 1. 创建 InventorySlotInteraction 组件
  - [ ] 1.1 创建文件 `Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs`
    - 实现 6 个 Unity 原生接口
    - 添加状态标志（isDragEnabled, pressTime, pressPosition）
    - 添加 Bind() 方法关联 SlotUI
    - _Requirements: design_v4_方案D.md - Interaction 组件_

  - [ ] 1.2 实现 OnPointerDown 逻辑
    - 记录按下时间和位置
    - 转发给 Manager（处理 Shift/Ctrl）
    - _Requirements: design_v4_方案D.md - 点击流程_

  - [ ] 1.3 实现 OnBeginDrag 逻辑
    - 检查时间阈值（0.15 秒）
    - 检查移动阈值（5 像素）
    - 满足条件时启用拖拽（isDragEnabled = true）
    - _Requirements: design_v4_方案D.md - 拖拽流程_

  - [ ] 1.4 实现 OnDrag 逻辑
    - 只有 isDragEnabled = true 时才响应
    - 转发给 Manager
    - _Requirements: design_v4_方案D.md - 拖拽流程_

  - [ ] 1.5 实现 OnEndDrag 逻辑
    - 只有 isDragEnabled = true 时才响应
    - 转发给 Manager
    - 恢复默认状态（isDragEnabled = false）
    - _Requirements: design_v4_方案D.md - 拖拽流程_

  - [ ] 1.6 实现 OnPointerUp 逻辑
    - 如果 isDragEnabled = false，说明是点击
    - 转发给 Manager（处理 Shift/Ctrl 放置）
    - _Requirements: design_v4_方案D.md - 点击流程_

  - [ ] 1.7 实现 OnDrop 逻辑
    - 转发给 Manager
    - _Requirements: design_v4_方案D.md - 拖拽流程_

- [ ] 2. Checkpoint - Interaction 组件编译验证
  - 确保编译通过，无错误无警告
  - 确认所有接口实现正确

### 阶段 2：创建 Manager

- [ ] 3. 创建 InventoryInteractionManager 组件
  - [ ] 3.1 创建文件 `Assets/YYY_Scripts/UI/Inventory/InventoryInteractionManager.cs`
    - 实现单例模式
    - 添加状态枚举（None/Dragging/HeldByShift/HeldByCtrl）
    - 添加配置参数
    - 添加引用字段
    - _Requirements: design_v4_方案D.md - Manager 组件_

  - [ ] 3.2 实现公共方法签名
    - `OnSlotPointerDown(int index, bool isEquip)`
    - `OnSlotPointerUp(int index, bool isEquip)`
    - `OnSlotBeginDrag(int index, bool isEquip, PointerEventData)`
    - `OnSlotDrag(PointerEventData)`
    - `OnSlotEndDrag(PointerEventData)`
    - `OnSlotDrop(int index, bool isEquip)`
    - `Cancel()` - 取消当前操作
    - `IsHolding` - 是否正在拿取物品
    - _Requirements: design_v4_方案D.md - Manager 组件_

- [ ] 4. 实现 OnSlotPointerDown 逻辑
  - [ ] 4.1 实现修饰键检测
    - 检测 Shift/Ctrl 键
    - Shift+Ctrl 同时按下不触发
    - _Requirements: design_v4_方案D.md - 点击流程_

  - [ ] 4.2 实现 Shift+左键 二分拿取
    - 拿起 floor(N/2)
    - 原槽位保留 N - floor(N/2)
    - 显示 HeldItemDisplay
    - 设置状态为 HeldByShift
    - _Requirements: design_v4_方案D.md - Shift 逻辑_

  - [ ] 4.3 实现 Ctrl+左键 单个拿取
    - 检查是否为装备类物品（快速装备）
    - 非装备：拿起 1 个
    - 显示 HeldItemDisplay
    - 设置状态为 HeldByCtrl
    - 启动长按持续拿取协程
    - _Requirements: design_v4_方案D.md - Ctrl 逻辑_

  - [ ] 4.4 实现普通按下（无修饰键）
    - 不拿起物品（仅记录状态）
    - 等待拖拽或松开
    - _Requirements: design_v4_方案D.md - 点击流程_

- [ ] 5. 实现 OnSlotBeginDrag 逻辑
  - [ ] 5.1 实现拖拽开始判定
    - 检查是否已经在 Held 状态（不进入拖拽）
    - 检查槽位是否有物品
    - _Requirements: design_v4_方案D.md - 拖拽流程_

  - [ ] 5.2 实现 StartDrag 方法
    - 保存源信息（sourceIndex, sourceIsEquipment）
    - 拿起物品（heldItem）
    - 清空源槽位
    - 显示 HeldItemDisplay
    - 设置状态为 Dragging
    - _Requirements: design_v4_方案D.md - 拖拽流程_

- [ ] 6. 实现 OnSlotDrag 逻辑
  - HeldItemDisplay 自己在 Update 中跟随鼠标
  - Manager 不需要额外处理
  - _Requirements: design_v4_方案D.md - 拖拽流程_

- [ ] 7. 实现 OnSlotDrop 逻辑
  - 记录放置目标（dropTargetIndex, dropTargetIsEquip）
  - 供 OnEndDrag 使用
  - _Requirements: design_v4_方案D.md - 拖拽流程_

- [ ] 8. 实现 OnSlotEndDrag 逻辑
  - [ ] 8.1 实现拖拽结束判定
    - 检查是否有放置目标（由 OnDrop 设置）
    - 检查是否在面板外
    - 检查是否在垃圾桶上
    - _Requirements: design_v4_方案D.md - 拖拽流程_

  - [ ] 8.2 实现交换/堆叠逻辑
    - 检查装备槽位限制
    - 检查是否可以堆叠
    - 执行交换或堆叠
    - _Requirements: design_v4_方案D.md - 拖拽流程_

  - [ ] 8.3 实现面板外丢弃
    - 在玩家位置生成掉落物
    - 设置丢弃冷却（5 秒）
    - _Requirements: design_v4_方案D.md - 拖拽流程_

- [ ] 9. 实现 OnSlotPointerUp 逻辑
  - [ ] 9.1 实现 HeldByShift 状态下的放置
    - 检查是否为连续二分（Shift 仍按住 + 同一槽位）
    - 否则尝试放置
    - _Requirements: design_v4_方案D.md - Shift 逻辑_

  - [ ] 9.2 实现 Shift 连续二分
    - 手上数量减半
    - 返回原槽位
    - 更新 HeldItemDisplay
    - _Requirements: design_v4_方案D.md - Shift 逻辑_

  - [ ] 9.3 实现 HeldByCtrl 状态下的放置
    - 停止长按持续拿取协程
    - 尝试放置
    - _Requirements: design_v4_方案D.md - Ctrl 逻辑_

  - [ ] 9.4 实现 Ctrl 长按持续拿取（协程）
    - 检查是否仍按住 Ctrl 和鼠标左键
    - 每 1/3.5 秒拿取 1 个
    - 更新 HeldItemDisplay
    - _Requirements: design_v4_方案D.md - Ctrl 逻辑_

- [ ] 10. Checkpoint - Manager 编译验证
  - 确保编译通过，无错误无警告
  - 确认所有方法实现正确

### 阶段 3：实现辅助功能

- [ ] 11. 实现快速装备功能
  - [ ] 11.1 实现 QuickEquip 方法
    - 检查物品类型
    - 获取目标装备槽位
    - 执行装备或交换
    - _Requirements: design_v4_方案D.md - 快速装备_

  - [ ] 11.2 实现戒指装备策略
    - 优先空槽位（槽位 4 > 槽位 5）
    - 交替替换（记录 lastReplacedRingSlot）
    - _Requirements: design_v4_方案D.md - 快速装备_

  - [ ] 11.3 实现装备槽位限制检查
    - `CanPlaceInEquipmentSlot(ItemStack, int slotIndex)`
    - 检查类型匹配
    - _Requirements: design_v4_方案D.md - 快速装备_

- [ ] 12. 实现取消和返回功能
  - [ ] 12.1 实现 ESC 取消
    - 在 Update 中检测 ESC 键
    - 调用 ReturnToSource()
    - _Requirements: design_v4_方案D.md - 取消功能_

  - [ ] 12.2 实现 ReturnToSource 方法
    - 停止 Ctrl 长按协程
    - 返回物品到原槽位
    - 如果原位被占用，尝试找空位
    - 无空位则丢弃
    - _Requirements: design_v4_方案D.md - 返回功能_

- [ ] 13. 实现 HeldItemDisplay 跟随鼠标
  - [ ] 13.1 确保 HeldItemDisplay 正确显示
    - 显示物品图标
    - 显示数量
    - _Requirements: design_v4_方案D.md - HeldItemDisplay_

  - [ ] 13.2 实现跟随鼠标（在 Update 中）
    - 设置位置为鼠标位置
    - _Requirements: design_v4_方案D.md - HeldItemDisplay_

- [ ] 14. 实现背包整理功能（V2 遗漏）
  - [ ] 14.1 确认 InventorySortService 存在
    - 检查文件是否存在
    - 如果不存在，创建 InventorySortService.cs
    - _Requirements: V2 tasks - Phase 8_

  - [ ] 14.2 实现合并相同物品
    - 遍历背包，合并相同 itemId 和 quality 的物品
    - _Requirements: V2 tasks - 7.2, 7.4_

  - [ ] 14.3 实现按优先级排序
    - 工具 > 可放置物品 > 消耗品 > 其他
    - 同类按 itemId 升序
    - _Requirements: V2 tasks - 7.2, 7.4_

  - [ ] 14.4 整理范围限制
    - 只排序 Up 区域（0-35）
    - 不影响 Down 区域（装备栏）
    - _Requirements: V2 tasks - 7.1, 7.3_

- [ ] 15. 实现 PackagePanelTabsUI 取消交互（V2 遗漏）
  - [ ] 15.1 恢复 CancelInteractionIfNeeded 方法
    - 取消注释
    - 调用 InventoryInteractionManager.Instance.Cancel()
    - _Requirements: V2 tasks - 输入场景矩阵修复_

  - [ ] 15.2 在 ClosePanel 中调用
    - 关闭面板时取消拿取状态
    - _Requirements: V2 tasks - 输入场景矩阵修复_

  - [ ] 15.3 在 OpenOrToggle 中调用
    - 切换页面时取消拿取状态
    - _Requirements: V2 tasks - 输入场景矩阵修复_

- [ ] 16. Checkpoint - 辅助功能验证
  - 确保编译通过，无错误无警告
  - 确认所有辅助功能实现正确

### 阶段 4：集成到 SlotUI

- [ ] 17. 集成 Interaction 到 InventorySlotUI
  - [ ] 17.1 在 Awake() 中自动添加 Interaction 组件
    - `var interaction = gameObject.AddComponent<InventorySlotInteraction>();`
    - `interaction.Bind(this, false);`
    - _Requirements: design_v4_方案D.md - 集成_

  - [ ] 17.2 确保 Index 属性可访问
    - `public int Index => index;`
    - _Requirements: design_v4_方案D.md - 集成_

- [ ] 18. 集成 Interaction 到 EquipmentSlotUI
  - [ ] 18.1 在 Awake() 中自动添加 Interaction 组件
    - `var interaction = gameObject.AddComponent<InventorySlotInteraction>();`
    - `interaction.Bind(this, true);`
    - _Requirements: design_v4_方案D.md - 集成_

  - [ ] 18.2 确保 Index 属性可访问
    - `public int Index => index;`
    - _Requirements: design_v4_方案D.md - 集成_

- [ ] 19. Checkpoint - 集成验证
  - 确保编译通过，无错误无警告
  - 确认 Interaction 组件正确添加

### 阶段 5：Unity 测试验证

- [ ] 20. 测试基础连接
  - [ ] 20.1 在 Unity 中测试点击槽位
    - 运行游戏，点击背包格子
    - 确认控制台输出日志
    - _Requirements: design_v4_方案D.md - 测试_

  - [ ] 20.2 验证 Toggle 组件不干扰事件
    - 确认 Toggle 的 `Interactable` 为 true
    - 确认槽位上的 Image 组件 `Raycast Target` 为 true
    - 点击格子后 Toggle 选中状态正常切换
    - _Requirements: design_v4_方案D.md - 测试_

- [ ] 21. Checkpoint - 基础连接验证
  - 确保点击槽位能触发事件
  - 如果不能触发，检查 Toggle/Image 的 Raycast 设置
  - **这一步通了，后面才有的谈**

- [ ] 22. 测试核心功能
  - [ ] 22.1 测试拖拽交换
    - 长按左键拖拽物品
    - 松开后交换位置
    - _Requirements: design_v4_方案D.md - 测试_

  - [ ] 22.2 测试 Shift+左键 二分拿取
    - Shift+左键拿起一半
    - 再次 Shift+左键同一槽位，手上数量再次减半
    - 点击空槽位放置
    - _Requirements: design_v4_方案D.md - 测试_

  - [ ] 22.3 测试 Ctrl+左键 单个拿取
    - Ctrl+左键拿起 1 个
    - 长按持续拿取
    - 点击空槽位放置
    - _Requirements: design_v4_方案D.md - 测试_

  - [ ] 22.4 测试面板外丢弃
    - 拖拽物品到面板外
    - 松开后生成掉落物
    - _Requirements: design_v4_方案D.md - 测试_

- [ ] 23. Checkpoint - 核心功能验证
  - 确保所有核心功能正常工作
  - 如有问题，记录并修复

- [ ] 24. 测试辅助功能
  - [ ] 24.1 测试快速装备
    - Ctrl+左键装备类物品
    - 空槽位直接装备
    - 已有装备交换位置
    - _Requirements: design_v4_方案D.md - 测试_

  - [ ] 24.2 测试 ESC 取消
    - 拿起物品后按 ESC
    - 物品返回原位
    - _Requirements: design_v4_方案D.md - 测试_

  - [ ] 24.3 测试 HeldItemDisplay 显示
    - 拿起物品后显示图标和数量
    - 跟随鼠标移动
    - _Requirements: design_v4_方案D.md - 测试_

  - [ ] 24.4 测试背包整理
    - 点击整理按钮
    - 相同物品合并
    - 按优先级排序
    - _Requirements: V2 tasks - Phase 8_

- [ ] 25. Checkpoint - 完整功能验证
  - 确保所有功能正常工作
  - 确保没有 bug 或异常

### 阶段 6：清理和优化

- [ ] 26. 清理和优化
  - [ ] 26.1 删除调试日志（测试通过后）
    - 移除所有 Debug.Log
    - _Requirements: design_v4_方案D.md - 清理_

  - [ ] 26.2 代码审查
    - 检查代码规范
    - 检查注释完整性
    - _Requirements: design_v4_方案D.md - 清理_

- [ ] 27. 更新文档
  - [ ] 27.1 更新 memory.md
    - 记录方案 D 的实施过程
    - 记录关键决策
    - _Requirements: design_v4_方案D.md - 文档_

  - [ ] 27.2 更新验收指南
    - 更新测试步骤
    - 更新预期结果
    - _Requirements: design_v4_方案D.md - 文档_

- [ ] 28. 最终 Checkpoint - 确保所有测试通过
  - 运行完整的功能测试
  - 确保编译通过，无错误无警告
  - 用户最终验收

---

## 任务依赖关系

```
1 (创建 Interaction 组件)
    ↓
2 (Checkpoint)
    ↓
3 (创建 Manager)
    ↓
4-9 (实现 Manager 方法)
    ↓
10 (Checkpoint)
    ↓
11-16 (实现辅助功能 + V2 遗漏任务)
    ↓
17-18 (集成到 SlotUI)
    ↓
19 (Checkpoint)
    ↓
20-21 (测试基础连接)
    ↓
22-23 (测试核心功能)
    ↓
24-25 (测试辅助功能)
    ↓
26-27 (清理和优化)
    ↓
28 (最终 Checkpoint)
```

---

## 相关文件

| 文件 | 操作 |
|------|------|
| `Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs` | 新建 |
| `Assets/YYY_Scripts/UI/Inventory/InventoryInteractionManager.cs` | 新建 |
| `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs` | 微调（添加 Interaction 组件） |
| `Assets/YYY_Scripts/UI/Inventory/EquipmentSlotUI.cs` | 微调（添加 Interaction 组件） |
| `Assets/YYY_Scripts/UI/Inventory/HeldItemDisplay.cs` | 保留 |
| `.kiro/specs/UI系统/0_背包交互系统升级/memory.md` | 更新 |
| `.kiro/specs/UI系统/0_背包交互系统升级/验收指南.md` | 更新 |

---

## 注意事项

1. **不要修改 SlotUI 的显示逻辑** - 只添加 Interaction 组件
2. **状态标志是关键** - `isDragEnabled` 控制接口行为
3. **阈值可调整** - 在 Manager 中暴露配置参数
4. **测试要全面** - 每个 Checkpoint 都要认真测试
5. **保留调试日志** - 测试通过后再删除
