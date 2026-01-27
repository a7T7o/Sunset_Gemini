# 农田系统深度重构 - 需求文档

**创建日期**: 2026-01-26  
**更新日期**: 2026-01-26（根据设计审视更新）  
**工作区**: `.kiro/specs/农田系统/6_农田系统深度重构/`

---

## 背景

经过深度反思和设计审视（详见 `深度分析报告.md` 和 `设计审视与重新思考.md`），我们确定了以下核心设计原则：

1. **耕地中心块不需要独立控制器** - 只是 Tilemap 上的 Tile，状态存储在 FarmTileData 中
2. **分层架构** - 数据层(FarmTileData) → 管理层(FarmTileManager/FarmlandBorderManager) → 实体层(CropController)
3. **简化调用链** - GameInputManager 直接调用 FarmTileManager，不经过 FarmingManagerNew
4. **作物系统后续设计** - 参考 TreeController 模式，CropController 独立管理生命周期

---

## 需求 1：修复 LayerTilemaps 坐标转换（P0 致命）

### 用户故事

作为玩家，我希望锄地时能够正确识别我点击的位置，而不是所有操作都发生在 (0,0,0)。

### 问题分析

当前 `LayerTilemaps.WorldToCell()` 使用旧版字段 `farmlandTilemap`，但场景中配置的是新版字段 `farmlandCenterTilemap`，导致坐标转换返回 (0,0,0)。

### 验收标准

- [x] 1.1 LayerTilemaps.WorldToCell() 优先使用新版字段 farmlandCenterTilemap
- [x] 1.2 如果新版字段为空，回退到旧版字段 farmlandTilemap
- [x] 1.3 如果都为空，使用 groundTilemap 作为最后回退
- [x] 1.4 LayerTilemaps.GetCellCenterWorld() 同样优先使用新版字段
- [x] 1.5 LayerTilemaps.IsValid() 支持新版配置（只配置 farmlandCenterTilemap 也算有效）

---

## 需求 2：修复 GameInputManager 调用链（P0 致命）

### 用户故事

作为玩家，我希望使用锄头右键点击地面时能够锄地，而不是看到 "FarmingManagerNew 未初始化" 的错误。

### 问题分析

当前 `GameInputManager.TryTillSoil()` 依赖 `FarmingManagerNew.Instance`，但场景中未配置该组件。根据简化架构，应该直接调用 `FarmTileManager.Instance`。

### 验收标准

- [x] 2.1 GameInputManager.TryTillSoil() 直接调用 FarmTileManager.Instance
- [x] 2.2 GameInputManager.TryWaterTile() 直接调用 FarmTileManager.Instance
- [x] 2.3 不再出现 "FarmingManagerNew 未初始化" 的错误日志
- [x] 2.4 场景中只需要配置 FarmTileManager 和 FarmlandBorderManager

---

## 需求 3：锄地功能正常工作（P0 核心）

### 用户故事

作为玩家，我希望使用锄头右键点击地面时：
1. 在点击位置生成中心块 Tile
2. 周围自动生成边界装饰 Tile
3. 如果该位置已有耕地，不重复创建

### 验收标准

- [ ] 3.1 锄地后在 farmlandCenterTilemap 上生成中心块 Tile（C0 未施肥）
- [ ] 3.2 锄地后在 farmlandBorderTilemap 上生成正确的边界 Tile
- [ ] 3.3 边界 Tile 根据周围中心块分布自动选择正确样式（U/D/L/R/UD/LR/UL/UR/DL/DR/UDL/UDR/ULR/DLR/UDLR）
- [ ] 3.4 对角线方向有中心块时，生成阴影 Tile（SLU/SRU/SLD/SRD）
- [ ] 3.5 已有耕地的位置不能重复锄地
- [ ] 3.6 没有地面 Tile 的位置不能锄地

---

## 需求 4：浇水功能正常工作（P1 重要）

### 用户故事

作为玩家，我希望使用水壶右键点击耕地时：
1. 耕地的浇水状态被更新
2. 今天已浇水的耕地不能重复浇水

### 验收标准

- [ ] 4.1 浇水后 FarmTileData.wateredToday 变为 true
- [ ] 4.2 浇水后 FarmTileData.moistureState 变为 WetWithPuddle
- [ ] 4.3 今天已浇水的耕地不能重复浇水
- [ ] 4.4 非耕地位置不能浇水

---

## 需求 5：代码清理（P2 可选）

### 用户故事

作为开发者，我希望代码库保持简洁，旧版字段有明确的注释说明。

### 验收标准

- [ ] 5.1 LayerTilemaps 中的旧版字段（farmlandTilemap、waterPuddleTilemap）添加注释说明
- [ ] 5.2 FarmingManagerNew 暂时保留但不在 GameInputManager 中使用
- [ ] 5.3 FarmVisualManager 暂时保留但不在当前流程中使用
- [ ] 5.4 CropManager 暂时保留，作物系统后续参考 TreeController 重新设计

---

## 资源清单（用户已确认）

### 中心块（2 个）

| 编号 | 名称 | 用途 |
|------|------|------|
| C0 | 未施肥中心块 | 锄地后默认状态 |
| C1 | 已施肥中心块 | 施肥后状态 |

### 边界（15 个）

| 类型 | 数量 | 说明 |
|------|------|------|
| 单方向 | 4 | U, D, L, R |
| 双方向对边 | 2 | UD, LR |
| 双方向相邻 | 4 | UL, UR, DL, DR |
| 三方向 | 4 | UDL, UDR, ULR, DLR |
| 四方向 | 1 | UDLR |

### 阴影（4 个）

| 编号 | 位置 |
|------|------|
| SLU | 左上角 |
| SRU | 右上角 |
| SLD | 左下角 |
| SRD | 右下角 |

### 水渍（3 个）

| 编号 | 用途 |
|------|------|
| W0 | 水渍变体 1 |
| W1 | 水渍变体 2 |
| W2 | 水渍变体 3 |

---

## 简化后的架构

```
GameInputManager.TryTillSoil()
  ↓ 直接调用
FarmTileManager.CreateTile()
  ↓ 通知
FarmlandBorderManager.OnCenterBlockPlaced()
  ↓ 更新
Tilemap.SetTile() (Center + Border)
```

**暂不使用的组件**：
- FarmingManagerNew（过度设计的协调者）
- FarmVisualManager（为旧系统设计）
- CropManager（作物系统后续再设计）

---

## 优先级

| 需求 | 优先级 | 说明 |
|------|--------|------|
| 需求 1 | P0 | 致命问题，必须修复 |
| 需求 2 | P0 | 致命问题，必须修复 |
| 需求 3 | P0 | 核心功能 |
| 需求 4 | P1 | 重要功能 |
| 需求 5 | P2 | 代码清理 |

---

**文档维护者**: AI Assistant  
**最后更新**: 2026-01-26
