using UnityEngine;

/// <summary>
/// 遮挡透明组件：当玩家被此物体遮挡时，自动变透明
/// 挂载到树木、房屋等父物体上（双层结构：父物体不需要SpriteRenderer）
/// 会自动处理所有子物体的SpriteRenderer
/// 
/// ✅ 支持像素采样精确检测（需要纹理设置为 Read/Write Enabled）
/// </summary>
public class OcclusionTransparency : MonoBehaviour
{
    [Header("透明度设置")]
    [HideInInspector] [SerializeField] private float occludedAlpha = 0.3f;
    
    [HideInInspector] [SerializeField] private float fadeSpeed = 8f;
    
    [Header("遮挡检测")]
    [HideInInspector] [SerializeField] private bool canBeOccluded = true;
    
    [Header("像素采样设置")]
    [Tooltip("是否启用像素采样精确检测（需要纹理设置为 Read/Write Enabled）")]
    [SerializeField] private bool usePixelSampling = true;
    
    [Tooltip("像素 alpha 阈值（低于此值视为透明）")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float alphaThreshold = 0.1f;
    
    [Tooltip("采样点数量（中心 + 四角 = 5）")]
    [Range(1, 9)]
    [SerializeField] private int samplePointCount = 5;
    
    private SpriteRenderer mainRenderer;  // 用于获取Order（从子物体找第一个有效的）
    private SpriteRenderer[] childRenderers;
    private float[] originalAlphas;  // 记录原始透明度
    private float currentAlpha = 1f;
    private float targetAlpha = 1f;
    private bool isOccluding = false;
    
    // ✅ 砍伐状态：砍伐中的树木透明度更深
    private bool isBeingChopped = false;
    private float choppingAlphaOffset = 0.25f;  // 砍伐时透明度偏移（更深）
    
    // ✅ 像素采样缓存
    private Texture2D _cachedTexture;
    private Sprite _cachedSprite;
    private bool _textureReadable = true;  // 纹理是否可读
    
    void Awake()
    {
        // 获取所有子物体的SpriteRenderer（包括自己，如果有的话）
        childRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        if (childRenderers.Length == 0)
        {
            Debug.LogWarning($"[OcclusionTransparency] {gameObject.name} 没有找到任何SpriteRenderer！组件已禁用。请使用 Tools → 🧹 清理无效的遮挡组件 删除此组件。");
            enabled = false;  // 禁用组件，避免后续错误
            return;
        }
        
        // 找到第一个有效的SpriteRenderer作为mainRenderer（用于获取Order）
        mainRenderer = childRenderers[0];
        
        // 初始化完成
        
        // 记录原始透明度
        originalAlphas = new float[childRenderers.Length];
        for (int i = 0; i < childRenderers.Length; i++)
        {
            originalAlphas[i] = childRenderers[i].color.a;
        }
        
        currentAlpha = 1f;
        targetAlpha = 1f;
    }
    
    void OnEnable()
    {
        // 从管理器初始化参数（支持标签自定义参数）
        if (OcclusionManager.Instance != null)
        {
            OcclusionManager.Instance.GetOcclusionParams(gameObject.tag, out float alpha, out float speed);
            occludedAlpha = alpha;
            fadeSpeed = speed;
        }
        
        // 延迟注册，确保OcclusionManager已初始化
        if (canBeOccluded)
        {
            StartCoroutine(RegisterDelayed());
        }
    }
    
    private System.Collections.IEnumerator RegisterDelayed()
    {
        // 等待 OcclusionManager 初始化完成（最多等待 2 秒）
        float timeout = 2f;
        float elapsed = 0f;
        
        while (OcclusionManager.Instance == null && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        if (OcclusionManager.Instance != null)
        {
            OcclusionManager.Instance.RegisterOccluder(this);
        }
        else
        {
            Debug.LogWarning($"[OcclusionTransparency] {gameObject.name} 注册失败！未找到OcclusionManager（等待超时）");
        }
    }
    
    void OnDisable()
    {
        // 从管理器注销
        if (canBeOccluded)
        {
            OcclusionManager.Instance?.UnregisterOccluder(this);
        }
        
        // 恢复原始透明度
        SetOccluding(false);
    }
    
    void Update()
    {
        // 平滑过渡到目标透明度
        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
        {
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            ApplyAlpha(currentAlpha);
        }
    }
    
    /// <summary>
    /// 设置是否正在遮挡玩家
    /// </summary>
    /// <param name="occluding">是否遮挡</param>
    /// <param name="customAlpha">自定义透明度（可选，-1表示使用默认值）</param>
    /// <param name="customSpeed">自定义渐变速度（可选，-1表示使用默认值）</param>
    public void SetOccluding(bool occluding, float customAlpha = -1f, float customSpeed = -1f)
    {
        if (isOccluding == occluding) return;
        
        isOccluding = occluding;
        
        // 如果提供了自定义参数，使用自定义参数
        if (customAlpha >= 0f)
        {
            occludedAlpha = customAlpha;
        }
        
        if (customSpeed >= 0f)
        {
            fadeSpeed = customSpeed;
        }
        
        // ✅ 砍伐中的树木透明度加深（更不透明，alpha值更高）
        float finalAlpha = occludedAlpha;
        if (isBeingChopped && occluding)
        {
            finalAlpha = Mathf.Min(1f, occludedAlpha + choppingAlphaOffset);
        }
        
        targetAlpha = occluding ? finalAlpha : 1f;
    }
    
    /// <summary>
    /// 设置砍伐状态（砍伐中的树木透明度加深，更不透明）
    /// </summary>
    /// <param name="chopping">是否正在被砍伐</param>
    /// <param name="alphaOffset">透明度偏移量（默认0.25，值越大越不透明）</param>
    public void SetChoppingState(bool chopping, float alphaOffset = 0.25f)
    {
        isBeingChopped = chopping;
        choppingAlphaOffset = alphaOffset;
        
        // 如果当前正在遮挡，立即更新目标透明度
        if (isOccluding)
        {
            float finalAlpha = occludedAlpha;
            if (isBeingChopped)
            {
                finalAlpha = Mathf.Min(1f, occludedAlpha + choppingAlphaOffset);
            }
            targetAlpha = finalAlpha;
        }
    }
    
    /// <summary>
    /// 获取当前是否处于砍伐状态
    /// </summary>
    public bool IsBeingChopped => isBeingChopped;
    
    /// <summary>
    /// 应用透明度到所有渲染器
    /// </summary>
    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < childRenderers.Length; i++)
        {
            if (childRenderers[i] == null) continue;
            
            Color color = childRenderers[i].color;
            // 基于原始透明度计算新透明度
            color.a = originalAlphas[i] * alpha;
            childRenderers[i].color = color;
        }
    }
    
    /// <summary>
    /// 获取物体的Sorting Order（用于判断是否在玩家前方）
    /// </summary>
    public int GetSortingOrder()
    {
        return mainRenderer != null ? mainRenderer.sortingOrder : 0;
    }
    
    /// <summary>
    /// 获取物体的Sorting Layer Name
    /// </summary>
    public string GetSortingLayerName()
    {
        return mainRenderer != null ? mainRenderer.sortingLayerName : "";
    }
    
    /// <summary>
    /// 检查是否有指定标签
    /// </summary>
    public bool HasTag(string tag)
    {
        return CompareTag(tag);
    }
    
    /// <summary>
    /// 获取所有标签（用于调试）
    /// </summary>
    public string[] GetTags()
    {
        return new string[] { gameObject.tag };
    }
    
    /// <summary>
    /// 获取物体边界（用于遮挡检测）
    /// ✅ 只返回主 SpriteRenderer 的 bounds，不包含子物体（如 Shadow）
    /// </summary>
    public Bounds GetBounds()
    {
        if (mainRenderer != null)
            return mainRenderer.bounds;
        return new Bounds(transform.position, Vector3.one);
    }
    
    /// <summary>
    /// 获取树木的 Collider 边界（用于树林边界计算）
    /// 优先使用父物体的 CompositeCollider2D，其次使用子物体的 Collider2D
    /// </summary>
    public Bounds GetColliderBounds()
    {
        // 优先检查父物体的 CompositeCollider2D
        Collider2D parentCollider = transform.parent?.GetComponent<Collider2D>();
        if (parentCollider != null)
        {
            return parentCollider.bounds;
        }
        
        // 其次检查自身的 Collider2D
        Collider2D selfCollider = GetComponent<Collider2D>();
        if (selfCollider != null)
        {
            return selfCollider.bounds;
        }
        
        // 检查子物体的 Collider2D
        Collider2D childCollider = GetComponentInChildren<Collider2D>();
        if (childCollider != null)
        {
            return childCollider.bounds;
        }
        
        // 回退到 Sprite Bounds
        return GetBounds();
    }
    
    /// <summary>
    /// 获取树木的成长阶段索引（用于动态调整连通距离）
    /// 返回 0-5 的阶段索引，兼容新版 TreeController（原 V2）
    /// </summary>
    public int GetTreeGrowthStageIndex()
    {
        TreeController treeController = GetComponent<TreeController>();
        if (treeController != null)
        {
            return treeController.GetCurrentStageIndex();
        }
        return 5; // 默认最大阶段
    }
    
    /// <summary>
    /// 获取树木的成长阶段（旧版兼容，映射到 GrowthStage 枚举）
    /// </summary>
    public GrowthStage GetTreeGrowthStage()
    {
        int stageIndex = GetTreeGrowthStageIndex();
        // 映射：0 = Sapling, 1-2 = Small, 3-5 = Large
        if (stageIndex == 0) return GrowthStage.Sapling;
        if (stageIndex <= 2) return GrowthStage.Small;
        return GrowthStage.Large;
    }
    
    /// <summary>
    /// 是否启用遮挡检测
    /// </summary>
    public bool CanBeOccluded => canBeOccluded;
    
    /// <summary>
    /// 动态设置是否可被遮挡（由TreeController等外部控制）
    /// </summary>
    public void SetCanBeOccluded(bool enabled)
    {
        if (canBeOccluded == enabled) return;
        
        bool wasEnabled = canBeOccluded;
        canBeOccluded = enabled;
        
        if (enabled && !wasEnabled)
        {
            // 启用：注册到管理器
            if (OcclusionManager.Instance != null)
            {
                OcclusionManager.Instance.RegisterOccluder(this);
            }
        }
        else if (!enabled && wasEnabled)
        {
            // 禁用：从管理器注销并恢复透明度
            if (OcclusionManager.Instance != null)
            {
                OcclusionManager.Instance.UnregisterOccluder(this);
            }
            SetOccluding(false);
        }
    }
    
    #region 像素采样精确检测
    
    /// <summary>
    /// 检查世界坐标点是否在 Sprite 的实际可见区域内（像素采样）
    /// </summary>
    /// <param name="worldPoint">世界坐标点</param>
    /// <returns>该点是否在可见像素区域内</returns>
    public bool ContainsPointPrecise(Vector2 worldPoint)
    {
        if (!usePixelSampling || mainRenderer == null)
        {
            // 回退到 Bounds 检测
            return GetBounds().Contains(worldPoint);
        }
        
        // 1. 快速预筛选：先用 Bounds 检测
        Bounds bounds = GetBounds();
        if (!bounds.Contains(worldPoint))
        {
            return false;
        }
        
        // 2. 获取 Sprite 和纹理
        Sprite sprite = mainRenderer.sprite;
        if (sprite == null) return false;
        
        // 检查缓存是否需要更新
        if (sprite != _cachedSprite)
        {
            _cachedSprite = sprite;
            _cachedTexture = sprite.texture;
            
            // 检查纹理是否可读
            if (_cachedTexture != null)
            {
                try
                {
                    // 尝试读取一个像素来测试是否可读
                    _cachedTexture.GetPixel(0, 0);
                    _textureReadable = true;
                }
                catch (UnityException)
                {
                    _textureReadable = false;
                    Debug.LogWarning($"[OcclusionTransparency] {gameObject.name} 的纹理不可读，已回退到 Bounds 检测。请在纹理导入设置中启用 Read/Write Enabled。");
                }
            }
        }
        
        // 纹理不可读，回退到 Bounds 检测
        if (_cachedTexture == null || !_textureReadable)
        {
            return true; // Bounds 已经检测通过
        }
        
        // 3. 世界坐标 → 本地坐标
        Vector2 localPoint = mainRenderer.transform.InverseTransformPoint(worldPoint);
        
        // 4. 本地坐标 → 纹理像素坐标
        Rect spriteRect = sprite.rect;
        Vector2 pivot = sprite.pivot;
        float pixelsPerUnit = sprite.pixelsPerUnit;
        
        // 计算像素坐标（相对于 Sprite 的 pivot）
        float pixelX = localPoint.x * pixelsPerUnit + pivot.x;
        float pixelY = localPoint.y * pixelsPerUnit + pivot.y;
        
        // 转换为纹理坐标
        int texX = Mathf.RoundToInt(spriteRect.x + pixelX);
        int texY = Mathf.RoundToInt(spriteRect.y + pixelY);
        
        // 5. 边界检查
        if (texX < spriteRect.x || texX >= spriteRect.x + spriteRect.width ||
            texY < spriteRect.y || texY >= spriteRect.y + spriteRect.height)
        {
            return false;
        }
        
        // 6. 采样像素
        try
        {
            Color pixel = _cachedTexture.GetPixel(texX, texY);
            return pixel.a > alphaThreshold;
        }
        catch
        {
            // 读取失败，回退
            return true;
        }
    }
    
    /// <summary>
    /// 多点采样计算遮挡占比（更精确）
    /// </summary>
    /// <param name="playerBounds">玩家的 Bounds</param>
    /// <returns>被遮挡的点占总采样点的比例（0-1）</returns>
    public float CalculateOcclusionRatioPrecise(Bounds playerBounds)
    {
        if (!usePixelSampling || mainRenderer == null)
        {
            // 回退到 Bounds 重叠计算
            return CalculateBoundsOverlapRatio(playerBounds);
        }
        
        // 根据采样点数量生成采样点
        Vector2[] samplePoints = GenerateSamplePoints(playerBounds, samplePointCount);
        
        int hitCount = 0;
        foreach (var point in samplePoints)
        {
            if (ContainsPointPrecise(point))
            {
                hitCount++;
            }
        }
        
        return (float)hitCount / samplePoints.Length;
    }
    
    /// <summary>
    /// 生成采样点（中心 + 边缘点）
    /// </summary>
    private Vector2[] GenerateSamplePoints(Bounds bounds, int count)
    {
        Vector2 center = bounds.center;
        Vector2 min = bounds.min;
        Vector2 max = bounds.max;
        
        switch (count)
        {
            case 1:
                return new Vector2[] { center };
            case 5:
                // 中心 + 四角
                return new Vector2[]
                {
                    center,
                    new Vector2(min.x, min.y),
                    new Vector2(max.x, min.y),
                    new Vector2(min.x, max.y),
                    new Vector2(max.x, max.y)
                };
            case 9:
                // 中心 + 四角 + 四边中点
                return new Vector2[]
                {
                    center,
                    new Vector2(min.x, min.y),
                    new Vector2(max.x, min.y),
                    new Vector2(min.x, max.y),
                    new Vector2(max.x, max.y),
                    new Vector2(center.x, min.y),
                    new Vector2(center.x, max.y),
                    new Vector2(min.x, center.y),
                    new Vector2(max.x, center.y)
                };
            default:
                // 默认 5 点
                return new Vector2[]
                {
                    center,
                    new Vector2(min.x, min.y),
                    new Vector2(max.x, min.y),
                    new Vector2(min.x, max.y),
                    new Vector2(max.x, max.y)
                };
        }
    }
    
    /// <summary>
    /// 计算 Bounds 重叠占比（回退方案）
    /// </summary>
    private float CalculateBoundsOverlapRatio(Bounds playerBounds)
    {
        Bounds occluderBounds = GetBounds();
        
        // 计算重叠区域
        float overlapMinX = Mathf.Max(playerBounds.min.x, occluderBounds.min.x);
        float overlapMaxX = Mathf.Min(playerBounds.max.x, occluderBounds.max.x);
        float overlapMinY = Mathf.Max(playerBounds.min.y, occluderBounds.min.y);
        float overlapMaxY = Mathf.Min(playerBounds.max.y, occluderBounds.max.y);
        
        float overlapWidth = overlapMaxX - overlapMinX;
        float overlapHeight = overlapMaxY - overlapMinY;
        
        // 没有重叠
        if (overlapWidth <= 0 || overlapHeight <= 0)
        {
            return 0f;
        }
        
        // 计算重叠面积
        float overlapArea = overlapWidth * overlapHeight;
        
        // 计算玩家面积
        float playerArea = playerBounds.size.x * playerBounds.size.y;
        
        // 避免除以零
        if (playerArea <= 0)
        {
            return 0f;
        }
        
        return overlapArea / playerArea;
    }
    
    /// <summary>
    /// 是否启用像素采样
    /// </summary>
    public bool UsePixelSampling => usePixelSampling;
    
    /// <summary>
    /// 纹理是否可读
    /// </summary>
    public bool IsTextureReadable => _textureReadable;
    
    #endregion
}
