# 农田系统深度重构 - 设计文档

**创建日期**: 2026-01-26  
**更新日期**: 2026-01-26（根据设计审视更新）  
**工作区**: `.kiro/specs/农田系统/6_农田系统深度重构/`

---

## 一、核心设计原则（来自设计审视）

### 1.1 耕地的本质

**耕地是"状态"，不是"实体"**

| 组件 | 本质 | 是否需要独立控制器 |
|------|------|-------------------|
| 耕地中心块 | Tilemap 上的 Tile | ❌ 不需要 - 只是视觉表示 |
| 边界装饰 | Tilemap 上的 Tile | ❌ 不需要 - 只是视觉表示 |
| 水渍 | Tilemap 上的 Tile | ❌ 不需要 - 只是视觉表示 |
| 作物 | 独立 GameObject | ✅ 需要 - 有生命周期（后续设计） |

### 1.2 分层架构

```
┌─────────────────────────────────────────────────────────────┐
│                       数据层                                 │
│  FarmTileData - 每个格子的状态（位置、浇水、作物引用）        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       管理层                                 │
│  FarmTileManager - 耕地数据管理（CRUD、浇水状态）            │
│  FarmlandBorderManager - 边界视觉管理（中心块、边界 Tile）   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       实体层（后续设计）                      │
│  CropController - 作物生命周期管理（参考 TreeController）    │
└─────────────────────────────────────────────────────────────┘
```

---

## 二、简化后的组件关系

```
┌─────────────────────────────────────────────────────────────┐
│                     GameInputManager                         │
│  - TryTillSoil(worldPosition)                               │
│  - TryWaterTile(worldPosition)                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ 直接调用（不经过 FarmingManagerNew）
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     FarmTileManager                          │
│  - CreateTile(layerIndex, cellPosition)                     │
│  - SetWatered(layerIndex, cellPosition, waterTime)          │
│  - GetTileData(layerIndex, cellPosition)                    │
│  - CanTillAt(layerIndex, cellPosition)                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ 通知
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   FarmlandBorderManager                      │
│  - OnCenterBlockPlaced(layerIndex, cellPosition)            │
│  - OnCenterBlockRemoved(layerIndex, cellPosition)           │
│  - UpdateBordersAround(layerIndex, centerPosition)          │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ 更新
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       Tilemaps                               │
│  - farmlandCenterTilemap (中心块)                           │
│  - farmlandBorderTilemap (边界装饰)                         │
└─────────────────────────────────────────────────────────────┘
```

### 暂不使用的组件

| 组件 | 原因 | 处理方式 |
|------|------|---------|
| FarmingManagerNew | 过度设计的协调者 | 保留代码，不在 GameInputManager 中调用 |
| FarmVisualManager | 为旧版 Rule Tile 系统设计 | 保留代码，不在当前流程中使用 |
| CropManager | 作物系统后续再设计 | 保留代码，参考 TreeController 模式重新设计 |

---

## 三、修改方案

### 3.1 LayerTilemaps.WorldToCell() 修改

**问题**：使用旧版字段 `farmlandTilemap`，但场景配置的是新版字段 `farmlandCenterTilemap`

**修改方案**：
```csharp
public Vector3Int WorldToCell(Vector3 worldPosition)
{
    // 优先使用新版字段
    if (farmlandCenterTilemap != null)
    {
        return farmlandCenterTilemap.WorldToCell(worldPosition);
    }
    
    // 回退到旧版字段
    if (farmlandTilemap != null)
    {
        return farmlandTilemap.WorldToCell(worldPosition);
    }
    
    // 最后尝试使用 groundTilemap
    if (groundTilemap != null)
    {
        return groundTilemap.WorldToCell(worldPosition);
    }
    
    return Vector3Int.zero;
}
```

### 3.2 LayerTilemaps.GetCellCenterWorld() 修改

**修改方案**：
```csharp
public Vector3 GetCellCenterWorld(Vector3Int cellPosition)
{
    // 优先使用新版字段
    if (farmlandCenterTilemap != null)
    {
        return farmlandCenterTilemap.GetCellCenterWorld(cellPosition);
    }
    
    // 回退到旧版字段
    if (farmlandTilemap != null)
    {
        return farmlandTilemap.GetCellCenterWorld(cellPosition);
    }
    
    // 最后尝试使用 groundTilemap
    if (groundTilemap != null)
    {
        return groundTilemap.GetCellCenterWorld(cellPosition);
    }
    
    return Vector3.zero;
}
```

### 3.3 LayerTilemaps.IsValid() 修改

**修改方案**：
```csharp
public bool IsValid()
{
    // 新版配置：只需要 farmlandCenterTilemap
    if (farmlandCenterTilemap != null)
    {
        return true;
    }
    
    // 旧版配置：需要 farmlandTilemap
    return farmlandTilemap != null;
}
```

### 3.4 GameInputManager.TryTillSoil() 修改

**问题**：依赖 `FarmingManagerNew.Instance`，但场景中未配置

**修改方案**：
```csharp
private bool TryTillSoil(Vector3 worldPosition)
{
    // 直接使用 FarmTileManager
    var farmTileManager = FarmTileManager.Instance;
    if (farmTileManager == null)
    {
        if (showDebugInfo)
            Debug.Log("[GameInputManager] FarmTileManager 未初始化");
        return false;
    }
    
    // 获取当前楼层
    int layerIndex = farmTileManager.GetCurrentLayerIndex(worldPosition);
    var tilemaps = farmTileManager.GetLayerTilemaps(layerIndex);
    if (tilemaps == null)
    {
        if (showDebugInfo)
            Debug.Log($"[GameInputManager] 楼层 {layerIndex} 的 Tilemap 未配置");
        return false;
    }
    
    // 转换为格子坐标
    Vector3Int cellPosition = tilemaps.WorldToCell(worldPosition);
    
    // 检查是否可以耕作
    if (!farmTileManager.CanTillAt(layerIndex, cellPosition))
    {
        if (showDebugInfo)
            Debug.Log($"[GameInputManager] 无法在 {cellPosition} 耕作");
        return false;
    }
    
    // 创建耕地
    bool success = farmTileManager.CreateTile(layerIndex, cellPosition);
    
    if (showDebugInfo)
        Debug.Log($"[GameInputManager] 锄地{(success ? "成功" : "失败")}: {cellPosition}");
    
    return success;
}
```

### 3.5 GameInputManager.TryWaterTile() 修改

**修改方案**：
```csharp
private bool TryWaterTile(Vector3 worldPosition)
{
    // 直接使用 FarmTileManager
    var farmTileManager = FarmTileManager.Instance;
    if (farmTileManager == null)
    {
        if (showDebugInfo)
            Debug.Log("[GameInputManager] FarmTileManager 未初始化");
        return false;
    }
    
    // 获取当前楼层
    int layerIndex = farmTileManager.GetCurrentLayerIndex(worldPosition);
    var tilemaps = farmTileManager.GetLayerTilemaps(layerIndex);
    if (tilemaps == null)
    {
        return false;
    }
    
    // 转换为格子坐标
    Vector3Int cellPosition = tilemaps.WorldToCell(worldPosition);
    
    // 获取当前游戏时间
    float currentHour = TimeManager.Instance != null ? TimeManager.Instance.CurrentHour : 0f;
    
    // 浇水
    bool success = farmTileManager.SetWatered(layerIndex, cellPosition, currentHour);
    
    if (showDebugInfo)
        Debug.Log($"[GameInputManager] 浇水{(success ? "成功" : "失败")}: {cellPosition}");
    
    return success;
}
```

---

## 四、数据流

### 4.1 锄地流程

```
1. 玩家右键点击地面（手持锄头）
   ↓
2. GameInputManager.TryTillSoil(worldPosition)
   ↓
3. FarmTileManager.GetCurrentLayerIndex(worldPosition) → layerIndex
   ↓
4. FarmTileManager.GetLayerTilemaps(layerIndex) → tilemaps
   ↓
5. tilemaps.WorldToCell(worldPosition) → cellPosition
   ↓
6. FarmTileManager.CanTillAt(layerIndex, cellPosition) → true/false
   ↓
7. FarmTileManager.CreateTile(layerIndex, cellPosition)
   ├── 创建 FarmTileData
   ├── 存入 farmTilesByLayer[layerIndex][cellPosition]
   └── 通知 FarmlandBorderManager.OnCenterBlockPlaced()
       ├── 放置中心块 Tile (C0)
       ├── 清除该位置的边界
       └── 更新周围边界
```

### 4.2 浇水流程

```
1. 玩家右键点击耕地（手持水壶）
   ↓
2. GameInputManager.TryWaterTile(worldPosition)
   ↓
3. FarmTileManager.GetCurrentLayerIndex(worldPosition) → layerIndex
   ↓
4. FarmTileManager.GetLayerTilemaps(layerIndex) → tilemaps
   ↓
5. tilemaps.WorldToCell(worldPosition) → cellPosition
   ↓
6. FarmTileManager.SetWatered(layerIndex, cellPosition, waterTime)
   ├── 检查是否已有耕地
   ├── 检查今天是否已浇水
   ├── 更新 FarmTileData.wateredToday = true
   ├── 更新 FarmTileData.moistureState = WetWithPuddle
   └── 添加到 wateredTodayTiles 集合
```

---

## 五、正确性属性

### P1. 锄地正确性

| 编号 | 属性 | 描述 |
|------|------|------|
| P1.1 | 中心块唯一性 | 同一位置只能有一个中心块 |
| P1.2 | 边界一致性 | 边界 Tile 必须与周围中心块分布一致 |
| P1.3 | 地面约束 | 只有有地面 Tile 的位置才能锄地 |

### P2. 浇水正确性

| 编号 | 属性 | 描述 |
|------|------|------|
| P2.1 | 浇水前提 | 只有已耕作的位置才能浇水 |
| P2.2 | 每日限制 | 同一耕地每天只能浇水一次 |
| P2.3 | 状态更新 | 浇水后 moistureState 必须变为 WetWithPuddle |

### P3. 坐标转换正确性

| 编号 | 属性 | 描述 |
|------|------|------|
| P3.1 | 非零坐标 | 有效配置下 WorldToCell 不应返回 (0,0,0)（除非点击位置确实是原点） |
| P3.2 | 双向一致 | WorldToCell 和 GetCellCenterWorld 互为逆操作 |

---

## 六、场景配置要求

### 6.1 必须配置的组件

| 组件 | 位置 | 说明 |
|------|------|------|
| FarmTileManager | MANAGERS 下 | 耕地状态管理 |
| FarmlandBorderManager | MANAGERS 下 | 边界视觉管理 |

### 6.2 必须配置的 Tilemap

| Tilemap | 位置 | 说明 |
|---------|------|------|
| farmlandCenterTilemap | LAYER N/Tilemaps/ | 中心块 |
| farmlandBorderTilemap | LAYER N/Tilemaps/ | 边界装饰 |
| groundTilemap | LAYER N/Tilemaps/ | 地面（用于检测可耕作区域） |

### 6.3 必须配置的 Tile 资源

FarmlandBorderManager 需要配置以下 Tile：
- 中心块：C0（未施肥）、C1（已施肥）
- 边界：U、D、L、R、UD、LR、UL、UR、DL、DR、UDL、UDR、ULR、DLR、UDLR
- 阴影（可选）：SLU、SRU、SLD、SRD

---

## 七、后续规划

### 阶段 2：湿润视觉效果

- 浇水后耕地颜色变深（代码实现，不新增 Sprite）
- 水渍 Tile 放置和消失

### 阶段 3：作物系统

- CropController 独立管理生命周期（参考 TreeController）
- 每个作物 GameObject 有自己的 CropController
- 耕地与作物通过 FarmTileData.cropController 引用绑定
- 可选：CropRegistry 用于全局查询

---

**文档维护者**: AI Assistant  
**最后更新**: 2026-01-26
