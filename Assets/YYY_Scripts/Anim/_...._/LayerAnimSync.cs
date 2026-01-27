using UnityEngine;

/// <summary>
/// 图层动画同步器（精确帧同步版 - 数据驱动优化）
/// 1. 在LateUpdate中精确同步Player动画时间到Tool
/// 2. 同步flipX
/// 3. 控制Tool显示/隐藏（只在使用工具动作时显示）
/// 4. 从PlayerToolController读取当前ToolData配置
/// </summary>
public class LayerAnimSync : MonoBehaviour
{
    [Header("━━━━ Player组件 ━━━━")]
    [Tooltip("Player的Animator（主控）")]
    [SerializeField] private Animator playerAnimator;

    [Tooltip("Player的SpriteRenderer（用于同步flipX）")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    [Header("━━━━ Tool组件 ━━━━")]
    [Tooltip("Tool的Animator（由PlayerToolController动态管理）")]
    [SerializeField] private Animator toolAnimator;

    [Tooltip("Tool的SpriteRenderer")]
    [SerializeField] private SpriteRenderer toolSpriteRenderer;

    [Header("━━━━ 控制器引用 ━━━━")]
    [Tooltip("玩家工具控制器（用于获取当前ToolData）")]
    [SerializeField] private PlayerToolController playerToolController;

    [Header("━━━━ 工具动作State ━━━━")]
    [Tooltip("需要显示工具的State列表")]
    [SerializeField] private int[] toolActiveStates = new int[] { 6, 7, 8, 9, 10 };  // Slice=6, Pierce=7, Crush=8, Fish=9, Watering=10

    [Header("━━━━ 动画结束处理 ━━━━")]
    [Tooltip("最后一帧锁定的归一化时间阈值（超过此值后锁定在最后一帧）\n例如：0.85 表示动画播放到 85% 后，工具锁定在最后一帧")]
    [SerializeField, Range(0.7f, 0.95f)] private float lastFrameLockThreshold = 0.85f;
    
    [Tooltip("是否启用最后一帧锁定（解决残影问题）")]
    [SerializeField] private bool enableLastFrameLock = true;
    
    [Tooltip("动画结束后隐藏工具的延迟帧数（防止闪烁）")]
    [SerializeField, Range(0, 5)] private int hideDelayFrames = 2;
    
    // 用于延迟隐藏的状态追踪
    private int _lastToolActiveState = -1;
    private int _hideDelayCounter = 0;
    private bool _isInHideDelay = false;
    
    // 强制隐藏标志（用于动作结束时立即隐藏工具）
    private bool _forceHideUntilNextAction = false;

    [Header("━━━━ 调试 ━━━━")]
    [Tooltip("启用工具动画调试日志")]
    [SerializeField] private bool enableToolDebug = true;
    
    [Tooltip("调试日志间隔（秒）")]
    [SerializeField] private float debugLogInterval = 0.5f;

    private const float EPSILON = 1e-6f;

    private int lastState = -1;
    private float lastDebugTime = 0f;

    // 延迟初始化标志
    private bool isInitialized = false;

    // ========== 公共方法 ==========
    
    /// <summary>
    /// 强制隐藏工具（动作结束时调用）
    /// 防止动画结束时的鬼畜/闪烁
    /// </summary>
    public void ForceHideTool()
    {
        _forceHideUntilNextAction = true;
        if (toolSpriteRenderer != null)
            toolSpriteRenderer.enabled = false;
    }
    
    /// <summary>
    /// 允许工具显示（新动作开始时调用）
    /// </summary>
    public void AllowToolShow()
    {
        _forceHideUntilNextAction = false;
    }

    // ========== 初始化和验证 ==========

    void Start()
    {
        // 延迟到第一帧LateUpdate时初始化，避免Graphs错误
        isInitialized = false;
    }


    void ValidateSetup()
    {
        // 从PlayerToolController获取Tool组件引用
        if (playerToolController != null)
        {
            if (toolAnimator == null)
                toolAnimator = playerToolController.ToolAnimator;
            if (toolSpriteRenderer == null)
                toolSpriteRenderer = playerToolController.ToolSpriteRenderer;
        }

        // 关键：禁用Tool Animator的自动播放，完全手动控制
        if (toolAnimator != null)
        {
            toolAnimator.speed = 0f;
        }
    }
    

    // ========== 持续同步逻辑 ==========

    void LateUpdate()
    {
        // 延迟初始化：等到第一帧LateUpdate时才执行
        if (!isInitialized)
        {
            ValidateSetup();
            isInitialized = true;
        }

        if (playerAnimator == null || toolAnimator == null)
            return;

        // 🎯 安全获取当前State
        int currentState = playerAnimator.SafeGetInteger("State", 0);

        // 同步参数并在当帧完成状态过渡评估
        SyncParameters();
        toolAnimator.Update(0);

        // 1. 控制Tool显示/隐藏
        UpdateToolVisibility(currentState);

        // 2. 🎯 精确帧同步：在LateUpdate末尾采样最准确的时间
        bool isToolActive = System.Array.IndexOf(toolActiveStates, currentState) >= 0;

        if (isToolActive)
        {
            AnimatorStateInfo playerState = playerAnimator.GetCurrentAnimatorStateInfo(0);
            float pNorm = playerState.normalizedTime % 1f;

            int direction = playerAnimator.SafeGetInteger("Direction", 0);

            // 优先使用预缓存的Hash（性能优化）
            // 简化版：只需要 direction，不再需要 quality
            int targetHash = -1;
            if (playerToolController != null)
            {
                targetHash = playerToolController.GetCachedStateHash(direction);
                // 验证Hash是否有效
                if (targetHash != -1 && toolAnimator != null && !toolAnimator.HasState(0, targetHash))
                {
                    targetHash = -1;
                }
            }

            // 如果缓存未命中，使用传统方式
            if (targetHash == -1)
            {
                int itemId = playerToolController != null ? playerToolController.CurrentItemId : toolAnimator.SafeGetInteger("ToolItemId", 0);
                targetHash = GetTargetToolStateHash(currentState, direction, itemId);
                
                // 调试：缓存未命中时输出详细信息
                if (enableToolDebug && Time.time - lastDebugTime > debugLogInterval)
                {
                    string stateName = StateIntToName(currentState);
                    string dirName = DirectionIntToName(direction);
                    string expectedClip = $"{stateName}_{dirName}_Clip_{itemId}";
                    string controllerName = toolAnimator.runtimeAnimatorController != null ? toolAnimator.runtimeAnimatorController.name : "NULL";
                    
                    Debug.Log($"<color=yellow>[工具动画] 缓存未命中！</color>\n" +
                        $"  State={currentState}({stateName}), Dir={direction}({dirName}), ItemId={itemId}\n" +
                        $"  期望状态: {expectedClip}\n" +
                        $"  Controller: {controllerName}\n" +
                        $"  Hash={targetHash}, HasState={toolAnimator.HasState(0, targetHash)}");
                    lastDebugTime = Time.time;
                }
            }

            if (targetHash != -1)
            {
                int toolCount = Mathf.Max(2, GetFrameCountForState(currentState));
                int playerCount = Mathf.Max(2, GetPlayerFrameCountForState(currentState));

                int frameIndex;
                float toolNorm;
                
                if (TryGetPlayerFrameIndex(out frameIndex))
                {
                    frameIndex = Mathf.Clamp(frameIndex, 0, playerCount - 1);
                    int mappedToolIndex = Mathf.Clamp(Mathf.RoundToInt(frameIndex * (toolCount - 1f) / (playerCount - 1f)), 0, toolCount - 1);
                    toolNorm = Mathf.Clamp01((mappedToolIndex / (float)(toolCount - 1)) + EPSILON);
                }
                else
                {
                    int approxIndex = Mathf.Clamp(Mathf.FloorToInt(pNorm * toolCount), 0, toolCount - 1);
                    toolNorm = Mathf.Clamp01((approxIndex / (float)(toolCount - 1)) + EPSILON);
                }
                
                // 最后一帧锁定：当动画接近结束时，锁定工具在最后一帧
                // 这样可以避免动画切换时工具位置跳变
                if (enableLastFrameLock && pNorm >= lastFrameLockThreshold)
                {
                    // 锁定在最后一帧（toolCount - 1）
                    toolNorm = Mathf.Clamp01(((toolCount - 1) / (float)(toolCount - 1)) + EPSILON);
                }
                
                toolAnimator.Play(targetHash, 0, toolNorm);
            }
            else
            {
                // 调试：目标状态未找到
                if (enableToolDebug && Time.time - lastDebugTime > debugLogInterval)
                {
                    int itemId = playerToolController != null ? playerToolController.CurrentItemId : toolAnimator.SafeGetInteger("ToolItemId", 0);
                    string stateName = StateIntToName(currentState);
                    string dirName = DirectionIntToName(direction);
                    string expectedClip = $"{stateName}_{dirName}_Clip_{itemId}";
                    string controllerName = toolAnimator.runtimeAnimatorController != null ? toolAnimator.runtimeAnimatorController.name : "NULL";
                    
                    Debug.LogWarning($"<color=red>[工具动画] 目标状态未找到！</color>\n" +
                        $"  State={currentState}({stateName}), Dir={direction}({dirName}), ItemId={itemId}\n" +
                        $"  期望状态: {expectedClip}\n" +
                        $"  Controller: {controllerName}\n" +
                        $"  回退到当前状态播放");
                    lastDebugTime = Time.time;
                }
                
                AnimatorStateInfo toolState = toolAnimator.GetCurrentAnimatorStateInfo(0);
                toolAnimator.Play(toolState.fullPathHash, 0, pNorm);
            }

            toolAnimator.Update(0);
        }

        // 3. 同步flipX
        SyncFlipX();

        lastState = currentState;
    }


    /// <summary>
    /// 控制Tool的显示/隐藏和图层顺序
    /// 使用延迟隐藏机制解决动画切换时的闪烁问题
    /// 工具始终显示在玩家上层（sortingOrder + 1）
    /// </summary>
    void UpdateToolVisibility(int currentState)
    {
        if (toolSpriteRenderer == null)
            return;
        
        // ★ 强制隐藏检查：如果被强制隐藏，直接返回
        if (_forceHideUntilNextAction)
        {
            toolSpriteRenderer.enabled = false;
            return;
        }
        
        // 检查当前State是否需要显示工具
        bool isToolActiveState = System.Array.IndexOf(toolActiveStates, currentState) >= 0;
        bool shouldShowTool = isToolActiveState;
        
        // 延迟隐藏逻辑：当从工具状态切换到非工具状态时，延迟几帧再隐藏
        if (isToolActiveState)
        {
            // 工具状态：重置延迟计数器和强制隐藏标志
            _lastToolActiveState = currentState;
            _isInHideDelay = false;
            _hideDelayCounter = 0;
        }
        else if (_lastToolActiveState >= 0)
        {
            // 刚从工具状态切换出来：开始延迟隐藏
            if (!_isInHideDelay)
            {
                _isInHideDelay = true;
                _hideDelayCounter = hideDelayFrames;
            }
            
            // 延迟期间保持显示
            if (_hideDelayCounter > 0)
            {
                shouldShowTool = true;
                _hideDelayCounter--;
            }
            else
            {
                // 延迟结束，真正隐藏
                _lastToolActiveState = -1;
                _isInHideDelay = false;
            }
        }
        
        toolSpriteRenderer.enabled = shouldShowTool;
        if (shouldShowTool && playerSpriteRenderer != null)
        {
            // 保持与玩家相同的 Sorting Layer
            toolSpriteRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
            
            // ★ 工具始终在玩家上层（所有状态、所有方向）
            toolSpriteRenderer.sortingOrder = playerSpriteRenderer.sortingOrder + 1;
        }
    }

    /// <summary>
    /// 同步动画参数（实时）
    /// 注意：只同步State和Direction，ToolType和ToolQuality由PlayerToolController直接设置
    /// </summary>
    void SyncParameters()
    {
        // 🎯 安全获取Player的当前参数
        int state = playerAnimator.SafeGetInteger("State", 0);
        int direction = playerAnimator.SafeGetInteger("Direction", 0);

        // 只同步State和Direction（ToolType和ToolQuality由PlayerToolController直接设置）
        toolAnimator.SafeSetInteger("State", state);
        toolAnimator.SafeSetInteger("Direction", direction);
    }
    
    /// <summary>
    /// 同步翻转状态
    /// </summary>
    void SyncFlipX()
    {
        if (playerSpriteRenderer != null && toolSpriteRenderer != null)
        {
            toolSpriteRenderer.flipX = playerSpriteRenderer.flipX;
        }
    }
    
    /// <summary>
    /// 获取状态名称（用于调试）
    /// </summary>
    string GetStateName(AnimatorStateInfo stateInfo)
    {
        // 尝试从Animator获取当前状态的Clip名称
        if (toolAnimator != null)
        {
            var clipInfo = toolAnimator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                return clipInfo[0].clip.name;
            }
        }
        return "Unknown";
    }

    /// <summary>
    /// 获取目标工具状态Hash
    /// 简化版格式：{ActionType}_{Direction}_Clip_{ItemID}
    /// </summary>
    int GetTargetToolStateHash(int state, int direction, int itemId)
    {
        string stateName = StateIntToName(state);
        if (string.IsNullOrEmpty(stateName)) return -1;
        string dirName = DirectionIntToName(direction);
        if (string.IsNullOrEmpty(dirName)) return -1;
        
        // 简化格式：不再包含 quality
        string clip = $"{stateName}_{dirName}_Clip_{itemId}";
        string path = $"Base Layer.{clip}";
        int hash = Animator.StringToHash(path);
        
        if (toolAnimator != null && toolAnimator.HasState(0, hash)) 
            return hash;
        
        return -1;
    }

    string StateIntToName(int state)
    {
        switch (state)
        {
            case 6: return "Slice";
            case 7: return "Pierce";
            case 8: return "Crush";
            default: return null;
        }
    }

    string DirectionIntToName(int direction)
    {
        switch (direction)
        {
            case 0: return "Down";
            case 1: return "Up";
            default: return "Side";
        }
    }

    int GetFrameCountForState(int state)
    {
        // 优先从当前ToolData读取帧数
        if (playerToolController != null)
        {
            return playerToolController.GetCurrentAnimationFrameCount();
        }

        // 回退到默认值
        return 8;
    }

    int GetPlayerFrameCountForState(int state)
    {
        // Player帧数通常与工具帧数一致
        if (playerToolController != null)
        {
            return playerToolController.GetCurrentAnimationFrameCount();
        }

        return 8;
    }

    bool TryGetPlayerFrameIndex(out int index)
    {
        index = 0;
        if (playerSpriteRenderer == null) return false;
        var sp = playerSpriteRenderer.sprite;
        if (sp == null) return false;
        string name = sp.name;
        if (string.IsNullOrEmpty(name)) return false;
        int end = name.Length - 1;
        int start = end;
        while (start >= 0 && char.IsDigit(name[start])) start--;
        if (start < end)
        {
            string num = name.Substring(start + 1);
            if (int.TryParse(num, out index)) return true;
        }
        int us = name.LastIndexOf('_');
        if (us >= 0 && us + 1 < name.Length)
        {
            int i = us + 1;
            while (i < name.Length && !char.IsDigit(name[i])) i++;
            if (i < name.Length)
            {
                int j = i;
                while (j < name.Length && char.IsDigit(name[j])) j++;
                string num2 = name.Substring(i, j - i);
                return int.TryParse(num2, out index);
            }
        }
        return false;
    }
}
