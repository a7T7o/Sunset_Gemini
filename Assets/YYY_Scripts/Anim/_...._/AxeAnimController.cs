using UnityEngine;

/// <summary>
/// Axe动画控制器
/// 和Player的Animator同步，播放对应的Axe sprite动画
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class AxeAnimController : MonoBehaviour
{
    [Header("同步目标")]
    [Tooltip("Player的Animator")]
    [SerializeField] private Animator playerAnimator;
    
    [Header("组件引用")]
    private Animator axeAnimator;
    private SpriteRenderer axeSpriteRenderer;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;

    // 延迟初始化标志
    private bool isInitialized = false;
    
    void Awake()
    {
        // 延迟到第一帧Update时初始化，避免Graphs错误
        isInitialized = false;
    }

    void Update()
    {
        // 在第一帧Update时才初始化
        if (!isInitialized)
        {
            InitializeComponents();
            isInitialized = true;
        }
    }

    void InitializeComponents()
    {
        axeAnimator = GetComponent<Animator>();
        axeSpriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    void Start()
    {
        if (playerAnimator == null)
        {
            Debug.LogError($"[AxeAnim] {gameObject.name} 未设置Player Animator！", this);
        }
    }
    
    void LateUpdate()
    {
        if (playerAnimator == null || axeAnimator == null)
            return;
        
        // 同步所有Animator参数
        SyncAnimatorParameters();
    }
    
    /// <summary>
    /// 同步Animator参数
    /// </summary>
    void SyncAnimatorParameters()
    {
        // 同步State
        if (playerAnimator.HasParameter("State"))
        {
            int state = playerAnimator.GetInteger("State");
            axeAnimator.SetInteger("State", state);
        }
        
        // 同步Direction
        if (playerAnimator.HasParameter("Direction"))
        {
            int direction = playerAnimator.GetInteger("Direction");
            axeAnimator.SetInteger("Direction", direction);
        }
        
        // 同步Type
        if (playerAnimator.HasParameter("Type"))
        {
            int type = playerAnimator.GetInteger("Type");
            axeAnimator.SetInteger("Type", type);
        }
        
        // 同步DirectionX和DirectionY（如果有）
        if (playerAnimator.HasParameter("DirectionX"))
        {
            float dirX = playerAnimator.GetFloat("DirectionX");
            axeAnimator.SetFloat("DirectionX", dirX);
        }
        
        if (playerAnimator.HasParameter("DirectionY"))
        {
            float dirY = playerAnimator.GetFloat("DirectionY");
            axeAnimator.SetFloat("DirectionY", dirY);
        }
        
        if (showDebugInfo)
        {
            AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo axeStateInfo = axeAnimator.GetCurrentAnimatorStateInfo(0);
            
            if (stateInfo.fullPathHash != axeStateInfo.fullPathHash)
            {
                Debug.Log($"[AxeAnim] Player State: {stateInfo.shortNameHash}, Axe State: {axeStateInfo.shortNameHash}");
            }
        }
    }
    
    /// <summary>
    /// 切换武器sprite序列（用于切换品质）
    /// </summary>
    public void SetWeaponSpriteSet(string weaponName)
    {
        // 这里可以切换Animator Controller或者Runtime Animator Controller Override
        // 根据weaponName加载对应的斧头sprite序列
        
        if (showDebugInfo)
            Debug.Log($"[AxeAnim] 切换武器: {weaponName}");
    }
    
#if UNITY_EDITOR
    void OnValidate()
    {
        // 自动查找Player Animator（只在编辑器中，且更安全）
        if (playerAnimator == null && !Application.isPlaying)
        {
            Transform player = transform.parent?.parent; // Axe -> HandMask -> Player
            if (player != null)
            {
                // 延迟到下一帧执行，避免Graphs错误
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && playerAnimator == null)
                    {
                        playerAnimator = player.GetComponent<Animator>();
                    }
                };
            }
        }
    }
#endif
}


