# 设计文档 V2 - 背包交互系统升级（重构版）

## 概述

本设计文档是基于 2026-01-05 回滚后的重新设计。吸取了之前实现的教训，采用更安全、更简单的方案。

### 核心设计原则

1. **保持原有显示逻辑不变** - `InventorySlotUI` 的 Icon/Amount 创建和刷新逻辑完全不动
2. **使用组合而非继承** - 拖拽功能作为独立组件添加，不修改原有类结构
3. **参考 ToolbarSlotUI** - 保持代码风格一致
4. **增量开发** - 每次只添加一个小功能，验证后再继续
5. **最小侵入** - 尽量减少对现有代码的修改

### 与 V1 设计的关键区别

| 方面 | V1 设计（已失败） | V2 设计（本次） |
|------|------------------|----------------|
| 槽位 UI 修改 | 大规模重构，添加多个接口 | **不修改**原有显示逻辑 |
| 拖拽管理 | 嵌入到 SlotUI 中 | 独立组件，通过事件通信 |
| Icon/Amount | 尝试查找 Target/Selected | **保持原有**自动创建逻辑 |
| 复杂度 | 高（状态机+多接口） | 低（简单事件驱动） |

## 架构

### 组件分离设计

```
┌─────────────────────────────────────────────────────────────────┐
│                        PackagePanel                              │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │            InventoryInteractionManager                   │    │
│  │                  (独立的交互管理器)                       │    │
│  │                                                          │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │    │
│  │  │ DragModule  │  │ ShiftModule │  │  CtrlModule     │  │    │
│  │  │ (拖拽模块)  │  │ (二分模块)  │  │  (单取模块)     │  │    │
│  │  └─────────────┘  └─────────────┘  └─────────────────┘  │    │
│  │                         │                                │    │
│  │                  HeldItemDisplay                         │    │
│  │                (跟随鼠标的物品显示)                       │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                         事件通信                                 │
│                              │                                   │
│  ┌───────────────────────────┼───────────────────────────────┐  │
│  │                           ▼                               │  │
│  │  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐   │  │
│  │  │InventorySlot│    │InventorySlot│    │EquipmentSlot│   │  │
│  │  │    UI       │    │    UI       │    │    UI       │   │  │
│  │  │ (不修改！)  │    │ (不修改！)  │    │ (不修改！)  │   │  │
│  │  └─────────────┘    └─────────────┘    └─────────────┘   │  │
│  │         Up 区域 (36格)              │    Down 区域 (6格)  │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 关键设计：SlotUI 不修改

**InventorySlotUI 和 EquipmentSlotUI 的以下部分完全不动：**
- `Awake()` 中的 Icon/Amount 查找和创建逻辑
- `Refresh()` 中的显示刷新逻辑
- `Bind()` 方法

**只添加：**
- 一个简单的 `GetIndex()` 方法供外部查询
- 一个 `IsEquipmentSlot` 属性供外部判断

## 组件与接口

### 1. InventoryInteractionManager（独立交互管理器）

这是一个**完全独立**的组件，挂载在 PackagePanel 上，不修改任何现有 SlotUI。

```csharp
/// <summary>
/// 背包交互管理器 - 独立组件，不修改现有 SlotUI
/// 通过 Unity 事件系统检测槽位上的输入
/// </summary>
public class InventoryInteractionManager : MonoBehaviour
{
    // === 状态 ===
    public enum State { None, Dragging, HeldByShift, HeldByCtrl }
    public State CurrentState { get; private set; }
    
    // === 拿起的物品信息 ===
    private ItemStack heldItem;
    private int sourceIndex;
    private bool sourceIsEquipment;
    
    // === 配置 ===
    [Header("配置")]
    [SerializeField] private float dragThreshold = 0.15f;
    [SerializeField] private float ctrlPickupRate = 3.5f;
    
    [Header("引用")]
    [SerializeField] private InventoryService inventory;
    [SerializeField] private EquipmentService equipment;
    [SerializeField] private ItemDatabase database;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private RectTransform trashCanRect;
    [SerializeField] private HeldItemDisplay heldDisplay;
    
    // === 核心方法 ===
    
    /// <summary>
    /// 检测鼠标下方的槽位（通过 Raycast）
    /// </summary>
    private (int index, bool isEquipment)? GetSlotUnderMouse();
    
    /// <summary>
    /// 开始拖拽
    /// </summary>
    public void StartDrag(int index, bool isEquipment);
    
    /// <summary>
    /// 结束拖拽（放置或丢弃）
    /// </summary>
    public void EndDrag();
    
    /// <summary>
    /// Shift 拿起（二分）
    /// </summary>
    public void ShiftPickup(int index, bool isEquipment);
    
    /// <summary>
    /// Ctrl 拿起（单个）
    /// </summary>
    public void CtrlPickup(int index, bool isEquipment);
    
    /// <summary>
    /// 放置物品
    /// </summary>
    public void PlaceItem(int targetIndex, bool isEquipment);
    
    /// <summary>
    /// 返回原位
    /// </summary>
    public void ReturnToSource();
    
    /// <summary>
    /// 丢弃物品
    /// </summary>
    public void DropItem();
    
    /// <summary>
    /// 取消当前操作
    /// </summary>
    public void Cancel();
}
```

### 2. HeldItemDisplay（跟随鼠标显示）

与 V1 设计相同，但更简单：

```csharp
/// <summary>
/// 跟随鼠标显示被拿起的物品
/// </summary>
public class HeldItemDisplay : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text amountText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        
        // ★ 确保初始隐藏
        if (canvasGroup != null) canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }
    
    public void Show(Sprite icon, int amount)
    {
        gameObject.SetActive(true);
        
        // ★ 显式启用 Image
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        
        if (amountText != null)
        {
            amountText.text = amount > 1 ? amount.ToString() : "";
        }
        
        if (canvasGroup != null) canvasGroup.alpha = 1;
    }
    
    public void UpdateAmount(int amount)
    {
        if (amountText != null)
        {
            amountText.text = amount > 1 ? amount.ToString() : "";
        }
    }
    
    public void Hide()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }
    
    void Update()
    {
        // 跟随鼠标
        if (parentCanvas != null && rectTransform != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                Input.mousePosition,
                parentCanvas.worldCamera,
                out Vector2 localPoint
            );
            rectTransform.localPosition = localPoint;
        }
    }
}
```

### 3. InventorySortService（整理服务）

保持 V1 设计，这部分没有问题：

```csharp
/// <summary>
/// 背包整理服务
/// </summary>
public class InventorySortService : MonoBehaviour
{
    [SerializeField] private InventoryService inventory;
    [SerializeField] private ItemDatabase database;
    
    public void SortInventory()
    {
        // 1. 合并相同物品
        MergeStacks();
        
        // 2. 按优先级排序
        SortByPriority();
    }
    
    private void MergeStacks() { /* ... */ }
    private void SortByPriority() { /* ... */ }
    
    private int GetPriority(ItemData item)
    {
        // 工具 > 武器 > 可放置 > 种子 > 消耗品 > 材料 > 其他
        if (item is ToolData) return 0;
        if (item is WeaponData) return 1;
        if (item.isPlaceable) return 2;
        if (item is SeedData) return 3;
        if (item.category == ItemCategory.Consumable) return 4;
        if (item.category == ItemCategory.Material) return 5;
        return 6;
    }
}
```

## 数据模型

### 物品状态（简化版）

```
     ┌─────────────┐
     │    None     │ ←── 初始状态
     └──────┬──────┘
            │
    ┌───────┼───────┐
    │       │       │
    ▼       ▼       ▼
┌───────┐ ┌───────┐ ┌───────┐
│Dragging│ │HeldBy │ │HeldBy │
│       │ │Shift  │ │Ctrl   │
└───┬───┘ └───┬───┘ └───┬───┘
    │         │         │
    └────┬────┴────┬────┘
         │         │
         ▼         ▼
    ┌─────────┐ ┌─────────┐
    │放置成功 │ │放置失败 │
    │(交换)   │ │(返回)   │
    └────┬────┘ └────┬────┘
         │           │
         └─────┬─────┘
               ▼
         ┌─────────┐
         │  None   │
         └─────────┘
```

### 装备槽位映射（不变）

| 槽位索引 | 装备类型 |
|---------|---------|
| 0 | Helmet（头盔） |
| 1 | Pants（裤子） |
| 2 | Armor（盔甲） |
| 3 | Shoes（鞋子） |
| 4 | Ring（戒指1，优先） |
| 5 | Ring（戒指2，备选） |

## 输入检测方案

### 方案：在 Manager 中统一处理输入

不修改 SlotUI，而是在 `InventoryInteractionManager.Update()` 中检测输入：

```csharp
void Update()
{
    // 只在面板激活时处理
    if (!gameObject.activeInHierarchy) return;
    
    // 检测鼠标下方的槽位
    var slotInfo = GetSlotUnderMouse();
    
    // 根据当前状态和输入处理
    switch (CurrentState)
    {
        case State.None:
            HandleNoneState(slotInfo);
            break;
        case State.Dragging:
            HandleDraggingState(slotInfo);
            break;
        case State.HeldByShift:
        case State.HeldByCtrl:
            HandleHeldState(slotInfo);
            break;
    }
}

private void HandleNoneState((int index, bool isEquipment)? slotInfo)
{
    if (slotInfo == null) return;
    
    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    
    // Shift+Ctrl 同时按下：不触发
    if (shift && ctrl) return;
    
    if (Input.GetMouseButtonDown(0))
    {
        if (shift)
        {
            ShiftPickup(slotInfo.Value.index, slotInfo.Value.isEquipment);
        }
        else if (ctrl)
        {
            CtrlPickup(slotInfo.Value.index, slotInfo.Value.isEquipment);
        }
        else
        {
            // 开始拖拽检测
            StartDragDetection(slotInfo.Value.index, slotInfo.Value.isEquipment);
        }
    }
}
```

### 槽位检测方法

```csharp
private (int index, bool isEquipment)? GetSlotUnderMouse()
{
    // 使用 EventSystem 的 Raycast
    var pointerData = new PointerEventData(EventSystem.current)
    {
        position = Input.mousePosition
    };
    
    var results = new List<RaycastResult>();
    EventSystem.current.RaycastAll(pointerData, results);
    
    foreach (var result in results)
    {
        // 检查是否是 InventorySlotUI
        var invSlot = result.gameObject.GetComponent<InventorySlotUI>();
        if (invSlot != null)
        {
            return (invSlot.Index, false);
        }
        
        // 检查是否是 EquipmentSlotUI
        var equipSlot = result.gameObject.GetComponent<EquipmentSlotUI>();
        if (equipSlot != null)
        {
            return (equipSlot.Index, true);
        }
    }
    
    return null;
}
```

## 对现有代码的最小修改

### InventorySlotUI - 只添加 Index 属性

```csharp
// 在 InventorySlotUI.cs 中只添加这一行：
public int Index => index;
```

### EquipmentSlotUI - 只添加 Index 属性

```csharp
// 在 EquipmentSlotUI.cs 中只添加这一行：
public int Index => index;
```

**就这些！不修改任何其他代码！**

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. 
Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 物品总量守恒
*For any* 拖拽交换操作，系统中物品总量应保持不变
**Validates: Requirements 1.2, 1.3**

### Property 2: Shift 二分数量正确
*For any* 数量为 N 的物品，Shift 拿取后手上数量为 floor(N/2)，原槽位为 N - floor(N/2)
**Validates: Requirements 2.1, 2.2**

### Property 3: Ctrl 单取数量正确
*For any* 数量为 N (N > 0) 的物品，Ctrl 拿取后手上增加 1，原槽位减少 1
**Validates: Requirements 3.1**

### Property 4: 状态互斥
*For any* 时刻，系统只能处于 None/Dragging/HeldByShift/HeldByCtrl 之一
**Validates: Requirements 4.1, 4.2, 4.3**

### Property 5: 放置失败回退
*For any* 放置失败的操作，物品应返回原槽位并恢复数量
**Validates: Requirements 2.6, 3.7**

### Property 6: 丢弃冷却
*For any* 被丢弃的物品，5 秒内或玩家未离开范围时不可拾取
**Validates: Requirements 5.3, 5.4**

### Property 7: 排序优先级
*For any* 排序后的背包，工具 > 可放置 > 消耗品 > 其他
**Validates: Requirements 7.2**

### Property 8: 装备类型限制
*For any* 装备槽位，只能放置对应类型的装备
**Validates: Requirements 9.1, 9.2**

## 错误处理

| 情况 | 处理方式 |
|------|---------|
| 拖拽到无效区域 | 返回原槽位 |
| 放置到有物品槽位（拿起状态） | 返回原槽位 |
| 装备类型不匹配 | 返回原槽位 |
| 丢弃失败 | 返回原槽位，记录日志 |

## 测试策略

### 手动测试清单

每完成一个功能后，必须手动测试：

1. **基础显示测试**
   - [ ] 打开背包，所有物品正常显示
   - [ ] 关闭背包，再打开，物品仍正常显示
   - [ ] Toolbar 物品正常显示

2. **拖拽测试**
   - [ ] 长按槽位，物品跟随鼠标
   - [ ] 拖拽到另一槽位，物品交换
   - [ ] 拖拽到空槽位，物品移动
   - [ ] 拖拽到面板外，物品丢弃

3. **Shift 测试**
   - [ ] Shift+点击，拿起一半
   - [ ] 再次 Shift+点击同一槽位，继续二分
   - [ ] 点击空槽位，放置
   - [ ] 点击有物品槽位，返回原位

4. **Ctrl 测试**
   - [ ] Ctrl+点击，拿起 1 个
   - [ ] Ctrl+长按，持续拿取
   - [ ] 松开 Ctrl，停止拿取

### 属性测试

使用 NUnit 进行属性测试，验证核心逻辑的正确性。

## 实现顺序

**严格按以下顺序实现，每步验证后再继续：**

1. **Phase 1: 基础设施**
   - 创建 HeldItemDisplay（独立组件）
   - 创建 InventoryInteractionManager 骨架
   - 验证：编译通过，不影响现有功能

2. **Phase 2: 拖拽功能**
   - 实现拖拽检测
   - 实现拖拽交换
   - 验证：拖拽正常工作，原有显示不受影响

3. **Phase 3: Shift 功能**
   - 实现 Shift 二分拿取
   - 实现放置逻辑
   - 验证：Shift 功能正常

4. **Phase 4: Ctrl 功能**
   - 实现 Ctrl 单取
   - 实现长按持续拿取
   - 验证：Ctrl 功能正常

5. **Phase 5: 丢弃和整理**
   - 实现丢弃机制
   - 实现整理功能
   - 验证：丢弃和整理正常

6. **Phase 6: 快速装备**
   - 实现 Ctrl+点击快速装备
   - 验证：快速装备正常

## 关键教训（来自 V1 失败）

### 🔴 绝对禁止

1. **不要修改 SlotUI 的 Awake/Refresh 逻辑**
2. **不要把 Target/Selected 当成 Icon**
3. **不要用 FindFirstObjectByType 查找 ScriptableObject**
4. **不要一次性重构整个文件**

### ✅ 必须遵守

1. **每次只添加一小部分代码**
2. **每次修改后立即验证编译和功能**
3. **参考 ToolbarSlotUI 的实现方式**
4. **保持代码简单，不要过度设计**
