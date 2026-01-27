using UnityEngine;
using System;

/// <summary>
/// 放置导航器
/// 负责计算导航目标点和检测玩家是否到达预览格子边界
/// 
/// 核心逻辑：
/// 1. 计算玩家当前位置到预览格子边界的最近点作为导航目标
/// 2. 持续检测玩家 Collider 边界与预览格子边界的距离
/// 3. 当距离小于触发距离且不重叠时，触发放置
/// </summary>
public class PlacementNavigator : MonoBehaviour
{
    #region 事件
    
    /// <summary>到达目标事件</summary>
    public event Action OnReachedTarget;
    
    /// <summary>导航取消事件</summary>
    public event Action OnNavigationCancelled;
    
    #endregion
    
    #region 序列化字段
    
    [Header("━━━━ 距离配置 ━━━━")]
    [Tooltip("触发放置的距离阈值（玩家中心到预览格子边缘的距离）")]
    [Range(0.1f, 2f)]
    [SerializeField] private float triggerDistance = 0.8f;  // 增大到 0.8f，确保覆盖 PlayerAutoNavigator 的 stopDistance
    
    [Tooltip("导航超时时间（秒）")]
    [SerializeField] private float navigationTimeout = 10f;
    
    [Header("━━━━ 调试 ━━━━")]
    [SerializeField] private bool showDebugInfo = false;  // 验收通过，关闭调试
    
    #endregion
    
    #region 私有字段
    
    private Transform playerTransform;
    private Collider2D playerCollider;
    private PlayerAutoNavigator autoNavigator;
    
    private bool isNavigating = false;
    private Vector3 targetPosition;
    private Bounds targetBounds;
    private float navigationStartTime;
    
    #endregion
    
    #region 属性
    
    /// <summary>是否正在导航</summary>
    public bool IsNavigating => isNavigating;
    
    /// <summary>目标位置</summary>
    public Vector3 TargetPosition => targetPosition;
    
    #endregion
    
    #region Unity 生命周期
    
    private void Awake()
    {
        // 查找 PlayerAutoNavigator
        autoNavigator = FindFirstObjectByType<PlayerAutoNavigator>();
    }
    
    private void Update()
    {
        if (!isNavigating) return;
        
        // 检查超时
        if (Time.time - navigationStartTime > navigationTimeout)
        {
            if (showDebugInfo)
                Debug.Log($"<color=red>[PlacementNavigator] 导航超时</color>");
            CancelNavigation();
            return;
        }
        
        // 检查是否到达
        if (CheckReached())
        {
            if (showDebugInfo)
                Debug.Log($"<color=green>[PlacementNavigator] 到达目标，触发放置</color>");
            
            isNavigating = false;
            OnReachedTarget?.Invoke();
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 初始化导航器
    /// </summary>
    public void Initialize(Transform player)
    {
        playerTransform = player;
        if (player != null)
        {
            playerCollider = player.GetComponent<Collider2D>();
        }
        
        if (autoNavigator == null)
        {
            autoNavigator = FindFirstObjectByType<PlayerAutoNavigator>();
        }
    }
    
    /// <summary>
    /// 计算导航目标点
    /// 目标点 = 预览格子边缘到玩家中心的最近点（ClosestPoint 统一方案）
    /// </summary>
    /// <param name="playerBounds">玩家 Collider 的 Bounds</param>
    /// <param name="previewBounds">预览格子的 Bounds</param>
    /// <returns>导航目标点</returns>
    public Vector3 CalculateNavigationTarget(Bounds playerBounds, Bounds previewBounds)
    {
        Vector3 playerCenter = playerBounds.center;
        
        // 直接使用预览格子边缘的最近点作为导航目标
        // 这与 CheckReached() 的距离计算方式一致
        Vector3 closestPoint = previewBounds.ClosestPoint(playerCenter);
        
        // 如果玩家中心在预览 Bounds 内部，ClosestPoint 会返回玩家中心
        // 这种情况下需要找到边界上的最近点
        if (previewBounds.Contains(playerCenter))
        {
            // 计算到各边界的距离，选择最近的边界
            float distToLeft = playerCenter.x - previewBounds.min.x;
            float distToRight = previewBounds.max.x - playerCenter.x;
            float distToBottom = playerCenter.y - previewBounds.min.y;
            float distToTop = previewBounds.max.y - playerCenter.y;
            
            float minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
            
            // 返回最近边界上的点（不向外偏移，因为已经在内部）
            if (minDist == distToLeft)
                closestPoint = new Vector3(previewBounds.min.x, playerCenter.y, 0);
            else if (minDist == distToRight)
                closestPoint = new Vector3(previewBounds.max.x, playerCenter.y, 0);
            else if (minDist == distToBottom)
                closestPoint = new Vector3(playerCenter.x, previewBounds.min.y, 0);
            else
                closestPoint = new Vector3(playerCenter.x, previewBounds.max.y, 0);
        }
        // 不再向外偏移，直接返回边缘最近点
        // 到达判断使用 triggerDistance 阈值，导航目标就是边缘点
        
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlacementNavigator] 导航目标点: {closestPoint} (ClosestPoint 统一方案)</color>");
        
        return closestPoint;
    }
    
    /// <summary>
    /// 开始导航到目标位置
    /// </summary>
    /// <param name="target">导航目标点</param>
    /// <param name="previewBounds">预览格子的 Bounds（用于到达检测）</param>
    public void StartNavigation(Vector3 target, Bounds previewBounds)
    {
        if (autoNavigator == null)
        {
            Debug.LogWarning("[PlacementNavigator] 未找到 PlayerAutoNavigator，无法导航");
            return;
        }
        
        targetPosition = target;
        targetBounds = previewBounds;
        isNavigating = true;
        navigationStartTime = Time.time;
        
        // 启动自动导航
        autoNavigator.SetDestination(target);
        
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlacementNavigator] 开始导航到: {target}</color>");
    }
    
    /// <summary>
    /// 取消导航
    /// </summary>
    public void CancelNavigation()
    {
        if (!isNavigating) return;
        
        isNavigating = false;
        
        // 取消自动导航
        if (autoNavigator != null)
        {
            autoNavigator.Cancel();
        }
        
        OnNavigationCancelled?.Invoke();
        
        if (showDebugInfo)
            Debug.Log($"<color=yellow>[PlacementNavigator] 导航已取消</color>");
    }
    
    /// <summary>
    /// 检查是否到达目标
    /// 条件：玩家中心到预览格子边缘的距离小于触发距离（ClosestPoint 统一方案）
    /// </summary>
    public bool CheckReached()
    {
        if (playerCollider == null) return false;
        
        Vector3 playerCenter = playerCollider.bounds.center;
        
        // 使用 ClosestPoint 计算玩家中心到预览格子边缘的距离
        float distance = CalculateDistanceToPreview(playerCenter, targetBounds);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[PlacementNavigator] CheckReached: 距离={distance:F3}, 触发距离={triggerDistance} (ClosestPoint)</color>");
        }
        
        // 到达条件：玩家中心到预览格子边缘的距离小于等于触发距离
        return distance <= triggerDistance;
    }
    
    /// <summary>
    /// 检查是否到达目标（使用指定的 Bounds）
    /// </summary>
    public bool CheckReached(Bounds playerBounds, Bounds previewBounds)
    {
        Vector3 playerCenter = playerBounds.center;
        float distance = CalculateDistanceToPreview(playerCenter, previewBounds);
        
        return distance <= triggerDistance;
    }
    
    /// <summary>
    /// 检查玩家是否已经在目标附近（无需导航）
    /// 使用 ClosestPoint 统一方案
    /// </summary>
    public bool IsAlreadyNearTarget(Bounds playerBounds, Bounds previewBounds)
    {
        Vector3 playerCenter = playerBounds.center;
        float distance = CalculateDistanceToPreview(playerCenter, previewBounds);
        
        bool result = distance <= triggerDistance;
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[PlacementNavigator] IsAlreadyNearTarget: distance={distance:F3}, triggerDistance={triggerDistance}, result={result} (ClosestPoint)</color>");
        }
        
        return result;
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 计算玩家中心到预览格子边缘的距离（ClosestPoint 统一方案）
    /// </summary>
    private float CalculateDistanceToPreview(Vector3 playerCenter, Bounds previewBounds)
    {
        Vector3 closestPoint = previewBounds.ClosestPoint(playerCenter);
        return Vector3.Distance(playerCenter, closestPoint);
    }
    
    /// <summary>
    /// 计算两个 Bounds 之间的最小距离（保留用于兼容，但不再使用）
    /// </summary>
    private float CalculateBoundsDistance(Bounds a, Bounds b)
    {
        // 如果重叠，距离为 0
        if (a.Intersects(b))
            return 0f;
        
        // 计算各轴上的距离
        float dx = 0f;
        float dy = 0f;
        
        // X 轴距离
        if (a.max.x < b.min.x)
            dx = b.min.x - a.max.x;
        else if (b.max.x < a.min.x)
            dx = a.min.x - b.max.x;
        
        // Y 轴距离
        if (a.max.y < b.min.y)
            dy = b.min.y - a.max.y;
        else if (b.max.y < a.min.y)
            dy = a.min.y - b.max.y;
        
        // 返回欧几里得距离
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
    
    #endregion
    
    #region 调试
    
    private void OnDrawGizmos()
    {
        if (!showDebugInfo || !isNavigating) return;
        
        // 绘制目标位置
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPosition, 0.2f);
        
        // 绘制目标 Bounds
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(targetBounds.center, targetBounds.size);
        
        // 绘制触发距离
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Vector3 expandedSize = targetBounds.size + Vector3.one * triggerDistance * 2;
        Gizmos.DrawWireCube(targetBounds.center, expandedSize);
    }
    
    #endregion
}
