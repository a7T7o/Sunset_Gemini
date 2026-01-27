# 实现计划

- [x] 1. 更新 InventoryBootstrap 数据结构




  - [x] 1.1 添加 BootItemList 类定义

    - 创建新的序列化类，包含 name、enabled、foldout、items 字段
    - _Requirements: 1.1, 1.2, 1.3, 2.1_
  - [x] 1.2 更新 InventoryBootstrap 字段

    - 添加 itemLists 列表字段
    - 保留旧版 items 字段用于迁移

    - _Requirements: 5.1_
  - [ ] 1.3 实现旧版数据迁移逻辑
    - 在 OnValidate 或首次访问时检测并迁移旧数据





    - _Requirements: 5.1_

  - [ ] 1.4 更新 Apply() 方法
    - 遍历所有启用的列表进行注入
    - 处理容量溢出情况
    - _Requirements: 2.1, 2.2, 2.3, 5.2, 5.4_


- [ ] 2. 创建 InventoryBootstrapEditor 自定义编辑器
  - [ ] 2.1 创建编辑器脚本基础结构
    - 创建 CustomEditor 类
    - 设置 SerializedProperty 引用

    - _Requirements: 4.2_
  - [ ] 2.2 实现列表管理 UI
    - 添加/删除列表按钮
    - 列表名称编辑

    - 启用/禁用复选框
    - 折叠/展开控制
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2_
  - [x] 2.3 实现物品条目 UI

    - 图标预览显示
    - 品质和数量编辑


    - 删除按钮
    - _Requirements: 4.3_
  - [ ] 2.4 实现拖放功能
    - 绘制拖放区域
    - 处理 DragAndDrop 事件
    - 批量添加物品
    - _Requirements: 3.1, 3.3_
  - [ ] 2.5 实现批量选择弹窗
    - 创建 EditorWindow 子类
    - 显示 ItemData 列表
    - 多选并添加到目标列表
    - _Requirements: 3.2, 3.3_
  - [ ] 2.6 实现右键上下文菜单
    - 删除、复制、上移、下移操作
    - _Requirements: 4.4_

- [ ] 3. 最终检查
  - 确保所有测试通过，询问用户是否有问题

