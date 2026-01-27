# 调试日志规范

**创建日期**: 2026-01-17  
**适用范围**: 所有 UI 和交互系统  
**优先级**: P1（强制执行）

---

## 核心原则

1. **精准性** - 只输出关键信息，不输出冗余信息
2. **简洁性** - 每次操作的日志数量 ≤ 5 行
3. **可控性** - 使用开关控制日志输出
4. **唯一性** - 关键错误只打印一次，避免重复

---

## 日志分级

### Level 1: 关键错误（必须输出）

**特征**:
- 导致功能完全失效
- 需要立即修复
- 用户可见的错误

**示例**:
```csharp
Debug.LogError($"[BoxPanelUI] 无法打开箱子: chest 为 null");
Debug.LogError($"[ChestInventory] Database 未初始化");
```

**规则**:
- 使用 `Debug.LogError()`
- 包含组件名前缀 `[ComponentName]`
- 说明错误原因和影响
- 只打印一次（使用静态标志防止重复）

---

### Level 2: 警告信息（条件输出）

**特征**:
- 功能部分失效或降级
- 可能导致后续问题
- 需要关注但不紧急

**示例**:
```csharp
Debug.LogWarning($"[BoxPanelUI] BindChestSlotData 失败: _database 为 null，使用默认值");
```

**规则**:
- 使用 `Debug.LogWarning()`
- 只在开发模式下输出
- 避免在循环中重复输出

---

### Level 3: 调试信息（默认关闭）

**特征**:
- 开发调试用
- 不影响功能
- 用于追踪执行流程

**示例**:
```csharp
if (showDebugInfo)
{
    Debug.Log($"[BoxPanelUI] Open: chest={chest.name}, slots={_upSlots.Count}");
}
```

**规则**:
- 使用 `Debug.Log()`
- 必须有开关控制（`showDebugInfo`、`DebugEnabled` 等）
- 默认关闭，只在需要时开启

---

## 高频函数日志规范

### 禁止在以下函数中输出详细日志

- `Update()` / `FixedUpdate()` / `LateUpdate()`
- `Refresh()` / `UpdateUI()` / `Repaint()`
- `OnPointerEnter()` / `OnPointerExit()` / `OnPointerMove()`
- 任何每帧调用或高频调用的函数

**例外**: 可以使用计数器限制输出频率

```csharp
private int _debugFrameCounter = 0;

void Update()
{
    if (showDebugInfo && _debugFrameCounter++ % 60 == 0)
    {
        Debug.Log($"[Component] 每秒输出一次的调试信息");
    }
}
```

---

## 组件级日志开关

### 标准实现

```csharp
public class MyComponent : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private void SomeMethod()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[MyComponent] 调试信息");
        }
    }
}
```

### 全局开关（可选）

```csharp
public static class DebugSettings
{
    public static bool EnableUIDebugLogs = false;
    public static bool EnableInventoryDebugLogs = false;
    public static bool EnableInteractionDebugLogs = false;
}
```

---

## 日志格式规范

### 标准格式

```
[组件名] 操作: 关键信息
```

**示例**:
```csharp
Debug.Log($"[BoxPanelUI] Open: chest={chest.name}");
Debug.LogWarning($"[InventorySlotUI] Bind 失败: container 为 null");
Debug.LogError($"[ChestInventory] TransferToInventory 失败: 目标槽位已满");
```

### 多行日志（避免使用）

**❌ 错误做法**:
```csharp
Debug.Log($"[UIItemIconScaler] UpdateScale:");
Debug.Log($"  sprite: {sprite.name}");
Debug.Log($"  sizeDelta: {sizeDelta}");
Debug.Log($"  rotation: {rotation}");
```

**✅ 正确做法**:
```csharp
if (showDebugInfo)
{
    Debug.Log($"[UIItemIconScaler] sprite={sprite.name}, size={sizeDelta}, rot={rotation}");
}
```

---

## 错误日志去重

### 问题场景

同一个错误在循环中被打印多次：

```csharp
// ❌ 错误做法
foreach (var slot in slots)
{
    if (slot.Container == null)
    {
        Debug.LogWarning($"[BoxPanelUI] 槽位 {slot.SlotIndex} 的 Container 为 null");
    }
}
```

### 解决方案

**方案 A: 统计后一次性输出**
```csharp
int nullCount = slots.Count(s => s.Container == null);
if (nullCount > 0)
{
    Debug.LogWarning($"[BoxPanelUI] {nullCount} 个槽位的 Container 为 null");
}
```

**方案 B: 使用静态标志**
```csharp
private static bool _hasLoggedContainerNull = false;

if (slot.Container == null && !_hasLoggedContainerNull)
{
    Debug.LogWarning($"[BoxPanelUI] 检测到槽位 Container 为 null（后续不再提示）");
    _hasLoggedContainerNull = true;
}
```

---

## 特定组件规范

### UIItemIconScaler

**问题**: 每次 Refresh 都输出详细日志，导致日志爆炸

**修复**:
```csharp
[Header("Debug")]
[SerializeField] private bool showDebugInfo = false;

private void UpdateScale()
{
    // ... 计算逻辑 ...
    
    if (showDebugInfo)
    {
        Debug.Log($"[UIItemIconScaler] sprite={sprite?.name}, size={sizeDelta}");
    }
}
```

---

### InventorySlotUI

**规则**:
- `Refresh()` 不输出日志（高频调用）
- `Bind()` / `BindContainer()` 只在失败时输出警告
- `OnPointerEnter/Exit` 不输出日志

**示例**:
```csharp
public void BindContainer(IItemContainer container, int slotIndex)
{
    if (container == null)
    {
        Debug.LogWarning($"[InventorySlotUI] BindContainer 失败: container 为 null");
        return;
    }
    
    // 不输出成功日志
    Container = container;
    SlotIndex = slotIndex;
    Refresh();
}
```

---

### BoxPanelUI

**规则**:
- `Open()` 输出一行关键信息
- `BindChestSlotData()` 失败时输出警告（只一次）
- `Refresh()` 不输出日志

**示例**:
```csharp
public void Open(ChestController chest)
{
    Debug.Log($"[BoxPanelUI] Open: chest={chest.name}, capacity={chest.Inventory.Capacity}");
    
    // ... 绑定逻辑 ...
}

private static bool _hasLoggedBindFailure = false;

private void BindChestSlotData()
{
    if (_database == null && !_hasLoggedBindFailure)
    {
        Debug.LogWarning($"[BoxPanelUI] BindChestSlotData 失败: _database 为 null");
        _hasLoggedBindFailure = true;
        return;
    }
    
    // 不输出成功日志
}
```

---

## 验收标准

修复完成后，日志输出必须满足：

- [ ] 打开箱子 UI：≤ 3 行日志
- [ ] 点击槽位：≤ 2 行日志
- [ ] 拖拽物品：≤ 3 行日志
- [ ] Sort 操作：≤ 2 行日志
- [ ] 关闭箱子 UI：≤ 1 行日志
- [ ] 无重复错误日志
- [ ] 无 Refresh 相关的详细日志

**总计**: 一次完整的箱子交互流程（打开 → 操作 → 关闭）日志数量 ≤ 15 行

---

## 实施检查清单

在提交代码前，必须检查：

- [ ] 所有 `Debug.Log()` 都有开关控制
- [ ] 高频函数中没有详细日志
- [ ] 错误日志不会重复输出
- [ ] 日志格式符合 `[组件名] 操作: 信息` 规范
- [ ] 关键错误使用 `Debug.LogError()`
- [ ] 警告信息使用 `Debug.LogWarning()`
- [ ] 调试信息使用 `Debug.Log()` + 开关

---

**文档维护者**: Code Reaper  
**最后更新**: 2026-01-17
