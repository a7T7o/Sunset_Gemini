---
inclusion: manual
priority: P3
keywords: [产品, 项目介绍, 概述]
lastUpdated: 2025-01-09
archivedFrom: .kiro/steering/product.md
archivedDate: 2025-01-09
---

## 中文概述（面向开发者本人）

- **最后更新**：2025-01-09
- **项目名称**：Sunset（日落）
- **类型**：2D 农场模拟游戏（类星露谷）
- **引擎**：Unity 6 (6000.0.62f1)
- **核心玩法**：
  - 时间系统（日夜循环、四季变化）
  - 农田系统（耕地、种植、浇水、收获）
  - 资源采集（砍树、挖矿）
  - 战斗系统（基础怪物 AI）
  - 背包系统（物品管理、快捷栏）
  - 导航系统（A* 寻路、动态障碍物）
  - NPC 交互（对话、任务）
- **当前完成度**：约 40%
- **当前阶段重点**：农田系统开发中

> **注意**：此为项目初期概述，最新状态请查看各模块的 memory.md 或询问我。

---

# Product Overview

**Project Name**: Sunset  
**Type**: 2D Farming Simulation Game  
**Engine**: Unity 6000.0.62f1  
**Language**: C# with Chinese documentation

## Core Concept

Sunset is a Stardew Valley-inspired 2D farming simulation game featuring:

- **Time System**: Dynamic day/night cycle with seasons (Spring, Summer, Autumn, Winter)
- **Farming Mechanics**: Tilling, planting, watering, and harvesting crops
- **Resource Gathering**: Chopping trees, mining rocks for materials
- **Combat System**: Basic enemy AI with health and weapon systems
- **Inventory Management**: Item database, backpack system with quick slots
- **World Navigation**: A* pathfinding with dynamic obstacle detection
- **NPC Interaction**: Dialogue system and quest framework

## Development Status

Currently in Phase 1 (40% complete) with core framework established:
- ✅ Time and season systems
- ✅ Player movement and animation synchronization
- ✅ Item and inventory systems
- ✅ UI framework with hotkey management
- ✅ World-scale navigation system
- ✅ Tree growth and seasonal changes
- 🚧 Farming system (in progress)
- 🚧 Combat system (planned)
- 🚧 NPC and quest systems (planned)

## Key Features

- **Seasonal Vegetation**: Trees and plants change appearance across 5 vegetation seasons
- **Frame-Perfect Animation Sync**: Zero-frame delay between player and tool animations
- **Dynamic World**: Trees grow, weather affects plants, obstacles update navigation
- **Comprehensive UI**: Multi-panel system with unified hotkey management
- **Event-Driven Architecture**: Decoupled systems communicating via events
