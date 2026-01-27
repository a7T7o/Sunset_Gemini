# 设计文档 - 箱子样式与交互完善

## 概述

本设计文档描述箱子系统的样式管理、交互完善、来源归属系统和钥匙开锁概率系统的技术实现方案。主要包括：
- ChestController 的 Sprite 状态管理
- 抖动和推动动画效果
- IResourceNode 接口实现
- BoxPanelUI 与 ChestController 的状态同步
- **箱子来源与归属系统**（ChestOrigin 枚举）
- **钥匙开锁概率系统**（KeyData SO）
- **批量生成工具更新**（支持 KeyData）

## 架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           ChestController                                │
├─────────────────────────────────────────────────────────────────────────┤
│ 字段:                                                                    │
│   - origin: ChestOrigin (来源：玩家制作/野外生成)                         │
│   - ownership: ChestOwnership (归属：玩家/世界)                           │
│   - isLocked: bool (是否上锁)                                            │
│   - hasBeenLocked: bool (是否曾经上过锁)                                  │
│   - spriteUnlockedClosed/Open, spriteLockedClosed/Open: Sprite          │
│   - isOpen: bool (是否打开)                                              │
├─────────────────────────────────────────────────────────────────────────┤
│ 方法:                                                                    │
│   + SetOpen(bool open): void                                             │
│   + UpdateSprite(): void                                                 │
│   + PlayShakeEffect(): void                                              │
│   + OnHit(ToolHitContext ctx): void                                      │
│   + CanAccept(ToolHitContext ctx): bool                                  │
│   + TryLockByPlayer(): LockResult                                        │
│   + TryUnlockWithKey(KeyData key): UnlockResult                          │
│   + CanBeMinedOrMoved(): bool                                            │
└─────────────────────────────────────────────────────────────────────────┘
                              │
                              │ 使用
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                              KeyData                                     │
├─────────────────────────────────────────────────────────────────────────┤
│   + keyMaterial: ToolMaterial (钥匙材质)                                 │
│   + unlockChance: float (开锁概率 0-1)                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

## 组件和接口

### 新增枚举：ChestOrigin

```csharp
/// <summary>
/// 箱子来源枚举
/// 决定箱子的交互规则
/// </summary>
public enum ChestOrigin
{
    PlayerCrafted = 0,  // 玩家制作
    WorldSpawned = 1    // 野外生成
}
```

### 新增数据类：KeyData

```csharp
/// <summary>
/// 钥匙数据 - 用于开锁野外上锁箱子
/// </summary>
[CreateAssetMenu(fileName = "Key_New", menuName = "Farm/Items/KeyData", order = 5)]
public class KeyData : ItemData
{
    [Header("=== 钥匙属性 ===")]
    [Tooltip("钥匙材质（复用工具材质枚举）")]
    public ToolMaterial keyMaterial = ToolMaterial.Wood;

    [Tooltip("开锁概率 (0-1)")]
    [Range(0f, 1f)]
    public float unlockChance = 0.1f;
}
```

### 修改 StorageData

```csharp
// 新增字段
[Header("=== 开锁概率 ===")]
[Tooltip("被开锁的基础概率（野外上锁箱子用）")]
[Range(0f, 1f)]
public float baseUnlockChance = 0.6f;
```

### ChestController 扩展

#### 新增字段

```csharp
[Header("=== 来源与归属 ===")]
[Tooltip("箱子来源")]
[SerializeField] private ChestOrigin origin = ChestOrigin.PlayerCrafted;

[Tooltip("是否曾经被上过锁（上过锁的箱子不能再次上锁）")]
[SerializeField] private bool hasBeenLocked = false;
```

#### 核心方法

```csharp
/// <summary>
/// 判断箱子是否可以被挖取或移动
/// </summary>
public bool CanBeMinedOrMoved()
{
    // 野外上锁箱子（无论是否已开锁）不能被挖取或移动
    if (origin == ChestOrigin.WorldSpawned && hasBeenLocked)
        return false;
    return true;
}

/// <summary>
/// 玩家尝试上锁（消耗锁道具）
/// </summary>
public LockResult TryLockByPlayer()
{
    // 已经上过锁的箱子不能再次上锁
    if (hasBeenLocked)
        return LockResult.AlreadyLocked;
    
    // 野外上锁箱子不能被玩家上锁
    if (origin == ChestOrigin.WorldSpawned && isLocked)
        return LockResult.AlreadyLocked;
    
    isLocked = true;
    hasBeenLocked = true;
    
    // 野外未锁箱子上锁后变为玩家归属
    if (origin == ChestOrigin.WorldSpawned)
        ownership = ChestOwnership.Player;
    
    UpdateSprite();
    return LockResult.Success;
}

/// <summary>
/// 使用钥匙尝试开锁（概率系统）
/// </summary>
public UnlockResult TryUnlockWithKey(KeyData keyData)
{
    if (!isLocked)
        return UnlockResult.NotLocked;
    
    // 玩家自己的箱子不需要钥匙
    if (ownership == ChestOwnership.Player)
        return UnlockResult.AlreadyOwned;
    
    // 计算开锁概率
    float totalChance = keyData.unlockChance + storageData.baseUnlockChance;
    bool success = Random.value <= totalChance;
    
    if (success)
    {
        isLocked = false;
        // 野外箱子开锁后保持 World 归属
        UpdateSprite();
        return UnlockResult.Success;
    }
    
    // 失败时钥匙会被消耗（由调用方处理）
    return UnlockResult.MaterialMismatch; // 复用枚举表示失败
}
```

### OnHit 方法修改

```csharp
public void OnHit(ToolHitContext ctx)
{
    // 始终播放抖动效果
    PlayShakeEffect();

    // 非镐子工具只抖动
    if (ctx.toolType != ToolType.Pickaxe)
        return;

    // 野外上锁箱子（包括已开锁的）不能被挖取或推动
    if (!CanBeMinedOrMoved())
    {
        if (showDebugInfo)
            Debug.Log("[ChestController] 野外上锁箱子不能被挖取或移动");
        return;
    }

    // 有物品：推动
    if (!IsEmpty)
    {
        TryPush(ctx.hitDir);
        return;
    }

    // 空箱子：造成伤害
    int damage = Mathf.Max(1, Mathf.RoundToInt(ctx.baseDamage));
    currentHealth -= damage;

    if (currentHealth <= 0)
        OnDestroyed();
}
```

## 数据模型

### 箱子状态矩阵

```
┌─────────────┬─────────────┬─────────────┬─────────────────────────────────┐
│   origin    │  isLocked   │ hasBeenLocked│         交互规则                │
├─────────────┼─────────────┼─────────────┼─────────────────────────────────┤
│ PlayerCrafted│   false    │    false    │ 可上锁、可挖取、可移动           │
│ PlayerCrafted│   true     │    true     │ 不能再上锁、可挖取、可移动       │
│ WorldSpawned │   false    │    false    │ 可上锁(变Player)、可挖取、可移动 │
│ WorldSpawned │   true     │    true     │ 需钥匙开锁、不能挖取、不能移动   │
│ WorldSpawned │   false    │    true     │ 已开锁、不能挖取、不能移动       │
└─────────────┴─────────────┴─────────────┴─────────────────────────────────┘
```

### 钥匙开锁概率表

```
┌─────────────┬─────────────┬─────────────┬─────────────┐
│  钥匙材质   │  钥匙概率   │  木箱概率   │  最终概率   │
├─────────────┼─────────────┼─────────────┼─────────────┤
│    木       │    0.10     │    0.60     │    0.70     │
│    石       │    0.15     │    0.60     │    0.75     │
│    铁       │    0.20     │    0.60     │    0.80     │
│    铜       │    0.25     │    0.60     │    0.85     │
│    钢       │    0.30     │    0.60     │    0.90     │
│    金       │    0.40     │    0.60     │    1.00     │
├─────────────┼─────────────┼─────────────┼─────────────┤
│    木       │    0.10     │    0.50     │    0.60     │
│    石       │    0.15     │    0.50     │    0.65     │
│    铁       │    0.20     │    0.50     │    0.70     │
│    铜       │    0.25     │    0.50     │    0.75     │
│    钢       │    0.30     │    0.50     │    0.80     │
│    金       │    0.40     │    0.50     │    0.90     │
└─────────────┴─────────────┴─────────────┴─────────────┘
```

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. 
Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 状态到 Sprite 的映射一致性

*对于任意* 箱子状态组合（isLocked, isOpen），调用 GetCurrentSprite() 应返回对应的 Sprite：
- (false, false) → spriteUnlockedClosed
- (false, true) → spriteUnlockedOpen
- (true, false) → spriteLockedClosed
- (true, true) → spriteLockedOpen

**Validates: Requirements 1.1, 1.2, 1.3, 1.4**

### Property 2: 工具类型决定伤害

*对于任意* 工具类型和可挖取的空箱子，只有镐子能造成伤害：
- 镐子攻击空箱子 → 血量减少
- 非镐子攻击空箱子 → 血量不变

**Validates: Requirements 2.1, 2.4**

### Property 3: 野外上锁箱子不可挖取

*对于任意* 野外上锁箱子（无论是否已开锁），CanBeMinedOrMoved() 应返回 false

**Validates: Requirements 8.1, 8.2**

### Property 4: 上过锁的箱子不能再次上锁

*对于任意* hasBeenLocked=true 的箱子，TryLockByPlayer() 应返回 AlreadyLocked

**Validates: Requirements 7.5, 9.4**

### Property 5: 开锁概率计算正确性

*对于任意* 钥匙和箱子组合，开锁概率应等于 keyData.unlockChance + storageData.baseUnlockChance

**Validates: Requirements 10.5**

### Property 6: 玩家箱子不需要钥匙

*对于任意* ownership=Player 的箱子，TryUnlockWithKey() 应返回 AlreadyOwned

**Validates: Requirements 9.2**

## 错误处理

1. **Sprite 为空**：如果任意 Sprite 字段为空，在 OnValidate 中输出警告，运行时使用当前 Sprite 不变
2. **KeyData 为空**：TryUnlockWithKey 传入 null 时返回失败
3. **StorageData 为空**：使用默认概率 0.5

## 测试策略

### 单元测试

1. **状态映射测试**：验证 GetCurrentSprite() 在不同状态下返回正确的 Sprite
2. **挖取权限测试**：验证 CanBeMinedOrMoved() 在不同来源和状态下返回正确值
3. **上锁逻辑测试**：验证 TryLockByPlayer() 在不同状态下的行为

### 属性测试

使用 NUnit 的 TestCase 属性进行参数化测试：

```csharp
[TestCase(ChestOrigin.WorldSpawned, true, true, ExpectedResult = false)]
[TestCase(ChestOrigin.WorldSpawned, false, true, ExpectedResult = false)]
[TestCase(ChestOrigin.WorldSpawned, false, false, ExpectedResult = true)]
[TestCase(ChestOrigin.PlayerCrafted, true, true, ExpectedResult = true)]
public bool CanBeMinedOrMoved_ReturnsCorrectValue(ChestOrigin origin, bool isLocked, bool hasBeenLocked)
{
    // 测试实现
}
```
