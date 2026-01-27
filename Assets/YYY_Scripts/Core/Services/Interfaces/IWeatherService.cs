namespace Sunset.Services
{
    /// <summary>
    /// 天气服务接口
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>获取当前天气</summary>
        WeatherSystem.Weather GetCurrentWeather();
        
        /// <summary>是否是晴天</summary>
        bool IsSunny();
        
        /// <summary>是否是雨天</summary>
        bool IsRainy();
        
        /// <summary>是否是枯萎天</summary>
        bool IsWithering();
        
        /// <summary>获取天气名称</summary>
        string GetCurrentWeatherName();
    }
}
