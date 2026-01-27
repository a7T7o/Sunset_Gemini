namespace FarmGame.Data
{
    /// <summary>
    /// 石头阶段枚举（4个阶段）
    /// M1（最大）→ M2 → M3（最终阶段）
    /// M4 是独立的装饰石头
    /// </summary>
    public enum StoneStage
    {
        /// <summary>最大阶段，血量36</summary>
        M1 = 0,
        
        /// <summary>中等阶段，血量17</summary>
        M2 = 1,
        
        /// <summary>最小可进阶阶段，血量9，最终阶段</summary>
        M3 = 2,
        
        /// <summary>装饰石头，血量4，最终阶段</summary>
        M4 = 3
    }
    
    /// <summary>
    /// 矿物类型枚举
    /// </summary>
    public enum OreType
    {
        /// <summary>纯石头（无矿物）</summary>
        None = 0,
        
        /// <summary>铜矿 - 需要生铁镐或更高</summary>
        C1 = 1,
        
        /// <summary>铁矿 - 需要石镐或更高</summary>
        C2 = 2,
        
        /// <summary>金矿 - 需要钢镐或更高</summary>
        C3 = 3
    }
    
    /// <summary>
    /// 材料等级枚举
    /// 用于工具和武器的材质等级判定
    /// 
    /// 等级对应：
    /// 0=木质, 1=石质, 2=生铁, 3=黄铜, 4=钢质, 5=金质
    /// </summary>
    public enum MaterialTier
    {
        /// <summary>木质 - 最低等级</summary>
        Wood = 0,
        
        /// <summary>石质</summary>
        Stone = 1,
        
        /// <summary>生铁</summary>
        Iron = 2,
        
        /// <summary>黄铜</summary>
        Brass = 3,
        
        /// <summary>钢质</summary>
        Steel = 4,
        
        /// <summary>金质 - 最高等级</summary>
        Gold = 5
    }
}
