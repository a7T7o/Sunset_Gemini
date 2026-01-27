# 全局调试系统 - 设计文档

## 概述

将分散在各组件中的调试开关集中到 TimeManager，通过控制事件是否发布来实现全局调试控制。这种设计遵循"单一职责"原则，让 TimeManager 成为时间事件的唯一控制点。

## 架构

### 当前架构（问题）

```
┌─────────────────────────────────────────────────────────────────┐
│                        当前架构（分散控制）                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  TimeManager                    TreeControllerV2                 │
│  ┌──────────────┐              ┌──────────────────────┐         │
│  │ 无事件开关    │──事件发布──→│ enableSeasonEvents   │         │
│  │              │              │ ├─ true: 订阅事件    │         │
│  │              │              │ └─ false: 不订阅     │         │
│  └──────────────┘              └──────────────────────┘         │
│                                                                  │
│  问题：每个订阅者都需要单独的开关，难以统一管理                    │
└─────────────────────────────────────────────────────────────────┘
```

### 目标架构（集中控制）

```
┌─────────────────────────────────────────────────────────────────┐
│                        目标架构（集中控制）                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  TimeManager                    TreeControllerV2                 │
│  ┌──────────────────┐          ┌──────────────────────┐         │
│  │ 事件发布开关      │          │ 始终订阅所有事件      │         │
│  │ ├─ enableDay     │──条件──→│                      │         │
│  │ ├─ enableSeason  │  发布   │ 无需单独开关          │         │
│  │ ├─ enableHour    │          │                      │         │
│  │ └─ enableMinute  │          │                      │         │
│  └──────────────────┘          └──────────────────────┘         │
│                                                                  │
│  优点：统一控制点，简化订阅者逻辑                                  │
└─────────────────────────────────────────────────────────────────┘
```

## 组件和接口

### TimeManager 新增字段

```csharp
#region 事件发布开关
[Header("━━━━ 事件发布开关 ━━━━")]
[Tooltip("是否发布每日变化事件（OnDayChanged）\n" +
         "关闭后：树木不成长、农作物不生长、NPC不更新")]
[SerializeField] private bool enableDayEvent = true;

[Tooltip("是否发布季节变化事件（OnSeasonChanged）\n" +
         "关闭后：树木不响应季节变化、不触发季节特效")]
[SerializeField] private bool enableSeasonEvent = true;

[Tooltip("是否发布小时变化事件（OnHourChanged）\n" +
         "关闭后：光照不变化、NPC日程不更新")]
[SerializeField] private bool enableHourEvent = true;

[Tooltip("是否发布分钟变化事件（OnMinuteChanged）\n" +
         "关闭后：精细时间显示不更新")]
[SerializeField] private bool enableMinuteEvent = true;
#endregion
```

### TimeManager 事件发布修改

```csharp
// 修改前
OnDayChanged?.Invoke(currentYear, currentDay, totalDaysPassed);

// 修改后
if (enableDayEvent)
{
    OnDayChanged?.Invoke(currentYear, currentDay, totalDaysPassed);
}
```

### TreeControllerV2 简化

```csharp
// 移除字段
// [SerializeField] private bool enableSeasonEvents = false;  // 删除

// Start() 中始终订阅
private void Start()
{
    // 始终订阅所有事件（由 TimeManager 控制是否发布）
    SeasonManager.OnSeasonChanged += OnSeasonChanged;
    SeasonManager.OnVegetationSeasonChanged += OnVegetationSeasonChanged;
    TimeManager.OnDayChanged += OnDayChanged;
    WeatherSystem.OnPlantsWither += OnWeatherWither;
    WeatherSystem.OnPlantsRecover += OnWeatherRecover;
    WeatherSystem.OnWinterSnow += OnWinterSnow;
    WeatherSystem.OnWinterMelt += OnWinterMelt;
    // ...
}
```

## 数据模型

### 事件开关配置

| 开关名称 | 默认值 | 影响的事件 | 影响的功能 |
|---------|-------|-----------|-----------|
| enableDayEvent | true | OnDayChanged | 树木成长、农作物生长、NPC日程 |
| enableSeasonEvent | true | OnSeasonChanged | 季节切换、植被变化、天气系统 |
| enableHourEvent | true | OnHourChanged | 光照变化、NPC活动、商店营业 |
| enableMinuteEvent | true | OnMinuteChanged | 时间UI更新、精细计时 |

## 正确性属性

*正确性属性是系统在所有有效执行中应保持为真的特征或行为——本质上是关于系统应该做什么的正式声明。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1: 事件开关控制事件发布
*对于任意* 事件开关状态，当开关为 false 时，对应的事件 SHALL 不被发布
**验证: 需求 1.1, 1.2, 1.3, 1.4**

### 属性 2: 订阅者无条件订阅
*对于任意* TreeControllerV2 实例，初始化时 SHALL 始终订阅所有相关事件
**验证: 需求 2.1**

### 属性 3: 开关切换即时生效
*对于任意* 事件开关，从 false 切换到 true 后，下一次事件触发时订阅者 SHALL 收到通知
**验证: 需求 4.2, 4.3**

### 属性 4: 功能兼容性
*对于任意* 系统状态，当所有开关都为 true 时，系统行为 SHALL 与修改前完全一致
**验证: 需求 4.1**

## 错误处理

1. **开关状态变化日志**: 当开关状态改变时，输出调试日志
2. **事件发布日志**: 当事件被阻止发布时，输出调试日志（仅在 showDebugInfo=true 时）

## 测试策略

### 单元测试
- 验证每个开关独立控制对应事件
- 验证开关切换即时生效
- 验证默认状态下功能正常

### 集成测试
- 验证 TreeControllerV2 在各种开关组合下的行为
- 验证运行时切换开关的效果
