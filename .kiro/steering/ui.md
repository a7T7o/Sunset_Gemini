---
inclusion: manual
priority: P1
keywords: [UI, 面板, 快捷键, Toggle, 工具栏, PackagePanel]
lastUpdated: 2025-01-09
---

# UI 系统规范

## 面板层级结构

```
UI
└── PackagePanel
    ├── Main（主面板容器）
    │   ├── 0_Props（道具页）
    │   ├── 1_Recipes（配方页）
    │   ├── 2_Ex（扩展页）
    │   ├── 3_Map（地图页）
    │   ├── 4_Relationship_NPC（关系页）
    │   └── 5_Settings（设置页）
    └── Top（顶部Tab栏）
        ├── Tab_Props
        ├── Tab_Recipes
        ├── Tab_Map
        ├── Tab_Relationship
        └── Tab_Settings
```

## 物品堆叠数量显示（Amount）

### 标准配置
| 属性 | 值 |
|------|------|
| 字体 | LegacyRuntime（默认字体） |
| 字体大小 | 18 |
| 字体样式 | 加粗与倾斜（BoldAndItalic） |
| 颜色 | 纯黑不透明（#000000） |
| 对齐 | 右对齐，靠下（LowerRight） |
| 锚点 | 全拉伸（0,0 到 1,1） |
| 位置 | 左21.2356，顶部41.568，右3.8808，底部0 |

### RectTransform 设置
```csharp
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.pivot = new Vector2(0.5f, 0.5f);
rt.offsetMin = new Vector2(21.2356f, 0f);      // left, bottom
rt.offsetMax = new Vector2(-3.8808f, -41.568f); // -right, -top
```

## 快捷键系统

| 按键 | 功能 |
|------|------|
| Tab/B | 切换显示 Props 页 |
| M | 切换显示 Map 页 |
| L | 切换显示 Relationship 页 |
| O | 切换显示 Settings 页 |
| ESC | 已打开任意页面→打开Settings页；未打开→打开Settings面板 |
| J | 保留给任务面板（当前无功能） |
| 1-8 | 快捷栏槽位选择 |
| 滚轮 | 快捷栏上下切换 |

## 面板切换逻辑

### 状态机设计
- 面板关闭 + 按键 → 打开面板并显示对应页
- 面板打开 + 按相同键 → 关闭面板
- 面板打开 + 按不同键 → 切换页面

### Toggle 状态同步
- 打开面板时：检查 `toggles[lastPageIndex].isOn`，若为 false 则设为 true
- 关闭面板时：保留 Toggle 选中状态

### ESC 键特殊处理
- 已打开任意页面 → 打开 Settings 页（不关闭面板）
- 未打开任何页面 → 打开 Settings 面板

### 双击逻辑
- 鼠标点击 Top 的 Toggle = 按快捷键
- 通过 Toggle 状态检测实现"同键双击关闭"

## 初始状态

- 游戏开始时：UI 根物体（名为"UI"）启用/激活
- PackagePanel：默认不激活，运行时按键才激活

## 工具栏系统

### 组件职责
| 组件 | 职责 |
|------|------|
| ToolbarUI | 工具栏容器，管理所有槽位 |
| ToolbarSlotUI | 单个槽位 UI，处理点击事件 |
| HotbarSelectionService | 管理选中索引，触发装备 |
| InventoryService | 背包数据管理 |
| GameInputManager | 输入检测，触发工具使用 |

### 工具选中流程
```
用户点击工具栏槽位
    ↓
ToolbarSlotUI.OnPointerClick()
    ↓
HotbarSelectionService.SelectIndex(index)
    ↓
EquipCurrentTool()
    ↓
playerToolController.EquipToolData(toolData, quality)
```

### 工具使用流程
```
用户左键点击世界空间
    ↓
GameInputManager.HandleUseCurrentTool()
    ↓
检测 IsPointerOverGameObject() → 如果在UI上则返回
    ↓
playerInteraction.RequestAction(action)
    ↓
animController.PlayAnimation(action, direction, flip)
```

### 工具类型到动画映射
```csharp
ToolType.Axe → AnimState.Slice        // 斧头 → 挥砍
ToolType.Sickle → AnimState.Slice     // 镰刀 → 挥砍
ToolType.Pickaxe → AnimState.Crush    // 镐子 → 挖掘
ToolType.Hoe → AnimState.Crush        // 锄头 → 挖掘（与镐子共用）
ToolType.WateringCan → AnimState.Watering  // 洒水壶 → 浇水
ToolType.FishingRod → AnimState.Fish  // 鱼竿 → 钓鱼
// 注意：Pierce (刺出) 用于长剑等武器，不是工具
```

## 编辑器交互规范

> **Tag/Layer 多选下拉框规范已迁移至 `rules.md` 的"编辑器交互规范"章节，因为这是全局规则，适用于所有系统（导航、建造、拾取等），不仅限于 UI。**
