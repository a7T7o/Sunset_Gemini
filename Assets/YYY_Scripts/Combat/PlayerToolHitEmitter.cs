using UnityEngine;
using System.Collections.Generic;
using FarmGame.Combat;
using FarmGame.Events;
using FarmGame.Data;

/// <summary>
/// 玩家工具命中发射器
/// 
/// 功能：
/// - 帧索引检测（frame 3-4 触发命中）
/// - 使用 Sprite Bounds 检测，不依赖物理碰撞体
/// - 支持工具-资源交互配置
/// </summary>
public class PlayerToolHitEmitter : MonoBehaviour
{
    #region 序列化字段
    
    [Header("命中检测设置")]
    [Tooltip("扇形角度（度）")]
    [Range(30f, 120f)]
    public float wedgeAngleDeg = 60f;
    
    [Tooltip("默认命中半径")]
    [Range(0.5f, 3f)]
    public float defaultReach = 1.5f;
    
    [Tooltip("命中窗口起始帧（0-7）")]
    [Range(0, 7)]
    public int hitWindowStart = 3;
    
    [Tooltip("命中窗口结束帧（0-7）")]
    [Range(0, 7)]
    public int hitWindowEnd = 4;
    
    [Header("工具-资源交互")]
    [Tooltip("交互配置表（可选，不设置则使用默认规则）")]
    public ToolResourceInteractionTable interactionTable;
    
    [Header("引用")]
    [Tooltip("工具 Animator")]
    public Animator toolAnimator;
    
    [Tooltip("玩家 SpriteRenderer")]
    public SpriteRenderer playerSpriteRenderer;
    
    [Tooltip("玩家 Collider2D（用于获取攻击起点）")]
    public Collider2D playerCollider;
    
    [Tooltip("玩家工具控制器（用于获取当前装备的工具类型）")]
    public PlayerToolController playerToolController;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool drawGizmos = true;
    
    #endregion
    
    #region 私有字段
    
    private bool _hitConsumedThisSwing = false;
    private int _lastFrameIndex = -1;
    private int _lastState = -1;
    
    // Gizmos 绘制用
    private Vector2 _lastHitOrigin;
    private Vector2 _lastHitForward;
    private float _lastHitReach;
    
    // 攻击状态常量
    private const int STATE_SLICE = 6;
    private const int STATE_CRUSH = 8;
    
    #endregion
    
    #region Unity 生命周期
    
    void Start()
    {
        if (toolAnimator == null)
        {
            var toolController = GetComponent<PlayerToolController>();
            if (toolController != null)
            {
                toolAnimator = toolController.ToolAnimator;
            }
        }
        
        if (playerSpriteRenderer == null)
        {
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // ✅ 自动获取玩家 Collider（优先使用显式引用）
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
            if (playerCollider == null)
            {
                playerCollider = GetComponentInChildren<Collider2D>();
            }
        }
        
        // ✅ 自动获取 PlayerToolController（用于获取当前装备的工具类型）
        if (playerToolController == null)
        {
            playerToolController = GetComponent<PlayerToolController>();
            if (playerToolController == null)
            {
                playerToolController = GetComponentInParent<PlayerToolController>();
            }
        }
        
        if (ResourceNodeRegistry.Instance == null)
        {
            Debug.LogWarning("[PlayerToolHitEmitter] ResourceNodeRegistry 未找到，命中检测将不可用");
        }
    }
    
    void Update()
    {
        if (toolAnimator == null) return;
        if (ResourceNodeRegistry.Instance == null) return;
        
        // ✅ 修复：使用 SafeGetInteger 避免编辑器模式下报错
        int currentState = toolAnimator.SafeGetInteger("State", 0);
        int currentFrame = GetCurrentAnimationFrame();
        
        // 检测是否是攻击状态
        bool isAttacking = currentState == STATE_SLICE || currentState == STATE_CRUSH;
        
        // 状态改变时重置
        if (currentState != _lastState)
        {
            _hitConsumedThisSwing = false;
            _lastState = currentState;
        }
        
        // ✅ 修复：当帧索引回到起始帧（动画循环）时重置命中标记
        // 这样连续砍伐时每次挥砍都能触发命中
        if (_lastFrameIndex > currentFrame && currentFrame < hitWindowStart)
        {
            _hitConsumedThisSwing = false;
        }
        
        // 检测是否在命中窗口内
        bool inHitWindow = currentFrame >= hitWindowStart && currentFrame <= hitWindowEnd;
        
        if (isAttacking && inHitWindow && !_hitConsumedThisSwing)
        {
            _hitConsumedThisSwing = true;
            TryHit();
        }
        
        _lastFrameIndex = currentFrame;
    }
    
    #endregion
    
    #region 命中检测
    
    /// <summary>
    /// 位置验证容差值
    /// </summary>
    private const float POSITION_TOLERANCE = 0.1f;
    
    private void TryHit()
    {
        Vector2 origin = GetPlayerCenter();
        Vector2 forward = GetFacingDirection();
        float reach = defaultReach;
        
        // ✅ 修复：使用 SafeGetInteger 避免编辑器模式下报错
        // 获取玩家朝向参数
        int direction = toolAnimator != null ? toolAnimator.SafeGetInteger("Direction", 0) : 0;
        bool flipX = playerSpriteRenderer != null && playerSpriteRenderer.flipX;
        
        _lastHitOrigin = origin;
        _lastHitForward = forward;
        _lastHitReach = reach;
        
        // ✅ 清空上次的命中记录
        _lastHitPoints.Clear();
        _lastHitDirections.Clear();
        _lastFallDirections.Clear();
        
        // 从注册表获取范围内的节点
        var nodes = ResourceNodeRegistry.Instance.GetNodesInRange(origin, reach);
        
        if (showDebugInfo)
        {
            string dirName = direction switch { 0 => "Down", 1 => "Up", 2 => "Side", _ => "Unknown" };
            
            Debug.Log($"<color=cyan>[PlayerToolHitEmitter] ========== 命中检测开始 ==========</color>\n" +
                     $"  玩家中心: ({origin.x:F2}, {origin.y:F2})\n" +
                     $"  玩家朝向: {dirName} (Direction={direction}, FlipX={flipX})\n" +
                     $"  朝向向量: ({forward.x:F2}, {forward.y:F2})\n" +
                     $"  攻击范围: {reach:F2}\n" +
                     $"  扇形角度: {wedgeAngleDeg}°\n" +
                     $"  范围内节点数: {nodes.Count}");
        }
        
        // 构建命中上下文
        var ctx = BuildContext();
        
        // ✅ 筛选通过所有验证的候选节点
        List<(IResourceNode node, float distance, Bounds colliderBounds)> validCandidates = new List<(IResourceNode, float, Bounds)>();
        
        foreach (var node in nodes)
        {
            if (node == null || node.IsDepleted) continue;
            
            // ✅ 使用 Collider bounds 而非 Sprite bounds
            Bounds colliderBounds = node.GetColliderBounds();
            
            // A. 扇形检测（使用 Collider bounds）
            if (!CheckWedgeBoundsIntersection(origin, forward, reach, wedgeAngleDeg, colliderBounds))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"<color=gray>[命中检测] {node.ResourceTag} 未通过扇形检测</color>");
                }
                continue;
            }
            
            // B. 位置验证
            if (!ValidatePositionRelation(origin, direction, flipX, colliderBounds))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>[命中检测] {node.ResourceTag} 未通过位置验证（玩家朝向与相对位置不符）</color>");
                }
                continue;
            }
            
            // ✅ 计算 Collider 中心到玩家中心的距离
            float distance = Vector2.Distance(origin, colliderBounds.center);
            validCandidates.Add((node, distance, colliderBounds));
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=green>[命中检测] {node.ResourceTag} 通过验证，距离: {distance:F2}</color>");
            }
        }
        
        // ✅ 只命中最近的一个
        if (validCandidates.Count > 0)
        {
            // 按距离排序，选择最近的
            validCandidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            var closest = validCandidates[0];
            
            // 更新命中点和方向
            ctx.hitPoint = closest.colliderBounds.ClosestPoint(origin);
            ctx.hitDir = ((Vector2)closest.colliderBounds.center - origin).normalized;
            
            // ✅ 记录命中点和方向用于调试绘制
            _lastHitPoints.Add(ctx.hitPoint);
            _lastHitDirections.Add(ctx.hitDir);
            
            #if UNITY_EDITOR
            // ✅ 计算倒下方向（用于调试绘制）
            Vector2 fallDir = CalculateFallDirection(direction, flipX);
            _lastFallDirections.Add(fallDir);
            #endif
            
            // 调用节点的命中处理
            closest.node.OnHit(ctx);
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=lime>[PlayerToolHitEmitter] ✓ 命中: {closest.node.ResourceTag}，距离: {closest.distance:F2}</color>");
            }
            
            // 广播事件
            ToolEvents.RaiseToolStrike(new ToolStrikeEventArgs
            {
                attacker = gameObject,
                toolItemId = ctx.toolItemId,
                toolQuality = ctx.toolQuality,
                actionState = ctx.actionState,
                frameIndex = GetCurrentAnimationFrame(),
                origin = origin,
                forward = forward,
                wedgeAngleDeg = wedgeAngleDeg,
                reach = reach,
                candidates = null
            });
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log($"<color=gray>[PlayerToolHitEmitter] 无有效命中目标</color>");
            }
        }
    }
    
    /// <summary>
    /// 验证玩家位置与朝向是否符合逻辑
    /// </summary>
    /// <param name="playerCenter">玩家 Collider 中心</param>
    /// <param name="direction">玩家朝向 (0=Down, 1=Up, 2=Side)</param>
    /// <param name="flipX">是否水平翻转</param>
    /// <param name="colliderBounds">目标 Collider 边界</param>
    /// <returns>位置关系是否符合逻辑</returns>
    private bool ValidatePositionRelation(Vector2 playerCenter, int direction, bool flipX, Bounds colliderBounds)
    {
        switch (direction)
        {
            case 0: // Down - 玩家必须在目标上方
                // playerY > collider.max.y - tolerance
                return playerCenter.y > colliderBounds.max.y - POSITION_TOLERANCE;
                
            case 1: // Up - 玩家必须在目标下方
                // playerY < collider.min.y + tolerance
                return playerCenter.y < colliderBounds.min.y + POSITION_TOLERANCE;
                
            case 2: // Side
                if (flipX) // 朝左 - 玩家必须在目标右侧
                {
                    // playerX > collider.max.x - tolerance
                    return playerCenter.x > colliderBounds.max.x - POSITION_TOLERANCE;
                }
                else // 朝右 - 玩家必须在目标左侧
                {
                    // playerX < collider.min.x + tolerance
                    return playerCenter.x < colliderBounds.min.x + POSITION_TOLERANCE;
                }
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 检查扇形与边界是否相交（使用 Collider bounds）
    /// </summary>
    private bool CheckWedgeBoundsIntersection(Vector2 origin, Vector2 forward, float reach, float angleDeg, Bounds bounds)
    {
        // 1. 先检查距离（快速剔除）
        Vector2 closestPoint = bounds.ClosestPoint(origin);
        float distSqr = (closestPoint - origin).sqrMagnitude;
        float dist = Mathf.Sqrt(distSqr);
        
        // 2. 检查角度
        Vector2 toClosest = (closestPoint - origin).normalized;
        float angle = Vector2.Angle(forward, toClosest);
        
        bool distanceOk = distSqr <= reach * reach;
        bool angleOk = angle <= angleDeg * 0.5f;
        bool result = distanceOk && angleOk;
        
        // ✅ 详细调试输出
        if (showDebugInfo)
        {
            Vector2 boundsCenter = bounds.center;
            Vector2 boundsMin = bounds.min;
            Vector2 boundsMax = bounds.max;
            
            string posRelation = GetPositionRelation(origin, bounds);
            
            Debug.Log($"<color={(result ? "green" : "red")}>[命中检测详情]</color>\n" +
                     $"  玩家中心: ({origin.x:F2}, {origin.y:F2})\n" +
                     $"  玩家朝向: ({forward.x:F2}, {forward.y:F2})\n" +
                     $"  树木Bounds中心: ({boundsCenter.x:F2}, {boundsCenter.y:F2})\n" +
                     $"  树木Bounds范围: min({boundsMin.x:F2}, {boundsMin.y:F2}) ~ max({boundsMax.x:F2}, {boundsMax.y:F2})\n" +
                     $"  最近点: ({closestPoint.x:F2}, {closestPoint.y:F2})\n" +
                     $"  位置关系: {posRelation}\n" +
                     $"  距离: {dist:F2} (限制: {reach:F2}) → {(distanceOk ? "✓" : "✗")}\n" +
                     $"  角度: {angle:F1}° (限制: ±{angleDeg * 0.5f:F1}°) → {(angleOk ? "✓" : "✗")}\n" +
                     $"  结果: {(result ? "命中" : "未命中")}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取玩家相对于树木的位置关系描述
    /// </summary>
    private string GetPositionRelation(Vector2 playerPos, Bounds treeBounds)
    {
        Vector2 center = treeBounds.center;
        Vector2 min = treeBounds.min;
        Vector2 max = treeBounds.max;
        
        string horizontal = "";
        string vertical = "";
        
        // 水平位置
        if (playerPos.x < min.x)
            horizontal = "左侧";
        else if (playerPos.x > max.x)
            horizontal = "右侧";
        else
            horizontal = "水平重叠";
        
        // 垂直位置
        if (playerPos.y < min.y)
            vertical = "下方";
        else if (playerPos.y > max.y)
            vertical = "上方";
        else
            vertical = "垂直重叠";
        
        // 特殊情况：玩家在树木内部
        if (treeBounds.Contains(playerPos))
        {
            return "在树木内部";
        }
        
        return $"{vertical} + {horizontal}";
    }
    
    private ToolHitContext BuildContext()
    {
        // ✅ 修复：使用 SafeGetInteger 避免编辑器模式下报错
        int toolItemId = toolAnimator != null ? toolAnimator.SafeGetInteger("ToolItemId", 0) : 0;
        // ✅ 修复：品质系统已改变，每个品质的工具都是独立的 ItemID
        // 不再需要单独的 quality 参数，默认为 0
        int toolQuality = 0;
        int state = toolAnimator != null ? toolAnimator.SafeGetInteger("State", 0) : 0;
        
        // ✅ 修复：从 PlayerToolController 获取实际的工具类型
        // 这样可以正确区分 Hoe 和 Pickaxe（它们都使用 Crush 动画状态）
        ToolType toolType = ToolType.None;
        float baseDamage = 1f;
        float energyCost = 2f;
        
        if (playerToolController != null && playerToolController.CurrentToolData != null)
        {
            var toolData = playerToolController.CurrentToolData;
            // 优先从当前装备的工具数据获取工具类型
            toolType = toolData.toolType;
            
            // ★ 修复：从 ToolData 获取正确的伤害值
            // 斧头的伤害基于材料等级
            baseDamage = GetToolBaseDamage(toolData);
            energyCost = toolData.energyCost;
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=cyan>[PlayerToolHitEmitter] BuildContext:\n" +
                          $"  - 工具：{toolData.itemName} (ID={toolData.itemID})\n" +
                          $"  - 类型：{toolType}\n" +
                          $"  - 材料等级：{toolData.materialTier}\n" +
                          $"  - 基础伤害：{baseDamage}\n" +
                          $"  - 精力消耗：{energyCost}</color>");
            }
        }
        else
        {
            // 回退：根据动画状态推断工具类型（不够精确，但作为兜底）
            toolType = state switch
            {
                STATE_SLICE => ToolType.Axe,
                STATE_CRUSH => ToolType.Pickaxe,  // 注意：这里无法区分 Hoe 和 Pickaxe
                _ => ToolType.None
            };
        }
        
        return new ToolHitContext
        {
            toolItemId = toolItemId,
            toolQuality = toolQuality,
            toolType = toolType,
            actionState = state,
            hitPoint = Vector2.zero,
            hitDir = Vector2.zero,
            attacker = gameObject,
            baseDamage = baseDamage,
            frameIndex = GetCurrentAnimationFrame()
        };
    }
    
    /// <summary>
    /// 获取工具的基础伤害值
    /// 斧头伤害基于材料等级：木=2, 石=3, 生铁=4, 黄铜=5, 钢=6, 金=8
    /// </summary>
    private float GetToolBaseDamage(ToolData toolData)
    {
        if (toolData == null) return 1f;
        
        // 如果工具配置了 canDealDamage 和 damageAmount，使用配置值
        if (toolData.canDealDamage && toolData.damageAmount > 0)
        {
            return toolData.damageAmount;
        }
        
        // 斧头的伤害基于材料等级
        if (toolData.toolType == ToolType.Axe)
        {
            return toolData.materialTier switch
            {
                MaterialTier.Wood => 2f,
                MaterialTier.Stone => 3f,
                MaterialTier.Iron => 4f,
                MaterialTier.Brass => 5f,
                MaterialTier.Steel => 6f,
                MaterialTier.Gold => 8f,
                _ => 2f
            };
        }
        
        // 其他工具默认伤害
        return 1f;
    }
    
    #endregion
    
    #region 辅助方法
    
    private Vector2 GetPlayerCenter()
    {
        // ✅ 优先使用显式引用的 playerCollider
        if (playerCollider != null)
        {
            return playerCollider.bounds.center;
        }
        
        // 回退到 SpriteRenderer 中心
        if (playerSpriteRenderer != null)
        {
            return playerSpriteRenderer.bounds.center;
        }
        
        return transform.position;
    }
    
    /// <summary>
    /// 获取玩家面对的方向（基于 Animator Direction 参数）
    /// </summary>
    private Vector2 GetFacingDirection()
    {
        if (toolAnimator == null) return Vector2.down;
        
        // ✅ 修复：使用 SafeGetInteger 避免编辑器模式下 Animator 未播放时报错
        int direction = toolAnimator.SafeGetInteger("Direction", 0);
        bool flipX = playerSpriteRenderer != null && playerSpriteRenderer.flipX;
        
        // ✅ 修正：Direction 参数映射
        // 0=Down, 1=Up, 2=Side（来自 PlayerAnimController.ConvertToAnimatorDirection）
        // Side 时根据 flipX 判断左右
        return direction switch
        {
            0 => Vector2.down,      // 朝下
            1 => Vector2.up,        // 朝上
            2 => flipX ? Vector2.left : Vector2.right,  // 朝左/右
            _ => Vector2.down
        };
    }
    
    private int GetCurrentAnimationFrame()
    {
        if (toolAnimator == null) return 0;
        
        // ✅ 修复：检查 Animator 是否有有效的 RuntimeAnimatorController
        if (toolAnimator.runtimeAnimatorController == null) return 0;
        
        var stateInfo = toolAnimator.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime % 1f;
        
        return Mathf.FloorToInt(normalizedTime * 8);
    }
    
    #endregion
    
    #region Gizmos
    
    private List<Vector2> _lastHitPoints = new List<Vector2>();
    private List<Vector2> _lastHitDirections = new List<Vector2>();
    private List<Vector2> _lastFallDirections = new List<Vector2>(); // 倒下方向（四向简化）
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        
        Vector2 origin = Application.isPlaying ? _lastHitOrigin : GetPlayerCenter();
        Vector2 forward = Application.isPlaying ? _lastHitForward : GetFacingDirection();
        float reach = Application.isPlaying ? _lastHitReach : defaultReach;
        
        // ✅ 绘制扇形边界（朝向玩家面对的方向）
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        float halfAngle = wedgeAngleDeg * 0.5f * Mathf.Deg2Rad;
        float forwardAngle = Mathf.Atan2(forward.y, forward.x);
        
        // 绘制扇形弧线
        int segments = 20;
        Vector2 prevPoint = origin;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = forwardAngle - halfAngle + (halfAngle * 2f * t);
            Vector2 point = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;
            
            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, point);
            }
            prevPoint = point;
        }
        
        // 绘制扇形边界线
        Vector2 leftEdge = new Vector2(
            Mathf.Cos(forwardAngle + halfAngle),
            Mathf.Sin(forwardAngle + halfAngle)
        ) * reach;
        
        Vector2 rightEdge = new Vector2(
            Mathf.Cos(forwardAngle - halfAngle),
            Mathf.Sin(forwardAngle - halfAngle)
        ) * reach;
        
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawLine(origin, origin + leftEdge);
        Gizmos.DrawLine(origin, origin + rightEdge);
        
        // ✅ 绘制玩家朝向箭头（粗箭头，蓝色）
        Gizmos.color = Color.blue;
        Vector2 arrowEnd = origin + forward * (reach * 0.8f);
        Gizmos.DrawLine(origin, arrowEnd);
        
        // 绘制箭头头部（三角形）
        Vector2 perpendicular = new Vector2(-forward.y, forward.x);
        Vector2 arrowTip1 = arrowEnd - forward * 0.3f + perpendicular * 0.15f;
        Vector2 arrowTip2 = arrowEnd - forward * 0.3f - perpendicular * 0.15f;
        Gizmos.DrawLine(arrowEnd, arrowTip1);
        Gizmos.DrawLine(arrowEnd, arrowTip2);
        Gizmos.DrawLine(arrowTip1, arrowTip2);
        
        // ✅ 绘制扇形中心线（绿色，表示攻击范围中心）
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + forward * reach);
        
        // ✅ 绘制命中点和倒下方向箭头
        if (Application.isPlaying && _lastHitPoints.Count > 0)
        {
            for (int i = 0; i < _lastHitPoints.Count; i++)
            {
                Vector2 hitPoint = _lastHitPoints[i];
                Vector2 fallDir = i < _lastFallDirections.Count ? _lastFallDirections[i] : Vector2.zero;
                
                // 绘制命中点（红色球）
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(hitPoint, 0.1f);
                
                // ✅ 绘制倒下方向箭头（黄色，四向）
                if (fallDir != Vector2.zero)
                {
                    Gizmos.color = Color.yellow;
                    Vector2 arrowDirEnd = hitPoint + fallDir * 0.6f;
                    
                    // 绘制粗箭头线
                    Gizmos.DrawLine(hitPoint, arrowDirEnd);
                    
                    // 绘制箭头头部（三角形）
                    Vector2 perpDir = new Vector2(-fallDir.y, fallDir.x);
                    Vector2 tip1 = arrowDirEnd - fallDir * 0.2f + perpDir * 0.12f;
                    Vector2 tip2 = arrowDirEnd - fallDir * 0.2f - perpDir * 0.12f;
                    Gizmos.DrawLine(arrowDirEnd, tip1);
                    Gizmos.DrawLine(arrowDirEnd, tip2);
                    Gizmos.DrawLine(tip1, tip2);
                }
            }
        }
    }
    
    /// <summary>
    /// 简化方向为四个基本方向（上下左右）
    /// </summary>
    private Vector2 GetSimplifiedDirection(Vector2 dir)
    {
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);
        
        if (absX > absY)
        {
            return dir.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            return dir.y > 0 ? Vector2.up : Vector2.down;
        }
    }
    
    /// <summary>
    /// 根据玩家朝向和翻转状态计算倒下方向
    /// ✅ 修正：Direction 参数映射 0=Down, 1=Up, 2=Side
    /// ✅ 与 TreeController.DetermineFallDirection 保持一致
    /// </summary>
    private Vector2 CalculateFallDirection(int playerDirection, bool playerFlipX)
    {
        switch (playerDirection)
        {
            case 0: // Down
                // Down: 向右倒（flipX时向左倒）
                return playerFlipX ? Vector2.left : Vector2.right;
            case 1: // Up
                // Up: 向左倒（flipX时向右倒）
                return playerFlipX ? Vector2.right : Vector2.left;
            case 2: // Side
                // Side: 向上倒
                return Vector2.up;
            default:
                return Vector2.right;
        }
    }
    #endif
    
    #endregion
}
