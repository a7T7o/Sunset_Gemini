# 实现计划 - 批量 SO 生成器分类优化

## 任务列表

- [x] 1. 重构枚举和映射系统



  - [ ] 1.1 添加 ItemMainCategory 大类枚举
    - 定义 6 个大类：ToolEquipment, Planting, Placeable, Consumable, Material, Other

    - _Requirements: 1.1_
  - [ ] 1.2 扩展 ItemSOType 枚举
    - 添加新类型：WorkstationData, StorageData, InteractiveDisplayData, SimpleEventData, FurnitureData, SpecialData

    - 重新组织枚举值，按大类分组
    - _Requirements: 2.1-2.6_
  - [ ] 1.3 创建 CategoryMapping 静态类
    - 实现 GetSubTypes() 大类到小类映射
    - 实现 GetStartID() 小类到起始 ID 映射
    - 实现 GetOutputFolder() 小类到输出路径映射

    - 实现 GetDisplayName() 和 GetCategoryName() 显示名称

    - 实现 GetCategoryColor() 大类颜色
    - _Requirements: 2.1-2.6, 3.1-3.12, 4.1-4.5_

- [x] 2. 重构 UI 绘制逻辑

  - [ ] 2.1 添加大类和小类状态字段
    - 添加 currentMainCategory 字段
    - 添加 currentSubType 字段
    - 移除旧的 soType 字段

    - _Requirements: 1.2, 1.3_
  - [ ] 2.2 重写 DrawTypeSelection() 方法
    - 绘制大类按钮行（6 个按钮）
    - 绘制小类按钮行（动态数量）

    - 显示当前选择的类型说明和 ID 范围
    - _Requirements: 1.1, 1.2, 2.1-2.6, 6.1-6.4_
  - [x] 2.3 实现大类按钮点击逻辑

    - 切换大类时更新小类列表

    - 自动选中该大类的第一个小类
    - 更新起始 ID 和输出路径
    - _Requirements: 1.2, 3.1-3.12, 4.1-4.5_
  - [x] 2.4 实现小类按钮点击逻辑

    - 切换小类时更新专属设置区域
    - 更新起始 ID 和输出路径
    - _Requirements: 2.1-2.6, 3.1-3.12, 4.1-4.5_


- [ ] 3. 添加新类型专属设置 UI
  - [ ] 3.1 添加工作台专属设置字段和 UI
    - workstationType 枚举选择

    - requiresFuel 开关
    - fuelSlotCount 数量（仅当 requiresFuel 为 true）
    - _Requirements: 5.1_
  - [x] 3.2 添加存储容器专属设置字段和 UI

    - storageCapacity 容量滑块
    - isLockable 开关

    - _Requirements: 5.2_
  - [ ] 3.3 添加交互展示专属设置字段和 UI
    - displayTitle 文本框
    - displayContent 多行文本框

    - displayDuration 时长滑块
    - _Requirements: 5.3_
  - [ ] 3.4 添加简单事件专属设置字段和 UI
    - eventType 枚举选择

    - isOneTime 开关
    - cooldownTime 冷却时间滑块
    - _Requirements: 5.4_


- [ ] 4. 实现新类型 SO 创建逻辑
  - [ ] 4.1 创建 CreateWorkstationData() 方法
    - 创建 WorkstationData 实例
    - 设置工作台专属字段

    - 自动设置 isPlaceable = true
    - _Requirements: 5.1, 5.5_

  - [x] 4.2 创建 CreateStorageData() 方法

    - 创建 StorageData 实例
    - 设置存储专属字段
    - 自动设置 isPlaceable = true
    - _Requirements: 5.2, 5.5_

  - [ ] 4.3 创建 CreateInteractiveDisplayData() 方法
    - 创建 InteractiveDisplayData 实例
    - 设置交互展示专属字段
    - 自动设置 isPlaceable = true

    - _Requirements: 5.3, 5.5_
  - [ ] 4.4 创建 CreateSimpleEventData() 方法
    - 创建 SimpleEventData 实例
    - 设置简单事件专属字段
    - 自动设置 isPlaceable = true

    - _Requirements: 5.4, 5.5_

  - [ ] 4.5 更新 CreateItemSO() 分发逻辑
    - 添加新类型的 switch case
    - 调用对应的创建方法
    - _Requirements: 5.1-5.5_

- [x] 5. 更新设置持久化

  - [ ] 5.1 更新 SaveSettings() 方法
    - 保存 currentMainCategory

    - 保存 currentSubType
    - 保存各大类最后选中的小类

    - _Requirements: 7.1, 7.3_
  - [x] 5.2 更新 LoadSettings() 方法

    - 加载 currentMainCategory
    - 加载 currentSubType
    - 加载各大类最后选中的小类
    - _Requirements: 7.2, 7.3_

  - [x] 5.3 添加新类型专属设置的持久化

    - 保存/加载工作台设置
    - 保存/加载存储设置
    - 保存/加载交互展示设置
    - 保存/加载简单事件设置

    - _Requirements: 7.1-7.3_

- [ ] 6. 更新辅助方法
  - [x] 6.1 更新 GetFilePrefix() 方法

    - 添加新类型的文件前缀
    - WorkstationData → "Workstation"
    - StorageData → "Storage"
    - InteractiveDisplayData → "Display"

    - SimpleEventData → "Event"
    - _Requirements: 5.1-5.4_
  - [ ] 6.2 更新 GetTypeName() 方法
    - 添加新类型的中文显示名称

    - _Requirements: 6.4_
  - [ ] 6.3 更新 AutoSetStartID() 方法
    - 使用 CategoryMapping.GetStartID()


    - _Requirements: 3.1-3.12_
  - [ ] 6.4 更新 GetAutoOutputFolder() 方法
    - 使用 CategoryMapping.GetOutputFolder()
    - _Requirements: 4.1-4.5_

- [ ] 7. 检查点 - 确保编译通过
  - 确保所有代码编译通过
  - 确保工具窗口能正常打开
  - 确保大类小类切换正常工作

- [ ] 8. 创建缺失的数据类（如果不存在）
  - [ ] 8.1 检查并创建 WorkstationData.cs
    - 继承 PlaceableItemData 或 ItemData
    - 添加工作台专属字段
    - 添加 CreateAssetMenu 属性
    - _Requirements: 5.1_
  - [ ] 8.2 检查并创建 StorageData.cs
    - 继承 PlaceableItemData 或 ItemData
    - 添加存储专属字段
    - 添加 CreateAssetMenu 属性
    - _Requirements: 5.2_
  - [ ] 8.3 检查并创建 InteractiveDisplayData.cs
    - 继承 PlaceableItemData 或 ItemData
    - 添加交互展示专属字段
    - 添加 CreateAssetMenu 属性
    - _Requirements: 5.3_
  - [ ] 8.4 检查并创建 SimpleEventData.cs
    - 继承 PlaceableItemData 或 ItemData
    - 添加简单事件专属字段
    - 添加 CreateAssetMenu 属性
    - _Requirements: 5.4_
  - [ ] 8.5 检查并创建相关枚举
    - WorkstationType 枚举（如不存在）
    - SimpleEventType 枚举（如不存在）
    - _Requirements: 5.1, 5.4_

- [ ] 9. 最终检查点
  - 确保所有代码编译通过
  - 确保所有类型都能正确生成 SO
  - 确保设置持久化正常工作

