# 全局调试系统 - 需求文档

## 介绍

本文档定义了将分散在各个组件中的调试开关集中到 TimeManager 进行统一管理的需求。目标是通过控制事件是否发布来实现全局调试控制，而不是在每个订阅者中单独控制。

## 术语表

- **TimeManager**: 时间管理器，负责游戏内时间流逝和事件发布
- **TreeControllerV2**: 树木控制器，订阅时间和季节事件
- **事件发布**: TimeManager 触发事件通知所有订阅者
- **调试开关**: 控制特定功能是否启用的布尔值

## 需求

### 需求 1

**用户故事:** 作为开发者，我希望在 TimeManager 中集中管理所有时间相关的调试开关，以便统一控制游戏内时间事件的发布。

#### 验收标准

1. WHEN 开发者在 TimeManager Inspector 中关闭"每日事件"开关 THEN TimeManager SHALL 停止发布 OnDayChanged 事件
2. WHEN 开发者在 TimeManager Inspector 中关闭"季节事件"开关 THEN TimeManager SHALL 停止发布 OnSeasonChanged 事件
3. WHEN 开发者在 TimeManager Inspector 中关闭"小时事件"开关 THEN TimeManager SHALL 停止发布 OnHourChanged 事件
4. WHEN 开发者在 TimeManager Inspector 中关闭"分钟事件"开关 THEN TimeManager SHALL 停止发布 OnMinuteChanged 事件
5. WHEN 任意事件开关被关闭 THEN 所有订阅该事件的组件 SHALL 不再收到该事件通知

### 需求 2

**用户故事:** 作为开发者，我希望移除 TreeControllerV2 中的 enableSeasonEvents 开关，以便简化树木组件的配置。

#### 验收标准

1. WHEN TreeControllerV2 初始化时 THEN TreeControllerV2 SHALL 始终订阅所有相关事件（无条件订阅）
2. WHEN TimeManager 的事件开关关闭时 THEN TreeControllerV2 SHALL 不再收到对应事件（由 TimeManager 控制）
3. WHEN 移除 enableSeasonEvents 字段后 THEN TreeControllerV2 的 Inspector SHALL 不再显示该选项

### 需求 3

**用户故事:** 作为开发者，我希望调试开关有清晰的分类和说明，以便快速理解每个开关的作用。

#### 验收标准

1. WHEN 查看 TimeManager Inspector 时 THEN 调试开关 SHALL 按功能分组显示（时间事件、调试快捷键）
2. WHEN 鼠标悬停在调试开关上时 THEN Inspector SHALL 显示该开关影响的具体功能说明
3. WHEN 调试开关状态改变时 THEN Console SHALL 输出状态变化日志（如果启用了调试信息）

### 需求 4

**用户故事:** 作为开发者，我希望修改调试开关后不影响原有的基础功能和详细逻辑，以便安全地进行调试。

#### 验收标准

1. WHEN 所有调试开关都开启时 THEN 系统 SHALL 与修改前的行为完全一致
2. WHEN 重新开启之前关闭的事件开关时 THEN 订阅者 SHALL 立即恢复接收事件
3. WHEN 游戏运行中切换调试开关 THEN 系统 SHALL 立即响应而无需重启游戏
