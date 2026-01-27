using UnityEngine;

/// <summary>
/// 导航单位接口：为玩家、NPC、怪物提供统一的导航能力
/// 支持障碍物分类和动态避让
/// </summary>
public interface INavigationUnit
{
    /// <summary>
    /// 获取单位类型（用于判断是否应该避让）
    /// </summary>
    NavigationUnitType GetUnitType();
    
    /// <summary>
    /// 获取单位当前位置
    /// </summary>
    Vector2 GetPosition();
    
    /// <summary>
    /// 获取单位碰撞体半径
    /// </summary>
    float GetColliderRadius();
    
    /// <summary>
    /// 是否应该避让另一个单位
    /// </summary>
    bool ShouldAvoid(INavigationUnit other);
    
    /// <summary>
    /// 获取避让半径（个人空间）
    /// </summary>
    float GetAvoidanceRadius();
}

/// <summary>
/// 导航单位类型
/// </summary>
public enum NavigationUnitType
{
    Player,       // 玩家：最高优先级，其他单位应避让
    NPC,          // NPC：友好单位，互相避让
    Enemy,        // 敌对怪物：与玩家/NPC避让
    StaticObstacle // 静态障碍物：不可穿越
}

/// <summary>
/// 导航单位基类（可选继承）
/// 提供默认的避让逻辑
/// </summary>
public abstract class NavigationUnitBase : MonoBehaviour, INavigationUnit
{
    [Header("导航单位配置")]
    [SerializeField] protected NavigationUnitType unitType = NavigationUnitType.NPC;
    [SerializeField, Range(0.3f, 2f)] protected float avoidanceRadius = 0.6f;
    
    protected Collider2D unitCollider;
    
    protected virtual void Awake()
    {
        unitCollider = GetComponent<Collider2D>();
        if (unitCollider == null)
            unitCollider = GetComponentInChildren<Collider2D>();
    }
    
    public virtual NavigationUnitType GetUnitType() => unitType;
    
    public virtual Vector2 GetPosition()
    {
        if (unitCollider != null)
            return unitCollider.bounds.center;
        return transform.position;
    }
    
    public virtual float GetColliderRadius()
    {
        if (unitCollider != null)
        {
            return Mathf.Max(unitCollider.bounds.extents.x, unitCollider.bounds.extents.y);
        }
        return 0.25f; // 默认半径
    }
    
    public virtual bool ShouldAvoid(INavigationUnit other)
    {
        if (other == null) return false;
        
        NavigationUnitType otherType = other.GetUnitType();
        
        switch (unitType)
        {
            case NavigationUnitType.Player:
                // 玩家避让NPC和敌人
                return otherType == NavigationUnitType.NPC || otherType == NavigationUnitType.Enemy;
            
            case NavigationUnitType.NPC:
                // NPC避让所有类型
                return true;
            
            case NavigationUnitType.Enemy:
                // 敌人避让玩家和其他敌人
                return otherType == NavigationUnitType.Player || otherType == NavigationUnitType.Enemy;
            
            case NavigationUnitType.StaticObstacle:
                // 静态障碍物不避让任何单位
                return false;
            
            default:
                return false;
        }
    }
    
    public virtual float GetAvoidanceRadius() => avoidanceRadius;
}
