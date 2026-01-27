using UnityEngine;

/// <summary>
/// TimeManager调试器 - 独立组件，方便删除
/// 提供快捷键控制时间跳转
/// ⚠️ 仅用于开发调试，发布前可直接删除此脚本
/// </summary>
public class TimeManagerDebugger : MonoBehaviour
{
    [Header("━━━━ 调试快捷键 ━━━━")]
    [Tooltip("启用调试快捷键")]
    public bool enableDebugKeys = true;
    
    [Header("方向键控制")]
    [Tooltip("→ 右箭头：跳到下一天")]
    public KeyCode nextDayKey = KeyCode.RightArrow;
    
    [Tooltip("↓ 下箭头：跳到下一季节")]
    public KeyCode nextSeasonKey = KeyCode.DownArrow;
    
    [Tooltip("↑ 上箭头：跳到上一季节")]
    public KeyCode prevSeasonKey = KeyCode.UpArrow;
    
    [Header("其他快捷键")]
    [Tooltip("T键：切换时间倍速（1x/5x）")]
    public KeyCode toggleSpeedKey = KeyCode.T;
    
    [Tooltip("P键：暂停/继续")]
    public KeyCode pauseKey = KeyCode.P;
    
    [Header("显示设置")]
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = true;
    
    private void Update()
    {
        if (!enableDebugKeys || TimeManager.Instance == null) return;
        
        // → 右箭头：下一天
        if (Input.GetKeyDown(nextDayKey))
        {
            AdvanceDay();
        }
        
        // ↓ 下箭头：下一季节
        if (Input.GetKeyDown(nextSeasonKey))
        {
            AdvanceSeason();
        }
        
        // ↑ 上箭头：上一季节
        if (Input.GetKeyDown(prevSeasonKey))
        {
            PreviousSeason();
        }
        
        // T键：切换倍速
        if (Input.GetKeyDown(toggleSpeedKey))
        {
            ToggleTimeScale();
        }
        
        // P键：暂停/继续
        if (Input.GetKeyDown(pauseKey))
        {
            TimeManager.Instance.TogglePause();
        }
    }
    
    /// <summary>
    /// 前进到下一天
    /// </summary>
    private void AdvanceDay()
    {
        TimeManager.Instance.Sleep();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[Debugger] → 跳到下一天: {TimeManager.Instance.GetFormattedTime()}</color>");
        }
    }
    
    /// <summary>
    /// 前进到下一季节
    /// </summary>
    private void AdvanceSeason()
    {
        SeasonManager.Season currentSeason = TimeManager.Instance.GetSeason();
        int nextSeasonIndex = ((int)currentSeason + 1) % 4;
        SeasonManager.Season nextSeason = (SeasonManager.Season)nextSeasonIndex;
        
        // 跳到下一季的第1天
        int currentYear = TimeManager.Instance.GetYear();
        if (nextSeason == SeasonManager.Season.Spring)
        {
            currentYear++; // 新年
        }
        
        TimeManager.Instance.SetTime(currentYear, nextSeason, 1, 6, 0);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=orange>[Debugger] ↓ 跳到下一季节: {nextSeason} (Year {currentYear})</color>");
        }
    }
    
    /// <summary>
    /// 返回到上一季节
    /// </summary>
    private void PreviousSeason()
    {
        SeasonManager.Season currentSeason = TimeManager.Instance.GetSeason();
        int prevSeasonIndex = ((int)currentSeason - 1 + 4) % 4;
        SeasonManager.Season prevSeason = (SeasonManager.Season)prevSeasonIndex;
        
        // 跳到上一季的第1天
        int currentYear = TimeManager.Instance.GetYear();
        if (prevSeason == SeasonManager.Season.Winter)
        {
            currentYear = Mathf.Max(1, currentYear - 1); // 上一年（最小Year 1）
        }
        
        TimeManager.Instance.SetTime(currentYear, prevSeason, 1, 6, 0);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>[Debugger] ↑ 跳到上一季节: {prevSeason} (Year {currentYear})</color>");
        }
    }
    
    /// <summary>
    /// 切换时间倍速
    /// </summary>
    private void ToggleTimeScale()
    {
        // 在1x和5x之间切换
        float currentScale = TimeManager.Instance.GetType()
            .GetField("timeScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(TimeManager.Instance) as float? ?? 1f;
        
        float newScale = currentScale >= 5f ? 1f : 5f;
        TimeManager.Instance.SetTimeScale(newScale);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=lime>[Debugger] ⚡ 时间倍速: {newScale}x</color>");
        }
    }
    
    private void OnGUI()
    {
        if (!enableDebugKeys) return;
        
        // 显示快捷键提示（左上角）
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        
        string helpText = "=== 调试快捷键 ===\n" +
                         "→  下一天\n" +
                         "↓  下一季节\n" +
                         "↑  上一季节\n" +
                         "T  切换倍速\n" +
                         "P  暂停/继续";
        
        GUI.Label(new Rect(10, 10, 200, 120), helpText, style);
    }
}

