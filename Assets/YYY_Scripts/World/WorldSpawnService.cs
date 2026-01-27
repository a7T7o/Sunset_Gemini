using UnityEngine;
using System.Collections.Generic;
using FarmGame.Data;
using FarmGame.World;

/// <summary>
/// 世界物品生成服务
/// 提供统一的物品生成接口，支持动画和对象池
/// </summary>
public class WorldSpawnService : MonoBehaviour
{
    public static WorldSpawnService Instance { get; private set; }

    [Header("引用")]
    [SerializeField] private ItemDatabase database;
    
    [Tooltip("默认的掉落预制体（可选，为空时使用对象池默认模板）")]
    [SerializeField] private GameObject defaultWorldPrefab;

    [Header("生成选项")]
    [SerializeField] private bool snapToGrid = false;
    [SerializeField] private float gridSize = 0.5f;
    [SerializeField] private float defaultSpreadRadius = 0.5f;

    public ItemDatabase Database => database;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 尝试获取数据库
        if (database == null)
        {
            database = Resources.Load<ItemDatabase>("Data/Database/MasterItemDatabase");
        }
    }

    #region 基础生成方法

    /// <summary>
    /// 生成物品（无动画）
    /// </summary>
    public WorldItemPickup Spawn(ItemStack stack, Vector3 worldPos)
    {
        return SpawnById(stack.itemId, stack.quality, stack.amount, worldPos, false);
    }

    /// <summary>
    /// 通过ID生成物品
    /// </summary>
    /// <param name="setSpawnCooldown">是否设置生成冷却（丢弃物品时应为 false）</param>
    public WorldItemPickup SpawnById(int itemId, int quality, int amount, Vector3 worldPos, bool playAnimation = false, bool setSpawnCooldown = true)
    {
        if (snapToGrid) worldPos = Snap(worldPos, gridSize);

        // 优先使用对象池
        if (WorldItemPool.Instance != null)
        {
            return WorldItemPool.Instance.SpawnById(itemId, quality, amount, worldPos, playAnimation, setSpawnCooldown);
        }

        // 回退：直接创建
        return SpawnDirectById(itemId, quality, amount, worldPos);
    }

    /// <summary>
    /// 通过ItemData生成物品
    /// </summary>
    public WorldItemPickup SpawnFromItem(ItemData data, int quality, int amount, Vector3 worldPos, bool playAnimation = false)
    {
        if (data == null) return null;
        if (snapToGrid) worldPos = Snap(worldPos, gridSize);

        // 优先使用对象池
        if (WorldItemPool.Instance != null)
        {
            return WorldItemPool.Instance.Spawn(data, quality, amount, worldPos, playAnimation);
        }

        // 回退：直接创建
        return SpawnDirect(data, quality, amount, worldPos);
    }

    #endregion

    #region 带动画的生成方法

    /// <summary>
    /// 生成物品并播放弹出动画
    /// </summary>
    public WorldItemPickup SpawnWithAnimation(ItemData data, int quality, int amount, 
                                               Vector3 origin, Vector3 direction)
    {
        if (data == null) return null;

        var item = SpawnFromItem(data, quality, amount, origin, true);
        
        if (item != null)
        {
            var dropAnim = item.GetComponent<WorldItemDrop>();
            if (dropAnim != null)
            {
                dropAnim.StartDrop(direction, Random.Range(0.8f, 1.2f));
            }
        }

        return item;
    }

    /// <summary>
    /// 批量生成多个物品（带随机偏移和动画）
    /// </summary>
    public List<WorldItemPickup> SpawnMultiple(ItemData data, int quality, int totalAmount, 
                                                Vector3 origin, float spreadRadius = -1f)
    {
        if (spreadRadius < 0f) spreadRadius = defaultSpreadRadius;

        // 优先使用对象池
        if (WorldItemPool.Instance != null)
        {
            return WorldItemPool.Instance.SpawnMultiple(data, quality, totalAmount, origin, spreadRadius);
        }

        // 回退：手动生成（使用均匀分布）
        var result = new List<WorldItemPickup>();
        int itemCount = totalAmount;
        
        // 计算均匀分布的位置
        List<Vector3> positions = CalculateScatteredPositions(origin, itemCount, spreadRadius);

        for (int i = 0; i < itemCount; i++)
        {
            Vector3 spawnPos = positions[i];
            var item = SpawnFromItem(data, quality, 1, spawnPos, true);
            if (item != null)
            {
                result.Add(item);
            }
        }

        return result;
    }
    
    /// <summary>
    /// 计算分散位置（均匀分布 + 轻微随机偏移）
    /// </summary>
    private List<Vector3> CalculateScatteredPositions(Vector3 origin, int count, float radius)
    {
        var positions = new List<Vector3>();
        
        if (count <= 0) return positions;
        
        if (count == 1)
        {
            // 单个物品：中心位置 + 轻微随机偏移
            float offsetX = Random.Range(-radius * 0.3f, radius * 0.3f);
            float offsetY = Random.Range(-radius * 0.3f, radius * 0.3f);
            positions.Add(origin + new Vector3(offsetX, offsetY, 0f));
            return positions;
        }
        
        // 多个物品：使用黄金角度螺旋分布 + 随机偏移
        float goldenAngle = 137.5f * Mathf.Deg2Rad;
        
        for (int i = 0; i < count; i++)
        {
            // 螺旋分布
            float t = (float)i / (count - 1);
            float r = radius * Mathf.Sqrt(t) * 0.8f; // 0.8 让物品更集中
            float angle = i * goldenAngle;
            
            // 基础位置
            float x = r * Mathf.Cos(angle);
            float y = r * Mathf.Sin(angle);
            
            // 添加轻微随机偏移（让分布更自然）
            float jitter = radius * 0.15f;
            x += Random.Range(-jitter, jitter);
            y += Random.Range(-jitter, jitter);
            
            positions.Add(origin + new Vector3(x, y, 0f));
        }
        
        return positions;
    }

    /// <summary>
    /// 批量生成多个物品（通过ID）
    /// </summary>
    public List<WorldItemPickup> SpawnMultipleById(int itemId, int quality, int totalAmount, 
                                                    Vector3 origin, float spreadRadius = -1f)
    {
        if (database == null) return new List<WorldItemPickup>();
        
        var data = database.GetItemByID(itemId);
        return SpawnMultiple(data, quality, totalAmount, origin, spreadRadius);
    }

    #endregion

    #region 箱子特殊处理

    /// <summary>
    /// 生成物品（自动检测是否为箱子类型）
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="quality">品质</param>
    /// <param name="amount">数量</param>
    /// <param name="worldPos">世界位置</param>
    /// <param name="direction">掉落方向</param>
    /// <returns>生成的物品（箱子返回null因为会自动放置）</returns>
    public WorldItemPickup SpawnItem(int itemId, int quality, int amount, Vector3 worldPos, Vector2 direction)
    {
        if (database == null) return null;
        
        var data = database.GetItemByID(itemId);
        if (data == null) return null;

        // 检查是否为箱子类型（Storage）
        if (data is StorageData storageData)
        {
            // 箱子特殊处理：掉落动画后自动放置
            StartCoroutine(HandleStorageDropCoroutine(storageData, worldPos, direction));
            return null;
        }

        // 普通物品：正常生成
        return SpawnWithAnimation(data, quality, amount, worldPos, direction);
    }

    /// <summary>
    /// 处理箱子掉落协程
    /// </summary>
    private System.Collections.IEnumerator HandleStorageDropCoroutine(StorageData storageData, Vector3 dropPosition, Vector2 direction)
    {
        // 创建临时掉落物（仅用于动画，不可拾取）
        var tempPickup = SpawnFromItem(storageData, 0, 1, dropPosition, true);
        if (tempPickup != null)
        {
            // 播放掉落动画
            var dropAnim = tempPickup.GetComponent<WorldItemDrop>();
            if (dropAnim != null)
            {
                dropAnim.StartDrop(direction, Random.Range(0.8f, 1.2f));
            }

            // 禁用拾取功能
            var collider = tempPickup.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            // 等待掉落动画完成
            yield return new WaitForSeconds(ChestDropHandler.DropAnimationDuration);

            // 获取最终位置
            Vector3 finalPos = tempPickup.transform.position;

            // 销毁临时掉落物
            if (WorldItemPool.Instance != null)
            {
                WorldItemPool.Instance.Despawn(tempPickup);
            }
            else
            {
                Destroy(tempPickup.gameObject);
            }

            // 在最终位置放置箱子
            ChestDropHandler.HandleChestDrop(storageData, finalPos, ChestOwnership.Player);
        }
    }

    #endregion

    #region 私有方法

    private WorldItemPickup SpawnDirect(ItemData data, int quality, int amount, Vector3 worldPos)
    {
        // 使用物品的worldPrefab或默认预制体
        GameObject prefab = data.worldPrefab != null ? data.worldPrefab : defaultWorldPrefab;
        
        if (prefab == null)
        {
            // 创建简单的运行时物体
            prefab = CreateSimplePickupPrefab();
        }

        var go = Instantiate(prefab, worldPos, Quaternion.identity);
        go.SetActive(true);

        var pickup = go.GetComponent<WorldItemPickup>();
        if (pickup == null)
        {
            pickup = go.AddComponent<WorldItemPickup>();
        }
        pickup.Init(data, quality, amount);

        return pickup;
    }

    private WorldItemPickup SpawnDirectById(int itemId, int quality, int amount, Vector3 worldPos)
    {
        if (database == null) return null;
        var data = database.GetItemByID(itemId);
        if (data == null) return null;
        return SpawnDirect(data, quality, amount, worldPos);
    }

    private GameObject CreateSimplePickupPrefab()
    {
        var root = new GameObject("WorldItem_Runtime");
        
        // ★ 设置 Tag 为 Pickup（用于 AutoPickupService 检测）
        root.tag = "Pickup";
        
        // ★ 添加 CircleCollider2D (Trigger) 用于拾取检测
        var collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;
        
        // Sprite子物体
        var spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(root.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        var sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Layer 1";

        // Shadow子物体
        var shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(root.transform);
        shadowObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        shadowObj.transform.localScale = new Vector3(0.5f, 0.3f, 1f);
        var shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sortingLayerName = "Layer 1";
        shadowSr.sortingOrder = -1;
        shadowSr.color = new Color(0f, 0f, 0f, 0.3f);

        // 组件
        root.AddComponent<WorldItemPickup>();
        root.AddComponent<WorldItemDrop>();

        return root;
    }

    private static Vector3 Snap(Vector3 p, float step)
    {
        float sx = Mathf.Round(p.x / step) * step;
        float sy = Mathf.Round(p.y / step) * step;
        return new Vector3(sx, sy, 0f);
    }

    #endregion
}
