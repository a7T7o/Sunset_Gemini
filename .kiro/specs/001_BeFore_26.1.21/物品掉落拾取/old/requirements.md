# Requirements Document

## Introduction

本需求文档定义了物品掉落与拾取系统的优化需求。包括简化掉落配置、优化掉落物分散显示、添加拾取飞向玩家动画、以及修复物品无法被拾取的问题。

## Glossary

- **WorldItemPickup**: 世界物品拾取组件，管理物品数据和拾取逻辑
- **WorldItemDrop**: 世界物品掉落动画组件，负责弹跳和浮动动画
- **WorldSpawnService**: 世界物品生成服务，提供统一的物品生成接口
- **WorldItemPool**: 世界物品对象池，管理物品的创建和回收
- **AutoPickupService**: 自动拾取服务，检测玩家周围的可拾取物品
- **DropTable**: 掉落表，定义掉落物品和概率（将被简化）
- **TreeControllerV2**: 树木控制器，管理树木砍伐和掉落

## Requirements

### Requirement 1

**User Story:** As a 开发者, I want 简化树木掉落配置, so that 我可以直接指定掉落的 ItemData SO 和固定数量，而不需要复杂的 DropTable。

#### Acceptance Criteria

1. WHEN TreeControllerV2 配置掉落时 THEN TreeControllerV2 SHALL 提供一个 ItemData 引用字段用于指定掉落物品
2. WHEN TreeControllerV2 配置掉落时 THEN TreeControllerV2 SHALL 提供每个阶段的固定掉落数量配置
3. WHEN 树木被砍倒时 THEN TreeControllerV2 SHALL 根据当前阶段的配置生成对应数量的掉落物

### Requirement 2

**User Story:** As a 玩家, I want 掉落物分散显示, so that 我可以直观地看到掉落物品的数量差异。

#### Acceptance Criteria

1. WHEN 多个物品掉落时 THEN WorldSpawnService SHALL 在树根 Collider 范围内均匀分散生成每个物品
2. WHEN 物品生成位置时 THEN WorldSpawnService SHALL 确保每个物品有独立的随机位置，避免完全重叠
3. WHEN 物品分散生成时 THEN WorldSpawnService SHALL 保持物品在合理范围内形成轻微堆叠效果

### Requirement 3

**User Story:** As a 玩家, I want 掉落物有弹起动画, so that 砍伐时有更好的视觉反馈。

#### Acceptance Criteria

1. WHEN 物品从树木掉落时 THEN WorldItemDrop SHALL 播放从生成位置轻微弹起的动画
2. WHEN 弹起动画播放时 THEN WorldItemDrop SHALL 使物品先向上弹起再落下

### Requirement 4

**User Story:** As a 玩家, I want 拾取物品时有飞向玩家的动画, so that 拾取过程更有动感。

#### Acceptance Criteria

1. WHEN 物品被自动拾取时 THEN AutoPickupService SHALL 触发物品飞向玩家的动画
2. WHEN 飞向动画播放时 THEN WorldItemPickup SHALL 平滑移动到玩家位置
3. WHEN 飞向动画完成时 THEN WorldItemPickup SHALL 完成拾取并添加到背包

### Requirement 5

**User Story:** As a 开发者, I want 修复物品无法被拾取的问题, so that 通过 Ctrl+左键 生成的物品可以正常拾取。

#### Acceptance Criteria

1. WHEN 物品被生成时 THEN WorldItemPickup SHALL 具有正确的 Tag 以被 AutoPickupService 检测
2. WHEN 物品被生成时 THEN WorldItemPickup SHALL 具有 Collider2D 组件以被物理检测
3. WHEN WorldPrefab 未生成时 THEN 系统 SHALL 提供明确的指导让用户生成所需的 WorldPrefab

### Requirement 6

**User Story:** As a 玩家, I want 背包满时物品不被吸引, so that 物品不会因为背包满而消失或卡住。

#### Acceptance Criteria

1. WHEN 物品进入拾取范围时 THEN AutoPickupService SHALL 先检查背包是否有空间容纳该物品
2. WHEN 背包无法容纳物品时 THEN AutoPickupService SHALL 跳过该物品的吸引动画，物品保持原位
3. WHEN 背包有空间时 THEN AutoPickupService SHALL 正常触发飞向玩家动画
4. WHEN 背包从满变为有空位时 THEN AutoPickupService SHALL 能够重新检测并吸引之前跳过的物品
