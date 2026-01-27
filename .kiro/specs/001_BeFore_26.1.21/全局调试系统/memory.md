# 全局调试系统 - 开发记忆

## 模块概述
将分散在各组件中的调试开关集中到 TimeManager 和 WeatherSystem，通过控制事件是否发布来实现全局调试控制。

## 当前状态
- **完成度**: 100%
- **最后更新**: 2025-01-04
- **状态**: 已完成

---

## 会话记录

### 会话 1 - 2025-01-04（全局调试系统重构）

**用户需求**:
> 现在请我们一起进入这些工作区的内容，我们现在需要对调试按钮做更清晰的分类，并让这个调试开关设置在timemanager里面

**用户补充需求**:
> 不对，我需要更细的分类，对于季节事件我需要一个单独的按钮，也就是季节内的事件比如下雪啊干燥啊什么的，干枯下雨什么的，这些是叫做季节事件，然后对于季节变更需要与季节事件分离开来

**最终分类**:

#### TimeManager 中的开关（时间事件 + 季节变更）

| 开关 | 默认值 | 影响事件 | 说明 |
|------|-------|---------|------|
| `enableMinuteEvent` | true | OnMinuteChanged | 分钟变化 |
| `enableHourEvent` | true | OnHourChanged | 小时变化 |
| `enableDayEvent` | true | OnDayChanged | 每日变化（树木成长） |
| `enableYearEvent` | true | OnYearChanged | 年份变化 |
| `enableSeasonChangeEvent` | true | OnSeasonChanged | 季节变更（春→夏→秋→冬） |

#### WeatherSystem 中的开关（天气事件）

| 开关 | 默认值 | 影响事件 | 说明 |
|------|-------|---------|------|
| `enableWitherEvent` | true | OnPlantsWither | 植物枯萎（高温） |
| `enableRecoverEvent` | true | OnPlantsRecover | 植物恢复（雨后） |
| `enableWinterSnowEvent` | true | OnWinterSnow | 冬季下雪（挂冰） |
| `enableWinterMeltEvent` | true | OnWinterMelt | 冬季融化（大太阳） |

**修改文件**:
- `Assets/YYY_Scripts/Service/TimeManager.cs`
  - 新增时间事件开关区域
  - 新增季节变更开关区域
  - 所有事件发布添加开关判断

- `Assets/YYY_Scripts/Service/WeatherSystem.cs`
  - 新增天气事件开关区域
  - 所有天气事件发布添加开关判断

- `Assets/YYY_Scripts/Controller/TreeControllerV2.cs`
  - 移除 `enableSeasonEvents` 字段
  - 始终订阅所有事件

**遗留问题**:
- 无

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 时间事件和季节变更分开 | 用户要求更细粒度控制 | 2025-01-04 |
| 天气事件放在 WeatherSystem | 天气事件由 WeatherSystem 发布 | 2025-01-04 |
| 默认所有开关为 true | 保持与修改前行为一致 | 2025-01-04 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Service/TimeManager.cs` | 时间管理器（时间事件+季节变更开关） |
| `Assets/YYY_Scripts/Service/WeatherSystem.cs` | 天气系统（天气事件开关） |
| `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` | 树木控制器（移除本地开关） |

---

## 使用说明

### Inspector 位置

1. **TimeManager** - 时间事件和季节变更
   - "━━━━ 时间事件开关 ━━━━"
   - "━━━━ 季节变更开关 ━━━━"

2. **WeatherSystem** - 天气事件
   - "━━━━ 天气事件开关 ━━━━"

### 常用调试场景

| 场景 | 操作 |
|------|------|
| 只测试树木成长 | TimeManager: enableDayEvent=true，其他可关 |
| 禁用天气影响 | WeatherSystem: 关闭所有天气事件开关 |
| 禁用季节切换 | TimeManager: enableSeasonChangeEvent=false |
