# 世界物品掉落系统 - 设计文档

**版本**: v1.0  
**日期**: 2025-12-17  
**状态**: ✅ 已完成

---

## Overview

本系统为 Sunset 农场模拟游戏提供完整的世界物品掉落与拾取功能。核心目标是：
1. 从 icon Sprite 自动生成 bagSprite 和 worldPrefab，减少美术工作量
2. 实现弹性掉落动画，提升游戏体验
3. 性能优化，支持大量掉落物品
4. 与石头系统集成，支持矿物和石料的差值掉落

---

## 与石头系统的集成

### 石头掉落物类型
- **矿物**：根据矿石类型（C1铜/C2铁/C3金）掉落对应矿物
- **石料**：所有石头都会掉落石料

### 差值掉落机制

石头系统使用差值掉落机制，每个阶段掉落的数量 = 当前阶段总量 - 下一阶段总量

| 阶段转换 | 矿物掉落计算 | 石料掉落 |
|---------|-------------|---------|
| M1→M2 | 当前矿物总量 - 下一阶段矿物总量 | 6 |
| M2→M3 | 当前矿物总量 - 下一阶段矿物总量（含量-1） | 2 |
| M3销毁 | 全部矿物 | 4 |
| M4销毁 | 0（无矿物） | 2 |

### 材料等级限制对掉落的影响
- 低等级镐子挖高等级矿：可以获得石料，但无法获得矿物
- 所有镐子都能获得石料
- 掉落物生成时需要检查工具材料等级

### StoneController 掉落调用示例

```csharp
// 在 StoneController 中
private void SpawnDrops(int oreCount, int stoneCount, bool canGetOre)
{
    Vector3 origin = GetPosition();
    
    // 掉落矿物（如果镐子等级足够）
    if (canGetOre && oreCount > 0 && oreDropItem != null)
    {
        WorldSpawnService.Instance.SpawnMultiple(
            oreDropItem, 
            quality: 0, 
            totalAmount: oreCount, 
            origin: origin, 
            spreadRadius: 0.5f
        );
    }
    
    // 掉落石料（所有镐子都能获得）
    if (stoneCount > 0 && stoneDropItem != null)
    {
        WorldSpawnService.Instance.SpawnMultiple(
            stoneDropItem, 
            quality: 0, 
            totalAmount: stoneCount, 
            origin: origin, 
            spreadRadius: 0.5f
        );
    }
}
```

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Editor Tools                              │
├─────────────────────────────────────────────────────────────────┤
│  WorldPrefabGenerator    │  BagSpriteGenerator                   │
│  - 从icon生成worldPrefab │  - 从icon生成bagSprite（45度旋转）    │
│  - 批量处理多个SO        │  - 自动缩放适配                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ItemData SO (Updated)                       │
├─────────────────────────────────────────────────────────────────┤
│  icon: Sprite           │ 基础图标，用于生成其他形式              │
│  bagSprite: Sprite      │ 背包/工具栏显示（为空时用icon）         │
│  worldPrefab: GameObject│ 世界预制体（含动画、阴影，无碰撞体）    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Runtime Components                           │
├─────────────────────────────────────────────────────────────────┤
│  WorldSpawnService      │  WorldItemDrop          │  WorldItemPool│
│  - 生成掉落物品         │  - 弹性动画控制         │  - 对象池管理 │
│  - 调用对象池           │  - 阴影同步             │  - 性能优化   │
│  - 位置随机偏移         │  - 距离检测暂停动画     │               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Components and Interfaces

### 1. ItemData SO 更新

```csharp
// ItemData.cs 新增/修改字段
public class ItemData : ScriptableObject
{
    [Header("=== 视觉资源 ===")]
    [Tooltip("基础图标（用于生成其他形式）")]
    public Sprite icon;

    [Tooltip("背包/工具栏显示图标（为空时使用icon）")]
    public Sprite bagSprite;

    [Tooltip("世界预制体（含动画、阴影，无碰撞体）")]
    public GameObject worldPrefab;

    // 移除旧字段: worldSprite

    /// <summary>
    /// 获取背包显示Sprite（优先bagSprite，回退到icon）
    /// </summary>
    public Sprite GetBagSprite() => bagSprite != null ? bagSprite : icon;
}
```

### 2. WorldItemDrop 组件

```csharp
public class WorldItemDrop : MonoBehaviour
{
    [Header("动画参数")]
    public float bounceHeight = 0.8f;      // 弹出高度
    public float bounceDecay = 0.5f;       // 弹跳衰减
    public int bounceCount = 3;            // 弹跳次数
    public float idleFloatAmplitude = 0.05f; // 浮动幅度
    public float idleFloatSpeed = 2f;      // 浮动速度

    [Header("性能")]
    public float animationActiveDistance = 15f; // 动画激活距离

    [Header("引用")]
    public Transform spriteTransform;      // Sprite子物体
    public Transform shadowTransform;      // 阴影子物体
    public SpriteRenderer shadowRenderer;  // 阴影渲染器

    // 状态
    private DropState currentState;
    private float stateTimer;
    private Vector3 velocity;
    private float groundY;
    private int currentBounce;

    public enum DropState { Bouncing, Idle, Paused }

    public void StartDrop(Vector3 direction, float force);
    public void SetPaused(bool paused);
    private void UpdateBouncing();
    private void UpdateIdle();
    private void UpdateShadow();
}
```

### 3. WorldItemPool 对象池

```csharp
public class WorldItemPool : MonoBehaviour
{
    public static WorldItemPool Instance { get; private set; }

    [Header("配置")]
    public GameObject defaultWorldPrefab;  // 默认预制体模板
    public int initialPoolSize = 20;
    public int maxPoolSize = 50;
    public int maxActiveItems = 100;       // 场景最大物品数

    public WorldItemPickup Spawn(ItemData data, Vector3 position, bool playAnimation);
    public void Despawn(WorldItemPickup item);
    private void CleanupOldestItems();
}
```

### 4. WorldSpawnService 更新

```csharp
public class WorldSpawnService : MonoBehaviour
{
    // 新增方法
    public WorldItemPickup SpawnWithAnimation(ItemData data, int quality, int amount, 
                                               Vector3 origin, Vector3 direction);
    public void SpawnMultiple(ItemData data, int quality, int totalAmount, 
                              Vector3 origin, float spreadRadius);
}
```

### 5. 编辑器工具

```csharp
// WorldPrefabGeneratorTool.cs
public class WorldPrefabGeneratorTool : EditorWindow
{
    [MenuItem("Tools/World Item/批量生成 World Prefab")]
    public static void ShowWindow();

    // 功能
    // 1. 选择多个 ItemData SO
    // 2. 从 icon 生成 45度旋转的 Sprite
    // 3. 创建 World Prefab（含阴影、动画组件）
    // 4. 自动赋值到 SO 的 worldPrefab 字段
}
```

---

## Data Models

### World Prefab 结构

```
WorldItem_{ItemName} (Prefab Root)
├── Transform
├── WorldItemPickup (Component)
├── WorldItemDrop (Component)
│
├── Sprite (Child)
│   ├── Transform (localPosition: 0, 0, 0)
│   └── SpriteRenderer
│       ├── Sprite: 45度旋转后的icon
│       ├── SortingLayer: "Layer 1"
│       └── SortingOrder: 动态计算
│
└── Shadow (Child)
    ├── Transform (localPosition: 0, -0.1, 0)
    └── SpriteRenderer
        ├── Sprite: 椭圆阴影
        ├── Color: (0, 0, 0, 0.3)
        ├── SortingLayer: "Layer 1"
        └── SortingOrder: Sprite - 1
```

### 掉落配置数据

```csharp
[System.Serializable]
public class DropConfig
{
    public ItemData item;
    public int minAmount = 1;
    public int maxAmount = 3;
    [Range(0f, 1f)]
    public float dropChance = 1f;
}

[System.Serializable]
public class DropTable
{
    public DropConfig[] drops;
    public float spreadRadius = 0.5f;
}
```

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Sprite旋转边界约束
*For any* icon Sprite，生成的45度旋转世界Sprite的边界框应该不超过原始icon的最大边长
**Validates: Requirements 1.2**

### Property 2: bagSprite回退逻辑
*For any* ItemData，当bagSprite为空时，GetBagSprite()应该返回icon
**Validates: Requirements 2.2**

### Property 3: worldPrefab回退逻辑
*For any* ItemData，当worldPrefab为空时，WorldSpawnService应该使用默认模板生成
**Validates: Requirements 2.3**

### Property 4: 掉落位置随机分布
*For any* 多个同时掉落的物品，每个物品的最终位置应该有随机偏移，不完全重叠
**Validates: Requirements 3.4**

### Property 5: 阴影高度同步
*For any* 正在弹跳的物品，阴影的缩放应该与物品高度成反比（物品越高，阴影越小）
**Validates: Requirements 4.2**

### Property 6: 距离动画暂停
*For any* 距离玩家超过animationActiveDistance的物品，动画应该处于暂停状态
**Validates: Requirements 5.1, 5.2**

### Property 7: 物品数量上限
*For any* 时刻，场景中活跃的掉落物品数量应该不超过maxActiveItems
**Validates: Requirements 5.4**

### Property 8: 掉落表物品生成
*For any* 矿石类型和品质，生成的掉落物品应该符合配置的DropTable
**Validates: Requirements 7.2**

### Property 9: 石头差值掉落正确性
*For any* 石头阶段转换，掉落的矿物数量应等于当前阶段矿物总量减去下一阶段矿物总量
**Validates: stone-system Requirements 4.1-4.3**

### Property 10: 材料等级限制掉落
*For any* 镐子和矿石组合，当镐子等级不足时，只应生成石料掉落物，不应生成矿物掉落物
**Validates: stone-system Requirements 8.1-8.10**

---

## Error Handling

| 场景 | 处理方式 |
|------|----------|
| icon为空 | 使用默认占位Sprite，输出警告日志 |
| worldPrefab为空 | 使用默认模板动态生成 |
| 对象池耗尽 | 扩展池大小（不超过maxPoolSize），或清理最旧物品 |
| 玩家引用丢失 | 暂停所有距离检测，保持动画播放 |

---

## Testing Strategy

### 单元测试
- ItemData.GetBagSprite() 回退逻辑
- Sprite旋转计算的边界约束
- DropTable 掉落概率计算

### 属性测试（Property-Based Testing）
使用 NUnit + FsCheck 或自定义随机测试框架：
- 配置每个属性测试运行 100 次迭代
- 测试标注格式：`**Feature: world-item-drop-system, Property {number}: {property_text}**`

### 集成测试
- 砍树后掉落物品生成
- 挖矿后掉落物品生成
- 对象池回收与复用

---

## Implementation Notes

### 45度旋转Sprite生成算法

```csharp
public static Sprite GenerateRotatedSprite(Sprite source)
{
    // 1. 获取原始尺寸
    int w = (int)source.rect.width;
    int h = (int)source.rect.height;
    int maxSize = Mathf.Max(w, h);
    
    // 2. 计算旋转后的边界框（45度旋转）
    // 旋转后对角线长度 = sqrt(w² + h²)
    // 但我们要保持在原始边界内，所以需要缩放
    float diagonal = Mathf.Sqrt(w * w + h * h);
    float scale = maxSize / diagonal;
    
    // 3. 创建新纹理，在中心绘制旋转后的Sprite
    int newSize = maxSize;
    Texture2D newTex = new Texture2D(newSize, newSize, TextureFormat.RGBA32, false);
    
    // 4. 逐像素旋转采样
    // ... 实现细节
    
    return Sprite.Create(newTex, new Rect(0, 0, newSize, newSize), 
                         new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
}
```

### 弹性动画状态机

```
┌─────────┐    落地     ┌─────────┐   弹跳完成   ┌─────────┐
│ Bouncing│ ─────────→ │ Bouncing│ ──────────→ │  Idle   │
│ (弹出)  │            │ (回弹)  │              │ (浮动)  │
└─────────┘            └─────────┘              └─────────┘
     │                      │                       │
     │    距离>阈值         │    距离>阈值          │   距离>阈值
     ▼                      ▼                       ▼
┌─────────────────────────────────────────────────────────┐
│                       Paused                             │
└─────────────────────────────────────────────────────────┘
```

### 性能优化策略

1. **距离检测优化**：每0.5秒检测一次，而非每帧
2. **对象池预热**：场景加载时预创建20个实例
3. **LOD策略**：远距离物品使用简化渲染
4. **批量清理**：超过上限时一次清理10个最旧物品
