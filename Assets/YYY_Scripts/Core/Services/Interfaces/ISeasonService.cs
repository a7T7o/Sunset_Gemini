namespace Sunset.Services
{
    /// <summary>
    /// 季节服务接口
    /// </summary>
    public interface ISeasonService
    {
        /// <summary>获取当前日历季节</summary>
        SeasonManager.Season GetCurrentSeason();
        
        /// <summary>获取当前植被季节</summary>
        SeasonManager.VegetationSeason GetCurrentVegetationSeason();
        
        /// <summary>获取季节过渡进度（0.0-1.0）</summary>
        float GetTransitionProgress();
        
        /// <summary>获取季节名称</summary>
        string GetSeasonName(SeasonManager.Season season);
        
        /// <summary>设置季节</summary>
        void SetSeason(SeasonManager.Season newSeason);
    }
}
