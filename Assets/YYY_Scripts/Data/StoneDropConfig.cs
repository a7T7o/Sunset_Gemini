using UnityEngine;
using FarmGame.Data;

namespace FarmGame.Data
{
    /// <summary>
    /// 石头矿物和石料掉落配置表
    /// 用于计算差值掉落数量
    /// </summary>
    public static class StoneDropConfig
    {
        #region 矿物总量配置
        
        /// <summary>
        /// M1 阶段矿物总量（索引=含量指数）
        /// M1_0=0, M1_1=3, M1_2=5, M1_3=7, M1_4=9
        /// </summary>
        public static readonly int[] M1_OreTotals = { 0, 3, 5, 7, 9 };
        
        /// <summary>
        /// M2 阶段矿物总量（索引=含量指数）
        /// M2_0=0, M2_1=1, M2_2=3, M2_3=5, M2_4=7
        /// </summary>
        public static readonly int[] M2_OreTotals = { 0, 1, 3, 5, 7 };
        
        /// <summary>
        /// M3 阶段矿物总量（索引=含量指数，最大为3）
        /// M3_0=0, M3_1=1, M3_2=2, M3_3=3
        /// </summary>
        public static readonly int[] M3_OreTotals = { 0, 1, 2, 3 };
        
        #endregion
        
        #region 石料总量配置
        
        /// <summary>
        /// 各阶段石料总量（索引=阶段）
        /// M1=12, M2=6, M3=2, M4=2
        /// 注：M3 与 M4 石料总量一致
        /// </summary>
        public static readonly int[] StoneTotals = { 12, 6, 2, 2 };
        
        #endregion
        
        #region 经验配置
        
        /// <summary>每个矿物提供的经验值</summary>
        public const int XP_PER_ORE = 2;
        
        /// <summary>每个石料提供的经验值</summary>
        public const int XP_PER_STONE = 1;
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 获取指定阶段和含量的矿物总量
        /// </summary>
        /// <param name="stage">石头阶段</param>
        /// <param name="oreIndex">矿物含量指数（0-4）</param>
        /// <returns>矿物总量</returns>
        public static int GetOreTotal(StoneStage stage, int oreIndex)
        {
            if (oreIndex < 0) return 0;
            
            switch (stage)
            {
                case StoneStage.M1:
                    return oreIndex < M1_OreTotals.Length ? M1_OreTotals[oreIndex] : 0;
                case StoneStage.M2:
                    return oreIndex < M2_OreTotals.Length ? M2_OreTotals[oreIndex] : 0;
                case StoneStage.M3:
                    return oreIndex < M3_OreTotals.Length ? M3_OreTotals[oreIndex] : 0;
                case StoneStage.M4:
                    return 0; // M4 是装饰石头，无矿物
                default:
                    return 0;
            }
        }
        
        /// <summary>
        /// 获取指定阶段的石料总量
        /// </summary>
        /// <param name="stage">石头阶段</param>
        /// <returns>石料总量</returns>
        public static int GetStoneTotal(StoneStage stage)
        {
            int index = (int)stage;
            return index >= 0 && index < StoneTotals.Length ? StoneTotals[index] : 0;
        }
        
        /// <summary>
        /// 计算阶段转换时的矿物掉落数量（差值掉落）
        /// </summary>
        /// <param name="fromStage">当前阶段</param>
        /// <param name="oreIndex">当前矿物含量指数</param>
        /// <param name="toStage">目标阶段</param>
        /// <param name="newOreIndex">目标矿物含量指数</param>
        /// <returns>应掉落的矿物数量</returns>
        public static int CalculateOreDropAmount(StoneStage fromStage, int oreIndex, StoneStage toStage, int newOreIndex)
        {
            int currentTotal = GetOreTotal(fromStage, oreIndex);
            int nextTotal = GetOreTotal(toStage, newOreIndex);
            return Mathf.Max(0, currentTotal - nextTotal);
        }
        
        /// <summary>
        /// 计算阶段转换时的石料掉落数量（差值掉落）
        /// </summary>
        /// <param name="fromStage">当前阶段</param>
        /// <param name="toStage">目标阶段（最终阶段时传入自身）</param>
        /// <returns>应掉落的石料数量</returns>
        public static int CalculateStoneDropAmount(StoneStage fromStage, StoneStage toStage)
        {
            int currentTotal = GetStoneTotal(fromStage);
            int nextTotal = GetStoneTotal(toStage);
            
            // 如果是最终阶段（fromStage == toStage），掉落全部石料
            if (fromStage == toStage)
            {
                return currentTotal;
            }
            
            return Mathf.Max(0, currentTotal - nextTotal);
        }
        
        /// <summary>
        /// 计算最终阶段的矿物掉落数量（全部掉落）
        /// </summary>
        /// <param name="stage">当前阶段（必须是最终阶段）</param>
        /// <param name="oreIndex">矿物含量指数</param>
        /// <returns>应掉落的矿物数量</returns>
        public static int CalculateFinalOreDropAmount(StoneStage stage, int oreIndex)
        {
            return GetOreTotal(stage, oreIndex);
        }
        
        /// <summary>
        /// 计算最终阶段的石料掉落数量（全部掉落）
        /// </summary>
        /// <param name="stage">当前阶段（必须是最终阶段）</param>
        /// <returns>应掉落的石料数量</returns>
        public static int CalculateFinalStoneDropAmount(StoneStage stage)
        {
            return GetStoneTotal(stage);
        }
        
        /// <summary>
        /// 计算经验值
        /// </summary>
        /// <param name="oreCount">矿物数量</param>
        /// <param name="stoneCount">石料数量</param>
        /// <returns>总经验值</returns>
        public static int CalculateExperience(int oreCount, int stoneCount)
        {
            return oreCount * XP_PER_ORE + stoneCount * XP_PER_STONE;
        }
        
        #endregion
    }
}
