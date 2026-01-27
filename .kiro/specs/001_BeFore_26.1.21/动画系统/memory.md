# 人物与工具动画系统 - 开发记忆

## 模块概述

人物与工具动画同步系统，实现工具动画与人物动画的帧级同步，包括：
- Mode A++ 帧索引硬锁同步机制
- 工具动作锁定系统
- 疾跑状态管理
- 三向动画生成工具

## 当前状态

- **完成度**: 98%
- **最后更新**: 2025-12-24
- **状态**: ✅ 已完成

## 会话记录

### 会话 2 - 2025-12-24（工具类型获取修复）

**问题说明**:
- `PlayerToolHitEmitter.BuildContext()` 根据动画状态推断工具类型
- `STATE_CRUSH = 8` 被映射到 `ToolType.Pickaxe`
- 但锄头（Hoe）也使用 `Crush` 动画状态
- 导致锄头无法挖出树苗

**修复方案**:
- 从 `PlayerToolController.CurrentToolData.toolType` 获取实际工具类型
- 不再仅依赖动画状态推断

**相关文件**:
- `Assets/YYY_Scripts/Combat/PlayerToolHitEmitter.cs` - 修改 BuildContext()
- `Assets/YYY_Scripts/Anim/Player/PlayerToolController.cs` - 提供 CurrentToolData 属性

---

## 核心功能

### Mode A++ 帧索引硬锁
- 用"帧号"而非"时间"做同步基准
- 从 Player Sprite 名称解析帧号
- 按帧数比例映射到 Tool 帧号
- 完全消除拖影，零帧延迟

### 工具动作锁定系统
- 时间计时器替代 normalizedTime
- ForcePlayAnimation 智能方向检测
- ForceHideTool / AllowToolShow 机制

### 疾跑状态管理
- 3秒疾跑记忆
- 双击切换模式
- 长按疾跑模式

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 使用帧索引而非时间同步 | 消除离散边界误差 | 2025-12-16 |
| 单一 Tool 子对象架构 | 简化管理，废弃复杂分组 | 2025-12-16 |
| 时间计时器替代 normalizedTime | 更稳定可靠 | 2025-12-16 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Anim/Player/PlayerAnimController.cs` | 动画播放、时间计时器 |
| `Assets/Scripts/Anim/_...._/LayerAnimSync.cs` | 帧同步、显隐控制 |
| `Assets/Scripts/Service/Player/ToolActionLockManager.cs` | 输入锁定、缓存 |
| `Assets/Scripts/Anim/Player/PlayerToolController.cs` | 工具切换、参数设置 |
| `Assets/Scripts/Service/Player/SprintStateManager.cs` | 疾跑状态管理 |

## 详细文档

- `Docx/分类/人物与工具动画/000_动画系统完整文档.md`
