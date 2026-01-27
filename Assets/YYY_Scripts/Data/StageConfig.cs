using UnityEngine;
using FarmGame.Data;

/// <summary>
/// 树木阶段配置类
/// 定义每个成长阶段的属性：成长天数、血量、掉落表、碰撞体、遮挡等
/// </summary>
[System.Serializable]
public class StageConfig
{
    [Header("成长设置")]
    [Tooltip("成长到下一阶段需要的天数（最后阶段设为0表示不再成长）")]
    [Range(0, 30)]
    public int daysToNextStage = 1;
    
    [Header("血量设置")]
    [Tooltip("该阶段的血量（0表示不可砍伐，如树苗）")]
    [Range(0, 100)]
    public int health = 0;
    
    [Header("树桩设置")]
    [Tooltip("该阶段被砍倒后是否留下树桩")]
    public bool hasStump = false;
    
    [Tooltip("树桩血量（仅当 hasStump=true 时有效）")]
    [Range(0, 50)]
    public int stumpHealth = 0;
    
    [Header("碰撞与遮挡")]
    [Tooltip("是否启用碰撞体")]
    public bool enableCollider = false;
    
    [Tooltip("是否启用遮挡透明")]
    public bool enableOcclusion = false;
    
    [Header("掉落设置")]
    [Tooltip("该阶段的掉落表（砍倒树干时使用）")]
    public DropTable dropTable;
    
    [Tooltip("树桩掉落表（砍倒树桩时使用，仅当 hasStump=true 时有效）")]
    public DropTable stumpDropTable;
    
    [Header("工具类型")]
    [Tooltip("该阶段接受的工具类型")]
    public ToolType acceptedToolType = ToolType.Axe;
    
    [Header("成长边距")]
    [Tooltip("上下方向的成长边距（单位：Unity 单位）\n成长时检测该方向上是否有障碍物")]
    [Range(0f, 5f)]
    public float verticalMargin = 0.5f;
    
    [Tooltip("左右方向的成长边距（单位：Unity 单位）\n成长时检测该方向上是否有障碍物")]
    [Range(0f, 5f)]
    public float horizontalMargin = 0.3f;
}

/// <summary>
/// 默认阶段配置工厂
/// 提供预设的6阶段配置
/// </summary>
public static class StageConfigFactory
{
    /// <summary>
    /// 创建默认的6阶段配置
    /// 阶段 | 天数 | 血量 | 碰撞 | 遮挡 | 树桩 | 树桩血量 | 工具
    /// -----|------|------|------|------|------|----------|------
    ///   0  |  1   |  0   |  ✗   |  ✗   |  ✗   |    -     | 锄头
    ///   1  |  2   |  4   |  ✓   |  ✓   |  ✗   |    -     | 斧头
    ///   2  |  2   |  9   |  ✓   |  ✓   |  ✗   |    4     | 斧头
    ///   3  |  4   | 17   |  ✓   |  ✓   |  ✓   |    9     | 斧头
    ///   4  |  5   | 28   |  ✓   |  ✓   |  ✓   |   12     | 斧头
    ///   5  |  0   | 40   |  ✓   |  ✓   |  ✓   |   16     | 斧头
    /// 
    /// 注意：碰撞与遮挡从阶段1开始启用，只有阶段0（树苗）没有
    /// </summary>
    public static StageConfig[] CreateDefaultConfigs()
    {
        return new StageConfig[]
        {
            // 阶段0：树苗（只能用锄头挖出，无碰撞无遮挡）
            new StageConfig
            {
                daysToNextStage = 1,
                health = 0,
                hasStump = false,
                stumpHealth = 0,
                enableCollider = false,
                enableOcclusion = false,
                acceptedToolType = ToolType.Hoe,
                verticalMargin = 0.2f,
                horizontalMargin = 0.15f
            },
            // 阶段1：小树苗（有碰撞有遮挡）
            new StageConfig
            {
                daysToNextStage = 2,
                health = 4,
                hasStump = false,
                stumpHealth = 0,
                enableCollider = true,
                enableOcclusion = true,
                acceptedToolType = ToolType.Axe,
                verticalMargin = 0.3f,
                horizontalMargin = 0.2f
            },
            // 阶段2：中等树
            new StageConfig
            {
                daysToNextStage = 2,
                health = 9,
                hasStump = false,
                stumpHealth = 4,
                enableCollider = true,
                enableOcclusion = true,
                acceptedToolType = ToolType.Axe,
                verticalMargin = 0.5f,
                horizontalMargin = 0.35f
            },
            // 阶段3：大树
            new StageConfig
            {
                daysToNextStage = 4,
                health = 17,
                hasStump = true,
                stumpHealth = 9,
                enableCollider = true,
                enableOcclusion = true,
                acceptedToolType = ToolType.Axe,
                verticalMargin = 0.7f,
                horizontalMargin = 0.5f
            },
            // 阶段4：成熟树
            new StageConfig
            {
                daysToNextStage = 5,
                health = 28,
                hasStump = true,
                stumpHealth = 12,
                enableCollider = true,
                enableOcclusion = true,
                acceptedToolType = ToolType.Axe,
                verticalMargin = 0.9f,
                horizontalMargin = 0.65f
            },
            // 阶段5：完全成熟（不再成长）
            new StageConfig
            {
                daysToNextStage = 0,
                health = 40,
                hasStump = true,
                stumpHealth = 16,
                enableCollider = true,
                enableOcclusion = true,
                acceptedToolType = ToolType.Axe,
                verticalMargin = 1.1f,
                horizontalMargin = 0.8f
            }
        };
    }
}
