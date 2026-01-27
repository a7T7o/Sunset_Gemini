---
inclusion: manual
priority: P4
keywords: [农田, 种植, 作物, 浇水, 耕地, FarmTileData]
lastUpdated: 2025-01-09
archivedFrom: .kiro/steering/farming.md
archivedDate: 2025-01-09
---

# 农田系统规范

## 系统概述

### 核心功能
- 耕地状态管理（未开垦/已开垦/已浇水）
- 作物生长周期（播种→生长→成熟→收获）
- 水分系统（3种视觉状态）
- 季节适应性

## 土壤状态

### SoilMoistureState 枚举
```csharp
public enum SoilMoistureState
{
    Dry,           // 干燥
    WetWithPuddle, // 湿润带水洼（浇水后2小时内）
    WetDark        // 湿润深色（浇水2小时后）
}
```

### 状态转换
- 浇水后 2 小时内：WetWithPuddle
- 浇水 2 小时后到第二天：WetDark
- 第二天开始：Dry

## FarmTileData 字段

```csharp
public bool isTilled;              // 是否已耕作
public bool wateredToday;          // 今天是否浇水
public bool wateredYesterday;      // 昨天是否浇水
public float waterTime;            // 浇水时间（小时）
public SoilMoistureState moistureState;  // 当前湿度状态
public CropInstance crop;          // 作物实例
```

## 时间系统集成

### 事件订阅
```csharp
// 正确：使用静态事件访问
TimeManager.OnDayChanged += OnDayChanged;
TimeManager.OnHourChanged += OnHourChanged;
```

### 每日更新逻辑
```csharp
private void OnDayChanged(int year, int day, int totalDays)
{
    foreach (var tileData in farmTiles.Values)
    {
        // 作物生长（如果昨天浇水）
        if (tileData.crop != null && tileData.wateredYesterday)
        {
            tileData.crop.currentGrowthDay++;
        }
        
        // 重置浇水状态
        tileData.wateredYesterday = tileData.wateredToday;
        tileData.wateredToday = false;
        tileData.moistureState = SoilMoistureState.Dry;
    }
}
```

## 季节判断

### 枚举对应
- ItemEnums.Season: Spring=0, Summer=1, Fall=2, Winter=3
- SeasonManager.Season: Spring=0, Summer=1, Autumn=2, Winter=3

## 作物生长

### CropInstance 核心方法
```csharp
public int GetCurrentGrowthStage()
{
    if (currentGrowthDay >= seedData.growthDays) 
        return seedData.growthStageSprites.Length - 1;
        
    float growthPercent = (float)currentGrowthDay / seedData.growthDays;
    return Mathf.FloorToInt(growthPercent * (seedData.growthStageSprites.Length - 1));
}

public bool IsMature()
{
    return currentGrowthDay >= seedData.growthDays;
}
```

## 精力消耗配置

```csharp
[Header("精力消耗")]
[SerializeField] private float tillingStaminaCost = 3f;   // 锄地
[SerializeField] private float wateringStaminaCost = 2f;  // 浇水
```
