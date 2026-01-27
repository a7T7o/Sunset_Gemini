# 设计文档

## 概述

优化 InventoryBootstrap 系统，支持多物品列表管理、批量添加、选择性注入等高级功能，同时保持向后兼容性。

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                    InventoryBootstrap                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  BootItemList[] itemLists  (多列表数据)              │    │
│  │  ├─ string name           (列表名称)                 │    │
│  │  ├─ bool enabled          (是否启用)                 │    │
│  │  ├─ bool foldout          (折叠状态)                 │    │
│  │  └─ List<BootItem> items  (物品列表)                 │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  旧版兼容字段 (自动迁移)                              │    │
│  │  └─ List<BootItem> items  (旧版单列表)               │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              InventoryBootstrapEditor                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  多列表管理 UI                                        │    │
│  │  ├─ 添加/删除列表按钮                                 │    │
│  │  ├─ 列表名称编辑                                      │    │
│  │  ├─ 启用/禁用复选框                                   │    │
│  │  └─ 折叠/展开控制                                     │    │
│  └─────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  批量添加功能                                         │    │
│  │  ├─ 拖放区域 (DragAndDrop)                           │    │
│  │  └─ 批量选择弹窗                                      │    │
│  └─────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  物品条目 UI                                          │    │
│  │  ├─ 图标预览                                          │    │
│  │  ├─ 右键上下文菜单                                    │    │
│  │  └─ 快速编辑控件                                      │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## 组件与接口

### BootItemList 结构

```csharp
[Serializable]
public class BootItemList
{
    public string name = "新列表";
    public bool enabled = true;
    public bool foldout = true;
    public List<BootItem> items = new List<BootItem>();
}
```

### InventoryBootstrap 核心方法

| 方法 | 说明 |
|------|------|
| Apply() | 执行注入，遍历所有启用的列表 |
| MigrateLegacyData() | 将旧版单列表数据迁移到新结构 |
| GetAllEnabledItems() | 获取所有启用列表中的物品 |

### InventoryBootstrapEditor 核心方法

| 方法 | 说明 |
|------|------|
| DrawListHeader() | 绘制列表头部（名称、启用、折叠） |
| DrawItemEntry() | 绘制单个物品条目 |
| DrawDropZone() | 绘制拖放区域 |
| HandleDragAndDrop() | 处理拖放事件 |
| ShowBatchSelectWindow() | 显示批量选择弹窗 |
| ShowItemContextMenu() | 显示物品右键菜单 |

## 数据模型

### 新数据结构

```csharp
// 物品列表（新增）
[Serializable]
public class BootItemList
{
    public string name = "新列表";
    public bool enabled = true;
    [NonSerialized] public bool foldout = true;
    public List<BootItem> items = new List<BootItem>();
}

// 物品条目（保持不变）
[Serializable]
public struct BootItem
{
    public ItemData item;
    [Range(0, 4)] public int quality;
    [Min(1)] public int amount;
}
```

### 字段布局

```csharp
public class InventoryBootstrap : MonoBehaviour
{
    // 引用
    [SerializeField, HideInInspector] private InventoryService inventory;
    
    // 启动选项
    [SerializeField] private bool runOnStart = false;
    [SerializeField] private bool runOnBuild = true;
    [SerializeField] private bool clearInventoryFirst = false;
    
    // 新版多列表
    [SerializeField] private List<BootItemList> itemLists = new List<BootItemList>();
    
    // 旧版兼容（自动迁移后清空）
    [SerializeField, HideInInspector] private List<BootItem> items = new List<BootItem>();
}
```

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 启用列表物品注入

*对于任意* 启用的物品列表，当执行 Apply() 时，该列表中的所有物品应被添加到背包中（在容量允许的情况下）

**Validates: Requirements 2.1**

### Property 2: 禁用列表物品跳过

*对于任意* 禁用的物品列表，当执行 Apply() 时，该列表中的物品不应被添加到背包中

**Validates: Requirements 2.2**

### Property 3: 容量溢出按序截断

*对于任意* 物品列表集合，当总物品数超过背包容量时，应按列表顺序注入直到背包满，后续物品被跳过

**Validates: Requirements 2.3**

### Property 4: 批量添加默认值

*对于任意* 批量添加的物品，其默认品质应为 0，默认数量应为 1

**Validates: Requirements 3.3**

### Property 5: 旧版数据迁移完整性

*对于任意* 旧版单列表配置，迁移后新结构应包含所有原有物品，且物品属性保持不变

**Validates: Requirements 5.1**

### Property 6: Apply 逻辑一致性

*对于任意* 有效的物品配置，Apply() 方法应将物品正确添加到背包，返回值与原有逻辑一致

**Validates: Requirements 5.2**

### Property 7: 清空选项生效

*对于任意* 背包状态，当 clearInventoryFirst 启用时，Apply() 应先清空背包再注入物品

**Validates: Requirements 5.4**

## 错误处理

| 场景 | 处理方式 |
|------|----------|
| ItemData 引用为空 | 跳过该条目，输出警告日志 |
| InventoryService 未找到 | 输出错误日志，终止注入 |
| 背包已满 | 记录未注入物品数量，输出警告 |
| 列表名称为空 | 使用默认名称"未命名列表" |

## 测试策略

### 单元测试

- 测试 MigrateLegacyData() 迁移逻辑
- 测试 GetAllEnabledItems() 过滤逻辑
- 测试 Apply() 注入逻辑

### 属性测试

使用 NUnit 进行属性测试：

- 验证启用/禁用列表的注入行为
- 验证容量溢出时的截断行为
- 验证迁移后数据完整性

### 手动测试

- 编辑器 UI 交互测试
- 拖放功能测试
- 性能测试（大量物品时的响应速度）

