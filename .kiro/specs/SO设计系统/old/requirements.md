# Requirements Document

## Introduction

本需求文档定义了 ScriptableObject (SO) 设计系统的扩展功能，包括批量修改工具、批量生成优化、配方批量创建等编辑器工具的需求规范。这些工具旨在提高游戏数据资产的创建和管理效率。

## Glossary

- **SO (ScriptableObject)**: Unity 的数据容器类，用于存储游戏物品、配方等数据
- **ItemData**: 物品数据基类，包含 ID、名称、图标等通用属性
- **RecipeData**: 配方数据类，定义制作所需材料和产物
- **ItemDatabase**: 物品数据库，统一管理所有物品和配方数据
- **MasterItemDatabase**: 主数据库资产文件，位于 Assets/Data/Database/
- **CraftingStation**: 制作设施枚举，定义配方在哪里制作
- **批量修改工具**: 对已存在的 SO 资产批量更新参数的编辑器工具
- **批量生成工具**: 从 Sprite 批量创建新 SO 资产的编辑器工具

## Requirements

### Requirement 1: SO 批量修改工具

**User Story:** As a 游戏开发者, I want to 批量修改已有 SO 资产的参数, so that 我可以快速统一调整多个物品的属性而无需逐个手动编辑。

#### Acceptance Criteria

1. WHEN 用户在 Project 窗口选择多个 SO 资产 THEN 批量修改工具 SHALL 自动识别并列出所有选中的 SO
2. WHEN 用户勾选某个参数的修改开关 THEN 批量修改工具 SHALL 仅修改该参数，未勾选的参数保持原值不变
3. WHEN 用户点击应用按钮 THEN 批量修改工具 SHALL 将勾选的参数值批量写入所有选中的 SO 资产
4. WHEN 批量修改完成 THEN 批量修改工具 SHALL 自动保存资产并刷新 AssetDatabase
5. WHEN 用户选择不同类型的 SO（如 ToolData、WeaponData）THEN 批量修改工具 SHALL 根据类型显示对应的专属参数字段
6. WHEN 批量修改工具启动 THEN 批量修改工具 SHALL 提供与批量生成工具一致的 UI 交互风格

### Requirement 2: 批量生成 SO 工具优化

**User Story:** As a 游戏开发者, I want to 在批量生成 SO 后自动同步到数据库, so that 我不需要手动执行数据库收集操作。

#### Acceptance Criteria

1. WHEN 批量生成 SO 完成 THEN 批量生成工具 SHALL 自动调用 ItemDatabase 的"自动收集所有物品数据"功能
2. WHEN 自动同步执行 THEN 批量生成工具 SHALL 在控制台输出同步结果日志
3. WHEN MasterItemDatabase 资产不存在 THEN 批量生成工具 SHALL 显示警告提示用户创建数据库
4. WHEN 批量生成工具调用数据库同步 THEN 批量生成工具 SHALL 不修改 ItemDatabase.cs 源代码，仅通过反射或公开方法调用

### Requirement 3: Recipe 批量创建工具

**User Story:** As a 游戏开发者, I want to 批量创建配方 SO 资产, so that 我可以快速建立游戏的制作系统数据。

#### Acceptance Criteria

1. WHEN 用户打开配方批量创建工具 THEN 配方批量创建工具 SHALL 显示配方 ID、名称、产物 ID、产物数量的输入区域
2. WHEN 用户添加材料项 THEN 配方批量创建工具 SHALL 支持动态添加/删除多个材料（itemID + amount）
3. WHEN 用户选择制作设施 THEN 配方批量创建工具 SHALL 提供 CraftingStation 枚举下拉选择
4. WHEN 用户点击批量创建 THEN 配方批量创建工具 SHALL 按行解析输入并创建对应的 RecipeData 资产
5. WHEN 配方创建完成 THEN 配方批量创建工具 SHALL 自动调用 ItemDatabase 的"自动收集所有配方数据"功能
6. WHEN 配方批量创建工具显示 UI THEN 配方批量创建工具 SHALL 采用与物品批量生成工具一致的交互风格
7. WHEN 用户输入配方 ID THEN 配方批量创建工具 SHALL 支持连续 ID 模式（首个 ID 后自动递增）

### Requirement 4: 数据库自动同步机制

**User Story:** As a 游戏开发者, I want to 所有批量工具都能自动同步数据库, so that 数据库始终保持最新状态。

#### Acceptance Criteria

1. WHEN 任何批量创建工具完成创建 THEN 该工具 SHALL 自动触发对应的数据库收集功能
2. WHEN 数据库同步完成 THEN 该工具 SHALL 显示同步结果（新增数量、总数量）
3. WHEN 数据库资产路径为 Assets/Data/Database/MasterItemDatabase.asset THEN 同步功能 SHALL 正确定位并更新该资产

