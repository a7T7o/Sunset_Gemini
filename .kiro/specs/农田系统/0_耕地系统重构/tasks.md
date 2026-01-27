# 实现计划 - 耕地系统重构

## 阶段概览

| 阶段 | 内容 | 状态 |
|------|------|------|
| 阶段 0 | 素材准备和 Rule Tile 配置 | ⏳ 用户操作 |
| 阶段 1 | 场景 Tilemap 结构搭建 | ⏳ 用户操作 |
| 阶段 2 | FarmingManager 重构 | ⏳ 待开始 |
| 阶段 3 | 锄头工具集成 | ⏳ 待开始 |
| 阶段 4 | 水渍系统实现 | ⏳ 待开始 |

---

## 阶段 0：素材准备和 Rule Tile 配置（用户操作）

- [ ] 0.1 导入耕地边界 Sprite（上、下、左、右、中心 A、中心 B）
  - 放入 `Assets/Sprites/Farm/Farmland/`
  - 设置 Pixels Per Unit、Filter Mode 等
  - _需求: 5.1_

- [ ] 0.2 导入水渍 Sprite（3 种样式）
  - 放入 `Assets/Sprites/Farm/WaterPuddle/`
  - _需求: 5.3_

- [ ] 0.3 创建 Farmland Rule Tile
  - 右键 → Create → 2D → Tiles → Rule Tile
  - 命名为 `Farmland_RuleTile`
  - 保存到 `Assets/111_Data/Tiles/`
  - _需求: 5.1_

- [ ] 0.4 配置 Rule Tile 规则
  - 添加上边界规则
  - 添加下边界规则
  - 添加左边界规则
  - 添加右边界规则
  - 添加中心随机规则（2 种变体）
  - _需求: 5.2_

- [ ] 0.5 创建 3 个水渍 Tile
  - 创建 `WaterPuddle_Tile_A`
  - 创建 `WaterPuddle_Tile_B`
  - 创建 `WaterPuddle_Tile_C`
  - _需求: 5.3_

---

## 阶段 1：场景 Tilemap 结构搭建（用户操作）

- [ ] 1.1 在 LAYER 1 创建 Farmland Tilemap
  - 在 `LAYER 1/Tilemaps` 下创建
  - 设置 Sorting Layer 为 `Layer 1`
  - 设置 Order in Layer 为 1
  - _需求: 3.1_

- [ ] 1.2 在 LAYER 1 创建 WaterPuddle Tilemap
  - 在 `LAYER 1/Tilemaps` 下创建
  - 设置 Sorting Layer 为 `Layer 1`
  - 设置 Order in Layer 为 2
  - _需求: 4.1_

- [ ] 1.3 为其他楼层重复创建 Tilemap
  - LAYER 2 的 Farmland 和 WaterPuddle
  - LAYER 3 的 Farmland 和 WaterPuddle（如有）
  - _需求: 3.2_

- [ ] 1.4 手动测试 Rule Tile
  - 使用 Tile Palette 在 Farmland Tilemap 绘制
  - 验证边界自动拼接效果
  - _需求: 2.1, 2.2, 2.3_

---

## 阶段 2：FarmingManager 重构

- [ ] 2.1 创建 LayerTilemaps 数据结构
  - 定义 LayerTilemaps 类
  - 包含 layerName、farmlandTilemap、waterPuddleTilemap、groundTilemap
  - _需求: 3.1, 3.2_

- [ ] 2.2 重构 FarmingManager 支持多楼层
  - 添加 LayerTilemaps[] 数组
  - 添加 GetLayerTilemaps(string layerName) 方法
  - 添加 GetCurrentLayerTilemaps() 方法
  - _需求: 3.1, 3.2, 3.3_

- [ ] 2.3 实现楼层检测逻辑
  - 根据玩家 Transform 父级检测当前楼层
  - 或根据玩家位置检测所属楼层
  - _需求: 3.3_

- [ ] 2.4 重构 TillSoil 方法
  - 获取当前楼层的 Tilemap
  - 使用 Rule Tile 设置耕地
  - 更新 FarmTileData 数据
  - _需求: 1.1, 1.2, 1.3, 1.4_

- [ ]* 2.5 编写属性测试：楼层隔离性
  - **属性 2: 楼层隔离性**
  - **验证: 需求 3.1, 3.2**

---

## 阶段 3：锄头工具集成

- [ ] 3.1 修改锄头交互逻辑
  - 检测右键点击
  - 调用 FarmingManager.TillSoil()
  - _需求: 1.1_

- [ ] 3.2 实现开垦前置条件检查
  - 检查点击位置是否有地面 Tile
  - 检查点击位置是否已有耕地
  - 检查点击位置是否有障碍物
  - _需求: 6.1, 6.2, 6.3_

- [ ] 3.3 添加开垦失败提示
  - 无地面时显示提示
  - 有障碍物时显示提示
  - _需求: 6.1, 6.3_

- [ ]* 3.4 编写属性测试：开垦前置条件
  - **属性 4: 开垦前置条件**
  - **验证: 需求 6.1, 6.2, 6.3**

---

## 阶段 4：水渍系统实现

- [ ] 4.1 重构 WaterTile 方法
  - 获取当前楼层的 WaterPuddle Tilemap
  - 随机选择 3 种水渍 Tile 之一
  - 设置水渍 Tile
  - _需求: 4.1, 4.2_

- [ ] 4.2 实现每日水渍清除
  - 在 OnDayChanged 事件中清除所有水渍
  - 遍历所有楼层的 WaterPuddle Tilemap
  - _需求: 4.3_

- [ ] 4.3 实现水渍状态切换
  - 浇水 2 小时后切换为深色状态
  - 可选：使用不同的 Tile 或颜色
  - _需求: 4.4_

- [ ]* 4.4 编写属性测试：水渍与耕地对应
  - **属性 3: 水渍与耕地对应**
  - **验证: 需求 4.1**

---

## 阶段 5：检查点

- [ ] 5. 确保所有测试通过，询问用户是否有问题
  - 验证 Rule Tile 边界拼接
  - 验证多楼层独立操作
  - 验证水渍显示和清除
