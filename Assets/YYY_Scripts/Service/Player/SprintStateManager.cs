using UnityEngine;

/// <summary>
/// 疾跑状态管理器：简化版
/// 核心逻辑：
/// 1. 按 Shift 切换奔跑状态（Toggle）
/// 2. 3秒无移动自动恢复步行
/// 3. 同步 UI 显示
/// </summary>
public class SprintStateManager : MonoBehaviour
{
    [Header("自动恢复设置")]
    [Tooltip("无移动自动恢复步行的时间（秒）")]
    [SerializeField] private float autoResetDuration = 3f;
    
    [Header("长按判定")]
    [Tooltip("长按判定阈值（秒）- 超过此时间视为长按")]
    [SerializeField] private float longPressThreshold = 0.2f;
    
    [Header("UI 引用")]
    [Tooltip("奔跑状态 UI Toggle")]
    [SerializeField] private UnityEngine.UI.Toggle sprintToggleUI;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 奔跑状态
    private bool isSprintEnabled = false;       // 当前是否奔跑
    private float lastMovementTime = 0f;        // 上次移动的时间
    
    // 长按检测
    private bool shiftPressed = false;          // Shift 是否按下
    private float shiftPressTime = 0f;          // Shift 按下的时间
    private bool wasLongPress = false;          // 是否是长按
    
    // 体力系统接口（预留）
    private IStaminaSystem staminaSystem;
    
    // 单例
    private static SprintStateManager instance;
    public static SprintStateManager Instance => instance;
    
    void Awake()
    {
        // 单例模式
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // 自动查找 UI Toggle
        if (sprintToggleUI == null)
        {
            GameObject shiftObj = GameObject.Find("Shift");
            if (shiftObj != null)
            {
                sprintToggleUI = shiftObj.GetComponent<UnityEngine.UI.Toggle>();
                if (sprintToggleUI != null && showDebugInfo)
                {
                    Debug.Log("<color=cyan>[奔跑] 自动找到 UI Toggle: Shift</color>");
                }
            }
        }
    }
    
    void Update()
    {
        // ✅ 统一处理 Shift 按键逻辑
        HandleShiftInput();
        
        // ✅ 3秒无移动自动恢复步行
        CheckAutoReset();
    }
    
    /// <summary>
    /// 统一处理 Shift 按键逻辑：支持单击和长按
    /// </summary>
    private void HandleShiftInput()
    {
        bool shiftDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        bool shiftUp = Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift);
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        // Shift 按下
        if (shiftDown)
        {
            shiftPressed = true;
            shiftPressTime = Time.time;
            wasLongPress = false;
        }
        
        // Shift 松开
        if (shiftUp)
        {
            float pressDuration = Time.time - shiftPressTime;
            
            if (pressDuration >= longPressThreshold)
            {
                // 长按：松开时恢复步行
                wasLongPress = true;
                SetSprintEnabled(false);
                
                if (showDebugInfo)
                {
                    Debug.Log("<color=orange>[奔跑] Shift 长按松开 - 恢复步行</color>");
                }
            }
            else if (!wasLongPress)
            {
                // 短按（单击）：Toggle 切换
                SetSprintEnabled(!isSprintEnabled);
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>[奔跑] Shift 单击 - 切换到{(isSprintEnabled ? "奔跑" : "步行")}</color>");
                }
            }
            
            shiftPressed = false;
        }
        
        // Shift 持续按住
        if (shiftPressed && shiftHeld)
        {
            float pressDuration = Time.time - shiftPressTime;
            
            // 超过阈值，激活长按奔跑
            if (pressDuration >= longPressThreshold && !wasLongPress)
            {
                wasLongPress = true;
                SetSprintEnabled(true);
                
                if (showDebugInfo)
                {
                    Debug.Log("<color=lime>[奔跑] Shift 长按激活 - 开始奔跑</color>");
                }
            }
        }
    }
    
    /// <summary>
    /// 检查自动恢复步行
    /// </summary>
    private void CheckAutoReset()
    {
        // 如果当前是奔跑状态，且超过3秒无移动，自动恢复步行
        if (isSprintEnabled && Time.time - lastMovementTime > autoResetDuration)
        {
            SetSprintEnabled(false);
            
            if (showDebugInfo)
            {
                Debug.Log("<color=orange>[奔跑] 3秒无移动 - 自动恢复步行</color>");
            }
        }
    }
    
    /// <summary>
    /// 设置奔跑状态
    /// </summary>
    private void SetSprintEnabled(bool enabled)
    {
        // 检查体力系统（如果存在）
        if (enabled && !CanSprint())
        {
            isSprintEnabled = false;
            UpdateUI();
            return;
        }
        
        isSprintEnabled = enabled;
        UpdateUI();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=lime>[奔跑] 状态更新 - {(enabled ? "奔跑" : "步行")}</color>");
        }
    }
    
    /// <summary>
    /// 更新 UI 显示
    /// </summary>
    private void UpdateUI()
    {
        if (sprintToggleUI != null)
        {
            sprintToggleUI.isOn = isSprintEnabled;
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=magenta>[奔跑] UI 更新 - Toggle.isOn = {isSprintEnabled}</color>");
            }
        }
    }
    
    /// <summary>
    /// 移动输入回调（由 PlayerMovement 和 PlayerAutoNavigator 调用）
    /// </summary>
    public void OnMovementInput(bool hasInput)
    {
        if (hasInput)
        {
            // 有移动输入，更新最后移动时间
            lastMovementTime = Time.time;
        }
    }
    
    /// <summary>
    /// 获取当前是否正在疾跑（统一状态）
    /// </summary>
    public bool IsSprinting()
    {
        return isSprintEnabled;
    }
    
    /// <summary>
    /// 获取导航是否应该疾跑
    /// </summary>
    public bool ShouldNavigationSprint()
    {
        return isSprintEnabled;
    }
    
    /// <summary>
    /// 检查是否可以疾跑（体力系统接口）
    /// </summary>
    private bool CanSprint()
    {
        // 如果体力系统存在，检查体力
        if (staminaSystem != null)
        {
            return staminaSystem.CanSprint();
        }
        
        // 否则总是允许疾跑
        return true;
    }
    
    /// <summary>
    /// 设置体力系统（预留接口）
    /// </summary>
    public void SetStaminaSystem(IStaminaSystem system)
    {
        staminaSystem = system;
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

/// <summary>
/// 体力系统接口（预留）
/// 后续实现体力系统时，让体力管理器实现此接口
/// </summary>
public interface IStaminaSystem
{
    /// <summary>
    /// 检查是否有足够体力疾跑
    /// </summary>
    bool CanSprint();
    
    /// <summary>
    /// 消耗体力
    /// </summary>
    void ConsumeStamina(float amount);
    
    /// <summary>
    /// 恢复体力
    /// </summary>
    void RegenerateStamina(float amount);
}
