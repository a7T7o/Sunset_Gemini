# UI设计指导

版本: v1.0
日期: 2025-11-17
作者: Cascade

---

## 1. 规范与基本约定

- 画布：`Canvas (Screen Space - Overlay)`，参考分辨率 1920x1080，`Canvas Scaler` 设为 `Scale With Screen Size`，`Match = 0.5`。
- 美术：沿用像素 UI 风格；保持 4/8 像素栅格对齐；所有可交互控件使用 `Button` + `Image`。
- 字体：当前可以使用内置 `Text`；若后续迁移 TMP，仅替换控件类型，事件/刷新逻辑不变。
- 命名：统一英文路径与 PascalCase 组件名；UI 脚本以 `*UI`/`*View` 结尾；常驻 HUD/Toolbar 与 PackagePanel 分开预制。

---

## 2. 预制体与目录

- `Assets/Prefabs/UI/HUD/TimeMoneyHUD.prefab`
- `Assets/Prefabs/UI/HUD/StatsHUD.prefab`
- `Assets/Prefabs/UI/Toolbar/Toolbar.prefab`
- `Assets/Prefabs/UI/Package/PackagePanel.prefab`
- `Assets/Prefabs/UI/Package/InventorySlotUI.prefab`
- `Assets/Prefabs/UI/Package/RecipeItemUI.prefab`
- `Assets/Prefabs/UI/Common/Tooltip.prefab`
- `Assets/Prefabs/UI/Common/DragGhost.prefab`

制作原则：
- 大面板（PackagePanel）首开实例化，后续只显隐。
- 列表项（Slot/Recipe/NPC）使用对象池；以 `LayoutGroup + ContentSizeFitter` 组合，滚动用 `ScrollRect`。
- 运行时脚本仅通过服务/事件拉取数据刷新 UI，不直接依赖 SO。

---

## 3. 背包/格子/拖拽（核心交互）

### 3.1 InventorySlotUI（组件结构）
- 子节点：`BG(Image)`、`Icon(Image)`、`Amount(Text)`、`QualityStars(Image[])`、`Highlight(Image)`、`Button`。
- 必要接口：
  - `SetEmpty()`、`SetStack(itemId, quality, amount)`、`Refresh()`。
  - `Bind(index, InventoryService service)` 只保存索引与服务；不保存具体数据副本。
- 事件接口：`OnPointerEnter/Exit` 显示 Tooltip；`IBeginDrag/IDrag/IEndDrag` 实现拖拽；`IPointerClick` 左右键行为（合并/交换/拆分半栈）。
- 刷新策略：
  - 默认全量 `RefreshAll()`；
  - 性能优化：订阅 `SlotChanged(index)` 时仅刷新受影响 Slot。

### 3.2 拖拽与垃圾桶
- 拖拽开始：生成 `DragGhost`（跟随鼠标，`raycastTarget=false`）。
- 放下：
  - 放在另一个 Slot：调用 `InventoryService.SwapOrMerge(from, to)`；
  - 放在 ToolBar：调用 `HotbarService.Bind(hotbarIndex, inventoryIndex)`；
  - 放在垃圾桶：弹出确认 `Confirm("将丢弃 XxN? ")` → `InventoryService.Remove(index, amount)`。
- 右键拆分：`SplitHalf(from)` 放到光标临时手持缓存，再次点击落入目标。

---

## 4. 工具栏（Toolbar）

- 结构：8 格 `ToolbarSlotUI`，每格可绑定背包索引；高亮显示当前选中。
- 交互：
  - 数字键 1-8 切换选中槽 → `EquipmentService.EquipFromHotbar(selectedIndex)`。
  - 拖动背包物品到某格建立绑定；再次拖动覆盖原绑定。
- 刷新：
  - `HotbarChanged` 或 `SlotChanged(boundIndex)` 引发对应格刷新图标与数量。

---

## 5. HUD（时间/金钱、HP/精力）

- TimeMoneyHUD：
  - 文本/图标显示 当前时间 HH:MM、当天/季节、金币数量；
  - 订阅 `TimeService.TimeTick` 与 `CurrencyService.GoldChanged`。
- StatsHUD：
  - 两条 `Image.fillAmount` 展示 HP 与 Stamina；
  - 鼠标悬浮 Tooltip：显示精确数值与“最近动作消耗/阈值提示”；
  - 订阅 `StatsService.StatsChanged`；低于 20% 闪烁/变色。

---

## 6. 详情/Tooltip

- Tooltip 内容：
  - 名称、描述；
  - 品质星（颜色：铜/银/金/紫）；
  - 售价计算（`QualityHelper.CalculatePriceWithQuality`）；
  - 使用/装备类型（来自 ItemData/ToolData）。
- 出现时机：指针进入 Slot/Toolbar；延时 0.15s；离开或点击关闭。
- 位置：跟随鼠标，屏幕边界内偏移。

---

## 7. 性能与稳定性

- 对象池：Slot/Recipe/NPC 项，避免频繁 Instantiate/Destroy。
- 刷新节流：大批量变更时使用 `BeginBatch()`/`EndBatch()` 合并到一次 `RefreshAll()`。
- 图集与 Sprite：尽量打包在同一图集，减少 UI 材质批次。
- 事件安全：UI 只读服务状态，不在 UI 回调里做逻辑循环修改（防止 re-entrancy）。

---

## 8. 与动画/装备联动（关键细节）

- 装备逻辑：
  - Hotbar 选择/点击使用工具 → `EquipmentService.Equip(itemId, quality)`；
  - 在内部：查询 `ItemDatabase` 得到 `ToolData`，`animKeyId = toolData.GetAnimationKeyId()`；
  - 写入 Animator：`ToolItemId = animKeyId`、`ToolQuality = quality`；
  - `LayerAnimSync` 负责状态名组装、`HasState` 检查与品质回退。
- 物品品质显示：
  - UI 仅显示星标与颜色；不改变图标。

---

## 9. 输入与可访问性

- 鼠标：点击/拖拽/右键拆分。
- 键盘：`I/Tab` 打开/关闭 PackagePanel；数字键 1-8 选择 ToolBar 槽；`Esc` 关闭当前面板。
- 手柄（预留）：将 Button 组装入 `EventSystem` + `Selectable` 链，便于后续导航。

---

## 10. 开发顺序（建议）

1) Toolbar 与 HUD（小而闭环，先验证服务事件与装备链路）
2) 背包道具页（网格、拖拽、拆分、Tooltip）
3) 配方/制作页（高亮/制作流程）
4) NPC/地图页（占位 UI + 数据模型草案）
5) 设置页（音量/键位占位 + 保存）

---

## 11. UI 事件到服务调用映射（拔高）

- Slot 点击/拖拽 → `InventoryService.*`
- Toolbar 选择 → `EquipmentService.EquipFromHotbar`
- 制作按钮 → `CraftService.TryCraft`
- HUD 显示 → 订阅 `TimeService/StatsService/CurrencyService`
- 保存按钮 → `SaveService.Save`

---

## 12. 验收（UI 交互维度）

- 背包与 Toolbar 联动：拖拽绑定、数字键切换装备、装备后动画状态正确且有品质回退。
- 悬浮提示：显示品质星与售价信息；
- 体力条：悬浮显示详细说明；体力不足时动画提示；
- 面板：打开/关闭不抖动、不泄漏对象；Profiler 中 GC 峰值低。

以上指导与《UI系统与总系统设计规划》配套，落地时以服务/事件为中枢，保障 UI 与物品/动画/存档的全盘联动。
