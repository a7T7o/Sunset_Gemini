using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 自动导航（右键点击）- v5.3 ClosestPoint 统一版
/// 
/// 核心改进：
/// 1. 斜向移动时固定朝向为左/右，避免摇头
/// 2. 路径平滑处理，减少崎岖路线
/// 3. 详细的卡顿诊断输出
/// 4. 视线优化跳过中间路径点
/// 5. 🔥 v4.0：使用 ClosestPoint 计算距离，与 GameInputManager 统一
/// </summary>
public class PlayerAutoNavigator : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Transform player;
    [SerializeField] private NavGrid2D navGrid;

    [Header("停止距离")]
    [SerializeField, Range(0.05f, 1f)] private float stopDistance = 0.2f;
    [SerializeField, Range(0.05f, 0.5f)] private float waypointTolerance = 0.15f;

    [Header("目标点去抖")]
    [SerializeField, Range(0.1f, 1f)] private float destinationChangeThreshold = 0.3f;

    [Header("障碍物检测")]
    [SerializeField] private LayerMask losObstacleMask;
    [SerializeField] private string[] losObstacleTags = new string[0];

    [Header("路径优化")]
    [Tooltip("启用视线优化（跳过可直达的路径点）")]
    [SerializeField] private bool enableLineOfSightOptimization = true;
    [Tooltip("视线检测安全边距")]
    [SerializeField, Range(0.1f, 0.5f)] private float losSafetyMargin = 0.2f;

    [Header("调试")]
    [SerializeField] private bool showPathGizmos = true;
    [SerializeField] private bool enableDetailedDebug = false;

    // 私有字段
    private Collider2D playerCollider;
    private float playerRadius;
    private bool active;
    private Vector3 targetPoint;
    private Transform targetTransform;
    private float followStopRadius = 0.6f;
    private bool runWhileNavigating;
    private readonly List<Vector2> path = new List<Vector2>();
    private int pathIndex;

    // 卡顿检测
    private Vector2 lastCheckPosition;
    private float lastCheckTime;
    private int stuckRetryCount;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_CHECK_INTERVAL = 0.3f;
    private const int MAX_STUCK_RETRIES = 3;

    // 到达回调
    private System.Action _onArrivedCallback;
    
    // 🔥 P0-1：navToken 机制 - 隔离不同导航请求
    private int _navToken = 0;
    private int _currentToken = 0;

    // 调试信息
    private List<string> debugLogs = new List<string>();

    public bool IsActive => active;

    void Awake()
    {
        if (player == null) player = transform;
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (navGrid == null) navGrid = FindFirstObjectByType<NavGrid2D>();
        
        playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null) playerCollider = GetComponentInChildren<Collider2D>();
        
        if (playerCollider != null)
        {
            playerRadius = Mathf.Max(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y);
            if (navGrid != null) navGrid.SetAgentRadius(playerRadius);
        }
        else
        {
            playerRadius = 0.25f;
        }
    }

    void Update()
    {
        if (!active || movement == null || player == null) return;

        // 🔥 修复：跟随模式下，每帧重新计算最近接近点（目标可能移动，玩家位置也在变）
        if (targetTransform != null)
        {
            targetPoint = CalculateClosestApproachPoint(targetTransform);
        }

        UpdateSprintState();

        if (CheckAndHandleStuck()) return;

        ExecuteNavigation();
    }

    public void SetDestination(Vector3 worldPos)
    {
        if (active && Vector3.Distance(worldPos, targetPoint) < destinationChangeThreshold) return;

        targetTransform = null;
        targetPoint = worldPos;
        active = true;

        if (SprintStateManager.Instance != null)
            runWhileNavigating = SprintStateManager.Instance.ShouldNavigationSprint();

        BuildPath();
        ResetStuckDetection();
    }

    public void FollowTarget(Transform t, float stopRadius)
    {
        FollowTarget(t, stopRadius, null);
    }

    /// <summary>
    /// 跟随目标并在到达后执行回调
    /// </summary>
    /// <param name="t">目标 Transform</param>
    /// <param name="stopRadius">停止距离</param>
    /// <param name="onArrived">到达后的回调</param>
    public void FollowTarget(Transform t, float stopRadius, System.Action onArrived)
    {
        // 🔥 P0-1：先强制取消旧导航（不触发回调）
        ForceCancel();
        
        // 🔥 P0-1：递增 token，隔离新导航请求
        _navToken++;
        _currentToken = _navToken;
        
        // 清除缓存
        _cachedTargetTransform = null;
        _cachedTargetCollider = null;
        
        targetTransform = t;
        
        // 🔥 修复：计算玩家到目标的最近接近点，而不是目标中心
        targetPoint = CalculateClosestApproachPoint(t);
        
        // 🔥 修复：根据目标 Collider 大小动态调整停止半径，确保足够近
        followStopRadius = CalculateOptimalStopRadius(t, stopRadius);
        
        _onArrivedCallback = onArrived;
        active = true;

        if (SprintStateManager.Instance != null)
            runWhileNavigating = SprintStateManager.Instance.ShouldNavigationSprint();

        // 🔥 v4.0：关键日志 - 使用 ClosestPoint 计算距离
        Vector2 playerStart = GetPlayerPosition();
        Vector2 targetClosest = GetTargetAnchorPoint(t);
        float distToTarget = Vector2.Distance(playerStart, targetClosest);
        
        var collider = _cachedTargetCollider;
        if (collider != null)
        {
            Bounds bounds = collider.bounds;
            Debug.Log($"<color=green>[Nav] 开始导航: 目标={t.name}</color>\n" +
                      $"  玩家起点: {playerStart}\n" +
                      $"  目标最近点(ClosestPoint): {targetClosest}, Collider大小: {(Vector2)bounds.size}\n" +
                      $"  当前距离: {distToTarget:F2}, 停止半径: {followStopRadius:F2}");
        }
        else
        {
            Debug.Log($"<color=green>[Nav] 开始导航: 目标={t.name}（无Collider）</color>\n" +
                      $"  玩家起点: {playerStart}\n" +
                      $"  目标位置: {(Vector2)t.position}\n" +
                      $"  停止半径: {followStopRadius:F2}");
        }

        BuildPath();
        ResetStuckDetection();
    }

    /// <summary>
    /// 取消导航（不触发回调）- 用于卡顿、路径失败等
    /// 🔥 P0-1：Cancel 不再执行回调
    /// </summary>
    public void Cancel()
    {
        ResetNavigationState();
        // 🔥 P0-1：不执行回调
    }
    
    /// <summary>
    /// 强制取消导航（不触发回调）- 用于切换目标、玩家移动打断
    /// 🔥 P0-1：使旧 token 失效
    /// </summary>
    public void ForceCancel()
    {
        _navToken++;  // 使旧 token 失效
        ResetNavigationState();
    }
    
    /// <summary>
    /// 🔥 v4.0：正常到达时调用（唯一允许执行回调的入口）
    /// </summary>
    private void CompleteArrival()
    {
        // 🔥 v4.0：关键日志 - 使用 ClosestPoint 计算距离
        Vector2 playerFinal = GetPlayerPosition();
        string targetName = targetTransform != null ? targetTransform.name : "无目标";
        
        // 计算到目标最近点的距离（与 GameInputManager 一致）
        float distToTarget = 0f;
        if (targetTransform != null)
        {
            Vector2 targetClosest = GetTargetAnchorPoint(targetTransform);
            distToTarget = Vector2.Distance(playerFinal, targetClosest);
        }
        
        Debug.Log($"<color=cyan>[Nav] 导航完成: 目标={targetName}</color>\n" +
                  $"  玩家最终位置: {playerFinal}\n" +
                  $"  到目标最近点距离: {distToTarget:F2}\n" +
                  $"  停止半径: {followStopRadius:F2}");
        
        // 只有 token 匹配才执行回调
        if (_currentToken == _navToken && _onArrivedCallback != null)
        {
            var callback = _onArrivedCallback;
            ResetNavigationState();
            callback.Invoke();
        }
        else
        {
            ResetNavigationState();
        }
    }
    
    /// <summary>
    /// 🔥 P0-1：重置导航状态（公共逻辑提取）
    /// </summary>
    private void ResetNavigationState()
    {
        active = false;
        targetTransform = null;
        _cachedTargetTransform = null;
        _cachedTargetCollider = null;
        path.Clear();
        pathIndex = 0;
        stuckRetryCount = 0;
        runWhileNavigating = false;
        _onArrivedCallback = null;
        if (movement != null) movement.SetMovementInput(Vector2.zero, false);
    }

    public void ToggleRunWhileNavigating() { }

    public void SetRunWhileNavigating(bool run) { runWhileNavigating = run; }

    private void ExecuteNavigation()
    {
        Vector2 playerPos = GetPlayerPosition();

        if (path.Count == 0)
        {
            BuildPath();
            if (path.Count == 0) 
            { 
                // 🔥 无路径时，检查是否已经足够近可以交互
                if (targetTransform != null && IsCloseEnoughToInteract(playerPos))
                {
                    CompleteArrival();
                }
                else
                {
                    // 无法到达，取消导航
                    Cancel();
                }
                return; 
            }
        }

        if (pathIndex >= path.Count) { Cancel(); return; }

        // 视线优化：尝试跳过中间路径点
        if (enableLineOfSightOptimization)
        {
            TrySkipWaypoints(playerPos);
        }

        Vector2 waypoint = path[pathIndex];
        Vector2 toWaypoint = waypoint - playerPos;
        float distance = toWaypoint.magnitude;
        
        bool isLastWaypoint = (pathIndex == path.Count - 1);
        
        // 🔥 核心修复：停止条件基于是否足够近可以交互
        if (isLastWaypoint && targetTransform != null)
        {
            // 检查是否已经足够近可以交互
            if (IsCloseEnoughToInteract(playerPos))
            {
                CompleteArrival();
                return;
            }
            
            // 检查是否已经到达路径终点（可走的最近点）
            if (distance <= waypointTolerance)
            {
                // 已经到达路径终点，这是 NavGrid 能到达的最近点
                // 直接完成导航，让交互系统判断是否在交互距离内
                CompleteArrival();
                return;
            }
        }
        else if (!isLastWaypoint)
        {
            // 非最后一个路径点：正常检查
            if (distance <= waypointTolerance)
            {
                pathIndex++;
                return;
            }
        }
        else
        {
            // 最后一个路径点但没有 targetTransform（普通导航）
            if (distance <= GetFinalStopDistance())
            {
                CompleteArrival();
                return;
            }
        }

        // 计算移动方向
        Vector2 moveDir = toWaypoint.normalized;
        
        // 碰撞调整（只在必要时）
        moveDir = AdjustDirectionByColliders(playerPos, moveDir);
        
        // 🔥 关键：斜向移动时固定朝向为左或右
        // 这样可以避免角色摇头
        Vector2 facingDir = GetFacingDirection(moveDir);
        
        movement.SetMovementInput(moveDir, runWhileNavigating, facingDir);
    }

    /// <summary>
    /// 🔥 获取朝向方向 - 斜向移动时固定为左或右
    /// </summary>
    private Vector2 GetFacingDirection(Vector2 moveDir)
    {
        // 如果主要是水平移动（|x| > |y|），使用移动方向
        if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
        {
            return moveDir;
        }
        
        // 如果主要是垂直移动，也使用移动方向
        if (Mathf.Abs(moveDir.y) > Mathf.Abs(moveDir.x) * 1.5f)
        {
            return moveDir;
        }
        
        // 斜向移动时，固定朝向为左或右
        // 根据 X 分量决定朝向
        if (moveDir.x > 0)
        {
            return Vector2.right; // 朝右
        }
        else if (moveDir.x < 0)
        {
            return Vector2.left; // 朝左
        }
        
        return moveDir;
    }

    /// <summary>
    /// 视线优化：尝试跳过可直达的中间路径点
    /// </summary>
    private void TrySkipWaypoints(Vector2 playerPos)
    {
        // 从当前路径点往后找，看能否直接到达更远的点
        for (int i = path.Count - 1; i > pathIndex; i--)
        {
            if (HasLineOfSight(playerPos, path[i]))
            {
                if (enableDetailedDebug && i > pathIndex + 1)
                {
                    Debug.Log($"<color=cyan>[Nav] 视线优化：跳过 {i - pathIndex - 1} 个路径点</color>");
                }
                pathIndex = i;
                break;
            }
        }
    }

    /// <summary>
    /// 视线检测：检查两点之间是否有障碍物
    /// 🔥 Unity 6 优化：使用 CircleCast 进行连续体积检测（考虑玩家半径）
    /// 🔥 策略：宁可误杀，不可放过 - 默认所有非 Trigger 的碰撞体都是障碍物
    /// 🔥 集成 NavGrid 检测，确保路径上所有点都是可走的
    /// </summary>
    private bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        float distance = direction.magnitude;
        if (distance < 0.1f) return true;
        
        // 🔥 第一步：检查 NavGrid 可走性（关键！）
        // 这可以检测到水域、悬崖等没有物理碰撞体但不可走的区域
        if (navGrid != null)
        {
            int sampleCount = Mathf.Max(5, Mathf.CeilToInt(distance / 0.3f));
            for (int i = 1; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                Vector2 samplePoint = Vector2.Lerp(from, to, t);
                
                if (!navGrid.IsWalkable(samplePoint))
                {
                    _lastLOSBlocked = true;
                    return false;
                }
            }
        }
        
        // 🔥 第二步：使用 CircleCast 进行连续体积检测（考虑玩家半径）
        // CircleCast 会以一个"带半径的圆"扫过去，比 Linecast 更准确
        float checkRadius = Mathf.Max(0.05f, (playerRadius + losSafetyMargin) * 0.8f);
        
        // 使用配置的 LayerMask，如果没配置则检测所有层
        int layerMask = losObstacleMask.value != 0 ? losObstacleMask.value : Physics2D.DefaultRaycastLayers;
        
        RaycastHit2D hit = Physics2D.CircleCast(
            from,                       // 起点
            checkRadius,                // 检测半径（考虑玩家体积）
            direction.normalized,       // 方向
            distance,                   // 距离
            layerMask                   // 层级遮罩
        );
        
        // 如果没有碰撞，视线通畅
        if (hit.collider == null)
        {
            _lastLOSBlocked = false;
            return true;
        }
        
        // 跳过玩家自己
        if (IsPlayerCollider(hit.collider))
        {
            _lastLOSBlocked = false;
            return true;
        }
        
        // 检查是否是障碍物
        bool isObstacle = IsObstacle(hit.collider);
        
        if (isObstacle)
        {
            if (enableDetailedDebug)
            {
                Debug.Log($"<color=red>[Nav] 视线被阻挡: {hit.collider.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, Tag: {hit.collider.tag}</color>");
            }
            _lastLOSHit = hit;
            _lastLOSBlocked = true;
            return false;
        }
        
        // 没有找到障碍物，视线通畅
        _lastLOSBlocked = false;
        return true;
    }
    
    // 🔥 调试可视化：记录最后一次视线检测的结果
    private RaycastHit2D _lastLOSHit;
    private bool _lastLOSBlocked;

    private void MoveDirectly(Vector2 playerPos)
    {
        Vector2 toTarget = (Vector2)targetPoint - playerPos;
        if (toTarget.magnitude <= GetFinalStopDistance()) { CompleteArrival(); return; }  // 🔥 P0-1：使用 CompleteArrival
        
        Vector2 moveDir = toTarget.normalized;
        Vector2 facingDir = GetFacingDirection(moveDir);
        movement.SetMovementInput(moveDir, runWhileNavigating, facingDir);
    }

    private void BuildPath()
    {
        path.Clear();
        pathIndex = 0;
        debugLogs.Clear();
        
        if (navGrid == null) 
        {
            AddDebugLog("NavGrid2D 未找到");
            return;
        }

        Vector2 start = GetPlayerPosition();
        Vector2 end = targetPoint;

        AddDebugLog($"开始寻路: 起点={start}, 终点={end}" + (targetTransform != null ? $", 目标={targetTransform.name}" : ""));

        // 检查起点是否可走
        if (!navGrid.IsWalkable(start))
        {
            AddDebugLog($"起点不可走，尝试查找最近可走点");
            
            if (!navGrid.TryFindNearestWalkable(start, out Vector2 validStart))
            {
                AddDebugLog("无法找到有效起点");
                LogFullDebugInfo("起点不可走且无法找到替代点");
                return;
            }
            AddDebugLog($"找到替代起点: {validStart}");
            start = validStart;
        }

        // 检查终点是否可走
        Vector2 actualEnd = end;
        if (!navGrid.IsWalkable(end))
        {
            AddDebugLog($"终点不可走，尝试查找最近可走点");
            
            if (navGrid.TryFindNearestWalkable(end, out Vector2 nearEnd))
            {
                AddDebugLog($"找到替代终点: {nearEnd}");
                actualEnd = nearEnd;
            }
            else
            {
                AddDebugLog("无法找到有效终点");
            }
        }

        // 尝试寻路
        if (!navGrid.TryFindPath(start, actualEnd, path))
        {
            AddDebugLog($"A* 寻路失败");
            LogFullDebugInfo("寻路失败");
            return;
        }

        AddDebugLog($"寻路成功: {path.Count} 个路径点");

        // 路径平滑处理
        SmoothPath();
        
        // 清理身后路径点
        CleanupPathBehindPlayer();
        
        if (enableDetailedDebug)
        {
            Debug.Log($"<color=green>[Nav] 路径构建成功：{path.Count} 个路径点</color>");
        }
    }

    /// <summary>
    /// 路径平滑处理 - 移除不必要的中间点
    /// </summary>
    private void SmoothPath()
    {
        if (path.Count < 3) return;
        
        List<Vector2> smoothed = new List<Vector2>();
        smoothed.Add(path[0]);
        
        int current = 0;
        while (current < path.Count - 1)
        {
            // 从当前点往后找，看能直接到达哪个点
            int farthest = current + 1;
            for (int i = path.Count - 1; i > current + 1; i--)
            {
                if (HasLineOfSight(path[current], path[i]))
                {
                    farthest = i;
                    break;
                }
            }
            
            smoothed.Add(path[farthest]);
            current = farthest;
        }
        
        int removed = path.Count - smoothed.Count;
        if (removed > 0)
        {
            AddDebugLog($"路径平滑：移除 {removed} 个冗余点");
        }
        
        path.Clear();
        path.AddRange(smoothed);
    }

    private void CleanupPathBehindPlayer()
    {
        if (path.Count < 2) return;
        Vector2 playerPos = GetPlayerPosition();

        while (path.Count > 1)
        {
            Vector2 first = path[0];
            if (Vector2.Distance(playerPos, first) < waypointTolerance * 0.5f) { path.RemoveAt(0); continue; }

            Vector2 second = path[1];
            Vector2 pathDir = (second - first).normalized;
            Vector2 toFirst = (first - playerPos).normalized;
            if (Vector2.Dot(toFirst, pathDir) < -0.2f) { path.RemoveAt(0); continue; }
            break;
        }
    }

    private bool CheckAndHandleStuck()
    {
        if (Time.time - lastCheckTime < STUCK_CHECK_INTERVAL) return false;

        Vector2 currentPos = GetPlayerPosition();
        float movedDistance = Vector2.Distance(currentPos, lastCheckPosition);
        lastCheckPosition = currentPos;
        lastCheckTime = Time.time;

        if (movedDistance < STUCK_THRESHOLD)
        {
            stuckRetryCount++;
            
            AddDebugLog($"检测到卡顿 ({stuckRetryCount}/{MAX_STUCK_RETRIES})，移动距离={movedDistance:F3}m");
            
            if (stuckRetryCount >= MAX_STUCK_RETRIES)
            {
                LogFullDebugInfo($"卡顿 {stuckRetryCount} 次后取消导航");
                Debug.LogWarning($"<color=red>[Nav] 卡顿 {stuckRetryCount} 次，取消导航</color>");
                Cancel();
                return true;
            }
            
            Debug.Log($"<color=yellow>[Nav] 检测到卡顿（{stuckRetryCount}/{MAX_STUCK_RETRIES}），重建路径</color>");
            BuildPath();
            
            if (path.Count == 0) 
            { 
                LogFullDebugInfo("重建路径失败");
                Cancel(); 
                return true; 
            }
        }
        else
        {
            stuckRetryCount = 0;
        }
        return false;
    }

    private void ResetStuckDetection()
    {
        lastCheckPosition = GetPlayerPosition();
        lastCheckTime = Time.time;
        stuckRetryCount = 0;
        debugLogs.Clear();
    }

    private Vector2 AdjustDirectionByColliders(Vector2 pos, Vector2 desiredDir)
    {
        if (playerCollider == null) return desiredDir;

        // 多点前瞻采样
        float[] aheadDistances = runWhileNavigating 
            ? new float[] { 0.15f, 0.35f, 0.6f }
            : new float[] { 0.1f, 0.25f, 0.45f };
        
        float clearance = playerRadius + 0.05f;
        Vector2 totalRepulse = Vector2.zero;
        int obstacleCount = 0;
        
        foreach (float ahead in aheadDistances)
        {
            Vector2 probe = pos + desiredDir * ahead;
            var hits = Physics2D.OverlapCircleAll(probe, clearance);
            float weight = 1f / (ahead + 0.1f);
            
            foreach (var hit in hits)
            {
                if (IsPlayerCollider(hit) || !IsObstacle(hit)) continue;
                obstacleCount++;
                
                Vector2 closest = hit.ClosestPoint(probe);
                Vector2 away = probe - closest;
                float dist = away.magnitude;
                
                if (dist < 0.01f)
                {
                    away = probe - (Vector2)hit.bounds.center;
                    dist = away.magnitude;
                }
                
                if (dist > 0.01f)
                {
                    float repulseStrength = 1f / (dist * dist + 0.1f);
                    totalRepulse += away.normalized * repulseStrength * weight;
                }
            }
        }

        // 如果没有障碍物，直接返回期望方向
        if (obstacleCount == 0 || totalRepulse.sqrMagnitude < 0.0001f) return desiredDir;

        // 计算调整后的方向
        Vector2 adjusted = (desiredDir + totalRepulse * 0.6f).normalized;
        
        // 限制最大偏转角度为 45 度
        float angle = Vector2.SignedAngle(desiredDir, adjusted);
        if (Mathf.Abs(angle) > 45f)
        {
            adjusted = RotateVector(desiredDir, Mathf.Sign(angle) * 45f);
        }

        return adjusted.sqrMagnitude > 0.001f ? adjusted : desiredDir;
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad) * v.x - Mathf.Sin(rad) * v.y, Mathf.Sin(rad) * v.x + Mathf.Cos(rad) * v.y);
    }

    private Vector2 GetPlayerPosition()
    {
        return playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)player.position;
    }

    private float GetFinalStopDistance()
    {
        return targetTransform != null ? followStopRadius : stopDistance;
    }

    private void UpdateSprintState()
    {
        if (SprintStateManager.Instance != null)
        {
            SprintStateManager.Instance.OnMovementInput(true);
            runWhileNavigating = SprintStateManager.Instance.ShouldNavigationSprint();
        }
    }

    private bool IsPlayerCollider(Collider2D col)
    {
        return col == playerCollider || col.transform == player || col.transform.IsChildOf(player);
    }

    /// <summary>
    /// 判断碰撞体是否是障碍物
    /// 🔥 策略：宁可误杀，不可放过
    /// 默认所有非 Trigger 的碰撞体都是障碍物，除非明确排除
    /// </summary>
    private bool IsObstacle(Collider2D col)
    {
        // 🔥 Trigger 通常不阻挡视线（如拾取物、触发区域）
        if (col.isTrigger) return false;
        
        // 排除掉落物和临时物体
        if (col.name.Contains("(Clone)") || col.name.Contains("Pickup")) return false;
        
        // 🔥 新策略：默认所有非 Trigger 的碰撞体都是障碍物
        // 除非它在排除列表中
        
        // 如果配置了 LayerMask，检查是否在 Mask 中
        if (losObstacleMask.value != 0)
        {
            bool inMask = ((1 << col.gameObject.layer) & losObstacleMask.value) != 0;
            if (inMask) return true; // 在 Mask 中，是障碍物
        }
        
        // 如果配置了 Tags，检查是否有匹配的 Tag
        if (losObstacleTags != null && losObstacleTags.Length > 0)
        {
            bool hasTag = HasAnyTag(col.transform, losObstacleTags);
            if (hasTag) return true; // 有匹配 Tag，是障碍物
        }
        
        // 🔥 关键：如果既没有配置 Mask 也没有配置 Tags，
        // 或者物体不在配置的 Mask/Tags 中，
        // 默认也视为障碍物（宁可误杀，不可放过）
        return true;
    }

    private static bool HasAnyTag(Transform t, string[] tags)
    {
        if (t == null || tags == null) return false;
        Transform current = t;
        while (current != null)
        {
            foreach (var tag in tags)
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    try { if (current.CompareTag(tag.Trim())) return true; } catch { }
                }
            }
            current = current.parent;
        }
        return false;
    }

    #region 🔥 智能目标点计算（v4.0 ClosestPoint 统一版）
    
    // 缓存目标 Collider，避免每帧 GetComponent
    private Collider2D _cachedTargetCollider;
    private Transform _cachedTargetTransform;
    
    /// <summary>
    /// 🔥 v4.0：获取目标最近点（使用 ClosestPoint）
    /// 
    /// 核心思路：
    /// 1. 使用 Collider.ClosestPoint(playerPos) 计算玩家到目标的最近点
    /// 2. 这样从任何方向接近都是最短路径，不会绕路
    /// 3. 与 GameInputManager 使用相同的距离计算方式
    /// </summary>
    private Vector2 GetTargetAnchorPoint(Transform target)
    {
        if (target == null) return transform.position;
        
        // 缓存 Collider
        if (_cachedTargetTransform != target)
        {
            _cachedTargetTransform = target;
            _cachedTargetCollider = target.GetComponent<Collider2D>();
            if (_cachedTargetCollider == null)
                _cachedTargetCollider = target.GetComponentInChildren<Collider2D>();
        }
        
        if (_cachedTargetCollider != null)
        {
            // 🔥 v4.0：使用 ClosestPoint 计算玩家到 Collider 的最近点
            Vector2 playerPos = GetPlayerPosition();
            return _cachedTargetCollider.ClosestPoint(playerPos);
        }
        
        return target.position;
    }
    
    /// <summary>
    /// 🔥 v4.0：计算玩家到目标的最近接近点
    /// 
    /// 核心思路：
    /// 1. 使用 ClosestPoint 计算玩家到 Collider 的最近点
    /// 2. 这样导航的目标与交互判断的目标一致
    /// </summary>
    private Vector3 CalculateClosestApproachPoint(Transform target)
    {
        return GetTargetAnchorPoint(target);
    }
    
    /// <summary>
    /// 根据交互距离计算最优停止半径
    /// 
    /// 核心思路：
    /// 1. 停止半径应该小于交互距离，确保到达后能触发交互
    /// 2. GameInputManager 允许 20% 容差，所以停止半径 = interactDist * 0.9
    /// 3. 这样玩家停下后，距离 < interactDist * 1.2，一定能交互
    /// </summary>
    private float CalculateOptimalStopRadius(Transform target, float defaultRadius)
    {
        // 尝试获取 IInteractable 的交互距离
        var interactable = target.GetComponent<IInteractable>();
        if (interactable != null)
        {
            float interactDist = interactable.InteractionDistance;
            // 停止半径 = 交互距离 * 0.9，确保在交互范围内
            return Mathf.Max(0.3f, interactDist * 0.9f);
        }
        
        // 没有 IInteractable，使用默认值
        return Mathf.Clamp(defaultRadius, 0.3f, 1.5f);
    }
    
    /// <summary>
    /// 🔥 v4.0：检查玩家是否足够近可以与目标交互
    /// 
    /// 核心思路：
    /// 1. 使用与 GameInputManager 相同的距离计算方式
    /// 2. 玩家中心 → Collider 最近点（ClosestPoint）
    /// </summary>
    private bool IsCloseEnoughToInteract(Vector2 playerPos)
    {
        Vector2 targetAnchor = GetTargetAnchorPoint(targetTransform);
        float distance = Vector2.Distance(playerPos, targetAnchor);
        return distance <= followStopRadius;
    }
    
    #endregion

    #region 调试输出

    private void AddDebugLog(string message)
    {
        debugLogs.Add($"[{Time.frameCount}] {message}");
    }

    /// <summary>
    /// 输出完整的调试信息
    /// </summary>
    private void LogFullDebugInfo(string reason)
    {
        if (!enableDetailedDebug) return;
        
        Vector2 playerPos = GetPlayerPosition();
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<color=red>═══════════════════════════════════════</color>");
        sb.AppendLine($"<color=red>[Nav] 导航诊断报告：{reason}</color>");
        sb.AppendLine($"<color=red>═══════════════════════════════════════</color>");
        
        // 基本信息
        sb.AppendLine($"<color=yellow>【基本信息】</color>");
        sb.AppendLine($"  玩家位置: {playerPos}");
        sb.AppendLine($"  目标位置: {(Vector2)targetPoint}");
        sb.AppendLine($"  距离目标: {Vector2.Distance(playerPos, targetPoint):F2}m");
        sb.AppendLine($"  玩家半径: {playerRadius:F2}m");
        sb.AppendLine($"  是否跑步: {runWhileNavigating}");
        
        // 路径信息
        sb.AppendLine($"<color=yellow>【路径信息】</color>");
        sb.AppendLine($"  路径点数: {path.Count}");
        sb.AppendLine($"  当前索引: {pathIndex}");
        if (path.Count > 0)
        {
            sb.AppendLine($"  路径点列表:");
            for (int i = 0; i < Mathf.Min(path.Count, 10); i++)
            {
                string marker = i == pathIndex ? " ← 当前" : "";
                sb.AppendLine($"    [{i}] {path[i]}{marker}");
            }
            if (path.Count > 10)
            {
                sb.AppendLine($"    ... 还有 {path.Count - 10} 个点");
            }
        }
        
        // 网格信息
        if (navGrid != null)
        {
            sb.AppendLine($"<color=yellow>【网格信息】</color>");
            sb.AppendLine($"  起点可走: {navGrid.IsWalkable(playerPos)}");
            sb.AppendLine($"  终点可走: {navGrid.IsWalkable(targetPoint)}");
        }
        
        // 周边障碍物
        sb.AppendLine($"<color=yellow>【周边障碍物】</color>");
        var nearbyObstacles = Physics2D.OverlapCircleAll(playerPos, 2f);
        int obstacleCount = 0;
        foreach (var col in nearbyObstacles)
        {
            if (IsPlayerCollider(col)) continue;
            if (IsObstacle(col))
            {
                obstacleCount++;
                Vector2 obstaclePos = col.transform.position;
                float dist = Vector2.Distance(playerPos, obstaclePos);
                Vector2 closest = col.ClosestPoint(playerPos);
                float closestDist = Vector2.Distance(playerPos, closest);
                
                if (obstacleCount <= 8)
                {
                    sb.AppendLine($"  {obstacleCount}. {col.name}");
                    sb.AppendLine($"     位置: {obstaclePos}, 距离: {dist:F2}m, 最近点距离: {closestDist:F2}m");
                }
            }
        }
        if (obstacleCount > 8)
        {
            sb.AppendLine($"  ... 还有 {obstacleCount - 8} 个障碍物");
        }
        if (obstacleCount == 0)
        {
            sb.AppendLine($"  无障碍物");
        }
        
        // 调试日志历史
        if (debugLogs.Count > 0)
        {
            sb.AppendLine($"<color=yellow>【调试日志】</color>");
            foreach (var log in debugLogs)
            {
                sb.AppendLine($"  {log}");
            }
        }
        
        // 卡顿信息
        sb.AppendLine($"<color=yellow>【卡顿信息】</color>");
        sb.AppendLine($"  卡顿次数: {stuckRetryCount}/{MAX_STUCK_RETRIES}");
        sb.AppendLine($"  上次检测位置: {lastCheckPosition}");
        sb.AppendLine($"  当前位置: {playerPos}");
        sb.AppendLine($"  位移: {Vector2.Distance(lastCheckPosition, playerPos):F3}m");
        
        sb.AppendLine($"<color=red>═══════════════════════════════════════</color>");
        
        Debug.LogWarning(sb.ToString());
    }

    #endregion

    void OnDrawGizmos()
    {
        if (!showPathGizmos || !active || path == null || path.Count == 0) return;

        // 绘制路径线
        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.Count - 1; i++) 
            Gizmos.DrawLine(path[i], path[i + 1]);

        // 绘制路径点
        for (int i = 0; i < path.Count; i++)
        {
            if (i < pathIndex)
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            else if (i == pathIndex)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.cyan;
            
            Gizmos.DrawSphere(path[i], 0.08f);
        }

        // 绘制目标点
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPoint, 0.25f);

        // 绘制当前移动方向
        if (playerCollider != null && pathIndex < path.Count)
        {
            Vector3 playerPos = playerCollider.bounds.center;
            
            // 到路径点的方向
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerPos, path[pathIndex]);
        }
        
        // 🔥 可视化调试：绘制视线检测结果
        if (enableLineOfSightOptimization && playerCollider != null && pathIndex < path.Count)
        {
            Vector2 playerPos = playerCollider.bounds.center;
            
            // 绘制从玩家到当前路径点的视线
            if (_lastLOSBlocked && _lastLOSHit.collider != null)
            {
                // 被阻挡：红线 + 红球
                Gizmos.color = Color.red;
                Gizmos.DrawLine(playerPos, _lastLOSHit.point);
                Gizmos.DrawWireSphere(_lastLOSHit.point, 0.15f);
                Gizmos.DrawSphere(_lastLOSHit.point, 0.08f);
            }
            else
            {
                // 通畅：绿线
                Gizmos.color = Color.green;
                for (int i = pathIndex; i < path.Count; i++)
                {
                    Vector2 checkFrom = (i == pathIndex) ? playerPos : path[i - 1];
                    Vector2 checkTo = path[i];
                    
                    // 简单检查（不调用 HasLineOfSight 避免递归）
                    Gizmos.DrawLine(checkFrom, checkTo);
                }
            }
        }
    }
}
