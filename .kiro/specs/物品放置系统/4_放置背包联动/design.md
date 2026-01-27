# 设计文档 - 放置系统与背包联动

## 概述

本设计文档定义放置系统与背包系统联动的架构和实现细节，确保放置成功后正确扣除背包物品，并在放置过程中正确处理各种中断情况。

## 架构

### 组件交互图

```
┌─────────────────────────────────────────────────────────────────────┐
│                        PlacementManagerV3                            │
│                                                                      │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐  │
│  │ PlacementSnapshot│    │ InterruptChecker │    │   StateManager  │  │
│  │  - itemId        │    │  - CheckAll()    │    │  - Idle         │  │
│  │  - quality       │    │  - 手持切换检测   │    │  - Preview      │  │
│  │  - slotIndex     │    │  - 背包打开检测   │    │  - Locked       │  │
│  │  - lockedPos     │    │  - 受击检测      │    │  - Navigating   │  │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘  │
│                                                                      │
│                              │                                       │
│                              ▼                                       │
│                    ┌─────────────────┐                              │
│                    │ ExecutePlacement │                              │
│                    │  1. 验证快照有效  │                              │
│                    │  2. 实例化物品    │                              │
│                    │  3. 扣除背包物品  │                              │
│                    │  4. 检查剩余数量  │                              │
│                    └─────────────────┘                              │
└─────────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              │               │               │
              ▼               ▼               ▼
    ┌─────────────────┐ ┌─────────────┐ ┌─────────────────┐
    │ InventoryService │ │HotbarSelect │ │PackagePanelTabs │
    │  - RemoveFromSlot│ │  Service    │ │      UI         │
    │  - GetSlot       │ │- OnSelected │ │ - IsPanelOpen   │
    └─────────────────┘ │  Changed    │ └─────────────────┘
                        └─────────────┘
```

### 数据流

```
左键点击（全绿）
    │
    ▼
创建 PlacementSnapshot
    │
    ├─ itemId = currentPlacementItem.itemID
    ├─ quality = currentItemQuality
    ├─ slotIndex = HotbarSelectionService.selectedIndex
    └─ lockedPosition = previewPosition
    │
    ▼
锁定预览位置 → 开始导航
    │
    ├─ 每帧检测中断条件 ─────────────────┐
    │   - 手持物品切换？                  │
    │   - 背包打开？                      │
    │   - 受击？                          │
    │   - 新导航/交互？                   │
    │                                     │
    │   如果任一条件满足 ─────────────────┤
    │                                     │
    │                                     ▼
    │                              取消导航
    │                              清除快照
    │                              返回 Preview/Idle
    │
    ▼
到达目标位置
    │
    ▼
验证快照有效性
    │
    ├─ 槽位物品是否匹配？
    │   - itemId 相同？
    │   - quality 相同？
    │   - amount > 0？
    │
    ├─ 如果不匹配 ─────────────────────────┐
    │                                       │
    │                                       ▼
    │                                取消放置
    │                                返回 Preview
    │
    ▼
执行放置
    │
    ├─ 1. 实例化预制体
    ├─ 2. 同步 Layer 和 Order
    ├─ 3. 调用 InventoryService.RemoveFromSlot(slotIndex, 1)
    ├─ 4. 播放音效和特效
    └─ 5. 广播事件
    │
    ▼
检查剩余数量
    │
    ├─ amount > 0 → 返回 Preview 状态
    └─ amount = 0 → 退出放置模式
```

## 组件与接口

### PlacementSnapshot 结构体

```csharp
/// <summary>
/// 放置快照 - 记录点击锁定时的物品信息
/// </summary>
public struct PlacementSnapshot
{
    public int itemId;           // 物品 ID
    public int quality;          // 物品品质
    public int slotIndex;        // 背包槽位索引
    public Vector3 lockedPosition; // 锁定的放置位置
    public bool isValid;         // 快照是否有效
    
    public static PlacementSnapshot Invalid => new PlacementSnapshot { isValid = false };
}
```

### PlacementManagerV3 新增字段和方法

```csharp
// === 新增字段 ===
private PlacementSnapshot currentSnapshot;
private InventoryService inventoryService;
private HotbarSelectionService hotbarSelection;
private PackagePanelTabsUI packageTabs;

// === 新增方法 ===

/// <summary>
/// 创建放置快照
/// </summary>
private PlacementSnapshot CreateSnapshot()
{
    return new PlacementSnapshot
    {
        itemId = currentPlacementItem.itemID,
        quality = currentItemQuality,
        slotIndex = hotbarSelection.selectedIndex,
        lockedPosition = placementPreview.LockedPosition,
        isValid = true
    };
}

/// <summary>
/// 验证快照是否仍然有效
/// </summary>
private bool ValidateSnapshot()
{
    if (!currentSnapshot.isValid) return false;
    
    var slot = inventoryService.GetSlot(currentSnapshot.slotIndex);
    if (slot.IsEmpty) return false;
    if (slot.itemId != currentSnapshot.itemId) return false;
    if (slot.quality != currentSnapshot.quality) return false;
    
    return true;
}

/// <summary>
/// 检测中断条件
/// </summary>
private bool CheckInterruptConditions()
{
    // 1. 背包打开
    if (packageTabs != null && packageTabs.IsPanelOpen())
        return true;
    
    // 2. 手持物品已切换（通过事件处理，这里不需要检测）
    
    // 3. 快照失效（物品被移动/使用）
    if (!ValidateSnapshot())
        return true;
    
    return false;
}

/// <summary>
/// 处理中断
/// </summary>
private void HandleInterrupt()
{
    // 取消导航
    if (navigator != null && navigator.IsNavigating)
        navigator.CancelNavigation();
    
    // 清除快照
    currentSnapshot = PlacementSnapshot.Invalid;
    
    // 解锁预览
    if (placementPreview != null)
        placementPreview.UnlockPosition();
    
    // 返回 Preview 状态（如果还在放置模式）
    if (currentState != PlacementState.Idle)
        ChangeState(PlacementState.Preview);
}

/// <summary>
/// 从背包扣除物品
/// </summary>
private bool DeductItemFromInventory()
{
    if (inventoryService == null || !currentSnapshot.isValid)
        return false;
    
    return inventoryService.RemoveFromSlot(currentSnapshot.slotIndex, 1);
}

/// <summary>
/// 检查是否还有剩余物品
/// </summary>
private bool HasMoreItems()
{
    if (inventoryService == null || !currentSnapshot.isValid)
        return false;
    
    var slot = inventoryService.GetSlot(currentSnapshot.slotIndex);
    return !slot.IsEmpty && slot.itemId == currentSnapshot.itemId;
}
```

### 事件订阅

```csharp
void Start()
{
    // ... 现有代码 ...
    
    // 订阅手持物品切换事件
    if (hotbarSelection != null)
        hotbarSelection.OnSelectedChanged += OnHotbarSelectionChanged;
}

void OnDestroy()
{
    // ... 现有代码 ...
    
    // 取消订阅
    if (hotbarSelection != null)
        hotbarSelection.OnSelectedChanged -= OnHotbarSelectionChanged;
}

/// <summary>
/// 手持物品切换回调
/// </summary>
private void OnHotbarSelectionChanged(int newIndex)
{
    // 如果正在放置过程中（Locked 或 Navigating），中断
    if (currentState == PlacementState.Locked || currentState == PlacementState.Navigating)
    {
        HandleInterrupt();
        // 退出放置模式（因为物品已切换）
        ExitPlacementMode();
    }
}
```

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. 
Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 物品数量守恒
*For any* 放置操作，背包物品减少的数量应等于成功放置的物品数量
**Validates: Requirements 1.1, 1.2**

### Property 2: 快照数据一致性
*For any* 放置执行时刻，使用的物品数据应与快照记录的数据一致
**Validates: Requirements 2.1, 2.2**

### Property 3: 中断后状态正确
*For any* 中断条件触发后，系统应处于 Preview 或 Idle 状态，且快照应被清除
**Validates: Requirements 3.1, 4.1, 6.1, 7.1**

### Property 4: 连续放置正确性
*For any* 放置成功后，如果剩余数量 > 0，系统应处于 Preview 状态
**Validates: Requirements 8.1, 8.2**

## 错误处理

| 情况 | 处理方式 |
|------|---------|
| 快照验证失败 | 取消放置，返回 Preview 状态，记录警告日志 |
| 背包扣除失败 | 销毁已实例化的物品，返回 Preview 状态，记录错误日志 |
| 导航超时 | 取消导航，清除快照，返回 Preview 状态 |
| 服务引用为空 | 记录错误日志，禁用相关功能 |

## 测试策略

### 手动测试清单

1. **基础放置测试**
   - [ ] 放置成功后背包物品数量 -1
   - [ ] 放置失败后背包物品数量不变
   - [ ] 物品用完后自动退出放置模式

2. **中断测试**
   - [ ] 放置过程中切换手持物品 → 取消放置
   - [ ] 放置过程中打开背包 → 取消放置
   - [ ] 放置过程中再次左键点击 → 重新开始

3. **连续放置测试**
   - [ ] 放置成功后继续 Preview 状态
   - [ ] 可以连续放置多个物品
   - [ ] 最后一个物品放置后退出放置模式

4. **边界情况测试**
   - [ ] 放置过程中物品被其他方式消耗 → 取消放置
   - [ ] 放置过程中物品被移动到其他槽位 → 取消放置

