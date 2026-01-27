# Implementation Plan

## SO 设计系统扩展 - 任务列表

- [x] 1. 创建数据库同步辅助类





  - [ ] 1.1 创建 DatabaseSyncHelper.cs 静态类
    - 定义数据库路径常量 `Assets/Data/Database/MasterItemDatabase.asset`
    - 实现 GetMasterDatabase() 方法定位数据库资产
    - 实现 AutoCollectAllItems() 方法（通过反射调用 ItemDatabase 的 ContextMenu 方法）
    - 实现 AutoCollectAllRecipes() 方法




    - 实现 SyncResult 结构体返回同步结果





    - _Requirements: 2.1, 2.4, 4.1, 4.3_
  - [ ] 1.2 Write property test for database sync
    - **Property 4: 数据库同步完整性**






    - **Validates: Requirements 2.1, 4.1**



- [ ] 2. 优化批量生成 SO 工具
  - [x] 2.1 修改 Tool_BatchItemSOGenerator.cs 的 GenerateItemSOs 方法

    - 在生成完成后调用 DatabaseSyncHelper.AutoCollectAllItems()
    - 在控制台输出同步结果日志
    - 处理数据库不存在的情况并显示警告
    - _Requirements: 2.1, 2.2, 2.3_
  - [ ] 2.2 修改 ItemSOBatchCreator.cs 的 CreateAssets 方法
    - 同样添加自动同步功能
    - 保持与 Tool_BatchItemSOGenerator 一致的行为

    - _Requirements: 2.1_

- [ ] 3. Checkpoint - 确保数据库同步功能正常
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 4. 创建 SO 批量修改工具
  - [x] 4.1 创建 Tool_BatchItemSOModifier.cs 编辑器窗口

    - 添加菜单项 `Tools/📝 批量修改物品 SO`
    - 实现 Selection 监听和 SO 过滤逻辑
    - 实现 RefreshSelection() 方法识别选中的 ItemData 及子类
    - _Requirements: 1.1, 1.6_
  - [ ] 4.2 Write property test for SO type identification
    - **Property 1: SO 类型识别正确性**

    - **Validates: Requirements 1.1**
  - [-] 4.3 实现通用属性修改区域



    - 绘制价格、堆叠数、描述等通用字段
    - 每个字段前添加勾选框控制是否修改

    - 实现 DrawModifyField<T>() 通用方法
    - _Requirements: 1.2_
  - [ ] 4.4 Write property test for modification isolation
    - **Property 2: 参数修改隔离性**
    - **Validates: Requirements 1.2**
  - [ ] 4.5 实现类型专属属性区域
    - 检测选中 SO 的具体类型（ToolData、WeaponData 等）

    - 根据类型显示对应的专属字段
    - 实现 GetTypeSpecificFields() 方法
    - _Requirements: 1.5_
  - [x] 4.6 Write property test for type-field mapping

    - **Property 3: 类型到字段映射一致性**
    - **Validates: Requirements 1.5**
  - [ ] 4.7 实现应用修改功能
    - 实现 ApplyModifications() 方法

    - 遍历选中 SO，仅修改勾选的字段
    - 调用 EditorUtility.SetDirty() 和 AssetDatabase.SaveAssets()
    - 修改完成后自动调用 DatabaseSyncHelper.AutoCollectAllItems()

    - _Requirements: 1.3, 1.4, 4.1_

- [ ] 5. Checkpoint - 确保批量修改工具正常
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. 创建配方批量创建工具
  - [ ] 6.1 创建 Tool_BatchRecipeCreator.cs 编辑器窗口
    - 添加菜单项 `Tools/📜 批量创建配方 SO`
    - 实现基础 UI 框架（与物品批量生成工具风格一致）
    - _Requirements: 3.1, 3.6_
  - [ ] 6.2 实现 ID 设置区域
    - 连续 ID 模式开关

    - 起始 ID 输入
    - 实现 ParseIds() 方法解析 ID 输入
    - _Requirements: 3.7_
  - [ ] 6.3 Write property test for ID sequence generation
    - **Property 5: ID 序列生成正确性**
    - **Validates: Requirements 3.7**
  - [ ] 6.4 实现配方信息输入区域
    - 配方名称多行输入
    - 产物 ID 多行输入
    - 产物数量多行输入（默认 1）
    - _Requirements: 3.1_
  - [ ] 6.5 实现材料列表编辑区域
    - 动态添加/删除材料项
    - 每个材料项包含 itemID 和 amount
    - 使用 ReorderableList 或自定义列表 UI
    - _Requirements: 3.2_
  - [ ] 6.6 实现制作设施选择
    - CraftingStation 枚举下拉选择
    - 其他配方属性（制作时间、经验、解锁条件等）
    - _Requirements: 3.3_
  - [ ] 6.7 实现批量创建功能
    - 实现 CreateRecipes() 方法
    - 按行解析输入并创建 RecipeData 资产
    - 文件命名格式：`Recipe_{id}_{name}.asset`
    - 创建完成后调用 DatabaseSyncHelper.AutoCollectAllRecipes()
    - _Requirements: 3.4, 3.5_
  - [ ] 6.8 Write property test for recipe parsing
    - **Property 6: 配方输入解析正确性**
    - **Validates: Requirements 3.4**
  - [ ] 6.9 Write property test for recipe database sync
    - **Property 7: 配方数据库同步完整性**
    - **Validates: Requirements 3.5**

- [ ] 7. Final Checkpoint - 确保所有工具正常工作
  - Ensure all tests pass, ask the user if questions arise.

