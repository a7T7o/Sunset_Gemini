namespace FarmGame.Data
{
    /// <summary>
    /// 物品大类（对应ID第一位）
    /// </summary>
    public enum ItemCategory
    {
        Tool = 0,           // 0XXX - 工具和武器
        Plant = 1,          // 1XXX - 种植类
        AnimalProduct = 2,  // 2XXX - 动物产品
        Material = 3,       // 3XXX - 矿物和材料
        Consumable = 4,     // 4XXX - 消耗品
        Food = 5,           // 5XXX - 食品
        Furniture = 6,      // 6XXX - 家具
        Special = 7         // 7XXX - 特殊物品
    }

    /// <summary>
    /// 工具子类（对应ID第二位）
    /// </summary>
    public enum ToolSubType
    {
        FarmTool = 0,      // 00XX - 农业工具（锄头、水壶、镰刀、钓鱼竿）
        GatherTool = 1,    // 01XX - 采集工具（镐子、斧头）
        Weapon = 2,        // 02XX - 武器
        Special = 3        // 03XX - 特殊工具
    }

    /// <summary>
    /// 具体工具类型
    /// </summary>
    public enum ToolType
    {
        None,
        // 农业工具
        Hoe,            // 锄头
        WateringCan,    // 水壶
        Sickle,         // 镰刀
        FishingRod,     // 钓鱼竿
        // 采集工具
        Pickaxe,        // 镐子
        Axe,            // 斧头
        // 武器（在WeaponType中详细定义）
        Weapon
    }

    /// <summary>
    /// 武器类型
    /// </summary>
    public enum WeaponType
    {
        Sword,          // 剑
        Bow,            // 弓
        Staff,          // 法杖
        Spear,          // 矛（预留）
        Hammer          // 锤（预留）
    }

    /// <summary>
    /// 季节
    /// </summary>
    public enum Season
    {
        Spring = 0,     // 春
        Summer = 1,     // 夏
        Fall = 2,       // 秋
        Winter = 3,     // 冬
        AllSeason = 4   // 全季节
    }

    /// <summary>
    /// 物品品质（仅影响价格，通过UI星星显示）
    /// Normal 无星星显示，其他品质在物品槽左下角显示星星
    /// 价格计算后向上取整
    /// </summary>
    public enum ItemQuality
    {
        Normal = 0,     // 普通/正常（无星星显示，价格×1.0）
        Rare = 1,       // 稀有（价格×1.25，向上取整）
        Epic = 2,       // 罕见（价格×2.0，向上取整）
        Legendary = 3   // 猎奇（价格×3.25，向上取整）
    }

    /// <summary>
    /// 材料子类（对应ID第二位）
    /// </summary>
    public enum MaterialSubType
    {
        Ore = 0,        // 30XX - 矿石（未加工）
        Ingot = 1,      // 31XX - 锭（加工后）
        Natural = 2,    // 32XX - 自然材料（木、石）
        Monster = 3     // 33XX - 怪物掉落
    }

    /// <summary>
    /// Buff类型（食物和药水的效果）
    /// </summary>
    public enum BuffType
    {
        None,           // 无Buff
        Speed,          // 移动速度提升
        Defense,        // 防御力提升
        Attack,         // 攻击力提升
        Luck,           // 幸运值提升
        Fishing,        // 钓鱼效率提升
        Mining,         // 挖矿效率提升
        Farming         // 种田效率提升
    }

    /// <summary>
    /// 配方设施类型（在哪里制作）
    /// </summary>
    public enum CraftingStation
    {
        None,           // 不需要设施（手工制作）
        CookingPot,     // 烹饪锅（煮食、汤类）
        Furnace,        // 熔炉（烧矿、冶炼金属锭）
        MagicTower,     // 魔法塔
        AnvilForge,     // 铁匠铺铁砧
        Workbench,      // 工作台（武器、金属物品、工具）
        AlchemyTable,   // 制药台（药品制作）
        Grill           // 烧烤架（烤肉、烧烤食物）
    }

    /// <summary>
    /// 装备类型（用于快速装备功能判断目标槽位）
    /// </summary>
    public enum EquipmentType
    {
        None = 0,       // 非装备物品
        Helmet = 1,     // 头盔 - 槽位0
        Armor = 2,      // 盔甲 - 槽位2
        Pants = 3,      // 裤子 - 槽位1
        Shoes = 4,      // 鞋子 - 槽位3
        Ring = 5,       // 戒指 - 槽位4,5（双槽位）
        Accessory = 6   // 饰品（预留）
    }

    /// <summary>
    /// 消耗品类型（用于右键使用判断）
    /// </summary>
    public enum ConsumableType
    {
        None = 0,       // 非消耗品
        Food = 1,       // 食物（食用）
        Potion = 2,     // 药水（使用）
        Buff = 3        // Buff物品（使用）
    }
}

