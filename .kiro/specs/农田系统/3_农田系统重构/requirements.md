# 农田系统重构 - 需求文档

**创建日期**: 2026-01-24  
**版本**: 1.0  
**状态**: 待审核

---

## 背景

根据 `2_农田系统锐评/锐评报告.md` 的分析结论：
- 现有农田系统与项目其他成熟系统（背包、放置、工具、导航、遮挡、时间）的集成度为零
- 代码质量评分 3/5，系统适配评分 1/5
- 修复成本 ≈ 重写成本，但修复后会得到"缝合怪"
- **结论：现有代码应该直接废弃，从零重构**

---

## 需求 1：耕地系统

### 用户故事

作为玩家，我希望能够使用锄头在可耕作的地面上创建耕地，以便后续种植作物。

### 验收标准

1.1 玩家装备锄头后，右键点击可耕作地面，在该位置创建耕地 Tile  
1.2 耕地使用 Rule Tile 自动拼接边界（单独一格显示完整边界，相邻有耕地自动切换为中心样式）  
1.3 中心样式有 2 种变体，随机选择  
1.4 不可耕作的位置（无地面 Tile、已有耕地、有障碍物）无法创建耕地  
1.5 每个楼层（LAYER 1/2/3）有独立的 Farmland Tilemap  
1.6 耕地操作消耗种子（如果精力系统存在）  
1.7 耕地操作播放音效和粒子效果  

### 依赖

- 工具系统（GameInputManager、PlayerToolHitEmitter）
- 导航系统（NavGrid2D）- 用于检测障碍物
- 时间系统（TimeManager）- 用于记录操作时间

---

## 需求 2：种植系统

### 用户故事

作为玩家，我希望能够在耕地上种植种子，种子会消耗背包中的物品并创建作物实例。

### 验收标准

2.1 玩家选中种子物品后，右键点击已耕作的耕地，种植种子  
2.2 种植成功后，从背包中移除 1 个种子  
2.3 种植成功后，在耕地位置创建作物 GameObject  
2.4 作物显示第一阶段的 Sprite  
2.5 季节检查：只有在正确季节才能种植（AllSeason 种子除外）  
2.6 已有作物的耕地无法再次种植  
2.7 种植操作播放音效  

### 依赖

- 背包系统（InventoryService）- 消耗种子
- 物品数据库（ItemDatabase）- 获取 SeedData
- 时间系统（TimeManager）- 获取当前季节

---

## 需求 3：浇水系统

### 用户故事

作为玩家，我希望能够使用水壶给耕地浇水，浇水后土壤显示湿润效果，并影响作物生长。

### 验收标准

3.1 玩家装备水壶后，右键点击耕地，给该耕地浇水  
3.2 浇水后立即显示水渍效果（WetWithPuddle 状态）  
3.3 浇水 2 小时后，水渍消失，显示深色湿润效果（WetDark 状态）  
3.4 第二天开始时，所有耕地恢复干燥状态（Dry 状态）  
3.5 水渍有 3 种样式变体，随机选择  
3.6 每块耕地每天只能浇水一次  
3.7 浇水操作消耗精力（如果精力系统存在）  
3.8 浇水操作播放音效和粒子效果  

### 依赖

- 工具系统（GameInputManager）
- 时间系统（TimeManager）- OnHourChanged、OnDayChanged 事件

---

## 需求 4：作物生长系统

### 用户故事

作为玩家，我希望种植的作物能够随时间生长，浇水的作物生长更快，长期不浇水的作物会枯萎。

### 验收标准

4.1 每天开始时（OnDayChanged），检查所有作物的生长状态  
4.2 昨天浇过水的作物，生长天数 +1  
4.3 昨天未浇水的作物，连续未浇水天数 +1  
4.4 连续 2 天未浇水的作物，生长停滞（不增加生长天数）  
4.5 连续 3 天未浇水的作物，枯萎（isWithered = true）  
4.6 枯萎的作物显示枯萎颜色（统一为 `new Color(0.8f, 0.7f, 0.4f)`）  
4.7 作物根据生长天数自动切换 Sprite（线性分布到各阶段）  
4.8 作物达到最大生长天数后，进入成熟状态  
4.9 成熟作物显示闪烁效果（可选）  

### 依赖

- 时间系统（TimeManager）- OnDayChanged 事件

---

## 需求 5：收获系统

### 用户故事

作为玩家，我希望能够收获成熟的作物，获得对应的作物物品添加到背包。

### 验收标准

5.1 玩家右键点击成熟作物，收获作物  
5.2 收获成功后，根据 SeedData.harvestAmountRange 随机获得作物数量  
5.3 收获的作物（CropData）添加到背包  
5.4 普通作物收获后，作物 GameObject 被销毁  
5.5 可重复收获作物（isReHarvestable = true）收获后，回退到指定生长阶段，可再次生长  
5.6 可重复收获作物有最大收获次数限制（maxHarvestCount，0 = 无限）  
5.7 枯萎的作物无法收获  
5.8 未成熟的作物无法收获  
5.9 收获操作播放音效  

### 依赖

- 背包系统（InventoryService）- 添加作物
- 物品数据库（ItemDatabase）- 获取 CropData

---

## 需求 6：多楼层支持

### 用户故事

作为玩家，我希望在不同楼层（LAYER 1/2/3）的耕地是独立的，互不影响。

### 验收标准

6.1 每个楼层有独立的 Farmland Tilemap 和 WaterPuddle Tilemap  
6.2 玩家在哪个楼层，就只能操作该楼层的耕地  
6.3 不同楼层的作物独立生长  
6.4 FarmingManager 支持 LayerTilemaps 数组配置  
6.5 自动检测玩家当前所在楼层  

### 依赖

- 场景层级结构（layers.md 规范）

---

## 需求 7：系统集成

### 用户故事

作为开发者，我希望农田系统与现有系统正确集成，遵循项目规范。

### 验收标准

7.1 使用静态事件订阅时间系统：`TimeManager.OnDayChanged`、`TimeManager.OnHourChanged`  
7.2 通过 InventoryService 访问背包：`AddItem()`、`RemoveItem()`  
7.3 通过 InventoryService.Database 访问 ItemDatabase  
7.4 工具交互通过 GameInputManager 触发  
7.5 种子（SeedData）由农田系统管理，与树苗（SaplingData）分离  
7.6 PlacementManagerV3 对 SeedData 返回 false，引导到农田系统  
7.7 作物创建/销毁后刷新导航网格（如果作物阻挡移动）  

### 依赖

- 所有现有系统

---

## 非功能需求

### 性能要求

- OnHourChanged 遍历优化：只遍历今天浇水的耕地，而非所有耕地
- OnDayChanged 遍历优化：只更新状态变化的耕地视觉
- 粒子效果使用对象池，避免频繁 Instantiate/Destroy

### 代码质量要求

- 单一职责：FarmingManager 只做协调，具体逻辑拆分到子模块
- 数据与逻辑分离：CropInstanceData 纯数据，CropController 处理逻辑
- 统一枯萎颜色：`new Color(0.8f, 0.7f, 0.4f)`
- 删除重复代码：CropInstance.UpdateVisuals() 删除，统一由 CropController 处理

---

## 用户操作前置条件

| 序号 | 前置条件 | 说明 |
|------|---------|------|
| U1 | Rule Tile 配置 | 创建耕地 Rule Tile，配置边界规则 |
| U2 | 水渍 Tile 创建 | 创建 3 种水渍 Tile |
| U3 | 场景 Tilemap 结构 | 在每个 LAYER 下创建 Farmland 和 WaterPuddle Tilemap |
| U4 | 作物 Prefab 配置 | 创建作物 Prefab，添加必要组件 |
| U5 | 种子 SO 创建 | 创建至少 1 个 SeedData SO 用于测试 |
| U6 | 作物 SO 创建 | 创建对应的 CropData SO |

---

## 废弃文件清单

以下文件将在重构完成后废弃：

| 文件 | 处理方式 |
|------|---------|
| `FarmingManager.cs` | 重写 |
| `CropGrowthSystem.cs` | 废弃，合并到新 FarmingManager |
| `CropController.cs` | 保留结构，重写逻辑 |
| `CropInstance.cs` | 废弃，重新设计数据结构 |
| `FarmTileData.cs` | 保留结构，扩展字段 |
| `SoilMoistureState.cs` | 保留 |

