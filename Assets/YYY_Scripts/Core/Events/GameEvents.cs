using UnityEngine;
using FarmGame.Data;

namespace Sunset.Events
{
    #region 时间系统事件
    
    /// <summary>
    /// 分钟变化事件（每10分钟触发）
    /// </summary>
    public class MinuteChangedEvent : IGameEvent
    {
        public int Hour { get; set; }
        public int Minute { get; set; }
    }
    
    /// <summary>
    /// 小时变化事件
    /// </summary>
    public class HourChangedEvent : IGameEvent
    {
        public int Hour { get; set; }
    }
    
    /// <summary>
    /// 天变化事件
    /// </summary>
    public class DayChangedEvent : IGameEvent
    {
        public int Year { get; set; }
        public int SeasonDay { get; set; }
        public int TotalDays { get; set; }
    }
    
    /// <summary>
    /// 季节变化事件
    /// </summary>
    public class SeasonChangedEvent : IGameEvent
    {
        public SeasonManager.Season NewSeason { get; set; }
        public int Year { get; set; }
    }
    
    /// <summary>
    /// 年变化事件
    /// </summary>
    public class YearChangedEvent : IGameEvent
    {
        public int Year { get; set; }
    }
    
    /// <summary>
    /// 睡眠事件
    /// </summary>
    public class SleepEvent : IGameEvent
    {
    }
    
    #endregion
    
    #region 天气系统事件
    
    /// <summary>
    /// 天气变化事件
    /// </summary>
    public class WeatherChangedEvent : IGameEvent
    {
        public WeatherSystem.Weather NewWeather { get; set; }
    }
    
    /// <summary>
    /// 植物枯萎事件
    /// </summary>
    public class PlantsWitherEvent : IGameEvent
    {
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// 植物恢复事件
    /// </summary>
    public class PlantsRecoverEvent : IGameEvent
    {
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// 冬季下雪事件
    /// </summary>
    public class WinterSnowEvent : IGameEvent
    {
    }
    
    /// <summary>
    /// 冬季融化事件
    /// </summary>
    public class WinterMeltEvent : IGameEvent
    {
    }
    
    #endregion
    
    #region 植被系统事件
    
    /// <summary>
    /// 植被季节变化事件
    /// </summary>
    public class VegetationSeasonChangedEvent : IGameEvent
    {
        public SeasonManager.VegetationSeason NewVegetationSeason { get; set; }
        public float TransitionProgress { get; set; }
    }
    
    #endregion
    
    #region 导航系统事件
    
    /// <summary>
    /// 请求导航网格刷新事件
    /// </summary>
    public class NavGridRefreshRequestEvent : IGameEvent
    {
        public Vector2? Position { get; set; } // 可选：指定刷新位置
        public float Radius { get; set; } = 0f; // 可选：指定刷新半径（0表示全局刷新）
    }
    
    #endregion
    
    #region 背包系统事件
    
    /// <summary>
    /// 背包变化事件
    /// </summary>
    public class InventoryChangedEvent : IGameEvent
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Amount { get; set; }
        public int Quality { get; set; }
    }
    
    /// <summary>
    /// 工具栏选择变化事件
    /// </summary>
    public class HotbarSelectionChangedEvent : IGameEvent
    {
        public int PreviousIndex { get; set; }
        public int NewIndex { get; set; }
    }
    
    /// <summary>
    /// 工具装备事件
    /// </summary>
    public class ToolEquippedEvent : IGameEvent
    {
        public int ItemId { get; set; }
        public int Quality { get; set; }
        public ToolType ToolType { get; set; }
    }
    
    #endregion
    
    #region 玩家事件
    
    /// <summary>
    /// 玩家动作开始事件
    /// </summary>
    public class PlayerActionStartEvent : IGameEvent
    {
        public PlayerAnimController.AnimState Action { get; set; }
        public PlayerAnimController.AnimDirection Direction { get; set; }
    }
    
    /// <summary>
    /// 玩家动作完成事件
    /// </summary>
    public class PlayerActionCompleteEvent : IGameEvent
    {
        public PlayerAnimController.AnimState Action { get; set; }
    }
    
    #endregion
    
    #region UI事件
    
    /// <summary>
    /// 面板打开事件
    /// </summary>
    public class PanelOpenedEvent : IGameEvent
    {
        public string PanelName { get; set; }
        public int PageIndex { get; set; }
    }
    
    /// <summary>
    /// 面板关闭事件
    /// </summary>
    public class PanelClosedEvent : IGameEvent
    {
        public string PanelName { get; set; }
    }
    
    #endregion
}
