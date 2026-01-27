using UnityEngine;
using FarmGame.Data;

/// <summary>
/// 可交互物体接口
/// 
/// 设计原则：
/// - InputManager 只负责检测输入和分发事件
/// - 具体的交互逻辑由各个物体自己实现
/// - 支持优先级排序（拾取 > 交互）
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 交互优先级（数值越大优先级越高）
    /// 建议值：
    /// - 拾取物：100
    /// - 箱子：50
    /// - NPC：30
    /// - 其他：10
    /// </summary>
    int InteractionPriority { get; }
    
    /// <summary>
    /// 交互距离（玩家需要在此距离内才能交互）
    /// </summary>
    float InteractionDistance { get; }
    
    /// <summary>
    /// 是否可以交互（当前状态下）
    /// </summary>
    bool CanInteract(InteractionContext context);
    
    /// <summary>
    /// 执行交互
    /// </summary>
    /// <param name="context">交互上下文，包含玩家信息、手持物品等</param>
    void OnInteract(InteractionContext context);
    
    /// <summary>
    /// 获取交互提示文本（用于UI显示）
    /// </summary>
    string GetInteractionHint(InteractionContext context);
}

/// <summary>
/// 交互上下文
/// 包含交互时需要的所有信息
/// </summary>
public class InteractionContext
{
    /// <summary>
    /// 玩家位置（Collider 中心）
    /// </summary>
    public Vector2 PlayerPosition { get; set; }
    
    /// <summary>
    /// 玩家 Transform
    /// </summary>
    public Transform PlayerTransform { get; set; }
    
    /// <summary>
    /// 当前手持物品ID（-1 表示空手）
    /// </summary>
    public int HeldItemId { get; set; } = -1;
    
    /// <summary>
    /// 当前手持物品品质
    /// </summary>
    public int HeldItemQuality { get; set; } = 0;
    
    /// <summary>
    /// 当前手持物品槽位索引
    /// </summary>
    public int HeldSlotIndex { get; set; } = -1;
    
    /// <summary>
    /// 背包服务引用（用于消耗物品等操作）
    /// </summary>
    public InventoryService Inventory { get; set; }
    
    /// <summary>
    /// 物品数据库引用
    /// </summary>
    public ItemDatabase Database { get; set; }
    
    /// <summary>
    /// 自动导航器引用（用于导航到目标后交互）
    /// </summary>
    public PlayerAutoNavigator Navigator { get; set; }
}
