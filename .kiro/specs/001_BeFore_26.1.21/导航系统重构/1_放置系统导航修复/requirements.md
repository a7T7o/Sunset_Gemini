# 需求文档：放置系统导航修复

## 简介

放置系统（PlacementNavigator）在计算导航目标点和到达判断时存在与右键可交互物品导航相同的问题：水平直线移动时，玩家可能停在预览格子侧面而无法触发放置。

## 术语表

- **PlacementNavigator**: 放置导航器，负责计算导航目标点和检测玩家是否到达预览格子
- **预览格子**: 放置物品时显示的预览位置（Bounds）
- **triggerDistance**: 触发放置的距离阈值（默认 0.5f）
- **ClosestPoint**: 玩家中心到预览格子边缘的最近点

## 需求

### 需求 1：统一距离计算方式

**用户故事**: 作为玩家，我希望放置物品时导航能准确到达预览格子旁边，以便顺利完成放置操作。

#### 验收标准

1. WHEN 计算导航目标点时 THE PlacementNavigator SHALL 使用预览格子边缘到玩家中心的最近点（ClosestPoint）
2. WHEN 判断是否到达目标时 THE PlacementNavigator SHALL 使用玩家中心到预览格子边缘的距离
3. WHEN 玩家从任意方向接近预览格子时 THE PlacementNavigator SHALL 正确触发放置

### 需求 2：消除水平移动失效问题

**用户故事**: 作为玩家，我希望从任何方向接近预览格子都能正常放置，不会出现停在侧面无法放置的情况。

#### 验收标准

1. WHEN 玩家从左侧水平接近预览格子时 THE System SHALL 正确触发放置
2. WHEN 玩家从右侧水平接近预览格子时 THE System SHALL 正确触发放置
3. WHEN 玩家从上方垂直接近预览格子时 THE System SHALL 正确触发放置
4. WHEN 玩家从下方垂直接近预览格子时 THE System SHALL 正确触发放置
5. WHEN 玩家从对角线方向接近预览格子时 THE System SHALL 正确触发放置
