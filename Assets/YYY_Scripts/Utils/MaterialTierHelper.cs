using UnityEngine;
using FarmGame.Data;

namespace FarmGame.Utils
{
    /// <summary>
    /// 材料等级判定工具类
    /// 用于判断工具是否能采集特定资源
    /// </summary>
    public static class MaterialTierHelper
    {
        #region 镐子采集判定
        
        /// <summary>
        /// 检查镐子是否能获取指定矿石的矿物
        /// 注意：所有镐子都能获得石料，此方法只判断矿物
        /// 
        /// 镐子采集能力表：
        /// | 镐子材质 | 材料等级 | 可获取矿物 |
        /// |---------|---------|-----------|
        /// | 木镐    | 0       | 无        |
        /// | 石镐    | 1       | 铁矿(C2)  |
        /// | 生铁镐  | 2       | 铁矿、铜矿 |
        /// | 黄铜镐  | 3       | 铁矿、铜矿 |
        /// | 钢镐    | 4       | 所有矿物  |
        /// | 金镐    | 5       | 所有矿物  |
        /// </summary>
        /// <param name="pickaxeTier">镐子材料等级（0-5）</param>
        /// <param name="oreType">矿物类型</param>
        /// <returns>是否能获取矿物</returns>
        public static bool CanMineOre(int pickaxeTier, OreType oreType)
        {
            // 纯石头或无矿物：返回 true（实际上不会掉落矿物）
            if (oreType == OreType.None) return true;
            
            switch (oreType)
            {
                case OreType.C2: // 铁矿 - 需要石镐(1)或更高
                    return pickaxeTier >= 1;
                    
                case OreType.C1: // 铜矿 - 需要生铁镐(2)或更高
                    return pickaxeTier >= 2;
                    
                case OreType.C3: // 金矿 - 需要钢镐(4)或更高
                    return pickaxeTier >= 4;
                    
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// 检查镐子是否能获取指定矿石的矿物（使用枚举）
        /// </summary>
        public static bool CanMineOre(MaterialTier pickaxeTier, OreType oreType)
        {
            return CanMineOre((int)pickaxeTier, oreType);
        }
        
        /// <summary>
        /// 所有镐子都能获得石料
        /// </summary>
        public static bool CanGetStone(int pickaxeTier) => true;
        
        /// <summary>
        /// 获取采集指定矿石所需的最低镐子等级
        /// </summary>
        /// <param name="oreType">矿物类型</param>
        /// <returns>所需最低材料等级</returns>
        public static int GetRequiredPickaxeTier(OreType oreType)
        {
            return oreType switch
            {
                OreType.None => 0,  // 纯石头：木镐即可
                OreType.C2 => 1,    // 铁矿：石镐
                OreType.C1 => 2,    // 铜矿：生铁镐
                OreType.C3 => 4,    // 金矿：钢镐
                _ => 0
            };
        }
        
        #endregion
        
        #region 斧头砍伐判定
        
        /// <summary>
        /// 检查斧头是否能砍伐指定阶段的树木
        /// 
        /// 树木砍伐限制表：
        /// | 斧头等级 | 可砍伐树木阶段 |
        /// |---------|---------------|
        /// | 0 (木斧) | 0-2           |
        /// | 1 (石斧) | 0-3           |
        /// | 2 (生铁斧) | 0-4         |
        /// | 3+ (黄铜斧及以上) | 0-5 (全部) |
        /// </summary>
        /// <param name="axeTier">斧头材料等级（0-5）</param>
        /// <param name="treeStage">树木阶段（0-5）</param>
        /// <returns>是否能砍伐</returns>
        public static bool CanChopTree(int axeTier, int treeStage)
        {
            int maxChoppableStage = GetMaxChoppableTreeStage(axeTier);
            return treeStage <= maxChoppableStage;
        }
        
        /// <summary>
        /// 检查斧头是否能砍伐指定阶段的树木（使用枚举）
        /// </summary>
        public static bool CanChopTree(MaterialTier axeTier, int treeStage)
        {
            return CanChopTree((int)axeTier, treeStage);
        }
        
        /// <summary>
        /// 获取指定斧头等级能砍伐的最大树木阶段
        /// </summary>
        /// <param name="axeTier">斧头材料等级（0-5）</param>
        /// <returns>能砍伐的最大树木阶段</returns>
        public static int GetMaxChoppableTreeStage(int axeTier)
        {
            return axeTier switch
            {
                0 => 2,  // 木斧：可砍 0-2 阶段
                1 => 3,  // 石斧：可砍 0-3 阶段
                2 => 4,  // 生铁斧：可砍 0-4 阶段
                _ => 5   // 黄铜斧(3)及以上：可砍所有阶段
            };
        }
        
        /// <summary>
        /// 获取砍伐指定阶段树木所需的最低斧头等级
        /// </summary>
        /// <param name="treeStage">树木阶段（0-5）</param>
        /// <returns>所需最低材料等级</returns>
        public static int GetRequiredAxeTier(int treeStage)
        {
            return treeStage switch
            {
                0 => 0,  // 阶段0：木斧即可
                1 => 0,  // 阶段1：木斧即可
                2 => 0,  // 阶段2：木斧即可
                3 => 1,  // 阶段3：需要石斧
                4 => 2,  // 阶段4：需要生铁斧
                5 => 3,  // 阶段5：需要黄铜斧
                _ => 0
            };
        }
        
        #endregion
        
        #region 工具类型判定
        
        /// <summary>
        /// 获取材料等级的中文名称
        /// </summary>
        public static string GetTierName(int tier)
        {
            return tier switch
            {
                0 => "木质",
                1 => "石质",
                2 => "生铁",
                3 => "黄铜",
                4 => "钢质",
                5 => "金质",
                _ => "未知"
            };
        }
        
        /// <summary>
        /// 获取材料等级的中文名称（使用枚举）
        /// </summary>
        public static string GetTierName(MaterialTier tier)
        {
            return GetTierName((int)tier);
        }
        
        /// <summary>
        /// 获取矿物类型的中文名称
        /// </summary>
        public static string GetOreTypeName(OreType oreType)
        {
            return oreType switch
            {
                OreType.None => "纯石头",
                OreType.C1 => "铜矿",
                OreType.C2 => "铁矿",
                OreType.C3 => "金矿",
                _ => "未知"
            };
        }
        
        #endregion
    }
}
