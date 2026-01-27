# UI系统与总系统设计规划

版本: v1.0
日期: 2025-11-17
作者: Cascade（基于 Docx/HD 与 Docx/Plan 全部文档与现状界面）

---

## 1. 背景与现状

- 你已完成 Canvas 的初步结构：`UI/PackagePanel/Main/{0_Props, 1_Recipes, 2_Ex, 3_Map, 4_Relationship_NPC, 5_Settings}` 与常驻 `ToolBar`。
- 目前元素大多是 Image 或带 Button 的 Image，没有使用 TextMeshPro；背包格与 ToolBar 的交互逻辑未实现。
- 顶部左侧的时间/日历/金钱 HUD、右下角的精力/血量条尚未加入；NPC/商店交互 UI 未设计。
- 代码侧已有：完整 Item SO 体系与 `ItemDatabase`、品质系统、`ToolData.GetAnimationKeyId()` 与动画命名/回退规范、`LayerAnimSync` 运行时帧锁同步、TriDirectional 生成器规划与控制器契约。

本规划目标：在不改变已验证的动画/物品工具链前提下，设计一套可落地、低耦合、可扩展的 UI+背包+装备+保存 的整体方案，确保与运行时动画同步、品质回退、ID 映射、制作/配方等系统无缝联动。

---

## 2. 设计目标

- 数据驱动：UI 只展示和操作“运行时数据模型”，不直接操作 SO。
- 事件联动：背包、装备、货币、时间、体力/血量变更以事件广播，UI 被动刷新（最小重绘）。
- 一致命名：动画状态名严格遵循 `{ActionType}_{Direction}_Clip_{itemId}_{quality}`，`itemId` 取 `ToolData.GetAnimationKeyId()`（或家族ID），`quality` 支持缺档回退到 0。
- 生命周期清晰：HUD 常驻；大型面板（背包/制作/地图……）按需实例化，首次创建后缓存，再次开关仅 SetActive；格子与行项目使用对象池。
- 易测与可保存：统一 SaveData（JSON）结构，支持背包/工具栏/装备/时间/货币/体力血量等关键状态落盘与复原。

---

## 3. 总体架构（三层）

- 数据层（Data）
  - ScriptableObject：`ItemData` 派生族、`RecipeData`、`ItemDatabase`。
  - 运行时模型：`ItemStack{ itemId:int, quality:int, amount:int }`、`InventorySlot{ stack?, locked }`、`Hotbar[8]`、`PlayerStats{ hp, stamina, maxHp, maxStamina }`、`WorldTime{ day, season, timeOfDay }`、`Wallet{ gold }`。
- 域服务层（Domain/Services）
  - `InventoryService`（添加/移除/交换/分堆/查询空位/批量变更事件）。
  - `EquipmentService`（装备/卸下；从 Hotbar 当前槽绑定；发出 `OnEquipped(itemId, quality)`）。
  - `CraftService`（读取 `ItemDatabase` 的 `RecipeData` 判定可制作/消耗材料/产出）。
  - `TimeService`、`StatsService`（HP/精力）、`CurrencyService`、`SaveService`（JSON）。
  - 事件总线（或 C# 事件聚合器）：`InventoryChanged`、`SlotChanged(index)`、`HotbarChanged`、`EquippedChanged`、`StatsChanged`、`TimeTick`、`GoldChanged`。
- 表现层（UI）
  - HUD（常驻）：时间/季节/金钱、HP/精力、系统提示。
  - 面板（按需）：背包（含分栏 Tab：道具/配方/制作/地图/NPC/设置）、商店、对话、提示框。
  - 工具栏（常驻）：8 格，选中高亮，与装备系统联动。

---

## 4. 关键联动

- 背包 ↔ 物品数据库
  - UI 展示 `InventoryService` 的 `InventorySlot[]`；栈显示来自 `ItemDatabase.GetItemByID(stack.itemId)` 的图标/名称；品质通过品质系统渲染星标与价格倍率。
- 装备 ↔ 动画同步
  - `EquipmentService` 确认当前装备（itemId, quality）。
  - `PlayerToolController` 在 `OnEquip` 时：
    - 读取 `ItemDatabase` 对应 `ToolData`，调用 `GetAnimationKeyId()` 取得动画用 `itemId`
    - 将 `ToolItemId = animKeyId` 与 `ToolQuality = quality` 写入 Tool Animator；`LayerAnimSync` 负责帧锁、方向、品质回退。
- 制作/配方 ↔ 背包
  - `CraftService` 订阅 `InventoryChanged` 以刷新可制作条目；制作按钮执行检查→扣除材料→添加产物→事件回流刷新。
- 体力/血量 ↔ 动作逻辑
  - `StatsService` 提供 `TryUseEnergy(cost)` 与 `Heal/TakeDamage`；UI 订阅 `StatsChanged` 更新条形与数值。
- 时间/季节 ↔ HUD/作物/刷新
  - `TimeService` 发出 `TimeTick`、`DayChanged` 等；HUD 即时更新；（农业系统未来可订阅）。
- 保存/读取 ↔ 全系统
  - `SaveService.Save()` 捕获各域服务当前状态合成为 `SaveData`；`Load()` 将状态回灌并触发事件，UI 随之刷新。

---

## 5. 组件与数据契约（最小版）

- InventoryChanged（事件载荷）
  - `changedSlots: int[]`（可空，空=全量刷新）
- EquipChanged
  - `itemId:int`（数据库ID）`quality:int` `animKeyId:int`（供调试日志）
- StatsChanged
  - `hp, maxHp, stamina, maxStamina`
- TimeTick
  - `day:int, season:int, hour:int, minute:int, gold:int`
- SaveData（JSON）
  - `inventory:{ slots:[{itemId,quality,amount}], capacity }`
  - `hotbar:{ slotIndex->inventoryIndex, selected }`
  - `player:{ hp, stamina, equipped:{ itemId, quality } }`
  - `world:{ day, season, timeOfDay }`
  - `wallet:{ gold }`

---

## 6. UI 面板设计（按需实例化+缓存）

- HUD（常驻）
  - `TimeMoneyHUD`：数字时间或图标表盘 + 金币数。
  - `StatsHUD`：HP/精力 `Image.fillAmount` + 细数（悬浮提示显示精确值与行动消耗说明）。
- PackagePanel（可见时激活）
  - 顶部 Tab 栏（Button/ToggleGroup）。
  - 子页：
    - 道具页（背包主网格 + 右侧详情 + 垃圾桶/排序按键）。
    - 配方页（左侧分类、右侧配方列表与产物预览、制作按钮）。
    - 制作页（工作台式布局，后续可与配方页合并）。
    - 地图页（当前区域小地图/标记）。
    - NPC 好感页（头像列表、进度条、礼物记录）。
    - 设置页（音量、分辨率、键位；先保留空壳）。
- ToolBar（常驻）
  - 8 格槽（Button + 高亮框），数字键 1-8 选中；选中槽变更触发 `EquipSelectedHotbar()` → `EquipmentService`。

生命周期与资源策略：
- Canvas/常驻 HUD/ToolBar 常驻。
- PackagePanel 首次打开时 `Instantiate(prefab)`，后续仅 `SetActive(true/false)`；内部网格的 Slot 使用对象池，避免频繁创建销毁。
- 大型滚动列表（配方/NPC）使用 `ScrollRect` + 复用项（Pool），每页 20-60 项即可流畅。

---

## 7. 交互规范（背包/工具栏/提示）

- Slot（单元格）交互
  - 左键：
    - 空手 → 拿起该栈；有手持 → 尝试合并（同 ID+品质）或交换。
  - 右键：
    - 拆分半栈到手上；若手持且同类 → 逐个放入。
  - 拖拽：`IBeginDrag/IDrag/IEndDrag` + 拖影图标；放到其他槽交换/合并；放到工具栏建立映射；放到垃圾桶弹确认。
  - 悬浮：显示 Tooltip（名称、描述、品质星、售价/消耗说明）。
- ToolBar
  - 数字键选择高亮；选中即 `EquipmentService` 尝试装备（若槽映射为空或槽物品不可装备则仅高亮）。
- 制作/配方
  - 条目高亮可制作（材料足够）；点击显示需求与产物；制作按钮调用 `CraftService.TryCraft(recipeId)`。
- HUD 提示
  - 体力不足时闪烁与音效；金钱变化飘字；时间整点提示（可选）。

---

## 8. 与动画系统的精确对接（已存在能力的应用）

- 命名：`{ActionType}_{Direction}_Clip_{itemId}_{quality}`，目录：`Assets/Animations/Clips/{ActionType}/{itemId}/{direction}/`。
- `ToolData` 字段：`useQualityIdMapping`、`animationDefaultId`、`GetAnimationKeyId()`；装备时用该 KeyId 写 Animator 的 `ToolItemId`。
- `LayerAnimSync`：
  - 运行时在 `LateUpdate` 镜像 `State/Direction`，以玩家帧号硬锁到工具帧；
  - 以 `ToolItemId`+`ToolQuality` 组装目标状态名并 `HasState` 检测，缺失回退 `quality=0`；
  - Tool SpriteRenderer 始终 `sortingOrder = player - 1`（下方）。

验收与回归：
- `t30` 菜单 `Farm → Items → 批量创建物品数据(SO)` 可见且可打开。
- `t31` 选 Sprite 试跑批量创建，ID/名称顺序正确，命名/保存路径符合规范。
- `t32` 运行时 `PlayerToolController` 设置的 `ToolItemId` 使用 `GetAnimationKeyId()`，`LayerAnimSync` 正常回退品质。

---

## 9. 保存/读取系统（最小可用方案）

- 存储格式：JSON（`Application.persistentDataPath`）；
- 触发：手动保存（设置页按钮）+ 场景切换自动保存；
- 安全：版本号 + 向后兼容（缺失字段使用默认值）；
- 过程：`SaveService.Gather()` 从各服务聚合 → `SaveData` → `JsonUtility.ToJson`；加载时逆过程并触发各服务事件以刷新 UI。

---

## 10. 目录与资源组织

- 预制体
  - `Assets/Prefabs/UI/HUD/TimeMoneyHUD.prefab`
  - `Assets/Prefabs/UI/HUD/StatsHUD.prefab`
  - `Assets/Prefabs/UI/Package/PackagePanel.prefab`
  - `Assets/Prefabs/UI/Package/InventorySlotUI.prefab`
  - `Assets/Prefabs/UI/Package/RecipeItemUI.prefab`
  - `Assets/Prefabs/UI/Common/Tooltip.prefab`
  - `Assets/Prefabs/UI/Common/DragGhost.prefab`
  - `Assets/Prefabs/UI/Toolbar/Toolbar.prefab`
- 脚本（建议路径）
  - `Assets/Scripts/UI/Core/{UIManager, Tooltip, DragGhost}.cs`
  - `Assets/Scripts/UI/Inventory/{InventoryUI, InventorySlotUI, HotbarUI}.cs`
  - `Assets/Scripts/Services/{InventoryService, EquipmentService, CraftService, TimeService, StatsService, CurrencyService, SaveService}.cs`

---

## 11. 实施里程碑（两周）

- 里程碑1（Day 1-2）基础数据与事件
  - 建立 `InventoryService/EquipmentService`、事件总线、`SaveService` 原型；
  - 将 `PlayerToolController` 接 `EquipmentService.EquippedChanged` 写 Animator 参数。
- 里程碑2（Day 3-4）HUD 与 ToolBar
  - 实现 `ToolBar`（热键1-8、选中高亮、与装备联动）；
  - 实现 `StatsHUD/TimeMoneyHUD` 与服务订阅。
- 里程碑3（Day 5-7）背包页与拖拽
  - `InventoryUI/SlotUI`（对象池+局部刷新）；
  - 拖拽/拆分/垃圾桶/排序；Tooltip。
- 里程碑4（Day 8-9）配方页
  - 可制作高亮、制作流程、与背包联动。
- 里程碑5（Day 10）保存/读取
  - 最小保存集成，切场景自动保存，读取回灌全系统。
- 里程碑6（Day 11-12）集成测试
  - t30~t32 验收 + UI 回归；动画同步、品质回退、工具在人物下方。

---

## 12. 风险与对策

- 事件风暴/刷新过度：以 `SlotChanged(index)` 为主，必要时批量；UI 按需刷新。
- ID 语义混淆：装备路径统一从 `ItemId → (animKeyId, quality)`；文档与 Inspector 明确“动画用ID=家族ID”。
- 资源抖动：大面板采用“首次实例化，后续显隐”，格子对象池避免 GC 峰值。
- 兼容文字系统：当前使用 `UnityEngine.UI.Text`，后续可逐步迁移 TMP（统一接口便于替换）。

---

## 13. 验收清单（UI 侧）

- 背包/工具栏：拖拽、拆分、合并、装备、垃圾桶删除确认。
- HUD：时间/金币/HP/精力实时刷新；体力不足有提示。
- 配方：材料满足高亮，可制作一键完成并更新背包。
- 保存/读取：重启后恢复背包、工具栏映射、装备、时间/金币/HP/精力。
- 动画联动：装备后工具动画立即与人物同步，无“GotoState not found”。

---

以上规划遵循你现有的动画/工具链约定（Docx/HD v3.2、三向生成器/控制器契约、品质回退、`GetAnimationKeyId()`），并在 UI/背包/存档上提供最小可行且可扩展的整体蓝图。后续按里程碑推进实现即可。
