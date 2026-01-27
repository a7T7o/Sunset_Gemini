namespace Sunset.Services
{
    /// <summary>
    /// 时间服务接口
    /// </summary>
    public interface ITimeService
    {
        /// <summary>获取当前年份</summary>
        int GetYear();
        
        /// <summary>获取当前季节</summary>
        SeasonManager.Season GetSeason();
        
        /// <summary>获取当前是本季第几天</summary>
        int GetDay();
        
        /// <summary>获取当前小时</summary>
        int GetHour();
        
        /// <summary>获取当前分钟</summary>
        int GetMinute();
        
        /// <summary>获取总天数</summary>
        int GetTotalDaysPassed();
        
        /// <summary>是否是白天</summary>
        bool IsDaytime();
        
        /// <summary>是否是夜晚</summary>
        bool IsNighttime();
        
        /// <summary>获取当天进度（0-1）</summary>
        float GetDayProgress();
        
        /// <summary>获取格式化时间字符串</summary>
        string GetFormattedTime();
        
        /// <summary>睡觉/跳过到下一天</summary>
        void Sleep();
        
        /// <summary>暂停/继续时间</summary>
        void TogglePause();
        
        /// <summary>设置时间流逝速度</summary>
        void SetTimeScale(float scale);
        
        /// <summary>设置暂停状态</summary>
        void SetPaused(bool paused);
        
        /// <summary>设置具体时间</summary>
        void SetTime(int year, SeasonManager.Season season, int day, int hour, int minute);
    }
}
