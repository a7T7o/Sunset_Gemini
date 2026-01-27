# 1_可放置物品SO设计 - 子工作区记忆

## 概述
- **创建日期**: 2025-12-29
- **状态**: ✅ 已完成
- **目标**: 设计可放置物品的 SO 类型体系

## 会话记录

### 会话 1 - 2025-12-29（设计与实现）

**用户需求**:
> 你这里有点多余了吧，这么多个预制体让我填入，如果是树苗的话我只需要给你提交一个预制体就行啊...然后对于可放置物体分为可制作物品的工作台（例如工作台，熔炉，劈柴架等内容），还有就是箱子这种可存储物品的放置物、然后还有交互展示UI提示内容的、交互后只有简单事件触发的类型

**完成任务**:
1. ✅ 创建 PlaceableItemData.cs - 可放置物品基类
2. ✅ 创建 PlaceableItemDataEditor.cs - 自定义编辑器
3. ✅ 优化 SaplingData.cs - 继承 PlaceableItemData，移除 plantableSeason
4. ✅ 创建 WorkstationData.cs - 工作台数据类
5. ✅ 创建 StorageData.cs - 存储容器数据类
6. ✅ 创建 InteractiveDisplayData.cs - 交互展示数据类
7. ✅ 创建 SimpleEventData.cs - 简单事件数据类

**类继承结构**:
```
ItemData (基类)
└── PlaceableItemData (可放置物品基类)
    ├── SaplingData (树苗)
    ├── WorkstationData (工作台)
    ├── StorageData (存储容器)
    ├── InteractiveDisplayData (交互展示)
    └── SimpleEventData (简单事件)
```

**Create 菜单组织**:
```
Create → Farm → Placeable →
  ├── Sapling（树苗）- ID: 1200-1299
  ├── Workstation（工作台）- ID: 1300-1399
  ├── Storage（存储容器）- ID: 1400-1499
  ├── Interactive Display（交互展示）- ID: 1500-1599
  └── Simple Event（简单事件）- ID: 1600-1699
```

**工作台类型**:
- Heating（取暖设施）
- Smelting（冶炼设施）
- Pharmacy（制药设施）
- Sawmill（锯木设施）
- Cooking（烹饪设施）
- Crafting（装备和工具制作设施）

---

### 会话 2 - 2025-12-30（架构决策确认）

**用户需求**:
> 你在思考一下，是否树苗也算可放置物品...把种子和树苗归为一类都作为可放置物品内的种子，你觉得这样可行吗

**最终决策**: ✅ **采用方案 B - 保持分离结构**

**核心区别**:
| 特性 | 种子 (SeedData) | 树苗 (SaplingData) |
|------|----------------|-------------------|
| **所属系统** | 农田系统 (FarmingSystem) | 放置系统 (PlacementManager) |
| **放置目标** | 耕地 Tilemap | 世界任意位置（整数坐标） |
| **生长管理** | FarmingSystem 每日更新 | TreeControllerV2 自动管理 |
| **浇水需求** | ✅ 需要浇水 | ❌ 不需要 |

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 树苗只需填写一个 treePrefab | 简化配置，季节样式由 TreeControllerV2 自动处理 | 2025-12-29 |
| 移除 plantableSeason 字段 | 运行时自动获取当前季节，冬季阻止放置 | 2025-12-29 |
| 种子与树苗保持分离 | 系统边界清晰，放置逻辑本质不同 | 2025-12-30 |
