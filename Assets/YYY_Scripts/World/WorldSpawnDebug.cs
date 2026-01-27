using UnityEngine;
using UnityEngine.Tilemaps;
using FarmGame.Data;

public class WorldSpawnDebug : MonoBehaviour
{
    [Header("Refs")]
    [HideInInspector] public WorldSpawnService spawnService;
    [HideInInspector] public InventoryService inventory;
    [HideInInspector] public ItemDatabase database;
    [HideInInspector] public HotbarSelectionService hotbar;
    [HideInInspector] public Camera worldCamera;

    [Header("Config")]
    public ItemData item;
    [Tooltip("品质覆盖值（仅对非工具/武器类物品生效）\n工具和武器会使用其 SO 中定义的 quality 字段")]
    [Range(0,4)] public int quality = 0;
    [Min(1)] public int amount = 1;
    public bool ctrlLeftClickToSpawn = true;
    public bool middleClickSpawnHotbar = true;
    
    [Header("动画选项")]
    [Tooltip("生成时是否播放弹性动画")]
    public bool playDropAnimation = true;

    void Awake()
    {
        if (spawnService == null) spawnService = FindFirstObjectByType<WorldSpawnService>();
        if (inventory == null) inventory = FindFirstObjectByType<InventoryService>();
        if (database == null) database = FindFirstObjectByType<ItemDatabase>();
        if (hotbar == null) hotbar = FindFirstObjectByType<HotbarSelectionService>();
        if (worldCamera == null) worldCamera = Camera.main;
    }

    void Update()
    {
        if (spawnService == null || worldCamera == null) return;

        if (ctrlLeftClickToSpawn && Input.GetMouseButtonDown(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            Vector3 mouse = Input.mousePosition;
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0f));
            world.z = 0f;
            if (item != null)
            {
                // 获取实际品质：工具/武器使用 SO 自带品质，其他物品使用 Inspector 设置的 quality
                int actualQuality = GetActualQuality(item);
                var spawned = spawnService.SpawnFromItem(item, actualQuality, amount, world, playDropAnimation);
                if (spawned != null)
                {
                    // 根据点击区域的Sorting Layer设置生成物体的层级
                    ApplySortingLayerFromClick(world, spawned.gameObject);
                }
            }
        }

        if (middleClickSpawnHotbar && Input.GetMouseButtonDown(2))
        {
            if (inventory != null && database != null && hotbar != null)
            {
                int idx = Mathf.Clamp(hotbar.selectedIndex, 0, InventoryService.HotbarWidth - 1);
                var s = inventory.GetSlot(idx);
                if (!s.IsEmpty)
                {
                    Vector3 mouse = Input.mousePosition;
                    Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0f));
                    world.z = 0f;
                    
                    // 使用带动画的生成方法
                    WorldItemPickup spawned = null;
                    if (playDropAnimation && WorldItemPool.Instance != null)
                    {
                        var data = database.GetItemByID(s.itemId);
                        if (data != null)
                        {
                            spawned = WorldItemPool.Instance.Spawn(data, s.quality, s.amount, world, true);
                        }
                    }
                    else
                    {
                        spawned = spawnService.Spawn(s, world);
                    }
                    
                    if (spawned != null)
                    {
                        // 根据点击区域的Sorting Layer设置生成物体的层级
                        ApplySortingLayerFromClick(world, spawned.gameObject);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取物品的实际品质
    /// 工具和武器：每个品质都是独立 ItemID，品质固定为 0（由 ItemID 本身决定品质）
    /// 其他物品：使用 Inspector 设置的 quality
    /// </summary>
    int GetActualQuality(ItemData itemData)
    {
        // 工具和武器：每个品质都是独立 ItemID，不再需要额外的 quality 参数
        if (itemData is ToolData || itemData is WeaponData)
        {
            return 0;  // 品质信息已包含在 ItemID 中
        }
        
        // 其他物品：使用 Inspector 设置的 quality
        return quality;
    }

    /// <summary>
    /// 根据点击位置检测该区域的Sorting Layer，并应用到生成的物体
    /// </summary>
    void ApplySortingLayerFromClick(Vector3 worldPos, GameObject spawnedObj)
    {
        // 检测点击位置的所有碰撞体
        var hits = Physics2D.OverlapPointAll(worldPos);
        
        string layerName = "Layer 1";  // 默认值
        int order = 0;
        
        // 遍历所有检测到的碰撞体
        foreach (var hit in hits)
        {
            // 跳过生成的物体和玩家
            if (hit.gameObject.name.Contains("Clone") || 
                hit.gameObject.name.Contains("Pickup") || 
                hit.gameObject.name.Contains("Player"))
            {
                continue;
            }
            
            // 尝试获取SpriteRenderer
            var sr = hit.GetComponentInParent<SpriteRenderer>();
            if (sr != null && !sr.gameObject.name.Contains("Clone"))
            {
                layerName = sr.sortingLayerName;
                order = sr.sortingOrder;
                break;
            }
            
            // 尝试获取TilemapRenderer
            var tr = hit.GetComponentInParent<TilemapRenderer>();
            if (tr != null)
            {
                layerName = tr.sortingLayerName;
                order = tr.sortingOrder;
                break;
            }
        }

        var spawnedSR = spawnedObj.GetComponentInChildren<SpriteRenderer>();
        if (spawnedSR != null)
        {
            if (!string.IsNullOrEmpty(layerName))
            {
                spawnedSR.sortingLayerName = layerName;
                spawnedSR.sortingOrder = order + 1;
                Debug.Log($"[WorldSpawnDebug] 设置生成物体: Layer={layerName}, Order={order + 1}");
            }
            else
            {
                spawnedSR.sortingLayerName = "Layer 1";
                spawnedSR.sortingOrder = 0;
                Debug.LogWarning($"[WorldSpawnDebug] 未检测到有效图层，使用默认 Layer 1");
            }
        }
        else
        {
            Debug.LogError($"[WorldSpawnDebug] 生成物体没有SpriteRenderer！");
        }
    }
}
