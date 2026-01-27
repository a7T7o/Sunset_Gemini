# 背包交互系统 V3 设计文档

## 版本信息

- **版本**: v3.0
- **日期**: 2026-01-06
- **状态**: 设计中

---

## 🔴 V2 失败原因分析

### 核心错误

V2 方案在 `InventoryInteractionManager.Update()` 中使用 `EventSystem.RaycastAll()` 检测槽位，这是**完全错误的方案**。

### 问题根源

1. **Toggle 组件会"吞噬"点击事件**
   - Unity 的 EventSystem 已经在处理 Toggle 的点击
   - Toggle 设置为 `RaycastTarget=true` 会拦截射线

2. **两套系统打架（竞争条件）**
   - Manager 在 Update 里用 RaycastAll 检测
   - Unity 的 EventSystem 也在处理同样的输入
   - 结果：Raycast 命中 0 个对象，或命中 Toggle 的子对象

3. **重新发明轮子**
   - Unity 已经提供了完善的事件接口
   - 手动 Raycast 是"用手动挡开自动挡的车"

### 为什么之前能用？

原来的背包能用是因为：
- 只有简单的 `IPointerClickHandler`
- Toggle 组件正常工作
- 没有额外的 Update + Raycast 干扰

---

## V3 设计方案

### 核心原则

1. **使用 Unity 原生事件接口** - 不再使用 Update + Raycast
2. **SlotUI 作为传感器** - 实现接口，转发事件给 Manager
3. **Manager 作为大脑** - 只接收信号执行逻辑，不检测输入
4. **保留原有显示逻辑** - `Refresh()` 等方法完全不动

### 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                     Unity EventSystem                        │
│  (自动处理射线检测、层级遮挡、事件分发)                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    InventorySlotUI (传感器)                  │
│  实现接口：                                                   │
│  - IPointerDownHandler   → OnSlotPointerDown()              │
│  - IPointerUpHandler     → OnSlotPointerUp()                │
│  - IBeginDragHandler     → OnSlotBeginDrag()                │
│  - IDragHandler          → OnSlotDrag()                     │
│  - IEndDragHandler       → OnSlotEndDrag()                  │
│  - IDropHandler          → OnSlotDrop()                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              InventoryInteractionManager (大脑)              │
│  公共方法（供 SlotUI 调用）：                                 │
│  - OnSlotPointerDown(index, isEquip)                        │
│  - OnSlotPointerUp(index, isEquip)                          │
│  - OnSlotBeginDrag(index, isEquip, eventData)               │
│  - OnSlotDrag(eventData)                                    │
│  - OnSlotEndDrag(eventData)                                 │
│  - OnSlotDrop(index, isEquip)                               │
│                                                              │
│  内部状态管理：                                               │
│  - 状态机 (None/Dragging/HeldByShift/HeldByCtrl)            │
│  - 修饰键检测 (Shift/Ctrl)                                   │
│  - 计时器 (拖拽阈值、Ctrl长按)                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    数据层 (完全不动)                          │
│  - InventoryService                                          │
│  - EquipmentService                                          │
│  - ItemDatabase                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 组件设计

### 1. InventorySlotUI 改造

**需要实现的接口**：

```csharp
public class InventorySlotUI : MonoBehaviour, 
    IPointerDownHandler,   // 按下：通知 Manager 准备开始
    IPointerUpHandler,     // 抬起：通知 Manager 结束
    IBeginDragHandler,     // 开始拖拽：通知 Manager 进入拖拽模式
    IDragHandler,          // 拖拽中：通知 Manager 更新图标位置
    IEndDragHandler,       // 结束拖拽：通知 Manager 放置物品
    IDropHandler           // 接收掉落：处理物品被拖到这个格子上
{
    // 原有的 Refresh() 等方法完全不动
}
```

**事件转发逻辑**：

```csharp
public void OnPointerDown(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;
    var mgr = InventoryInteractionManager.Instance;
    if (mgr != null) mgr.OnSlotPointerDown(index, isEquipment: false);
}

public void OnPointerUp(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;
    var mgr = InventoryInteractionManager.Instance;
    if (mgr != null) mgr.OnSlotPointerUp(index, isEquipment: false);
}

public void OnBeginDrag(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;
    var mgr = InventoryInteractionManager.Instance;
    if (mgr != null) mgr.OnSlotBeginDrag(index, isEquipment: false, eventData);
}

public void OnDrag(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;
    var mgr = InventoryInteractionManager.Instance;
    if (mgr != null) mgr.OnSlotDrag(eventData);
}

public void OnEndDrag(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;
    var mgr = InventoryInteractionManager.Instance;
    if (mgr != null) mgr.OnSlotEndDrag(eventData);
}

public void OnDrop(PointerEventData eventData)
{
    var mgr = InventoryInteractionManager.Instance;
    if (mgr != null) mgr.OnSlotDrop(index, isEquipment: false);
}
```

### 2. InventoryInteractionManager 改造

**删除的内容**：
- ❌ `Update()` 方法中的所有 Raycast 检测
- ❌ `GetSlotUnderMouse()` 方法
- ❌ `raycastResults` 缓存

**保留的内容**：
- ✅ 状态机 (State 枚举)
- ✅ 物品操作逻辑 (拖拽、Shift二分、Ctrl单拿)
- ✅ 数据层交互 (GetStack/SetStack)
- ✅ HeldItemDisplay 控制

**新增的公共方法**：

```csharp
// 供 SlotUI 调用的公共方法
public void OnSlotPointerDown(int index, bool isEquip);
public void OnSlotPointerUp(int index, bool isEquip);
public void OnSlotBeginDrag(int index, bool isEquip, PointerEventData eventData);
public void OnSlotDrag(PointerEventData eventData);
public void OnSlotEndDrag(PointerEventData eventData);
public void OnSlotDrop(int index, bool isEquip);
```

### 3. EquipmentSlotUI 改造

与 `InventorySlotUI` 相同，实现相同的接口，转发事件时 `isEquip = true`。

---

## 交互流程

### 场景 1：普通点击（无修饰键）

```
用户点击槽位
    ↓
Unity 触发 OnPointerDown
    ↓
SlotUI 调用 Manager.OnSlotPointerDown(index, false)
    ↓
Manager 记录 pressTime、pressIndex
    ↓
用户松开鼠标
    ↓
Unity 触发 OnPointerUp
    ↓
SlotUI 调用 Manager.OnSlotPointerUp(index, false)
    ↓
Manager 检查：时间短 + 无移动 = 普通点击
    ↓
不做任何操作（只是选中）
```

### 场景 2：拖拽交换

```
用户按下并移动
    ↓
Unity 触发 OnPointerDown → Manager 记录开始
    ↓
Unity 检测到移动，触发 OnBeginDrag
    ↓
SlotUI 调用 Manager.OnSlotBeginDrag(index, false, eventData)
    ↓
Manager 检查时间阈值（0.15s），进入 Dragging 状态
    ↓
显示 HeldItemDisplay，清空源槽位
    ↓
Unity 持续触发 OnDrag
    ↓
SlotUI 调用 Manager.OnSlotDrag(eventData)
    ↓
Manager 更新 HeldItemDisplay 位置
    ↓
用户松开鼠标
    ↓
Unity 触发 OnEndDrag
    ↓
SlotUI 调用 Manager.OnSlotEndDrag(eventData)
    ↓
如果目标槽位有 OnDrop 被触发 → 执行交换
如果在面板外 → 丢弃物品
如果在面板内但无槽位 → 返回原位
```

### 场景 3：Shift+左键 二分拿取

```
用户按住 Shift，点击槽位
    ↓
Unity 触发 OnPointerDown
    ↓
SlotUI 调用 Manager.OnSlotPointerDown(index, false)
    ↓
Manager 检测到 Shift 按下，执行 ShiftPickup()
    ↓
拿起一半，显示 HeldItemDisplay
    ↓
状态变为 HeldByShift
    ↓
用户点击空槽位
    ↓
Unity 触发 OnPointerDown
    ↓
Manager 检测到 HeldByShift 状态，执行 TryPlace()
```

### 场景 4：Ctrl+左键 单个拿取（长按持续）

```
用户按住 Ctrl，按下槽位
    ↓
Unity 触发 OnPointerDown
    ↓
SlotUI 调用 Manager.OnSlotPointerDown(index, false)
    ↓
Manager 检测到 Ctrl 按下，执行 CtrlPickup()
    ↓
拿起 1 个，显示 HeldItemDisplay
    ↓
状态变为 HeldByCtrl，启动协程
    ↓
协程每 0.28 秒（3.5个/秒）执行 ContinueCtrlPickup()
    ↓
用户松开鼠标或 Ctrl
    ↓
Unity 触发 OnPointerUp
    ↓
Manager 停止协程，保持 HeldByCtrl 状态
```

---

## 与 Toggle 的和谐共存

### 问题

Toggle 组件会在 `OnPointerClick` 时切换选中状态，可能与我们的交互逻辑冲突。

### 解决方案

1. **使用 OnPointerDown/Up 而非 OnPointerClick**
   - 我们的逻辑在 Down/Up 中处理
   - Toggle 的 Click 事件不影响我们

2. **拖拽时 Toggle 不会触发**
   - Unity 的 Drag 系统会自动取消 Click 事件
   - 拖拽开始后，Toggle 不会切换状态

3. **保留 Toggle 的选中功能**
   - Toggle 仍然可以显示选中高亮
   - 我们不干扰 Toggle 的视觉反馈

---

## 实施步骤

### 第一步：清理战场

1. 删除 `InventoryInteractionManager.Update()` 中的所有 Raycast 代码
2. 删除 `GetSlotUnderMouse()` 方法
3. 保留状态机和物品操作逻辑

### 第二步：验证基础连接

1. 让 `InventorySlotUI` 实现 `IPointerClickHandler`
2. 在 `OnPointerClick` 中打印日志
3. 运行游戏，确认点击能触发日志

### 第三步：实现完整接口

1. `InventorySlotUI` 实现所有 6 个接口
2. 每个接口方法转发给 Manager
3. `EquipmentSlotUI` 同样处理

### 第四步：实现 Manager 逻辑

1. 添加公共方法供 SlotUI 调用
2. 在公共方法中实现状态机逻辑
3. 处理修饰键检测和计时器

### 第五步：测试验证

1. 普通点击（无反应，只选中）
2. 拖拽交换
3. Shift+左键 二分拿取
4. Ctrl+左键 单个拿取
5. 面板外丢弃
6. ESC 取消

---

## 正确性属性

| 属性 | 描述 |
|------|------|
| CP1 | 点击槽位能触发 OnPointerDown/Up |
| CP2 | 拖拽能触发 OnBeginDrag/OnDrag/OnEndDrag |
| CP3 | 放置能触发 OnDrop |
| CP4 | Toggle 选中状态不受影响 |
| CP5 | 原有的 Refresh() 显示逻辑正常工作 |
| CP6 | Shift+左键 能二分拿取 |
| CP7 | Ctrl+左键 能单个拿取 |
| CP8 | 拖拽交换正常工作 |
| CP9 | 面板外松开能丢弃物品 |
| CP10 | ESC 能取消操作 |

---

## 与 V2 的对比

| 方面 | V2 (错误) | V3 (正确) |
|------|-----------|-----------|
| 输入检测 | Update + Raycast | Unity 原生接口 |
| 槽位识别 | 手动射线检测 | 接口自动传递 |
| 与 Toggle 关系 | 冲突 | 和谐共存 |
| 代码复杂度 | 高（重新发明轮子） | 低（利用现有系统） |
| 稳定性 | 不稳定（竞争条件） | 稳定（单一事件流） |

---

## 参考资料

- 客观评价文档：`.kiro/specs/UI系统/0_背包交互系统升级/客观评价.md`
- 错误反省文档：`.kiro/specs/UI系统/0_背包交互系统升级/错误反省.md`
- ToolbarSlotUI 参考：`Assets/YYY_Scripts/UI/Toolbar/ToolbarSlotUI.cs`
