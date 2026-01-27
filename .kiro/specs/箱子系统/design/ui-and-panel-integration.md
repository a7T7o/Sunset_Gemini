# 箱子系统 - UI 与面板集成

## 文档说明
对应需求 8-10：UI 布局、与 PackagePanel 的互斥

---

## ✅ 当前状态：已实现（2026-01-13 重构）

### 🔴 核心原则：预制体绑定，不动态生成

用户已有 4 种 UI 预制体（Box_12, Box_24, Box_36, Box_48），格子已预置好。
**BoxPanelUI 只做数据绑定，绝对不生成/销毁任何槽位！**

---

## UI 预制体结构（必须严格遵守）

```
Box_12 (UI Prefab Root)
├── Background      (Image)
├── BT_Sort_Up      (Button - 整理箱子)
├── BT_Sort_Down    (Button - 整理背包)
├── BT_TrashCan     (Button - 垃圾桶)
├── Up              (GridLayoutGroup + ToggleGroup)
│    ├── Up_00      (InventorySlotUI + Toggle + Image)
│    ├── Up_01
│    └── ... (已预置好 12 个)
└── Down            (GridLayoutGroup + ToggleGroup)
     ├── Down_00    (InventorySlotUI + Toggle + Image)
     ├── Down_01
     └── ... (已预置好 36 个)
```

### 预制体对应关系

| StorageData | 容量 | UI 预制体 |
|-------------|------|----------|
| Box_1 (小木箱) | 12 | Box_12 |
| Box_2 (大木箱) | 24 | Box_24 |
| Box_3 (小铁箱) | 36 | Box_36 |
| Box_4 (大铁箱) | 48 | Box_48 |

---

## 架构设计

### StorageData（数据层）

```csharp
public class StorageData : PlaceableItemData
{
    // 存储属性
    public int storageCapacity;
    public int storageRows;
    public int storageCols;
    
    // 预制体
    public GameObject storagePrefab;    // 世界物体预制体
    public GameObject boxUiPrefab;      // 🔥 UI 预制体（Box_12/24/36/48）
}
```

### ChestController（交互层）

```csharp
public void OnInteract(InteractionContext context)
{
    // ... 锁/钥匙处理 ...
    
    // 打开箱子 UI - 实例化对应的 UI Prefab
    OpenBoxUI();
}

private void OpenBoxUI()
{
    // 1. 检查是否已有打开的 BoxPanelUI
    if (BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen)
    {
        if (BoxPanelUI.ActiveInstance.CurrentChest == this) return;
        BoxPanelUI.ActiveInstance.Close();
    }
    
    // 2. 实例化 storageData.boxUiPrefab
    var canvas = FindFirstObjectByType<Canvas>();
    var uiInstance = Instantiate(storageData.boxUiPrefab, canvas.transform);
    var boxPanelUI = uiInstance.GetComponent<BoxPanelUI>();
    
    // 3. 打开 UI
    boxPanelUI.Open(this);
}
```

### BoxPanelUI（表现层）

```csharp
public class BoxPanelUI : MonoBehaviour
{
    // 🔴 只收集，不生成
    private void CollectSlots()
    {
        _chestSlots = new List<InventorySlotUI>(upGridParent.GetComponentsInChildren<InventorySlotUI>(true));
        _inventorySlots = new List<InventorySlotUI>(downGridParent.GetComponentsInChildren<InventorySlotUI>(true));
    }
    
    // 🔴 只绑定数据，不修改槽位数量
    private void RefreshChestSlots()
    {
        for (int i = 0; i < _chestSlots.Count; i++)
        {
            if (i < capacity)
            {
                slot.gameObject.SetActive(true);
                BindChestSlotData(slot, i);
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }
    }
    
    // 静态属性：当前活跃实例（用于互斥管理）
    public static BoxPanelUI ActiveInstance { get; private set; }
}
```

---

## 面板互斥逻辑

```
ChestController.OpenBoxUI()
├── 检查 BoxPanelUI.ActiveInstance
│   └── 如果已打开其他箱子 → 关闭它
├── 实例化 storageData.boxUiPrefab
└── boxPanelUI.Open(this)
    └── ClosePackagePanel()

PackagePanelTabsUI.OpenPanel()
└── CloseBoxPanelIfOpen()
    └── BoxPanelUI.ActiveInstance?.Close()

GameInputManager.HandlePanelHotkeys()
├── ESC → 优先关闭 BoxPanelUI.ActiveInstance
└── Tab/B/M → CloseBoxPanelIfOpen() + OpenPackagePanel
```

---

## 需求验收状态

### 需求 8：右键点击 → 自动导航 → 打开 UI

| 验收标准 | 状态 | 说明 |
|---------|------|------|
| 距离大于交互距离 → 自动导航 | ✅ | GameInputManager.HandleInteractable() |
| 到达后自动打开 UI | ✅ | 回调机制 |
| 已在距离内 → 直接打开 | ✅ | `OnInteract()` 调用 `OpenBoxUI()` |

### 需求 9：Box 与 PackagePanel 互斥

| 验收标准 | 状态 | 说明 |
|---------|------|------|
| Box 打开时隐藏 PackagePanel | ✅ | BoxPanelUI.ClosePackagePanel() |
| PackagePanel 打开时隐藏 Box | ✅ | PackagePanelTabsUI.CloseBoxPanelIfOpen() |
| ESC 键关闭 Box | ✅ | GameInputManager.HandlePanelHotkeys() |
| 背包键关闭 Box 并打开 PackagePanel | ✅ | GameInputManager.HandlePanelHotkeys() |

### 需求 10：UI 布局

| 验收标准 | 状态 | 说明 |
|---------|------|------|
| 上方区域显示箱子格子 | ✅ | BoxPanelUI.RefreshChestSlots() |
| 下方区域显示背包格子 | ✅ | BoxPanelUI.RefreshInventorySlots() |
| 不同容量使用不同预制体 | ✅ | StorageData.boxUiPrefab |
| 关闭时保存变更 | ✅ | 通过 ChestInventory 事件自动同步 |

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Service/Inventory/ChestInventory.cs` | 箱子库存类 |
| `Assets/YYY_Scripts/World/Placeable/ChestController.cs` | 箱子控制器（实例化 UI） |
| `Assets/YYY_Scripts/Data/Items/StorageData.cs` | 存储数据（新增 boxUiPrefab） |
| `Assets/YYY_Scripts/UI/Box/BoxPanelUI.cs` | 箱子 UI 面板（只绑定不生成） |
| `Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs` | 面板切换管理 |
| `Assets/YYY_Scripts/Controller/Input/GameInputManager.cs` | 输入管理 |

---

## Unity 编辑器配置

### 1. 配置 StorageData

为每个箱子类型的 StorageData 配置对应的 `boxUiPrefab`：

| StorageData | boxUiPrefab |
|-------------|-------------|
| Storage_Box_1 | Box_12 |
| Storage_Box_2 | Box_24 |
| Storage_Box_3 | Box_36 |
| Storage_Box_4 | Box_48 |

### 2. 确保 UI 预制体结构正确

每个 Box_XX 预制体必须：
- 根物体挂载 `BoxPanelUI` 组件
- 有 `Up` 子物体（箱子格子容器）
- 有 `Down` 子物体（背包格子容器）
- Up/Down 下的格子挂载 `InventorySlotUI` 组件
