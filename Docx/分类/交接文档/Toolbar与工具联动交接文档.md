# Toolbar与工具联动交接文档

版本: v1.0  
日期: 2024-12-11  
作者: Kiro AI  
状态: ✅ 已验证通过

---

## 1. 文档目的

本文档详细记录 Sunset 项目中 **工具栏(Toolbar)** 与 **工具系统** 的联动机制,包括:
- 系统架构与组件职责
- 数据流与事件流
- 关键代码路径
- 常见问题与解决方案
- 扩展指南

供后续开发者理解和维护该系统。

---

## 2. 系统架构概览

### 2.1 核心组件

```
┌─────────────────────────────────────────────────────────────────────┐
│                           用户交互层                                  │
├─────────────────────────────────────────────────────────────────────┤
│  ToolbarUI          ToolbarSlotUI        GameInputManager           │
│  (工具栏容器)        (单个槽位UI)          (输入管理器)                │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           服务层                                      │
├─────────────────────────────────────────────────────────────────────┤
│  HotbarSelectionService    InventoryService    PlayerInteraction    │
│  (选中管理+装备)            (背包数据)           (动作执行)            │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           控制层                                      │
├─────────────────────────────────────────────────────────────────────┤
│  PlayerToolController      PlayerAnimController   LayerAnimSync     │
│  (工具参数设置)             (动画状态机)           (帧同步)            │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           数据层                                      │
├─────────────────────────────────────────────────────────────────────┤
│  ItemDatabase (SO)         ToolData (SO)         WeaponData (SO)    │
│  (物品数据库)               (工具数据)             (武器数据)          │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 组件职责表

| 组件 | 类型 | 职责 | 文件路径 |
|------|------|------|----------|
| `ToolbarUI` | MonoBehaviour | 工具栏容器,管理所有槽位 | `Assets/Scripts/UI/Toolbar/ToolbarUI.cs` |
| `ToolbarSlotUI` | MonoBehaviour | 单个槽位UI,处理点击事件 | `Assets/Scripts/UI/Toolbar/ToolbarSlotUI.cs` |
| `HotbarSelectionService` | MonoBehaviour | 管理选中索引,触发装备 | `Assets/Scripts/Service/Inventory/HotbarSelectionService.cs` |
| `InventoryService` | MonoBehaviour | 背包数据管理,提供槽位信息 | `Assets/Scripts/Service/Inventory/InventoryService.cs` |
| `GameInputManager` | MonoBehaviour | 输入检测,触发工具使用动画 | `Assets/Scripts/Controller/Input/GameInputManager.cs` |
| `PlayerInteraction` | MonoBehaviour | 执行动作,播放动画 | `Assets/Scripts/Service/Player/PlayerInteraction.cs` |
| `PlayerToolController` | MonoBehaviour | 设置工具Animator参数 | `Assets/Scripts/Anim/Player/PlayerToolController.cs` |
| `PlayerAnimController` | MonoBehaviour | 玩家动画状态机控制 | `Assets/Scripts/Anim/Player/PlayerAnimController.cs` |
| `ItemDatabase` | ScriptableObject | 物品数据库,存储所有物品 | `Assets/Scripts/Data/Database/ItemDatabase.cs` |
| `ToolData` | ScriptableObject | 工具数据定义 | `Assets/Scripts/Data/Items/ToolData.cs` |
| `WeaponData` | ScriptableObject | 武器数据定义 | `Assets/Scripts/Data/Items/WeaponData.cs` |

---

## 3. 数据流详解

### 3.1 工具选中流程

```
用户点击工具栏槽位
    │
    ▼
ToolbarSlotUI.OnPointerClick()
    │ 调用
    ▼
HotbarSelectionService.SelectIndex(index)
    │
    ├─→ 更新 selectedIndex
    │
    ├─→ 调用 EquipCurrentTool()
    │       │
    │       ├─→ inventory.GetSlot(selectedIndex) 获取槽位数据
    │       │
    │       ├─→ database.GetItemByID(slot.itemId) 获取物品数据
    │       │
    │       └─→ playerToolController.EquipToolData(toolData, quality)
    │               │
    │               └─→ 设置 Animator 参数: ToolType, ToolQuality, ToolItemId
    │
    └─→ 触发 OnSelectedChanged 事件
            │
            └─→ ToolbarSlotUI.RefreshSelection() 更新UI高亮
```

### 3.2 工具使用流程

```
用户左键点击世界空间
    │
    ▼
GameInputManager.Update()
    │ 调用
    ▼
HandleUseCurrentTool()
    │
    ├─→ 检测 Input.GetMouseButtonDown(0)
    │
    ├─→ 检测 IsPointerOverGameObject() → 如果在UI上则返回
    │
    ├─→ inventory.GetSlot(selectedIndex) 获取槽位数据
    │
    ├─→ database.GetItemByID(slot.itemId) 获取物品数据
    │
    └─→ playerInteraction.RequestAction(action)
            │
            ▼
        PlayerInteraction.PerformAction(action)
            │
            ├─→ playerMovement.StopMovement() 停止移动
            │
            └─→ animController.PlayAnimation(action, direction, flip)
                    │
                    └─→ 设置 Animator 参数: State, Direction
                            │
                            ▼
                        LayerAnimSync.LateUpdate()
                            │
                            └─→ 同步工具动画帧
```

### 3.3 数据结构

#### ItemStack (槽位数据)
```csharp
public struct ItemStack
{
    public int itemId;    // 物品ID
    public int quality;   // 品质等级
    public int amount;    // 数量
    public bool IsEmpty => itemId < 0 || amount <= 0;
}
```

#### 工具类型映射
```csharp
public enum ToolType
{
    Axe = 0,        // 斧头 → Slice 动画
    Pickaxe = 1,    // 镐子 → Crush 动画
    Hoe = 2,        // 锄头 → Pierce 动画
    WateringCan = 3,// 水壶 → Watering 动画
    Sickle = 4,     // 镰刀 → Slice 动画
    FishingRod = 5  // 鱼竿 → Fish 动画
}
```

#### 动画状态映射
```csharp
public enum AnimState
{
    Idle = 0,
    Walk = 1,
    Run = 2,
    Carry = 3,
    Collect = 4,
    Hit = 5,
    Slice = 6,      // 斧头/镰刀
    Pierce = 7,     // 锄头
    Crush = 8,      // 镐子
    Fish = 9,       // 鱼竿
    Watering = 10,  // 水壶
    Death = 11
}
```

---

## 4. 事件系统

### 4.1 事件列表

| 事件 | 发布者 | 订阅者 | 触发时机 |
|------|--------|--------|----------|
| `OnSelectedChanged(int)` | HotbarSelectionService | ToolbarUI, ToolbarSlotUI | 选中索引变化 |
| `OnSlotChanged(int)` | InventoryService | ToolbarSlotUI | 槽位内容变化 |
| `OnHotbarSlotChanged(int)` | InventoryService | ToolbarSlotUI | 热键栏槽位变化 |
| `OnInventoryChanged` | InventoryService | 各UI组件 | 背包整体变化 |

### 4.2 事件订阅示例

```csharp
// ToolbarSlotUI.cs
void OnEnable()
{
    if (inventory != null)
    {
        inventory.OnHotbarSlotChanged += HandleHotbarChanged;
        inventory.OnSlotChanged += HandleAnySlotChanged;
    }
    if (selection != null)
    {
        selection.OnSelectedChanged += HandleSelectionChanged;
    }
}

void OnDisable()
{
    if (inventory != null)
    {
        inventory.OnHotbarSlotChanged -= HandleHotbarChanged;
        inventory.OnSlotChanged -= HandleAnySlotChanged;
    }
    if (selection != null)
    {
        selection.OnSelectedChanged -= HandleSelectionChanged;
    }
}
```

---

## 5. 关键代码路径

### 5.1 装备工具

**入口**: `HotbarSelectionService.SelectIndex(int index)`

```csharp
public void SelectIndex(int index)
{
    int clamped = Mathf.Clamp(index, 0, InventoryService.HotbarWidth - 1);
    if (clamped == selectedIndex) return;
    selectedIndex = clamped;
    
    // 选中变化时立即装备工具
    EquipCurrentTool();
    
    OnSelectedChanged?.Invoke(selectedIndex);
}

private void EquipCurrentTool()
{
    if (playerToolController == null || inventory == null || database == null)
        return;

    var slot = inventory.GetSlot(selectedIndex);
    if (slot.IsEmpty) return;

    var itemData = database.GetItemByID(slot.itemId);
    if (itemData == null) return;

    if (itemData is ToolData toolData)
        playerToolController.EquipToolData(toolData, slot.quality);
    else if (itemData is WeaponData weaponData)
        playerToolController.EquipWeaponData(weaponData, slot.quality);
}
```

### 5.2 使用工具

**入口**: `GameInputManager.HandleUseCurrentTool()`

```csharp
void HandleUseCurrentTool()
{
    if (!Input.GetMouseButtonDown(0)) return;
    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
    
    if (inventory == null || database == null || hotbarSelection == null) return;
    
    int idx = Mathf.Clamp(hotbarSelection.selectedIndex, 0, InventoryService.HotbarWidth - 1);
    var slot = inventory.GetSlot(idx);
    if (slot.IsEmpty) return;
    
    var itemData = database.GetItemByID(slot.itemId);
    if (itemData == null) return;

    if (itemData is ToolData tool)
    {
        var action = ResolveAction(tool.toolType);
        playerInteraction?.RequestAction(action);
    }
    else if (itemData is WeaponData)
    {
        playerInteraction?.RequestAction(PlayerAnimController.AnimState.Slice);
    }
}
```

### 5.3 工具类型到动画映射

```csharp
PlayerAnimController.AnimState ResolveAction(ToolType type)
{
    switch (type)
    {
        case ToolType.Axe: return PlayerAnimController.AnimState.Slice;
        case ToolType.Pickaxe: return PlayerAnimController.AnimState.Crush;
        case ToolType.Sickle: return PlayerAnimController.AnimState.Slice;
        case ToolType.WateringCan: return PlayerAnimController.AnimState.Watering;
        case ToolType.FishingRod: return PlayerAnimController.AnimState.Fish;
        case ToolType.Hoe: return PlayerAnimController.AnimState.Pierce;
        default: return PlayerAnimController.AnimState.Slice;
    }
}
```

---

## 6. ScriptableObject 引用获取

### 6.1 问题背景

`ItemDatabase` 是 `ScriptableObject` 资产,不是场景中的 `MonoBehaviour` 组件。

**错误做法**:
```csharp
database = FindFirstObjectByType<ItemDatabase>();  // 永远返回 null
```

### 6.2 正确做法

从已有引用的 `MonoBehaviour` 获取:

```csharp
// InventoryService.cs
public ItemDatabase Database => database;  // 公开属性

// HotbarSelectionService.cs / GameInputManager.cs
void Awake()
{
    if (inventory == null)
        inventory = FindFirstObjectByType<InventoryService>();
    
    // 从 InventoryService 获取 database
    if (inventory != null)
        database = inventory.Database;
}
```

### 6.3 Inspector 配置

确保以下组件在 Inspector 中正确配置了 `ItemDatabase` 引用:

| 组件 | 字段名 | 资产 |
|------|--------|------|
| InventoryService | Database | MasterItemDatabase |
| EquipmentService | Database | MasterItemDatabase |

---

## 7. 场景配置

### 7.1 必需的 GameObject

```
Scene
├── ─── MANAGERS ───
│   ├── InventorySystem (InventoryService)
│   ├── HotbarSelection (HotbarSelectionService)
│   ├── EquipmentSystem (EquipmentService)
│   └── Systems (GameInputManager)
├── ─── DYNAMIC ───
│   └── Player
│       ├── PlayerMovement
│       ├── PlayerAnimController
│       ├── PlayerToolController
│       ├── PlayerInteraction
│       └── LayerAnimSync
└── ─── UI ───
    └── Canvas
        └── ToolBar (ToolbarUI)
            ├── Bar_00_TG (ToolbarSlotUI)
            ├── Bar_01_TG (ToolbarSlotUI)
            └── ... (共12个槽位)
```

### 7.2 组件引用配置

#### HotbarSelectionService
| 字段 | 引用 |
|------|------|
| Player Tool Controller | Player (PlayerToolController) |
| Inventory | InventorySystem (InventoryService) |
| Database | 自动从 Inventory 获取 |

#### GameInputManager
| 字段 | 引用 |
|------|------|
| Player Movement | 自动查找 |
| Player Interaction | 自动查找 |
| Player Tool Controller | 自动查找 |
| Inventory | 自动查找 |
| Hotbar Selection | 自动查找 |
| Database | 自动从 Inventory 获取 |

---

## 8. 输入系统

### 8.1 工具栏快捷键

| 按键 | 功能 |
|------|------|
| 1-5 | 选中对应槽位 (0-4) |
| 滚轮上 | 选中上一个槽位 |
| 滚轮下 | 选中下一个槽位 |
| 左键点击槽位 | 选中该槽位 |

### 8.2 工具使用

| 输入 | 条件 | 功能 |
|------|------|------|
| 左键点击 | 不在UI上 | 使用当前工具 |
| 左键点击 | 在UI上 | 无效果 |

### 8.3 代码位置

```csharp
// GameInputManager.cs
void HandleHotbarSelection()
{
    // 滚轮切换
    float scroll = Input.mouseScrollDelta.y;
    if (scroll < 0f) hotbarSelection?.SelectNext();
    else if (scroll > 0f) hotbarSelection?.SelectPrev();

    // 数字键选择
    if (Input.GetKeyDown(KeyCode.Alpha1)) hotbarSelection?.SelectIndex(0);
    if (Input.GetKeyDown(KeyCode.Alpha2)) hotbarSelection?.SelectIndex(1);
    if (Input.GetKeyDown(KeyCode.Alpha3)) hotbarSelection?.SelectIndex(2);
    if (Input.GetKeyDown(KeyCode.Alpha4)) hotbarSelection?.SelectIndex(3);
    if (Input.GetKeyDown(KeyCode.Alpha5)) hotbarSelection?.SelectIndex(4);
}
```

---

## 9. 动画同步系统

### 9.1 Mode A++ 帧索引硬锁

工具动画与玩家动画采用 **帧索引硬锁** 同步方案:

1. 从 Player 的 Sprite 名称解析帧号
2. 按帧数比例映射到 Tool 帧号
3. 使用 `Play(hash, normalizedTime) + Update(0)` 实现当帧同步
4. 添加极小正偏置避免边界抖动

### 9.2 动画状态命名规范

```
{ActionType}_{Direction}_Clip_{itemId}_{quality}
```

示例:
- `Slice_Down_Clip_0_0` - 斧头向下砍,品质0
- `Crush_Side_Clip_1_0` - 镐子侧面挖,品质0

### 9.3 相关文档

- `Docx/Summary/人物与工具动画同步方案总结.md`
- `Docx/Plan/手持物品动画同步与三向生成器总体规划.md`

---

## 10. 常见问题与解决方案

### 10.1 工具动画不触发

**症状**: 点击世界空间,工具动画不播放

**排查步骤**:
1. 检查 Console 是否有 `database 为 null` 错误
2. 检查 `InventoryService` 的 Database 字段是否配置
3. 检查 `HotbarSelectionService` 的引用是否正确
4. 检查槽位是否有工具物品

**解决方案**: 确保 `InventoryService.Database` 已配置 `MasterItemDatabase`

### 10.2 选中槽位不高亮

**症状**: 点击工具栏槽位,高亮框不显示

**排查步骤**:
1. 检查 `ToolbarSlotUI` 的 `selectedOverlay` 引用
2. 检查 `HotbarSelectionService` 的 `OnSelectedChanged` 事件是否触发

**解决方案**: 确保 `ToolbarSlotUI` 的 `Selected` 子物体存在且有 Image 组件

### 10.3 工具类型不匹配

**症状**: 选中斧头但播放镐子动画

**排查步骤**:
1. 检查 `ToolData.toolType` 是否正确设置
2. 检查 `ResolveAction()` 映射是否正确

**解决方案**: 在 Inspector 中检查工具的 `toolType` 字段

---

## 11. 扩展指南

### 11.1 添加新工具类型

1. 在 `ToolType` 枚举中添加新类型
2. 在 `AnimState` 枚举中添加对应动画状态(如需要)
3. 在 `ResolveAction()` 中添加映射
4. 创建对应的动画剪辑和控制器

### 11.2 添加工具使用效果

在 `PlayerInteraction.PerformAction()` 中添加:

```csharp
private void PerformAction(PlayerAnimController.AnimState action)
{
    // ... 现有代码 ...
    
    // 添加工具使用效果
    OnToolUsed?.Invoke(action);  // 触发事件
    PlayToolSound(action);       // 播放音效
    SpawnToolEffect(action);     // 生成特效
}
```

### 11.3 添加工具耐久度

1. 在 `ItemStack` 中添加 `durability` 字段
2. 在 `PlayerInteraction.OnActionComplete()` 中减少耐久度
3. 在 `InventoryService` 中添加耐久度更新方法

---

## 12. 调试技巧

### 12.1 启用调试日志

在相关脚本中已添加详细的调试日志,以 `[组件名]` 为前缀:

```
[HotbarSelection] EquipCurrentTool 被调用, selectedIndex=0
[GameInput] 检测到左键点击
[PlayerInteraction] RequestAction 被调用: action=Slice
```

### 12.2 Inspector 实时查看

- `PlayerToolController`: 查看 `currentToolType` 和 `currentToolQuality`
- `HotbarSelectionService`: 查看 `selectedIndex`
- `InventoryService`: 查看 `slots` 数组内容

### 12.3 移除调试日志

修复完成后,可以移除或注释掉 `Debug.Log` 语句以提高性能。

---

## 13. 版本历史

| 版本 | 日期 | 修改内容 |
|------|------|----------|
| v1.0 | 2024-12-11 | 初始版本,修复工具动画不触发问题 |

---

## 14. 相关文档索引

| 文档 | 路径 | 内容 |
|------|------|------|
| 修复方案 | `Docx/Solutions/工具栏工具装备与使用分离修复方案.md` | 本次修复的详细方案 |
| 动画同步 | `Docx/Summary/人物与工具动画同步方案总结.md` | Mode A++ 帧锁定方案 |
| UI系统设计 | `Docx/Plan/UI系统与总系统设计规划.md` | UI整体架构设计 |
| 第一阶段报告 | `Docx/Summary/第一阶段完结报告.md` | 项目进度总结 |
| 手持物品规划 | `Docx/Plan/手持物品动画同步与三向生成器总体规划.md` | 工具动画生成器规划 |

---

**文档完成时间**: 2024-12-11  
**维护责任人**: 开发团队  
**下次更新**: 添加新功能时
