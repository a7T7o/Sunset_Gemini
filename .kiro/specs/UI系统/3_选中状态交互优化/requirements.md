# 选中状态交互优化 - 需求文档

## 概述

本次优化针对背包和箱子 UI 的选中状态（Toggle 红框）交互逻辑进行完善。当前的选中状态触发是左键主导，配合各种修饰键和状态并发的交互。本次修改主要是对选中状态的处理进行适配和完善。

---

## 需求 1：左键单击选中/取消选中（Toggle 基础行为）

### 用户故事

作为玩家，我希望在背包/箱子 UI 中：
- 不带修饰键的左键单击一个槽位 = 选中该槽位（显示红色选中框）
- 再次左键单击同一个槽位 = 取消选中
- 这就是 Toggle 的基础操作，代码应该保持与 Toggle 组件的配合

### 验收标准

1. WHEN 玩家不带修饰键左键单击一个槽位 THEN 该槽位变为选中状态（Toggle.isOn = true）
2. WHEN 玩家再次左键单击同一个已选中的槽位 THEN 该槽位取消选中（Toggle.isOn = false）
3. WHEN 玩家点击同一区域的另一个槽位 THEN 新槽位变为选中，原槽位自动取消选中（ToggleGroup 保证）
4. WHEN 玩家点击空槽位 THEN 空槽位也应该显示选中框

### 技术说明

- 每个区域（背包 Down 区、箱子 Up 区）都已绑定了 ToggleGroup
- ToggleGroup 保证一个区域内只有一个框被选中
- 代码只需要配合 Toggle 组件，不需要手动管理互斥逻辑

### 当前状态

- `InventorySlotUI.OnPointerClick` 中没有实际逻辑，依赖 Toggle 组件自动管理
- 这部分应该已经正常工作，需要确认不被其他逻辑干扰

---

## 需求 2：拿起物品后放置时的选中状态绑定

### 用户故事

作为玩家，我希望用任意方式拿起物品后放置时：
- 放置物品的目标槽位自动变为选中状态
- 原槽位的选中状态被取消

### 前置条件

**触发选中状态变更的前置条件**：用任意方式拿起物体后放置
- 拖拽放置
- Shift 拿取后点击放置
- Ctrl 拿取后点击放置

### 场景分析

#### 场景 A：同一区域内放置
- 从背包槽位 A 拿起，放置到背包槽位 B
- 结果：槽位 B 变为选中状态（槽位 A 自动取消选中，因为 ToggleGroup）

#### 场景 B：跨区域放置
- 从箱子槽位 A 拿起，放置到背包槽位 B
- 结果：
  - 箱子区域：槽位 A 取消选中（需要手动调用 Deselect）
  - 背包区域：槽位 B 变为选中状态

### 验收标准

1. WHEN 玩家在同一区域内拖拽放置物品 THEN 目标槽位变为选中状态
2. WHEN 玩家跨区域拖拽放置物品 THEN 原区域取消选中，目标区域槽位变为选中
3. WHEN 玩家 Shift 拿取后点击放置 THEN 目标槽位变为选中状态
4. WHEN 玩家 Ctrl 拿取后点击放置 THEN 目标槽位变为选中状态
5. WHEN 玩家交换物品时 THEN 目标槽位变为选中状态
6. WHEN 玩家堆叠物品时 THEN 目标槽位变为选中状态
7. WHEN 玩家取消拿取（ESC 或返回原位）THEN 选中状态不变

### 当前状态

- `InventoryInteractionManager.ExecutePlacement` 中有 `SelectSlot()` 调用
- 但只处理了背包槽位（通过 `inventorySlots` 数组）
- 没有处理箱子槽位的选中
- 跨区域拖拽时没有取消原区域的选中状态

---

## 需求 3：修复 Ctrl+长按拿取数量显示 Bug

### 问题描述

当格子物品数量 > 1 时，使用 Ctrl+长按左键拿取：
- 预期：拿干净后手上显示数量 = 原槽位总数量（如 10 个）
- 实际：拿干净后手上显示数量比原来少 1 个（如 10 个变 9 个）
- 放置后又变回 10 个（说明数据是对的，只是显示有问题）

### 复现步骤

1. 准备一个槽位有 10 个物品
2. Ctrl+长按左键拿取
3. 等待拿干净（源槽位变空）
4. 观察手上显示数量：显示 9 个（错误）
5. 放置到空槽位
6. 观察槽位数量：显示 10 个（正确）

### 根因分析

查看 `InventoryInteractionManager.ContinueCtrlPickup()` 协程：

```csharp
private IEnumerator ContinueCtrlPickup()
{
    float interval = 1f / ctrlPickupRate;
    while (true)
    {
        yield return new WaitForSeconds(interval);
        
        // ... 检查条件 ...
        
        heldItem.amount++;  // 增加手上数量
        if (src.amount > 1)
            SetSlot(..., amount = src.amount - 1);  // 减少源槽位
        else
        {
            ClearSlot(sourceIndex, sourceIsEquip);  // 清空源槽位
            break;  // 🔥 问题点：break 后没有调用 ShowHeld()
        }
        ShowHeld();  // 只有在 else 分支外才调用
    }
    ctrlCoroutine = null;
}
```

**问题根因**：当源槽位只剩 1 个时，执行 `ClearSlot` 后直接 `break`，没有调用 `ShowHeld()` 更新显示。此时 `heldItem.amount` 已经 +1 了，但显示没有更新。

### 验收标准

1. WHEN 玩家 Ctrl+长按拿取 10 个物品直到拿干净 THEN 手上显示 10 个
2. WHEN 玩家放置后 THEN 槽位显示 10 个
3. WHEN 玩家 Ctrl+长按拿取过程中松开 THEN 手上数量 + 源槽位数量 = 原始数量

---

## 技术约束

1. **ToggleGroup 规则**：每个区域都已绑定 ToggleGroup，一个区域只能有一个框被选中，代码只需配合
2. **不破坏现有功能**：修改不能影响现有的拖拽、堆叠、交换逻辑
3. **日志规范**：遵循 `debug-logging-standards.md`，不输出高频日志
4. **最小侵入**：尽量少修改代码，只在必要的地方添加选中状态处理

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs` | 槽位 UI，包含 Select/Deselect 方法 |
| `Assets/YYY_Scripts/UI/Inventory/InventorySlotInteraction.cs` | 槽位交互，处理拖拽和点击 |
| `Assets/YYY_Scripts/UI/Inventory/InventoryInteractionManager.cs` | 交互管理器，处理 Held 状态和放置逻辑 |
| `Assets/YYY_Scripts/UI/Inventory/SlotDragContext.cs` | 拖拽上下文，跨容器拖拽支持 |
| `Assets/YYY_Scripts/UI/Inventory/HeldItemDisplay.cs` | Held 物品显示 |

---

## 优先级

| 需求 | 优先级 | 说明 |
|------|--------|------|
| 需求 3（Ctrl+长按显示 Bug） | P0 | 显示不一致问题，影响用户体验 |
| 需求 2（放置后选中绑定） | P1 | 交互体验优化，核心需求 |
| 需求 1（左键选中确认） | P2 | 基础功能确认，应该已经正常 |

---

**创建日期**: 2026-01-20
**状态**: 待审核
