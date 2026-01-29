/// <summary>
/// 成长阶段（旧版3阶段，保留用于兼容）
/// </summary>
public enum GrowthStage
{
    Sapling,    // 树苗
    Small,      // 小树
    Large       // 大树
}

/// <summary>
/// 树的状态
/// </summary>
public enum TreeState
{
    Normal,         // 正常
    Withered,       // 枯萎
    Frozen,         // 冰封（仅冬季树苗）
    Melted,         // 冰融化（冬季晴天）
    Stump           // 树桩
}
