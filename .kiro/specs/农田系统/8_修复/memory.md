# 农田系统修复 - 开发记忆

## 模块概述

修复农田系统的多个问题，包括重复锄地、浇水坐标错误、预览系统缺失、SO 字段设计问题等。

## 当前状态

- **完成度**: 0%
- **最后更新**: 2026-01-27
- **状态**: 🚧 问题分析中

---

## 会话 1 - 2026-01-27（问题收集与分析）

**用户需求**:
> 对于当前的耕地还有众多问题需要处理：
> 1. 首先对于当前的耕地你可以重复在同一个地方进行多次锄地，这是不对的交互，对已经锄好的地不能重新再锄一遍
> 2. 其次对于当前的耕地我希望和我们的放置系统一样，可以进行预览，这样允许耕地和不允许耕地都能预览也能提前控制
> 3. 然后对于当前的耕地一直报错，我明明站在耕地上，但是却一直不能正确的浇水，如图也能看到，好像是耕地所站的位置和我所站的位置不是一个参考系，像是坐标不一样
> 4. 对于水壶的交互你可以直接用SO设置的EffectRadius来就好，但是你的这个SO工具专属属性的EffectRadius最低就是1，我想设置到0.4都不行，所以你还需要修改一下SO的内容
> 5. 还有之前就在.kiro\specs\SO设计系统，这个工作区提到过的字段修复，你的tool类型SO还能看到放置配置，消耗品配置，然后plant还能看到装备配置和消耗品配置

---

## 🔴 问题清单

### 问题 1：重复锄地（交互逻辑错误）

**现象**：可以在同一个位置重复锄地

**代码分析**：
查看 `FarmTileManager.CreateTile()` 方法：
```csharp
// 检查是否已存在耕地
if (layerTiles.ContainsKey(cellPosition))
{
    var existingTile = layerTiles[cellPosition];
    if (existingTile.isTilled)
    {
        if (showDebugInfo)
            Debug.Log($"[FarmTileManager] 该位置已有耕地: Layer={layerIndex}, Pos={cellPosition}");
        return false;  // ← 这里应该阻止重复锄地
    }
}
```

**问题根源**：
代码逻辑看起来正确，但 `GameInputManager.TryTillSoil()` 调用的是 `CanTillAt()` 检查，然后调用 `CreateTile()`。需要检查：
1. `CanTillAt()` 是否正确返回 false
2. 是否有其他地方绕过了检查

**我的反思**：
- 代码逻辑看起来正确，但实际运行时出现问题
- 可能是 `CanTillAt()` 和 `CreateTile()` 的检查逻辑不一致
- 需要添加更详细的日志来定位问题

---

### 问题 2：缺少耕地预览系统

**现象**：锄地时没有预览，无法提前知道能否耕作

**用户期望**：
- 参考放置系统 `PlacementPreviewV3` 的实现
- 显示绿色/红色预览表示可以/不可以耕作
- 用户会提供预览 Sprite

**我的反思**：
- 这是一个合理的需求，放置系统已经有成熟的预览实现
- 耕地预览比放置预览简单，只需要 1x1 格子
- 需要创建 `FarmlandPreview` 组件

**设计思路**：
1. 创建 `FarmlandPreview.cs` 组件
2. 在 `GameInputManager` 中检测锄头装备时显示预览
3. 预览跟随鼠标，根据 `CanTillAt()` 结果显示颜色
4. 用户配置预览 Sprite

---

### 问题 3：浇水坐标错误（致命问题）

**现象**：站在耕地上无法浇水，坐标不匹配

**代码分析**：
```csharp
// GameInputManager.TryWaterTile()
Vector3Int cellPosition = tilemaps.WorldToCell(worldPosition);  // worldPosition 是鼠标位置
bool success = farmTileManager.SetWatered(layerIndex, cellPosition, currentHour);
```

**问题根源**：
1. `worldPosition` 是鼠标世界坐标
2. `tilemaps.WorldToCell()` 使用的 Tilemap 可能与创建耕地时使用的不同
3. 如果 `farmlandCenterTilemap` 和 `groundTilemap` 的 Grid 设置不同，坐标转换会不一致

**我的反思**：
- 这是一个严重的坐标系统问题
- 创建耕地和浇水使用的坐标转换必须一致
- 需要检查 `LayerTilemaps.WorldToCell()` 的实现

**LayerTilemaps.WorldToCell() 分析**：
```csharp
public Vector3Int WorldToCell(Vector3 worldPosition)
{
    // 优先使用新版字段
    if (farmlandCenterTilemap != null)
        return farmlandCenterTilemap.WorldToCell(worldPosition);
    
    // 回退到旧版字段
    if (farmlandTilemap != null)
        return farmlandTilemap.WorldToCell(worldPosition);
    
    // 最后尝试使用 groundTilemap
    if (groundTilemap != null)
        return groundTilemap.WorldToCell(worldPosition);
    
    return Vector3Int.zero;
}
```

**可能的问题**：
- 如果 `farmlandCenterTilemap` 未配置，会回退到 `groundTilemap`
- 但 `CreateTile()` 中检查的是 `groundTilemap.GetTile()`
- 两个 Tilemap 的 Grid 设置可能不同

---

### 问题 4：ToolData.effectRadius 范围限制

**现象**：`effectRadius` 最低只能设置为 1，无法设置 0.4

**代码分析**：
```csharp
// ToolData.cs
[Tooltip("作用范围（1=单格，3=3x3范围）")]
[Range(1, 5)]
public int effectRadius = 1;
```

**问题根源**：
- `effectRadius` 是 `int` 类型，且 `[Range(1, 5)]` 限制了最小值为 1
- 用户需要 0.4 这样的小数值

**修复方案**：
1. 将 `effectRadius` 改为 `float` 类型
2. 修改 `[Range]` 属性为 `[Range(0.1f, 5f)]`
3. 更新所有使用 `effectRadius` 的代码

---

### 问题 5：SO 字段设计问题（跨工作区）

**现象**：
- ToolData 显示"放置配置"、"消耗品配置"
- PlantData 显示"装备配置"、"消耗品配置"

**问题根源**：
这是 `.kiro/specs/SO设计系统/0_字段优化重构/` 工作区的遗留问题，基类 `ItemData` 中包含了所有子类的字段。

**我的反思**：
- 这个问题在 SO 设计系统工作区已经有详细的需求文档
- 需要先完成 SO 字段优化重构，再继续农田系统
- 或者可以先用 `[HideInInspector]` 临时隐藏不需要的字段

---

## 🔴 我的深度反思

### 1. 坐标系统混乱

**问题**：
- 创建耕地时使用 `groundTilemap` 检查是否有地面
- 浇水时使用 `farmlandCenterTilemap` 转换坐标
- 两个 Tilemap 可能有不同的 Grid 设置

**教训**：
- 所有耕地操作必须使用同一个 Tilemap 进行坐标转换
- 应该在 `FarmTileManager` 中统一坐标转换逻辑

### 2. 缺少预览系统

**问题**：
- 放置系统有完善的预览，但农田系统没有
- 用户无法提前知道能否耕作

**教训**：
- 任何需要精确位置的操作都应该有预览
- 应该参考放置系统的设计模式

### 3. SO 字段设计债务

**问题**：
- 基类字段过于臃肿
- 子类显示不相关的配置区域

**教训**：
- 应该先完成 SO 字段优化，再开发新功能
- 技术债务会影响后续开发效率

---

## 修复优先级

| 优先级 | 问题 | 影响 | 预估工时 |
|--------|------|------|---------|
| P0 | 浇水坐标错误 | 致命，功能完全不可用 | 1h |
| P0 | 重复锄地 | 严重，交互逻辑错误 | 0.5h |
| P1 | 耕地预览系统 | 重要，用户体验差 | 2h |
| P1 | effectRadius 范围 | 重要，配置受限 | 0.5h |
| P2 | SO 字段设计 | 中等，影响开发体验 | 4h（跨工作区） |

---

## 下一步计划

1. **P0 修复**：
   - 统一坐标转换逻辑
   - 修复重复锄地检查

2. **P1 实现**：
   - 创建 `FarmlandPreview` 组件
   - 修改 `effectRadius` 为 float 类型

3. **P2 处理**：
   - 在 SO 设计系统工作区完成字段优化
   - 或临时使用 `[HideInInspector]` 隐藏

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Farm/FarmTileManager.cs` | 耕地管理器 |
| `Assets/YYY_Scripts/Controller/Input/GameInputManager.cs` | 输入管理器 |
| `Assets/YYY_Scripts/Farm/Data/LayerTilemaps.cs` | 楼层 Tilemap 配置 |
| `Assets/YYY_Scripts/Data/Items/ToolData.cs` | 工具数据 |
| `Assets/YYY_Scripts/Service/Placement/PlacementPreviewV3.cs` | 放置预览（参考） |
