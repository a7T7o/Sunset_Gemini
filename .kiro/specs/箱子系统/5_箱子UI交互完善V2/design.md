# 箱子 UI 交互完善 V2 - 设计文档

## 概述

本文档描述修复箱子 UI 系统根本性架构缺陷的技术设计。基于 Code Reaper Review Session 11 的锐评及强制修正指令，我们将：

1. **修复 UI 互斥与导航**（P0-1 新增）
2. **修复 `_database` 初始化问题**（P0-3）
3. **实现跨容器拖拽（SlotDragContext）**（P0-2 - 完整实现，非占位）
4. **实施严格的日志规范**
5. **添加 Sort 方法的事件触发**（P0-4）

**锐评来源**：`.kiro/specs/箱子系统/4_箱子UI交互完善/code-reaper-reviews/review-session11-up-mirror-critique.md`

**预计总时间**：3-4 小时（不含回归测试）

---

## 设计 0：UI 互斥与导航修复（P0-1 新增）

### 问题分析

**用户复现的问题**：
> BoxOpen → Tab 关 → 右键远处箱子B → 直接打开背包，不导航

**根本原因**：
1. `GameInputManager` 内部有自定义的 `CloseBoxPanelIfOpen` 分支直接关闭 Box
2. Tab/B/M/L/O 的面板切换没有统一走 `PackagePanelTabsUI` 接口
3. `IsAnyPanelOpen` 返回值与视觉状态不一致

### 解决方案

#### 1. 移除 GameInputManager 对 Box 的直接关闭分支

**修改前**（问题代码）：
```csharp
// GameInputManager.cs
private void HandleTabKey()
{
    // ❌ 直接关闭 Box，绕过 tabs 接口
    if (BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen)
    {
        BoxPanelUI.ActiveInstance.Close();
    }
    // ...
}
```

**修改后**：
```csharp
// GameInputManager.cs
private void HandleTabKey()
{
    // ✅ 统一走 tabs 接口
    var tabs = PackagePanelTabsUI.Instance;
    if (tabs != null)
    {
        tabs.OpenOrToggle(0); // 0 = Props 页面
    }
}
```

#### 2. 统一调用 tabs 接口

**PackagePanelTabsUI.OpenOrToggle() 修改**：
```csharp
public void OpenOrToggle(int pageIndex)
{
    // 🔥 统一关闭 Box（如果打开）
    CloseBoxPanelIfOpen();
    
    // 恢复 Main/Top（如果被隐藏）
    RestoreMainAndTop();
    
    // 原有逻辑
    if (IsOpen && _currentPageIndex == pageIndex)
    {
        ClosePanel();
    }
    else
    {
        OpenPanel(pageIndex);
    }
}

public void OpenPanel(int pageIndex)
{
    // 🔥 统一关闭 Box（如果打开）
    CloseBoxPanelIfOpen();
    
    // 恢复 Main/Top（如果被隐藏）
    RestoreMainAndTop();
    
    // 原有逻辑
    // ...
}

private void CloseBoxPanelIfOpen()
{
    if (BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen)
    {
        BoxPanelUI.ActiveInstance.Close();
    }
}

private void RestoreMainAndTop()
{
    if (_mainParent != null) _mainParent.SetActive(true);
    if (_topParent != null) _topParent.SetActive(true);
}
```

#### 3. 保证 IsAnyPanelOpen 严格反映真实状态

```csharp
// PackagePanelTabsUI.cs
public bool IsAnyPanelOpen
{
    get
    {
        // 检查 panelRoot 是否真实激活
        bool panelRootActive = _panelRoot != null && _panelRoot.activeSelf;
        
        // 检查 BoxPanelUI 是否真实打开
        bool boxOpen = BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen;
        
        return panelRootActive || boxOpen;
    }
}
```

### 测试用例

| 测试 | 操作 | 预期结果 |
|------|------|---------|
| T0-1 | BoxOpen → Tab | 背包页面出现、Box 关闭 |
| T0-2 | BoxOpen → B | 背包 Recipes 页出现、Box 关闭 |
| T0-3 | NoPanel → 右键远处箱子 | 导航到位后开 UI |
| T0-4 | BoxOpen → 右键另一个远处箱子 | 导航到新箱子并切换箱子 UI |

---

## 设计 1：修复 `_database` 初始化问题

### 问题分析

**时序问题**：
```
BoxPanelUI.Start()
    └── _database = _inventoryService.Database  // ✅ 有值

... (时间流逝) ...

BoxPanelUI.Open(chest)
    └── BindChestSlotData()
            └── if (_database == null) return;  // ❌ 为 null！
```

**可能的原因**：
1. `_inventoryService` 在 Start 和 Open 之间被销毁或重置
2. `_inventoryService.Database` 在 Start 和 Open 之间被设置为 null
3. 场景切换或其他生命周期事件导致引用丢失

### 解决方案

**方案 A：在 Open() 时重新获取 _database（推荐）**

防御性编程，即使 Start 时有值也重新获取。

```csharp
public void Open(ChestController chest)
{
    if (chest == null)
    {
        Debug.LogWarning("[BoxPanelUI] 尝试打开空箱子");
        return;
    }

    // 检查箱子是否可以打开
    var result = chest.TryOpen();
    if (result != OpenResult.Success)
    {
        Debug.Log($"[BoxPanelUI] 无法打开箱子: {result}");
        return;
    }

    _currentChest = chest;

    // 🔥 修复 1：重新获取 database（防御性编程）
    // 优先级 1：从箱子的 Inventory 获取
    if (_database == null && chest?.Inventory?.Database != null)
    {
        _database = chest.Inventory.Database;
    }
    
    // 优先级 2：从 InventoryService 获取
    if (_database == null && _inventoryService != null)
    {
        _database = _inventoryService.Database;
    }
    
    // 优先级 3：尝试查找 InventoryService
    if (_database == null)
    {
        _inventoryService = FindFirstObjectByType<InventoryService>();
        if (_inventoryService != null)
        {
            _database = _inventoryService.Database;
        }
    }
    
    // 🔥 修复 2：最后的防御 - 如果还是 null，输出错误并返回
    if (_database == null)
    {
        Debug.LogError("[BoxPanelUI] Open 失败: _database 为 null，无法绑定槽位");
        return;
    }

    // 设置数据库引用到箱子
    if (_database != null)
    {
        chest.SetDatabase(_database);
    }

    // 关闭其他已打开的 BoxPanelUI（互斥）
    if (_activeInstance != null && _activeInstance != this && _activeInstance._isOpen)
    {
        _activeInstance.Close();
    }
    _activeInstance = this;

    // 显示面板
    gameObject.SetActive(true);
    _isOpen = true;

    // 订阅箱子库存事件
    SubscribeToChest();

    // 刷新UI
    RefreshUI();

    Debug.Log($"[BoxPanelUI] 打开箱子: {chest.StorageData?.itemName}, 容量={chest.StorageData?.storageCapacity}");
}
```

**方案 B：检查 ChestController.SetDatabase() 调用时机**

确认 `ChestController.SetDatabase()` 在 `BoxPanelUI.Open()` 之前被调用。

**推荐**：方案 A（防御性编程，更可靠）

---

## 设计 2：实现跨容器拖拽（SlotDragContext）（P0-2 完整实现）

### 问题分析

**当前架构**：
```
InventorySlotUI (UI 层)
    ├── Container: IItemContainer (可以是 ChestInventory 或 InventoryService)
    └── SlotIndex: int

InventorySlotInteraction (交互层)
    └── 所有交互都通过 InventoryInteractionManager 处理
        └── 只支持 InventoryService
```

**问题**：
- UI 层支持多种容器，但交互层只支持单一容器
- 两层之间没有正确传递容器信息
- 跨容器拖拽（Up↔Down）完全不可用

### 解决方案

**核心思路**：新增轻量级 `SlotDragContext` 静态服务类，记录拖拽上下文

#### 1. SlotDragContext 静态服务类

```csharp
// SlotDragContext.cs（新建文件）
using UnityEngine;

namespace YYY.UI.Inventory
{
    /// <summary>
    /// 槽位拖拽上下文 - 记录拖拽源信息，支持跨容器拖拽
    /// </summary>
    public static class SlotDragContext
    {
        /// <summary>源容器（ChestInventory 或 InventoryService）</summary>
        public static IItemContainer SourceContainer { get; private set; }
        
        /// <summary>源槽位索引</summary>
        public static int SourceIndex { get; private set; } = -1;
        
        /// <summary>源物品快照</summary>
        public static ItemStack SourceItem { get; private set; }
        
        /// <summary>是否来自装备槽</summary>
        public static bool SourceIsEquip { get; private set; }
        
        /// <summary>是否有有效的拖拽上下文</summary>
        public static bool IsValid => SourceContainer != null && SourceIndex >= 0;
        
        /// <summary>源是否为箱子</summary>
        public static bool IsSourceChest => SourceContainer is ChestInventory;
        
        /// <summary>源是否为背包</summary>
        public static bool IsSourceInventory => SourceContainer is InventoryService;
        
        /// <summary>
        /// 开始拖拽 - 记录源信息
        /// </summary>
        public static void BeginDrag(IItemContainer container, int index, ItemStack item, bool isEquip = false)
        {
            SourceContainer = container;
            SourceIndex = index;
            SourceItem = item;
            SourceIsEquip = isEquip;
            
            Debug.Log($"[SlotDragContext] BeginDrag: container={container?.GetType().Name}, index={index}, item={item.itemId}x{item.amount}");
        }
        
        /// <summary>
        /// 清空拖拽上下文
        /// </summary>
        public static void Clear()
        {
            SourceContainer = null;
            SourceIndex = -1;
            SourceItem = ItemStack.Empty;
            SourceIsEquip = false;
        }
    }
}
```

#### 2. InventorySlotInteraction 修改 - 容器类型判断

```csharp
// InventorySlotInteraction.cs

/// <summary>
/// 获取当前容器
/// </summary>
private IItemContainer CurrentContainer
{
    get
    {
        if (!isEquip && inventorySlotUI != null)
            return inventorySlotUI.Container;
        return null;
    }
}

/// <summary>
/// 判断是否为箱子槽位
/// </summary>
private bool IsChestSlot => CurrentContainer is ChestInventory;

/// <summary>
/// 获取当前槽位索引
/// </summary>
private int SlotIndex => isEquip ? equipSlotIndex : (inventorySlotUI?.SlotIndex ?? -1);
```

#### 3. OnBeginDrag 修改 - 记录拖拽上下文

```csharp
public void OnBeginDrag(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;
    
    // 检查拖拽条件
    float holdTime = Time.time - pressTime;
    float moveDistance = Vector2.Distance(eventData.position, pressPosition);
    
    if (holdTime < 0.15f && moveDistance < 5f) return;
    
    isDragging = true;
    int index = SlotIndex;
    
    // 🔥 箱子槽位拖拽
    if (IsChestSlot)
    {
        var chest = CurrentContainer as ChestInventory;
        if (chest == null) return;
        
        var item = chest.GetSlot(index);
        if (item.IsEmpty) return;
        
        // 记录拖拽上下文
        SlotDragContext.BeginDrag(chest, index, item, false);
        
        // 显示拖拽图标
        var manager = InventoryInteractionManager.Instance;
        if (manager != null)
        {
            manager.ShowDragIcon(item);
        }
        return;
    }
    
    // 🔥 背包槽位拖拽
    if (CurrentContainer is InventoryService inventory)
    {
        var item = inventory.GetSlot(index);
        if (item.IsEmpty) return;
        
        // 记录拖拽上下文
        SlotDragContext.BeginDrag(inventory, index, item, isEquip);
        
        // 使用原有 Manager 逻辑显示拖拽图标
        var manager = InventoryInteractionManager.Instance;
        if (manager != null)
        {
            manager.OnSlotBeginDrag(index, isEquip, eventData);
        }
        return;
    }
}
```

#### 4. OnDrop 修改 - 跨容器拖拽处理

```csharp
public void OnDrop(PointerEventData eventData)
{
    if (!SlotDragContext.IsValid) return;
    
    int targetIndex = SlotIndex;
    var targetContainer = CurrentContainer;
    
    // 🔥 目标是箱子槽位
    if (targetContainer is ChestInventory targetChest)
    {
        HandleDropToChest(targetChest, targetIndex);
        return;
    }
    
    // 🔥 目标是背包槽位
    if (targetContainer is InventoryService targetInventory)
    {
        HandleDropToInventory(targetInventory, targetIndex);
        return;
    }
}

/// <summary>
/// 处理 Drop 到箱子槽位
/// </summary>
private void HandleDropToChest(ChestInventory targetChest, int targetIndex)
{
    var sourceContainer = SlotDragContext.SourceContainer;
    int sourceIndex = SlotDragContext.SourceIndex;
    
    // 情况 1：箱子 → 箱子（同一个箱子内交换）
    if (sourceContainer == targetChest)
    {
        targetChest.SwapOrMerge(sourceIndex, targetIndex);
        Debug.Log($"[Interaction] 箱子内拖拽: {sourceIndex} -> {targetIndex}");
    }
    // 情况 2：背包 → 箱子
    else if (sourceContainer is InventoryService sourceInventory)
    {
        targetChest.TransferFromInventory(sourceInventory, sourceIndex, targetIndex);
        Debug.Log($"[Interaction] 背包→箱子: inv[{sourceIndex}] -> chest[{targetIndex}]");
    }
    
    // 清空拖拽上下文
    SlotDragContext.Clear();
    ClearDragIcon();
}

/// <summary>
/// 处理 Drop 到背包槽位
/// </summary>
private void HandleDropToInventory(InventoryService targetInventory, int targetIndex)
{
    var sourceContainer = SlotDragContext.SourceContainer;
    int sourceIndex = SlotDragContext.SourceIndex;
    
    // 情况 1：箱子 → 背包
    if (sourceContainer is ChestInventory sourceChest)
    {
        sourceChest.TransferToInventory(targetInventory, sourceIndex, targetIndex);
        Debug.Log($"[Interaction] 箱子→背包: chest[{sourceIndex}] -> inv[{targetIndex}]");
    }
    // 情况 2：背包 → 背包（沿用原有 Manager 逻辑）
    else if (sourceContainer is InventoryService)
    {
        var manager = InventoryInteractionManager.Instance;
        if (manager != null)
        {
            manager.OnSlotDrop(targetIndex, isEquip);
        }
    }
    
    // 清空拖拽上下文
    SlotDragContext.Clear();
    ClearDragIcon();
}

/// <summary>
/// 清空拖拽图标
/// </summary>
private void ClearDragIcon()
{
    var manager = InventoryInteractionManager.Instance;
    if (manager != null)
    {
        manager.ClearHeldItem();
    }
}
```

#### 5. OnEndDrag 修改 - 清理拖拽状态

```csharp
public void OnEndDrag(PointerEventData eventData)
{
    isDragging = false;
    
    // 如果拖拽上下文仍有效（说明没有成功 Drop），清空它
    if (SlotDragContext.IsValid)
    {
        SlotDragContext.Clear();
        ClearDragIcon();
    }
}
```

### 交互场景矩阵（完整实现）

| 拖拽起点 | 拖拽终点 | 调用方法 | 实现状态 |
|---------|---------|---------|---------|
| Up 槽位 | Up 槽位 | `ChestInventory.SwapOrMerge(a, b)` | ✅ 完整实现 |
| Up 槽位 | Down 槽位 | `ChestInventory.TransferToInventory(inventory, chestSlot, inventorySlot)` | ✅ 完整实现 |
| Down 槽位 | Up 槽位 | `ChestInventory.TransferFromInventory(inventory, inventorySlot, chestSlot)` | ✅ 完整实现 |
| Down 槽位 | Down 槽位 | `InventoryInteractionManager`（原有逻辑） | ✅ 保持不变 |

### 测试用例

| 测试 | 操作 | 预期结果 |
|------|------|---------|
| T2-1 | Up→Up 拖拽 | 箱子内物品交换/合并，UI 自动刷新 |
| T2-2 | Down→Up 拖拽 | 物品从背包进入箱子，两区域 UI 自动刷新 |
| T2-3 | Up→Down 拖拽 | 物品从箱子进入背包，两区域 UI 自动刷新 |
| T2-4 | Down→Down 拖拽 | 背包内物品交换/合并（原有逻辑） |

---

## 设计 3：实施日志规范

### 问题分析

**326 条日志的组成**：
- 24 条 `BindChestSlotData 失败` 警告（关键信息）
- ~250 条 `[UIItemIconScaler]` 日志（噪音）
- ~50 条 `Refresh` 相关日志（噪音）

**问题**：
- 关键错误信息被大量无用日志淹没
- 开发者需要手动筛选才能找到问题根源

### 解决方案

#### 1. UIItemIconScaler 添加日志开关

```csharp
// UIItemIconScaler.cs

[Header("Debug")]
[SerializeField] private bool showDebugInfo = false;

private void UpdateScale()
{
    // ... 计算逻辑 ...
    
    // 🔥 只在开关开启时输出日志
    if (showDebugInfo)
    {
        Debug.Log($"[UIItemIconScaler] sprite={sprite?.name}, size={sizeDelta}");
    }
}
```

#### 2. BoxPanelUI 错误日志去重

```csharp
// BoxPanelUI.cs

private static bool _hasLoggedBindFailure = false;

private void BindChestSlotData(InventorySlotUI slot, int index)
{
    if (_currentChest?.Inventory == null || _database == null)
    {
        // 🔥 只打印一次错误日志
        if (!_hasLoggedBindFailure)
        {
            Debug.LogWarning($"[BoxPanelUI] BindChestSlotData 失败: chest={_currentChest != null}, inventory={_currentChest?.Inventory != null}, db={_database != null}");
            _hasLoggedBindFailure = true;
        }
        return;
    }

    // 🔥 成功时不输出日志
    slot.BindContainer(_currentChest.Inventory, index);
}
```

#### 3. InventorySlotUI 只在失败时输出日志

```csharp
// InventorySlotUI.cs

public void BindContainer(IItemContainer container, int slotIndex)
{
    if (container == null)
    {
        Debug.LogWarning($"[InventorySlotUI] BindContainer 失败: container 为 null");
        return;
    }
    
    // 🔥 成功时不输出日志
    Container = container;
    SlotIndex = slotIndex;
    Refresh();
}

public void Refresh()
{
    // 🔥 Refresh 是高频函数，不输出日志
    // ...
}
```

#### 4. 日志分级标准

| 级别 | 使用场景 | 示例 |
|------|---------|------|
| `Debug.LogError()` | 导致功能完全失效的错误 | `_database 为 null 且无法获取` |
| `Debug.LogWarning()` | 功能部分失效或降级 | `BindChestSlotData 失败` |
| `Debug.Log()` | 调试信息（需要开关控制） | `打开箱子: {chest.name}` |

### 验收标准

修复完成后，日志输出必须满足：

- [ ] 打开箱子 UI：≤ 3 行日志
- [ ] 点击槽位：≤ 2 行日志
- [ ] 拖拽物品：≤ 3 行日志
- [ ] Sort 操作：≤ 2 行日志
- [ ] 关闭箱子 UI：≤ 1 行日志
- [ ] 无重复错误日志
- [ ] 无 Refresh 相关的详细日志

**总计**：一次完整的箱子交互流程（打开 → 操作 → 关闭）日志数量 ≤ 15 行

---

## 设计 4：添加 Sort 方法的事件触发

### 问题分析

**当前代码**：
```csharp
// ChestInventory.cs
public void Sort()
{
    // ... 排序逻辑 ...
    
    // 写回槽位
    for (int i = 0; i < _capacity; i++)
    {
        _slots[i] = i < merged.Count ? merged[i] : ItemStack.Empty;
    }
    
    // ❌ 没有触发事件，UI 不会刷新
}
```

**问题**：
- Sort 方法修改了槽位数据，但没有通知 UI 刷新
- BoxPanelUI 订阅了 `OnInventoryChanged` 事件，但 Sort 没有触发

### 解决方案

#### 1. ChestInventory.Sort() 添加事件触发

```csharp
// ChestInventory.cs

public void Sort()
{
    // ... 排序逻辑 ...
    
    // 写回槽位
    for (int i = 0; i < _capacity; i++)
    {
        _slots[i] = i < merged.Count ? merged[i] : ItemStack.Empty;
    }

    // 🔥 触发全局刷新事件，通知 UI 更新
    RaiseInventoryChanged();
    
    Debug.Log($"[ChestInventory] Sort 完成");
}

private void RaiseInventoryChanged()
{
    OnInventoryChanged?.Invoke();
}
```

#### 2. InventoryService.Sort() 添加事件触发

```csharp
// InventoryService.cs

public void Sort()
{
    // ... 排序逻辑 ...
    
    // 写回槽位（从第二行开始）
    for (int i = 0; i < sortCount; i++)
    {
        int slotIndex = sortStart + i;
        slots[slotIndex] = i < merged.Count ? merged[i] : ItemStack.Empty;
    }

    // 🔥 触发全局刷新事件，通知 UI 更新
    RaiseInventoryChanged();
    
    Debug.Log($"[InventoryService] Sort 完成");
}

private void RaiseInventoryChanged()
{
    OnInventoryChanged?.Invoke();
}
```

#### 3. BoxPanelUI 订阅事件（已实现）

```csharp
// BoxPanelUI.cs

private void SubscribeToChest()
{
    if (_currentChest?.Inventory == null) return;

    _currentChest.Inventory.OnSlotChanged += OnChestSlotChanged;
    _currentChest.Inventory.OnInventoryChanged += OnChestInventoryChanged;  // ✅ 已订阅
}

private void OnChestInventoryChanged()
{
    Debug.Log("[BoxPanelUI] OnChestInventoryChanged - 刷新箱子槽位");
    RefreshChestSlots();  // ✅ 刷新 UI
}
```

---

## 数据流图

### 打开箱子流程

```
用户点击箱子
    ↓
GameInputManager.HandleInteractable()
    ↓
ChestController.OnInteract()
    ↓
ChestController.OpenBoxUI()
    ↓
PackagePanelTabsUI.OpenBoxUI(prefab)
    ├── EnsurePanelOpenForBox()
    ├── HideMainAndTop()
    └── Instantiate(prefab)
            ↓
        BoxPanelUI.Open(chest)
            ├── 🔥 重新获取 _database（防御性编程）
            ├── SubscribeToChest()
            └── RefreshUI()
                    ├── RefreshChestSlots()
                    │       └── BindChestSlotData()
                    │               └── slot.BindContainer(ChestInventory, index)
                    └── RefreshInventorySlots()
                            └── slot.Bind(InventoryService, index)
```

### 拖拽交互流程

```
用户拖拽 Up 槽位
    ↓
InventorySlotInteraction.OnBeginDrag()
    ├── 检查 Container 类型
    ├── if (Container is ChestInventory)
    │       └── 记录拖拽状态（draggedChestItem, draggedChestSlotIndex）
    └── else
            └── InventoryInteractionManager.OnBeginDrag()

用户 Drop 到另一个 Up 槽位
    ↓
InventorySlotInteraction.OnDrop()
    ├── 检查 Container 类型
    ├── if (Container is ChestInventory)
    │       └── if (draggedChestInventory == chest)
    │               └── ChestInventory.SwapOrMerge(a, b)
    │                       └── 触发 OnSlotChanged 事件
    │                               └── BoxPanelUI.OnChestSlotChanged()
    │                                       └── RefreshSingleChestSlot()
    └── else
            └── InventoryInteractionManager.OnDrop()
```

### Sort 操作流程

```
用户点击 Sort Up 按钮
    ↓
BoxPanelUI.OnSortUpClicked()
    ↓
ChestInventory.Sort()
    ├── 排序逻辑
    ├── 写回槽位
    └── 🔥 RaiseInventoryChanged()
            └── 触发 OnInventoryChanged 事件
                    └── BoxPanelUI.OnChestInventoryChanged()
                            └── RefreshChestSlots()
```

---

## 相关文件修改清单

| 文件 | 修改内容 | 代码行数 |
|------|----------|---------|
| `SlotDragContext.cs` | **新建** - 拖拽上下文静态服务类 | ~50 行 |
| `PackagePanelTabsUI.cs` | 添加 CloseBoxPanelIfOpen、RestoreMainAndTop、修复 IsAnyPanelOpen | ~30 行 |
| `GameInputManager.cs` | 移除对 Box 的直接关闭分支，统一走 tabs 接口 | ~20 行 |
| `BoxPanelUI.cs` | 修复 `_database` 初始化，添加日志去重 | ~30 行 |
| `InventorySlotInteraction.cs` | 扩展支持 ChestInventory，使用 SlotDragContext | ~100 行 |
| `ChestInventory.cs` | 添加 Sort 事件触发 | ~3 行 |
| `InventoryService.cs` | 添加 Sort 事件触发 | ~3 行 |
| `UIItemIconScaler.cs` | 添加日志开关 | ~5 行 |
| `InventorySlotUI.cs` | 移除成功日志 | ~2 行 |

**总计**：约 243 行代码修改 + 50 行新建文件 = ~293 行

---

## 验证清单

修复完成后，必须验证：

### 功能验证

- [ ] **UI 互斥与导航**（P0-1 新增）
  - [ ] BoxOpen → Tab：背包页面出现、Box 关闭
  - [ ] BoxOpen → B/M/L/O：对应背包页面出现、Box 关闭
  - [ ] NoPanel → 右键远处箱子：导航到位后开 UI
  - [ ] BoxOpen → 右键另一个远处箱子：导航到新箱子并切换箱子 UI
  - [ ] IsAnyPanelOpen 与视觉状态一致

- [ ] **_database 初始化**（P0-3）
  - [ ] `BindChestSlotData()` 成功调用（`_database` 不为 null）
  - [ ] Up 槽位的 `Container` 属性指向 `ChestInventory`
  - [ ] Up 槽位显示箱子内容（不是背包内容）

- [ ] **跨容器拖拽**（P0-2）
  - [ ] Up→Up 拖拽：箱子内物品交换/合并
  - [ ] Down→Up 拖拽：物品从背包进入箱子
  - [ ] Up→Down 拖拽：物品从箱子进入背包
  - [ ] Down→Down 拖拽：背包内物品交换/合并（原有逻辑）
  - [ ] 全过程 UI 自动刷新，无卡死、无镜像

- [ ] **Sort 功能**（P0-4）
  - [ ] Sort Up 按钮对箱子排序生效
  - [ ] Sort Down 按钮对背包排序生效

### 日志验证

- [ ] 打开箱子 UI：≤ 3 行日志
- [ ] 点击槽位：≤ 2 行日志
- [ ] 拖拽物品：≤ 3 行日志
- [ ] Sort 操作：≤ 2 行日志
- [ ] 关闭箱子 UI：≤ 1 行日志
- [ ] 无重复错误日志
- [ ] 无 Refresh 相关的详细日志

---

## 教训总结

### 教训 1：双层绑定的陷阱

**问题**：`InventorySlotUI` 有 `Container` 属性，但 `InventorySlotInteraction` 忽略了它

**根源**：
- UI 层（`InventorySlotUI`）支持多种容器
- 交互层（`InventorySlotInteraction`）只支持单一容器
- 两层之间没有正确传递容器信息

**原则**：**UI 绑定和交互逻辑必须使用相同的数据源**

### 教训 2：防御性编程的重要性

**问题**：`_database` 在 Start 时有值，但 Open 时为 null

**根源**：依赖初始化顺序，没有防御性检查

**原则**：**关键依赖在使用前必须重新验证，不能假设初始化顺序**

### 教训 3：日志噪音是技术债

**问题**：326 条日志中只有 24 条是关键信息

**根源**：没有日志分级和开关控制

**原则**：**调试日志必须精准、简洁，关键错误必须突出显示**

---

## 下一步行动

1. **修复 UI 互斥与导航**（30 分钟）
2. **修复 `_database` 初始化**（15 分钟）
3. **实现跨容器拖拽（SlotDragContext）**（60 分钟）
4. **实施日志规范**（15 分钟）
5. **添加 Sort 事件触发**（10 分钟）
6. **完整验证清单**（30 分钟）

**总计**：约 3-4 小时完成修复（不含回归测试）
