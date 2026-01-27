# SO 字段优化重构 - 任务列表

## 任务概述

本任务列表用于指导 SO 字段优化重构的实施，按照优先级和依赖关系组织。

---

## 1. 核心数据类修改

- [ ] 1.1 修改 ItemData.cs 基类
  - 移除 `equipmentType` 字段
  - 移除 `consumableType` 字段
  - 添加 `[Obsolete]` 标记到 `bagSprite` 字段
  - 添加 `IsConsumable()` 虚方法
  - 添加 `GetConsumableType()` 虚方法
  - _需求: 1.1, 1.2, 1.3, 1.4, 6.1_

- [ ] 1.2 修改 FoodData.cs
  - 添加 `consumableType` 字段（默认值 ConsumableType.Food）
  - 重写 `IsConsumable()` 方法返回 true
  - 重写 `GetConsumableType()` 方法返回 consumableType
  - _需求: 2.1_

- [ ] 1.3 修改 PotionData.cs
  - 添加 `consumableType` 字段（默认值 ConsumableType.Potion）
  - 重写 `IsConsumable()` 方法返回 true
  - 重写 `GetConsumableType()` 方法返回 consumableType
  - _需求: 2.2_

- [ ]* 1.4 编写单元测试
  - 测试 ItemData.IsConsumable() 返回 false
  - 测试 FoodData.IsConsumable() 返回 true
  - 测试 PotionData.IsConsumable() 返回 true
  - 测试 FoodData.GetConsumableType() 返回正确类型
  - 测试 PotionData.GetConsumableType() 返回正确类型
  - _需求: 2.1, 2.2, 2.3, 2.4_

---

## 2. 游戏逻辑代码更新

- [ ] 2.1 更新 InventoryService.cs
  - 查找所有使用 `equipmentType` 的代码
  - 查找所有使用 `consumableType` 的代码
  - 替换为使用 `IsConsumable()` 和 `GetConsumableType()` 方法
  - 添加类型检查和转换（如需要）
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 2.2 更新 ItemSlotUI.cs
  - 查找右键点击处理代码
  - 更新为使用 `IsConsumable()` 和 `GetConsumableType()` 方法
  - 测试右键使用食物和药水
  - _需求: 5.1, 5.2_

- [ ] 2.3 搜索并更新其他使用这些字段的代码
  - 使用全局搜索查找 `equipmentType` 和 `consumableType`
  - 逐个文件检查和更新
  - 确保所有引用都已更新
  - _需求: 5.5_

- [ ]* 2.4 编写集成测试
  - 测试右键使用食物
  - 测试右键使用药水
  - 测试右键使用种子（不应尝试访问 consumableType）
  - 测试右键使用材料（不应尝试访问 consumableType）
  - _需求: 5.1, 5.2, 5.3, 5.4_

---

## 3. 批量生成工具更新

- [ ] 3.1 更新 Tool_BatchItemSOGenerator.cs
  - 移除基类中 equipmentType 和 consumableType 的设置
  - 在 FoodData 生成方法中添加 consumableType 设置
  - 在 PotionData 生成方法中添加 consumableType 设置
  - 移除 bagSprite 的设置
  - 测试生成各种类型的 SO
  - _需求: 3.1, 3.2, 3.3, 3.4, 6.4_

- [ ] 3.2 更新 ItemSOBatchCreator.cs
  - 检查是否使用了 equipmentType 或 consumableType
  - 如果使用，按照 3.1 的方式更新
  - 测试生成功能
  - _需求: 3.1, 3.2, 3.3, 3.4_

- [ ]* 3.3 编写批量生成工具测试
  - 测试生成 FoodData（验证 consumableType 正确）
  - 测试生成 PotionData（验证 consumableType 正确）
  - 测试生成 SeedData（验证无 equipmentType 和 consumableType）
  - 测试生成 MaterialData（验证无 equipmentType 和 consumableType）
  - 测试数据库自动同步
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5_

---

## 4. 批量修改工具更新

- [ ] 4.1 更新 Tool_BatchItemSOModifier.cs
  - 移除 equipmentType 和 consumableType 的通用修改选项
  - 添加类型检测逻辑
  - 只在选中 FoodData/PotionData 时显示 consumableType 选项
  - 添加"清除 bagSprite"选项
  - 测试批量修改功能
  - _需求: 4.1, 4.2, 4.3, 4.4, 6.5_

- [ ]* 4.2 编写批量修改工具测试
  - 测试选中 FoodData 时显示 consumableType 选项
  - 测试选中 SeedData 时不显示 equipmentType 和 consumableType 选项
  - 测试选中混合类型时只显示共有字段
  - 测试批量修改成功
  - 测试数据库自动同步
  - _需求: 4.1, 4.2, 4.3, 4.4, 4.5_

---

## 5. 字段清理工具

- [ ] 5.1 创建 Tool_SOFieldCleaner.cs
  - 扫描所有 SO 资产
  - 检测不需要 equipmentType 的 SO（SeedData, MaterialData 等）
  - 检测不需要 consumableType 的 SO（SeedData, MaterialData 等）
  - 将无效字段重置为默认值
  - 保存修改的资产
  - 输出清理报告
  - _需求: 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 5.2 运行字段清理工具
  - 备份项目（使用版本控制）
  - 运行清理工具
  - 检查清理报告
  - 验证 SO 资产正常
  - _需求: 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ]* 5.3 编写字段清理工具测试
  - 创建包含无效字段的测试 SO
  - 运行清理工具
  - 验证无效字段被清理
  - 验证有效字段保持不变
  - 验证清理报告正确
  - _需求: 8.1, 8.2, 8.3, 8.4, 8.5_

---

## 6. 文档更新

- [ ] 6.1 更新 SO 参数设计文档
  - 版本号升级到 v2.0
  - 更新 ItemData 字段列表（移除 equipmentType 和 consumableType）
  - 添加 FoodData 的 consumableType 字段说明
  - 添加 PotionData 的 consumableType 字段说明
  - 标注 bagSprite 为"已废弃"
  - 添加虚方法使用说明
  - _需求: 7.1, 7.2, 6.3_

- [ ] 6.2 更新 so-design.md steering 规则
  - 更新字段说明
  - 添加虚方法使用示例
  - 更新批量工具使用说明
  - _需求: 7.3_

- [ ] 6.3 更新 SO 设计概要文档
  - 标注为"已实施"
  - 添加实施日期
  - 添加实施结果总结
  - _需求: 7.5_

- [ ] 6.4 更新工作区 memory.md
  - 添加本次会话记录
  - 记录完成的任务
  - 记录修改的文件
  - 记录解决方案思路
  - 记录遗留问题（如有）
  - _需求: 7.4_

---

## 7. 测试与验收

- [ ] 7.1 Inspector 显示测试
  - 打开 SeedData SO，验证无装备配置和消耗品配置
  - 打开 MaterialData SO，验证无装备配置和消耗品配置
  - 打开 CropData SO，验证无装备配置和消耗品配置
  - 打开 KeyLockData SO，验证无装备配置和消耗品配置
  - 打开 FoodData SO，验证有消耗品配置但无装备配置
  - 打开 PotionData SO，验证有消耗品配置但无装备配置
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5_

- [ ] 7.2 游戏功能测试
  - 启动游戏
  - 右键点击食物，验证正确执行食用逻辑
  - 右键点击药水，验证正确执行使用逻辑
  - 右键点击种子，验证正确执行种植逻辑
  - 右键点击材料，验证无错误
  - 检查控制台无错误
  - _需求: 5.1, 5.2, 5.3, 5.4_

- [ ] 7.3 批量工具测试
  - 使用批量生成工具创建 FoodData，验证 consumableType 正确
  - 使用批量生成工具创建 PotionData，验证 consumableType 正确
  - 使用批量生成工具创建 SeedData，验证无无关字段
  - 使用批量修改工具修改 FoodData，验证显示 consumableType 选项
  - 使用批量修改工具修改 SeedData，验证不显示无关选项
  - _需求: 3.1, 3.2, 3.3, 4.1, 4.2_

- [ ] 7.4 性能测试
  - 批量修改 100 个 SO，验证在 5 秒内完成
  - 运行字段清理工具，验证在 10 秒内完成
  - _需求: 非功能需求_

- [ ] 7.5 文档验收
  - 检查 SO 参数设计文档已更新到 v2.0
  - 检查 so-design.md 已更新
  - 检查工作区 memory.md 已更新
  - 检查 SO 设计概要文档已标注为"已实施"
  - _需求: 7.1, 7.2, 7.3, 7.4, 7.5_

---

## 8. 最终检查点

- [ ] 8. 确保所有测试通过，询问用户是否有问题
  - 确保所有测试通过，询问用户是否有问题

---

## 任务依赖关系

```
1. 核心数据类修改
   ├── 1.1 ItemData.cs
   ├── 1.2 FoodData.cs
   ├── 1.3 PotionData.cs
   └── 1.4 单元测试*
       ↓
2. 游戏逻辑代码更新
   ├── 2.1 InventoryService.cs
   ├── 2.2 ItemSlotUI.cs
   ├── 2.3 其他代码
   └── 2.4 集成测试*
       ↓
3. 批量生成工具更新
   ├── 3.1 Tool_BatchItemSOGenerator.cs
   ├── 3.2 ItemSOBatchCreator.cs
   └── 3.3 批量生成工具测试*
       ↓
4. 批量修改工具更新
   ├── 4.1 Tool_BatchItemSOModifier.cs
   └── 4.2 批量修改工具测试*
       ↓
5. 字段清理工具
   ├── 5.1 Tool_SOFieldCleaner.cs
   ├── 5.2 运行清理工具
   └── 5.3 字段清理工具测试*
       ↓
6. 文档更新
   ├── 6.1 SO 参数设计文档
   ├── 6.2 so-design.md
   ├── 6.3 SO 设计概要文档
   └── 6.4 工作区 memory.md
       ↓
7. 测试与验收
   ├── 7.1 Inspector 显示测试
   ├── 7.2 游戏功能测试
   ├── 7.3 批量工具测试
   ├── 7.4 性能测试
   └── 7.5 文档验收
       ↓
8. 最终检查点
```

---

## 注意事项

1. **备份项目**：在开始修改前，确保使用版本控制备份项目
2. **逐步测试**：每完成一个任务，立即测试相关功能
3. **编译检查**：每次修改后编译项目，确保无错误
4. **文档同步**：修改代码的同时更新相关文档
5. **可选任务**：标记为 `*` 的任务是可选的测试任务，可以根据时间和需求决定是否执行

---

*任务列表结束*
