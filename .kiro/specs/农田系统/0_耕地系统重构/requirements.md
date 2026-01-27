# 需求文档 - 耕地系统重构

## 介绍

重构农田系统的耕地功能，使用 Rule Tile 实现自动边界拼接，支持阶梯式楼层结构。

## 术语表

- **耕地系统 (Farmland System)**: 管理耕地状态、种植、浇水、收获的系统
- **Rule Tile**: Unity 的自动拼接 Tile，根据相邻格子自动选择正确的 Sprite
- **楼层 (Layer)**: 场景中的阶梯式层级，如 LAYER 1 = 一楼
- **Farmland Tilemap**: 每个楼层独立的耕地 Tilemap
- **WaterPuddle Tilemap**: 每个楼层独立的水渍叠加 Tilemap

---

## 需求 1

**用户故事**: 作为玩家，我想用锄头右键点击地面来开垦耕地，以便种植作物。

### 验收标准

1. WHEN 玩家手持锄头并右键点击地面 THEN 耕地系统 SHALL 在点击位置创建耕地格子
2. WHEN 第一次开垦某个位置 THEN 耕地系统 SHALL 创建一个完整的耕地格子（带边界）
3. WHEN 在已有耕地旁边开垦 THEN 耕地系统 SHALL 自动扩展耕地区域并更新边界显示
4. WHEN 耕地区域扩展 THEN Rule Tile SHALL 自动切换边界格子为中心格子

---

## 需求 2

**用户故事**: 作为玩家，我想看到耕地的边界自动拼接，以便获得视觉上连贯的农田。

### 验收标准

1. WHEN 只有单独一格耕地 THEN Rule Tile SHALL 显示完整边界（上下左右都是边界样式）
2. WHEN 耕地相邻有其他耕地 THEN Rule Tile SHALL 自动切换为中心样式
3. WHEN 耕地区域边缘 THEN Rule Tile SHALL 显示对应方向的边界样式（上/下/左/右）
4. WHEN 中心区域有多格耕地 THEN Rule Tile SHALL 随机选择 2 种中心样式之一

---

## 需求 3

**用户故事**: 作为玩家，我想在不同楼层独立管理耕地，以便在多层场景中种植。

### 验收标准

1. WHEN 玩家在 LAYER 1 开垦 THEN 耕地系统 SHALL 只修改 LAYER 1 的 Farmland Tilemap
2. WHEN 玩家在 LAYER 2 开垦 THEN 耕地系统 SHALL 只修改 LAYER 2 的 Farmland Tilemap
3. WHEN 检测玩家当前楼层 THEN 耕地系统 SHALL 根据玩家位置确定操作的目标 Tilemap

---

## 需求 4

**用户故事**: 作为玩家，我想浇水后看到水渍效果，以便知道哪些耕地已浇水。

### 验收标准

1. WHEN 玩家对耕地浇水 THEN 耕地系统 SHALL 在 WaterPuddle Tilemap 显示水渍
2. WHEN 显示水渍 THEN 耕地系统 SHALL 随机选择 3 种水渍样式之一
3. WHEN 新的一天开始 THEN 耕地系统 SHALL 清除所有水渍显示
4. WHEN 浇水超过 2 小时 THEN 耕地系统 SHALL 将水渍状态切换为深色湿润状态

---

## 需求 5

**用户故事**: 作为开发者，我想配置 Rule Tile 资源，以便系统能正确显示耕地边界。

### 验收标准

1. WHEN 创建 Rule Tile 资源 THEN 开发者 SHALL 配置 9 种边界规则（上、下、左、右、四角、中心）
2. WHEN 配置中心样式 THEN Rule Tile SHALL 支持 2 种随机变体
3. WHEN 配置水渍 Tile THEN 开发者 SHALL 创建 3 个独立的 Tile 资源

---

## 需求 6

**用户故事**: 作为玩家，我想只能在有效地面上开垦耕地，以便保持游戏逻辑合理。

### 验收标准

1. WHEN 点击位置没有地面 Tile THEN 耕地系统 SHALL 阻止开垦并给出提示
2. WHEN 点击位置已有耕地 THEN 耕地系统 SHALL 阻止重复开垦
3. WHEN 点击位置有障碍物 THEN 耕地系统 SHALL 阻止开垦
