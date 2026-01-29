# 2_完善工作 - 设计文档

## 概述

本文档详细描述修复 Phase 2 数据核心重构中发现的 Bug 的技术方案。

---

## 一、箱子存档修复 (P0-1)

### 1.1 问题根因分析

**现象**: 箱子放入物品 → 保存 → 重启加载 → 物品消失

**根因**:
1. `ChestInventoryV2.ToSaveData()` 可能没有正确调用 `InventoryItem.PrepareForSerialization()`
2. `JsonUtility` 无法序列化 `Dictionary`，必须先转换为 `List<PropertyEntry>`
3. `ChestController.Save()` 可能没有正确收集库存数据

### 1.2 修复方案

#### 1.2.1 修改 ChestInventoryV2.ToSaveData()

```csharp
public List<InventorySlotSaveData> ToSaveData()
{
    var result = new List<InventorySlotSaveData>();
    
    for (int i = 0; i < _capacity; i++)
    {
        var item = _items[i];
        
        // 空槽位也要保存（保持索引一致性）
        if (item == null || item.IsEmpty)
        {
            result.Add(new InventorySlotSaveData { slotIndex = i });
            continue;
        }
        
        // 🔥 关键：必须调用 PrepareForSerialization
        item.PrepareForSerialization();
        
        var slotData = new InventorySlotSaveData
        {
            slotIndex = i,
            itemId = item.ItemId,
            quality = item.Quality,
            amount = item.Amount,
            instanceId = item.InstanceId,
            currentDurability = item.CurrentDurability,
            maxDurability = item.MaxDurability
        };
        
        result.Add(slotData);
    }
    
    return result;
}
```

#### 1.2.2 修改 ChestController.Save()

确保 `Save()` 方法正确调用 `_inventoryV2.ToSaveData()`：

```csharp
public WorldObjectSaveData Save()
{
    // ... 基础数据 ...
    
    var chestData = new ChestSaveData
    {
        capacity = storageData?.storageCapacity ?? 20,
        isLocked = isLocked,
        customName = storageData?.itemName
    };
    
    // 🔥 关键：优先使用 V2 库存
    if (_inventoryV2 != null)
    {
        chestData.slots = _inventoryV2.ToSaveData();
        
        if (showDebugInfo)
            Debug.Log($"[ChestController] Save: 保存 {chestData.slots.Count} 个槽位");
    }
    
    data.genericData = JsonUtility.ToJson(chestData);
    return data;
}
```

#### 1.2.3 修改 ChestController.Load()

确保 `Load()` 方法正确恢复数据：

```csharp
public void Load(WorldObjectSaveData data)
{
    // ... 基础数据恢复 ...
    
    if (!string.IsNullOrEmpty(data.genericData))
    {
        var chestData = JsonUtility.FromJson<ChestSaveData>(data.genericData);
        if (chestData != null)
        {
            isLocked = chestData.isLocked;
            
            // 🔥 关键：恢复 V2 库存
            if (_inventoryV2 != null && chestData.slots != null)
            {
                _inventoryV2.LoadFromSaveData(chestData.slots);
                
                if (showDebugInfo)
                    Debug.Log($"[ChestController] Load: 恢复 {chestData.slots.Count} 个槽位");
            }
        }
    }
    
    UpdateSprite();
}
```

---

## 二、BoxUI Up 区域交互修复 (P0-2)

### 2.1 问题根因分析

**现象**: BoxUI 的 Up 区域（箱子槽位）无法交互，Down 区域可以

**根因**:
1. `BoxPanelUI.BindChestSlotData()` 没有正确绑定 `Container`
2. `InventorySlotUI.BindContainer()` 可能没有被正确调用
3. Up 区域槽位的 `Container` 为 `null`，导致交互逻辑失效

### 2.2 修复方案

#### 2.2.1 修改 BoxPanelUI.BindChestSlotData()

```csharp
private void BindChestSlotData(InventorySlotUI slot, int index)
{
    if (_currentChest == null)
    {
        Debug.LogError("[BoxPanelUI] BindChestSlotData: _currentChest 为 null");
        return;
    }
    
    // 🔥 关键：使用 ChestInventory 作为 Container
    // ChestInventory 实现了 IItemContainer 接口
    var chestInventory = _currentChest.Inventory;
    if (chestInventory == null)
    {
        Debug.LogError("[BoxPanelUI] BindChestSlotData: ChestInventory 为 null");
        return;
    }
    
    // 🔥 使用 BindContainer 方法绑定
    slot.BindContainer(chestInventory, index);
    
    // 🔥 绑定后立即刷新
    slot.Refresh();
}
```

#### 2.2.2 确保 ChestInventory 实现 IItemContainer

检查 `ChestInventory` 是否正确实现了 `IItemContainer` 接口：

```csharp
public interface IItemContainer
{
    int Capacity { get; }
    ItemDatabase Database { get; }
    ItemStack GetSlot(int index);
    bool SetSlot(int index, ItemStack stack);
    event Action<int> OnSlotChanged;
}
```

---

## 三、Ctrl/Shift + 左键吞物品修复 (P0-3)

### 3.1 问题根因分析

**现象**: Ctrl + 左键拿起物品，再放回同一格子，物品数量变成 1

**根因**:
1. `ChestInventoryV2.SetItem()` 或 `AddItem()` 的数学逻辑有错误
2. 拆分物品时，"剩余部分"没有正确写回原槽位
3. 或者在合并时，数量计算错误

### 3.2 修复方案

#### 3.2.1 审查 ChestInventoryV2 的拆分逻辑

问题可能出在 `InventorySlotInteraction` 或 `InventoryInteractionManager` 中。

**正确的拆分流程**:
1. 原槽位有 10 个物品
2. Ctrl + 左键拿起 5 个（一半）
3. 原槽位应该剩余 5 个
4. 手上持有 5 个
5. 放回原槽位时，应该合并为 10 个

**检查点**:
- `InventoryItem.Split()` 方法是否正确
- `ChestInventoryV2.SetItem()` 是否覆盖了原有数据
- 合并逻辑是否正确计算数量

#### 3.2.2 修改 ChestInventoryV2.SetItem()

确保 `SetItem` 不会意外覆盖数据：

```csharp
public bool SetItem(int index, InventoryItem item)
{
    if (!InRange(index)) return false;
    
    // 🔥 如果是合并操作（同一物品），应该增加数量而不是覆盖
    var existing = _items[index];
    if (existing != null && !existing.IsEmpty && item != null && !item.IsEmpty)
    {
        if (existing.CanStackWith(item))
        {
            // 合并数量
            existing.AddAmount(item.Amount);
            RaiseSlotChanged(index);
            return true;
        }
    }
    
    // 直接设置
    _items[index] = item;
    RaiseSlotChanged(index);
    return true;
}
```

#### 3.2.3 检查 InventorySlotInteraction 的拆分逻辑

确保拆分时正确处理原槽位：

```csharp
// 伪代码：正确的拆分逻辑
void OnCtrlClick(int slotIndex)
{
    var item = container.GetItem(slotIndex);
    if (item == null || item.IsEmpty) return;
    
    int halfAmount = item.Amount / 2;
    if (halfAmount <= 0) return;
    
    // 1. 创建拆分后的物品（手上持有）
    var splitItem = item.Clone();
    splitItem.SetAmount(halfAmount);
    
    // 2. 减少原槽位数量
    item.AddAmount(-halfAmount);
    
    // 3. 如果原槽位为空，清除
    if (item.IsEmpty)
    {
        container.ClearItem(slotIndex);
    }
    else
    {
        // 🔥 关键：通知槽位变化
        container.RaiseSlotChanged(slotIndex);
    }
    
    // 4. 设置手上持有的物品
    SetHeldItem(splitItem);
}
```

---

## 四、背包 UI 刷新修复 (P1-1)

### 4.1 问题根因分析

**现象**: 从箱子操作完，按 Tab 打开背包，显示过时数据

**根因**:
1. `InventoryPanelUI` 在 `OnEnable` 时没有强制刷新
2. 事件订阅在 UI 关闭时断开，重新打开时没有同步最新数据

### 4.2 修复方案

#### 4.2.1 修改 InventoryPanelUI.OnEnable()

```csharp
void OnEnable()
{
    // 🔥 关键：打开时强制刷新
    RefreshAll();
}
```

#### 4.2.2 修改 BoxPanelUI.Close()

```csharp
public void Close()
{
    // ... 现有逻辑 ...
    
    // 🔥 关键：关闭时触发背包刷新
    var inventoryPanel = FindFirstObjectByType<InventoryPanelUI>();
    if (inventoryPanel != null)
    {
        inventoryPanel.RefreshAll();
    }
    
    // 或者通过事件通知
    _inventoryService?.RaiseInventoryChanged();
}
```

---

## 五、游戏时间恢复 (P1-2)

### 5.1 修复方案

#### 5.1.1 修改 TimeManager.cs

添加 `SetTime()` 接口：

```csharp
/// <summary>
/// 设置游戏时间（用于存档恢复）
/// </summary>
public void SetTime(int day, int season, int year, int hour, int minute)
{
    _currentDay = day;
    _currentSeason = (Season)season;
    _currentYear = year;
    _currentHour = hour;
    _currentMinute = minute;
    
    // 触发时间变化事件
    OnTimeChanged?.Invoke();
    
    if (showDebugInfo)
        Debug.Log($"[TimeManager] SetTime: Day={day}, Season={season}, Year={year}, {hour}:{minute}");
}
```

#### 5.1.2 修改 SaveManager.RestoreGameTimeData()

```csharp
private void RestoreGameTimeData(GameTimeSaveData data)
{
    if (data == null) return;
    
    var timeManager = FindFirstObjectByType<TimeManager>();
    if (timeManager != null)
    {
        timeManager.SetTime(data.day, data.season, data.year, data.hour, data.minute);
    }
}
```

#### 5.1.3 修改 SaveManager.CollectGameTimeData()

```csharp
private GameTimeSaveData CollectGameTimeData()
{
    var data = new GameTimeSaveData();
    
    var timeManager = FindFirstObjectByType<TimeManager>();
    if (timeManager != null)
    {
        data.day = timeManager.CurrentDay;
        data.season = (int)timeManager.CurrentSeason;
        data.year = timeManager.CurrentYear;
        data.hour = timeManager.CurrentHour;
        data.minute = timeManager.CurrentMinute;
    }
    
    return data;
}
```

---

## 六、耐久度条样式优化 (P2-1)

### 6.1 修复方案

#### 6.1.1 修改 InventorySlotUI.CreateDurabilityBar()

```csharp
private void CreateDurabilityBar()
{
    // ... 检查已存在 ...
    
    // 🔥 样式参数
    float bottomOffset = 6f / 64f;  // 距离底部 6px（假设槽位 64px）
    float barHeight = 4f / 64f;     // 条高度 4px
    float sideMargin = 6f / 64f;    // 左右边距 6px（贴着 4px 边框）
    
    // 创建背景条（深灰色 + 1px 黑色描边效果）
    var bgGo = new GameObject("DurabilityBarBg");
    bgGo.transform.SetParent(transform, false);
    _durabilityBarBg = bgGo.AddComponent<Image>();
    _durabilityBarBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // 更深的背景
    _durabilityBarBg.raycastTarget = false;
    
    var bgRt = (RectTransform)_durabilityBarBg.transform;
    bgRt.anchorMin = new Vector2(sideMargin, bottomOffset);
    bgRt.anchorMax = new Vector2(1f - sideMargin, bottomOffset + barHeight);
    bgRt.offsetMin = Vector2.zero;
    bgRt.offsetMax = Vector2.zero;
    
    // 🔥 添加 Outline 组件实现描边
    var outline = bgGo.AddComponent<Outline>();
    outline.effectColor = Color.black;
    outline.effectDistance = new Vector2(1, 1);
    
    // 创建前景条（绿色）
    var barGo = new GameObject("DurabilityBar");
    barGo.transform.SetParent(transform, false);
    _durabilityBar = barGo.AddComponent<Image>();
    _durabilityBar.color = new Color(0.2f, 0.8f, 0.2f, 1f);
    _durabilityBar.raycastTarget = false;
    
    var barRt = (RectTransform)_durabilityBar.transform;
    // 🔥 前景条稍微内缩，留出描边空间
    float innerMargin = 1f / 64f;
    barRt.anchorMin = new Vector2(sideMargin + innerMargin, bottomOffset + innerMargin);
    barRt.anchorMax = new Vector2(1f - sideMargin - innerMargin, bottomOffset + barHeight - innerMargin);
    barRt.offsetMin = Vector2.zero;
    barRt.offsetMax = Vector2.zero;
    barRt.pivot = new Vector2(0, 0.5f);
    
    // 默认隐藏
    _durabilityBarBg.enabled = false;
    _durabilityBar.enabled = false;
}
```

---

## 七、交互矩阵设计 (P1+-2)

### 7.1 状态切换矩阵

| 当前状态 | 输入指令 | 预期行为 | 数据处理 |
|---------|---------|---------|---------|
| Game Loop (无UI) | E (面对箱子) | 打开 BoxUI | 绑定数据，刷新 UI |
| Game Loop (无UI) | TAB | 打开 InventoryUI | 刷新 UI |
| BoxUI (打开中) | TAB | 关闭 BoxUI → 打开 InventoryUI | **强制刷新背包数据** |
| BoxUI (打开中) | ESC / E | 关闭 BoxUI → 回到 Game Loop | 清理引用，释放鼠标 |
| InventoryUI (打开中) | TAB / ESC | 关闭 InventoryUI → 回到 Game Loop | 无 |
| InventoryUI (打开中) | E | 关闭 InventoryUI → 触发场景交互 | 如果面前有箱子，下一帧打开箱子 |

### 7.2 实现方案

#### 7.2.1 修改 PackagePanelTabsUI.OnTabPressed()

```csharp
public void OnTabPressed()
{
    // 如果 BoxUI 打开中
    if (_boxPanelUI != null && _boxPanelUI.IsOpen)
    {
        // 1. 先处理手持物品
        ReturnHeldItemToInventory();
        
        // 2. 关闭 BoxUI
        _boxPanelUI.Close();
        
        // 3. 打开 InventoryUI
        OpenInventoryPanel();
        
        // 4. 强制刷新背包数据
        _inventoryPanelUI.RefreshAll();
        return;
    }
    
    // 如果 InventoryUI 打开中
    if (IsOpen)
    {
        Close();
        return;
    }
    
    // 否则打开 InventoryUI
    Open();
}
```

#### 7.2.2 修改 GameInputManager.OnInteract()

```csharp
private void OnInteract()
{
    // 如果 InventoryUI 打开中
    if (_packagePanelTabsUI != null && _packagePanelTabsUI.IsOpen)
    {
        // 关闭 InventoryUI
        _packagePanelTabsUI.Close();
        
        // 触发场景交互（如果面前有箱子，会打开箱子）
        TryInteractWithWorld();
        return;
    }
    
    // 正常场景交互
    TryInteractWithWorld();
}
```

---

## 八、物品归位逻辑 (P1+-1)

### 8.1 物品安全原则

**核心原则**：物品安全第一（Item Safety First）

**绝对禁止**：在任何情况下销毁玩家物品

### 8.2 操作中断与异常矩阵

| 当前操作 | 中断事件 | 处置逻辑 |
|---------|---------|---------|
| 手持物品 | ESC / 关闭 UI | 退回原位 → 背包空位 → 扔在脚下 |
| 拆分中 | 切换 Slot / 关闭 UI | 取消拆分，原槽位恢复原数量 |
| 拖拽中 | 鼠标移出 UI 区域松开 | 触发丢弃逻辑（生成 WorldItemDrop） |
| UI 切换 | 鼠标上有物品 | 执行"手持物品关闭"逻辑 |

### 8.3 实现方案

#### 8.3.1 添加 ReturnHeldItemToInventory() 方法

在 `InventoryInteractionManager.cs` 中添加：

```csharp
/// <summary>
/// 将手持物品归位
/// 优先级：原槽位 → 背包空位 → 扔在脚下
/// </summary>
public void ReturnHeldItemToInventory()
{
    if (_heldItem == null || _heldItem.IsEmpty) return;
    
    // 1. 尝试放回原槽位
    if (_originSlotIndex >= 0 && _originContainer != null)
    {
        var originSlot = _originContainer.GetSlot(_originSlotIndex);
        if (originSlot == null || originSlot.IsEmpty)
        {
            _originContainer.SetSlot(_originSlotIndex, _heldItem);
            ClearHeldItem();
            Debug.Log($"[InventoryInteractionManager] 物品归位：放回原槽位 {_originSlotIndex}");
            return;
        }
        
        // 尝试合并
        if (originSlot.CanStackWith(_heldItem))
        {
            originSlot.AddAmount(_heldItem.Amount);
            _originContainer.RaiseSlotChanged(_originSlotIndex);
            ClearHeldItem();
            Debug.Log($"[InventoryInteractionManager] 物品归位：合并到原槽位 {_originSlotIndex}");
            return;
        }
    }
    
    // 2. 尝试放入背包空位
    var inventoryService = InventoryService.Instance;
    if (inventoryService != null)
    {
        int emptySlot = inventoryService.FindEmptySlot();
        if (emptySlot >= 0)
        {
            inventoryService.SetItem(emptySlot, _heldItem);
            ClearHeldItem();
            Debug.Log($"[InventoryInteractionManager] 物品归位：放入背包空位 {emptySlot}");
            return;
        }
    }
    
    // 3. 扔在脚下（生成 WorldItemDrop）
    DropItemAtPlayerFeet(_heldItem);
    ClearHeldItem();
    Debug.Log($"[InventoryInteractionManager] 物品归位：扔在脚下");
}

/// <summary>
/// 在玩家脚下生成掉落物
/// </summary>
private void DropItemAtPlayerFeet(InventoryItem item)
{
    var player = FindFirstObjectByType<PlayerController>();
    if (player == null) return;
    
    var dropPos = player.transform.position;
    
    // 使用 WorldItemDropService 或直接实例化
    var dropService = WorldItemDropService.Instance;
    if (dropService != null)
    {
        dropService.SpawnDrop(item.ItemId, item.Amount, dropPos, item.Quality);
    }
}
```

#### 8.3.2 在 UI 关闭时调用

修改 `BoxPanelUI.Close()`：

```csharp
public void Close()
{
    // 🔥 关键：先处理手持物品
    var interactionManager = InventoryInteractionManager.Instance;
    if (interactionManager != null)
    {
        interactionManager.ReturnHeldItemToInventory();
    }
    
    // ... 现有关闭逻辑 ...
}
```

修改 `PackagePanelTabsUI.Close()`：

```csharp
public void Close()
{
    // 🔥 关键：先处理手持物品
    var interactionManager = InventoryInteractionManager.Instance;
    if (interactionManager != null)
    {
        interactionManager.ReturnHeldItemToInventory();
    }
    
    // ... 现有关闭逻辑 ...
}
```

---

## 九、正确性属性

### 9.1 存档正确性

**属性 P1**: 对于任意箱子 C 和物品集合 I，如果 I 被放入 C 后保存，则加载后 C 中的物品集合等于 I。

```
∀C, I: Save(C with I) → Load() → C.Items == I
```

### 9.2 拆分正确性

**属性 P2**: 对于任意物品 item 数量为 N，拆分 M 个后，原槽位剩余 N-M，手上持有 M。

```
∀item, N, M where M < N:
  Split(item, M) → original.Amount == N-M ∧ held.Amount == M
```

### 9.3 合并正确性

**属性 P3**: 对于任意两个可堆叠物品 A(数量 Na) 和 B(数量 Nb)，合并后总数量为 Na + Nb。

```
∀A, B where A.CanStackWith(B):
  Merge(A, B) → result.Amount == Na + Nb
```

### 9.4 UI 刷新正确性

**属性 P4**: 对于任意 UI 面板 P，在 OnEnable 后，P 显示的数据等于当前数据源的数据。

```
∀P: P.OnEnable() → P.DisplayedData == DataSource.CurrentData
```

### 9.5 物品安全正确性

**属性 P5**: 对于任意手持物品 H，关闭 UI 后，H 必须存在于（原槽位 ∨ 背包 ∨ 世界掉落物）之一。

```
∀H: CloseUI(holding H) → H ∈ (OriginSlot ∪ Inventory ∪ WorldDrop)
```

**属性 P6**: 对于任意 UI 状态切换，切换后的 UI 显示的数据必须是最新的。

```
∀UI_A, UI_B: Switch(UI_A → UI_B) → UI_B.DisplayedData == DataSource.CurrentData
```

---

## 十、测试策略

### 10.1 单元测试

- `ChestInventoryV2.ToSaveData()` 序列化测试
- `ChestInventoryV2.LoadFromSaveData()` 反序列化测试
- `InventoryItem.Split()` 拆分测试
- `InventoryItem.CanStackWith()` 堆叠判断测试

### 10.2 集成测试

- 箱子存档/加载完整流程测试
- BoxUI 交互流程测试
- 背包 UI 刷新测试

### 10.3 手动验收测试

参见 `验收指南.md`
