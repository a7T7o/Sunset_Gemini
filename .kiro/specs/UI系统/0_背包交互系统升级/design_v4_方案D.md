# 背包交互系统 V4 - 方案 D 设计文档

## 版本信息

- **版本**: v4.0 (方案 D)
- **日期**: 2026-01-06
- **状态**: 设计中

---

## 核心问题回顾

### V3 方案的致命缺陷

当 `InventorySlotUI` 同时实现 `IPointerDownHandler` 和 `IBeginDragHandler` 时：

1. **Unity EventSystem 的事件处理顺序**:
   ```
   用户按下鼠标
       ↓
   OnPointerDown（立即触发，但被拖拽系统"劫持"）
       ↓
   等待判断是点击还是拖拽
       ↓
   如果移动 → OnBeginDrag → OnDrag → OnEndDrag
   如果不移动 → OnPointerUp → OnPointerClick
   ```

2. **问题**:
   - `OnPointerDown` 和 `OnPointerUp` 会被延迟或跳过
   - 无法在按下时检测修饰键（Shift/Ctrl）
   - 无法实现长按持续拿取

---

## 方案 D：分离组件架构

### 核心思路

**职责分离 + 状态驱动的接口管理**

1. **InventorySlotUI** - 只负责显示（完全不动）
2. **InventorySlotInteraction** - 只负责交互（新组件）
3. **InventoryInteractionManager** - 只负责逻辑（大脑）

### 架构图

```
Unity EventSystem
    ↓
InventorySlotInteraction (传感器 + 状态控制)
    ↓
InventoryInteractionManager (大脑)
    ↓
数据层 (InventoryService / EquipmentService)
    ↓
InventorySlotUI (显示) ← 监听数据变化自动刷新
```

---

## 核心设计：状态驱动的接口管理

### 问题的本质

Unity EventSystem 的接口冲突是因为**同时启用了点击和拖拽接口**。

### 解决方案

**根据当前状态动态控制接口行为**：

1. **默认状态**：只响应点击，忽略拖拽
2. **检测到长按**：启用拖拽响应
3. **拖拽结束**：恢复默认状态

### 实现方式

```csharp
public class InventorySlotInteraction : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    // 状态标志
    private bool isDragEnabled = false;
    private float pressTime;
    private Vector2 pressPosition;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        // 记录按下状态
        pressTime = Time.time;
        pressPosition = eventData.position;
        isDragEnabled = false;
        
        // 转发给 Manager（处理 Shift/Ctrl 逻辑）
        InventoryInteractionManager.Instance?.OnSlotPointerDown(slotUI.Index, isEquip);
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        // 检查是否满足拖拽条件
        float holdTime = Time.time - pressTime;
        float moveDistance = Vector2.Distance(eventData.position, pressPosition);
        
        if (holdTime >= 0.15f || moveDistance >= 5f)
        {
            // 启用拖拽
            isDragEnabled = true;
            InventoryInteractionManager.Instance?.OnSlotBeginDrag(slotUI.Index, isEquip, eventData);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        // 只有启用拖拽时才响应
        if (!isDragEnabled) return;
        InventoryInteractionManager.Instance?.OnSlotDrag(eventData);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        // 只有启用拖拽时才响应
        if (!isDragEnabled) return;
        InventoryInteractionManager.Instance?.OnSlotEndDrag(eventData);
        isDragEnabled = false;
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        // 如果没有启用拖拽，说明是点击
        if (!isDragEnabled)
        {
            InventoryInteractionManager.Instance?.OnSlotPointerUp(slotUI.Index, isEquip);
        }
        
        isDragEnabled = false;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        InventoryInteractionManager.Instance?.OnSlotDrop(slotUI.Index, isEquip);
    }
}
```

---

## 组件职责详解

### 1. InventorySlotUI（显示层）

**职责**：
- 显示物品图标和数量
- 监听数据变化自动刷新
- **不处理任何交互**

**保持不变**：
- `Bind()` - 绑定数据源
- `Refresh()` - 刷新显示
- `OnSlotChanged()` - 监听数据变化

**新增**：
- `public int Index => index;` - 供 Interaction 组件查询

### 2. InventorySlotInteraction（交互层）

**职责**：
- 实现 Unity 原生事件接口
- 状态驱动的接口管理
- 转发事件给 Manager

**核心字段**：
```csharp
private InventorySlotUI slotUI;      // 关联的 SlotUI
private bool isEquip;                 // 是否为装备槽
private bool isDragEnabled;           // 拖拽是否启用
private float pressTime;              // 按下时间
private Vector2 pressPosition;        // 按下位置
```

**核心方法**：
- `Bind(InventorySlotUI slotUI, bool isEquip)` - 绑定 SlotUI
- `OnPointerDown/Up` - 处理点击
- `OnBeginDrag/Drag/EndDrag` - 处理拖拽（状态控制）
- `OnDrop` - 处理放置

### 3. InventoryInteractionManager（逻辑层）

**职责**：
- 管理物品拿取状态
- 处理 Shift/Ctrl 逻辑
- 处理拖拽交换逻辑
- 处理快速装备逻辑

**保持不变**：
- 状态机枚举（None/Dragging/HeldByShift/HeldByCtrl）
- 所有业务逻辑方法
- 数据层交互

**修改**：
- 不再依赖 SlotUI 实现接口
- 只接收 Interaction 组件转发的事件

---

## 状态流转图

### 点击流程（Shift/Ctrl）

```
用户按下 Shift/Ctrl + 左键
    ↓
OnPointerDown → Manager.OnSlotPointerDown
    ↓
Manager 检测修饰键，拿起物品
    ↓
OnPointerUp → Manager.OnSlotPointerUp
    ↓
Manager 放置物品
```

### 拖拽流程

```
用户按下左键
    ↓
OnPointerDown → 记录状态
    ↓
用户移动鼠标（或长按 0.15 秒）
    ↓
OnBeginDrag → 检查条件 → 启用拖拽
    ↓
OnDrag → 跟随鼠标
    ↓
OnEndDrag → Manager 处理放置
    ↓
恢复默认状态
```

---

## 关键优势

### 1. 完全避免接口冲突

- 通过 `isDragEnabled` 标志控制接口行为
- 点击和拖拽不会互相干扰

### 2. 保留所有功能

- ✅ Shift+左键 二分拿取
- ✅ Ctrl+左键 单个拿取（长按持续）
- ✅ 拖拽交换
- ✅ 快速装备
- ✅ 所有修饰键检测

### 3. 职责清晰

- **InventorySlotUI** - 只管显示
- **InventorySlotInteraction** - 只管交互
- **InventoryInteractionManager** - 只管逻辑

### 4. 易于调试

- 可以单独禁用 Interaction 组件测试显示
- 可以单独测试 Manager 的逻辑
- 状态标志清晰可见

### 5. 易于扩展

- 新增交互方式只需修改 Interaction 组件
- 新增业务逻辑只需修改 Manager
- 显示逻辑完全独立

---

## 实施步骤

### 阶段 1：创建 Interaction 组件

1. 创建 `InventorySlotInteraction.cs`
2. 实现 6 个 Unity 原生接口
3. 实现状态驱动的接口管理
4. 添加 `Bind()` 方法关联 SlotUI

### 阶段 2：创建 Manager

1. 创建 `InventoryInteractionManager.cs`
2. 实现单例模式
3. 实现公共方法（供 Interaction 调用）
4. 实现所有业务逻辑

### 阶段 3：集成到 SlotUI

1. 在 `InventorySlotUI.Awake()` 中自动添加 Interaction 组件
2. 调用 `Interaction.Bind(this, false)`
3. 同样处理 `EquipmentSlotUI`

### 阶段 4：测试验证

1. 测试点击功能（Shift/Ctrl）
2. 测试拖拽功能
3. 测试快速装备
4. 测试所有边界情况

---

## 配置参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| dragTimeThreshold | 0.15f | 拖拽触发时间阈值（秒） |
| dragMoveThreshold | 5f | 拖拽触发移动阈值（像素） |
| ctrlPickupRate | 3.5f | Ctrl 长按拿取速度（个/秒） |
| dropCooldown | 5f | 丢弃冷却时间（秒） |

---

## 与 V3 方案的对比

| 特性 | V3 方案 | V4 方案 D |
|------|---------|-----------|
| 接口冲突 | ❌ 存在 | ✅ 完全避免 |
| 点击检测 | ❌ 被劫持 | ✅ 正常工作 |
| 修饰键检测 | ❌ 无法检测 | ✅ 正常工作 |
| 拖拽功能 | ✅ 正常 | ✅ 正常 |
| 职责分离 | ⚠️ 部分 | ✅ 完全分离 |
| 易于调试 | ⚠️ 一般 | ✅ 非常清晰 |
| 代码复杂度 | ⚠️ 中等 | ✅ 简单清晰 |

---

## 潜在问题和解决方案

### 问题 1：组件依赖管理

**问题**：Interaction 组件需要引用 SlotUI

**解决方案**：
- 在 SlotUI.Awake() 中自动添加 Interaction 组件
- 调用 `Interaction.Bind(this, isEquip)` 建立关联

### 问题 2：拖拽阈值调整

**问题**：不同用户对拖拽触发的敏感度不同

**解决方案**：
- 在 Manager 中暴露配置参数
- 可以在 Inspector 中调整

### 问题 3：性能开销

**问题**：每个槽位都有一个 Interaction 组件

**解决方案**：
- Interaction 组件非常轻量（只有几个字段）
- 只在需要时启用拖拽响应
- 性能影响可以忽略不计

---

## 下一步

1. 用户确认方案 D 设计
2. 创建 `tasks_v4.md` 任务列表
3. 按任务列表逐步实施
4. 在 Unity 中测试验证
5. 更新验收指南
