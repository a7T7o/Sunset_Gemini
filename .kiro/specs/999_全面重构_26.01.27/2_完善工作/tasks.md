# 2_完善工作 - 任务列表

## 概述

本任务列表按优先级排序，P0 为阻断性 Bug 必须优先修复。

---

## P0 - 阻断性 Bug 修复

### 1. 修复箱子存档丢失 (P0-1)

- [x] 1.1 检查并修复 ChestInventoryV2.ToSaveData()
  - 确保遍历所有槽位时调用 `item.PrepareForSerialization()`
  - 确保空槽位也被正确序列化（保持索引一致性）
  - 添加调试日志验证序列化数据
  - 注：根因是 _inventory 和 _inventoryV2 数据不同步

- [x] 1.2 检查并修复 ChestController.Save()
  - 确保优先使用 `_inventoryV2.ToSaveData()`
  - 添加调试日志输出保存的槽位数量
  - 添加 SyncInventoryToV2() 保存前同步数据

- [x] 1.3 检查并修复 ChestController.Load()
  - 确保正确调用 `_inventoryV2.LoadFromSaveData()`
  - 添加调试日志输出恢复的槽位数量
  - 添加 SyncV2ToInventory() 加载后同步数据

- [ ] 1.4 验证存档文件内容
  - 保存后检查 JSON 文件中箱子数据是否正确
  - 确认 `worldObjects` 数组包含箱子数据

### 2. 修复 BoxUI Up 区域交互 (P0-2)

- [x] 2.1 检查 BoxPanelUI.BindChestSlotData()
  - 确保使用 `slot.BindContainer(chestInventory, index)` 绑定
  - 确保 `chestInventory` 不为 null
  - 绑定后调用 `slot.Refresh()`

- [x] 2.2 检查 ChestInventory 是否实现 IItemContainer
  - 确认 `GetSlot()`, `SetSlot()`, `OnSlotChanged` 等接口正确实现
  - 确认 `Database` 属性返回正确的 ItemDatabase

- [x] 2.3 检查 InventorySlotUI.BindContainer()
  - 确保 `container` 字段被正确赋值
  - 确保事件订阅正确

- [x] 2.4 添加调试日志验证绑定
  - 在 `BindChestSlotData` 中输出绑定信息
  - 在 `InventorySlotUI.BindContainer` 中输出容器类型

### 3. 修复 Ctrl/Shift + 左键吞物品 (P0-3)

- [x] 3.1 审查 InventorySlotInteraction 的拆分逻辑
  - 检查 Ctrl + 左键拆分时的数量计算
  - 检查 Shift + 左键拆分时的数量计算
  - 确保原槽位数量正确减少
  - **根因**：HandleSameContainerDrop 中 sourceIndex == targetIndex 时直接覆盖而非合并

- [x] 3.2 修复 HandleSameContainerDrop 方法
  - 当 sourceIndex == targetIndex 时，应该合并物品而非覆盖
  - 修复后：先获取当前槽位内容，再合并数量

- [ ] 3.3 验证修复效果
  - Ctrl + 左键拿起 1 个，点击放回同一格子，验证数量正确合并
  - Shift + 左键拿起一半，点击放回同一格子，验证数量正确合并

---

## P1+ - 安全性问题修复

### 4. 实现物品归位逻辑 (P1+-1)

- [x] 4.1 在 InventoryInteractionManager 中添加 ReturnHeldItemToInventory() 方法
  - 实现优先级：原槽位 → 背包空位 → 扔在脚下
  - 添加 DropItemAtPlayerFeet() 辅助方法
  - 添加调试日志

- [x] 4.2 修改 BoxPanelUI.Close()
  - 关闭前调用 ReturnHeldItemToInventory()
  - 确保手持物品不丢失

- [x] 4.3 修改 PackagePanelTabsUI.Close()
  - 关闭前调用 ReturnHeldItemToInventory()
  - 确保手持物品不丢失

- [ ] 4.4 验证物品归位逻辑
  - 测试手持物品按 ESC 关闭 UI
  - 测试原槽位已满时放入背包
  - 测试背包满时扔在脚下

### 5. 实现 UI 状态切换矩阵 (P1+-2)

- [x] 5.1 修改 PackagePanelTabsUI.OnTabPressed()
  - 如果 BoxUI 打开中，先关闭 BoxUI 再打开 InventoryUI
  - 切换时强制刷新背包数据
  - 切换前处理手持物品

- [x] 5.2 修改 GameInputManager.OnInteract()
  - 如果 InventoryUI 打开中，先关闭再触发场景交互
  - 确保 E 键行为符合交互矩阵
  - 注：右键交互已实现 Box 关闭逻辑

- [ ] 5.3 验证 UI 状态切换
  - 测试 BoxUI → TAB → InventoryUI
  - 测试 InventoryUI → E → BoxUI（面前有箱子）
  - 测试切换后数据是否最新

---

## P1 - 重要问题修复

### 6. 修复背包 UI 不刷新 (P1-1)

- [x] 6.1 修改 InventoryPanelUI
  - 在 `OnEnable()` 中添加 `RefreshAll()` 调用
  - 确保每次打开面板都刷新数据

- [ ] 6.2 修改 BoxPanelUI.Close()
  - 关闭时触发背包刷新
  - 可以通过 `InventoryService.RaiseInventoryChanged()` 或直接调用 `RefreshAll()`

- [ ] 6.3 验证刷新机制
  - 测试从箱子操作后按 Tab 打开背包
  - 测试在 BoxUI 排序后按 Tab 打开背包

### 7. 修复游戏时间恢复 (P1-2)

- [x] 7.1 修改 TimeManager.cs
  - 添加 `SetTime(int day, int season, int year, int hour, int minute)` 方法
  - 在方法中更新所有时间字段
  - 触发时间变化事件
  - 注：TimeManager.SetTime() 已存在

- [x] 7.2 修改 SaveManager.CollectGameTimeData()
  - 从 TimeManager 获取当前时间数据
  - 确保所有时间字段都被保存

- [x] 7.3 修改 SaveManager.RestoreGameTimeData()
  - 调用 `TimeManager.SetTime()` 恢复时间
  - 添加调试日志

- [ ] 7.4 验证时间恢复
  - 保存游戏，记录当前时间
  - 加载游戏，验证时间是否恢复

---

## P2 - 优化改进

### 8. 耐久度条样式优化 (P2-1)

- [x] 8.1 修改 InventorySlotUI.CreateDurabilityBar()
  - 调整位置：距离底部 6px
  - 调整宽度：左右各留出适当空隙（贴着 4px 边框）
  - 添加 1px 黑色描边（使用背景条实现）

- [x] 8.2 同步修改 ToolbarSlotUI.CreateDurabilityBar()
  - 保持与 InventorySlotUI 一致的样式

- [ ] 8.3 验证视觉效果
  - 检查耐久度条位置是否正确
  - 检查描边是否清晰可见

---

## 验收测试

### 9. 完整验收测试

- [ ] 9.1 箱子存档测试
  - 放入物品 → 保存 → 关闭游戏 → 重新运行 → 加载 → 验证物品存在

- [ ] 9.2 BoxUI 交互测试
  - Up 区域点击、拖拽测试
  - Ctrl + 左键拆分测试
  - Shift + 左键拆分测试
  - 拆分后放回合并测试

- [ ] 9.3 背包刷新测试
  - 从箱子操作后按 Tab 打开背包
  - 在 BoxUI 排序后按 Tab 打开背包

- [ ] 9.4 时间恢复测试
  - 保存时记录时间
  - 加载后验证时间恢复

- [ ] 9.5 耐久度条视觉测试
  - 检查位置、大小、描边

- [ ] 9.6 物品安全测试（新增）
  - 手持物品按 ESC 关闭 UI，验证物品归位
  - 原槽位已满时，验证物品放入背包
  - 背包满时，验证物品掉落在脚下

- [ ] 9.7 UI 状态切换测试（新增）
  - BoxUI 打开时按 TAB，验证切换到 InventoryUI
  - 切换后验证背包数据是最新的
  - InventoryUI 打开时按 E（面前有箱子），验证切换到 BoxUI

---

## 执行顺序

1. **第一战场**: P0-1 箱子存档 → P0-2 BoxUI 交互 → P0-3 吞物品
2. **第二战场**: P1+-1 物品归位逻辑 → P1+-2 UI 状态切换矩阵
3. **第三战场**: P1-1 背包刷新 → P1-2 时间恢复
4. **第四战场**: P2-1 耐久度条样式
5. **验收**: 完整测试

---

## 注意事项

- 每完成一个任务，更新任务状态为 `[x]`
- 遇到问题时记录到 memory.md
- 修复完 P0 后先进行验收测试，再继续 P1
- 所有修改需要在 Unity 中重新编译验证
