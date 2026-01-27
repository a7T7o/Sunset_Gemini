using UnityEngine;

/// <summary>
/// 自动拾取：在玩家周围半径内自动吸取 WorldItemPickup
/// </summary>
public class AutoPickupService : MonoBehaviour
{
    [SerializeField, HideInInspector] private InventoryService inventory;
    [SerializeField] private float pickupRadius = 1.2f;
    [Header("筛选（优先Tag）")]
    [SerializeField] private string[] pickupTags = new[] { "Pickup" };
    [SerializeField] private LayerMask pickupMask; // 可选：附加图层过滤
    [SerializeField] private int maxPerFrame = 6;
    [SerializeField] private bool showGizmos = true;
    
    [Header("飞向动画")]
    [Tooltip("是否启用飞向玩家动画")]
    [SerializeField] private bool enableFlyAnimation = true;
    
    [Header("性能优化")]
    [Tooltip("检测缓冲区大小")]
    [SerializeField] private int hitBufferSize = 32;

    private Collider2D playerCollider;
    private Collider2D[] _hitBuffer;  // 复用的碰撞检测缓冲区

    void Awake()
    {
        if (inventory == null) inventory = FindFirstObjectByType<InventoryService>();
        // 获取Player的Collider2D作为中心点参考
        playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null) playerCollider = GetComponentInChildren<Collider2D>();
        
        // 初始化碰撞检测缓冲区
        _hitBuffer = new Collider2D[hitBufferSize];
    }

    void Update()
    {
        int count = 0;
        // 使用Player Collider的中心作为拾取半径的中心点
        Vector2 center = playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)transform.position;
        
        // ★ 使用新的 OverlapCircle API（返回数量）
        int hitCount = Physics2D.OverlapCircle(center, pickupRadius, new ContactFilter2D().NoFilter(), _hitBuffer);
        
        // 记录当前帧检测到的物品，用于离开范围检测
        var currentFramePickups = new System.Collections.Generic.HashSet<WorldItemPickup>();
        
        for (int i = 0; i < hitCount; i++)
        {
            var h = _hitBuffer[i];
            if (h == null) continue;
            // 先按标签筛选（若配置了）
            if (pickupTags != null && pickupTags.Length > 0)
            {
                if (!AutoPickupUtil.HasAnyTag(h.transform, pickupTags)) continue;
            }
            // 可选：再按图层附加过滤
            if (pickupMask.value != 0 && ((1 << h.gameObject.layer) & pickupMask.value) == 0) continue;
            var pickup = h.GetComponentInParent<WorldItemPickup>();
            if (pickup == null) pickup = h.GetComponent<WorldItemPickup>();
            if (pickup == null) continue;
            
            currentFramePickups.Add(pickup);
            
            // 跳过正在飞行的物品
            if (pickup.IsFlying) continue;
            
            // ★ 检查拾取冷却
            if (!pickup.CanBePickedUp()) continue;
            
            // ★ 关键：在触发吸引动画前检查背包是否有空间
            if (!CanPickupItem(pickup)) continue;
            
            if (enableFlyAnimation)
            {
                // 触发飞向玩家动画
                pickup.FlyToPlayer(transform, inventory);
            }
            else
            {
                // 直接拾取
                pickup.TryPickup(inventory);
            }
            
            count++;
            if (count >= maxPerFrame) break;
        }
        
        // 检测离开范围的物品
        CheckExitedPickups(currentFramePickups);
    }
    
    // 上一帧检测到的物品
    private System.Collections.Generic.HashSet<WorldItemPickup> _lastFramePickups = new System.Collections.Generic.HashSet<WorldItemPickup>();
    
    /// <summary>
    /// 检测离开拾取范围的物品
    /// </summary>
    private void CheckExitedPickups(System.Collections.Generic.HashSet<WorldItemPickup> currentPickups)
    {
        // 找出上一帧有但这一帧没有的物品（离开了范围）
        foreach (var pickup in _lastFramePickups)
        {
            if (pickup != null && !currentPickups.Contains(pickup))
            {
                pickup.OnPlayerExitRange();
            }
        }
        
        // 更新记录
        _lastFramePickups = currentPickups;
    }

    /// <summary>
    /// 检查背包是否有空间容纳指定物品
    /// </summary>
    private bool CanPickupItem(WorldItemPickup pickup)
    {
        if (inventory == null) return false;
        return inventory.CanAddItem(pickup.itemId, pickup.quality, pickup.amount);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        // Gizmo也使用Collider中心
        Vector2 center = playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)transform.position;
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(center, pickupRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, pickupRadius);
    }
#endif
}

static class AutoPickupUtil
{
    public static bool HasAnyTag(Transform t, string[] tags)
    {
        if (t == null || tags == null) return false;
        foreach (var tag in tags)
        {
            if (!string.IsNullOrEmpty(tag) && t.CompareTag(tag)) return true;
        }
        var p = t.parent;
        while (p != null)
        {
            foreach (var tag in tags)
            {
                if (!string.IsNullOrEmpty(tag) && p.CompareTag(tag)) return true;
            }
            p = p.parent;
        }
        return false;
    }
}
