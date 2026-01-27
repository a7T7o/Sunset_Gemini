# 需求文档

## 简介

优化 InventoryBootstrap 脚本的编辑器交互体验，支持多物品列表管理、批量添加物品、以及更灵活的注入控制。

## 术语表

- **InventoryBootstrap**：背包启动注入系统，用于在游戏启动时向背包注入预设物品
- **BootItemList**：物品注入列表，包含一组待注入的物品配置
- **ItemData**：物品数据 ScriptableObject，定义物品的基本属性
- **InventoryService**：背包服务，管理玩家背包数据

## 需求

### 需求 1

**用户故事：** 作为开发者，我希望能够创建和管理多个物品注入列表，以便我可以为不同的测试场景准备不同的物品组合。

#### 验收标准

1. WHEN 用户点击"添加列表"按钮 THEN InventoryBootstrap SHALL 创建一个新的空物品列表并显示在界面中
2. WHEN 用户点击列表的删除按钮 THEN InventoryBootstrap SHALL 移除该列表及其所有物品配置
3. WHEN 用户编辑列表名称输入框 THEN InventoryBootstrap SHALL 更新该列表的显示名称
4. WHEN 用户展开或折叠列表 THEN InventoryBootstrap SHALL 保持该列表的折叠状态直到下次操作

### 需求 2

**用户故事：** 作为开发者，我希望能够选择性地启用或禁用某些列表进行注入，以便我可以快速切换不同的测试配置。

#### 验收标准

1. WHEN 用户勾选列表的启用复选框 THEN InventoryBootstrap SHALL 在注入时包含该列表的所有物品
2. WHEN 用户取消勾选列表的启用复选框 THEN InventoryBootstrap SHALL 在注入时跳过该列表
3. WHEN 背包容量不足以容纳所有启用列表的物品 THEN InventoryBootstrap SHALL 按列表顺序注入直到背包满，并记录未注入的物品数量

### 需求 3

**用户故事：** 作为开发者，我希望能够通过拖拽或选择的方式批量添加物品到列表中，以便我可以快速配置测试物品。

#### 验收标准

1. WHEN 用户将多个 ItemData SO 文件拖拽到列表的拖放区域 THEN InventoryBootstrap SHALL 将所有拖入的物品添加到该列表末尾
2. WHEN 用户点击"批量选择"按钮并在弹出窗口中选择多个 ItemData THEN InventoryBootstrap SHALL 将选中的物品添加到该列表末尾
3. WHEN 批量添加物品时 THEN InventoryBootstrap SHALL 为每个新添加的物品设置默认品质为 0、默认数量为 1

### 需求 4

**用户故事：** 作为开发者，我希望编辑器界面简洁高效且响应迅速，以便我可以流畅地进行物品配置操作。

#### 验收标准

1. WHEN 列表包含大量物品时 THEN InventoryBootstrap 编辑器 SHALL 使用虚拟化或分页显示以保持流畅的滚动性能
2. WHEN 用户进行任何编辑操作 THEN InventoryBootstrap 编辑器 SHALL 在 100 毫秒内响应并更新界面
3. WHEN 用户悬停在物品条目上 THEN InventoryBootstrap 编辑器 SHALL 显示物品图标预览和基本信息
4. WHEN 用户右键点击物品条目 THEN InventoryBootstrap 编辑器 SHALL 显示上下文菜单（删除、复制、上移、下移）

### 需求 5

**用户故事：** 作为开发者，我希望保持原有的基础功能不变，以便现有的配置和工作流程不受影响。

#### 验收标准

1. WHEN 使用旧版单列表配置的场景加载时 THEN InventoryBootstrap SHALL 自动将旧配置迁移到新的多列表结构中
2. WHEN 调用 Apply() 方法时 THEN InventoryBootstrap SHALL 按照原有逻辑注入物品到背包
3. WHEN runOnStart 或 runOnBuild 选项启用时 THEN InventoryBootstrap SHALL 在相应环境下自动执行注入
4. WHEN clearInventoryFirst 选项启用时 THEN InventoryBootstrap SHALL 在注入前清空背包

