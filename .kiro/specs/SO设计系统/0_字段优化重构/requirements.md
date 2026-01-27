# SO 字段优化重构 - 需求文档

## 简介

本需求旨在优化 ScriptableObject 系统的字段设计，解决基类字段过于臃肿的问题，提升 Inspector 的清晰度和代码的可维护性。

## 术语表

- **ItemData**：所有物品数据的基类
- **equipmentType**：装备类型字段（Helmet/Armor/Pants/Shoes/Ring）
- **consumableType**：消耗品类型字段（Food/Potion/Buff）
- **Inspector**：Unity 编辑器中的属性面板
- **SO**：ScriptableObject 的缩写

---

## 需求 1：移除基类中的特定类型字段

**用户故事**：作为开发者，我希望在编辑种子、材料等非装备/非消耗品的 SO 时，不看到无关的装备配置和消耗品配置区域，以便更清晰地管理物品数据。

### 验收标准

1. WHEN 开发者打开 SeedData SO THEN Inspector 中不应显示"装备配置"和"消耗品配置"区域
2. WHEN 开发者打开 MaterialData SO THEN Inspector 中不应显示"装备配置"和"消耗品配置"区域
3. WHEN 开发者打开 CropData SO THEN Inspector 中不应显示"装备配置"和"消耗品配置"区域
4. WHEN 开发者打开 KeyLockData SO THEN Inspector 中不应显示"装备配置"和"消耗品配置"区域
5. WHEN 开发者打开 FoodData SO THEN Inspector 中应显示"消耗品配置"区域但不显示"装备配置"区域

---

## 需求 2：在需要的子类中添加字段

**用户故事**：作为开发者，我希望只在需要的 SO 类型中看到相关字段，以便快速定位和配置正确的属性。

### 验收标准

1. WHEN 开发者打开 FoodData SO THEN Inspector 中应显示 consumableType 字段
2. WHEN 开发者打开 PotionData SO THEN Inspector 中应显示 consumableType 字段
3. WHEN 开发者打开 ToolData SO THEN Inspector 中不应显示 equipmentType 字段（工具不装备到装备栏）
4. WHEN 开发者打开 WeaponData SO THEN Inspector 中不应显示 equipmentType 字段（武器不装备到装备栏）
5. WHEN 所有字段移动完成 THEN 现有的快速装备和右键使用功能应继续正常工作

---

## 需求 3：更新批量生成工具

**用户故事**：作为开发者，我希望批量生成工具能够正确处理新的字段结构，以便快速创建符合规范的 SO 资产。

### 验收标准

1. WHEN 使用批量生成工具创建 FoodData THEN 应正确设置 consumableType 字段
2. WHEN 使用批量生成工具创建 PotionData THEN 应正确设置 consumableType 字段
3. WHEN 使用批量生成工具创建 SeedData THEN 不应尝试设置 equipmentType 或 consumableType
4. WHEN 使用批量生成工具创建 MaterialData THEN 不应尝试设置 equipmentType 或 consumableType
5. WHEN 批量生成完成 THEN 应自动同步到 ItemDatabase

---

## 需求 4：更新批量修改工具

**用户故事**：作为开发者，我希望批量修改工具能够识别不同 SO 类型的可用字段，以便安全地批量修改属性。

### 验收标准

1. WHEN 选中多个 FoodData SO THEN 批量修改工具应显示 consumableType 选项
2. WHEN 选中多个 SeedData SO THEN 批量修改工具不应显示 equipmentType 或 consumableType 选项
3. WHEN 选中混合类型的 SO THEN 批量修改工具应只显示所有类型共有的字段
4. WHEN 批量修改完成 THEN 应自动同步到 ItemDatabase
5. WHEN 批量修改完成 THEN 应在控制台输出修改摘要

---

## 需求 5：更新相关代码

**用户故事**：作为开发者，我希望所有使用 equipmentType 和 consumableType 的代码都能正确处理新的字段位置，以便系统功能不受影响。

### 验收标准

1. WHEN 玩家右键点击食物 THEN 应正确识别 consumableType 并执行食用逻辑
2. WHEN 玩家右键点击药水 THEN 应正确识别 consumableType 并执行使用逻辑
3. WHEN 玩家右键点击种子 THEN 不应尝试访问 consumableType 字段
4. WHEN 玩家右键点击材料 THEN 不应尝试访问 consumableType 字段
5. WHEN 系统检查物品是否可装备 THEN 应正确处理没有 equipmentType 字段的物品

---

## 需求 6：标注废弃字段

**用户故事**：作为开发者，我希望废弃的字段被明确标注，以便避免误用并为未来的清理做准备。

### 验收标准

1. WHEN 开发者查看 ItemData.bagSprite 字段 THEN 应看到 [Obsolete] 标记和说明
2. WHEN 开发者在代码中使用 bagSprite THEN 编译器应显示警告信息
3. WHEN 开发者查看 SO 参数设计文档 THEN 应看到 bagSprite 标注为"已废弃"
4. WHEN 开发者使用批量生成工具 THEN 不应设置 bagSprite 字段
5. WHEN 开发者使用批量修改工具 THEN 应有"清除 bagSprite"选项

---

## 需求 7：更新文档

**用户故事**：作为开发者，我希望所有相关文档都反映最新的字段结构，以便正确理解和使用 SO 系统。

### 验收标准

1. WHEN 开发者查看 SO 参数设计文档 THEN 应看到更新到 v2.0 的版本号
2. WHEN 开发者查看 SO 参数设计文档 THEN 应看到 equipmentType 和 consumableType 的新位置说明
3. WHEN 开发者查看 so-design.md steering 规则 THEN 应看到更新的字段说明
4. WHEN 开发者查看工作区 memory.md THEN 应看到本次重构的完整记录
5. WHEN 开发者查看 SO 设计概要文档 THEN 应看到标注为"已实施"的状态

---

## 需求 8：创建字段清理工具

**用户故事**：作为开发者，我希望有一个工具能够批量清理现有 SO 中的无效字段，以便快速完成迁移。

### 验收标准

1. WHEN 开发者运行字段清理工具 THEN 应扫描所有 SO 资产
2. WHEN 工具检测到 SeedData 中的 equipmentType 字段 THEN 应将其重置为默认值
3. WHEN 工具检测到 MaterialData 中的 consumableType 字段 THEN 应将其重置为默认值
4. WHEN 工具完成清理 THEN 应在控制台输出清理报告
5. WHEN 工具完成清理 THEN 应自动保存所有修改的资产

---

## 非功能需求

### 性能要求

1. 批量修改工具应在 5 秒内完成 100 个 SO 的修改
2. 字段清理工具应在 10 秒内完成全部 SO 的扫描和清理

### 兼容性要求

1. 现有的 SO 资产应能够自动适应新的字段结构（字段丢失不影响功能）
2. 现有的游戏逻辑应继续正常工作
3. 现有的 UI 系统应继续正常工作

### 可维护性要求

1. 代码修改应遵循单一职责原则
2. 新增的字段应有清晰的注释说明
3. 批量工具应有详细的日志输出

---

## 约束条件

1. 不能破坏现有的游戏功能
2. 不能导致现有 SO 资产无法使用
3. 必须保持与现有代码的兼容性
4. 必须更新所有相关文档

---

## 依赖关系

| 依赖项 | 说明 |
|--------|------|
| ItemData.cs | 基类，需要移除字段 |
| FoodData.cs | 需要添加 consumableType |
| PotionData.cs | 需要添加 consumableType |
| Tool_BatchItemSOGenerator.cs | 需要更新生成逻辑 |
| Tool_BatchItemSOModifier.cs | 需要更新修改逻辑 |
| InventoryService.cs | 可能需要更新右键使用逻辑 |

---

## 验收测试场景

### 场景 1：创建新的种子 SO

1. 打开批量生成工具
2. 选择 SeedData 类型
3. 输入种子信息
4. 生成 SO
5. 验证：Inspector 中不显示装备配置和消耗品配置

### 场景 2：创建新的食物 SO

1. 打开批量生成工具
2. 选择 FoodData 类型
3. 输入食物信息和 consumableType
4. 生成 SO
5. 验证：Inspector 中显示消耗品配置但不显示装备配置

### 场景 3：批量修改现有 SO

1. 选中多个 SeedData SO
2. 打开批量修改工具
3. 验证：不显示 equipmentType 和 consumableType 选项
4. 修改其他字段
5. 验证：修改成功且不影响其他字段

### 场景 4：清理现有 SO

1. 运行字段清理工具
2. 验证：所有 SeedData 的 equipmentType 被重置
3. 验证：所有 MaterialData 的 consumableType 被重置
4. 验证：FoodData 和 PotionData 的 consumableType 保持不变

### 场景 5：游戏功能测试

1. 启动游戏
2. 右键点击食物
3. 验证：正确执行食用逻辑
4. 右键点击种子
5. 验证：正确执行种植逻辑（不尝试访问 consumableType）

---

*需求文档结束*
