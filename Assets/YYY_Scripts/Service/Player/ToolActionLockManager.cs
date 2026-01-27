using UnityEngine;

/// <summary>
/// 工具动作锁定管理器
/// 负责：
/// 1. 工具使用期间锁定 toolbar 和手持物品
/// 2. 缓存 toolbar 切换输入（快捷键/鼠标/滚轮）
/// 3. 缓存移动方向输入
/// 4. 支持长按连续使用工具
/// </summary>
public class ToolActionLockManager : MonoBehaviour
{
    #region 单例
    private static ToolActionLockManager _instance;
    public static ToolActionLockManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ToolActionLockManager>();
            }
            return _instance;
        }
    }
    #endregion

    #region 锁定状态
    /// <summary>是否正在执行工具动作（锁定状态）</summary>
    public bool IsLocked { get; private set; } = false;
    
    /// <summary>是否正在长按（连续使用模式）</summary>
    public bool IsContinuousMode { get; private set; } = false;
    #endregion

    #region 输入缓冲
    /// <summary>缓存的 hotbar 索引（-1 表示无缓存）</summary>
    private int _cachedHotbarIndex = -1;
    
    /// <summary>缓存的移动方向（null 表示无缓存）</summary>
    private Vector2? _cachedDirection = null;
    
    /// <summary>是否有缓存的 hotbar 输入</summary>
    public bool HasCachedHotbarInput => _cachedHotbarIndex >= 0;
    
    /// <summary>是否有缓存的方向输入</summary>
    public bool HasCachedDirection => _cachedDirection.HasValue;
    #endregion

    #region 调试
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = false;
    #endregion

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    #region 锁定控制
    /// <summary>
    /// 开始工具动作，进入锁定状态
    /// </summary>
    public void BeginAction()
    {
        IsLocked = true;
        IsContinuousMode = false;
        
        if (enableDebugLog)
            Debug.Log($"<color=cyan>[ToolLock] 开始动作，进入锁定状态</color>");
    }

    /// <summary>
    /// 结束工具动作，退出锁定状态
    /// </summary>
    /// <param name="continuingAction">是否继续下一个动作（长按模式）</param>
    public void EndAction(bool continuingAction = false)
    {
        if (continuingAction)
        {
            // 长按模式：清空缓存，保持锁定
            IsContinuousMode = true;
            ClearHotbarCache();
            // 方向缓存保留，在下一个动作开始前应用
            
            if (enableDebugLog)
                Debug.Log($"<color=yellow>[ToolLock] 连续动作模式，清空 hotbar 缓存</color>");
        }
        else
        {
            // 正常结束：解锁
            IsLocked = false;
            IsContinuousMode = false;
            
            if (enableDebugLog)
                Debug.Log($"<color=green>[ToolLock] 动作结束，解除锁定</color>");
        }
    }

    /// <summary>
    /// 强制解锁（用于异常情况）
    /// </summary>
    public void ForceUnlock()
    {
        IsLocked = false;
        IsContinuousMode = false;
        ClearAllCache();
        
        if (enableDebugLog)
            Debug.Log($"<color=red>[ToolLock] 强制解锁</color>");
    }
    #endregion

    #region Hotbar 缓存
    /// <summary>
    /// 缓存 hotbar 切换输入（覆盖式，只保留最后一个）
    /// </summary>
    public void CacheHotbarInput(int index)
    {
        if (!IsLocked) return; // 未锁定时不需要缓存
        
        _cachedHotbarIndex = index;
        
        if (enableDebugLog)
            Debug.Log($"<color=orange>[ToolLock] 缓存 hotbar 输入: {index}</color>");
    }

    /// <summary>
    /// 读取并清空 hotbar 缓存
    /// </summary>
    /// <returns>缓存的索引，-1 表示无缓存</returns>
    public int ConsumeHotbarCache()
    {
        int cached = _cachedHotbarIndex;
        _cachedHotbarIndex = -1;
        
        if (cached >= 0 && enableDebugLog)
            Debug.Log($"<color=lime>[ToolLock] 消费 hotbar 缓存: {cached}</color>");
        
        return cached;
    }

    /// <summary>
    /// 清空 hotbar 缓存（不读取）
    /// </summary>
    public void ClearHotbarCache()
    {
        _cachedHotbarIndex = -1;
    }
    #endregion

    #region 方向缓存
    /// <summary>
    /// 缓存移动方向输入（覆盖式，只保留最后一个）
    /// </summary>
    public void CacheDirection(Vector2 direction)
    {
        if (!IsLocked) return; // 未锁定时不需要缓存
        
        // 只缓存有效的方向输入
        if (direction.sqrMagnitude > 0.01f)
        {
            _cachedDirection = direction.normalized;
            
            if (enableDebugLog)
                Debug.Log($"<color=orange>[ToolLock] 缓存方向输入: {direction}</color>");
        }
    }

    /// <summary>
    /// 读取并清空方向缓存
    /// </summary>
    /// <returns>缓存的方向，null 表示无缓存</returns>
    public Vector2? ConsumeDirectionCache()
    {
        Vector2? cached = _cachedDirection;
        _cachedDirection = null;
        
        if (cached.HasValue && enableDebugLog)
            Debug.Log($"<color=lime>[ToolLock] 消费方向缓存: {cached.Value}</color>");
        
        return cached;
    }

    /// <summary>
    /// 清空方向缓存（不读取）
    /// </summary>
    public void ClearDirectionCache()
    {
        _cachedDirection = null;
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public void ClearAllCache()
    {
        _cachedHotbarIndex = -1;
        _cachedDirection = null;
    }
    #endregion
}
