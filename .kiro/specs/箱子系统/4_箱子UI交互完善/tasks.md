# 箱子 UI 交互完善 - 任务列表

## 概述

本任务列表分为**用户待完成项**和**代码待完成项**两部分。

---

## 用户待完成项（Unity 编辑器操作）

### U1. 配置 Box UI 预制体的 Up 区域

**优先级**: P0

**操作步骤**:
1. [ ] 打开 `Assets/222_Prefabs/UI/Box_12.prefab`
2. [ ] 展开 Up 节点
3. [ ] 选中所有 Up_XX 子物体（Up_00, Up_01, ... Up_11）
4. [ ] 为每个子物体添加 `InventorySlotUI` 组件
5. [ ] 保存预制体
6. [ ] 对 `Box_24.prefab` 重复以上步骤（Up_00 ~ Up_23）
7. [ ] 对 `Box_36.prefab` 重复以上步骤（Up_00 ~ Up_35）
8. [ ] 对 `Box_48.prefab` 重复以上步骤（Up_00 ~ Up_47）

**验证方法**:
- 打开箱子后控制台输出 `收集到 XX 个箱子槽位`（XX > 0）

---

### U2. 确认 Down 区域槽位数量

**优先级**: P0

**检查项**:
1. [ ] 确认 `Box_12.prefab` 的 Down 区域槽位数量（应与 UI 设计一致，默认 36 格 = 3×12）
2. [ ] 确认 `Box_24.prefab` 的 Down 区域槽位数量
3. [ ] 确认 `Box_36.prefab` 的 Down 区域槽位数量
4. [ ] 确认 `Box_48.prefab` 的 Down 区域槽位数量

**如果槽位不足**:
- 复制现有槽位，命名为 Down_XX
- 确保每个槽位都有 `InventorySlotUI` 组件

**注意**：Down 区域是背包的镜像视图（0-35），不额外改变选择状态。若实际 UI 设计与 36 格不同，需在 design.md 中更新说明。

---

### U3. 配置 PackagePanelTabsUI 的 boxUIRoot

**优先级**: P1

**检查项**:
1. [ ] 确认 `PackagePanelTabsUI` 组件的 `boxUIRoot` 字段已配置
2. [ ] 如果未配置，在 PackagePanel 下创建 `BoxUIRoot` 空物体
3. [ ] 将 `BoxUIRoot` 拖拽到 `boxUIRoot` 字段

---

## 代码待完成项

### C1. 修复 Down 区域索引

**优先级**: P0
**文件**: `Assets/YYY_Scripts/UI/Box/BoxPanelUI.cs`

**任务**:
- [ ] 修改 `RefreshInventorySlots()` 方法
- [ ] 将 `startIndex` 从 `InventoryService.HotbarWidth`（12）改为 `0`

**代码修改**:
```csharp
// 修改前
int startIndex = InventoryService.HotbarWidth;

// 修改后
int startIndex = 0; // Down 显示完整背包（0-35）
```

**验证方法**:
- 打开箱子后 Down 区域显示背包第一行（与 Hotbar 同步）

---

### C2. 修复 Tab 键与背包打开的互斥逻辑

**优先级**: P0
**文件**: `Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs`

**任务**:
- [ ] 修改 `CloseBoxUI()` 方法
- [ ] 确保 `ShowMainAndTop()` 被正确调用
- [ ] 添加页面恢复逻辑
- [ ] 在 `OpenPanel()` / `OpenOrToggle(idx)` 开头调用 `CloseBoxPanelIfOpen()`

**代码修改**:
```csharp
public void OpenOrToggle(int idx)
{
    // 🔥 关键：打开背包前先关闭 Box UI
    CloseBoxPanelIfOpen();
    
    // 原有逻辑...
}

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
}
```

**验证方法**:
- 打开箱子 → 按 Tab 关闭 → 再按 Tab 能打开背包
- 打开箱子 → 按 B/M/L/O → 箱子关闭，对应背包页面打开

---

### C3. 修复箱子交互检测

**优先级**: P1
**文件**: `Assets/YYY_Scripts/Controller/Input/GameInputManager.cs`

**任务**:
- [ ] 修改交互检测逻辑
- [ ] 对箱子使用 `GetBounds()` 检测而非 Collider

**代码修改**:
```csharp
// 在 HandleInteractable() 或相关方法中
// 对于实现 IResourceNode 的物体，使用 GetBounds() 检测
var resourceNode = interactable as IResourceNode;
if (resourceNode != null)
{
    var bounds = resourceNode.GetBounds();
    Vector3 worldClickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    worldClickPos.z = 0;
    if (!bounds.Contains(worldClickPos))
    {
        return; // 点击不在 Sprite 范围内
    }
}
```

**验证方法**:
- 点击箱子 Sprite 任意位置都能触发交互

---

### C4. 添加箱子存储验证日志

**优先级**: P1
**文件**: `Assets/YYY_Scripts/World/Placeable/ChestController.cs`

**任务**:
- [ ] 在 `Initialize()` 中添加调试日志
- [ ] 验证每个箱子有独立的 ChestInventory 实例

**代码修改**:
```csharp
// ChestController.Initialize()
_inventory = new ChestInventory(storageData.storageCapacity);
if (showDebugInfo)
    Debug.Log($"[ChestController] 创建 ChestInventory: instanceId={GetInstanceID()}, capacity={storageData.storageCapacity}");
```

**验证方法**:
- 放置多个箱子，检查控制台日志确认每个箱子有不同的 instanceId

---

### C5. 修复 Up 区域数据绑定

**优先级**: P0
**文件**: `Assets/YYY_Scripts/UI/Box/BoxPanelUI.cs`

**任务**:
- [ ] 确认 `BindChestSlotData()` 正确绑定数据
- [ ] 如果 `InventorySlotUI` 有专用的箱子绑定方法，使用该方法

**检查项**:
- [ ] `InventorySlotUI` 是否支持绑定 ChestInventory？
- [ ] 如果不支持，需要扩展 `InventorySlotUI` 或使用其他方式

**可能的修改**:
```csharp
// 如果 InventorySlotUI 支持 BindChest 方法
slot.BindChest(_currentChest.Inventory, _database, index);

// 如果不支持，使用手动绑定（当前实现）
BindChestSlotData(slot, index);
```

---

### C6. 实现箱子 Sprite 底部对齐

**优先级**: P0
**文件**: `Assets/YYY_Scripts/World/Placeable/ChestController.cs`

**任务**:
- [ ] 添加 `_anchorWorldPos` 和 `_anchorInitialized` 字段
- [ ] 在 `Awake()` 中记录初始底部中心位置
- [ ] 实现 `GetCurrentBottomCenterWorld()` 方法
- [ ] 实现 `ApplySpriteWithBottomAlign(Sprite newSprite)` 方法
- [ ] 修改 `UpdateSpriteForState()` 使用底部对齐

**代码修改**:
```csharp
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

private void ApplySpriteWithBottomAlign(Sprite newSprite)
{
    if (_spriteRenderer == null || newSprite == null) return;
    
    _spriteRenderer.sprite = newSprite;
    
    Vector3 newBottomCenter = GetCurrentBottomCenterWorld();
    Vector3 delta = _anchorWorldPos - newBottomCenter;
    transform.position += delta;
}
```

**验证方法**:
- 开合箱子时 Sprite 视觉位置不移动

---

### C7. 实现 Sprite→Collider→NavGrid 调用链

**优先级**: P0
**文件**: `Assets/YYY_Scripts/World/Placeable/ChestController.cs`

**前置检查**:
- [ ] 确认箱子 Sprite 是否已配置 physicsShape（在 Sprite Editor 中检查）
- [ ] 如果没有配置 physicsShape，`GetPhysicsShapeCount()` 返回 0，需要先配置

**任务**:
- [ ] 实现 `UpdateColliderShape()` 方法
- [ ] 实现 `RequestNavGridRefresh()` 方法
- [ ] 在 `SetOpen()` 中调用完整链路

**代码修改**:
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

public void SetOpen(bool open)
{
    _isOpen = open;
    UpdateSpriteForState();      // 1. 更新 Sprite（含底部对齐）
    UpdateColliderShape();       // 2. 重建 Collider
    RequestNavGridRefresh();     // 3. 刷新 NavGrid
}
```

**验证方法**:
- 开合箱子后，NavGrid 阻挡区域跟随变化
- 路径规划不会穿过箱子

---

### C8. 验证并记录"UI 打开时世界输入禁用"

**优先级**: P0
**文件**: `Assets/YYY_Scripts/Controller/Input/GameInputManager.cs`

**任务**:
- [ ] 阅读 `GameInputManager` 中的 UI 屏蔽逻辑
- [ ] 验证 Box UI 打开时 `IsAnyPanelOpen` 返回 true
- [ ] 验证以下输入被正确禁用：
  - [ ] 左键点击世界（使用工具）
  - [ ] 右键点击世界（自动导航）
  - [ ] 滚轮切换工具
  - [ ] 数字键切换工具
- [ ] 验证以下输入仍然可用：
  - [ ] Tab 键（切换面板）
  - [ ] ESC 键（关闭面板）
- [ ] 在文档中记录完整的禁用列表

**验证方法**:
- 打开箱子 UI，点击世界不会触发任何行为
- 打开箱子 UI，滚轮不会切换工具

---

### C9. 修复 Tab 键状态机逻辑

**优先级**: P0
**文件**: `Assets/YYY_Scripts/UI/Tabs/PackagePanelTabsUI.cs`

**任务**:
- [ ] 添加 `_wasBackpackOpenBeforeBox` 字段
- [ ] 在 `OpenBoxUI()` 中记录进入前状态
- [ ] 修改 `CloseBoxUI()` 根据进入前状态和触发方式决定行为
- [ ] 区分 Tab 触发和 ESC 触发的关闭行为

**代码修改**:
```csharp
private bool _wasBackpackOpenBeforeBox = false;

public void OpenBoxUI(GameObject boxUiPrefab)
{
    _wasBackpackOpenBeforeBox = IsBackpackVisible();
    // ... 现有逻辑
}

public void CloseBoxUI(bool openBackpackAfter = false)
{
    // ... 销毁 Box UI
    
    if (openBackpackAfter)
    {
        // Tab 触发：关闭 Box 后打开背包
        ShowMainAndTop();
        SetVisiblePage(currentIndex >= 0 ? currentIndex : 0);
    }
    else
    {
        // ESC 触发：返回 NoPanel
        ClosePanel();
    }
    
    _wasBackpackOpenBeforeBox = false;
}
```

**验证方法**:
- BoxOpen 时按 Tab → 关闭箱子，打开背包
- BoxOpen 时按 ESC → 关闭箱子，返回世界

---

## 任务依赖关系

```
U1 (配置 Up 区域) ──┐
                    ├──→ C5 (Up 区域数据绑定)
U2 (确认 Down 槽位) ─┘
                    
C1 (Down 索引修复) ──→ 验证 Down 区域显示

C2 (Tab 键修复) ──→ 验证面板切换

C3 (交互检测) ──→ 验证点击范围

C4 (存储验证) ──→ 验证独立存储
```

---

## 验收测试

### 测试 1：Up 区域显示

1. [ ] 放置一个 24 格箱子
2. [ ] 向箱子放入物品
3. [ ] 关闭并重新打开箱子
4. [ ] **预期**：Up 区域显示箱子内物品

### 测试 2：Down 区域显示

1. [ ] 确保背包有物品（包括 Hotbar）
2. [ ] 打开箱子
3. [ ] **预期**：Down 区域显示完整背包（0-35），第一行与 Hotbar 同步

### 测试 3：Tab 键切换

1. [ ] 打开箱子
2. [ ] 按 Tab 键
3. [ ] **预期**：箱子 UI 关闭，背包面板打开
4. [ ] 再按 Tab 键
5. [ ] **预期**：背包面板关闭

### 测试 4：ESC 键关闭

1. [ ] 打开箱子
2. [ ] 按 ESC 键
3. [ ] **预期**：箱子 UI 关闭，返回世界（不打开背包）

### 测试 5：箱子交互范围

1. [ ] 放置一个箱子
2. [ ] 点击箱子 Sprite 的上半部分（非 Collider 区域）
3. [ ] **预期**：箱子 UI 打开

### 测试 6：独立存储

1. [ ] 放置箱子 A，放入物品
2. [ ] 放置箱子 B
3. [ ] 打开箱子 B
4. [ ] **预期**：箱子 B 为空
5. [ ] 打开箱子 A
6. [ ] **预期**：箱子 A 物品仍在

### 测试 7：Sprite 底部对齐

1. [ ] 放置一个箱子
2. [ ] 记录箱子底部位置
3. [ ] 打开箱子（触发 Sprite 切换）
4. [ ] **预期**：箱子底部位置不变，视觉上不跳动

### 测试 8：Collider + NavGrid 同步

1. [ ] 放置一个箱子
2. [ ] 打开箱子
3. [ ] 尝试让角色走向箱子位置
4. [ ] **预期**：路径规划绕过箱子，不会穿过

### 测试 9：UI 打开时世界输入禁用

1. [ ] 打开箱子 UI
2. [ ] 点击世界中的地面
3. [ ] **预期**：角色不移动
4. [ ] 滚动鼠标滚轮
5. [ ] **预期**：工具不切换
6. [ ] 按数字键 1-5
7. [ ] **预期**：工具不切换

### 测试 10：状态机完整性

1. [ ] 从 NoPanel 按 Tab → **预期**：打开背包
2. [ ] 从 BackpackOpen 点击箱子 → **预期**：关闭背包，打开箱子
3. [ ] 从 BoxOpen 按 Tab → **预期**：关闭箱子，打开背包
4. [ ] 从 BoxOpen 按 ESC → **预期**：关闭箱子，返回 NoPanel
5. [ ] 从 BoxOpen 按 B/M/L/O → **预期**：关闭箱子，打开对应背包页面

---

## 进度跟踪

| 任务 | 状态 | 完成日期 |
|------|------|----------|
| U1 | ✅ 已完成 | 2026-01-16 |
| U2 | ✅ 已完成 | 2026-01-16 |
| U3 | ✅ 已完成 | 2026-01-16 |
| C1 | ✅ 已完成 | 2026-01-16 |
| C2 | ✅ 已完成 | 2026-01-16 |
| C3 | ✅ 已完成 | 2026-01-16 |
| C4 | ✅ 已完成 | 2026-01-16 |
| C5 | ✅ 已完成 | 2026-01-16 |
| C6 | ✅ 已完成 | 2026-01-16 |
| C7 | ✅ 已完成 | 2026-01-16 |
| C8 | ✅ 已完成 | 2026-01-16 |
| C9 | ✅ 已完成 | 2026-01-16 |
