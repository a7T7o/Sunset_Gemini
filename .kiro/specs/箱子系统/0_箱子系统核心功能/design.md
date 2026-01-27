# 设计文档

## 概述

箱子系统是一个完整的存储容器功能模块，包含数据层（StorageData SO扩展）、逻辑层（ChestController世界物体控制）、表现层（Box UI面板）三个核心部分。系统需要与现有的放置系统、工具系统、背包系统、UI系统深度集成。

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                        数据层                                │
├─────────────────────────────────────────────────────────────┤
│  StorageData (SO)          │  ChestMaterial (Enum)          │
│  - maxHealth               │  - Wood                        │
│  - chestMaterial           │  - Iron                        │
│  - storageRows/Cols        │  - Special                     │
│  - storageCapacity         │                                │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        逻辑层                                │
├─────────────────────────────────────────────────────────────┤
│  ChestController (MonoBehaviour)                            │
│  - currentHealth           - OnHit(damage, toolType)        │
│  - ownership               - TryLock(lockMaterial)          │
│  - isLocked                - TryUnlock(keyMaterial)         │
│  - contents                - TryOpen()                      │
│                            - TryPush(direction)             │
├─────────────────────────────────────────────────────────────┤
│  ChestDropHandler (静态工具类)                               │
│  - HandleChestDrop()       - FindEmptyPosition()            │
│  - SpawnPlacedChest()      - SpiralSearch()                 │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        表现层                                │
├─────────────────────────────────────────────────────────────┤
│  BoxPanelUI (MonoBehaviour)                                 │
│  - chestSlots (Up区域)     - Open(ChestController)          │
│  - inventorySlots (Down)   - Close()                        │
│  - currentChest            - RefreshUI()                    │
├─────────────────────────────────────────────────────────────┤
│  UIPanelManager (扩展 GameInputManager)                     │
│  - 管理 PackagePanel 和 Box 的互斥显示                       │
│  - 处理快捷键切换逻辑                                        │
└─────────────────────────────────────────────────────────────┘
```

## 组件与接口

### 1. ChestMaterial 枚举

```csharp
namespace FarmGame.Data
{
    public enum ChestMaterial
    {
        Wood = 0,    // 木质
        Iron = 1,    // 铁质
        Special = 2  // 特殊（预留）
    }
}
```

### 2. ChestOwnership 枚举

```csharp
namespace FarmGame.Data
{
    public enum ChestOwnership
    {
        Player = 0,  // 玩家所有（可直接打开）
        World = 1    // 世界所有（需要钥匙）
    }
}
```

### 3. StorageData SO 扩展

```csharp
// 新增字段
[Header("=== 箱子专属属性 ===")]
[Tooltip("箱子材质类型")]
public ChestMaterial chestMaterial = ChestMaterial.Wood;

[Tooltip("箱子最大血量")]
[Range(1, 20)]
public int maxHealth = 2;

// 修改字段范围
[Range(1, 15)]
public int storageCols = 5;
```

### 4. ChestController 组件

**职责**：管理箱子世界物体的所有交互逻辑

**核心接口**：
- `OnHit(int damage, ToolType toolType)` - 受击处理
- `TryLock(ChestMaterial lockMaterial)` - 尝试上锁
- `TryUnlock(ChestMaterial keyMaterial)` - 尝试解锁
- `TryOpen()` - 尝试打开
- `TryPush(Vector2 direction)` - 尝试推动

### 5. ChestDropHandler 静态类

**职责**：处理箱子掉落后的自动放置逻辑

**核心接口**：
- `HandleChestDrop(ItemData itemData, Vector3 dropPosition)` - 处理箱子掉落
- `FindEmptyPosition(Vector3 center, float maxRadius)` - 螺旋搜索空位

### 6. BoxPanelUI 组件

**职责**：管理箱子交互UI面板

**核心接口**：
- `Open(ChestController chest)` - 打开指定箱子的UI
- `Close()` - 关闭UI
- `RefreshUI()` - 刷新显示

## 数据模型

### ChestRuntimeData（运行时数据）

```csharp
public class ChestRuntimeData
{
    public int currentHealth;           // 当前血量
    public ChestOwnership ownership;    // 归属
    public bool isLocked;               // 是否上锁
    public List<ItemStack> contents;    // 内容物
}
```

### 箱子类型配置

| 类型 | ID | 材质 | 行×列 | 容量 | 血量 |
|------|-----|------|-------|------|------|
| 小木箱 | 1400 | Wood | 1×12 | 12 | 2 |
| 大木箱 | 1401 | Wood | 2×12 | 24 | 3 |
| 小铁箱 | 1402 | Iron | 3×12 | 36 | 4 |
| 大铁箱 | 1403 | Iron | 4×12 | 48 | 5 |

### 锁与钥匙配置

| 类型 | ID | 材质 | 说明 |
|------|-----|------|------|
| 木锁 | 1410 | Wood | 对木箱上锁 |
| 铁锁 | 1411 | Iron | 对铁箱上锁 |
| 木钥匙 | 1420 | Wood | 解锁木箱 |
| 铁钥匙 | 1421 | Iron | 解锁铁箱 |

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 容量计算一致性
*For any* StorageData 实例，storageCapacity 应该等于 storageRows × storageCols
**Validates: Requirements 1.2**

### Property 2: 镐子伤害传递
*For any* 镐子命中空箱子的情况，箱子受到的伤害应该等于镐子的伤害值
**Validates: Requirements 4.1**

### Property 3: 非镐子工具无伤害
*For any* 非镐子工具命中箱子的情况，箱子血量应该保持不变
**Validates: Requirements 4.3**

### Property 4: 有物品箱子不受伤害
*For any* 镐子命中有物品箱子的情况，箱子血量应该保持不变
**Validates: Requirements 4.4**

### Property 5: 推动碰撞阻挡
*For any* 推动操作，如果目标位置存在碰撞体，箱子位置应该保持不变
**Validates: Requirements 5.3**

### Property 6: 材质匹配上锁
*For any* 上锁操作，只有当锁材质与箱子材质匹配时才能成功上锁
**Validates: Requirements 6.1, 6.2**

### Property 7: 材质匹配解锁
*For any* 解锁操作，只有当钥匙材质与箱子材质匹配且箱子归属为世界时才能成功解锁
**Validates: Requirements 7.1, 7.2**

### Property 8: 玩家箱子直接打开
*For any* 归属为玩家的箱子，无论是否上锁都应该能直接打开
**Validates: Requirements 7.4**

### Property 9: UI面板互斥
*For any* 时刻，PackagePanel 和 Box 最多只能有一个处于显示状态
**Validates: Requirements 9.1, 9.2**

### Property 10: 掉落过程不可拾取
*For any* 处于掉落动画状态的箱子，不应该变为可拾取状态
**Validates: Requirements 3.4**

## 错误处理

### 1. 空位搜索失败
- 场景：螺旋搜索在最大半径内找不到空位
- 处理：箱子保留在背包，显示提示"附近没有空位放置箱子"

### 2. 材质不匹配
- 场景：锁/钥匙与箱子材质不匹配
- 处理：显示提示"锁与箱子材质不匹配"或"钥匙与箱子材质不匹配"

### 3. 箱子已上锁
- 场景：尝试对已上锁的箱子再次上锁
- 处理：显示提示"箱子已上锁"

### 4. 需要钥匙
- 场景：尝试打开归属为世界的上锁箱子
- 处理：显示提示"需要钥匙才能打开"

## 测试策略

### 单元测试
- StorageData 容量计算验证
- ChestMaterial 枚举值验证
- 材质匹配逻辑验证

### 属性测试
- 使用 NUnit + 随机数据生成器
- 测试伤害计算、材质匹配、UI互斥等核心属性
- 每个属性测试运行 100 次迭代

### 集成测试
- 箱子掉落→自动放置流程
- 上锁→解锁→打开流程
- UI面板切换流程
