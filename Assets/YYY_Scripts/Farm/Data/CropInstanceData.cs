using UnityEngine;

namespace FarmGame.Farm
{
    /// <summary>
    /// 作物实例数据 - 纯数据类，可序列化
    /// 用于存储单个作物的运行时状态
    /// </summary>
    [System.Serializable]
    public class CropInstanceData
    {
        #region 种子信息
        
        /// <summary>
        /// 种子 ID（用于从 ItemDatabase 获取 SeedData）
        /// </summary>
        public int seedDataID;
        
        #endregion

        #region 生长状态
        
        /// <summary>
        /// 当前生长阶段（0 = 种子，N = 成熟）
        /// </summary>
        public int currentStage;
        
        /// <summary>
        /// 种植时的游戏天数
        /// </summary>
        public int plantedDay;
        
        /// <summary>
        /// 实际生长天数（不包含停滞天数）
        /// </summary>
        public int grownDays;
        
        #endregion

        #region 浇水状态
        
        /// <summary>
        /// 连续未浇水天数
        /// </summary>
        public int daysWithoutWater;
        
        /// <summary>
        /// 是否枯萎
        /// </summary>
        public bool isWithered;
        
        #endregion

        #region 收获状态
        
        /// <summary>
        /// 已收获次数（可重复收获作物用）
        /// </summary>
        public int harvestCount;
        
        /// <summary>
        /// 上次收获的天数（可重复收获作物用）
        /// </summary>
        public int lastHarvestDay;
        
        #endregion

        #region 品质
        
        /// <summary>
        /// 作物品质（0 = Normal, 1 = Silver, 2 = Gold, 3 = Iridium）
        /// </summary>
        public int quality;
        
        #endregion

        #region 构造函数
        
        /// <summary>
        /// 默认构造函数（用于序列化）
        /// </summary>
        public CropInstanceData()
        {
            seedDataID = 0;
            currentStage = 0;
            plantedDay = 0;
            grownDays = 0;
            daysWithoutWater = 0;
            isWithered = false;
            harvestCount = 0;
            lastHarvestDay = -1;
            quality = 0;
        }
        
        /// <summary>
        /// 创建新作物实例数据
        /// </summary>
        /// <param name="seedID">种子 ID</param>
        /// <param name="currentDay">当前游戏天数</param>
        public CropInstanceData(int seedID, int currentDay)
        {
            seedDataID = seedID;
            currentStage = 0;
            plantedDay = currentDay;
            grownDays = 0;
            daysWithoutWater = 0;
            isWithered = false;
            harvestCount = 0;
            lastHarvestDay = -1;
            quality = 0;
        }
        
        #endregion

        #region 状态查询
        
        /// <summary>
        /// 检查是否可以再次收获（可重复收获作物）
        /// </summary>
        /// <param name="currentDay">当前游戏天数</param>
        /// <param name="reHarvestDays">重复收获间隔天数</param>
        /// <returns>是否可以收获</returns>
        public bool CanReHarvest(int currentDay, int reHarvestDays)
        {
            if (lastHarvestDay < 0) return true; // 第一次收获
            
            int daysSinceLastHarvest = currentDay - lastHarvestDay;
            return daysSinceLastHarvest >= reHarvestDays;
        }
        
        #endregion
    }
}
