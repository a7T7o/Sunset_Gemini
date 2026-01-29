# 装备与工具系统任务列表

**创建日期**: 2026-01-29
**更新日期**: 2026-01-29
**任务代号**: Operation Arsenal (军火库行动) - Execute Phase
**决策状态**: 🔒 文档已锁定 → 🔥 代码执行中

---

## 执行计划（三波攻击）

根据锐评003的指令，按以下顺序执行：

| 波次 | 目标 | 任务 | 状态 |
|------|------|------|------|
| 🚀 第一波 | 数据层 (Data Layer) | Task 1: 创建 EquipmentData | ✅ 完成 |
| 🚀 第二波 | 逻辑层 (Service Layer) | Task 2: 重构 EquipmentService | ✅ 完成 |
| 🚀 第三波 | 工具层 (Tool Layer) | Task 3: 扩展批量生成工具 | ✅ 完成 |
| ✅ 验收 | 功能验证 | Task 4: 验证与测试 | ⏳ 待执行 |

---

## 详细任务

### 🚀 第一波：数据层 (Data Layer) - Task 1

**目标**: 建立装备数据的物理基础
**文件**: 新建 `Assets/YYY_Scripts/Data/Items/EquipmentData.cs`

- [x] 1.1 创建 EquipmentData.cs 文件
  - [x] 1.1.1 创建 `Assets/YYY_Scripts/Data/Items/EquipmentData.cs`
  - [x] 1.1.2 继承 `ItemData`
  - [x] 1.1.3 添加 `[CreateAssetMenu]` 特性

- [x] 1.2 添加装备专属字段
  - [x] 1.2.1 `EquipmentType equipmentType` - 装备类型（覆盖或复用基类）
  - [x] 1.2.2 `int defense` - 防御力
  - [x] 1.2.3 `List<StatModifier> attributes` - 属性加成（预留）
  - [x] 1.2.4 `GameObject equipmentModel` - 纸娃娃模型（预留）

- [x] 1.3 创建辅助结构
  - [x] 1.3.1 创建 `StatModifier` 结构体
  - [x] 1.3.2 创建 `StatType` 枚举
  - [x] 1.3.3 创建 `ModifierType` 枚举（合并到 StatModifier 中）

**⚠️ 注意**: 不要动 `ItemData.cs` 文件，防止 meta 数据变动导致资产丢失

---

### 🚀 第二波：逻辑层 (Service Layer) - Task 2 (P0 级)

**目标**: 让装备栏拥有"记忆"
**文件**: 重构 `Assets/YYY_Scripts/Service/Equipment/EquipmentService.cs`

- [x] 2.1 修改 EquipmentService 数据结构
  - [x] 2.1.1 将 `ItemStack[]` 替换为 `InventoryItem[]`
  - [x] 2.1.2 更新 `GetEquip` / `SetEquip` 方法
  - [x] 2.1.3 更新 `ClearEquip` 方法

- [x] 2.2 实现槽位限制（P0 核心）
  - [x] 2.2.1 实现 `CanEquipAt(int slotIndex, ItemData itemData)` 方法
  - [x] 2.2.2 在 `EquipItem` 方法中调用槽位检查
  - [x] 2.2.3 支持 EquipmentData 和 ItemData 两种类型

- [x] 2.3 实现 IPersistentObject 接口
  - [x] 2.3.1 添加 `PersistentId` 属性
  - [x] 2.3.2 实现 `Save()` 方法
  - [x] 2.3.3 实现 `Load()` 方法

- [x] 2.4 创建存档数据结构
  - [x] 2.4.1 创建 `EquipmentSaveData` 类

- [ ] 2.5 更新相关 UI
  - [ ] 2.5.1 更新 `EquipmentSlotUI.Refresh()` 方法
  - [ ] 2.5.2 确保 `InventoryInteractionManager` 兼容新数据结构

---

### 🚀 第三波：工具层 (Tool Layer) - Task 3

**目标**: 让策划能一键生成神装
**文件**: 修改 `Assets/Editor/Tool_BatchItemSOGenerator.cs`

- [x] 3.1 扩展枚举和映射
  - [x] 3.1.1 在 `ItemSOType` 中添加 `EquipmentData`
  - [x] 3.1.2 更新 `CategoryToSubTypes` 映射
  - [x] 3.1.3 更新 `SubTypeNames` 映射
  - [x] 3.1.4 更新 `SubTypeStartIDs` 映射
  - [x] 3.1.5 更新 `SubTypeOutputFolders` 映射

- [x] 3.2 添加装备设置面板
  - [x] 3.2.1 添加 `selectedEquipmentType` 字段
  - [x] 3.2.2 实现 `DrawEquipmentSettings()` 方法
  - [x] 3.2.3 在 `DrawTypeSpecificSettings()` 中调用

- [x] 3.3 实现装备生成逻辑
  - [x] 3.3.1 实现 `CreateEquipmentData()` 方法
  - [x] 3.3.2 在 `CreateItemSO()` 中添加分支
  - [x] 3.3.3 更新 `GetFilePrefix()` 方法
  - [x] 3.3.4 自动设置 equipmentType（策划不需要手动选）

- [ ] 3.4 创建输出目录结构
  - [ ] 3.4.1 创建 `Assets/111_Data/Items/Equipment/` 目录
  - [ ] 3.4.2 创建子目录：Helmets, Armors, Pants, Shoes, Rings, Accessories

---

### ✅ 验收：Task 4

**目标**: 确保装备系统正常工作

- [ ] 4.1 功能验证
  - [ ] 4.1.1 装备物品到装备栏
  - [ ] 4.1.2 验证槽位限制（戒指不能戴头上）
  - [ ] 4.1.3 保存游戏
  - [ ] 4.1.4 重新加载游戏
  - [ ] 4.1.5 验证装备栏数据正确恢复

- [ ] 4.2 边界测试
  - [ ] 4.2.1 空装备栏存档/读档
  - [ ] 4.2.2 满装备栏存档/读档
  - [ ] 4.2.3 部分装备栏存档/读档
  - [ ] 4.2.4 尝试将错误类型装备放入槽位（应被拒绝）

---

## 依赖关系

```
🚀 第一波 (Task 1) ──┬──> 🚀 第二波 (Task 2) ──> ✅ 验收 (Task 4)
                     │
                     └──> 🚀 第三波 (Task 3)
```

- 第二波和第三波都依赖第一波完成
- 验收依赖第二波完成
- 第三波可以与第二波并行进行（在第一波完成后）

---

## 汇报规则

每完成一波攻击（一个 Stage），提交代码给架构师审查。

---

## 验收标准

### Task 1 验收
- [ ] 编译通过，无错误
- [ ] EquipmentData 继承 ItemData
- [ ] 包含 equipmentType, defense, attributes, equipmentModel 字段
- [ ] 可以在 Unity 编辑器中创建 EquipmentData 资产

### Task 2 验收
- [ ] 编译通过，无错误
- [ ] 装备栏数据能够保存到存档
- [ ] 读档后装备栏数据正确恢复
- [ ] 槽位限制生效（戒指只能放戒指槽）
- [ ] 现有装备交互功能不受影响

### Task 3 验收
- [ ] 编译通过，无错误
- [ ] 批量生成工具显示"装备"小类按钮
- [ ] 能够选择 `equipmentType`
- [ ] 生成时自动设置 equipmentType
- [ ] 生成的 SO 文件路径和命名正确

### Task 4 验收
- [ ] 所有测试用例通过
- [ ] 无数据丢失
- [ ] 无异常日志
- [ ] 槽位限制正常工作
