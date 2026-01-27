using UnityEngine;

public class PlayerAnimController : MonoBehaviour
{

    public enum AnimState
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Carry = 3,
        Collect = 4,
        Hit = 5,
        Slice = 6,
        Pierce = 7,
        Crush = 8,
        Fish = 9,
        Watering = 10,
        Death = 11
    }
    
    public enum AnimDirection
    {
        Down = 0,
        Up = 1,
        Right = 2,
        Left = 3
    }
    
    public enum CarryState
    {
        Idle = 0,
        Walk = 1,
        Run = 2
    }
    
    [Header("组件引用")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform shadowTransform;
    
    [Header("调试设置")]
    [SerializeField, Range(0.1f, 3f), Tooltip("动画播放速度倍率（1.0=正常，<1慢放，>1快放）")]
    private float animationSpeedMultiplier = 1f;
    
    [SerializeField, Tooltip("显示动画调用日志")]
    private bool showAnimationCallLog = false;
    
    private AnimState currentState = AnimState.Idle;
    private AnimDirection currentDirection = AnimDirection.Down;
    private CarryState currentCarryState = CarryState.Idle;
    private bool isFlipped = false;

    // 延迟初始化标志
    private bool isInitialized = false;
    
    private void Awake()
    {
        // 延迟到第一帧Update时初始化，避免Graphs错误
        isInitialized = false;
    }

    private void Update()
    {
        // 在第一帧Update时才初始化
        if (!isInitialized)
        {
            InitializeComponents();
            isInitialized = true;
        }
    }

    private void InitializeComponents()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (shadowTransform == null) shadowTransform = transform.Find("Shadow");

        ApplyAnimationSpeed();
    }
    
    private void Start()
    {
        PlayAnimation(AnimState.Idle, AnimDirection.Down);
    }
    
    private void ApplyAnimationSpeed()
    {
        if (animator != null)
        {
            animator.speed = animationSpeedMultiplier;
        }
    }
    
    public void PlayAnimation(AnimState state, AnimDirection direction, bool shouldFlip = false)
    {
        if (state == currentState && direction == currentDirection && shouldFlip == isFlipped)
            return;

        PlayAnimationInternal(state, direction, shouldFlip);
    }
    
    /// <summary>
    /// 连续动作播放（用于长按连续使用工具）
    /// 
    /// 核心逻辑：
    /// - 方向改变时：切换到新方向的动画
    /// - 方向不变时：什么都不做，让动画自然循环
    /// 
    /// 这样可以避免"抽搐"问题，因为不会强制重置动画
    /// </summary>
    public void ForcePlayAnimation(AnimState state, AnimDirection direction, bool shouldFlip = false)
    {
        // 检查方向是否改变
        bool directionChanged = (direction != currentDirection) || (shouldFlip != isFlipped);
        
        if (directionChanged)
        {
            // 方向改变：正常切换动画（Animator会自动处理过渡）
            PlayAnimationInternal(state, direction, shouldFlip);
        }
        // 方向不变：什么都不做，让动画自然循环
        // 这是解决"抽搐"问题的关键！
    }
    
    private void PlayAnimationInternal(AnimState state, AnimDirection direction, bool shouldFlip)
    {
        if (showAnimationCallLog)
        {
            Debug.Log($"[AnimController] PlayAnimation: {state} - {direction} (Flip:{shouldFlip}) | Frame:{Time.frameCount}");
        }

        currentState = state;
        currentDirection = direction;

        int animatorDirection = ConvertToAnimatorDirection(direction);

        if (animator != null)
        {
            animator.SetInteger("State", (int)state);
            animator.SetInteger("Direction", animatorDirection);
        }

        HandleFlip(direction, shouldFlip);
    }
    
    private int ConvertToAnimatorDirection(AnimDirection direction)
    {
        return direction switch
        {
            AnimDirection.Down => 0,
            AnimDirection.Up => 1,
            AnimDirection.Right => 2,
            AnimDirection.Left => 2,
            _ => 0
        };
    }
    
    private void HandleFlip(AnimDirection direction, bool shouldFlip)
    {
        if (direction == AnimDirection.Right || direction == AnimDirection.Left)
        {
            bool needFlip = direction == AnimDirection.Left || shouldFlip;
            SetFlip(needFlip);
        }
    }
    
    public void PlayIdle(AnimDirection direction, bool shouldFlip = false)
    {
        PlayAnimation(AnimState.Idle, direction, shouldFlip);
    }
    
    public void PlayWalk(AnimDirection direction, bool shouldFlip = false)
    {
        PlayAnimation(AnimState.Walk, direction, shouldFlip);
    }
    
    public void PlayRun(AnimDirection direction, bool shouldFlip = false)
    {
        PlayAnimation(AnimState.Run, direction, shouldFlip);
    }
    
    public void PlayCarry(CarryState carryState, AnimDirection direction, bool shouldFlip = false)
    {
        // 缓存检查 - 避免重复设置
        if (currentState == AnimState.Carry && 
            currentCarryState == carryState && 
            currentDirection == direction && 
            isFlipped == shouldFlip)
            return;
        
        if (showAnimationCallLog)
        {
            Debug.Log($"[AnimController] PlayCarry: {carryState} - {direction} (Flip:{shouldFlip}) | Frame:{Time.frameCount}");
        }
        
        // 更新当前状态和方向
        currentState = AnimState.Carry;
        currentDirection = direction;
        currentCarryState = carryState;

        if (animator != null)
        {
            animator.SetInteger("State", (int)AnimState.Carry);
            animator.SetInteger("Direction", ConvertToAnimatorDirection(direction));
            animator.SetInteger("Type", (int)carryState);
        }
        HandleFlip(direction, shouldFlip);
    }
    
    private void SetFlip(bool flipLeft)
    {
        if (spriteRenderer == null || isFlipped == flipLeft) return;
        
        isFlipped = flipLeft;
        spriteRenderer.flipX = flipLeft;
        
        if (shadowTransform != null)
        {
            Vector3 scale = shadowTransform.localScale;
            scale.x = flipLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            shadowTransform.localScale = scale;
            
            Vector3 pos = shadowTransform.localPosition;
            pos.x = flipLeft ? 0.14f : 0.09f;
            shadowTransform.localPosition = pos;
        }
    }
    
    public AnimState GetCurrentState() => currentState;
    public AnimDirection GetCurrentDirection() => currentDirection;
    public CarryState GetCurrentCarryState() => currentCarryState;
    public bool IsFlipped() => isFlipped;
    
    public bool IsMoving()
    {
        return currentState == AnimState.Walk || 
               currentState == AnimState.Run || 
               (currentState == AnimState.Carry && currentCarryState != CarryState.Idle);
    }
    
    [Header("动画时长设置")]
    [SerializeField, Tooltip("工具动画的固定时长（秒）\n根据动画剪辑的实际时长设置")]
    private float toolAnimationDuration = 0.8f;
    
    // 动画计时器
    private float _actionStartTime = 0f;
    private bool _isPerformingToolAction = false;
    
    /// <summary>
    /// 开始工具动作计时
    /// </summary>
    public void StartAnimationTracking()
    {
        _actionStartTime = Time.time;
        _isPerformingToolAction = true;
    }
    
    /// <summary>
    /// 停止工具动作计时
    /// </summary>
    public void StopAnimationTracking()
    {
        _isPerformingToolAction = false;
    }
    
    /// <summary>
    /// 判断动画是否完成
    /// 使用简单的时间计时器，不依赖 Animator 的 normalizedTime
    /// </summary>
    public bool IsAnimationFinished()
    {
        if (!_isPerformingToolAction) return true;
        
        float elapsed = Time.time - _actionStartTime;
        return elapsed >= toolAnimationDuration;
    }
    
    /// <summary>
    /// 获取当前动画进度 (0-1)
    /// </summary>
    public float GetAnimationProgress()
    {
        if (!_isPerformingToolAction) return 0f;
        
        float elapsed = Time.time - _actionStartTime;
        return Mathf.Clamp01(elapsed / toolAnimationDuration);
    }
    
#if UNITY_EDITOR
    [ContextMenu("测试/播放待机-下")]
    private void TestIdleDown() => PlayIdle(AnimDirection.Down);
    
    [ContextMenu("测试/播放行走-右")]
    private void TestWalkRight() => PlayWalk(AnimDirection.Right);
    
    [ContextMenu("测试/播放跑步-左")]
    private void TestRunLeft() => PlayRun(AnimDirection.Left);
    
    [ContextMenu("测试/Carry行走-右")]
    private void TestCarryWalkRight() => PlayCarry(CarryState.Walk, AnimDirection.Right);
    
    private void OnValidate()
    {
        // 延迟执行，避免Graphs错误
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return; // 对象可能已被销毁

            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            // 编辑器模式下也应用速度，方便实时调试
            ApplyAnimationSpeed();
        };
    }
#endif
}

