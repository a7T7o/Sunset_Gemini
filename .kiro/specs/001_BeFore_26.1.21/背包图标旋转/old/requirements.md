# 背包图标旋转显示系统 - 需求文档

## 简介

本系统实现背包/工具栏/装备栏中物品图标的 45 度旋转显示效果，与世界物品的视觉风格保持一致。通过 UI 层面的 Transform 旋转实现，无需生成额外的 bagSprite 资源。

## 术语表

- **ItemData**: 物品数据 ScriptableObject，包含 icon、bagSprite 等视觉资源字段
- **icon**: 物品的基础图标 Sprite（正向，未旋转）
- **bagSprite**: 背包显示用的 Sprite（将被废弃，改用 icon + 旋转）
- **UIItemIconScaler**: UI 图标缩放适配工具类
- **InventorySlotUI**: 背包槽位 UI 组件
- **ToolbarSlotUI**: 工具栏槽位 UI 组件
- **EquipmentSlotUI**: 装备栏槽位 UI 组件

## 需求列表

### 需求 1：UI 图标旋转显示

**用户故事:** 作为玩家，我希望背包中的物品图标旋转 45 度显示，这样视觉风格与世界物品保持一致。

#### 验收标准

1. 当物品图标在背包槽位显示时，系统应将图标 Z 轴旋转 45 度
2. 当物品图标在工具栏槽位显示时，系统应将图标 Z 轴旋转 45 度
3. 当物品图标在装备栏槽位显示时，系统应将图标 Z 轴旋转 45 度
4. 当图标旋转时，系统应使用 Transform 旋转而非图片处理，保持像素完整性

---

### 需求 2：简化 Sprite 管理

**用户故事:** 作为开发者，我希望简化 sprite 管理，不需要维护单独的 bagSprite 资源。

#### 验收标准

1. 当在 UI 中显示物品时，系统应直接使用 icon 字段并应用旋转
2. 当 bagSprite 字段为空时，系统应使用 icon 并应用 45 度旋转作为显示图标
3. 当 bagSprite 字段不为空时，系统仍应应用 45 度旋转以保持一致性
4. ItemData.GetBagSprite() 方法应返回 icon sprite（旋转由 UI 层处理）

---

### 需求 3：旋转角度可配置

**用户故事:** 作为开发者，我希望旋转角度可配置，以便在需要时调整视觉风格。

#### 验收标准

1. UIItemIconScaler 类应有可配置的旋转角度参数（默认 45 度）
2. 当旋转角度改变时，所有 UI 槽位应反映新的旋转角度
3. 旋转角度应在所有 UI 组件（背包、工具栏、装备栏）中保持一致

---

### 需求 4：旋转后缩放适配

**用户故事:** 作为开发者，我希望图标缩放能考虑旋转，使旋转后的图标正确适配槽位。

#### 验收标准

1. 当计算图标尺寸时，系统应考虑旋转后的边界框
2. 当正方形图标旋转 45 度时，系统应缩放使其适配显示区域
3. 旋转后图标的角落不应超出槽位的显示边界
4. 当图标非正方形时，系统应计算正确的旋转后边界框并相应缩放

---

### 需求 5：更新 WorldPrefabGeneratorTool

**用户故事:** 作为开发者，我希望更新 WorldPrefabGeneratorTool，使其不再生成 bagSprite。

#### 验收标准

1. WorldPrefabGeneratorTool 不应生成或设置 bagSprite 字段
2. 工具应只设置 ItemData 的 icon 字段
3. 当生成预制体时，工具应将 bagSprite 保持为 null

---

### 需求 6：更新批量 SO 生成工具

**用户故事:** 作为开发者，我希望更新批量 SO 生成工具，使其遵循新的 sprite 规范。

#### 验收标准

1. Tool_BatchItemSOGenerator 不应设置 bagSprite 字段
2. Tool_BatchItemSOModifier 应有清除 bagSprite 字段的选项
3. 当创建新 ItemData 时，工具应将 bagSprite 保持为 null

