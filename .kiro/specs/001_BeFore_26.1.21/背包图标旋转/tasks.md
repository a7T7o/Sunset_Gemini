# 实现计划

- [x] 1. 修改 UIItemIconScaler 添加旋转功能
  - [x] 1.1 添加 ICON_ROTATION_Z 常量（默认 45 度）
    - 在类顶部添加 `private const float ICON_ROTATION_Z = 45f;`
    - _需求: 3.1_
  - [x] 1.2 修改 SetIconWithAutoScale 方法添加旋转后边界框计算
    - 计算旋转后的 rotatedWidth 和 rotatedHeight
    - 使用旋转后边界框计算缩放比例
    - _需求: 4.1, 4.2, 4.4_
  - [x] 1.3 应用旋转到 RectTransform
    - 设置 `rt.localRotation = Quaternion.Euler(0, 0, ICON_ROTATION_Z)`
    - _需求: 1.1, 1.2, 1.3, 1.4_
  - [x] 1.4 边界框计算验证（代码审查通过）
    - 正方形和非正方形 sprite 旋转后边界框计算正确
    - **属性 2: 边界框不超出**
    - **验证: 需求 4.2, 4.3**

- [x] 2. 验证现有 UI 组件自动获得旋转效果
  - [x] 2.1 验证 InventorySlotUI 显示旋转图标
    - 确认使用 UIItemIconScaler.SetIconWithAutoScale ✅
    - _需求: 1.1_
  - [x] 2.2 验证 ToolbarSlotUI 显示旋转图标
    - 确认使用 UIItemIconScaler.SetIconWithAutoScale ✅
    - _需求: 1.2_
  - [x] 2.3 验证 EquipmentSlotUI 显示旋转图标
    - 确认使用 UIItemIconScaler.SetIconWithAutoScale ✅
    - _需求: 1.3_

- [x] 3. 确保 ItemData.GetBagSprite() 逻辑正确
  - [x] 3.1 验证 GetBagSprite() 返回 icon（当 bagSprite 为空时）
    - 当前实现已正确：`return bagSprite != null ? bagSprite : icon;`
    - _需求: 2.2, 2.4_
  - [x] 3.2 回退逻辑验证（代码审查通过）
    - **属性 4: 回退逻辑一致性**
    - **验证: 需求 2.1, 2.2, 2.4**

- [x] 4. 更新批量生成工具
  - [x] 4.1 确认 WorldPrefabGeneratorTool 不设置 bagSprite
    - 检查 GeneratePrefab 方法 ✅ 不涉及 bagSprite
    - _需求: 5.1, 5.2, 5.3_
  - [x] 4.2 确认 Tool_BatchItemSOGenerator 不设置 bagSprite
    - 检查创建 ItemData 的代码 ✅ 不涉及 bagSprite
    - _需求: 6.1, 6.3_
  - [x] 4.3 在 Tool_BatchItemSOModifier 添加清除 bagSprite 选项
    - 添加 clearBagSprite 勾选框 ✅
    - 勾选时将选中 SO 的 bagSprite 设为 null ✅
    - _需求: 6.2_

- [x] 5. 检查点 - 代码审查通过
  - 所有核心功能已实现并验证

- [x] 6. 更新相关文档和 memory
  - [x] 6.1 更新 item-drop-pickup-system/memory.md ✅
  - [x] 6.2 更新 ui-system/memory.md ✅
  - [x] 6.3 更新 so-design-system/memory.md ✅
  - [x] 6.4 更新 world prefab生成与功能设计.md ✅

- [x] 7. 最终检查点 - 全部完成
  - 所有任务已完成，等待用户运行时测试验证

