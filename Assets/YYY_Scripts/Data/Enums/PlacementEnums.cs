namespace FarmGame.Data
{
    /// <summary>
    /// 箱子来源枚举
    /// 决定箱子的交互规则（玩家制作 vs 野外生成）
    /// </summary>
    public enum ChestOrigin
    {
        PlayerCrafted = 0,  // 玩家制作 - 可上锁、可挖取、可移动
        WorldSpawned = 1    // 野外生成 - 上锁后不能挖取/移动
    }

    /// <summary>
    /// 放置类型枚举
    /// 定义不同物品的放置规则
    /// </summary>
    public enum PlacementType
    {
        None = 0,
        Sapling = 1,
        Decoration = 2,
        Building = 3,
        Furniture = 4,
        Workstation = 5,
        Storage = 6,
        InteractiveDisplay = 7,
        SimpleEvent = 8
    }

    /// <summary>
    /// 放置验证失败原因
    /// </summary>
    public enum PlacementInvalidReason
    {
        None = 0,
        OutOfRange = 1,
        ObstacleBlocking = 2,
        OnFarmland = 3,
        OnWater = 4,
        InsufficientSpace = 5,
        WrongSeason = 6,
        InvalidTerrain = 7,
        TreeTooClose = 8,
        BuildingOverlap = 9,
        IndoorOnly = 10,
        OutdoorOnly = 11,
        LayerMismatch = 12,      // Layer 不一致
        CollisionDetected = 13   // 碰撞检测到障碍物
    }

    /// <summary>
    /// 工作台类型枚举
    /// </summary>
    public enum WorkstationType
    {
        Heating = 0,        // 取暖设施（壁炉、火盆等）
        Smelting = 1,       // 冶炼设施（熔炉、高炉等）
        Pharmacy = 2,       // 制药设施（炼药台、蒸馏器等）
        Sawmill = 3,        // 锯木设施（劈柴架、锯木台等）
        Cooking = 4,        // 烹饪设施（厨房、烤炉等）
        Crafting = 5        // 装备和工具制作设施（工作台、铁砧等）
    }

    /// <summary>
    /// 简单事件类型枚举
    /// </summary>
    public enum SimpleEventType
    {
        PlaySound = 0,      // 播放音效
        PlayAnimation = 1,  // 播放动画
        Teleport = 2,       // 传送
        Unlock = 3,         // 解锁
        GiveItem = 4,       // 给予物品
        TriggerQuest = 5,   // 触发任务
        ShowMessage = 6     // 显示消息
    }

    /// <summary>
    /// 箱子材质类型枚举
    /// 用于锁/钥匙与箱子的材质匹配
    /// </summary>
    public enum ChestMaterial
    {
        Wood = 0,       // 木质
        Iron = 1,       // 铁质
        Special = 2     // 特殊（预留）
    }

    /// <summary>
    /// 箱子归属枚举
    /// 决定箱子是否需要钥匙才能打开
    /// </summary>
    public enum ChestOwnership
    {
        Player = 0,     // 玩家所有（可直接打开）
        World = 1,      // 世界所有（需要钥匙）
        Locked = 2      // 已上锁（需要钥匙解锁）
    }
    
    /// <summary>
    /// 上锁结果枚举
    /// </summary>
    public enum LockResult
    {
        Success = 0,            // 上锁成功
        AlreadyLocked = 1,      // 已经上锁
        MaterialMismatch = 2    // 材质不匹配
    }
    
    /// <summary>
    /// 解锁结果枚举
    /// </summary>
    public enum UnlockResult
    {
        Success = 0,            // 解锁成功
        NotLocked = 1,          // 未上锁
        AlreadyOwned = 2,       // 已是玩家所有
        MaterialMismatch = 3    // 材质不匹配
    }
    
    /// <summary>
    /// 打开箱子结果枚举
    /// </summary>
    public enum OpenResult
    {
        Success = 0,            // 打开成功
        Locked = 1,             // 已上锁
        NotOwned = 2            // 非玩家所有
    }
}
