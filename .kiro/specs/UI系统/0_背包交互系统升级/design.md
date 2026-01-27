# 设计文档 - 背包交互系统升级

## 概述

本设计文档描述背包 UI 交互系统的全面升级方案。系统需要支持三种独立的物品操作模式（拖拽、Shift 二分、Ctrl 单取），并与现有的背包、装备、掉落物系统无缝集成。

核心设计原则：
1. **状态机驱动**：使用明确的状态机管理物品的拿起/放置/拖拽状态
2. **模式互斥**：拖拽模式与 Shift/Ctrl 模式完全独立，互不干扰
3. **事件驱动**：通过事件系统解耦 UI 与数据层
4. **可配置性**：关键参数（如拖拽阈值、拿取速度）可在 Inspector 中配置

## 架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        PackagePanel                              │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                 InventoryDragDropManager                 │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │    │
│  │  │ DragHandler │  │ ShiftHandler│  │  CtrlHandler    │  │    │
│  │  │ (拖拽模式)  │  │ (二分模式)  │  │  (单取模式)     │  │    │
│  │  └─────────────┘  └─────────────┘  └─────────────────┘  │    │
│  │                         │                                │    │
│  │                    HeldItemDisplay                       │    │
│  │                   (跟随鼠标的物品显示)                    │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│  ┌───────────────────────────┼───────────────────────────────┐  │
│  │                           ▼                               │  │
│  │  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐   │  │
│  │  │InventorySlot│    │InventorySlot│    │EquipmentSlot│   │  │
│  │  │    UI       │    │    UI       │    │    UI       │   │  │
│  │  └─────────────┘    └─────────────┘    └─────────────┘   │  │
│  │         Up 区域 (36格)              │    Down 区域 (6格)  │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              │                                   │
│  ┌───────────────────────────┼───────────────────────────────┐  │
│  │                           ▼                               │  │
│  │  ┌─────────────┐    ┌─────────────┐                      │  │
│  │  │  BT_Sort    │    │BT_TrashCan  │                      │  │
│  │  │ (整理按钮)  │    │ (垃圾桶)    │                      │  │
│  │  └─────────────┘    └─────────────┘                      │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                        数据层                                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │InventoryService │  │EquipmentService │  │ WorldItemPool   │  │
│  │   (背包数据)    │  │   (装备数据)    │  │  (掉落物管理)   │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## 组件与接口

### 1. InventoryDragDropManager（核心管理器）

负责协调所有拖拽和拿取操作的中央管理器。

```csharp
public class InventoryDragDropManager : MonoBehaviour
{
    // 状态枚举
    public enum InteractionState
    {
        None,           // 无操作
        Dragging,       // 拖拽中（长按触发）
        HeldByShift,    // Shift 拿起状态
        HeldByCtrl      // Ctrl 拿起状态
    }
    
    // 当前状态
    public InteractionState CurrentState { get; private set; }
    
    // 拿起的物品信息
    public ItemStack HeldItem { get; private set; }
    public int SourceSlotIndex { get; private set; }
    public bool IsSourceEquipment { get; private set; }
    
    // 配置参数
    [SerializeField] private float dragThreshold = 0.15f;        // 拖拽触发阈值（秒）
    [SerializeField] private float ctrlPickupRate = 3.5f;        // Ctrl 长按拿取速度（个/秒）
    
    // 核心方法
    public void StartDrag(int slotIndex, bool isEquipment);
    public void StartShiftPickup(int slotIndex, bool isEquipment);
    public void StartCtrlPickup(int slotIndex, bool isEquipment);
    public void ContinueCtrlPickup();                            // 长按持续拿取
    public bool TryPlace(int targetSlotIndex, bool isEquipment);
    public void ReturnToSource();
    public void DropItem();
    public void TrashItem();
}
```

### 2. HeldItemDisplay（跟随鼠标显示）

显示被拿起/拖拽的物品，跟随鼠标移动。

```csharp
public class HeldItemDisplay : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text amountText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    public void Show(ItemStack item, ItemDatabase database);
    public void UpdateAmount(int amount);
    public void Hide();
    public void FollowMouse();  // 在 Update 中调用
}
```

### 3. InventorySlotUI（槽位 UI 扩展）

扩展现有的 InventorySlotUI，添加拖拽和拿取支持。

```csharp
public class InventorySlotUI : MonoBehaviour, 
    IPointerDownHandler, IPointerUpHandler, 
    IPointerClickHandler, IDragHandler, IDropHandler
{
    // 新增：拖拽相关
    private float pointerDownTime;
    private bool isDragCandidate;
    
    // 新增：与 Manager 交互
    private InventoryDragDropManager dragDropManager;
    
    // 接口实现
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData);
    void IPointerUpHandler.OnPointerUp(PointerEventData eventData);
    void IPointerClickHandler.OnPointerClick(PointerEventData eventData);
    void IDragHandler.OnDrag(PointerEventData eventData);
    void IDropHandler.OnDrop(PointerEventData eventData);
}
```

### 4. InventorySortService（整理服务）

负责背包物品排序逻辑。

```csharp
public class InventorySortService
{
    // 排序优先级
    public enum ItemSortPriority
    {
        Tool = 0,       // 工具
        Placeable = 1,  // 可放置物品
        Consumable = 2, // 消耗品
        Other = 3       // 其他
    }
    
    public void SortInventory(InventoryService inventory, ItemDatabase database);
    public ItemSortPriority GetItemPriority(ItemData item);
}
```

### 5. WorldItemPickup 扩展

扩展现有的 WorldItemPickup，添加拾取冷却支持。

```csharp
public class WorldItemPickup : MonoBehaviour
{
    // 新增：拾取冷却
    private float pickupCooldownEndTime;
    private bool hasLeftPickupRange;
    
    // 新增方法
    public void SetPickupCooldown(float duration);
    public bool CanBePickedUp();
    public void OnPlayerExitRange();
    public void OnPlayerEnterRange();
}
```

## 数据模型

### 物品状态机

```
                    ┌─────────────┐
                    │    None     │
                    │  (初始状态) │
                    └──────┬──────┘
                           │
           ┌───────────────┼───────────────┐
           │               │               │
           ▼               ▼               ▼
    ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
    │  Dragging   │ │ HeldByShift │ │ HeldByCtrl  │
    │ (长按拖拽)  │ │ (Shift拿起) │ │ (Ctrl拿起)  │
    └──────┬──────┘ └──────┬──────┘ └──────┬──────┘
           │               │               │
           │    ┌──────────┴──────────┐    │
           │    │                     │    │
           ▼    ▼                     ▼    ▼
    ┌─────────────┐            ┌─────────────┐
    │   放置成功   │            │   放置失败   │
    │ (交换/移动) │            │  (返回原位)  │
    └─────────────┘            └─────────────┘
           │                          │
           └──────────┬───────────────┘
                      ▼
               ┌─────────────┐
               │    None     │
               └─────────────┘
```

### 装备槽位映射

| 槽位索引 | 装备类型 | 说明 |
|---------|---------|------|
| 0 | Helmet | 头盔 |
| 1 | Pants | 裤子 |
| 2 | Armor | 盔甲 |
| 3 | Shoes | 鞋子 |
| 4 | Ring | 戒指1（优先） |
| 5 | Ring | 戒指2（备选） |

### 戒指替换策略

```csharp
// 记录最近被替换的戒指槽位
private int lastReplacedRingSlot = -1;

public int GetRingSlotToReplace()
{
    // 如果槽位4为空，优先装备到槽位4
    if (IsSlotEmpty(4)) return 4;
    // 如果槽位5为空，装备到槽位5
    if (IsSlotEmpty(5)) return 5;
    // 两个都有装备，替换最近未被替换的
    return lastReplacedRingSlot == 4 ? 5 : 4;
}
```

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. 
Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 拖拽交换保持物品总量不变
*For any* 两个槽位 A 和 B，执行拖拽交换后，A 和 B 的物品内容应该完全互换，系统中物品总量保持不变
**Validates: Requirements 1.2, 1.3**

### Property 2: Shift 二分拿取数量正确
*For any* 数量为 N 的物品槽位，执行 Shift 拿取后，手上数量应为 floor(N/2)，原槽位数量应为 N - floor(N/2)，总量保持不变
**Validates: Requirements 2.1, 2.2, 2.3**

### Property 3: Ctrl 单个拿取数量正确
*For any* 数量为 N (N > 0) 的物品槽位，执行 Ctrl 拿取后，手上数量应增加 1，原槽位数量应减少 1，总量保持不变
**Validates: Requirements 3.1**

### Property 4: 状态互斥性
*For any* 时刻，系统只能处于 None、Dragging、HeldByShift、HeldByCtrl 四种状态之一，且 Shift+Ctrl 同时按下时不触发任何状态转换
**Validates: Requirements 4.1, 4.2, 4.3, 4.4**

### Property 5: 放置失败回退正确性
*For any* 拿起状态的物品，如果放置到有其他物品的槽位，应该返回原槽位并恢复原数量
**Validates: Requirements 2.6, 3.7**

### Property 6: 丢弃冷却机制
*For any* 被丢弃的物品，在 5 秒内且玩家未离开过拾取范围时，不应被自动拾取；5 秒后或玩家离开再进入范围后，应可被拾取
**Validates: Requirements 5.3, 5.4, 5.5**

### Property 7: 掉落物生成冷却
*For any* 资源节点生成的掉落物，在 1 秒内不应被自动拾取
**Validates: Requirements 6.1, 6.2, 6.3**

### Property 8: 排序优先级正确性
*For any* 排序后的背包，所有工具类物品应排在可放置物品之前，可放置物品应排在消耗品之前，消耗品应排在其他物品之前
**Validates: Requirements 7.2, 7.4**

### Property 9: 装备槽位类型限制
*For any* 装备槽位，只能放置对应类型的装备，非装备物品或类型不匹配的装备应被拒绝
**Validates: Requirements 9.1, 9.2, 9.3**

### Property 10: 戒指装备策略
*For any* 戒指装备操作，应优先装备到空槽位（4优先于5），两个都有装备时应替换最近未被替换的槽位
**Validates: Requirements 8.4, 8.5, 8.6**

### Property 11: 槽位清空一致性
*For any* 槽位，当数量变为 0 时，应设置为 Empty 状态，UI 应清除显示，并触发变更事件
**Validates: Requirements 10.1, 10.2, 10.3**

## 错误处理

### 异常情况处理

| 情况 | 处理方式 |
|------|---------|
| 拖拽到无效区域 | 返回原槽位 |
| 放置到已有物品的槽位（拿起状态） | 返回原槽位 |
| 装备类型不匹配 | 返回原槽位 |
| 背包已满无法放置 | 返回原槽位 |
| 丢弃时无法生成掉落物 | 记录错误日志，返回原槽位 |

### 边界条件

| 条件 | 处理方式 |
|------|---------|
| 数量为 1 时 Shift 二分 | 不执行操作 |
| 数量为 0 时任何操作 | 不执行操作，清空槽位 |
| 空槽位上的操作 | 不执行拿取操作 |

## 测试策略

### 单元测试

1. **InventorySortService 测试**
   - 测试各类物品的优先级判定
   - 测试排序后的顺序正确性

2. **戒指装备策略测试**
   - 测试空槽位优先级
   - 测试替换策略

### 属性测试

使用 NUnit + FsCheck 进行属性测试：

1. **交换不变性测试**
   - 生成随机的两个 ItemStack
   - 执行交换
   - 验证总量不变

2. **二分正确性测试**
   - 生成随机数量 (1-999)
   - 执行多次二分
   - 验证每次二分后数量正确

3. **排序正确性测试**
   - 生成随机的物品列表
   - 执行排序
   - 验证排序后顺序符合优先级规则

### 集成测试

1. **拖拽流程测试**
   - 模拟长按 → 拖拽 → 放置的完整流程

2. **Shift/Ctrl 流程测试**
   - 模拟各种键位组合的操作流程

3. **丢弃和拾取测试**
   - 测试丢弃后的冷却机制
   - 测试离开范围后重新进入的拾取
