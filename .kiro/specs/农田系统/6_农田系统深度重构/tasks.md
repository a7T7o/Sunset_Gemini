# 农田系统深度重构 - 任务列表

**创建日期**: 2026-01-26  
**更新日期**: 2026-01-26（根据设计审视更新）  
**工作区**: `.kiro/specs/农田系统/6_农田系统深度重构/`

---

## 阶段 1：修复 LayerTilemaps（P0 致命问题）

- [x] 1.1 修改 LayerTilemaps.WorldToCell() 优先使用新版字段 farmlandCenterTilemap
- [x] 1.2 修改 LayerTilemaps.GetCellCenterWorld() 优先使用新版字段
- [x] 1.3 修改 LayerTilemaps.IsValid() 支持新版配置
- [x] 1.4 为旧版字段添加注释说明（farmlandTilemap、waterPuddleTilemap）

---

## 阶段 2：修复 GameInputManager 调用链（P0 致命问题）

- [x] 2.1 修改 GameInputManager.TryTillSoil() 直接调用 FarmTileManager.Instance
- [x] 2.2 修改 GameInputManager.TryWaterTile() 直接调用 FarmTileManager.Instance
- [x] 2.3 添加正确的楼层检测和坐标转换逻辑
- [x] 2.4 添加 TimeManager 集成获取当前游戏时间

---

## 阶段 3：代码清理（P2 可选）

- [ ] 3.1 为 FarmingManagerNew 添加注释说明（暂不使用）
- [ ] 3.2 为 FarmVisualManager 添加注释说明（暂不使用）
- [ ] 3.3 为 CropManager 添加注释说明（后续参考 TreeController 重新设计）

---

## 验收清单

### 锄地功能

- [ ] 点击地面能生成中心块 Tile（在 farmlandCenterTilemap 上）
- [ ] 边界能正确计算和放置（在 farmlandBorderTilemap 上）
- [ ] 不报 "FarmingManagerNew 未初始化" 错误
- [ ] 坐标转换正确（不是所有操作都在 (0,0,0)）

### 浇水功能

- [ ] 点击耕地能更新 wateredToday 状态
- [ ] 点击耕地能更新 moistureState 为 WetWithPuddle
- [ ] 今天已浇水的耕地不能重复浇水

### 场景配置

- [ ] 场景中只需要 FarmTileManager 和 FarmlandBorderManager
- [ ] 不需要配置 FarmingManagerNew

---

## 预估工时

| 阶段 | 任务数 | 预估时间 |
|------|--------|---------|
| 阶段 1 | 4 | 0.5h |
| 阶段 2 | 4 | 0.5h |
| 阶段 3 | 3 | 0.5h |
| **总计** | **11** | **1.5h** |

---

**文档维护者**: AI Assistant  
**最后更新**: 2026-01-26
