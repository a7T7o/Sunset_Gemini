# 物品显示尺寸统一系统 - 需求文档

## 简介

本系统为所有物品提供统一的显示尺寸控制机制。通过在 ItemData SO 中添加可选的像素尺寸限定参数，实现世界物品（World Prefab）、阴影等场景元素的统一尺寸控制。背包/工具栏/配方界面内的显示则保持现有的自适应填充逻辑，但需要修复旋转后边界计算问题。

## 术语表

- **ItemData**: 物品数据 ScriptableObject 基类，包含所有物品的共同属性
- **World Prefab**: 物品在世界中的预制体，包含 Sprite、阴影和动画组件
- **displayPixelSize**: 新增的像素尺寸限定参数，定义物品在世界中显示的限定方框边长
- **UIItemIconScaler**: UI 图标缩放适配工具类，处理背包/工具栏中的图标显示
- **WorldPrefabGeneratorTool**: 编辑器工具，批量生成 World Prefab
- **Tool_BatchItemSOGenerator**: 编辑器工具，批量创建 ItemData SO

## 需求列表

### 需求 1：ItemData 新增显示尺寸参数

**用户故事:** 作为开发者，我希望在 ItemData SO 中配置物品的世界显示尺寸，这样可以精确控制每个物品在场景中的大小。

#### 验收标准

1. 当编辑 ItemData SO 时，系统应提供一个可选的「使用自定义显示尺寸」勾选框
2. 当勾选「使用自定义显示尺寸」时，系统应显示一个像素尺寸输入字段（displayPixelSize）
3. 当 displayPixelSize 设置为 N 像素时，系统应将物品 Sprite 等比例缩放至适配 N×N 像素的方框内
4. 当未勾选「使用自定义显示尺寸」时，系统应使用默认的缩放逻辑（保持现有行为）

---

### 需求 2：World Prefab 应用显示尺寸

**用户故事:** 作为开发者，我希望 World Prefab 根据 ItemData 的显示尺寸参数生成，这样世界中的物品大小可以统一调控。

#### 验收标准

1. 当生成 World Prefab 时，WorldPrefabGeneratorTool 应读取 ItemData 的 displayPixelSize 参数
2. 当 ItemData 启用自定义显示尺寸时，World Prefab 的 Sprite 应等比例缩放至适配指定像素方框
3. 当 ItemData 未启用自定义显示尺寸时，World Prefab 应使用工具的默认缩放参数
4. 当 World Prefab 的 Sprite 尺寸改变时，阴影应同步调整大小和位置

---

### 需求 3：运行时世界物品应用显示尺寸

**用户故事:** 作为开发者，我希望运行时生成的世界物品也能应用显示尺寸参数，这样动态生成的掉落物与预制体保持一致。

#### 验收标准

1. 当 WorldItemPickup 初始化时，系统应检查关联 ItemData 的显示尺寸设置
2. 当 ItemData 启用自定义显示尺寸时，WorldItemPickup 应调整 Sprite 缩放以适配指定像素方框
3. 当 WorldItemPickup 调整 Sprite 缩放时，阴影子物体应同步更新缩放

---

### 需求 4：背包/工具栏图标旋转后边界修复

**用户故事:** 作为开发者，我希望背包和工具栏中的图标在旋转后能正确计算边界，这样图标能完整填充槽位而不会被裁剪或留有过多空白。

#### 验收标准

1. 当计算旋转后图标尺寸时，UIItemIconScaler 应使用旋转后的实际宽高作为边界
2. 当图标旋转 45 度后，系统应计算旋转后 Sprite 的实际像素宽高
3. 当设置图标 RectTransform 尺寸时，系统应使用旋转后的边界尺寸进行等比例缩放
4. 当图标显示在槽位中时，系统应保留 2 像素的内边距

---

### 需求 5：背包/工具栏保持自适应填充

**用户故事:** 作为玩家，我希望背包和工具栏中的物品图标能自适应填充槽位，这样所有物品在 UI 中显示大小一致。

#### 验收标准

1. 当物品图标在背包槽位显示时，系统应忽略 ItemData 的 displayPixelSize 参数
2. 当物品图标在工具栏槽位显示时，系统应忽略 ItemData 的 displayPixelSize 参数
3. 当物品图标在配方界面显示时，系统应忽略 ItemData 的 displayPixelSize 参数
4. 当图标显示在 UI 槽位时，系统应将图标等比例缩放至填满槽位（减去 2 像素内边距）

---

### 需求 6：批量 SO 生成工具支持显示尺寸

**用户故事:** 作为开发者，我希望批量创建 ItemData SO 时可以设置显示尺寸参数，这样可以快速配置大量物品。

#### 验收标准

1. 当使用 Tool_BatchItemSOGenerator 创建 SO 时，工具应提供「启用自定义显示尺寸」选项
2. 当启用自定义显示尺寸时，工具应提供像素尺寸输入字段
3. 当批量创建 SO 时，工具应将显示尺寸设置应用到所有新创建的 ItemData
4. 当未启用自定义显示尺寸时，工具应保持 ItemData 的默认设置（useCustomDisplaySize = false）

---

### 需求 7：显示尺寸参数验证

**用户故事:** 作为开发者，我希望显示尺寸参数有合理的范围限制，这样可以避免配置错误导致的显示问题。

#### 验收标准

1. 当设置 displayPixelSize 时，系统应限制值在 8-128 像素范围内
2. 当 displayPixelSize 设置为 0 或负数时，系统应使用默认值（32 像素）
3. 当 ItemData 验证时，系统应检查显示尺寸参数的合理性并输出警告

