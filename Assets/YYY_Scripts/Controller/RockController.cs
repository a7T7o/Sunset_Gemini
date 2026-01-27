using UnityEngine;
using FarmGame.Data;

/// <summary>
/// 矿石/岩石控制器
/// 继承自 ResourceNode，实现挖矿掉落功能
/// </summary>
public class RockController : ResourceNode
{
    #region 配置

    [Header("━━━━ 矿石配置 ━━━━")]
    [Tooltip("矿石类型（用于区分不同矿石）")]
    [SerializeField] private RockType rockType = RockType.Stone;

    [Tooltip("矿石等级（影响掉落品质）")]
    [Range(1, 5)]
    [SerializeField] private int rockLevel = 1;

    [Header("━━━━ 视觉效果 ━━━━")]
    [Tooltip("受击时的Sprite（可选）")]
    [SerializeField] private Sprite[] damageSprites;

    [Tooltip("破碎后的残骸Sprite（可选）")]
    [SerializeField] private Sprite debrisSprite;

    [Tooltip("破碎后是否隐藏")]
    [SerializeField] private bool hideOnDeplete = true;

    [Header("━━━━ 重生设置 ━━━━")]
    [Tooltip("是否可以重生")]
    [SerializeField] private bool canRespawn = true;

    [Tooltip("重生需要的天数")]
    [SerializeField] private int respawnDays = 3;

    #endregion

    #region 私有字段

    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;
    private int depletedDay = -1;

    #endregion

    #region 属性

    public RockType Type => rockType;
    public int Level => rockLevel;

    #endregion

    #region Unity生命周期

    protected override void Awake()
    {
        base.Awake();
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer != null)
            originalSprite = spriteRenderer.sprite;
    }

    private void Start()
    {
        // 订阅时间事件（用于重生）
        if (canRespawn)
        {
            TimeManager.OnDayChanged += OnDayChanged;
        }
    }

    private void OnDestroy()
    {
        if (canRespawn)
        {
            TimeManager.OnDayChanged -= OnDayChanged;
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 对矿石造成伤害
    /// </summary>
    public override bool TakeDamage(int damage = 1)
    {
        if (isDepleted) return true;

        bool result = base.TakeDamage(damage);

        // 更新受损Sprite
        UpdateDamageSprite();

        return result;
    }

    #endregion

    #region 保护方法

    protected override void OnDepleted()
    {
        // 记录耗尽日期
        if (TimeManager.Instance != null)
        {
            depletedDay = TimeManager.Instance.GetTotalDaysPassed();
        }

        // 更新视觉效果
        if (spriteRenderer != null)
        {
            if (hideOnDeplete)
            {
                spriteRenderer.enabled = false;
            }
            else if (debrisSprite != null)
            {
                spriteRenderer.sprite = debrisSprite;
            }
        }

        // 禁用碰撞体
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        Debug.Log($"<color=orange>[RockController] {gameObject.name} 被挖掘完毕！</color>");
    }

    protected override void OnReset()
    {
        depletedDay = -1;

        // 恢复视觉效果
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = originalSprite;
        }

        // 启用碰撞体
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        Debug.Log($"<color=green>[RockController] {gameObject.name} 重生！</color>");
    }

    #endregion

    #region 私有方法

    private void UpdateDamageSprite()
    {
        if (spriteRenderer == null || damageSprites == null || damageSprites.Length == 0)
            return;

        // 根据剩余生命值选择Sprite
        float healthRatio = (float)currentHealth / maxHealth;
        int spriteIndex = Mathf.FloorToInt((1f - healthRatio) * damageSprites.Length);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, damageSprites.Length - 1);

        if (damageSprites[spriteIndex] != null)
        {
            spriteRenderer.sprite = damageSprites[spriteIndex];
        }
    }

    private void OnDayChanged(int year, int seasonDay, int totalDays)
    {
        if (!canRespawn || !isDepleted) return;

        // 检查是否可以重生
        if (depletedDay >= 0 && totalDays - depletedDay >= respawnDays)
        {
            Reset();
        }
    }

    #endregion

    #region 编辑器

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        
        if (respawnDays < 1) respawnDays = 1;
    }

    [UnityEditor.MenuItem("CONTEXT/RockController/🔨 测试挖掘")]
    private static void TestMine(UnityEditor.MenuCommand command)
    {
        RockController rock = command.context as RockController;
        if (rock == null) return;
        
        rock.TakeDamage(1);
        UnityEditor.EditorUtility.SetDirty(rock);
    }

    [UnityEditor.MenuItem("CONTEXT/RockController/💥 直接破碎")]
    private static void TestDeplete(UnityEditor.MenuCommand command)
    {
        RockController rock = command.context as RockController;
        if (rock == null) return;
        
        rock.TakeDamage(rock.maxHealth);
        UnityEditor.EditorUtility.SetDirty(rock);
    }
#endif

    #endregion
}

/// <summary>
/// 矿石类型枚举
/// </summary>
public enum RockType
{
    Stone,      // 普通石头
    Copper,     // 铜矿
    Iron,       // 铁矿
    Gold,       // 金矿
    Crystal,    // 水晶
    Gem         // 宝石
}
