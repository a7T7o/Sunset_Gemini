# 装备与工具系统需求文档

**创建日期**: 2026-01-29
**更新日期**: 2026-01-29
**任务代号**: Operation Arsenal (军火库行动) - Execute Phase
**决策状态**: 🔒 文档已锁定 → 🔥 代码执行中

---

## 背景

Phase 2 交互修复已通过审查，现在进入 Phase 3：装备与工具链重构。

当前问题：
1. `EquipmentService` 使用 `ItemStack[]`，未实现 `IPersistentObject`，存档后装备丢失
2. `Tool_BatchItemSOGenerator` 缺少装备生成入口
3. 装备系统处于"石器时代"状态

**架构决策**：采用渐进式重构（选项 A），创建 `EquipmentData : ItemData` 子类，不修改 `ItemData` 基类，保护现有 SO 资产。

---

## 用户故事

### US-1 装备持久化（P0）

**作为** 玩家
**我希望** 我的装备栏数据能够保存到存档中
**以便** 读档后装备不会丢失

**验收标准**:
- [ ] 1.1 装备栏数据（itemId, quality, amount）能够正确保存
- [ ] 1.2 读档后装备栏能够正确恢复
- [ ] 1.3 EquipmentService 实现 IPersistentObject 接口
- [ ] 1.4 内部存储从 ItemStack[] 升级为 InventoryItem[]

### US-2 装备槽位限制（P0）

**作为** 玩家
**我希望** 装备只能放入对应的槽位
**以便** 戒指不能戴在头上，头盔不能穿在脚上

**验收标准**:
- [ ] 2.1 EquipItem 方法检查 equipmentType 是否匹配目标槽位
- [ ] 2.2 槽位 0 只接受 Helmet
- [ ] 2.3 槽位 1 只接受 Pants
- [ ] 2.4 槽位 2 只接受 Armor
- [ ] 2.5 槽位 3 只接受 Shoes
- [ ] 2.6 槽位 4/5 只接受 Ring

### US-3 装备数据结构（P0）

**作为** 开发者
**我希望** 有独立的 EquipmentData 类存储装备专属属性
**以便** 支持防御力、属性加成、纸娃娃模型等功能

**验收标准**:
- [ ] 3.1 创建 `EquipmentData : ItemData` 子类
- [ ] 3.2 包含 `equipmentType` 字段（覆盖或复用基类枚举）
- [ ] 3.3 包含 `defense` 字段（防御力）
- [ ] 3.4 包含 `attributes` 字段（预留属性加成）
- [ ] 3.5 包含 `equipmentModel` 字段（预留纸娃娃系统）

### US-4 批量生成装备（P1）

**作为** 策划/开发者
**我希望** 编辑器工具支持批量生成装备类物品
**以便** 高效创建头盔、盔甲、裤子、鞋子、戒指等装备 SO

**验收标准**:
- [ ] 4.1 `Tool_BatchItemSOGenerator` 增加"装备"小类
- [ ] 4.2 支持选择 `equipmentType`
- [ ] 4.3 生成时自动设置 equipmentType（策划不需要手动选）
- [ ] 4.4 自动设置正确的 ID 范围和输出路径

---

## 设计决策

### 决策 1：装备数据结构 ✅ 已决定

**选项 A（已批准）**：创建 `EquipmentData : ItemData` 子类
- 继承 ItemData，不修改基类
- 新增装备专属字段：defense, attributes, equipmentModel
- 保护现有 SO 资产不受影响

~~**选项 B**：大规模重构 ItemData 基类~~
- ❌ 已否决：风险太高，可能导致现有 SO 资产数据丢失

### 决策 2：EquipmentService 数据结构 ✅ 已决定

**当前**：`ItemStack[]`
**目标**：`InventoryItem[]`（与背包一致，支持耐久和附魔）

---

## 优先级

| 优先级 | 用户故事 | 说明 |
|--------|---------|------|
| P0 | US-1 装备持久化 | 必须修复，否则装备系统不可用 |
| P0 | US-2 装备槽位限制 | 核心功能，防止错误装备 |
| P0 | US-3 装备数据结构 | 创建 EquipmentData 子类 |
| P1 | US-4 批量生成装备 | 提高开发效率 |

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Service/Equipment/EquipmentService.cs` | 装备服务（需重构） |
| `Assets/YYY_Scripts/Data/Enums/ItemEnums.cs` | EquipmentType 枚举 |
| `Assets/YYY_Scripts/Data/Items/EquipmentData.cs` | 装备数据类（新建） |
| `Assets/Editor/Tool_BatchItemSOGenerator.cs` | 批量生成工具（需扩展） |
| `Assets/YYY_Scripts/UI/Inventory/EquipmentSlotUI.cs` | 装备槽位 UI |
