using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Data;

/// <summary>
/// 世界物品对象池
/// 管理掉落物品的创建、回收和数量限制
/// </summary>
public class WorldItemPool : MonoBehaviour
{
    #region 单例

    public static WorldItemPool Instance { get; private set; }

    #endregion

    #region 配置

    [Header("对象池配置")]
    [Tooltip("默认的世界物品预制体模板")]
    [SerializeField] private GameObject defaultWorldPrefab;
    
    [Tooltip("初始池大小")]
    [SerializeField] private int initialPoolSize = 20;
    
    [Tooltip("最大池大小")]
    [SerializeField] private int maxPoolSize = 50;
    
    [Tooltip("每种物品类型的最大池大小")]
    [SerializeField] private int maxPoolSizePerItem = 10;
    
    [Tooltip("场景中最大活跃物品数量")]
    [SerializeField] private int maxActiveItems = 100;
    
    [Tooltip("超出上限时每次清理的数量")]
    [SerializeField] private int cleanupBatchSize = 10;
    
    [Header("冷却配置")]
    [Tooltip("资源节点掉落物的默认拾取冷却时间（秒）")]
    [SerializeField] private float defaultSpawnCooldown = 1f;

    [Header("引用")]
    [Tooltip("物品数据库")]
    [SerializeField] private ItemDatabase database;

    #endregion

    #region 私有字段

    private Stack<WorldItemPickup> _defaultPool = new Stack<WorldItemPickup>();
    private Dictionary<int, Stack<WorldItemPickup>> _poolByItemId = new Dictionary<int, Stack<WorldItemPickup>>();
    private List<WorldItemPickup> _activeItems = new List<WorldItemPickup>();
    private Transform _poolContainer;

    #endregion

    #region 属性

    public int ActiveCount => _activeItems.Count;
    public int PoolCount => _defaultPool.Count + _poolByItemId.Values.Sum(stack => stack.Count);
    public ItemDatabase Database => database;

    #endregion

    #region Unity生命周期

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 创建池容器
        _poolContainer = new GameObject("WorldItemPool_Container").transform;
        _poolContainer.SetParent(transform);
        _poolContainer.localPosition = new Vector3(9999f, 9999f, 0f);

        // 尝试获取数据库
        if (database == null)
        {
            database = Resources.Load<ItemDatabase>("Data/Database/MasterItemDatabase");
        }

        // 创建默认预制体（如果未指定）
        if (defaultWorldPrefab == null)
        {
            CreateDefaultPrefab();
        }

        // 预热对象池
        WarmupPool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 从池中获取一个物品实例
    /// </summary>
    /// <param name="setSpawnCooldown">是否设置生成冷却（资源节点掉落物使用）</param>
    public WorldItemPickup Spawn(ItemData data, int quality, int amount, Vector3 position, bool playAnimation = true, bool setSpawnCooldown = true)
    {
        // 检查数量上限
        if (_activeItems.Count >= maxActiveItems)
        {
            CleanupOldestItems();
        }

        // ★ 优先使用 ItemData.worldPrefab
        WorldItemPickup item = GetFromPoolByItemId(data);
        if (item == null) return null;

        // 设置位置
        item.transform.position = position;
        item.gameObject.SetActive(true);

        // 初始化数据
        item.Init(data, quality, amount);
        
        // ★ 设置生成冷却（使用可配置参数）
        if (setSpawnCooldown)
        {
            item.SetSpawnCooldown(defaultSpawnCooldown);
        }

        // 播放动画
        if (playAnimation)
        {
            var dropAnim = item.GetComponent<WorldItemDrop>();
            if (dropAnim != null)
            {
                // 随机方向弹出
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                dropAnim.StartDrop(direction, Random.Range(0.5f, 1.5f));
            }
        }

        _activeItems.Add(item);
        return item;
    }

    /// <summary>
    /// 从池中获取一个物品实例（通过ID）
    /// </summary>
    /// <param name="setSpawnCooldown">是否设置生成冷却</param>
    public WorldItemPickup SpawnById(int itemId, int quality, int amount, Vector3 position, bool playAnimation = true, bool setSpawnCooldown = true)
    {
        if (database == null) return null;
        
        var data = database.GetItemByID(itemId);
        if (data == null) return null;

        return Spawn(data, quality, amount, position, playAnimation, setSpawnCooldown);
    }

    /// <summary>
    /// 回收物品到池中
    /// </summary>
    public void Despawn(WorldItemPickup item)
    {
        if (item == null) return;

        _activeItems.Remove(item);

        // 停止动画
        var dropAnim = item.GetComponent<WorldItemDrop>();
        if (dropAnim != null)
        {
            dropAnim.StopAnimation();
        }
        
        // ★ 重置物品状态
        item.Reset();

        // ★ 根据 itemId 放入对应的池
        int itemId = item.ItemId;
        if (itemId >= 0)
        {
            if (!_poolByItemId.ContainsKey(itemId))
            {
                _poolByItemId[itemId] = new Stack<WorldItemPickup>();
            }
            
            if (_poolByItemId[itemId].Count < maxPoolSizePerItem)
            {
                item.gameObject.SetActive(false);
                item.transform.SetParent(_poolContainer);
                item.transform.localPosition = Vector3.zero;
                _poolByItemId[itemId].Push(item);
                Debug.Log($"<color=cyan>[WorldItemPool] 回收到 itemId={itemId} 池，当前池大小={_poolByItemId[itemId].Count}</color>");
                return;
            }
            else
            {
                Debug.Log($"<color=yellow>[WorldItemPool] itemId={itemId} 池已满，销毁物品</color>");
            }
        }
        
        // 池已满或无效 itemId，尝试放入默认池
        if (_defaultPool.Count < maxPoolSize)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(_poolContainer);
            item.transform.localPosition = Vector3.zero;
            _defaultPool.Push(item);
        }
        else
        {
            // 默认池也满了，直接销毁
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// 批量生成多个物品（带随机偏移）
    /// </summary>
    public List<WorldItemPickup> SpawnMultiple(ItemData data, int quality, int totalAmount, 
                                                Vector3 origin, float spreadRadius = 0.5f)
    {
        var result = new List<WorldItemPickup>();
        
        // 根据堆叠上限分配数量（每个物品单独生成，不堆叠）
        int itemCount = totalAmount;
        
        // 计算均匀分布的位置
        List<Vector3> positions = CalculateScatteredPositions(origin, itemCount, spreadRadius);

        for (int i = 0; i < itemCount; i++)
        {
            Vector3 spawnPos = positions[i];
            var item = Spawn(data, quality, 1, spawnPos, true);
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
    /// 清理所有活跃物品
    /// </summary>
    public void ClearAll()
    {
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            Despawn(_activeItems[i]);
        }
        _activeItems.Clear();
    }

    #endregion

    #region 私有方法

    private void CreateDefaultPrefab()
    {
        // 创建默认预制体结构
        var root = new GameObject("DefaultWorldItem");
        root.transform.SetParent(_poolContainer);
        root.transform.localPosition = Vector3.zero;
        
        // ★ 设置 Tag 为 Pickup（用于 AutoPickupService 检测）
        root.tag = "Pickup";

        // 添加组件
        var pickup = root.AddComponent<WorldItemPickup>();
        var dropAnim = root.AddComponent<WorldItemDrop>();
        
        // ★ 添加 CircleCollider2D (Trigger) 用于拾取检测
        var collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // 创建Sprite子物体
        var spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(root.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        var sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Layer 1";

        // 创建Shadow子物体
        var shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(root.transform);
        shadowObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        shadowObj.transform.localScale = new Vector3(0.5f, 0.3f, 1f);
        var shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sortingLayerName = "Layer 1";
        shadowSr.sortingOrder = -1;
        shadowSr.color = new Color(0f, 0f, 0f, 0.3f);

        // 创建椭圆阴影Sprite（使用Unity内置圆形）
        shadowSr.sprite = CreateEllipseShadowSprite();

        root.SetActive(false);
        defaultWorldPrefab = root;
    }

    private Sprite CreateEllipseShadowSprite()
    {
        // 创建一个简单的椭圆阴影纹理
        int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radiusX = size / 2f;
        float radiusY = size / 3f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center.x) / radiusX;
                float dy = (y - center.y) / radiusY;
                float dist = dx * dx + dy * dy;

                if (dist <= 1f)
                {
                    float alpha = 1f - dist;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha * 0.5f);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    private void WarmupPool()
    {
        if (defaultWorldPrefab == null) return;

        for (int i = 0; i < initialPoolSize; i++)
        {
            var obj = Instantiate(defaultWorldPrefab, _poolContainer);
            obj.SetActive(false);
            var pickup = obj.GetComponent<WorldItemPickup>();
            if (pickup != null)
            {
                _defaultPool.Push(pickup);
            }
        }
    }

    private WorldItemPickup GetFromPool()
    {
        if (_defaultPool.Count > 0)
        {
            return _defaultPool.Pop();
        }

        // 池为空，创建新实例
        if (defaultWorldPrefab != null && _activeItems.Count + _defaultPool.Count < maxPoolSize)
        {
            var obj = Instantiate(defaultWorldPrefab);
            return obj.GetComponent<WorldItemPickup>();
        }

        Debug.LogWarning("[WorldItemPool] 对象池已满，无法创建新实例");
        return null;
    }
    
    /// <summary>
    /// 根据 ItemData 从池中获取物品（优先使用 worldPrefab）
    /// </summary>
    private WorldItemPickup GetFromPoolByItemId(ItemData data)
    {
        if (data == null) return GetFromPool();
        
        int itemId = data.itemID;
        
        // 1. 尝试从对应 itemId 的池中获取
        if (_poolByItemId.TryGetValue(itemId, out var pool) && pool.Count > 0)
        {
            Debug.Log($"<color=cyan>[WorldItemPool] 从 itemId={itemId} 池中获取</color>");
            return pool.Pop();
        }
        
        // 2. 检查是否有 worldPrefab
        if (data.worldPrefab != null)
        {
            Debug.Log($"<color=green>[WorldItemPool] 使用 worldPrefab 实例化 itemId={itemId}</color>");
            var obj = Instantiate(data.worldPrefab);
            var pickup = obj.GetComponent<WorldItemPickup>();
            if (pickup != null)
            {
                return pickup;
            }
            else
            {
                Debug.LogWarning($"[WorldItemPool] worldPrefab 缺少 WorldItemPickup 组件，回退到默认池");
                Destroy(obj);
            }
        }
        else
        {
            Debug.LogWarning($"[WorldItemPool] itemId={itemId} 没有 worldPrefab，使用默认预制体");
        }
        
        // 3. 回退到默认池
        return GetFromPool();
    }

    private void CleanupOldestItems()
    {
        int toRemove = Mathf.Min(cleanupBatchSize, _activeItems.Count);
        
        for (int i = 0; i < toRemove; i++)
        {
            if (_activeItems.Count > 0)
            {
                // 移除最早添加的物品
                var oldest = _activeItems[0];
                Despawn(oldest);
            }
        }

        Debug.Log($"[WorldItemPool] 清理了 {toRemove} 个物品，当前活跃: {_activeItems.Count}");
    }

    #endregion

    #region 编辑器

#if UNITY_EDITOR
    [ContextMenu("清理所有物品")]
    private void DEBUG_ClearAll()
    {
        ClearAll();
    }

    [ContextMenu("显示池状态")]
    private void DEBUG_ShowStatus()
    {
        int totalPooled = _defaultPool.Count + _poolByItemId.Values.Sum(stack => stack.Count);
        Debug.Log($"[WorldItemPool] 活跃: {_activeItems.Count}, 池中总计: {totalPooled} (默认池: {_defaultPool.Count}, 分类池: {_poolByItemId.Count} 种)");
        
        foreach (var kvp in _poolByItemId)
        {
            Debug.Log($"  - itemId={kvp.Key}: {kvp.Value.Count} 个");
        }
    }
#endif

    #endregion
}
