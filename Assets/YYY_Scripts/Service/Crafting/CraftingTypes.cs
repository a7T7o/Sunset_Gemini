using UnityEngine;

/// <summary>
/// 制作结果
/// </summary>
public struct CraftResult
{
    /// <summary>是否成功</summary>
    public bool success;
    
    /// <summary>结果消息</summary>
    public string message;
    
    /// <summary>产物 ID</summary>
    public int resultItemId;
    
    /// <summary>产物数量</summary>
    public int resultAmount;
    
    /// <summary>失败原因</summary>
    public FailReason failReason;
}

/// <summary>
/// 制作失败原因
/// </summary>
public enum FailReason
{
    None,
    InvalidRecipe,          // 无效配方
    InsufficientMaterials,  // 材料不足
    InventoryFull,          // 背包已满
    RecipeLocked,           // 配方未解锁
    LevelTooLow             // 等级不足
}

/// <summary>
/// 材料状态
/// </summary>
public struct MaterialStatus
{
    /// <summary>物品 ID</summary>
    public int itemId;
    
    /// <summary>物品名称</summary>
    public string itemName;
    
    /// <summary>物品图标</summary>
    public Sprite icon;
    
    /// <summary>需要数量</summary>
    public int required;
    
    /// <summary>持有数量</summary>
    public int owned;
    
    /// <summary>是否足够</summary>
    public bool sufficient;
}
