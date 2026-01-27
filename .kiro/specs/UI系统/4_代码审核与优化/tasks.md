# UI 交互系统代码审核与优化 - 任务列表

**创建日期**: 2026-01-21  
**关联设计**: `design.md`

---

## 任务概览

| 阶段 | 任务数 | 预计工时 | 优先级 |
|------|--------|---------|--------|
| Phase 1: 缓存引用 | 4 | 2h | P0 |
| Phase 2: 互斥检查 | 3 | 1h | P0 |
| Phase 3: 提取丢弃方法 | 4 | 2h | P1 |
| Phase 4: 日志清理 | 3 | 1h | P1 |
| Phase 5: 日志规范修复 | 4 | 1h | P1 |
| Phase 6: 垃圾桶优化 | 3 | 2h | P2 |
| Phase 7: 测试验证 | 3 | 2h | P0 |

---

## Phase 1: 缓存引用（P0）

- [x] 1. Phase 1: 缓存引用
  - [x] 1.1 InventorySlotInteraction 缓存实现
    - 添加 `_cachedInventoryService`、`_cachedPlayer`、`_cachedManager` 字段
    - 添加 `CachedInventoryService`、`CachedPlayer`、`CachedManager` 懒加载属性
    - 替换所有 `FindFirstObjectByType<InventoryService>()` 为 `CachedInventoryService`
    - 替换所有 `FindFirstObjectByType<PlayerController>()` 为 `CachedPlayer`
    - 替换所有 `FindFirstObjectByType<InventoryInteractionManager>()` 为 `CachedManager`
  - [x] 1.2 BoxPanelUI 缓存实现
    - 添加 `_cachedInventoryService`、`_cachedPackagePanel` 字段
    - 添加 `CachedInventoryService`、`CachedPackagePanel` 懒加载属性
    - 替换所有 `FindFirstObjectByType<InventoryService>()` 为 `CachedInventoryService`
    - 替换所有 `FindFirstObjectByType<PackagePanelTabsUI>()` 为 `CachedPackagePanel`
  - [x] 1.3 ChestController 缓存实现
    - 添加 `_cachedPackagePanel`、`_cachedCanvas` 字段
    - 添加 `CachedPackagePanel`、`CachedCanvas` 懒加载属性
    - 替换 `OpenBoxUI()` 中的 `FindFirstObjectByType` 调用
  - [x] 1.4 编译验证
    - 确保所有文件编译通过
    - 确保 Unity 编辑器无报错

---

## Phase 2: 互斥检查（P0）

- [x] 2. Phase 2: 互斥检查
  - [x] 2.1 确保 IsHolding 属性存在
    - 检查 `InventoryInteractionManager` 是否有 `IsHolding` 属性
    - 如果没有，添加 `public bool IsHolding => currentState == InteractionState.HeldByShift || currentState == InteractionState.HeldByCtrl;`
  - [x] 2.2 SlotDragContext 添加互斥检查
    - 在 `Begin()` 方法开头添加检查
    - 如果 `InventoryInteractionManager.Instance?.IsHolding == true`，输出警告并返回
  - [x] 2.3 InventoryInteractionManager 添加互斥检查
    - 在 `ShiftPickup()` 方法开头添加检查
    - 在 `CtrlPickup()` 方法开头添加检查
    - 如果 `SlotDragContext.IsDragging == true`，输出警告并返回

---

## Phase 3: 提取丢弃方法（P1）

- [x] 3. Phase 3: 提取丢弃方法
  - [x] 3.1 创建 ItemDropHelper.cs
    - 在 `Assets/YYY_Scripts/UI/Utility/` 目录下创建文件
    - 实现 `DropAtPlayer(ItemStack item, float cooldown = 5f)` 方法
    - 实现 `DropAtPosition(ItemStack item, Vector3 position, float cooldown = 5f)` 方法
    - 实现 `GetPlayerDropPosition()` 私有方法（使用 Collider 中心）
    - 实现 `SpawnWorldItem()` 私有方法
  - [x] 3.2 修改 InventorySlotInteraction.DropItemFromContext()
    - 替换丢弃逻辑为 `ItemDropHelper.DropAtPlayer(item)`
    - 删除原有的 PlayerController 查找和位置计算代码
  - [x] 3.3 修改 BoxPanelUI.DropItemFromContext()
    - 替换丢弃逻辑为 `ItemDropHelper.DropAtPlayer(item)`
    - 删除原有的 PlayerController 查找和位置计算代码
  - [x] 3.4 修改 InventoryInteractionManager.DropItem()
    - 替换丢弃逻辑为 `ItemDropHelper.DropAtPlayer(heldItem, dropCooldown)`
    - 删除原有的 PlayerController 查找和位置计算代码

---

## Phase 4: 日志清理（P1）

- [x] 4. Phase 4: 日志清理
  - [x] 4.1 清理 InventoryInteractionManager.cs
    - 删除所有 `// Debug.Log(...)` 注释代码
    - 删除所有 `/* Debug.Log(...) */` 注释代码
    - 保留 `if (showDebugInfo)` 控制的日志
  - [x] 4.2 清理 InventorySlotInteraction.cs
    - 删除所有注释掉的日志代码
  - [x] 4.3 清理 BoxPanelUI.cs
    - 删除所有注释掉的日志代码

---

## Phase 5: 日志规范修复（P1）

- [x] 5. Phase 5: 日志规范修复
  - [x] 5.1 HeldItemDisplay 日志开关
    - 添加 `[Header("Debug")] [SerializeField] private bool showDebugInfo = false;` 字段
    - 将 `Show()` 方法中的 `Debug.Log` 包裹在 `if (showDebugInfo)` 中
  - [x] 5.2 BoxPanelUI 静态标志修复
    - 将 `_hasLoggedBindFailure` 从 `static` 改为实例字段
    - 在 `OnEnable()` 中重置为 `false`
  - [x] 5.3 InventoryService.Sort() 日志开关
    - 检查是否有 `showDebugInfo` 字段，如果没有则添加
    - 将 `Sort()` 末尾的 `Debug.Log` 包裹在 `if (showDebugInfo)` 中
  - [x] 5.4 验证日志输出
    - 运行游戏，执行完整交互流程
    - 确保 Console 中无高频日志输出
    - 确保错误情况有明确日志

---

## Phase 6: 垃圾桶优化（P2 - 可选）

- [ ]* 6. Phase 6: 垃圾桶优化
  - [ ]* 6.1 创建 TrashCanRegistry.cs
    - 在 `Assets/YYY_Scripts/UI/Utility/` 目录下创建文件
    - 实现 `Register(RectTransform trashCan)` 方法
    - 实现 `Unregister(RectTransform trashCan)` 方法
    - 实现 `IsOverAnyTrashCan(Vector2 screenPosition)` 方法
    - 实现 `Clear()` 方法
  - [ ]* 6.2 修改垃圾桶组件
    - 在垃圾桶组件的 `OnEnable()` 中调用 `TrashCanRegistry.Register()`
    - 在垃圾桶组件的 `OnDisable()` 中调用 `TrashCanRegistry.Unregister()`
  - [ ]* 6.3 简化 IsOverTrashCan()
    - 修改 `InventorySlotInteraction.IsOverTrashCan()` 使用 `TrashCanRegistry.IsOverAnyTrashCan()`
    - 删除原有的 `FindFirstObjectByType` 查找逻辑

---

## Phase 7: 测试验证（P0）

- [x] 7. Phase 7: 测试验证
  - [ ] 7.1 功能测试
    - 测试背包内拖拽（Down→Down）
    - 测试箱子内拖拽（Up→Up）
    - 测试跨容器拖拽（Up↔Down）
    - 测试 Shift 拿取 + 放置
    - 测试 Ctrl 拿取 + 放置
    - 测试拖拽到垃圾桶
    - 测试拖拽到面板外
    - 测试 ESC 取消
  - [ ] 7.2 互斥检查测试
    - 测试 Manager 持有时尝试 DragContext（应被拒绝）
    - 测试 DragContext 活跃时尝试 Manager（应被拒绝）
    - 验证物品不会丢失
  - [ ] 7.3 性能验证
    - 打开箱子 UI 无明显延迟
    - 拖拽操作流畅
    - Console 中无高频日志输出

---

## 完成标准

### 必须完成（P0 + P1）

- [ ] Phase 1-5 全部任务完成
- [ ] 所有功能测试通过
- [ ] 互斥检查测试通过
- [ ] 编译无错误，Unity 无报错
- [ ] 日志输出符合规范

### 可选完成（P2）

- [ ] Phase 6 垃圾桶优化完成

---

## 注意事项

1. **最小侵入原则**：不改变现有方法签名和外部行为
2. **防御性编程**：所有缓存引用使用前做 null 检查
3. **渐进式实施**：每个 Phase 完成后进行编译验证
4. **回滚准备**：每个 Phase 完成后提交 Git，便于回滚

---

**任务列表完成**
