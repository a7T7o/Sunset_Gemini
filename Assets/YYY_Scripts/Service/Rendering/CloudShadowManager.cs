using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 云朵阴影管理器
/// 管理云朵阴影的生成、移动、销毁
/// 
/// 核心机制：
/// 1. 云朵从移动方向的起始边缘生成
/// 2. 云朵移动到终点边缘后销毁并回收到对象池
/// 3. 云朵之间保持最小间距，避免重叠
/// 4. 严格控制云朵数量，不超过 maxClouds
/// </summary>
[ExecuteAlways]
public class CloudShadowManager : MonoBehaviour
{
    public enum WeatherState { Sunny, PartlyCloudy, Overcast, Rain, Snow }
    
    public enum AreaSizeMode
    {
        Manual,         // 手动设置区域大小
        FromNavGrid,    // 从 NavGrid2D 获取
        AutoDetect      // 自动检测 Tilemap 边界
    }

    [Header("Enable")] 
    [SerializeField] private bool enableCloudShadows = true;

    [Header("Appearance")]
    [Range(0f, 1f)]
    [SerializeField] private float intensity = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float density = 0.6f;
    [Tooltip("云朵缩放范围 - 增大差异让云朵大小更随机")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.4f, 2.5f);
    [Tooltip("Sprites used for cloud shadows (grayscale/alpha). If empty, manager remains idle.")]
    [SerializeField] private Sprite[] cloudSprites;
    [Tooltip("Optional material (Multiply recommended). If null, Sprite default material is used.")]
    [SerializeField] private Material cloudMaterial;

    [Header("Movement")]
    [SerializeField] private Vector2 direction = new Vector2(1f, 0f);
    [SerializeField, Range(0f, 5f)] private float speed = 0.4f;

    [Header("Area")]
    [Tooltip("区域大小获取模式")]
    [SerializeField] private AreaSizeMode areaSizeMode = AreaSizeMode.Manual;
    [Tooltip("Cloud simulation area size centered at manager's transform.")]
    [SerializeField] private Vector2 areaSize = new Vector2(40f, 24f);
    [Tooltip("自动检测时使用的世界层级名称")]
    [SerializeField] private string[] worldLayerNames = new string[] { "LAYER 1", "LAYER 2", "LAYER 3" };
    [Tooltip("边界扩展（留出余量）")]
    [SerializeField] private float boundsPadding = 5f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "CloudShadow";
    [SerializeField] private int sortingOrder = 0;

    [Header("Anti-Overlap")]
    [Tooltip("云朵之间的最小间距（中心点距离）")]
    [SerializeField] private float minCloudSpacing = 5f;
    [Tooltip("生成时尝试找到不重叠位置的最大次数")]
    [SerializeField] private int maxSpawnAttempts = 15;
    [Tooltip("生成冷却时间（秒），避免短时间内生成过多云朵")]
    [SerializeField] private float spawnCooldown = 0.5f;

    [Header("Weather Gate (manual)")]
    [SerializeField] private bool useWeatherGate = false;
    [SerializeField] private WeatherState currentWeather = WeatherState.Sunny;
    [SerializeField] private bool enableInSunny = true;
    [SerializeField] private bool enableInPartlyCloudy = true;
    [SerializeField] private bool enableInOvercast = false;
    [SerializeField] private bool enableInRain = false;
    [SerializeField] private bool enableInSnow = false;

    [Header("Seed & Preview")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool randomizeOnStart = true;
    [SerializeField] private bool previewInEditor = false;

    [Header("Limits")]
    [SerializeField, Range(1, 20)] private int maxClouds = 6;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = false;

    private System.Random rng;
    private float lastSpawnTime = -999f;

    private struct Cloud
    {
        public Transform transform;
        public SpriteRenderer sr;
        public float halfWidth;
        public float halfHeight;
        public int id; // 用于调试
    }

    private readonly List<Cloud> active = new List<Cloud>();
    private readonly Stack<GameObject> pool = new Stack<GameObject>();
    private bool initialized;
    private int cloudIdCounter = 0;

    void OnEnable()
    {
        EnsureRng();
        if (Application.isPlaying)
        {
            InitializeIfNeeded();
        }
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorUpdate;
        UnityEditor.EditorApplication.update += EditorUpdate;
        #endif
    }

    void OnDisable()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorUpdate;
        #endif
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            InitializeIfNeeded();
            
            // 订阅天气系统事件
            if (useWeatherGate)
            {
                WeatherSystem.OnWeatherChanged += OnWeatherChanged;
                
                // 初始化天气状态
                if (WeatherSystem.Instance != null)
                {
                    UpdateWeatherState(WeatherSystem.Instance.GetCurrentWeather());
                }
            }
        }
    }
    
    void OnDestroy()
    {
        if (useWeatherGate)
        {
            WeatherSystem.OnWeatherChanged -= OnWeatherChanged;
        }
    }
    
    /// <summary>
    /// 天气变化回调
    /// </summary>
    private void OnWeatherChanged(WeatherSystem.Weather weather)
    {
        UpdateWeatherState(weather);
    }
    
    /// <summary>
    /// 根据天气更新云影状态
    /// </summary>
    private void UpdateWeatherState(WeatherSystem.Weather weather)
    {
        // 将 WeatherSystem.Weather 映射到 CloudShadowManager.WeatherState
        WeatherState state = weather switch
        {
            WeatherSystem.Weather.Sunny => WeatherState.Sunny,
            WeatherSystem.Weather.Rainy => WeatherState.Rain,
            WeatherSystem.Weather.Withering => WeatherState.Overcast, // 枯萎天视为阴天
            _ => WeatherState.Sunny
        };
        
        SetWeatherState(state);
        
        Debug.Log($"<color=cyan>[CloudShadowManager] 天气变化: {weather} → {state}, 云影启用: {IsWeatherAllowed(state)}</color>");
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        SimulateStep(Time.deltaTime);
    }

    public void SimulateStep(float dt)
    {
        if (!enableCloudShadows) return;
        if (useWeatherGate && !IsWeatherAllowed(currentWeather)) return;

        if (cloudSprites == null || cloudSprites.Length == 0) return;
        if (density <= 0f || maxClouds <= 0) { DespawnAll(); return; }

        InitializeIfNeeded();
        
        // 计算目标云朵数量
        int target = Mathf.Clamp(Mathf.RoundToInt(density * maxClouds), 1, maxClouds);

        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : new Vector2(1f, 0f);
        float spd = Mathf.Max(0f, speed);

        Rect area = GetWorldAreaRect();
        
        // 第一步：清理无效云朵和超出边界的云朵
        CleanupInvalidClouds(area, dir);
        
        // 第二步：移动所有云朵
        MoveClouds(dir, spd, dt);
        
        // 第三步：如果数量超过目标，移除多余的
        while (active.Count > target)
        {
            DespawnLast();
        }
        
        // 第四步：如果数量不足且冷却时间已过，生成新云朵
        float currentTime = Time.time;
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            currentTime = (float)UnityEditor.EditorApplication.timeSinceStartup;
        }
        #endif
        if (active.Count < target && (currentTime - lastSpawnTime) >= spawnCooldown)
        {
            // 每帧最多生成1个云朵，避免瞬间生成过多
            if (TrySpawnOneAtEdge(area, dir))
            {
                lastSpawnTime = currentTime;
            }
        }
        
        if (enableDebug)
        {
            Debug.Log($"[CloudShadow] Active: {active.Count}/{target}, Pool: {pool.Count}");
        }
    }
    
    /// <summary>
    /// 清理无效和超出边界的云朵
    /// </summary>
    private void CleanupInvalidClouds(Rect area, Vector2 dir)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Cloud c = active[i];
            
            // 检查 Transform 是否有效
            if (c.transform == null)
            {
                active.RemoveAt(i);
                continue;
            }
            
            Vector3 p = c.transform.position;
            
            // 计算销毁边界（云朵完全离开区域后销毁）
            float margin = 1f;
            bool outOfBounds = false;
            
            if (dir.x > 0f && p.x > area.xMax + c.halfWidth + margin) outOfBounds = true;
            else if (dir.x < 0f && p.x < area.xMin - c.halfWidth - margin) outOfBounds = true;
            else if (dir.y > 0f && p.y > area.yMax + c.halfHeight + margin) outOfBounds = true;
            else if (dir.y < 0f && p.y < area.yMin - c.halfHeight - margin) outOfBounds = true;
            
            if (outOfBounds)
            {
                if (enableDebug)
                {
                    Debug.Log($"[CloudShadow] 销毁云朵 #{c.id} (超出边界)");
                }
                DespawnAt(i);
            }
        }
    }
    
    /// <summary>
    /// 移动所有云朵
    /// </summary>
    private void MoveClouds(Vector2 dir, float spd, float dt)
    {
        for (int i = 0; i < active.Count; i++)
        {
            Cloud c = active[i];
            if (c.transform == null) continue;
            
            Vector3 p = c.transform.position;
            p += (Vector3)(dir * spd * dt);
            c.transform.position = p;
        }
    }

    private void InitializeIfNeeded()
    {
        if (initialized) return;
        initialized = true;
        if (randomizeOnStart) seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        EnsureRng();
        
        // 根据模式自动获取区域大小
        UpdateAreaSizeFromMode();
        
        // 初始化时在整个区域内随机分布云朵
        RebuildClouds();
    }
    
    /// <summary>
    /// 根据当前模式更新区域大小
    /// </summary>
    public void UpdateAreaSizeFromMode()
    {
        switch (areaSizeMode)
        {
            case AreaSizeMode.FromNavGrid:
                UpdateAreaSizeFromNavGrid();
                break;
            case AreaSizeMode.AutoDetect:
                DetectWorldBounds();
                break;
            // Manual 模式不做任何处理
        }
    }
    
    /// <summary>
    /// 从 NavGrid2D 获取区域大小
    /// </summary>
    private void UpdateAreaSizeFromNavGrid()
    {
        NavGrid2D navGrid = FindFirstObjectByType<NavGrid2D>();
        if (navGrid != null)
        {
            // 使用反射获取 NavGrid2D 的 worldSize（因为是私有字段）
            var worldSizeField = typeof(NavGrid2D).GetField("worldSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var worldOriginField = typeof(NavGrid2D).GetField("worldOrigin", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (worldSizeField != null && worldOriginField != null)
            {
                Vector2 navWorldSize = (Vector2)worldSizeField.GetValue(navGrid);
                Vector2 navWorldOrigin = (Vector2)worldOriginField.GetValue(navGrid);
                
                areaSize = navWorldSize + Vector2.one * boundsPadding * 2f;
                
                // 将 CloudShadowManager 移动到世界中心
                Vector3 worldCenter = new Vector3(
                    navWorldOrigin.x + navWorldSize.x * 0.5f,
                    navWorldOrigin.y + navWorldSize.y * 0.5f,
                    transform.position.z
                );
                transform.position = worldCenter;
                
                Debug.Log($"<color=cyan>[CloudShadowManager] 从 NavGrid2D 获取区域: Size={areaSize}, Center={worldCenter}</color>");
            }
        }
        else
        {
            Debug.LogWarning("[CloudShadowManager] 未找到 NavGrid2D，使用手动设置的区域大小");
        }
    }
    
    /// <summary>
    /// 自动检测世界边界（基于 Tilemap）
    /// </summary>
    private void DetectWorldBounds()
    {
        Bounds totalBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool boundsInitialized = false;
        
        // 查找所有指定层级下的 Tilemap
        foreach (string layerName in worldLayerNames)
        {
            GameObject layerObj = GameObject.Find(layerName);
            if (layerObj == null) continue;
            
            Tilemap[] tilemaps = layerObj.GetComponentsInChildren<Tilemap>();
            foreach (Tilemap tilemap in tilemaps)
            {
                tilemap.CompressBounds();
                BoundsInt tilemapBounds = tilemap.cellBounds;
                
                if (tilemapBounds.size.x == 0 || tilemapBounds.size.y == 0) continue;
                
                Vector3 worldMin = tilemap.transform.TransformPoint(tilemap.CellToLocal(tilemapBounds.min));
                Vector3 worldMax = tilemap.transform.TransformPoint(tilemap.CellToLocal(tilemapBounds.max));
                Bounds worldBounds = new Bounds();
                worldBounds.SetMinMax(worldMin, worldMax);
                
                if (!boundsInitialized)
                {
                    totalBounds = worldBounds;
                    boundsInitialized = true;
                }
                else
                {
                    totalBounds.Encapsulate(worldBounds);
                }
            }
        }
        
        if (boundsInitialized)
        {
            // 添加边界扩展
            totalBounds.Expand(boundsPadding * 2f);
            
            areaSize = new Vector2(totalBounds.size.x, totalBounds.size.y);
            
            // 将 CloudShadowManager 移动到世界中心
            Vector3 worldCenter = new Vector3(totalBounds.center.x, totalBounds.center.y, transform.position.z);
            transform.position = worldCenter;
            
            Debug.Log($"<color=cyan>[CloudShadowManager] 自动检测区域: Size={areaSize}, Center={worldCenter}</color>");
        }
        else
        {
            Debug.LogWarning("[CloudShadowManager] 未找到有效的 Tilemap，使用手动设置的区域大小");
        }
    }

    /// <summary>
    /// 尝试在移动方向的起始边缘生成新云朵
    /// </summary>
    /// <returns>是否成功生成</returns>
    private bool TrySpawnOneAtEdge(Rect area, Vector2 dir)
    {
        // 严格检查数量限制
        if (active.Count >= maxClouds) return false;
        
        Sprite sprite = PickSprite();
        if (sprite == null) return false;
        
        // 先计算缩放和尺寸
        float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, Next01());
        
        // 估算云朵尺寸（基于 sprite 原始尺寸和缩放）
        float estimatedHalfW = (sprite.bounds.extents.x * scale);
        float estimatedHalfH = (sprite.bounds.extents.y * scale);
        
        // 尝试找到不重叠的位置
        Vector3? position = TryFindNonOverlappingEdgePosition(area, dir, estimatedHalfW, estimatedHalfH);
        
        if (!position.HasValue)
        {
            if (enableDebug)
            {
                Debug.Log($"[CloudShadow] 无法找到不重叠的位置，跳过生成");
            }
            return false;
        }
        
        // 创建云朵对象
        GameObject go = GetOrCreateCloudObject();
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (cloudMaterial != null) sr.sharedMaterial = cloudMaterial;

        int sortingId = SortingLayer.NameToID(sortingLayerName);
        if (sortingId != 0 || sortingLayerName == "Default")
        {
            sr.sortingLayerID = sortingId;
            sr.sortingOrder = sortingOrder;
        }

        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.transform.position = position.Value;
        
        // 获取实际尺寸
        Bounds b = sr.bounds;
        float halfW = b.extents.x;
        float halfH = b.extents.y;
        
        int cloudId = cloudIdCounter++;
        var c = new Cloud 
        { 
            transform = go.transform, 
            sr = sr, 
            halfWidth = halfW, 
            halfHeight = halfH,
            id = cloudId
        };
        
        Color col = sr.color; 
        col.a = intensity; 
        sr.color = col;
        
        active.Add(c);
        
        if (enableDebug)
        {
            Debug.Log($"[CloudShadow] 生成云朵 #{cloudId} at {position.Value}, scale={scale:F2}, 当前数量: {active.Count}");
        }
        
        return true;
    }
    
    /// <summary>
    /// 初始化时在整个区域内随机分布云朵
    /// </summary>
    private bool TrySpawnOneRandom(Rect area)
    {
        if (active.Count >= maxClouds) return false;
        
        Sprite sprite = PickSprite();
        if (sprite == null) return false;
        
        float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, Next01());
        float estimatedHalfW = sprite.bounds.extents.x * scale;
        float estimatedHalfH = sprite.bounds.extents.y * scale;
        
        // 尝试找到不重叠的位置
        Vector3? position = TryFindNonOverlappingAreaPosition(area, estimatedHalfW, estimatedHalfH);
        
        if (!position.HasValue) return false;
        
        GameObject go = GetOrCreateCloudObject();
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (cloudMaterial != null) sr.sharedMaterial = cloudMaterial;

        int sortingId = SortingLayer.NameToID(sortingLayerName);
        if (sortingId != 0 || sortingLayerName == "Default")
        {
            sr.sortingLayerID = sortingId;
            sr.sortingOrder = sortingOrder;
        }

        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.transform.position = position.Value;
        
        Bounds b = sr.bounds;
        int cloudId = cloudIdCounter++;
        var c = new Cloud 
        { 
            transform = go.transform, 
            sr = sr, 
            halfWidth = b.extents.x, 
            halfHeight = b.extents.y,
            id = cloudId
        };
        
        Color col = sr.color; 
        col.a = intensity; 
        sr.color = col;
        
        active.Add(c);
        return true;
    }
    
    /// <summary>
    /// 获取或创建云朵对象
    /// </summary>
    private GameObject GetOrCreateCloudObject()
    {
        GameObject go;
        if (pool.Count > 0)
        {
            go = pool.Pop();
            go.SetActive(true);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) go.AddComponent<SpriteRenderer>();
            go.transform.SetParent(transform, false);
        }
        else
        {
            go = new GameObject("CloudShadow");
            go.transform.SetParent(transform, false);
            go.AddComponent<SpriteRenderer>();
        }
        return go;
    }
    
    /// <summary>
    /// 尝试在边缘找到不重叠的位置
    /// </summary>
    /// <returns>找到的位置，如果找不到返回 null</returns>
    private Vector3? TryFindNonOverlappingEdgePosition(Rect area, Vector2 dir, float halfW, float halfH)
    {
        // 计算生成边缘位置（在区域外一点点）
        float spawnOffset = 0.5f;
        float left = area.xMin - halfW - spawnOffset;
        float right = area.xMax + halfW + spawnOffset;
        float bottom = area.yMin - halfH - spawnOffset;
        float top = area.yMax + halfH + spawnOffset;
        
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 p = Vector3.zero;
            
            // 根据移动方向决定生成边缘
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                // 水平移动：在左边或右边生成
                p.x = dir.x > 0f ? left : right;
                p.y = Mathf.Lerp(area.yMin, area.yMax, Next01());
            }
            else
            {
                // 垂直移动：在上边或下边生成
                p.y = dir.y > 0f ? bottom : top;
                p.x = Mathf.Lerp(area.xMin, area.xMax, Next01());
            }
            
            if (!IsOverlappingWithExisting(p, halfW, halfH))
            {
                return p;
            }
        }
        
        return null; // 找不到合适位置
    }
    
    /// <summary>
    /// 尝试在整个区域内找到不重叠的位置
    /// </summary>
    private Vector3? TryFindNonOverlappingAreaPosition(Rect area, float halfW, float halfH)
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 p = new Vector3(
                Mathf.Lerp(area.xMin + halfW, area.xMax - halfW, Next01()),
                Mathf.Lerp(area.yMin + halfH, area.yMax - halfH, Next01()),
                0f
            );
            
            if (!IsOverlappingWithExisting(p, halfW, halfH))
            {
                return p;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 检查位置是否与现有云朵重叠
    /// 使用简单的圆形距离检测
    /// </summary>
    private bool IsOverlappingWithExisting(Vector3 position, float halfW, float halfH)
    {
        float myRadius = Mathf.Max(halfW, halfH);
        
        foreach (var cloud in active)
        {
            if (cloud.transform == null) continue;
            
            float otherRadius = Mathf.Max(cloud.halfWidth, cloud.halfHeight);
            float dist = Vector2.Distance(position, cloud.transform.position);
            float minDist = minCloudSpacing + myRadius + otherRadius;
            
            if (dist < minDist)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 销毁指定索引的云朵
    /// </summary>
    private void DespawnAt(int index)
    {
        if (index < 0 || index >= active.Count) return;
        var c = active[index];
        if (c.transform != null)
        {
            var go = c.transform.gameObject;
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            pool.Push(go);
        }
        active.RemoveAt(index);
    }

    private void DespawnLast()
    {
        if (active.Count == 0) return;
        DespawnAt(active.Count - 1);
    }

    private void DespawnAll()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var c = active[i];
            if (c.transform != null)
            {
                var go = c.transform.gameObject;
                go.SetActive(false);
                go.transform.SetParent(transform, false);
                pool.Push(go);
            }
        }
        active.Clear();
    }

    private Sprite PickSprite()
    {
        if (cloudSprites == null || cloudSprites.Length == 0) return null;
        int idx = Mathf.Clamp((int)(Next01() * cloudSprites.Length), 0, cloudSprites.Length - 1);
        return cloudSprites[idx];
    }

    private Rect GetWorldAreaRect()
    {
        Vector3 c = transform.position;
        float w = Mathf.Max(0.1f, areaSize.x);
        float h = Mathf.Max(0.1f, areaSize.y);
        return new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h);
    }

    private bool IsWeatherAllowed(WeatherState s)
    {
        switch (s)
        {
            case WeatherState.Sunny: return enableInSunny;
            case WeatherState.PartlyCloudy: return enableInPartlyCloudy;
            case WeatherState.Overcast: return enableInOvercast;
            case WeatherState.Rain: return enableInRain;
            case WeatherState.Snow: return enableInSnow;
        }
        return true;
    }

    private void EnsureRng()
    {
        if (rng == null) rng = new System.Random(seed);
    }

    private float Next01()
    {
        if (rng == null) EnsureRng();
        return (float)rng.NextDouble();
    }

    public void RandomizeSeed()
    {
        seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        rng = new System.Random(seed);
    }

    public void SetWeatherState(WeatherState s)
    {
        currentWeather = s;
    }

    public void EditorRebuildNow()
    {
        initialized = false;
        InitializeIfNeeded();
    }
    
    /// <summary>
    /// 重建所有云朵（清除后重新生成）
    /// </summary>
    private void RebuildClouds()
    {
        DespawnAll();
        cloudIdCounter = 0;
        
        Rect area = GetWorldAreaRect();
        int target = Mathf.Clamp(Mathf.RoundToInt(density * maxClouds), 1, maxClouds);
        
        int spawned = 0;
        int maxAttempts = target * 3; // 防止无限循环
        int attempts = 0;
        
        while (spawned < target && attempts < maxAttempts)
        {
            if (TrySpawnOneRandom(area))
            {
                spawned++;
            }
            attempts++;
        }
        
        if (enableDebug)
        {
            Debug.Log($"[CloudShadow] 初始化完成: 生成 {spawned}/{target} 个云朵");
        }
    }

    public void EditorDespawnAll()
    {
        DespawnAll();
    }

    #if UNITY_EDITOR
    private double lastEditorTime;
    private void EditorUpdate()
    {
        if (Application.isPlaying) return;
        if (!previewInEditor) return;
        double now = UnityEditor.EditorApplication.timeSinceStartup;
        double dt = now - lastEditorTime;
        lastEditorTime = now;
        SimulateStep((float)Mathf.Clamp((float)dt, 0f, 0.1f));
    }
    #endif

    void OnDrawGizmosSelected()
    {
        Rect r = GetWorldAreaRect();
        Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
        Gizmos.DrawWireCube(new Vector3(r.center.x, r.center.y, 0f), new Vector3(r.width, r.height, 0f));

        // Draw movement direction arrow
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : new Vector2(1f, 0f);
        float len = Mathf.Min(r.width, r.height) * 0.3f;
        Vector3 start = new Vector3(r.center.x, r.center.y, 0f);
        Vector3 end = start + (Vector3)(dir * len);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawLine(start, end);
        // arrow head
        Vector3 right = Quaternion.Euler(0, 0, 150f) * (Vector3)(dir * (len * 0.2f));
        Vector3 left = Quaternion.Euler(0, 0, -150f) * (Vector3)(dir * (len * 0.2f));
        Gizmos.DrawLine(end, end + right);
        Gizmos.DrawLine(end, end + left);
    }
}
