using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("组件引用")]
    private Rigidbody2D rb;
    private PlayerAnimController animController;
    private PlayerInteraction playerInteraction;
    private SprintStateManager sprintManager;

    [Header("移动速度")]
    public float RunSpeed = 4f;
    public float WalkSpeed = 2f;
    
    private Vector2 movementInput;
    private bool isShiftHeld = false;
    
    // 当前朝向（始终根据输入更新，不受动作影响）
    private PlayerAnimController.AnimDirection currentFacingDirection = PlayerAnimController.AnimDirection.Down;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animController = GetComponent<PlayerAnimController>();
        playerInteraction = GetComponent<PlayerInteraction>();
        
        // 获取疾跑状态管理器（如果不存在则创建）
        sprintManager = SprintStateManager.Instance;
        if (sprintManager == null)
        {
            GameObject managerObj = new GameObject("SprintStateManager");
            sprintManager = managerObj.AddComponent<SprintStateManager>();
        }
    }

    void Update()
    {
        // 如果正在执行动作，禁止移动和朝向更新
        if (playerInteraction != null && playerInteraction.IsPerformingAction())
        {
            rb.linearVelocity = Vector2.zero;
            // 动作期间不更新朝向，方向输入由 ToolActionLockManager 缓存
            return;
        }
        
        // 非动作状态：正常更新朝向
        UpdateFacingDirection();

        // 正常移动和动画
        UpdateMovement();
        UpdateAnimation();
    }
    
    private void UpdateMovement()
    {
        float currentSpeed = isShiftHeld ? RunSpeed : WalkSpeed;
        rb.linearVelocity = movementInput.normalized * currentSpeed;
    }

    private void UpdateAnimation()
    {
        if (animController == null) return;
        
        // 获取当前方向
        PlayerAnimController.AnimDirection direction = movementInput.magnitude < 0.01f 
            ? animController.GetCurrentDirection() 
            : GetDirection();
        
        bool shouldFlip = (direction == PlayerAnimController.AnimDirection.Left);
        
        // 检查是否在Carry状态
        bool isCarrying = playerInteraction != null && playerInteraction.IsCarrying();
        
        if (isCarrying)
        {
            // Carry状态的动画
            if (movementInput.magnitude < 0.01f)
            {
                animController.PlayCarry(PlayerAnimController.CarryState.Idle, direction, shouldFlip);
            }
            else if (isShiftHeld)
            {
                animController.PlayCarry(PlayerAnimController.CarryState.Run, direction, shouldFlip);
            }
            else
            {
                animController.PlayCarry(PlayerAnimController.CarryState.Walk, direction, shouldFlip);
            }
        }
        else
        {
            // 普通移动动画
            if (movementInput.magnitude < 0.01f)
            {
                animController.PlayIdle(direction, shouldFlip);
            }
            else if (isShiftHeld)
            {
                animController.PlayRun(direction, shouldFlip);
            }
            else
            {
                animController.PlayWalk(direction, shouldFlip);
            }
        }
    }

    private PlayerAnimController.AnimDirection GetDirection()
    {
        if (Mathf.Abs(movementInput.y) > Mathf.Abs(movementInput.x))
        {
            return movementInput.y > 0 ? 
                PlayerAnimController.AnimDirection.Up : 
                PlayerAnimController.AnimDirection.Down;
        }
        else
        {
            return movementInput.x > 0 ? 
                PlayerAnimController.AnimDirection.Right : 
                PlayerAnimController.AnimDirection.Left;
        }
    }

    public void SetMovementInput(Vector2 input, bool isShifted)
    {
        SetMovementInput(input, isShifted, null);
    }

    /// <summary>
    /// 设置移动输入，可选指定朝向方向
    /// </summary>
    /// <param name="input">移动输入</param>
    /// <param name="isShifted">是否疾跑</param>
    /// <param name="facingDirection">可选的朝向方向（用于斜向移动时固定朝向）</param>
    public void SetMovementInput(Vector2 input, bool isShifted, Vector2? facingDirection)
    {
        movementInput = input;
        
        // ✅ 从 SprintStateManager 获取统一的疾跑状态
        if (sprintManager != null)
        {
            // 通知 SprintStateManager 移动状态
            bool hasInput = input.magnitude > 0.01f;
            sprintManager.OnMovementInput(hasInput);
            
            isShiftHeld = sprintManager.IsSprinting();
        }
        else
        {
            isShiftHeld = isShifted;
        }
        
        // 🔥 如果指定了朝向方向，使用指定的朝向
        if (facingDirection.HasValue && facingDirection.Value.sqrMagnitude > 0.01f)
        {
            SetFacingDirection(facingDirection.Value);
        }
    }

    private void UpdateFacingDirection()
    {
        // 有输入就更新朝向（不管是否在动作中）
        if (movementInput.magnitude > 0.01f)
        {
            currentFacingDirection = GetDirection();
        }
    }
    
    public PlayerAnimController.AnimDirection GetCurrentFacingDirection()
    {
        return currentFacingDirection;
    }
    
    /// <summary>
    /// 设置玩家朝向（用于缓存方向应用）
    /// </summary>
    /// <param name="direction">方向向量</param>
    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;
        
        // 将方向向量转换为 AnimDirection
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            currentFacingDirection = direction.y > 0 
                ? PlayerAnimController.AnimDirection.Up 
                : PlayerAnimController.AnimDirection.Down;
        }
        else
        {
            currentFacingDirection = direction.x > 0 
                ? PlayerAnimController.AnimDirection.Right 
                : PlayerAnimController.AnimDirection.Left;
        }
    }

    public void StopMovement()
    {
        movementInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }
}
