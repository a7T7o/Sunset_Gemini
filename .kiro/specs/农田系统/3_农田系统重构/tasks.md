# 农田系统重构 - 任务列表

**创建日期**: 2026-01-24  
**版本**: 1.0  
**状态**: 进行中

---

## 阶段 0：用户前置准备

> ⚠️ 以下任务需要用户在 Unity 编辑器中手动完成

- [ ] U0.1 创建耕地 Rule Tile（配置边界规则、中心变体）
- [ ] U0.2 创建 3 种水渍 Tile
- [ ] U0.3 在 LAYER 1 下创建 Farmland Tilemap
- [ ] U0.4 在 LAYER 1 下创建 WaterPuddle Tilemap
- [ ] U0.5 创建作物 Prefab（添加 SpriteRenderer、CropController）
- [ ] U0.6 创建至少 1 个 SeedData SO 用于测试
- [ ] U0.7 创建对应的 CropData SO

---

## 阶段 1：数据结构重构

### 1.1 创建新数据类

- [x] 1.1.1 创建 `CropInstanceData.cs` - 纯数据类，可序列化
  - 字段：seedDataID, currentStage, plantedDay, grownDays, daysWithoutWater, isWithered, harvestCount, lastHarvestDay, quality
  - **验证**: 需求 4.1-4.9

- [x] 1.1.2 创建 `LayerTilemaps.cs` - 楼层 Tilemap 配置
  - 字段：layerName, farmlandTilemap, waterPuddleTilemap, groundTilemap
  - **验证**: 需求 6.1

- [x] 1.1.3 扩展 `FarmTileData.cs` - 添加楼层索引和水渍变体
  - 新增字段：layerIndex, puddleVariant
  - 修改 crop 字段类型为 CropInstanceData
  - **验证**: 需求 6.1

### 1.2 废弃旧数据类

- [x] 1.2.1 标记 `CropInstance.cs` 为 [Obsolete]
  - 添加注释说明迁移到 CropInstanceData
  - 暂不删除，等待全部迁移完成

---

## 阶段 2：子管理器实现

### 2.1 FarmTileManager

- [x] 2.1.1 创建 `FarmTileManager.cs` 基础结构
  - 单例模式
  - LayerTilemaps[] 配置
  - farmTilesByLayer 字典
  - wateredTodayTiles 集合

- [x] 2.1.2 实现楼层检测逻辑
  - GetCurrentLayerIndex(Vector3 playerPosition)
  - GetLayerTilemaps(int layerIndex)
  - **验证**: 需求 6.5

- [x] 2.1.3 实现耕地 CRUD 操作
  - GetTileData(int layerIndex, Vector3Int cellPosition)
  - CreateTile(int layerIndex, Vector3Int cellPosition)
  - **验证**: 需求 1.1, 1.5

- [x] 2.1.4 实现浇水状态管理
  - SetWatered(int layerIndex, Vector3Int cellPosition, float waterTime)
  - ResetDailyWaterState()
  - **验证**: 需求 3.1-3.6

### 2.2 CropManager

- [x] 2.2.1 创建 `CropManager.cs` 基础结构
  - 单例模式
  - cropPrefab 配置
  - activeCrops 字典

- [x] 2.2.2 实现作物创建/销毁
  - CreateCrop(int layerIndex, Vector3Int cellPosition, SeedData seedData, int plantedDay)
  - DestroyCrop(int layerIndex, Vector3Int cellPosition)
  - **验证**: 需求 2.3, 5.4

- [x] 2.2.3 实现每日生长逻辑
  - ProcessDailyGrowth(FarmTileData tileData)
  - 浇水检查、生长、枯萎逻辑
  - **验证**: 需求 4.1-4.8

- [x] 2.2.4 实现收获逻辑
  - TryHarvest(int layerIndex, Vector3Int cellPosition, out int cropID, out int amount)
  - 可重复收获处理
  - **验证**: 需求 5.1-5.8

### 2.3 FarmVisualManager

- [x] 2.3.1 创建 `FarmVisualManager.cs` 基础结构
  - Tile 资源配置
  - 音效配置
  - 粒子效果配置

- [x] 2.3.2 实现 Tile 视觉更新
  - UpdateTileVisual(LayerTilemaps tilemaps, Vector3Int cellPosition, FarmTileData tileData)
  - 3 种状态切换：Dry, WetWithPuddle, WetDark
  - **验证**: 需求 3.2-3.5

- [x] 2.3.3 实现粒子效果对象池
  - GetParticle(GameObject prefab)
  - ReturnParticle(GameObject particle, float delay)
  - **验证**: 非功能需求 - 性能

- [x] 2.3.4 实现音效和粒子播放
  - PlayTillingEffects(Vector3 worldPosition)
  - PlayWateringEffects(Vector3 worldPosition)
  - PlayHarvestEffects(Vector3 worldPosition)
  - **验证**: 需求 1.7, 3.8, 5.9

---

## 阶段 3：FarmingManager 重写

### 3.1 基础结构

- [x] 3.1.1 重写 `FarmingManagerNew.cs` 基础结构
  - 单例模式
  - 子管理器引用
  - ItemDatabase 获取逻辑
  - **验证**: 需求 7.3

- [x] 3.1.2 实现时间事件订阅
  - 使用静态事件：TimeManager.OnDayChanged, TimeManager.OnHourChanged
  - OnDayChanged 调用 ResetDailyWaterState() 和 ProcessAllCrops()
  - OnHourChanged 调用水渍状态检查
  - **验证**: 需求 7.1

### 3.2 公共接口实现

- [x] 3.2.1 实现 TillSoil(Vector3 worldPosition)
  - 楼层检测
  - 可耕作检查
  - 调用 FarmTileManager.CreateTile()
  - 调用 FarmVisualManager.PlayTillingEffects()
  - **验证**: 需求 1.1-1.7

- [x] 3.2.2 实现 PlantSeed(Vector3 worldPosition, int seedDataID)
  - 楼层检测
  - 耕地检查
  - 季节检查
  - 背包消耗：InventoryService.RemoveItem()
  - 调用 CropManager.CreateCrop()
  - **验证**: 需求 2.1-2.7, 7.2

- [x] 3.2.3 实现 WaterTile(Vector3 worldPosition)
  - 楼层检测
  - 耕地检查
  - 每日一次检查
  - 调用 FarmTileManager.SetWatered()
  - 调用 FarmVisualManager.UpdateTileVisual()
  - **验证**: 需求 3.1-3.8

- [x] 3.2.4 实现 HarvestCrop(Vector3 worldPosition, out int cropID, out int amount)
  - 楼层检测
  - 作物检查
  - 调用 CropManager.TryHarvest()
  - 背包添加：InventoryService.AddItem()
  - **验证**: 需求 5.1-5.9, 7.2

---

## 阶段 4：CropController 重写

- [x] 4.1 重写 `CropController.cs` 基础结构
  - SpriteRenderer 引用
  - SeedData 和 CropInstanceData 引用
  - 统一枯萎颜色：new Color(0.8f, 0.7f, 0.4f)

- [x] 4.2 实现 Initialize(SeedData seedData, CropInstanceData data)
  - 初始化引用
  - 调用 UpdateVisuals()

- [x] 4.3 实现 UpdateVisuals()
  - 根据 currentStage 设置 Sprite
  - 枯萎状态设置颜色
  - **验证**: 需求 4.6, 4.7

- [x] 4.4 实现生长和收获方法
  - Grow() - 增加生长天数，更新阶段
  - SetWithered() - 设置枯萎状态
  - ResetForReHarvest(int reGrowStage) - 可重复收获重置
  - IsMature() / IsWithered() - 状态查询
  - **验证**: 需求 4.8, 5.5

- [x] 4.5 实现成熟闪烁效果（可选）
  - 使用协程代替 Update
  - 或使用 Shader 实现
  - **验证**: 需求 4.9

---

## 阶段 5：系统集成

### 5.1 工具系统集成

- [x] 5.1.1 在 GameInputManager 中添加农田工具处理
  - 锄头（Hoe）→ FarmingManager.TillSoil()
  - 水壶（WateringCan）→ FarmingManager.WaterTile()
  - **验证**: 需求 7.4

- [x] 5.1.2 添加收获交互
  - 右键点击成熟作物 → FarmingManager.HarvestCrop()
  - **验证**: 需求 5.1

### 5.2 放置系统边界

- [x] 5.2.1 在 PlacementManagerV3 中添加 SeedData 检查
  - SeedData 返回 false，引导到农田系统
  - **验证**: 需求 7.5

### 5.3 导航系统集成（可选）

- [ ]* 5.3.1 作物创建/销毁后刷新导航网格
  - NavGrid2D.OnRequestGridRefresh?.Invoke()
  - **验证**: 需求 7.7
  - **说明**: 作物种在耕地上，不阻挡导航，暂不需要实现

---

## 阶段 6：清理和测试

### 6.1 废弃代码清理

- [x] 6.1.1 标记 `CropGrowthSystem.cs` 为废弃
  - 功能已合并到 CropManager
  - 保留文件以兼容旧版 FarmingManager
  - 添加 [Obsolete] 标记

- [x] 6.1.2 标记 `CropInstance.cs` 为废弃（已完成）
  - 功能已替换为 CropInstanceData
  - 已在阶段 1 添加 [Obsolete] 标记

- [x] 6.1.3 更新 FarmingSystemEditor 支持新版管理器
  - 添加对 FarmingManagerNew 的支持
  - 保留对旧版的兼容

### 6.2 单元测试

- [ ] 6.2.1 创建 `FarmingSystemTests.cs`
  - P1.1 耕地唯一性测试
  - P2.1 种植消耗测试
  - P4.2 枯萎条件测试
  - P5.1 收获添加测试
  - **说明**: 待用户完成阶段 0 配置后进行

### 6.3 集成测试

- [ ] 6.3.1 完整流程测试
  - 锄地 → 种植 → 浇水 → 等待生长 → 收获
  - 验证背包物品变化
  - 验证视觉状态变化
  - **说明**: 待用户完成阶段 0 配置后进行

---

## 任务统计

| 阶段 | 任务数 | 预估工时 |
|------|--------|---------|
| 阶段 0 | 7 | 用户操作 |
| 阶段 1 | 4 | 2h |
| 阶段 2 | 11 | 6h |
| 阶段 3 | 6 | 4h |
| 阶段 4 | 5 | 2h |
| 阶段 5 | 4 | 2h |
| 阶段 6 | 5 | 2h |
| **总计** | **42** | **18h** |

---

## 执行顺序建议

1. **用户完成阶段 0**（素材和场景配置）
2. **开发完成阶段 1**（数据结构）
3. **开发完成阶段 2**（子管理器）
4. **开发完成阶段 3**（FarmingManager）
5. **开发完成阶段 4**（CropController）
6. **开发完成阶段 5**（系统集成）
7. **开发完成阶段 6**（清理和测试）

---

## 验收检查清单

- [ ] 锄地功能正常（Rule Tile 自动拼接）
- [ ] 种植功能正常（消耗种子）
- [ ] 浇水功能正常（3 种视觉状态）
- [ ] 作物每日生长正常
- [ ] 未浇水停滞/枯萎正常
- [ ] 收获功能正常（添加到背包）
- [ ] 可重复收获正常
- [ ] 多楼层独立操作正常
- [ ] 季节检查正常
- [ ] 无控制台错误

