# 耕地 Tile 系统设计 - 任务列表

**创建日期**: 2026-01-25  
**状态**: 已完成

---

## 任务列表

- [x] 1. 扩展 LayerTilemaps 类
  - [x] 1.1 添加 farmlandCenterTilemap 字段
  - [x] 1.2 添加 farmlandBorderTilemap 字段
  - [x] 1.3 更新 IsValid() 方法

- [x] 2. 创建 FarmlandBorderManager 核心类
  - [x] 2.1 创建单例结构和 Tile 资源引用
  - [x] 2.2 实现 IsCenterBlock() 检测方法
  - [x] 2.3 实现 CheckNeighborCenters() 方法
  - [x] 2.4 实现 SelectBorderTile() 边界选择算法
  - [x] 2.5 实现 PlaceShadowsIfNeeded() 阴影放置逻辑
  - [x] 2.6 实现 UpdateBordersAround() 边界更新方法
  - [x] 2.7 实现 OnCenterBlockPlaced() 公开接口
  - [x] 2.8 实现 OnCenterBlockRemoved() 公开接口

- [x] 3. 集成到现有系统
  - [x] 3.1 修改 FarmTileManager.CreateTile() 调用边界管理器
  - [x] 3.2 添加 RemoveTile() 方法（如果需要）

- [x] 4. 创建配置指南文档

- [x] 5. 创建 Tile 资产
  - [x] 5.1 创建 FarmlandTileCreator.cs 编辑器工具
  - [x] 5.2 通过菜单自动创建 24 个 Tile 资产

- [x] 6. 创建验收指南文档

---

## 任务详情

### 任务 1: 扩展 LayerTilemaps 类

**文件**: `Assets/YYY_Scripts/Farm/Data/LayerTilemaps.cs`

添加两个新的 Tilemap 引用字段，用于存放中心块和边界 Tile。

### 任务 2: 创建 FarmlandBorderManager 核心类

**文件**: `Assets/YYY_Scripts/Farm/FarmlandBorderManager.cs`

这是本系统的核心组件，负责：
- 存储所有边界 Tile 资源引用
- 计算边界类型
- 更新边界 Tile

### 任务 3: 集成到现有系统

修改 `FarmTileManager`，在创建耕地时自动调用边界管理器。

### 任务 4: 创建配置指南文档

创建详细的 Unity 配置指南，包括：
- Tilemap 创建步骤
- Tile 资源创建步骤
- 组件配置步骤

---

**文档维护者**: Kiro  
**最后更新**: 2026-01-25
