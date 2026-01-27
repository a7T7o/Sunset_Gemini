# 丢弃物预制体统一 - 需求文档

## 简介

本文档定义了背包丢弃物与资源节点掉落物统一使用 WorldItem Prefab 的需求，确保所有掉落物都有正确的视觉效果（阴影、动画等）。

## 术语表

- **WorldItemPool**：世界物品对象池，管理掉落物的创建和回收
- **WorldItem Prefab**：预先生成的世界物品预制体，包含 Sprite、Shadow、动画组件等
- **丢弃物**：玩家从背包丢弃的物品
- **掉落物**：资源节点（树木、矿石等）产生的物品
- **defaultWorldPrefab**：WorldItemPool 动态创建的默认预制体（缺少阴影等效果）

---

## 需求

### 需求 1：丢弃物使用正确的 WorldItem Prefab

**用户故事**：作为玩家，我希望从背包丢弃的物品与砍树/采矿掉落的物品外观一致，都有阴影和动画效果。

#### 验收标准

1. WHEN 玩家从背包丢弃物品 THEN WorldItemPool SHALL 优先使用 ItemData.worldPrefab 生成掉落物
2. WHEN ItemData.worldPrefab 不存在 THEN WorldItemPool SHALL 回退到 defaultWorldPrefab
3. WHEN 使用 worldPrefab 生成掉落物 THEN 掉落物 SHALL 显示正确的阴影效果
4. WHEN 使用 worldPrefab 生成掉落物 THEN 掉落物 SHALL 播放正确的浮动动画

### 需求 2：统一掉落物管理机制

**用户故事**：作为开发者，我希望所有掉落物（丢弃物、资源掉落物）都通过 WorldItemPool 统一管理，便于维护和扩展。

#### 验收标准

1. WHEN TreeController 生成掉落物 THEN TreeController SHALL 通过 WorldItemPool 生成
2. WHEN StoneController 生成掉落物 THEN StoneController SHALL 通过 WorldItemPool 生成
3. WHEN 背包丢弃物品 THEN InventoryInteractionManager SHALL 通过 WorldItemPool 生成
4. WHEN WorldItemPool 生成物品 THEN WorldItemPool SHALL 优先查找对应的 worldPrefab

### 需求 3：对象池支持多种预制体

**用户故事**：作为开发者，我希望 WorldItemPool 能够管理多种不同的 WorldItem Prefab，而不仅仅是单一的 defaultWorldPrefab。

#### 验收标准

1. WHEN WorldItemPool 初始化 THEN WorldItemPool SHALL 支持按 itemId 缓存不同的预制体实例
2. WHEN 回收物品到池中 THEN WorldItemPool SHALL 根据物品类型放入对应的子池
3. WHEN 从池中获取物品 THEN WorldItemPool SHALL 优先从对应类型的子池获取

---

## 当前问题分析

### 问题现象

用户报告：从背包丢弃的物品没有阴影，与砍树掉落的物品外观不一致。

### 问题原因

1. **WorldItemPool.Spawn** 使用 `defaultWorldPrefab`（动态创建的简单预制体）
2. `defaultWorldPrefab` 的阴影是程序生成的简单椭圆，与预制体中的阴影不同
3. 没有查找 `ItemData.worldPrefab` 来使用正确的预制体

### 解决方向

1. 修改 `WorldItemPool.Spawn` 方法，优先使用 `ItemData.worldPrefab`
2. 如果 `worldPrefab` 不存在，回退到 `defaultWorldPrefab`
3. 考虑按 itemId 分类管理对象池，提高复用效率
