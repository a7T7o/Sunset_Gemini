# ⏰ TimeManager 使用说明

## 📋 时间系统概述

这是一个完全仿照《星露谷物语》的时间系统，包含：
- **游戏时间**：年/季/日/时/分
- **时间流逝**：可配置速度，默认1天=20分钟
- **事件系统**：5个时间事件（分钟/小时/天/季/年）
- **自动集成**：与SeasonManager、TreeController无缝协作

---

## 🎮 快速开始

### 第1步：创建TimeManager

```
1. Hierarchy窗口右键 → Create Empty
2. 命名：GameManager
3. Add Component → Time Manager
4. 完成！（TimeManager会自动设为单例）
```

### 第2步：创建SeasonManager（可选，但推荐）

```
1. 选中同一个GameManager
2. Add Component → Season Manager
3. 勾选"Use Time Manager"
4. 完成！（SeasonManager会自动订阅TimeManager）
```

### 第3步：运行测试

```
1. 点击运行
2. 观察Console日志：
   - [TimeManager] 初始化完成
   - [SeasonManager] 初始化完成 - 使用TimeManager
   - 时间开始流逝...

3. 快捷键测试：
   - T键：切换5倍速
   - P键：暂停/继续
   - N键：跳到下一天
```

---

## ⚙️ Inspector配置详解

### 当前时间
- **Current Year**: 当前年份（默认1）
- **Current Season**: 当前季节（Spring/Summer/Autumn/Winter）
- **Current Day**: 本季第几天（1-7）
- **Current Hour**: 当前小时（6-26，26=凌晨2点）
- **Current Minute**: 当前分钟（0/10/20/30/40/50）

### 时间流逝设置
- **Real Seconds Per Game Day**: 1游戏天=多少现实秒
  - 默认：1200秒（20分钟）
  - 星露谷物语：1200秒
  - 测试推荐：120秒（2分钟）
  
- **Time Scale**: 时间流逝倍率
  - 1.0 = 正常速度
  - 2.0 = 2倍速
  - 5.0 = 5倍速（快速测试）
  
- **Is Paused**: 暂停时间流逝

### 游戏时间设置
- **Day Start Hour**: 每天开始时间（默认6 = 06:00 AM）
- **Day End Hour**: 每天结束时间（默认26 = 02:00 AM次日）
- **Hours Per Day**: 每天有多少小时（默认20）
- **Minute Steps Per Hour**: 每小时跳跃几次（默认6，即每10分钟）

### 季节设置
- **Days Per Season**: 每季多少天（默认7天）

### 调试
- **Show Debug Info**: 显示详细日志
- **Enable Debug Keys**: 启用快捷键

---

## 🎯 时间计算逻辑

### 星露谷物语时间系统

```yaml
游戏一天:
  - 开始时间: 06:00 AM
  - 结束时间: 02:00 AM (次日)
  - 总时长: 20小时

时间跳跃:
  - 每10分钟为一个时间步
  - 1小时 = 6个时间步
  - 1天 = 20小时 × 6步 = 120步

现实时间映射:
  - 1游戏天 = 1200秒现实时间 (20分钟)
  - 1游戏小时 = 60秒现实时间 (1分钟)
  - 1游戏10分钟 = 10秒现实时间
```

### 一年的构成

```
1年 = 4季 × 7天 = 28天

季节顺序:
  Spring (春) → Summer (夏) → Autumn (秋) → Winter (冬)
```

---

## 📡 事件系统

TimeManager提供5个静态事件，供其他脚本订阅：

### 1. OnMinuteChanged（每10分钟）

```csharp
TimeManager.OnMinuteChanged += (int hour, int minute) =>
{
    Debug.Log($"时间: {hour}:{minute}");
};
```

### 2. OnHourChanged（每小时）

```csharp
TimeManager.OnHourChanged += (int hour) =>
{
    Debug.Log($"新的一小时: {hour}:00");
};
```

### 3. OnDayChanged（每天06:00）

```csharp
TimeManager.OnDayChanged += (int year, int seasonDay, int totalDays) =>
{
    Debug.Log($"新的一天！Year {year} Day {seasonDay} (总第{totalDays}天)");
};
```

### 4. OnSeasonChanged（季节变化）

```csharp
TimeManager.OnSeasonChanged += (SeasonManager.Season newSeason, int year) =>
{
    Debug.Log($"季节变化: {newSeason} (Year {year})");
};
```

### 5. OnYearChanged（新年）

```csharp
TimeManager.OnYearChanged += (int year) =>
{
    Debug.Log($"新的一年: Year {year}");
};
```

### 6. OnSleep（睡觉）

```csharp
TimeManager.OnSleep += () =>
{
    Debug.Log("玩家睡觉，跳到下一天");
};
```

---

## 🔌 其他脚本集成示例

### SeasonManager集成（已完成）

```csharp
// SeasonManager.cs 中
private void Start()
{
    if (useTimeManager)
    {
        TimeManager.OnSeasonChanged += OnTimeManagerSeasonChanged;
    }
}

private void OnTimeManagerSeasonChanged(SeasonManager.Season newSeason, int year)
{
    SetSeason(newSeason);
}
```

### TreeController集成（已完成）

```csharp
// TreeController.cs 中
private void Start()
{
    TimeManager.OnDayChanged += OnDayChangedByTimeManager;
}

private void OnDayChangedByTimeManager(int year, int seasonDay, int totalDays)
{
    // 检查树木成长
    int daysSincePlanted = totalDays - plantedDay;
    int requiredDays = GetRequiredDaysForNextStage();  // 根据当前阶段获取所需天数
    if (daysSincePlanted >= requiredDays)
    {
        Grow();
        plantedDay = totalDays;  // 重置种植日期用于下一阶段
    }
}
```

### 自定义NPC行为示例

```csharp
public class NPCSchedule : MonoBehaviour
{
    void Start()
    {
        TimeManager.OnHourChanged += OnHourChanged;
    }
    
    void OnHourChanged(int hour)
    {
        switch (hour)
        {
            case 8:  GoToShop(); break;
            case 12: GoToHome(); break;
            case 18: GoToBar(); break;
            case 22: GoToSleep(); break;
        }
    }
}
```

---

## 🛠️ 公共API

### 时间控制

```csharp
// 睡觉（跳到下一天早上06:00）
TimeManager.Instance.Sleep();

// 暂停/继续时间
TimeManager.Instance.TogglePause();
TimeManager.Instance.SetPaused(true);  // 暂停
TimeManager.Instance.SetPaused(false); // 继续

// 设置时间流速
TimeManager.Instance.SetTimeScale(5f); // 5倍速

// 设置具体时间
TimeManager.Instance.SetTime(
    year: 2, 
    season: SeasonManager.Season.Summer, 
    day: 5, 
    hour: 14, 
    minute: 30
);
```

### 时间查询

```csharp
// 获取当前时间
int year = TimeManager.Instance.GetYear();
SeasonManager.Season season = TimeManager.Instance.GetSeason();
int day = TimeManager.Instance.GetDay();
int hour = TimeManager.Instance.GetHour();
int minute = TimeManager.Instance.GetMinute();

// 获取总天数（从游戏开始）
int totalDays = TimeManager.Instance.GetTotalDaysPassed();

// 获取格式化字符串
string timeStr = TimeManager.Instance.GetFormattedTime();
// 输出: "Year 1 Spring Day 3 02:30 PM"

// 判断白天/夜晚
bool isDay = TimeManager.Instance.IsDaytime();    // 06:00-18:00
bool isNight = TimeManager.Instance.IsNighttime(); // 18:00-02:00

// 获取当天进度（0-1）
float progress = TimeManager.Instance.GetDayProgress();
// 0 = 06:00, 0.5 = 中午, 1 = 02:00
```

---

## 🎨 UI显示

### 方法1：使用TimeDisplayUI组件（推荐）

```
1. Canvas上创建TextMeshPro对象
2. Add Component → Time Display UI
3. 拖拽Text组件到"Time Text TMP"
4. 调整显示格式
```

### 方法2：自定义UI脚本

```csharp
public class MyTimeUI : MonoBehaviour
{
    public Text timeText;
    
    void Start()
    {
        TimeManager.OnMinuteChanged += UpdateUI;
    }
    
    void UpdateUI(int hour, int minute)
    {
        timeText.text = TimeManager.Instance.GetFormattedTime();
    }
}
```

---

## ⚡ 快捷键（调试用）

启用条件：勾选`Enable Debug Keys`

| 按键 | 功能 |
|-----|------|
| **T** | 切换时间倍速（1x ↔ 5x） |
| **P** | 暂停/继续时间 |
| **N** | 跳到下一天（06:00） |

---

## 🔍 右键菜单（Inspector中）

在TimeManager组件上右键：

```
🌅 跳到早上06:00
🌆 跳到傍晚18:00
🌙 跳到夜晚22:00
⏭️ 跳到下一天
🍂 跳到下一季
📅 跳到下一年
⚡ 切换5倍速
```

---

## 💡 最佳实践

### 1. 单例模式
TimeManager是单例，全局只有一个实例：
```csharp
TimeManager.Instance.GetYear(); // ✅ 正确
```

### 2. 事件订阅与取消
务必在OnDestroy中取消订阅，避免内存泄漏：
```csharp
void OnDestroy()
{
    TimeManager.OnDayChanged -= OnDayChanged;
}
```

### 3. 检查Instance是否存在
在早期初始化时，Instance可能为null：
```csharp
if (TimeManager.Instance != null)
{
    int year = TimeManager.Instance.GetYear();
}
```

### 4. 测试建议
- 开发测试：`Real Seconds Per Game Day = 120`（2分钟）
- 正式游戏：`Real Seconds Per Game Day = 1200`（20分钟）

---

## ❓ 常见问题

### Q1: TimeManager找不到？
**A:** 检查场景中是否有挂载TimeManager的GameObject。

### Q2: 时间不流逝？
**A:** 检查`Is Paused`是否勾选，或`Time Scale`是否为0。

### Q3: 快捷键不工作？
**A:** 确保`Enable Debug Keys`已勾选。

### Q4: 树木不成长？
**A:** 
1. 检查TreeController是否勾选`Auto Grow`
2. 确保不是冬季（冬季不成长）
3. 检查`Days To Stage 1`（树苗→小树）和`Days To Stage 2`（小树→大树）设置

### Q5: 如何自定义一天的时长？
**A:** 修改`Real Seconds Per Game Day`：
- 600秒 = 10分钟
- 1200秒 = 20分钟（默认）
- 2400秒 = 40分钟

### Q6: 如何让一季有28天（像星露谷）？
**A:** 修改`Days Per Season`为28。

---

## 🎯 完整测试流程

```
1. ✅ 创建GameManager + TimeManager
2. ✅ 创建SeasonManager（勾选Use Time Manager）
3. ✅ 运行游戏
4. ✅ 观察日志：时间开始流逝
5. ✅ 按T键切换5倍速
6. ✅ 等待7天，观察季节变化
7. ✅ 创建树木预制体 + TreeController
8. ✅ 观察树木成长（每2天一个阶段）
9. ✅ 完成！
```

---

## 📚 相关文档

- `TreeController_使用说明.md` - 树木成长系统
- `SeasonManager.cs` - 季节管理
- `TimeDisplayUI.cs` - UI显示组件

---

**享受你的星露谷时光！🌾**

