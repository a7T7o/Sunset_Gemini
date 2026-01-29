# 1_入门工作 - 开发记忆

## 模块概述

Phase 2 数据核心重构的入门工作，建立稳固的数据地基。

## 当前状态

- **完成度**: 80%
- **最后更新**: 2026-01-28
- **状态**: 进行中（待用户验收）

## 会话记录

### 会话 1 - 2026-01-28

**锐评来源**: 锐评001

**用户需求**:
> 1_入门工作/锐评001.md，来吧走起

**任务目标**:
1. 创建 `InventoryItem` 类 - 支持动态属性的物品实例
2. 创建 `SaveDataDTOs` - 纯数据类用于 JSON 序列化
3. 创建 `IPersistentObject` 接口 - 所有需要存档的物体实现

**完成状态**: ✅ 完成

---

### 会话 2 - 2026-01-28

**锐评来源**: 锐评002

**用户需求**:
> 锐评002.md，来吧走起

**任务目标**:
1. 实现 `PersistentObjectRegistry` - 持久化对象注册中心
2. 重写 `ChestController` - 实现 IPersistentObject，使用 InventoryItem
3. 实现 `SaveManager` MVP - 存档/读档核心功能

**执行模式**: Aggressive（侵略性迭代）

**完成内容**:
1. ✅ `PersistentObjectRegistry.cs` - 单例注册中心
   - Dictionary<string, IPersistentObject> 管理所有对象
   - 按类型分组快速查询
   - CollectAllSaveData / RestoreAllFromSaveData 方法

2. ✅ `SaveManager.cs` - MVP 存档管理器
   - SaveGame(slotName) / LoadGame(slotName)
   - 收集游戏时间、玩家、背包、世界对象数据
   - JSON 序列化到 Application.persistentDataPath

3. ✅ `ChestInventoryV2.cs` - 新版箱子库存
   - 内部使用 InventoryItem[] 存储
   - 支持耐久度、附魔等动态属性
   - 提供 ItemStack 兼容接口供旧 UI 使用
   - ToSaveData / LoadFromSaveData 序列化支持

4. ✅ `ChestController.cs` 修改
   - 实现 IPersistentObject 接口
   - 添加 _persistentId 字段（编辑器预生成）
   - 添加 _inventoryV2 字段（V2 库存）
   - 实现 Save() / Load() 方法
   - 在 Start/OnDestroy 中注册/注销到 Registry

**编译状态**: ✅ 通过（4 个无关警告）

**创建文件**:
- `Assets/YYY_Scripts/Data/Core/PersistentObjectRegistry.cs`
- `Assets/YYY_Scripts/Data/Core/SaveManager.cs`
- `Assets/YYY_Scripts/Service/Inventory/ChestInventoryV2.cs`

**修改文件**:
- `Assets/YYY_Scripts/World/Placeable/ChestController.cs`

**遗留问题**:
- [ ] 需要在场景中测试存档/读档功能
- [ ] 需要验证耐久度和附魔属性的保存/恢复
- [ ] TimeManager 需要提供设置接口供读档使用
- [ ] 其他持久化对象（TreeController, CropController）待改造

**当前架构分析**:
- `ItemData` (ScriptableObject) - 物品静态定义，包含 ID、名称、图标等
- `ItemStack` (struct) - 只存储 itemId, quality, amount
- `InventoryService` - 36 格背包，基于 ItemStack 数组

**问题诊断**:
1. **物品实例数据缺失**: ItemStack 只能存"是什么物品"，不能存"这把特定的物品"
2. **世界对象无 GUID**: 箱子、作物等无法在存档/读档时正确关联
3. **事件总线混乱**: 单例调用与事件驱动混用

**设计思路**:

1. **InventoryItem 设计**:
   - 使用 class 而非 struct（需要 null、引用语义、Dictionary）
   - 包含实例 ID（GUID）用于存档关联
   - 支持耐久度系统（currentDurability, maxDurability）
   - 支持动态属性（Dictionary<string, string>）
   - 提供与 ItemStack 的互转方法（兼容旧系统）
   - 有动态属性或耐久度的物品不能堆叠

2. **SaveDataDTOs 设计**:
   - GameSaveData 作为根结构，包含版本号
   - 分离各子系统数据（玩家、背包、世界对象、农田）
   - WorldObjectSaveData 使用 genericData 存储特定对象数据
   - 提供 SaveDataHelper 辅助转换

3. **IPersistentObject 设计**:
   - 接口定义 PersistentId、ObjectType、Save/Load
   - PersistentObjectBase 提供基础实现
   - 支持编辑器预生成 ID
   - IPersistentObjectRegistry 管理注册和查找

**创建文件**:
- `Assets/YYY_Scripts/Data/Core/InventoryItem.cs` - 物品实例数据类
- `Assets/YYY_Scripts/Data/Core/SaveDataDTOs.cs` - 存档数据传输对象
- `Assets/YYY_Scripts/Data/Core/IPersistentObject.cs` - 持久化接口和基类

**编译状态**: ✅ 通过（0 警告）

**遗留问题**:
- [ ] 需要实现 PersistentObjectRegistry 单例
- [ ] 需要将 ChestController、TreeController、CropController 改为继承 PersistentObjectBase
- [ ] 需要实现 SaveManager 来协调存档/读档流程
- [ ] InventoryService 需要升级为使用 InventoryItem（或保持兼容层）
- [ ] OcclusionTransparency 的 TreeController 逻辑需要重写（Phase 1 遗留）

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| InventoryItem 使用 class | 需要 null、引用语义、Dictionary | 2026-01-28 |
| 动态属性用 Dictionary<string, string> | 简单、通用、易序列化 | 2026-01-28 |
| 保留 ItemStack 兼容层 | 渐进式迁移，不破坏现有系统 | 2026-01-28 |
| WorldObjectSaveData 用 genericData | 灵活存储不同类型对象的特有数据 | 2026-01-28 |
| PersistentObjectBase 提供基类 | 减少重复代码，统一 ID 管理 | 2026-01-28 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Data/Items/ItemData.cs` | 物品静态定义（ScriptableObject） |
| `Assets/YYY_Scripts/Service/Inventory/ItemStack.cs` | 当前物品堆叠结构（struct） |
| `Assets/YYY_Scripts/Service/Inventory/InventoryService.cs` | 背包服务 |
| `Assets/YYY_Scripts/Data/Core/InventoryItem.cs` | **新建** - 物品实例数据类 |
| `Assets/YYY_Scripts/Data/Core/SaveDataDTOs.cs` | **新建** - 存档数据传输对象 |
| `Assets/YYY_Scripts/Data/Core/IPersistentObject.cs` | **新建** - 持久化接口和基类 |


### 会话 3 - 2026-01-28

**锐评来源**: 锐评003 + 锐评004

**用户需求**:
> 锐评004.md，来吧走起

**任务目标** (Operation Backpack - 背包行动):
1. Step 1: 数据核心重构 - 创建 PlayerInventoryData，重构 InventoryService
2. Step 2: 存档系统集成 - 更新 SaveManager 的背包数据收集/恢复逻辑
3. Step 3: 临时耐久度显示 - 在 SlotUI 上显示耐久度条
4. Step 4: 闭环验证 - 用户在 Unity 中测试

**执行模式**: 一条龙完成

**完成内容**:

1. ✅ **Step 1: 数据核心重构**
   - `PlayerInventoryData.cs` - 新建纯数据类
     - 内部使用 InventoryItem[] 存储
     - 支持 Hotbar 优先逻辑（前 12 格优先）
     - 支持 Sort 排序（不排序 Hotbar）
     - 提供 ItemStack 兼容接口
     - ToSaveData / LoadFromSaveData 序列化支持
   - `InventoryService.cs` - 完全重写
     - 实现 IPersistentObject 接口
     - 内部使用 PlayerInventoryData
     - 保留所有旧接口签名（兼容现有 UI）
     - 新增 GetInventoryItem / SetInventoryItem / AddInventoryItem API
     - 支持旧数据迁移（_legacySlots）

2. ✅ **Step 2: 存档系统集成**
   - `SaveManager.cs` - 更新
     - CollectInventoryData 方法：从 InventoryService 收集背包数据
     - RestoreInventoryData 方法：恢复背包数据到 InventoryService
     - 背包数据通过 IPersistentObject 接口序列化

3. ✅ **Step 3: 临时耐久度显示**
   - `InventorySlotUI.cs` - 添加耐久度条
     - CreateDurabilityBar() - 代码动态生成耐久度条（无需美术资源）
     - UpdateDurabilityBar() - 根据 InventoryItem 更新显示
     - 颜色渐变：绿色(100%) → 黄色(50%) → 红色(0%)
   - `ToolbarSlotUI.cs` - 添加耐久度条
     - 与 InventorySlotUI 相同的实现

4. ⏳ **Step 4: 闭环验证** - 待用户测试
   - 捡起工具 → 消耗耐久 → 看到 UI 上的绿条变短
   - 点击保存 → 停止游戏
   - 启动游戏 → 点击加载 → 绿条必须依然是短的

**编译状态**: ✅ 通过（0 错误 0 警告）

**创建文件**:
- `Assets/YYY_Scripts/Data/Core/PlayerInventoryData.cs`

**修改文件**:
- `Assets/YYY_Scripts/Service/Inventory/InventoryService.cs` - 完全重写
- `Assets/YYY_Scripts/Data/Core/SaveManager.cs` - 更新背包数据收集/恢复
- `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs` - 添加耐久度条
- `Assets/YYY_Scripts/UI/Toolbar/ToolbarSlotUI.cs` - 添加耐久度条

**遗留问题**:
- [ ] 需要用户在 Unity 中测试 Step 4 闭环验证
- [ ] 需要验证耐久度消耗逻辑（工具使用时调用 UseDurability）
- [ ] 需要验证存档/读档后耐久度是否正确恢复

---

### 会话 3 补充 - 2026-01-28

**用户反馈**:
> 1、首先我没有看到保存游戏的按钮
> 2、对于耐久显示我没看到，消耗不会显示
> 3、我不知道如何触发手动测试和读档，你没有给我配置指南呀

**问题诊断**:
- 漏掉了用户体验部分：没有提供运行时的保存/加载按钮
- 没有提供测试耐久度的方法
- 没有提供配置指南

**补充内容**:

1. ✅ **SaveLoadDebugUI.cs** - 存档/读档调试 UI
   - 左上角显示保存/加载按钮
   - 快捷键：F5 保存，F9 加载
   - 显示当前状态

2. ✅ **DurabilityTestUI.cs** - 耐久度测试 UI
   - "给工具添加耐久度"按钮
   - "消耗耐久度"按钮（快捷键 U）
   - 实时显示当前工具的耐久度状态

3. ✅ **InventoryService.cs** - 添加刷新方法
   - `RefreshSlot(int index)` - 刷新指定槽位
   - `RefreshAll()` - 刷新所有槽位

4. ✅ **验收指南.md** - 完整的配置和测试指南

**创建文件**:
- `Assets/YYY_Scripts/UI/Debug/SaveLoadDebugUI.cs`
- `Assets/YYY_Scripts/UI/Debug/DurabilityTestUI.cs`
- `.kiro/specs/999_全面重构_26.01.27/1_入门工作/验收指南.md`

**修改文件**:
- `Assets/YYY_Scripts/Service/Inventory/InventoryService.cs` - 添加 RefreshSlot/RefreshAll 方法

**编译状态**: ✅ 通过（0 错误 4 无关警告）

---

### 会话 4 - 2026-01-28

**用户反馈**:
> 1. 调试 UI 位置不对，需要放在屏幕左下角
> 2. 存档路径需要改到 Assets/Save 目录

**问题诊断**:
- 调试 UI 使用了错误的锚点方式，导致位置计算错误
- 存档路径使用 Application.persistentDataPath（C盘），用户希望放在项目目录

**修复内容**:

1. ✅ **SaveLoadDebugUI.cs** - 重写 UI 位置逻辑
   - 使用左下角锚点 `anchorMin/Max = (0, 0)`
   - `panelOffset = (120, 80)` 从左下角偏移
   - 使用 CanvasScaler.ScaleWithScreenSize，参考分辨率 1920x1080

2. ✅ **DurabilityTestUI.cs** - 重写 UI 位置逻辑
   - 使用左下角锚点 `anchorMin/Max = (0, 0)`
   - `panelOffset = (350, 80)` 从左下角偏移，在 SaveLoad 面板右边
   - 使用 CanvasScaler.ScaleWithScreenSize，参考分辨率 1920x1080

3. ✅ **SaveManager.cs** - 修改存档路径
   - 编辑器模式：`Assets/Save/`
   - 打包后：`{游戏根目录}/Save/`

**修改文件**:
- `Assets/YYY_Scripts/UI/Debug/SaveLoadDebugUI.cs` - 重写 UI 位置
- `Assets/YYY_Scripts/UI/Debug/DurabilityTestUI.cs` - 重写 UI 位置
- `Assets/YYY_Scripts/Data/Core/SaveManager.cs` - 修改存档路径

**编译状态**: ✅ 通过（0 错误 4 无关警告）

**存档路径**:
- 编辑器模式：`{项目路径}/Assets/Save/slot1.json`
- 打包后：`{游戏根目录}/Save/slot1.json`

---
