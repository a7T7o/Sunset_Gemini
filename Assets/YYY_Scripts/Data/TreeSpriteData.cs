using UnityEngine;

/// <summary>
/// 影子配置 - 包含 Sprite 和缩放
/// 用于配置每个阶段的影子显示
/// </summary>
[System.Serializable]
public class ShadowConfig
{
    [Tooltip("影子 Sprite（可选，未配置则使用原始 Sprite）")]
    public Sprite sprite;
    
    [Tooltip("影子缩放")]
    [Range(0f, 2f)]
    public float scale = 1f;
    
    /// <summary>
    /// 创建默认的影子配置数组（5个元素，对应阶段1-5）
    /// 阶段0没有影子
    /// </summary>
    public static ShadowConfig[] CreateDefaultConfigs()
    {
        return new ShadowConfig[]
        {
            new ShadowConfig { sprite = null, scale = 0f },    // 阶段1（无影子）
            new ShadowConfig { sprite = null, scale = 0.6f },  // 阶段2
            new ShadowConfig { sprite = null, scale = 0.8f },  // 阶段3
            new ShadowConfig { sprite = null, scale = 0.9f },  // 阶段4
            new ShadowConfig { sprite = null, scale = 1.0f }   // 阶段5
        };
    }
}

/// <summary>
/// 季节 Sprite 集合
/// 包含5种植被季节的 Sprite
/// 
/// ★ 字段名称与视觉样式映射：
/// - spring = 春季样式（绿色茂盛）
/// - summer = 夏季样式（深绿色）
/// - earlyFall = 早秋样式（开始变黄）
/// - lateFall = 晚秋样式（黄色/橙色）
/// - winter = 冬季样式（挂冰/光秃）
/// 
/// ★ 显示时间线：
/// - 春1-14：100% spring
/// - 春15-28：spring → summer 渐变
/// - 夏1-14：100% summer
/// - 夏15-28：summer → earlyFall 渐变
/// - 秋1-14：earlyFall → lateFall 渐变
/// - 秋15-28：100% lateFall
/// - 冬1-28：100% winter
/// </summary>
[System.Serializable]
public class SeasonSpriteSet
{
    [Tooltip("春季样式 Sprite")]
    public Sprite spring;
    
    [Tooltip("夏季样式 Sprite")]
    public Sprite summer;
    
    [Tooltip("早秋样式 Sprite")]
    public Sprite earlyFall;
    
    [Tooltip("晚秋样式 Sprite")]
    public Sprite lateFall;
    
    [Tooltip("冬季样式 Sprite（挂冰状态）")]
    public Sprite winter;
    
    /// <summary>
    /// 根据植被季节获取对应的 Sprite
    /// </summary>
    public Sprite GetSprite(SeasonManager.VegetationSeason season)
    {
        return season switch
        {
            SeasonManager.VegetationSeason.Spring => spring,
            SeasonManager.VegetationSeason.Summer => summer,
            SeasonManager.VegetationSeason.EarlyFall => earlyFall,
            SeasonManager.VegetationSeason.LateFall => lateFall,
            SeasonManager.VegetationSeason.Winter => winter,
            _ => spring
        };
    }
}

/// <summary>
/// 单个阶段的 Sprite 数据
/// 包含正常状态、枯萎状态、冬季状态的 Sprite
/// </summary>
[System.Serializable]
public class StageSpriteData
{
    [Header("━━━━ 正常状态 ━━━━")]
    [Tooltip("正常状态的季节 Sprite 集合")]
    public SeasonSpriteSet normal;
    
    [Header("━━━━ 枯萎状态（可选）━━━━")]
    [Tooltip("是否有枯萎状态（阶段0通常没有）")]
    public bool hasWitheredState = false;
    
    [Tooltip("枯萎状态的 Sprite（夏季枯萎）")]
    public Sprite witheredSummer;
    
    [Tooltip("枯萎状态的 Sprite（秋季枯萎）")]
    public Sprite witheredFall;
    
    [Header("━━━━ 树桩状态（可选）━━━━")]
    [Tooltip("是否有树桩（阶段3-5有）")]
    public bool hasStump = false;
    
    [Tooltip("春夏树桩 Sprite")]
    public Sprite stumpSpringSummer;
    
    [Tooltip("秋季树桩 Sprite")]
    public Sprite stumpFall;
    
    [Tooltip("冬季树桩 Sprite")]
    public Sprite stumpWinter;
    
    /// <summary>
    /// 获取树桩 Sprite
    /// </summary>
    public Sprite GetStumpSprite(SeasonManager.VegetationSeason season)
    {
        if (!hasStump) return null;
        
        return season switch
        {
            SeasonManager.VegetationSeason.Spring => stumpSpringSummer,
            SeasonManager.VegetationSeason.Summer => stumpSpringSummer,
            SeasonManager.VegetationSeason.EarlyFall => stumpFall,
            SeasonManager.VegetationSeason.LateFall => stumpFall,
            SeasonManager.VegetationSeason.Winter => stumpWinter,
            _ => stumpSpringSummer
        };
    }
    
    /// <summary>
    /// 获取枯萎 Sprite
    /// </summary>
    public Sprite GetWitheredSprite(SeasonManager.VegetationSeason season)
    {
        if (!hasWitheredState) return null;
        
        return season switch
        {
            SeasonManager.VegetationSeason.Spring => witheredSummer, // 春季不应有枯萎，降级
            SeasonManager.VegetationSeason.Summer => witheredSummer,
            SeasonManager.VegetationSeason.EarlyFall => witheredFall,
            SeasonManager.VegetationSeason.LateFall => witheredFall,
            SeasonManager.VegetationSeason.Winter => witheredFall,
            _ => witheredSummer
        };
    }
}

/// <summary>
/// 完整的树木 Sprite 数据
/// 包含6个阶段的所有 Sprite 配置
/// </summary>
[System.Serializable]
public class TreeSpriteConfig
{
    [Header("6阶段 Sprite 数据")]
    [Tooltip("阶段0：树苗")]
    public StageSpriteData stage0;
    
    [Tooltip("阶段1：小树苗")]
    public StageSpriteData stage1;
    
    [Tooltip("阶段2：中等树")]
    public StageSpriteData stage2;
    
    [Tooltip("阶段3：大树")]
    public StageSpriteData stage3;
    
    [Tooltip("阶段4：成熟树")]
    public StageSpriteData stage4;
    
    [Tooltip("阶段5：完全成熟")]
    public StageSpriteData stage5;
    
    /// <summary>
    /// 根据阶段索引获取 Sprite 数据
    /// </summary>
    public StageSpriteData GetStageData(int stageIndex)
    {
        return stageIndex switch
        {
            0 => stage0,
            1 => stage1,
            2 => stage2,
            3 => stage3,
            4 => stage4,
            5 => stage5,
            _ => stage0
        };
    }
}
