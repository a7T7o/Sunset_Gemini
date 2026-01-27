using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 天气系统 - 管理雨天、枯萎等气候事件
/// </summary>
public class WeatherSystem : MonoBehaviour
{
    #region 单例
    private static WeatherSystem instance;
    public static WeatherSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<WeatherSystem>();
            }
            return instance;
        }
    }
    #endregion
    
    /// <summary>
    /// 天气类型
    /// </summary>
    public enum Weather
    {
        Sunny,      // 晴天
        Rainy,      // 雨天
        Withering   // 枯萎天（极端高温）
    }
    
    [Header("━━━━ 当前天气 ━━━━")]
    [SerializeField] private Weather currentWeather = Weather.Sunny;
    
    [Header("━━━━ 夏季天气规则 ━━━━")]
    [Tooltip("夏季枯萎日（所有植物枯萎）")]
    public List<int> summerWitheringDays = new List<int> { 8, 14, 20 };
    
    [Tooltip("夏季下雨日（滋润植物）")]
    public List<int> summerRainyDays = new List<int> { 1, 4, 6, 10, 18, 26 };
    
    [Header("━━━━ 秋季天气规则 ━━━━")]
    [Tooltip("秋季枯萎日（直接枯萎，不恢复，无雨）")]
    public List<int> fallWitheringDays = new List<int> { 6, 14, 22 };
    
    [Header("━━━━ 冬季天气规则 ━━━━")]
    [Tooltip("冬季下雪日（树苗休眠，挂冰）")]
    public List<int> winterSnowDays = new List<int> { 1, 5, 11, 21, 26 };
    
    [Tooltip("冬季融化日（大太阳，冰雪融化）")]
    public List<int> winterMeltDays = new List<int> { 3, 8, 17, 24, 28 };
    
    [Header("━━━━ 调试 ━━━━")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("━━━━ 天气事件开关 ━━━━")]
    [Tooltip("是否发布植物枯萎事件（OnPlantsWither）\n" +
             "关闭后：极端高温不会导致植物枯萎")]
    [SerializeField] private bool enableWitherEvent = true;
    
    [Tooltip("是否发布植物恢复事件（OnPlantsRecover）\n" +
             "关闭后：雨后植物不会自动恢复")]
    [SerializeField] private bool enableRecoverEvent = true;
    
    [Tooltip("是否发布冬季下雪事件（OnWinterSnow）\n" +
             "关闭后：冬季下雪不会影响植物")]
    [SerializeField] private bool enableWinterSnowEvent = true;
    
    [Tooltip("是否发布冬季融化事件（OnWinterMelt）\n" +
             "关闭后：冬季融化不会影响植物")]
    [SerializeField] private bool enableWinterMeltEvent = true;
    
    // 上一次下雨的日期（总天数）
    private int lastRainyDay = -1;
    
    // 当前是否处于雨后恢复期
    private bool isPostRainRecovery = false;
    
    #region 事件系统
    /// <summary>天气变化事件</summary>
    public static event Action<Weather> OnWeatherChanged;
    
    /// <summary>植物枯萎事件（所有植物应枯萎）</summary>
    public static event Action OnPlantsWither;
    
    /// <summary>植物恢复事件（枯萎的植物应恢复）</summary>
    public static event Action OnPlantsRecover;
    
    /// <summary>冬季下雪事件（树苗休眠，挂冰）</summary>
    public static event Action OnWinterSnow;
    
    /// <summary>冬季融化事件（大太阳，冰雪融化）</summary>
    public static event Action OnWinterMelt;
    #endregion
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // ✅ DontDestroyOnLoad 由 PersistentManagers 统一处理
            // 不再在此调用，避免 "only works for root GameObjects" 警告
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 订阅时间事件
        TimeManager.OnDayChanged += OnDayChanged;
        TimeManager.OnSeasonChanged += OnSeasonChanged;
        
        // 初始化天气
        CheckWeather();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[WeatherSystem] 初始化完成</color>");
        }
    }
    
    private void OnDestroy()
    {
        TimeManager.OnDayChanged -= OnDayChanged;
        TimeManager.OnSeasonChanged -= OnSeasonChanged;
    }
    
    /// <summary>
    /// 每日回调 - 检查天气
    /// </summary>
    private void OnDayChanged(int year, int seasonDay, int totalDays)
    {
        CheckWeather();
    }
    
    /// <summary>
    /// 季节变化回调
    /// </summary>
    private void OnSeasonChanged(SeasonManager.Season newSeason, int year)
    {
        // 季节变化时重置天气
        CheckWeather();
        
        // 冬季：所有植物枯萎
        if (newSeason == SeasonManager.Season.Winter)
        {
            TriggerPlantsWither("冬季到来");
        }
        // 离开冬季：植物恢复
        else if (TimeManager.Instance != null)
        {
            SeasonManager.Season prevSeason = (SeasonManager.Season)(((int)newSeason - 1 + 4) % 4);
            if (prevSeason == SeasonManager.Season.Winter)
            {
                TriggerPlantsRecover("春季复苏");
            }
        }
    }
    
    /// <summary>
    /// 检查并更新天气
    /// </summary>
    private void CheckWeather()
    {
        if (TimeManager.Instance == null) return;
        
        SeasonManager.Season currentSeason = TimeManager.Instance.GetSeason();
        int currentDay = TimeManager.Instance.GetDay();
        int totalDays = TimeManager.Instance.GetTotalDaysPassed();
        
        Weather newWeather = Weather.Sunny; // 默认晴天
        
        // 冬季：下雪日（挂冰）或融化日（晴天）
        if (currentSeason == SeasonManager.Season.Winter)
        {
            if (winterSnowDays.Contains(currentDay))
            {
                newWeather = Weather.Withering; // 使用Withering表示下雪
                
                // ★ 受天气事件开关控制
                if (enableWinterSnowEvent)
                {
                    OnWinterSnow?.Invoke();
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=cyan>[WeatherSystem] ❄️ 冬季第{currentDay}天下雪（树苗休眠，挂冰）</color>");
                }
            }
            else if (winterMeltDays.Contains(currentDay))
            {
                newWeather = Weather.Sunny;
                
                // ★ 受天气事件开关控制
                if (enableWinterMeltEvent)
                {
                    OnWinterMelt?.Invoke();
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>[WeatherSystem] ☀️ 冬季第{currentDay}天大太阳（冰雪融化）</color>");
                }
            }
            else
            {
                // 其他日子保持上一天的状态
                newWeather = currentWeather;
            }
        }
        // 夏季：检查特殊天气
        else if (currentSeason == SeasonManager.Season.Summer)
        {
            // 检查是否是枯萎日
            if (summerWitheringDays.Contains(currentDay))
            {
                newWeather = Weather.Withering;
                TriggerPlantsWither($"夏季第{currentDay}天高温");
            }
            // 检查是否是雨天
            else if (summerRainyDays.Contains(currentDay))
            {
                newWeather = Weather.Rainy;
                lastRainyDay = totalDays;
                isPostRainRecovery = false; // 重置恢复标记
            }
            // 检查是否是雨后第二天（恢复日）
            else if (lastRainyDay >= 0 && totalDays == lastRainyDay + 1 && !isPostRainRecovery)
            {
                newWeather = Weather.Sunny;
                isPostRainRecovery = true;
                TriggerPlantsRecover("雨后恢复");
            }
        }
        // 秋季：检查枯萎日（直接枯萎，不恢复，无雨）
        else if (currentSeason == SeasonManager.Season.Autumn)
        {
            if (fallWitheringDays.Contains(currentDay))
            {
                newWeather = Weather.Withering;
                TriggerPlantsWither($"秋季第{currentDay}天枯萎（不恢复）");
            }
        }
        
        // 更新天气
        if (currentWeather != newWeather)
        {
            SetWeather(newWeather);
        }
    }
    
    /// <summary>
    /// 设置天气
    /// </summary>
    private void SetWeather(Weather weather)
    {
        currentWeather = weather;
        OnWeatherChanged?.Invoke(currentWeather);
        
        if (showDebugInfo)
        {
            string weatherName = GetWeatherName(weather);
            Debug.Log($"<color=yellow>[WeatherSystem] 天气变化: {weatherName}</color>");
        }
    }
    
    /// <summary>
    /// 触发植物枯萎
    /// </summary>
    private void TriggerPlantsWither(string reason)
    {
        // ★ 受天气事件开关控制
        if (enableWitherEvent)
        {
            OnPlantsWither?.Invoke();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=red>[WeatherSystem] 🥀 植物枯萎 - {reason}</color>");
        }
    }
    
    /// <summary>
    /// 触发植物恢复
    /// </summary>
    private void TriggerPlantsRecover(string reason)
    {
        // ★ 受天气事件开关控制
        if (enableRecoverEvent)
        {
            OnPlantsRecover?.Invoke();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=green>[WeatherSystem] 🌱 植物恢复 - {reason}</color>");
        }
    }
    
    #region 公共接口
    /// <summary>
    /// 获取当前天气
    /// </summary>
    public Weather GetCurrentWeather()
    {
        return currentWeather;
    }
    
    /// <summary>
    /// 是否是晴天
    /// </summary>
    public bool IsSunny()
    {
        return currentWeather == Weather.Sunny;
    }
    
    /// <summary>
    /// 是否是雨天
    /// </summary>
    public bool IsRainy()
    {
        return currentWeather == Weather.Rainy;
    }
    
    /// <summary>
    /// 是否是枯萎天（植物应枯萎）
    /// </summary>
    public bool IsWithering()
    {
        return currentWeather == Weather.Withering;
    }
    
    /// <summary>
    /// 获取天气名称
    /// </summary>
    public string GetWeatherName(Weather weather)
    {
        switch (weather)
        {
            case Weather.Sunny: return "☀️ 晴天";
            case Weather.Rainy: return "🌧️ 雨天";
            case Weather.Withering: return "🥀 枯萎天";
            default: return "未知";
        }
    }
    
    /// <summary>
    /// 获取当前天气名称
    /// </summary>
    public string GetCurrentWeatherName()
    {
        return GetWeatherName(currentWeather);
    }
    #endregion
}

