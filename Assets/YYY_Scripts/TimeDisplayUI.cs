using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 时间显示UI - 显示游戏内时间
/// 挂载到Canvas上的Text/TextMeshPro对象
/// </summary>
public class TimeDisplayUI : MonoBehaviour
{
    [Header("UI组件")]
    [Tooltip("使用TextMeshPro显示时间（推荐）")]
    public TextMeshProUGUI timeText_TMP;
    
    [Tooltip("或使用Unity UI Text显示时间")]
    public Text timeText_Legacy;
    
    [Header("显示设置")]
    [Tooltip("时间显示格式\n{0}=年, {1}=季节, {2}=日, {3}=时间\n例: Year {0} {1} Day {2} {3}")]
    public string timeFormat = "Year {0} {1}\nDay {2} {3}";
    
    [Tooltip("简洁模式（只显示时间）")]
    public bool compactMode = false;
    
    [Tooltip("简洁模式格式")]
    public string compactFormat = "{3}";
    
    [Header("季节显示")]
    [Tooltip("使用中文季节名")]
    public bool useChineseSeasonName = true;
    
    [Header("更新设置")]
    [Tooltip("更新间隔（秒，0=每帧更新）")]
    public float updateInterval = 1f;
    
    private float updateTimer = 0f;
    
    private void Start()
    {
        // 订阅时间变化事件
        TimeManager.OnMinuteChanged += OnTimeChanged;
        
        // 立即更新一次
        UpdateTimeDisplay();
    }
    
    private void OnDestroy()
    {
        // 取消订阅
        TimeManager.OnMinuteChanged -= OnTimeChanged;
    }
    
    private void Update()
    {
        if (updateInterval > 0)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateTimeDisplay();
            }
        }
    }
    
    private void OnTimeChanged(int hour, int minute)
    {
        if (updateInterval == 0)
        {
            UpdateTimeDisplay();
        }
    }
    
    private void UpdateTimeDisplay()
    {
        if (TimeManager.Instance == null) return;
        
        int year = TimeManager.Instance.GetYear();
        SeasonManager.Season season = TimeManager.Instance.GetSeason();
        int day = TimeManager.Instance.GetDay();
        int hour = TimeManager.Instance.GetHour();
        int minute = TimeManager.Instance.GetMinute();
        
        string seasonName = GetSeasonName(season);
        string time = FormatTime(hour, minute);
        
        string displayText;
        if (compactMode)
        {
            displayText = string.Format(compactFormat, year, seasonName, day, time);
        }
        else
        {
            displayText = string.Format(timeFormat, year, seasonName, day, time);
        }
        
        // 更新UI
        if (timeText_TMP != null)
        {
            timeText_TMP.text = displayText;
        }
        else if (timeText_Legacy != null)
        {
            timeText_Legacy.text = displayText;
        }
    }
    
    private string GetSeasonName(SeasonManager.Season season)
    {
        if (useChineseSeasonName)
        {
            switch (season)
            {
                case SeasonManager.Season.Spring: return "春天";
                case SeasonManager.Season.Summer: return "夏天";
                case SeasonManager.Season.Autumn: return "秋天";
                case SeasonManager.Season.Winter: return "冬天";
                default: return "未知";
            }
        }
        else
        {
            return season.ToString();
        }
    }
    
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
}

