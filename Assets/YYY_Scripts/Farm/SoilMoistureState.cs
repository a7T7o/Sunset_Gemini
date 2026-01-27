namespace FarmGame.Farm
{
    /// <summary>
    /// 土壤湿度状态（用于视觉显示）
    /// </summary>
    public enum SoilMoistureState
    {
        Dry = 0,            // 干燥（未浇水）
        WetWithPuddle = 1,  // 湿润+水渍（浇水后2小时内）
        WetDark = 2         // 湿润+深色（浇水2小时后，直到第二天）
    }
}
