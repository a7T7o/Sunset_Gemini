using UnityEngine;

/// <summary>
/// 世界物品掉落动画组件
/// 负责弹性掉落动画、浮动待拾取动画、阴影同步
/// </summary>
public class WorldItemDrop : MonoBehaviour
{
    #region 动画参数

    [Header("弹跳参数")]
    [Tooltip("初始弹出高度")]
    [SerializeField] private float bounceHeight = 0.8f;
    
    [Tooltip("弹跳衰减系数")]
    [SerializeField] private float bounceDecay = 0.5f;
    
    [Tooltip("弹跳次数")]
    [SerializeField] private int maxBounceCount = 3;
    
    [Tooltip("重力加速度")]
    [SerializeField] private float gravity = 15f;

    [Header("浮动参数")]
    [Tooltip("浮动幅度")]
    [SerializeField] private float idleFloatAmplitude = 0.03f;
    
    [Tooltip("浮动速度")]
    [SerializeField] private float idleFloatSpeed = 2.5f;

    [Header("阴影呼吸参数")]
    [Tooltip("阴影最小缩放比例（物品最高时）")]
    [SerializeField] private float shadowMinScaleRatio = 0.85f;
    
    [Tooltip("阴影最大缩放比例（物品最低时）")]
    [SerializeField] private float shadowMaxScaleRatio = 1.0f;
    
    [Tooltip("阴影最小透明度（物品最高时）")]
    [SerializeField] private float shadowMinAlpha = 0.25f;
    
    [Tooltip("阴影最大透明度（物品最低时）")]
    [SerializeField] private float shadowMaxAlpha = 0.4f;

    [Header("性能优化")]
    [Tooltip("动画激活距离")]
    [SerializeField] private float animationActiveDistance = 15f;
    
    [Tooltip("距离检测间隔")]
    [SerializeField] private float distanceCheckInterval = 0.5f;

    #endregion

    #region 引用

    [Header("组件引用（自动获取）")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private SpriteRenderer shadowRenderer;

    #endregion

    #region 状态

    public enum DropState
    {
        Idle,       // 浮动待拾取
        Bouncing,   // 弹跳中
        Paused      // 暂停（距离优化）
    }

    private DropState _currentState = DropState.Idle;
    private float _verticalVelocity;
    private float _currentHeight;
    private int _currentBounceCount;
    private float _currentBounceHeight;
    private float _lastDistanceCheckTime;
    private Transform _playerTransform;
    private Vector3 _shadowInitialScale;
    private Vector3 _spriteInitialLocalPos;
    private float _idlePhase;
    private Color _shadowBaseColor;

    public DropState CurrentState => _currentState;

    #endregion

    #region Unity生命周期

    private void Awake()
    {
        // 自动获取组件引用
        if (spriteTransform == null)
            spriteTransform = transform.Find("Sprite");
        
        if (shadowTransform == null)
            shadowTransform = transform.Find("Shadow");
        
        if (shadowTransform != null)
        {
            _shadowInitialScale = shadowTransform.localScale;
            if (shadowRenderer == null)
                shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
            if (shadowRenderer != null)
                _shadowBaseColor = shadowRenderer.color;
        }
        
        if (spriteTransform != null)
            _spriteInitialLocalPos = spriteTransform.localPosition;
    }

    private void Start()
    {
        // 查找玩家
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerTransform = player.transform;
        
        _idlePhase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        // 距离检测优化
        if (Time.time - _lastDistanceCheckTime > distanceCheckInterval)
        {
            _lastDistanceCheckTime = Time.time;
            CheckDistanceAndUpdateState();
        }

        switch (_currentState)
        {
            case DropState.Bouncing:
                UpdateBouncing();
                break;
            case DropState.Idle:
                UpdateIdle();
                break;
            case DropState.Paused:
                break;
        }

        UpdateShadow();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 开始掉落动画
    /// </summary>
    public void StartDrop(Vector3 direction, float force = 1f)
    {
        _currentHeight = 0f;
        _currentBounceCount = 0;
        _currentBounceHeight = bounceHeight;
        _verticalVelocity = Mathf.Sqrt(2f * gravity * _currentBounceHeight);
        _currentState = DropState.Bouncing;

        if (direction.sqrMagnitude > 0.001f)
        {
            Vector3 horizontalOffset = direction.normalized * force * 0.3f;
            horizontalOffset.z = 0f;
            transform.position += horizontalOffset;
        }
    }

    /// <summary>
    /// 设置暂停状态
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (paused && _currentState != DropState.Paused)
        {
            _currentState = DropState.Paused;
        }
        else if (!paused && _currentState == DropState.Paused)
        {
            _currentState = DropState.Idle;
        }
    }

    /// <summary>
    /// 停止动画并重置
    /// </summary>
    public void StopAnimation()
    {
        _currentState = DropState.Idle;
        _currentHeight = 0f;
        if (spriteTransform != null)
            spriteTransform.localPosition = _spriteInitialLocalPos;
    }

    #endregion

    #region 私有方法

    private void UpdateBouncing()
    {
        _verticalVelocity -= gravity * Time.deltaTime;
        _currentHeight += _verticalVelocity * Time.deltaTime;

        if (_currentHeight <= 0f)
        {
            _currentHeight = 0f;
            _currentBounceCount++;

            if (_currentBounceCount >= maxBounceCount)
            {
                _currentState = DropState.Idle;
                _currentHeight = 0f;
            }
            else
            {
                _currentBounceHeight *= bounceDecay;
                _verticalVelocity = Mathf.Sqrt(2f * gravity * _currentBounceHeight);
            }
        }

        if (spriteTransform != null)
        {
            spriteTransform.localPosition = _spriteInitialLocalPos + new Vector3(0f, _currentHeight, 0f);
        }
    }

    private void UpdateIdle()
    {
        _idlePhase += idleFloatSpeed * Time.deltaTime;
        float floatOffset = Mathf.Sin(_idlePhase) * idleFloatAmplitude;

        if (spriteTransform != null)
        {
            spriteTransform.localPosition = _spriteInitialLocalPos + new Vector3(0f, floatOffset, 0f);
        }

        _currentHeight = floatOffset;
    }

    private void UpdateShadow()
    {
        if (shadowTransform == null) return;

        // 计算高度比例（用于阴影呼吸）
        // 浮动时 _currentHeight 在 [-amplitude, +amplitude] 范围
        // 弹跳时 _currentHeight 在 [0, bounceHeight] 范围
        float maxHeight = _currentState == DropState.Bouncing ? bounceHeight : idleFloatAmplitude;
        float heightRatio = Mathf.Clamp01(Mathf.Abs(_currentHeight) / Mathf.Max(maxHeight, 0.01f));
        
        // 阴影缩放：物品越高，阴影越小
        float scaleRatio = Mathf.Lerp(shadowMaxScaleRatio, shadowMinScaleRatio, heightRatio);
        shadowTransform.localScale = _shadowInitialScale * scaleRatio;

        // 阴影透明度：物品越高，阴影越淡
        if (shadowRenderer != null)
        {
            float alpha = Mathf.Lerp(shadowMaxAlpha, shadowMinAlpha, heightRatio);
            Color c = _shadowBaseColor;
            c.a = alpha;
            shadowRenderer.color = c;
        }
    }

    private void CheckDistanceAndUpdateState()
    {
        if (_playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        
        if (distance > animationActiveDistance)
        {
            if (_currentState != DropState.Paused && _currentState != DropState.Bouncing)
            {
                _currentState = DropState.Paused;
            }
        }
        else
        {
            if (_currentState == DropState.Paused)
            {
                _currentState = DropState.Idle;
            }
        }
    }

    #endregion

    #region 编辑器

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            if (spriteTransform == null)
                spriteTransform = transform.Find("Sprite");
            if (shadowTransform == null)
                shadowTransform = transform.Find("Shadow");
        };
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, animationActiveDistance);
    }
#endif

    #endregion
}
