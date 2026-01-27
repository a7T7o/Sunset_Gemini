using UnityEngine;
using FarmGame.Data;

/// <summary>
/// 石头阶段配置类
/// 定义每个阶段的属性：血量、石料总量、是否最终阶段等
/// </summary>
[System.Serializable]
public class StoneStageConfig
{
    [Header("血量设置")]
    [Tooltip("该阶段的血量")]
    [Range(1, 100)]
    public int health = 36;
    
    [Header("掉落设置")]
    [Tooltip("该阶段的石料总量（用于计算差值掉落）")]
    [Range(0, 20)]
    public int stoneTotalCount = 12;
    
    [Header("阶段设置")]
    [Tooltip("是否为最终阶段（M3和M4为true）")]
    public bool isFinalStage = false;
    
    [Tooltip("下一阶段（非最终阶段有效）")]
    public StoneStage nextStage = StoneStage.M2;
    
    [Tooltip("转换到下一阶段时含量指数是否减1")]
    public bool decreaseOreIndexOnTransition = false;
}

/// <summary>
/// 默认石头阶段配置工厂
/// 提供预设的4阶段配置
/// </summary>
public static class StoneStageConfigFactory
{
    /// <summary>
    /// 创建默认的4阶段配置
    /// 
    /// | 阶段 | 血量 | 石料总量 | 石料掉落 | 最终阶段 | 下一阶段 | 含量减少 |
    /// |------|------|---------|---------|---------|---------|---------|
    /// | M1   | 36   | 12      | 6       | ✗       | M2      | ✗       |
    /// | M2   | 17   | 6       | 4       | ✗       | M3      | ✓       |
    /// | M3   | 9*   | 2       | 2       | ✓       | -       | -       |
    /// | M4   | 4    | 2       | 2       | ✓       | -       | -       |
    /// 
    /// * M3 阶段 oreIndex=0（无矿物）时血量为 4，与 M4 一致
    /// </summary>
    public static StoneStageConfig[] CreateDefaultConfigs()
    {
        return new StoneStageConfig[]
        {
            // M1：最大阶段
            new StoneStageConfig
            {
                health = 36,
                stoneTotalCount = 12,
                isFinalStage = false,
                nextStage = StoneStage.M2,
                decreaseOreIndexOnTransition = false
            },
            // M2：中等阶段
            new StoneStageConfig
            {
                health = 17,
                stoneTotalCount = 6,
                isFinalStage = false,
                nextStage = StoneStage.M3,
                decreaseOreIndexOnTransition = true  // M2→M3 含量减1
            },
            // M3：最小阶段（最终阶段）
            new StoneStageConfig
            {
                health = 9,
                stoneTotalCount = 2,  // 与 M4 一致
                isFinalStage = true,
                nextStage = StoneStage.M3,  // 无效，因为是最终阶段
                decreaseOreIndexOnTransition = false
            },
            // M4：装饰石头（最终阶段）
            new StoneStageConfig
            {
                health = 4,
                stoneTotalCount = 2,
                isFinalStage = true,
                nextStage = StoneStage.M4,  // 无效，因为是最终阶段
                decreaseOreIndexOnTransition = false
            }
        };
    }
}
