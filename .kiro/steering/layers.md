---
inclusion: always
priority: P0
isCanonical: true
canonicalDomain: [Sorting Layers, 楼层结构, 树木层级结构, 场景层级, 命中检测]
lastUpdated: 2025-01-09
---

# 项目层级设计规范

## 🔴 核心概念：阶梯式楼层结构 🔴

本项目采用**阶梯式/楼层式**层级设计：
- 每个 LAYER 代表一个"楼层"（如 LAYER 1 = 一楼，LAYER 2 = 二楼）
- **每个楼层内部结构完全一致**
- 玩家在某一楼层时，只与该楼层的物体交互

## 场景层级结构

```
Scene
├── ─── LAYER 1 ─── （一楼）
│   ├── Props/                    # 场景物体（树木、石头、建筑等）
│   └── Tilemaps/                 # 地面 Tilemap
│       ├── GroundBase            # 地面基础
│       ├── Grass                 # 草地装饰
│       ├── Wall                  # 墙壁/边界
│       └── Farmland              # 耕地（农田系统）
│
├── ─── LAYER 2 ─── （二楼）
│   ├── Props/                    # 场景物体
│   └── Tilemaps/                 # 地面 Tilemap
│       ├── GroundBase
│       ├── Grass
│       ├── Wall
│       └── Farmland
│
├── ─── LAYER 3 ─── （三楼）
│   ├── Props/                    # 场景物体
│   └── Tilemaps/                 # 地面 Tilemap
│       ├── GroundBase
│       ├── Grass
│       ├── Wall
│       └── Farmland
│
├── ─── DYNAMIC ───
│   └── Player                    # 玩家（会在不同楼层间移动）
│
├── ─── MANAGERS ───
│   ├── TimeManager
│   ├── SeasonManager
│   ├── FarmingManager            # 农田管理器
│   └── AudioManager
│
└── ─── UI ───
    └── Canvas
```

## 楼层内部结构（每层一致）

```
─── LAYER N ───
├── Props/                        # 场景物体容器
│   ├── Trees/                    # 树木
│   ├── Rocks/                    # 石头
│   └── Buildings/                # 建筑
│
└── Tilemaps/                     # Tilemap 容器
    ├── GroundBase                # 地面基础（最底层）
    ├── Grass                     # 草地装饰
    ├── Wall                      # 墙壁/边界
    ├── Farmland                  # 耕地（干燥状态）
    └── WaterPuddle               # 水渍叠加层（浇水后显示）
```

## Sorting Layers（从下到上）

| 序号 | Sorting Layer | 用途 |
|------|---------------|------|
| 1 | Background | 背景 |
| 2 | Ground | 地面 Tilemap |
| 3 | Layer 1 | 一楼物体 |
| 4 | Layer 2 | 二楼物体 |
| 5 | Layer 3 | 三楼物体 |
| 6 | Effects | 特效 |
| 7 | CloudShadow | 云朵阴影 |
| 8 | UI | 用户界面 |

## 楼层交互规则

1. **玩家位置决定当前楼层** - 玩家在哪个 LAYER 下，就与哪个楼层交互
2. **耕地操作限定当前楼层** - 锄头只能耕作当前楼层的 Farmland Tilemap
3. **物体放置限定当前楼层** - 树苗、建筑等只能放置在当前楼层的 Props 下
4. **Tilemap 独立** - 每个楼层有独立的 Farmland 和 WaterPuddle Tilemap

## 农田系统 Tilemap 结构

每个楼层需要以下 Tilemap 支持农田系统：

```
─── LAYER N ───
└── Tilemaps/
    ├── GroundBase                # 地面基础（可耕作区域检测）
    ├── Farmland                  # 耕地 Tilemap（使用 Rule Tile）
    │   └── Rule Tile 自动拼接边界
    └── WaterPuddle               # 水渍叠加层
        └── 浇水后随机显示 3 种水渍样式之一
```

### 耕地 Rule Tile 配置

耕地使用 Rule Tile 实现自动边界拼接：
- 单独一格 → 显示完整边界（上下左右都是边界）
- 相邻有耕地 → 自动切换为中心样式
- 中心有 2 种样式变体，随机选择

### 水渍叠加层

水渍是独立的 Tilemap，叠加在 Farmland 之上：
- 浇水时在 WaterPuddle Tilemap 设置水渍 Tile
- 3 种水渍样式随机选择
- 第二天自动清除

## 树木层级结构（关键）

```
Tree_M1_00（父物体，放置在 LAYER N/Props/ 下）
├─ 位置 = 树根 = 种植点（游戏逻辑位置）
├─ Layer = Default（不要修改！）
├─ Rigidbody2D (Static)
├─ CompositeCollider2D (物理碰撞用)
├─ OcclusionTransparency
│
├─ Tree（子物体）
│    ├─ SpriteRenderer（渲染位置）
│    ├─ PolygonCollider2D (Merge) ← 只覆盖树根，用于物理碰撞
│    └─ TreeController（AlignSpriteBottom）
│
└─ Shadow（子物体）
     └─ SpriteRenderer（阴影）
```

### 重要说明

1. **父物体 Layer 有用途** - 不要随意修改，用于导航网格检测
2. **PolygonCollider2D 只覆盖树根** - 用于物理碰撞和导航阻挡，不是整个树干
3. **命中检测使用 Sprite Bounds** - 不依赖碰撞体，使用 SpriteRenderer.bounds
4. **树木放置在对应楼层的 Props 下** - 确保与正确楼层交互

## 命中检测规范

### ❌ 错误做法
```csharp
// 不要使用 LayerMask 检测树木
resourceMask = LayerMask.GetMask("Resource");
Physics2D.OverlapCircleAll(origin, reach, resourceMask);
```

### ✅ 正确做法
```csharp
// 使用 ResourceNodeRegistry + Sprite Bounds
var nodes = ResourceNodeRegistry.Instance.GetNodesInRange(origin, reach);
foreach (var node in nodes)
{
    Bounds bounds = node.GetBounds(); // SpriteRenderer.bounds
    if (CheckBoundsIntersection(bounds, origin, forward, reach, angle))
    {
        node.OnHit(ctx);
    }
}
```

## 导航网格层级过滤

NavGrid2D 使用以下层级名称过滤世界物体：
- `LAYER 1`
- `LAYER 2`
- `LAYER 3`

只有在这些层级下的物体才会被纳入导航网格计算。

## 标签（Tags）

| 标签 | 用途 |
|------|------|
| Tree | 树木（单数形式） |
| Rock | 矿石 |
| Building | 建筑 |
| Player | 玩家 |
| Interactable | 可交互物体 |

## 碰撞体用途说明

| 组件 | 位置 | 用途 |
|------|------|------|
| CompositeCollider2D | 树木父物体 | 物理碰撞、导航阻挡 |
| PolygonCollider2D (Merge) | Tree 子物体 | 合并到父物体的 CompositeCollider2D |
| BoxCollider2D (Trigger) | 可选 | 交互检测（如果需要） |

**注意**：命中检测不使用碰撞体，而是使用 Sprite Bounds！

