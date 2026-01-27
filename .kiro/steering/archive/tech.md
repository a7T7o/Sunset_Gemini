---
inclusion: manual
priority: P3
keywords: [技术栈, Unity, 包管理, 架构]
lastUpdated: 2025-01-09
archivedFrom: .kiro/steering/tech.md
archivedDate: 2025-01-09
---

## 中文概述（面向开发者本人）

- **最后更新**：2025-01-09
- **引擎版本**：Unity 6 (6000.0.62f1)
- **平台**：Windows
- **脚本语言**：C# (.NET Standard 2.1)
- **核心包**：
  - Input System 1.14.2（新输入系统）
  - Cinemachine 3.1.4（摄像机控制）
  - 2D Feature 2.0.1（2D 功能包）
  - uGUI 2.0.0（UI 系统）
  - Timeline 1.8.9（时间轴动画）
- **设计模式**：
  - 单例模式（核心管理器）
  - 观察者模式（事件驱动通信）
  - 状态机（敌人 AI、玩家动画）
  - 对象池（掉落物、粒子、敌人）
  - ScriptableObject（数据驱动设计）
- **目标帧率**：60 FPS
- **版本控制**：Git

> **注意**：此为技术栈快照，实际使用的包版本可能已更新。

---

# Technical Stack

## Engine & Version

- **Unity**: 6000.0.62f1 (Unity 6)
- **Platform**: Windows (win32)
- **Scripting**: C# (.NET Standard 2.1)

## Key Packages

```json
{
  "com.unity.inputsystem": "1.14.2",
  "com.unity.cinemachine": "3.1.4",
  "com.unity.feature.2d": "2.0.1",
  "com.unity.ugui": "2.0.0",
  "com.unity.timeline": "1.8.9",
  "com.unity.visualscripting": "1.9.7"
}
```

## Architecture Patterns

### Design Patterns
- **Singleton**: Core managers (TimeManager, SeasonManager, InventorySystem, AudioManager)
- **Observer**: Event-driven communication between systems
- **State Machine**: Enemy AI, player animation control
- **Object Pool**: Enemies, dropped items, particle effects
- **Factory**: Item generation, enemy spawning
- **ScriptableObject**: Data-driven design for items, crops, enemies

### Code Organization

```
Assets/Scripts/
├── Core/          # Core managers (GameManager, TimeManager, etc.)
├── Player/        # Player control and animation
├── Items/         # Item and inventory systems
├── Farming/       # Farming-related systems
├── Combat/        # Combat and weapon systems
├── Enemies/       # Enemy AI
├── UI/            # All UI scripts
├── Environment/   # Trees, rocks, world objects
├── NPC/           # NPC and dialogue
├── Service/       # Shared services (navigation, rendering, etc.)
└── Utils/         # Utility classes and extensions
```

## Common Commands

### Unity Editor
- **Play Mode**: F5 or Ctrl+P
- **Pause**: Ctrl+Shift+P
- **Step Frame**: Ctrl+Alt+P

### Custom Editor Tools
Located in `Assets/Editor/`:
- **Batch Tools**: Prefab generation, sprite settings, animation setup
- **Animation Tools**: Layer animation sync, tri-directional animation generator
- **Tilemap Tools**: Tilemap to sprite conversion
- **Debug Tools**: Occlusion manager, navigation visualizer

### Testing
- No automated test framework currently
- Manual testing via Unity Play Mode
- Debug menu accessible via context menu on managers

## Build System

- **Build Target**: Windows Standalone
- **Build Location**: Not configured yet
- **Asset Pipeline**: Unity's default asset pipeline
- **Scripting Backend**: Mono (default)

## Performance Considerations

- **Target FPS**: 60 FPS
- **Object Pooling**: Used for frequently spawned objects
- **Update Optimization**: Event-driven over Update() where possible
- **Culling**: Viewport culling for off-screen objects
- **NavGrid**: Configurable cell size (recommended 100x80 for ~150ms rebuild)

## Version Control

- **System**: Git
- **Ignored**: Library/, Temp/, Logs/, UserSettings/
- **Tracked**: Assets/, Packages/, ProjectSettings/, Docx/, Plan/
