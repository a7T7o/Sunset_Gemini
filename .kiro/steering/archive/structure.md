---
inclusion: manual
priority: P3
keywords: [项目结构, 文件夹, 命名规范, 场景结构]
lastUpdated: 2025-01-09
archivedFrom: .kiro/steering/structure.md
archivedDate: 2025-01-09
---

## 中文概述（面向开发者本人）

- **最后更新**：2025-01-09
- **根目录结构**：
  - `Assets/` - Unity 资源（脚本、精灵、预制体、场景）
  - `Docx/` - 设计文档和技术方案
  - `Packages/` - 包清单
  - `ProjectSettings/` - Unity 项目设置
- **脚本组织**（`Assets/Scripts/`）：
  - `Service/` - 核心管理器和服务
  - `Controller/Player/` - 玩家移动和输入
  - `Data/` - 数据结构（物品、配方、枚举）
  - `Farm/` - 农田系统
  - `UI/` - 所有 UI 组件
  - `World/` - 世界物体（拾取物、生成器）
  - `Utils/` - 工具类和扩展
- **命名规范**：
  - 类名：PascalCase（如 `TimeManager`）
  - 私有字段：camelCase（如 `currentTime`）
  - 常量：UPPER_CASE（如 `MAX_HEALTH`）
  - GameObject：PascalCase + 下划线（如 `Tree_M1_00`）

> **注意**：此为目录结构概述，详细规范请参考对应的 steering 规则（如 `layers.md`、`coding-standards.md`）。

---

# Project Structure

## Root Folders

```
Sunset/
├── Assets/              # Unity assets (scripts, sprites, prefabs, scenes)
├── Docx/               # Design documents and technical solutions
├── Plan/               # Planning documents
├── Library/            # Unity cache (ignored)
├── Logs/               # Unity logs (ignored)
├── Packages/           # Package manifest
├── ProjectSettings/    # Unity project settings
├── Temp/               # Temporary files (ignored)
└── UserSettings/       # User-specific settings (ignored)
```

## Assets Organization

### Scripts (`Assets/Scripts/`)

**Core Systems**:
- `Service/` - Core managers and services
  - `TimeManager.cs` - Time and day/night cycle
  - `SeasonManager.cs` - Season management
  - `WeatherSystem.cs` - Weather effects
  - `DynamicSortingOrder.cs` - Sprite sorting

**Player**:
- `Controller/Player/` - Player movement and input
- `Anim/Player/` - Player animation controllers

**Game Systems**:
- `Data/` - Data structures (items, recipes, enums)
  - `Items/` - Item ScriptableObjects
  - `Database/` - Item database
  - `Recipes/` - Crafting recipes
- `Farm/` - Farming system (crops, tilling, watering)
- `UI/` - All UI components
  - `Inventory/` - Inventory UI
  - `Tabs/` - Tab panel management
  - `Toolbar/` - Quick toolbar
- `World/` - World objects (pickups, spawning)

**Utilities**:
- `Utils/` - Helper classes and extensions
- `Service/Navigation/` - A* pathfinding system
- `Service/Rendering/` - Rendering utilities

### Sprites (`Assets/Sprites/`)

```
Sprites/
├── Anim/           # Animation sprite sheets
├── House/          # Building sprites
├── Sheet/          # Sprite sheets
├── UI/             # UI icons and elements
├── Generated/      # Auto-generated sprites
└── Pivot_Example/  # Pivot alignment examples
```

### Prefabs (`Assets/Prefabs/`)

```
Prefabs/
├── Farm/           # Farming-related prefabs
├── Tree/           # Tree prefabs (with growth stages)
├── Rock/           # Rock and mineral prefabs
├── House/          # Building prefabs
├── UI/             # UI panel prefabs
└── Dungeon props/  # Dungeon/combat props
```

### Animations (`Assets/Animations/`)

```
Animations/
├── 0/              # Base animations
├── Controller/     # Animation controllers
└── Type/           # Animation types/categories
```

### Data (`Assets/Data/`)

```
Data/
├── Database/       # ScriptableObject databases
├── Items/          # Item data assets
└── Recipes/        # Recipe data assets
```

### Editor Tools (`Assets/Editor/`)

Custom editor scripts for workflow automation:
- Batch operations (prefabs, sprites, animations)
- Animation setup tools
- Tilemap utilities
- Debug visualizers

## Documentation (`Docx/`)

```
Docx/
├── Plan/           # Design documents
├── Solutions/      # Technical solution documents
├── Summary/        # Phase completion reports
└── HD/             # Handoff documents
```

## Naming Conventions

### Scripts
- **PascalCase** for classes: `TimeManager`, `PlayerController`
- **camelCase** for private fields: `currentTime`, `isMoving`
- **PascalCase** for public properties: `CurrentSeason`, `IsActive`
- **UPPER_CASE** for constants: `MAX_HEALTH`, `DEFAULT_SPEED`

### GameObjects
- **PascalCase** with underscores: `Tree_M1_00`, `Player_Character`
- Prefixes for organization: `UI_`, `NPC_`, `Enemy_`

### Assets
- **Descriptive names**: `Player_Idle_Down_0`, `Tree_Spring_Large`
- **Numbered sequences**: `_0`, `_1`, `_2` for animation frames
- **Quality suffix**: `_0_High`, `_0_Low` for quality variants

## Scene Structure

> **详细的场景层级结构请参考 `layers.md`，此处仅为概述。**

### Sorting Layers (Bottom to Top)
1. Background
2. Ground
3. Objects
4. Characters
5. Effects
6. UI

## Key Files

- `Assets/InputSystem_Actions.inputactions` - Input system configuration
- `Packages/manifest.json` - Package dependencies
- `ProjectSettings/ProjectVersion.txt` - Unity version
- `ProjectSettings/TagManager.asset` - Tags and layers
