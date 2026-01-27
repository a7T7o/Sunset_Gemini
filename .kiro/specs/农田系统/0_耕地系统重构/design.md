# 设计文档 - 耕地系统重构

## 概述

本设计文档描述如何使用 Unity Rule Tile 实现耕地自动边界拼接，并支持阶梯式楼层结构。

## 架构

### 系统组件

```
┌─────────────────────────────────────────────────────────────┐
│                      FarmingManager                          │
│  - 管理所有楼层的耕地数据                                      │
│  - 处理锄头交互                                               │
│  - 协调 Tilemap 更新                                          │
└─────────────────────┬───────────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
┌───────────┐  ┌───────────┐  ┌───────────┐
│  LAYER 1  │  │  LAYER 2  │  │  LAYER 3  │
│ Farmland  │  │ Farmland  │  │ Farmland  │
│ Tilemap   │  │ Tilemap   │  │ Tilemap   │
│           │  │           │  │           │
│ WaterPuddle│ │ WaterPuddle│ │ WaterPuddle│
│ Tilemap   │  │ Tilemap   │  │ Tilemap   │
└───────────┘  └───────────┘  └───────────┘
```

### 数据流

```
玩家右键点击
    │
    ▼
检测玩家当前楼层
    │
    ▼
获取对应楼层的 Farmland Tilemap
    │
    ▼
检查点击位置是否可开垦
    │
    ├─ 否 → 显示提示，结束
    │
    ▼ 是
在 Farmland Tilemap 设置 Rule Tile
    │
    ▼
Rule Tile 自动更新相邻格子边界
    │
    ▼
创建/更新 FarmTileData 数据
```

## 组件和接口

### FarmingManager 扩展

```csharp
public class FarmingManager : MonoBehaviour
{
    // 每个楼层的 Tilemap 引用
    [System.Serializable]
    public class LayerTilemaps
    {
        public string layerName;           // "LAYER 1", "LAYER 2", etc.
        public Tilemap farmlandTilemap;    // 耕地 Tilemap
        public Tilemap waterPuddleTilemap; // 水渍 Tilemap
        public Tilemap groundTilemap;      // 地面 Tilemap（用于检测可开垦区域）
    }
    
    [SerializeField] private LayerTilemaps[] layerTilemaps;
    [SerializeField] private RuleTile farmlandRuleTile;  // 耕地 Rule Tile
    [SerializeField] private TileBase[] waterPuddleTiles; // 3 种水渍 Tile
    
    // 获取玩家当前楼层的 Tilemap
    public LayerTilemaps GetCurrentLayerTilemaps();
    
    // 在指定位置开垦耕地
    public bool TillSoilAtPosition(Vector3 worldPosition);
}
```

### 楼层检测

```csharp
public class LayerDetector
{
    // 根据玩家位置检测当前楼层
    public static string GetPlayerCurrentLayer(Transform player);
    
    // 根据世界坐标检测所属楼层
    public static string GetLayerAtPosition(Vector3 worldPosition);
}
```

## 数据模型

### FarmTileData 扩展

```csharp
[System.Serializable]
public class FarmTileData
{
    public Vector3Int position;
    public string layerName;           // 所属楼层
    public bool isTilled;
    public bool wateredToday;
    public bool wateredYesterday;
    public float waterTime;
    public SoilMoistureState moistureState;
    public CropInstance crop;
}
```

### 数据存储结构

```csharp
// 按楼层分组存储耕地数据
private Dictionary<string, Dictionary<Vector3Int, FarmTileData>> farmTilesByLayer;

// 示例：
// farmTilesByLayer["LAYER 1"][Vector3Int(0,0,0)] = FarmTileData
// farmTilesByLayer["LAYER 2"][Vector3Int(0,0,0)] = FarmTileData
```

## Rule Tile 配置指南

### 素材准备

你需要准备以下 Sprite：

| 编号 | 名称 | 用途 |
|------|------|------|
| 1 | Top | 上边界 |
| 2 | Bottom | 下边界 |
| 3 | Left | 左边界 |
| 4 | Right | 右边界 |
| 5 | TopLeft | 左上角 |
| 6 | TopRight | 右上角 |
| 7 | BottomLeft | 左下角 |
| 8 | BottomRight | 右下角 |
| 9 | Center_A | 中心样式 A |
| 10 | Center_B | 中心样式 B |

### Rule Tile 创建步骤

1. **创建 Rule Tile 资源**
   - 右键 Project 窗口 → Create → 2D → Tiles → Rule Tile
   - 命名为 `Farmland_RuleTile`

2. **配置默认 Sprite**
   - 设置 Default Sprite 为 `Center_A`

3. **添加规则**（共 9 条规则）

#### 规则 1：四周都有耕地 → 中心
```
邻居检测：
  [✓] [✓] [✓]
  [✓] [X] [✓]
  [✓] [✓] [✓]
  
输出：Center_A 或 Center_B（随机）
```

#### 规则 2：上方无耕地 → 上边界
```
邻居检测：
  [✗] [✗] [✗]
  [✓] [X] [✓]
  [✓] [✓] [✓]
  
输出：Top
```

#### 规则 3：下方无耕地 → 下边界
```
邻居检测：
  [✓] [✓] [✓]
  [✓] [X] [✓]
  [✗] [✗] [✗]
  
输出：Bottom
```

#### 规则 4：左方无耕地 → 左边界
```
邻居检测：
  [✗] [✓] [✓]
  [✗] [X] [✓]
  [✗] [✓] [✓]
  
输出：Left
```

#### 规则 5：右方无耕地 → 右边界
```
邻居检测：
  [✓] [✓] [✗]
  [✓] [X] [✗]
  [✓] [✓] [✗]
  
输出：Right
```

#### 规则 6-9：四个角落（类似配置）

### 简化方案：使用 Unity 内置模板

如果你的素材是标准的 9 宫格切片，可以使用更简单的方法：

1. 将所有边界 Sprite 放在一个 Sprite Sheet 中
2. 使用 Sprite Editor 切片为 9 宫格
3. 创建 Rule Tile 时选择 "Tiling Rules" 模板

## 水渍系统

### 水渍 Tile 配置

1. 创建 3 个普通 Tile（不是 Rule Tile）
2. 分别设置 3 种水渍 Sprite
3. 浇水时随机选择一个

### 水渍叠加逻辑

```csharp
public void WaterTileAtCell(Vector3Int cellPosition, string layerName)
{
    var tilemaps = GetLayerTilemaps(layerName);
    
    // 随机选择水渍样式
    int randomIndex = Random.Range(0, waterPuddleTiles.Length);
    TileBase waterTile = waterPuddleTiles[randomIndex];
    
    // 在水渍 Tilemap 设置 Tile
    tilemaps.waterPuddleTilemap.SetTile(cellPosition, waterTile);
}
```

## 正确性属性

*属性是系统应该满足的通用规则，用于验证实现的正确性。*

### 属性 1：耕地边界一致性
*对于任意* 耕地格子，如果其相邻位置有耕地，则该方向不应显示边界样式
**验证: 需求 2.1, 2.2**

### 属性 2：楼层隔离性
*对于任意* 耕地操作，只应影响玩家当前楼层的 Tilemap，不应影响其他楼层
**验证: 需求 3.1, 3.2**

### 属性 3：水渍与耕地对应
*对于任意* 水渍格子，其位置必须存在对应的耕地格子
**验证: 需求 4.1**

### 属性 4：开垦前置条件
*对于任意* 开垦操作，必须满足：位置有地面 Tile、位置无已有耕地、位置无障碍物
**验证: 需求 6.1, 6.2, 6.3**

## 错误处理

| 错误情况 | 处理方式 |
|---------|---------|
| 点击位置无地面 | 显示提示"此处无法开垦" |
| 点击位置已有耕地 | 静默忽略，不重复开垦 |
| 点击位置有障碍物 | 显示提示"此处有障碍物" |
| 未找到当前楼层 Tilemap | 记录错误日志，操作失败 |

## 测试策略

### 单元测试
- 测试 Rule Tile 边界规则是否正确
- 测试楼层检测逻辑
- 测试水渍随机选择

### 集成测试
- 测试锄头交互完整流程
- 测试多楼层独立操作
- 测试浇水和水渍显示

### 手动测试
- 验证视觉效果是否符合预期
- 验证边界拼接是否自然
