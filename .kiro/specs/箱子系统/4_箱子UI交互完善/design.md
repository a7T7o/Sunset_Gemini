# 箱子 UI 交互完善 - 设计文档

## 概述

本文档描述修复箱子 UI 系统核心交互问题的技术设计。

---

## 设计 1：Up 区域槽位绑定

### 问题分析

当前 `BoxPanelUI.CollectSlots()` 使用 `GetComponentsInChildren<InventorySlotUI>()` 收集槽位，但 Up 区域的子物体没有挂载 `InventorySlotUI` 组件。

### 解决方案

**方案 A：用户配置预制体（推荐）**

用户在 Unity 编辑器中为 Box UI 预制体的 Up 区域子物体添加 `InventorySlotUI` 组件。

**预制体结构**：
```
Box_24 (UI Prefab Root)
├── Up              (GridLayoutGroup)
│    ├── Up_00      (Toggle + InventorySlotUI + Image)  ← 需要添加
│    ├── Up_01      (Toggle + InventorySlotUI + Image)
│    └── ...
└── Down            (GridLayoutGroup)
     ├── Down_00    (Toggle + InventorySlotUI + Image)  ← 已配置
     └── ...
```

**方案 B：代码自动添加组件（不推荐）**

在 `CollectSlots()` 中检测并自动添加缺失的组件。违反"预制体绑定，不动态生成"原则。

### 选择

采用**方案 A**，由用户配置预制体。

### 用户操作步骤

1. 打开 `Assets/222_Prefabs/UI/Box_12.prefab`
2. 展开 Up 节点
3. 选中所有 Up_XX 子物体
4. 添加 `InventorySlotUI` 组件
5. 对 Box_24, Box_36, Box_48 重复以上步骤

---

## 设计 2：Down 区域索引修正

### 问题分析

当前代码：
```csharp
// BoxPanelUI.RefreshInventorySlots()
int startIndex = InventoryService.HotbarWidth; // = 12
```

用户澄清：Hotbar 是背包第一行的映射，完全共用。Down 区域应显示完整背包（0-35）。

### 解决方案

修改 `startIndex` 从 12 改为 0：

```csharp
// 修正后
int startIndex = 0; // Down 显示完整背包（0-35）
```

### 影响分析

| 修改前 | 修改后 |
|--------|--------|
| Down 显示 12-35（24 格） | Down 显示 0-35（36 格） |
| 第一行不显示 | 第一行与 Hotbar 同步 |

### 预制体要求

Down 区域需要 36 个槽位（3 行 × 12 列）。

---

## 设计 3：Tab 键面板切换修复

### 问题分析

当前流程：
```
打开箱子 → PackagePanelTabsUI.OpenBoxUI()
         → EnsurePanelOpenForBox()
         → HideMainAndTop()
         → 实例化 Box UI

关闭箱子 → PackagePanelTabsUI.CloseBoxUI()
         → boxPanelUI.Close()
         → Destroy(_activeBoxUI)
         → ShowMainAndTop()  ← 可能时机不对

再按 Tab → OpenOrToggle(0)
         → panelRoot.activeSelf = true  ← 但 Main/Top 可能仍隐藏
```

### 解决方案

确保 `CloseBoxUI()` 正确恢复 Main/Top 状态：

```csharp
public void CloseBoxUI()
{
    if (_activeBoxUI != null)
    {
        var boxPanelUI = _activeBoxUI.GetComponent<FarmGame.UI.BoxPanelUI>();
        if (boxPanelUI != null && boxPanelUI.IsOpen)
        {
            boxPanelUI.Close();
        }
        Destroy(_activeBoxUI);
        _activeBoxUI = null;
    }

    // 🔥 关键：确保 Main/Top 恢复
    ShowMainAndTop();
    
    // 🔥 新增：如果 panelRoot 仍然 active，确保页面可见
    if (panelRoot != null && panelRoot.activeSelf && currentIndex >= 0)
    {
        SetVisiblePage(currentIndex);
    }
}
```

### 验证点

1. 关闭箱子后 `topParent.activeSelf` 应为 true
2. 关闭箱子后 `pagesParent.activeSelf` 应为 true
3. 按 Tab 后背包面板正常显示

### 调用时机（锐评后新增）

`CloseBoxPanelIfOpen()` 必须在以下入口点被调用：

1. **`OpenPanel()`**：直接打开背包面板时
2. **`OpenOrToggle(int idx)`**：通过快捷键（Tab/B/M/L/O）打开背包时
3. **`HandlePanelHotkeys()`**：处理面板快捷键时（如果直接调用 OpenOrToggle 则已覆盖）

**调用顺序**：
```csharp
public void OpenOrToggle(int idx)
{
    CloseBoxPanelIfOpen();  // 🔥 第一步：关闭 Box UI
    // ... 原有逻辑
}
```

---

## 设计 4：箱子交互基于 Sprite Bounds

### 问题分析

当前交互检测基于 Collider，但箱子的 Collider 只覆盖底部（与树木类似）。

### 解决方案

**方案 A：修改 GameInputManager 的交互检测**

在 `HandleInteractable()` 中，对箱子使用 Sprite Bounds 检测：

```csharp
// 检测点击是否在 Sprite Bounds 内
if (chest != null)
{
    var bounds = chest.GetBounds(); // SpriteRenderer.bounds
    if (bounds.Contains(worldClickPos))
    {
        // 触发交互
    }
}
```

**方案 B：为箱子添加覆盖整个 Sprite 的触发器**

在箱子预制体上添加一个 BoxCollider2D (Trigger)，覆盖整个 Sprite。

**方案 C：使用 IInteractable 的 GetBounds()**

`ChestController` 已实现 `IResourceNode.GetBounds()`，返回 `SpriteRenderer.bounds`。可以复用此方法进行交互检测。

### 选择

采用**方案 C**，复用现有的 `GetBounds()` 方法。

### 实现细节

修改 `GameInputManager.HandleInteractable()` 或相关检测逻辑：

```csharp
// 对于实现 IResourceNode 的物体，使用 GetBounds() 检测
var resourceNode = interactable as IResourceNode;
if (resourceNode != null)
{
    var bounds = resourceNode.GetBounds();
    if (!bounds.Contains(worldClickPos))
    {
        return; // 点击不在 Sprite 范围内
    }
}
```

---

## 设计 5：箱子独立存储验证

### 当前实现分析

```csharp
// ChestController.Initialize()
_inventory = new ChestInventory(storageData.storageCapacity);
```

每次调用 `Initialize()` 都会创建新的 `ChestInventory` 实例，这是正确的。

### 验证方法

添加调试日志：

```csharp
// ChestController.Initialize()
_inventory = new ChestInventory(storageData.storageCapacity);
Debug.Log($"[ChestController] 创建 ChestInventory: instanceId={GetInstanceID()}, capacity={storageData.storageCapacity}");
```

### 测试步骤

1. 放置箱子 A，放入物品
2. 放置箱子 B，确认为空
3. 关闭并重新打开箱子 A，确认物品仍在
4. 检查控制台日志，确认每个箱子有独立的 instanceId

---

## 设计 6：箱子 Sprite 底部对齐与 Collider/NavGrid 同步

### 问题分析

箱子开合时 Sprite 切换，如果不做底部对齐，视觉上会出现"跳动"。同时，Collider 形状需要同步更新，否则物理碰撞和导航网格会与视觉不一致。

### 解决方案

**规范来源**：`maintenance-guidelines.md` 第十一章

#### 1. 底部对齐实现

```csharp
// ChestController.cs
private Vector3 _anchorWorldPos;
private bool _anchorInitialized = false;

private void Awake()
{
    if (!_anchorInitialized)
    {
        _anchorWorldPos = GetCurrentBottomCenterWorld();
        _anchorInitialized = true;
    }
}

private Vector3 GetCurrentBottomCenterWorld()
{
    if (_spriteRenderer == null || _spriteRenderer.sprite == null)
        return transform.position;
    
    var bounds = _spriteRenderer.bounds;
    return new Vector3(bounds.center.x, bounds.min.y, transform.position.z);
}

private void ApplySpriteWithBottomAlign(Sprite newSprite)
{
    if (_spriteRenderer == null || newSprite == null) return;
    
    _spriteRenderer.sprite = newSprite;
    
    Vector3 newBottomCenter = GetCurrentBottomCenterWorld();
    Vector3 delta = _anchorWorldPos - newBottomCenter;
    transform.position += delta;
}
```

#### 2. Collider 同步实现

```csharp
private void UpdateColliderShape()
{
    if (_polyCollider == null || _spriteRenderer == null) return;
    
    var sprite = _spriteRenderer.sprite;
    int shapeCount = sprite.GetPhysicsShapeCount();
    
    _polyCollider.pathCount = shapeCount;
    
    var physicsShape = new List<Vector2>();
    for (int i = 0; i < shapeCount; i++)
    {
        physicsShape.Clear();
        sprite.GetPhysicsShape(i, physicsShape);
        _polyCollider.SetPath(i, physicsShape);
    }
    
    Physics2D.SyncTransforms();
}
```

#### 3. NavGrid 刷新

```csharp
private void RequestNavGridRefresh()
{
    NavGrid2D.OnRequestGridRefresh?.Invoke();
}
```

#### 4. 完整调用链

```csharp
public void SetOpen(bool open)
{
    _isOpen = open;
    UpdateSpriteForState();      // 1. 更新 Sprite（含底部对齐）
    UpdateColliderShape();       // 2. 重建 Collider
    RequestNavGridRefresh();     // 3. 刷新 NavGrid
}

private void UpdateSpriteForState()
{
    Sprite targetSprite = GetSpriteForCurrentState();
    ApplySpriteWithBottomAlign(targetSprite);
}
```

---

## 设计 7：面板打开时禁用世界输入

### 问题分析

用户明确要求"详细记录，而不是省略"。当 Box UI 或 PackagePanel 打开时，需要禁用对世界的输入。

### 当前实现分析

`GameInputManager` 中已有以下机制：

1. **IsAnyPanelOpen 检查**：
```csharp
if (PackagePanelTabsUI.Instance != null && PackagePanelTabsUI.Instance.IsOpen)
    return; // 禁用世界输入
```

2. **EventSystem UI 检查**：
```csharp
if (EventSystem.current.IsPointerOverGameObject())
    return; // 鼠标在 UI 上，禁用世界输入
```

3. **pointerOverUI 标志**：
```csharp
private bool pointerOverUI;
// 在 Update 中更新，用于判断鼠标是否在 UI 上
```

### 需要验证的点

1. Box UI 打开时，`PackagePanelTabsUI.Instance.IsOpen` 是否返回 true？
   - 当前设计：Box UI 在 PackagePanel 内部，panelRoot 保持 active，所以 IsOpen 应该返回 true
   
2. 以下输入是否被正确禁用？
   - 左键点击世界（使用工具）
   - 右键点击世界（自动导航）
   - 滚轮切换工具
   - 数字键切换工具

3. 以下输入是否仍然可用？
   - Tab 键（切换面板）
   - ESC 键（关闭面板）

### 设计结论

现有实现应该已经覆盖了大部分场景，但需要：
1. 验证 Box UI 打开时 `IsOpen` 返回值
2. 确认滚轮和数字键的禁用逻辑
3. 在文档中记录完整的禁用列表

---

## 设计 8：箱子 UI 与背包 UI 的互斥状态机

### 状态定义

```
┌─────────────────────────────────────────────────────────────┐
│                        状态机                               │
├─────────────────────────────────────────────────────────────┤
│  NoPanel        - 无 UI 打开，玩家在世界中                   │
│  BackpackOpen   - 背包面板打开（Main/Top 可见）              │
│  BoxOpen        - 箱子 UI 打开（Main/Top 隐藏，Box UI 可见） │
└─────────────────────────────────────────────────────────────┘
```

### 状态转换图

```
                    Tab
    ┌──────────────────────────────┐
    │                              │
    ▼                              │
┌─────────┐    点击箱子      ┌─────────────┐
│ NoPanel │ ───────────────► │   BoxOpen   │
└─────────┘                  └─────────────┘
    │  ▲                          │  ▲
    │  │                          │  │
Tab │  │ Tab/ESC              Tab │  │ ESC
    │  │                          │  │
    ▼  │                          ▼  │
┌─────────────┐   点击箱子   ┌─────────────┐
│BackpackOpen │ ────────────►│   BoxOpen   │
└─────────────┘              └─────────────┘
```

### 关键实现点

#### 1. 记录进入 Box 模式前的状态

```csharp
// PackagePanelTabsUI.cs
private bool _wasBackpackOpenBeforeBox = false;

public void OpenBoxUI(GameObject boxUiPrefab)
{
    _wasBackpackOpenBeforeBox = IsBackpackVisible(); // 记录进入前状态
    EnsurePanelOpenForBox();
    HideMainAndTop();
    // ... 实例化 Box UI
}
```

#### 2. 关闭 Box 时根据进入前状态决定行为

```csharp
public void CloseBoxUI()
{
    if (_activeBoxUI != null)
    {
        // ... 销毁 Box UI
    }

    // 根据进入前状态决定行为
    if (_wasBackpackOpenBeforeBox)
    {
        ShowMainAndTop();
        SetVisiblePage(currentIndex);
    }
    else
    {
        // 进入前背包是关闭的，关闭 Box 后也保持关闭
        ClosePanel();
    }
    
    _wasBackpackOpenBeforeBox = false;
}
```

#### 3. Tab 键在 BoxOpen 状态下的行为

当前设计：BoxOpen 时按 Tab → 关闭 Box → 打开背包

这意味着 `CloseBoxUI()` 后，如果是 Tab 触发的，应该打开背包而不是返回 NoPanel。

需要区分：
- Tab 触发的关闭 → 打开背包
- ESC 触发的关闭 → 返回 NoPanel
- 点击另一个箱子 → 切换到新箱子

### 打开背包时的互斥检查（锐评后新增）

**核心约束**：任何打开 PackagePanel 的路径都必须先检查并关闭 Box UI。

#### 需要添加互斥检查的入口点

| 入口方法 | 触发方式 | 需要调用 |
|---------|---------|---------|
| `OpenPanel()` | 代码直接调用 | `CloseBoxPanelIfOpen()` |
| `OpenOrToggle(int idx)` | Tab/B/M/L/O 快捷键 | `CloseBoxPanelIfOpen()` |

#### 实现示例

```csharp
public void OpenOrToggle(int idx)
{
    // 🔥 关键：打开背包前先关闭 Box UI
    CloseBoxPanelIfOpen();
    
    // 原有逻辑...
    if (!panelRoot.activeSelf)
    {
        OpenPanel();
        SetVisiblePage(idx);
    }
    else if (currentIndex == idx)
    {
        ClosePanel();
    }
    else
    {
        SetVisiblePage(idx);
    }
}

private void CloseBoxPanelIfOpen()
{
    if (_activeBoxUI != null)
    {
        var boxPanelUI = _activeBoxUI.GetComponent<FarmGame.UI.BoxPanelUI>();
        if (boxPanelUI != null && boxPanelUI.IsOpen)
        {
            boxPanelUI.Close();
        }
        Destroy(_activeBoxUI);
        _activeBoxUI = null;
        
        // 恢复 Main/Top
        ShowMainAndTop();
    }
}
```

#### 验证点

1. 从 BoxOpen 按 Tab → 箱子关闭，背包打开
2. 从 BoxOpen 按 B → 箱子关闭，背包打开到 Recipes 页
3. 从 BoxOpen 按 M → 箱子关闭，背包打开到 Map 页
4. 不允许 Box 和背包 Main/Top 同时可见

---

## 组件交互图

```
┌─────────────────────────────────────────────────────────────┐
│                      PackagePanel                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  6_Top (Tab 按钮)                                    │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Main (背包页面)                                     │   │
│  │  ├── 0_Props                                        │   │
│  │  ├── 1_Recipes                                      │   │
│  │  └── ...                                            │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  BoxUIRoot (箱子 UI 容器)                            │   │
│  │  └── Box_XX (动态实例化)                             │   │
│  │       ├── Up (箱子槽位)                              │   │
│  │       └── Down (背包槽位)                            │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘

打开箱子时：
- 6_Top 隐藏
- Main 隐藏
- BoxUIRoot 显示 Box_XX

关闭箱子时：
- Box_XX 销毁
- 6_Top 恢复
- Main 恢复
```

---

## 数据流

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
    │       └── OnPanelJustOpened()
    │               └── InventoryPanelUI.EnsureBuilt()
    ├── HideMainAndTop()
    └── Instantiate(prefab)
            ↓
        BoxPanelUI.Open(chest)
            ├── SubscribeToChest()
            └── RefreshUI()
                    ├── RefreshChestSlots()  → Up 区域
                    └── RefreshInventorySlots()  → Down 区域
```

---

## 相关文件

| 文件 | 修改内容 |
|------|----------|
| `BoxPanelUI.cs` | 修改 `startIndex` 从 12 改为 0 |
| `PackagePanelTabsUI.cs` | 修复 `CloseBoxUI()` 状态机逻辑 |
| `GameInputManager.cs` | 修改交互检测使用 Sprite Bounds |
| `ChestController.cs` | 实现 Sprite 底部对齐 + Collider 同步 + NavGrid 刷新 |
| `Box_*.prefab` | 用户配置 Up 区域的 InventorySlotUI |
