# 全局系统 - 开发记忆

## 模块概述

项目全局系统，包括：
- 时间与季节系统
- 事件系统
- 服务定位器
- 对象池

## 当前状态

- **完成度**: 90%
- **最后更新**: 2025-12-16
- **状态**: ✅ 已完成

## 核心功能

### 时间系统
- 事件订阅：`TimeManager.OnDayChanged`、`OnHourChanged`、`OnMinuteChanged`
- 获取时间：`TimeManager.GetDay()`、`GetHour()`、`GetSeason()`

### 季节系统
- 四季循环：Spring → Summer → Autumn → Winter
- 植被季节细分（5个阶段）

### 事件系统
- 静态事件订阅方式
- 全局事件：`NavGrid2D.OnRequestGridRefresh`

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 静态事件订阅 | 简化访问，避免实例查找 | 2025-12-16 |
| 事件驱动替代 Update | 性能优化 | 2025-12-16 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Service/TimeManager.cs` | 时间管理器 |
| `Assets/Scripts/Service/SeasonManager.cs` | 季节管理器 |

## 详细文档

- `Docx/分类/全局/000_项目全局文档.md`
