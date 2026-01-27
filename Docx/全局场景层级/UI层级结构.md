# UI 层级结构文档

## 更新规则

**每次对场景内 UI 物体进行新建、删除或修改组件时，必须同步更新本文档！**

## 最后更新

- **日期**: 2026-01-07
- **更新者**: Kiro
- **更新内容**: 初始创建，记录 PackagePanel 完整层级结构

---

## UI 根物体

```
UI (Canvas)
├── Canvas (Component)
├── CanvasScaler (Component)
├── GraphicRaycaster (Component) ← 必须存在，否则 UI 无法点击
│
├── State (GameObject)
├── ToolBar (GameObject)
├── PackagePanel (GameObject) ← 背包主面板
├── HeldItemDisplay (GameObject) ← 拖拽/拿取时跟随鼠标的物品显示
└── SprintStateManager (GameObject)
```

---

## PackagePanel 详细结构

```
PackagePanel
├── PackagePanelTabsUI (Component) ← 面板切换控制
├── CanvasGroup (Component)
│
├── Background (Image) ← 背景图片，不参与交互判定
│
├── Main (RectTransform) ← 背包主区域
│   ├── InventoryPanelClickHandler (Component, isDropZone=false)
│   │
│   ├── Background (Image)
│   │
│   ├── 0_Props (GameObject) ← 道具页
│   │   ├── BG (Image)
│   │   ├── Player_BG_0 (Image)
│   │   ├── Player_BG_1 (Image)
│   │   │
│   │   ├── BT_Sort (Button) ← 整理按钮
│   │   │   └── Button (Component)
│   │   │
│   │   ├── BT_TrashCan (Button) ← 垃圾桶按钮
│   │   │   ├── Button (Component)
│   │   │   └── InventoryPanelClickHandler (Component, isDropZone=true)
│   │   │
│   │   ├── Up (ToggleGroup) ← 背包槽位区域（36格）
│   │   │   ├── Up_00 ~ Up_35 (背包槽位)
│   │   │   │   ├── Image (Component, raycastTarget=true)
│   │   │   │   ├── Toggle (Component)
│   │   │   │   │   ├── targetGraphic = Up_XX (Image)
│   │   │   │   │   ├── graphic = Selected (Image)
│   │   │   │   │   └── group = Up (ToggleGroup)
│   │   │   │   ├── InventorySlotUI (Component)
│   │   │   │   ├── InventorySlotInteraction (Component)
│   │   │   │   │
│   │   │   │   ├── Selected (Image) ← 选中样式（红色边框）
│   │   │   │   │   └── 由 Toggle.graphic 控制显示
│   │   │   │   │
│   │   │   │   ├── Icon (Image, raycastTarget=false) ← 物品图标
│   │   │   │   │   └── 由 InventorySlotUI 动态创建
│   │   │   │   │
│   │   │   │   └── Amount (Text, raycastTarget=false) ← 数量文本
│   │   │   │       └── 由 InventorySlotUI 动态创建
│   │   │   │
│   │   │   └── ToggleGroup (Component)
│   │   │
│   │   └── Down (ToggleGroup) ← 装备槽位区域（6格）
│   │       ├── Down_00 ~ Down_05 (装备槽位)
│   │       │   ├── Image (Component, raycastTarget=true)
│   │       │   ├── EquipmentSlotUI (Component)
│   │       │   ├── InventorySlotInteraction (Component)
│   │       │   │
│   │       │   ├── Icon (Image) ← 装备图标
│   │       │   └── Amount (Text) ← 数量文本
│   │       │
│   │       └── ToggleGroup (Component)
│   │
│   ├── 1_Recipes (GameObject) ← 配方页
│   ├── 2_Ex (GameObject) ← 扩展页
│   ├── 3_Map (GameObject) ← 地图页
│   ├── 4_Relationship_NPC (GameObject) ← 关系页
│   └── 5_Settings (GameObject) ← 设置页
│
└── Top (RectTransform) ← Tab 栏区域
    ├── InventoryPanelClickHandler (Component, isDropZone=false)
    │
    ├── Top_0 ~ Top_5 (Tab 按钮)
    │   ├── Toggle (Component)
    │   └── Image (Component)
    │
    └── ToggleGroup (Component)
```

---

## HeldItemDisplay 结构

```
HeldItemDisplay (GameObject)
├── HeldItemDisplay (Component)
├── CanvasGroup (Component)
│   ├── alpha = 0 (初始隐藏)
│   └── blocksRaycasts = false (不阻挡射线)
│
├── Icon (Image)
│   └── raycastTarget = false
│
└── Amount (Text)
    └── raycastTarget = false
```

---

## InventoryInteractionManager 配置

挂载在 **PackagePanel** 上，需要配置以下引用：

| 字段 | 引用目标 | 说明 |
|------|---------|------|
| inventory | InventoryService | 背包服务 |
| equipment | EquipmentService | 装备服务 |
| database | ItemDatabase | 物品数据库 |
| heldDisplay | HeldItemDisplay | 跟随鼠标显示组件 |
| panelRect | PackagePanel | 面板 RectTransform（兼容） |
| mainRect | Main | 背包主区域 |
| topRect | Top | Tab 栏区域 |
| trashCanRect | BT_TrashCan | 垃圾桶区域 |
| inventoryBoundsRect | InventoryBounds | ★ 背包实际可见区域（用于丢弃判定） |
| inventorySlots | Up_00 ~ Up_35 | 36 个背包槽位 |
| equipmentSlots | Down_00 ~ Down_05 | 6 个装备槽位 |
| sortButton | BT_Sort | 整理按钮 |
| sortService | InventorySortService | 整理服务 |

### InventoryBounds 配置说明

`inventoryBoundsRect` 是一个可选字段，用于精确判定"背包可见区域"：

1. **为什么需要**：Main 的 RectTransform 可能是全屏大小（1920x1080），导致 `IsInsidePanel()` 总是返回 true
2. **如何配置**：
   - 在 PackagePanel 下创建一个空 GameObject，命名为 `InventoryBounds`
   - 设置其 RectTransform 大小为背包的实际可见区域
   - 将其拖入 `inventoryBoundsRect` 字段
3. **如果不配置**：回退到使用 Main + Top 区域判定

---

## 关键组件说明

### InventorySlotUI
- 负责显示物品图标和数量
- 动态创建 Icon 和 Amount 子物体
- 监听 Toggle 状态变化控制选中样式

### InventorySlotInteraction
- 实现 Unity 原生事件接口
- 转发事件给 InventoryInteractionManager
- 处理拖拽检测（长按 0.15 秒或移动 5 像素）

### InventoryPanelClickHandler
- 检测非槽位区域的点击
- `isDropZone=true` 时为垃圾桶，点击丢弃物品
- `isDropZone=false` 时为普通区域，点击返回原位

### HeldItemDisplay
- 跟随鼠标显示被拿起的物品
- 使用 CanvasGroup 控制显示/隐藏
- blocksRaycasts=false 确保不阻挡其他 UI 点击

---

## 丢弃区域判定

**背包区域判定优先级**：
1. 如果配置了 `inventoryBoundsRect` → 使用该区域判定
2. 否则使用 Main + Top 区域判定（不包括 Background）

**丢弃逻辑**：
- 在背包区域内松开 → 不丢弃（返回原位或放置到槽位）
- 在背包区域外松开 → 丢弃
- 在 BT_TrashCan 上松开 → 丢弃

**拖拽目标索引常量**：
- `DROP_TARGET_NONE = -2` - 无目标槽位
- `DROP_TARGET_TRASH = -1` - 垃圾桶
- `>= 0` - 目标槽位索引

---

## 装备槽位映射

| 槽位索引 | 装备类型 |
|---------|---------|
| 0 | 头盔 (Helmet) |
| 1 | 裤子 (Pants) |
| 2 | 盔甲 (Armor) |
| 3 | 鞋子 (Shoes) |
| 4 | 戒指1 (Ring) |
| 5 | 戒指2 (Ring) |
