using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 精力系统 - 管理玩家精力值
/// 工具使用成功时消耗精力，武器使用不消耗精力
/// </summary>
public class EnergySystem : MonoBehaviour
{
    #region 常量
    private const int DEFAULT_MAX_ENERGY = 150;
    #endregion

    #region 静态成员
    public static EnergySystem Instance { get; private set; }
    
    /// <summary>
    /// 精力变化事件 (当前值, 最大值)
    /// </summary>
    public static event Action<int, int> OnEnergyChanged;
    
    /// <summary>
    /// 精力耗尽事件
    /// </summary>
    public static event Action OnEnergyDepleted;
    #endregion

    #region 序列化字段
    [Header("=== 精力配置 ===")]
    [Tooltip("最大精力值")]
    [SerializeField] private int maxEnergy = DEFAULT_MAX_ENERGY;

    [Tooltip("当前精力值")]
    [SerializeField] private int currentEnergy;

    [Header("=== UI 引用 ===")]
    [Tooltip("精力条 Slider (UI/State/EP)")]
    [SerializeField] private Slider energySlider;

    [Header("=== 调试 ===")]
    [SerializeField] private bool showDebugInfo = false;
    #endregion

    #region 公共属性
    public int MaxEnergy => maxEnergy;
    public int CurrentEnergy => currentEnergy;
    public float EnergyPercent => maxEnergy > 0 ? (float)currentEnergy / maxEnergy : 0f;
    public bool IsExhausted => currentEnergy <= 0;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentEnergy = maxEnergy;
    }

    private void Start()
    {
        if (energySlider == null)
            TryFindEnergySlider();
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion


    #region 公共方法
    /// <summary>
    /// 尝试消耗精力（成功操作时调用）
    /// </summary>
    public bool TryConsumeEnergy(int amount)
    {
        if (amount <= 0)
            return true;

        if (currentEnergy < amount)
        {
            if (showDebugInfo)
                Debug.Log($"<color=yellow>[EnergySystem] 精力不足！需要 {amount}，当前 {currentEnergy}</color>");
            return false;
        }

        currentEnergy -= amount;
        
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[EnergySystem] 消耗精力 {amount}，剩余 {currentEnergy}/{maxEnergy}</color>");

        UpdateUI();
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

        if (currentEnergy <= 0)
        {
            OnEnergyDepleted?.Invoke();
            if (showDebugInfo)
                Debug.Log($"<color=red>[EnergySystem] 精力耗尽！</color>");
        }

        return true;
    }

    /// <summary>
    /// 恢复精力
    /// </summary>
    public void RestoreEnergy(int amount)
    {
        if (amount <= 0)
            return;

        int oldEnergy = currentEnergy;
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);

        if (showDebugInfo)
            Debug.Log($"<color=green>[EnergySystem] 恢复精力 {currentEnergy - oldEnergy}，当前 {currentEnergy}/{maxEnergy}</color>");

        UpdateUI();
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    /// <summary>
    /// 完全恢复精力（睡觉时调用）
    /// </summary>
    public void FullRestore()
    {
        currentEnergy = maxEnergy;

        if (showDebugInfo)
            Debug.Log($"<color=green>[EnergySystem] 精力完全恢复！{currentEnergy}/{maxEnergy}</color>");

        UpdateUI();
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    /// <summary>
    /// 设置最大精力值
    /// </summary>
    public void SetMaxEnergy(int newMax)
    {
        maxEnergy = Mathf.Max(1, newMax);
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        UpdateUI();
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    /// <summary>
    /// 检查是否有足够精力
    /// </summary>
    public bool HasEnoughEnergy(int amount)
    {
        return currentEnergy >= amount;
    }
    #endregion

    #region 私有方法
    private void UpdateUI()
    {
        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value = currentEnergy;
        }
    }

    private void TryFindEnergySlider()
    {
        var uiRoot = GameObject.Find("UI");
        if (uiRoot != null)
        {
            var stateTransform = uiRoot.transform.Find("State");
            if (stateTransform != null)
            {
                var epTransform = stateTransform.Find("EP");
                if (epTransform != null)
                {
                    energySlider = epTransform.GetComponent<Slider>();
                    if (energySlider != null && showDebugInfo)
                        Debug.Log($"<color=cyan>[EnergySystem] 自动找到 EP Slider</color>");
                }
            }
        }
    }
    #endregion

#if UNITY_EDITOR
    #region 编辑器方法
    [ContextMenu("测试 - 消耗10点精力")]
    private void DEBUG_ConsumeEnergy() => TryConsumeEnergy(10);

    [ContextMenu("测试 - 恢复50点精力")]
    private void DEBUG_RestoreEnergy() => RestoreEnergy(50);

    [ContextMenu("测试 - 完全恢复")]
    private void DEBUG_FullRestore() => FullRestore();
    #endregion
#endif
}
