using UnityEngine;
using System;

/// <summary>
/// 时间管理器 - 星露谷物语风格
/// 时间系统：年/季/日/时/分
/// 1天 = 20分钟现实时间（可配置）
/// 1季 = 7天
/// 游戏时间：06:00 - 02:00（凌晨2点强制睡觉）
/// </summary>
public class TimeManager : MonoBehaviour
{
    #region 单例
    private static TimeManager instance;
    public static TimeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<TimeManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("TimeManager");
                    instance = go.AddComponent<TimeManager>();
                }
            }
            return instance;
        }
    }
    #endregion

    #region 时间数据
    [Header("=== 当前时间 ===")]
    [SerializeField] private int currentYear = 1;
    [SerializeField] private SeasonManager.Season currentSeason = SeasonManager.Season.Spring;
    [SerializeField] private int currentDay = 1;        // 1-28（每季28天）
    [SerializeField] private int currentHour = 6;       // 6-26（06:00 - 02:00，用24+2表示）
    [SerializeField] private int currentMinute = 0;     // 0/10/20/30/40/50

    [Header("=== 时间流逝设置 ===")]
    [Tooltip("1游戏天 = 多少现实秒（星露谷默认1200秒=20分钟）")]
    [SerializeField] private float realSecondsPerGameDay = 1200f; // 20分钟

    [Tooltip("时间流逝速度倍率（1.0=正常，2.0=2倍速）")]
    [SerializeField] private float timeScale = 1.0f;

    [Tooltip("是否暂停时间")]
    [SerializeField] private bool isPaused = false;

    [Header("=== 游戏时间设置 ===")]
    [Tooltip("每天开始时间（小时）")]
    [SerializeField] private int dayStartHour = 6;

    [Tooltip("每天结束时间（小时，26=凌晨2点）")]
    [SerializeField] private int dayEndHour = 26;

    [Tooltip("每个游戏天有多少小时")]
    [SerializeField] private int hoursPerDay = 20; // 06:00-02:00 = 20小时

    [Tooltip("每小时有多少分钟跳跃（星露谷是6次，每10分钟）")]
    [SerializeField] private int minuteStepsPerHour = 6;

    [Header("=== 季节设置 ===")]
    [Tooltip("每季多少天（星露谷物语=28天）")]
    [SerializeField] private int daysPerSeason = 28;
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // ✅ 自动修复旧场景中的错误值
        if (daysPerSeason != 28)
        {
            Debug.LogWarning($"<color=yellow>[TimeManager] 检测到错误的daysPerSeason值({daysPerSeason})，已自动修正为28</color>");
            daysPerSeason = 28;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
    #endif

    [Header("=== 调试 ===")]
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("━━━━ 时间事件开关 ━━━━")]
    [Tooltip("是否发布分钟变化事件（OnMinuteChanged）\n" +
             "关闭后：精细时间显示不更新")]
    [SerializeField] private bool enableMinuteEvent = true;
    
    [Tooltip("是否发布小时变化事件（OnHourChanged）\n" +
             "关闭后：光照不变化、NPC日程不更新")]
    [SerializeField] private bool enableHourEvent = true;
    
    [Tooltip("是否发布每日变化事件（OnDayChanged）\n" +
             "关闭后：树木不成长、农作物不生长")]
    [SerializeField] private bool enableDayEvent = true;
    
    [Tooltip("是否发布年变化事件（OnYearChanged）\n" +
             "关闭后：年份变化不通知")]
    [SerializeField] private bool enableYearEvent = true;
    
    [Header("━━━━ 季节变更开关 ━━━━")]
    [Tooltip("是否发布季节变更事件（OnSeasonChanged）\n" +
             "关闭后：春→夏→秋→冬 的季节切换不通知订阅者\n" +
             "注意：这只控制事件通知，内部季节状态仍会更新")]
    [SerializeField] private bool enableSeasonChangeEvent = true;
    #endregion

    #region 内部变量
    private float gameTimeAccumulator = 0f; // 累积的游戏时间
    private float realSecondsPerGameMinute; // 每游戏分钟需要多少现实秒
    private int totalDaysPassed = 0;        // 总共过了多少天
    #endregion

    #region 事件系统
    /// <summary>分钟改变事件（每10分钟触发）</summary>
    public static event Action<int, int> OnMinuteChanged; // (hour, minute)
    
    /// <summary>小时改变事件</summary>
    public static event Action<int> OnHourChanged; // (hour)
    
    /// <summary>天改变事件</summary>
    public static event Action<int, int, int> OnDayChanged; // (year, season_day, total_days)
    
    /// <summary>季节改变事件</summary>
    public static event Action<SeasonManager.Season, int> OnSeasonChanged; // (new_season, year)
    
    /// <summary>年改变事件</summary>
    public static event Action<int> OnYearChanged; // (year)

    /// <summary>睡觉/跳过一天事件</summary>
    public static event Action OnSleep;
    #endregion

    #region 初始化
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // ✅ DontDestroyOnLoad 由 PersistentManagers 统一处理
            // 不再在此调用，避免 "only works for root GameObjects" 警告
            Initialize();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        CalculateTimeStep();
        totalDaysPassed = CalculateTotalDays();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[TimeManager] 初始化完成</color>\n" +
                     $"  当前时间：Year {currentYear} {currentSeason} Day {currentDay} {FormatTime(currentHour, currentMinute)}\n" +
                     $"  1游戏天 = {realSecondsPerGameDay}秒 ({realSecondsPerGameDay/60f:F1}分钟)\n" +
                     $"  1游戏分钟 = {realSecondsPerGameMinute:F2}秒");
        }
    }

    private void CalculateTimeStep()
    {
        // 游戏一天 = 20小时 × 6次/小时 = 120个时间步（每步10分钟）
        int totalMinuteSteps = hoursPerDay * minuteStepsPerHour;
        realSecondsPerGameMinute = realSecondsPerGameDay / totalMinuteSteps;
    }

    private int CalculateTotalDays()
    {
        int seasonIndex = (int)currentSeason;
        return (currentYear - 1) * (daysPerSeason * 4) + seasonIndex * daysPerSeason + (currentDay - 1);
    }
    #endregion

    #region 时间流逝
    private void Update()
    {
        if (isPaused) return;

        // ⚠️ 已弃用：请使用TimeManagerDebugger组件
        // 保留代码以防兼容性问题，但默认关闭

        // 时间累积
        gameTimeAccumulator += Time.deltaTime * timeScale;

        // 每达到一个时间步，前进10分钟
        if (gameTimeAccumulator >= realSecondsPerGameMinute)
        {
            gameTimeAccumulator -= realSecondsPerGameMinute;
            AdvanceMinute();
        }
    }

    private void AdvanceMinute()
    {
        currentMinute += 10;

        if (currentMinute >= 60)
        {
            currentMinute = 0;
            AdvanceHour();
        }

        // ★ 受事件开关控制
        if (enableMinuteEvent)
        {
            OnMinuteChanged?.Invoke(currentHour, currentMinute);
        }

        if (showDebugInfo && currentMinute == 0) // 整点显示
        {
            Debug.Log($"<color=yellow>[Time] {FormatTime(currentHour, currentMinute)}</color>");
        }
    }

    private void AdvanceHour()
    {
        currentHour++;

        // ★ 受事件开关控制
        if (enableHourEvent)
        {
            OnHourChanged?.Invoke(currentHour);
        }

        // 到达凌晨2点，强制睡觉，进入下一天
        if (currentHour > dayEndHour)
        {
            Sleep();
        }
    }

    private void AdvanceDay()
    {
        currentDay++;
        currentHour = dayStartHour;
        currentMinute = 0;
        totalDaysPassed++;

        // ★ 受事件开关控制
        if (enableDayEvent)
        {
            OnDayChanged?.Invoke(currentYear, currentDay, totalDaysPassed);
        }

        if (showDebugInfo)
        {
            Debug.Log($"<color=green>[Time] 新的一天！Year {currentYear} {currentSeason} Day {currentDay}</color>");
        }

        // 检查是否进入下一季
        if (currentDay > daysPerSeason)
        {
            currentDay = 1;
            AdvanceSeason();
        }
    }

    private void AdvanceSeason()
    {
        int nextSeasonIndex = ((int)currentSeason + 1) % 4;
        currentSeason = (SeasonManager.Season)nextSeasonIndex;

        // ★ 受季节变更开关控制
        if (enableSeasonChangeEvent)
        {
            OnSeasonChanged?.Invoke(currentSeason, currentYear);
        }

        if (showDebugInfo)
        {
            Debug.Log($"<color=orange>[Time] 季节变化！现在是 {currentSeason}</color>");
        }

        // 更新SeasonManager（始终更新，不受开关影响）
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.SetSeason(currentSeason);
        }

        // 检查是否进入下一年
        if (currentSeason == SeasonManager.Season.Spring)
        {
            AdvanceYear();
        }
    }

    private void AdvanceYear()
    {
        currentYear++;
        
        // ★ 受事件开关控制
        if (enableYearEvent)
        {
            OnYearChanged?.Invoke(currentYear);
        }

        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[Time] 新的一年！Year {currentYear}</color>");
        }
    }
    #endregion

    #region 公共接口
    /// <summary>睡觉/跳过到下一天早上06:00</summary>
    public void Sleep()
    {
        OnSleep?.Invoke();
        AdvanceDay();

        if (showDebugInfo)
        {
            Debug.Log($"<color=magenta>[Time] 睡觉 → 第二天 {FormatTime(currentHour, currentMinute)}</color>");
        }
    }

    /// <summary>暂停/继续时间</summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>[Time] {(isPaused ? "暂停" : "继续")}</color>");
        }
    }

    /// <summary>设置时间流逝速度</summary>
    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Max(0, scale);
    }

    /// <summary>设置暂停状态</summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    /// <summary>设置具体时间</summary>
    public void SetTime(int year, SeasonManager.Season season, int day, int hour, int minute)
    {
        SeasonManager.Season oldSeason = currentSeason;
        int oldYear = currentYear;
        
        currentYear = year;
        currentSeason = season;
        currentDay = Mathf.Clamp(day, 1, daysPerSeason);
        currentHour = Mathf.Clamp(hour, dayStartHour, dayEndHour);
        currentMinute = Mathf.Clamp(minute / 10 * 10, 0, 50); // 取整到10的倍数
        totalDaysPassed = CalculateTotalDays();
        
        // ✅ 触发事件（受开关控制）
        if (oldSeason != currentSeason)
        {
            // ★ 受季节变更开关控制
            if (enableSeasonChangeEvent)
            {
                OnSeasonChanged?.Invoke(currentSeason, currentYear);
            }
            
            // 通知SeasonManager（始终更新，不受开关影响）
            if (SeasonManager.Instance != null)
            {
                SeasonManager.Instance.SetSeason(currentSeason);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=orange>[TimeManager] 季节变化: {oldSeason} → {currentSeason}</color>");
            }
        }
        
        if (oldYear != currentYear)
        {
            // ★ 受事件开关控制
            if (enableYearEvent)
            {
                OnYearChanged?.Invoke(currentYear);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=cyan>[TimeManager] 年份变化: {oldYear} → {currentYear}</color>");
            }
        }
    }

    /// <summary>获取当前时间（格式化字符串）</summary>
    public string GetFormattedTime()
    {
        return $"Year {currentYear} {currentSeason} Day {currentDay} {FormatTime(currentHour, currentMinute)}";
    }

    /// <summary>获取当前是第几天（从游戏开始算）</summary>
    public int GetTotalDaysPassed()
    {
        return totalDaysPassed;
    }

    /// <summary>获取当前年份</summary>
    public int GetYear() => currentYear;

    /// <summary>获取当前季节</summary>
    public SeasonManager.Season GetSeason() => currentSeason;

    /// <summary>获取当前是本季第几天</summary>
    public int GetDay() => currentDay;

    /// <summary>获取当前小时</summary>
    public int GetHour() => currentHour;

    /// <summary>获取当前分钟</summary>
    public int GetMinute() => currentMinute;

    /// <summary>是否是白天（06:00-18:00）</summary>
    public bool IsDaytime()
    {
        return currentHour >= 6 && currentHour < 18;
    }

    /// <summary>是否是夜晚（18:00-02:00）</summary>
    public bool IsNighttime()
    {
        return !IsDaytime();
    }

    /// <summary>获取当前时间进度（0-1，0=06:00, 1=02:00）</summary>
    public float GetDayProgress()
    {
        int totalMinutes = (currentHour - dayStartHour) * 60 + currentMinute;
        int totalMinutesInDay = hoursPerDay * 60;
        return Mathf.Clamp01((float)totalMinutes / totalMinutesInDay);
    }
    #endregion

    #region 工具方法
    private string FormatTime(int hour, int minute)
    {
        int displayHour = hour;
        string period = "AM";

        if (hour >= 12)
        {
            period = "PM";
            if (hour > 12) displayHour = hour - 12;
        }
        if (hour >= 24) // 凌晨
        {
            displayHour = hour - 24;
            period = "AM";
        }

        return $"{displayHour:D2}:{minute:D2} {period}";
    }
    #endregion

    #region 编辑器功能
#if UNITY_EDITOR
    [ContextMenu("🌅 跳到早上06:00")]
    private void DEBUG_JumpToMorning()
    {
        currentHour = 6;
        currentMinute = 0;
        Debug.Log("⏰ 跳到早上06:00");
    }

    [ContextMenu("🌆 跳到傍晚18:00")]
    private void DEBUG_JumpToEvening()
    {
        currentHour = 18;
        currentMinute = 0;
        Debug.Log("⏰ 跳到傍晚18:00");
    }

    [ContextMenu("🌙 跳到夜晚22:00")]
    private void DEBUG_JumpToNight()
    {
        currentHour = 22;
        currentMinute = 0;
        Debug.Log("⏰ 跳到夜晚22:00");
    }

    [ContextMenu("⏭️ 跳到下一天")]
    private void DEBUG_NextDay()
    {
        Sleep();
    }

    [ContextMenu("🍂 跳到下一季")]
    private void DEBUG_NextSeason()
    {
        currentDay = daysPerSeason;
        AdvanceDay();
    }

    [ContextMenu("📅 跳到下一年")]
    private void DEBUG_NextYear()
    {
        currentSeason = SeasonManager.Season.Winter;
        currentDay = daysPerSeason;
        AdvanceDay();
    }

    [ContextMenu("⚡ 切换5倍速")]
    private void DEBUG_ToggleSpeed()
    {
        timeScale = timeScale == 1f ? 5f : 1f;
        Debug.Log($"⚡ 时间速度: {timeScale}x");
    }
#endif
    #endregion
}

