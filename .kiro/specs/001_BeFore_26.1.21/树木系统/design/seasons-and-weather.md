# 树木系统 - 季节与天气设计文档

## 文档说明
本文档从 memory.md 迁移而来，包含树木系统的季节/天气完整设计。

---

## 植被季节系统设计

### 用户原始需求

> "现在全盘重新布局植物的季节变化，现在分为：早春（春季上半部分）、晚春早夏（春季下半部分加夏季前半部分）、晚夏早秋（夏季前半部分加秋季下半部分）、晚秋（秋季下半部分）、冬（整个冬季），以上五个阶段，季节时间不变，只是对季节的植物规则进行细分"

### 字段名称与视觉样式映射（2025-12-30 更新）

| 字段名称（新） | 字段名称（旧） | 实际视觉样式 | 说明 |
|--------------|--------------|-------------|------|
| `spring` | `earlySpring` | **春季样式** | 绿色茂盛 |
| `summer` | `lateSpringEarlySummer` | **夏季样式** | 深绿色 |
| `earlyFall` | `lateSummerEarlyFall` | **早秋样式** | 开始变黄 |
| `lateFall` | `lateFall` | **晚秋样式** | 黄色/橙色 |
| `winter` | `winter` | **冬季样式** | 挂冰/光秃 |

### 时间线与显示规则

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           一年四季时间线                                  │
├─────────────────────────────────────────────────────────────────────────┤
│ 春季                                                                     │
│ ├─ 春1-14天：100% 春季样式（Spring）                                     │
│ └─ 春15-28天：春季样式 → 夏季样式 渐变                                   │
│                                                                          │
│ 夏季                                                                     │
│ ├─ 夏1-14天：100% 夏季样式（Summer）                                     │
│ └─ 夏15-28天：夏季样式 → 早秋样式 渐变                                   │
│                                                                          │
│ 秋季（特殊：一进入就开始渐变）                                           │
│ ├─ 秋1-14天：早秋样式 → 晚秋样式 渐变（EarlyFall）                       │
│ └─ 秋15-28天：100% 晚秋样式（LateFall）                                  │
│                                                                          │
│ 冬季                                                                     │
│ └─ 冬1-28天：100% 冬季样式（Winter）                                     │
└─────────────────────────────────────────────────────────────────────────┘
```

### 渐变机制

1. **基于树木ID的随机种子**：每棵树有固定的随机值（0-1）
2. **进度比较**：如果 `treeSeedValue < progress`，显示下一季节样式
3. **线性渐变**：progress 从 0 线性增加到 1（14天内）
4. **效果**：场景中的树木逐渐从当前季节样式过渡到下一季节样式

### 渐变进度计算公式

```csharp
// SeasonManager.UpdateVegetationSeason() 中的计算

// 春季渐变（春15-28）
if (currentSeason == Spring && dayInSeason > 14)
{
    progress = (dayInSeason - 14) / 14f;  // 1/14 → 14/14
}

// 夏季渐变（夏15-28）
if (currentSeason == Summer && dayInSeason > 14)
{
    progress = (dayInSeason - 14) / 14f;  // 1/14 → 14/14
}

// 秋季渐变（秋1-14，特殊：第1天就开始）
if (currentSeason == Autumn && dayInSeason <= 14)
{
    progress = dayInSeason / 14f;  // 1/14 → 14/14
}
```

### 渐变样式选择逻辑

```csharp
// TreeControllerV2.GetNormalSprite() 中的逻辑

int seed = treeID + currentStageIndex * 100;
Random.InitState(seed);
float treeSeedValue = Random.value;  // 0.0 - 1.0 固定值

if (treeSeedValue < progress)
{
    // 显示下一季节样式
    return stageData.normal.GetSprite(GetNextVegetationSeason(vegSeason));
}
else
{
    // 显示当前季节样式
    return stageData.normal.GetSprite(vegSeason);
}
```

---

## 季节事件系统设计

### 事件订阅列表

TreeControllerV2 在 `Start()` 中订阅以下事件：

```csharp
SeasonManager.OnSeasonChanged += OnSeasonChanged;           // 日历季节变化
SeasonManager.OnVegetationSeasonChanged += OnVegetationSeasonChanged;  // 植被季节变化
TimeManager.OnDayChanged += OnDayChanged;                   // 每日变化（成长）
WeatherSystem.OnPlantsWither += OnWeatherWither;            // 天气枯萎
WeatherSystem.OnPlantsRecover += OnWeatherRecover;          // 天气恢复
WeatherSystem.OnWinterSnow += OnWinterSnow;                 // 冬季下雪
WeatherSystem.OnWinterMelt += OnWinterMelt;                 // 冬季融化
```

### 事件处理详解

#### OnSeasonChanged（日历季节变化）

| 新季节 | 树木状态 | 处理方式 |
|--------|---------|---------|
| 春季 | Withered/Frozen/Melted | 恢复为 Normal，清除 isWeatherWithered |
| 冬季 | Stage 0（树苗） | **直接销毁**（调用 DestroyTree()） |
| 其他 | 任意 | 调用 UpdateSprite() 更新显示 |

#### OnWeatherWither（天气枯萎）

| 当前状态 | 处理方式 |
|---------|---------|
| Normal | 设置 isWeatherWithered=true，状态改为 Withered，更新显示 |
| 其他 | 不处理 |

#### OnWinterSnow / OnWinterMelt

| 当前阶段 | 处理方式 |
|---------|---------|
| Stage 0（树苗） | **直接销毁** |
| Stage 1-5 | 状态改为 Frozen/Melted，更新显示 |

### 树木状态枚举

```csharp
public enum TreeState
{
    Normal,    // 正常状态
    Withered,  // 枯萎状态（天气导致）
    Frozen,    // 冰封状态（冬季下雪）
    Melted,    // 融化状态（冬季大太阳）
    Stump      // 树桩状态（被砍倒后）
}
```

### 状态与样式映射

| 状态 | 样式选择逻辑 |
|------|-------------|
| Normal | 根据植被季节和渐变进度选择 spring/summer/earlyFall/lateFall/winter |
| Withered | 根据季节选择 witheredSummer 或 witheredFall |
| Frozen | 显示 winter Sprite（挂冰状态） |
| Melted | 降级显示 witheredFall Sprite |
| Stump | 根据季节选择 stumpSpringSummer/stumpFall/stumpWinter |

---

## 天气系统与事件触发

### 天气类型枚举

```csharp
public enum Weather
{
    Sunny,      // 晴天
    Rainy,      // 雨天
    Withering   // 枯萎天（极端高温）/ 冬季下雪
}
```

### 各季节天气规则

#### 夏季（Summer）

| 天气类型 | 触发日期 | 事件 | 效果 |
|---------|---------|------|------|
| 枯萎日 | 第 8、14、20 天 | `OnPlantsWither` | 所有植物枯萎 |
| 雨天 | 第 1、4、6、10、18、26 天 | 无事件 | 滋润植物 |
| 雨后恢复 | 雨天后第二天 | `OnPlantsRecover` | 枯萎植物恢复 |

#### 秋季（Autumn）

| 天气类型 | 触发日期 | 事件 | 效果 |
|---------|---------|------|------|
| 枯萎日 | 第 6、14、22 天 | `OnPlantsWither` | 植物枯萎（不恢复） |

**秋季特殊规则**：秋季枯萎后**不会自动恢复**，只有等到春季才会恢复。

#### 冬季（Winter）

| 天气类型 | 触发日期 | 事件 | 效果 |
|---------|---------|------|------|
| 下雪日 | 第 1、5、11、21、26 天 | `OnWinterSnow` | 树苗死亡，其他挂冰 |
| 融化日 | 第 3、8、17、24、28 天 | `OnWinterMelt` | 树苗死亡，其他枯萎 |

---

## 一年完整时间表（112天）

| 日历季节 | 天数范围 | 总天数 | 植被季节 | 渐变进度 | 显示样式 |
|---------|---------|-------|---------|---------|---------|
| 春季 | 1-14 | 1-14 | Spring | 0% | 100% spring |
| 春季 | 15-28 | 15-28 | Spring | 7%→100% | spring → summer 渐变 |
| 夏季 | 1-14 | 29-42 | Summer | 0% | 100% summer |
| 夏季 | 15-28 | 43-56 | Summer | 7%→100% | summer → earlyFall 渐变 |
| 秋季 | 1-14 | 57-70 | EarlyFall | 7%→100% | earlyFall → lateFall 渐变 |
| 秋季 | 15-28 | 71-84 | LateFall | 0% | 100% lateFall |
| 冬季 | 1-28 | 85-112 | Winter | 0% | 100% winter |

---

## 相关代码文件

| 文件 | 职责 |
|------|------|
| `SeasonManager.cs` | 管理植被季节和渐变进度 |
| `TreeControllerV2.cs` | 根据季节选择 Sprite |
| `TreeSpriteData.cs` | 定义 SeasonSpriteSet 数据结构 |
| `WeatherSystem.cs` | 天气系统，触发植物事件 |
