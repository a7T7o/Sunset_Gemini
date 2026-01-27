# 耕地 Tile 系统设计 - 设计文档

**创建日期**: 2026-01-25  
**版本**: 1.0  
**状态**: 已完成

---

## 架构概述

本系统采用"中心块 + 边界装饰"模式，核心特点：
- **中心块 (C)** 是玩家锄地创建的"真正的耕地"
- **边界 Tile** 是围绕中心块的视觉装饰，不是耕地
- **阴影 Tile** 在对角线方向有中心块时显示

---

## Tilemap 层级结构

```
─── LAYER N ───
└── Tilemaps/
    ├── GroundBase           ← 地面基础（已存在，用于检测可耕作区域）
    ├── Farmland_Center      ← 耕地中心块（新建）
    ├── Farmland_Border      ← 耕地边界（新建）
    └── WaterPuddle          ← 水渍（已存在）
```

### 层级说明

| Tilemap | Sorting Layer | Order | 用途 |
|---------|---------------|-------|------|
| GroundBase | Ground | 0 | 地面基础，检测可耕作区域 |
| Farmland_Center | Ground | 1 | 耕地中心块 |
| Farmland_Border | Ground | 2 | 耕地边界装饰 |
| WaterPuddle | Ground | 3 | 水渍叠加 |

---

## 核心组件设计

### 1. FarmlandBorderManager（新建）

负责边界 Tile 的计算和更新。

```csharp
public class FarmlandBorderManager : MonoBehaviour
{
    // 单例
    public static FarmlandBorderManager Instance { get; private set; }
    
    // Tile 资源引用
    [Header("中心块")]
    [SerializeField] private TileBase centerTile;           // C
    
    [Header("单方向边界")]
    [SerializeField] private TileBase borderU;              // U
    [SerializeField] private TileBase borderD;              // D
    [SerializeField] private TileBase borderL;              // L
    [SerializeField] private TileBase borderR;              // R
    
    [Header("双方向边界 - 对边")]
    [SerializeField] private TileBase borderUD;             // UD
    [SerializeField] private TileBase borderLR;             // LR
    
    [Header("双方向边界 - 相邻")]
    [SerializeField] private TileBase borderUL;             // UL
    [SerializeField] private TileBase borderUR;             // UR
    [SerializeField] private TileBase borderDL;             // DL
    [SerializeField] private TileBase borderDR;             // DR
    
    [Header("三方向边界")]
    [SerializeField] private TileBase borderUDL;            // UDL
    [SerializeField] private TileBase borderUDR;            // UDR
    [SerializeField] private TileBase borderULR;            // ULR
    [SerializeField] private TileBase borderDLR;            // DLR
    
    [Header("四方向边界")]
    [SerializeField] private TileBase borderUDLR;           // UDLR
    
    [Header("角落阴影")]
    [SerializeField] private TileBase shadowLU;             // SLU
    [SerializeField] private TileBase shadowRU;             // SRU
    [SerializeField] private TileBase shadowLD;             // SLD
    [SerializeField] private TileBase shadowRD;             // SRD
    
    // 核心方法
    public void OnCenterBlockPlaced(int layerIndex, Vector3Int cellPosition);
    public void OnCenterBlockRemoved(int layerIndex, Vector3Int cellPosition);
    public void UpdateBordersAround(int layerIndex, Vector3Int centerPosition);
}
```

### 2. LayerTilemaps 扩展

在现有 `LayerTilemaps` 类中添加新的 Tilemap 引用：

```csharp
[System.Serializable]
public class LayerTilemaps
{
    // 现有字段...
    
    [Header("耕地 Tile 系统（新）")]
    [Tooltip("耕地中心块 Tilemap")]
    public Tilemap farmlandCenterTilemap;
    
    [Tooltip("耕地边界 Tilemap")]
    public Tilemap farmlandBorderTilemap;
}
```

---

## 边界选择算法

### 输入

对于每个非中心块的格子 `(x, y)`，检测周围 8 个方向：

```
四个正方向：
- hasU = (x, y+1) 有中心块
- hasD = (x, y-1) 有中心块
- hasL = (x-1, y) 有中心块
- hasR = (x+1, y) 有中心块

四个对角线：
- hasLU = (x-1, y+1) 有中心块
- hasRU = (x+1, y+1) 有中心块
- hasLD = (x-1, y-1) 有中心块
- hasRD = (x+1, y-1) 有中心块
```

### 边界选择逻辑

```csharp
TileBase SelectBorderTile(bool hasU, bool hasD, bool hasL, bool hasR)
{
    int count = (hasU ? 1 : 0) + (hasD ? 1 : 0) + (hasL ? 1 : 0) + (hasR ? 1 : 0);
    
    switch (count)
    {
        case 0:
            return null; // 无边界，可能是阴影
            
        case 1:
            if (hasU) return borderU;
            if (hasD) return borderD;
            if (hasL) return borderL;
            if (hasR) return borderR;
            break;
            
        case 2:
            // 对边
            if (hasU && hasD) return borderUD;
            if (hasL && hasR) return borderLR;
            // 相邻
            if (hasU && hasL) return borderUL;
            if (hasU && hasR) return borderUR;
            if (hasD && hasL) return borderDL;
            if (hasD && hasR) return borderDR;
            break;
            
        case 3:
            if (!hasR) return borderUDL;
            if (!hasL) return borderUDR;
            if (!hasD) return borderULR;
            if (!hasU) return borderDLR;
            break;
            
        case 4:
            return borderUDLR;
    }
    
    return null;
}
```

### 阴影选择逻辑

只有当四个正方向都没有中心块时，才检查对角线放置阴影：

```csharp
void PlaceShadowsIfNeeded(Vector3Int pos, bool hasU, bool hasD, bool hasL, bool hasR,
                          bool hasLU, bool hasRU, bool hasLD, bool hasRD)
{
    // 只有四个正方向都没有中心块时，才放置阴影
    if (hasU || hasD || hasL || hasR) return;
    
    // 检查对角线
    if (hasLU) PlaceTile(pos, shadowLU);
    else if (hasRU) PlaceTile(pos, shadowRU);
    else if (hasLD) PlaceTile(pos, shadowLD);
    else if (hasRD) PlaceTile(pos, shadowRD);
}
```

---

## 工作流程

### 玩家锄地流程

```
1. 玩家使用锄头点击地面
2. GameInputManager 检测到锄地操作
3. 调用 FarmTileManager.CreateTile() 创建耕地数据
4. 调用 FarmlandBorderManager.OnCenterBlockPlaced()
   4.1 在 Farmland_Center 放置中心块 C
   4.2 遍历周围 3×3 范围（共 8 个格子）
   4.3 对每个格子计算边界类型
   4.4 在 Farmland_Border 放置对应边界 Tile
5. 播放锄地音效和粒子效果
```

### 边界更新流程

```
UpdateBordersAround(layerIndex, centerPosition):
    for dx in [-1, 0, 1]:
        for dy in [-1, 0, 1]:
            if dx == 0 && dy == 0: continue  // 跳过中心
            
            neighborPos = centerPosition + (dx, dy)
            
            // 如果邻居是中心块，跳过
            if IsCenterBlock(neighborPos): continue
            
            // 计算邻居周围的中心块分布
            (hasU, hasD, hasL, hasR, hasLU, hasRU, hasLD, hasRD) = 
                CheckNeighborCenters(neighborPos)
            
            // 选择边界 Tile
            borderTile = SelectBorderTile(hasU, hasD, hasL, hasR)
            
            if borderTile != null:
                PlaceBorderTile(neighborPos, borderTile)
            else:
                // 检查是否需要放置阴影
                PlaceShadowsIfNeeded(neighborPos, ...)
```

---

## 数据结构

### 中心块存储

使用现有的 `FarmTileManager.farmTilesByLayer` 字典存储中心块数据：

```csharp
// Key: layerIndex, Value: (cellPosition -> FarmTileData)
Dictionary<int, Dictionary<Vector3Int, FarmTileData>> farmTilesByLayer;
```

### 边界不存储数据

边界 Tile 是纯视觉效果，不需要存储数据。每次中心块变化时，重新计算并更新边界。

---

## 与现有系统集成

### FarmTileManager 修改

```csharp
public bool CreateTile(int layerIndex, Vector3Int cellPosition)
{
    // ... 现有逻辑 ...
    
    // 新增：通知边界管理器
    if (FarmlandBorderManager.Instance != null)
    {
        FarmlandBorderManager.Instance.OnCenterBlockPlaced(layerIndex, cellPosition);
    }
    
    return true;
}
```

### FarmVisualManager 修改

水渍 Tile 仍然使用现有的 `waterPuddleTilemap`，不需要修改。

---

## 正确性属性

### P1: 中心块唯一性

每个格子最多只能有一个中心块。

```
∀ layer, pos: 
    CreateTile(layer, pos) 成功后，
    再次调用 CreateTile(layer, pos) 必须返回 false
```

### P2: 边界正确性

边界 Tile 的选择必须与周围中心块分布一致。

```
∀ borderPos:
    borderTile = GetBorderTileAt(borderPos)
    (hasU, hasD, hasL, hasR) = CheckNeighborCenters(borderPos)
    
    borderTile == SelectBorderTile(hasU, hasD, hasL, hasR)
```

### P3: 阴影正确性

阴影只在对角线方向有中心块且四个正方向都没有中心块时显示。

```
∀ shadowPos:
    hasShadow = HasShadowAt(shadowPos)
    (hasU, hasD, hasL, hasR) = CheckNeighborCenters(shadowPos)
    (hasLU, hasRU, hasLD, hasRD) = CheckDiagonalCenters(shadowPos)
    
    hasShadow == (!hasU && !hasD && !hasL && !hasR && 
                  (hasLU || hasRU || hasLD || hasRD))
```

### P4: 边界更新完整性

放置/移除中心块后，周围 3×3 范围内的所有边界都必须更新。

```
∀ centerPos:
    OnCenterBlockPlaced(centerPos) 后，
    ∀ neighborPos in 3×3 范围:
        GetBorderTileAt(neighborPos) 必须反映最新的中心块分布
```

---

## 性能考虑

### 更新范围限制

每次放置/移除中心块只更新周围 3×3 范围（8 个格子），不会全局刷新。

### 批量操作优化

如果需要批量放置中心块（如加载存档），可以先放置所有中心块，最后统一刷新边界：

```csharp
public void BatchPlaceCenterBlocks(List<Vector3Int> positions)
{
    // 1. 放置所有中心块
    foreach (var pos in positions)
    {
        PlaceCenterTile(pos);
    }
    
    // 2. 收集所有需要更新的边界位置
    HashSet<Vector3Int> borderPositions = new HashSet<Vector3Int>();
    foreach (var pos in positions)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                borderPositions.Add(pos + new Vector3Int(dx, dy, 0));
            }
        }
    }
    
    // 3. 批量更新边界
    foreach (var borderPos in borderPositions)
    {
        UpdateBorderAt(borderPos);
    }
}
```

---

## 文件清单

| 文件 | 类型 | 说明 |
|------|------|------|
| `FarmlandBorderManager.cs` | 新建 | 边界管理器 |
| `LayerTilemaps.cs` | 修改 | 添加新 Tilemap 引用 |
| `FarmTileManager.cs` | 修改 | 集成边界管理器调用 |

---

**文档维护者**: Kiro  
**最后更新**: 2026-01-25
