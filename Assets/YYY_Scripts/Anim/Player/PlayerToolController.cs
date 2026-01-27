using UnityEngine;
using FarmGame.Data;
using System.Collections.Generic;

/// <summary>
/// 玩家工具控制器
/// 负责切换和控制工具动画
/// 与PlayerAnimController协同工作
/// 数据驱动：从ToolData直接读取AnimatorController和动画配置
/// 
/// 动画命名规范（简化版）：
/// - 每个品质的工具都是独立 ItemID
/// - 动画状态名格式：{ActionType}_{Direction}_Clip_{ItemID}
/// - 例如：Slice_Down_Clip_0（ItemID=0的斧头）
/// </summary>
[RequireComponent(typeof(PlayerAnimController))]
public class PlayerToolController : MonoBehaviour
{
    [Header("━━━━ 组件引用 ━━━━")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator toolAnimator;
    [SerializeField] private SpriteRenderer toolSpriteRenderer;
    [SerializeField] private ToolData equippedToolData;
    [SerializeField] private WeaponData equippedWeaponData;

    [Header("━━━━ 当前装备 ━━━━")]
    [Tooltip("当前工具类型")]
    [SerializeField] private int currentToolType = 0;

    [Tooltip("当前工具 ItemID")]
    [SerializeField] private int currentItemId = -1;

    [Header("━━━━ 调试 ━━━━")]
    [Tooltip("启用工具装备调试日志")]
    [SerializeField] private bool enableDebug = true;

    // 预缓存的状态Hash（避免运行时字符串拼接）
    // Key: direction (0=Down, 1=Up, 2=Side)
    private Dictionary<int, int> cachedStateHashes = new Dictionary<int, int>();

    // 公开当前ToolData供LayerAnimSync读取
    public ToolData CurrentToolData => equippedToolData;
    public WeaponData CurrentWeaponData => equippedWeaponData;
    public Animator ToolAnimator => toolAnimator;
    public SpriteRenderer ToolSpriteRenderer => toolSpriteRenderer;
    public int CurrentItemId => currentItemId;
    
    // ========== 初始化 ==========

    void Start()
    {
        // 如果有初始装备的ToolData，进行初始化
        if (equippedToolData != null)
        {
            EquipToolData(equippedToolData);
        }
    }

    // ========== 公共接口 ==========

    /// <summary>
    /// 装备工具（数据驱动版本）
    /// 直接从ToolData读取AnimatorController
    /// 动画状态名格式（简化版）：{ActionType}_{Direction}_Clip_{ItemID}
    /// 每个品质的工具都是独立 ItemID，不再需要 quality 参数
    /// </summary>
    public void EquipToolData(ToolData data)
    {
        equippedWeaponData = null;
        equippedToolData = data;

        if (data == null || toolAnimator == null)
            return;

        // 1. 直接从ToolData获取Controller并设置
        if (data.animatorController != null)
        {
            toolAnimator.runtimeAnimatorController = data.animatorController;
        }
        else
        {
            Debug.LogWarning($"[PlayerToolController] {data.itemName} 没有配置 animatorController！");
        }

        // 2. 获取动画ID（直接使用 itemID）
        currentToolType = (int)data.toolType;
        currentItemId = data.GetAnimationId();

        // 3. 设置 Animator 参数
        toolAnimator.SafeSetInteger("ToolItemId", currentItemId);

        // 4. 预缓存状态Hash（性能优化）
        CacheStateHashes(data, currentItemId);

        if (enableDebug)
        {
            string actionName = data.GetAnimActionName();
            Debug.Log($"<color=cyan>[PlayerToolController] 装备工具</color>\n" +
                $"  名称: {data.itemName}\n" +
                $"  ItemID: {currentItemId}\n" +
                $"  动作类型: {actionName} (State={data.GetAnimStateValue()})\n" +
                $"  Controller: {data.animatorController?.name ?? "NULL"}\n" +
                $"  期望状态名: {actionName}_Down_Clip_{currentItemId}");
        }
    }

    /// <summary>
    /// 装备武器
    /// 动画状态名格式：{ActionType}_{Direction}_Clip_{ItemID}
    /// </summary>
    public void EquipWeaponData(WeaponData data)
    {
        equippedToolData = null;
        equippedWeaponData = data;

        if (data == null || toolAnimator == null)
            return;

        // 1. 直接从WeaponData获取Controller并设置
        if (data.animatorController != null)
        {
            toolAnimator.runtimeAnimatorController = data.animatorController;
        }
        else
        {
            Debug.LogWarning($"[PlayerToolController] {data.itemName} 没有配置 animatorController！");
        }

        // 2. 获取动画ID（直接使用 itemID）
        currentItemId = data.GetAnimationId();

        // 3. 设置 Animator 参数
        toolAnimator.SafeSetInteger("ToolItemId", currentItemId);

        // 4. 预缓存状态Hash（性能优化）
        CacheWeaponStateHashes(data, currentItemId);

        if (enableDebug)
        {
            string actionName = data.GetAnimActionName();
            Debug.Log($"<color=cyan>[PlayerToolController] 装备武器</color>\n" +
                $"  名称: {data.itemName}\n" +
                $"  ItemID: {currentItemId}\n" +
                $"  动作类型: {actionName} (State={data.GetAnimStateValue()})\n" +
                $"  Controller: {data.animatorController?.name ?? "NULL"}\n" +
                $"  期望状态名: {actionName}_Down_Clip_{currentItemId}");
        }
    }

    /// <summary>
    /// 预缓存武器状态Hash
    /// </summary>
    private void CacheWeaponStateHashes(WeaponData data, int itemId)
    {
        cachedStateHashes.Clear();

        string stateName = data.GetAnimActionName();
        string[] directions = { "Down", "Up", "Side" };

        if (enableDebug)
        {
            Debug.Log($"<color=green>[PlayerToolController] 缓存武器状态Hash</color>\n" +
                $"  动作类型: {stateName}, ItemID: {itemId}");
        }

        for (int dir = 0; dir < 3; dir++)
        {
            string dirName = directions[dir];
            // 简化格式：{ActionType}_{Direction}_Clip_{ItemID}
            string clip = $"{stateName}_{dirName}_Clip_{itemId}";
            string path = $"Base Layer.{clip}";
            int hash = Animator.StringToHash(path);
            cachedStateHashes[dir] = hash;
            
            // 验证状态是否存在于Controller中
            if (enableDebug && toolAnimator != null && toolAnimator.runtimeAnimatorController != null)
            {
                bool hasState = toolAnimator.HasState(0, hash);
                string status = hasState ? "<color=green>✓</color>" : "<color=red>✗</color>";
                Debug.Log($"  {status} {clip} (Hash={hash})");
            }
        }
    }

    /// <summary>
    /// 卸下当前装备
    /// </summary>
    public void UnequipCurrent()
    {
        equippedToolData = null;
        equippedWeaponData = null;
        currentItemId = -1;
        cachedStateHashes.Clear();

        if (enableDebug)
        {
            Debug.Log("<color=yellow>[PlayerToolController] 卸下装备</color>");
        }
    }

    /// <summary>
    /// 预缓存状态Hash，避免运行时字符串拼接
    /// 简化版：只缓存 direction，不再需要 quality
    /// </summary>
    private void CacheStateHashes(ToolData data, int itemId)
    {
        cachedStateHashes.Clear();

        string stateName = data.GetAnimActionName();
        string[] directions = { "Down", "Up", "Side" };

        if (enableDebug)
        {
            Debug.Log($"<color=green>[PlayerToolController] 缓存状态Hash</color>\n" +
                $"  动作类型: {stateName}, ItemID: {itemId}");
        }

        for (int dir = 0; dir < 3; dir++)
        {
            string dirName = directions[dir];
            // 简化格式：{ActionType}_{Direction}_Clip_{ItemID}
            string clip = $"{stateName}_{dirName}_Clip_{itemId}";
            string path = $"Base Layer.{clip}";
            int hash = Animator.StringToHash(path);
            cachedStateHashes[dir] = hash;
            
            // 验证状态是否存在于Controller中
            if (enableDebug && toolAnimator != null && toolAnimator.runtimeAnimatorController != null)
            {
                bool hasState = toolAnimator.HasState(0, hash);
                string status = hasState ? "<color=green>✓</color>" : "<color=red>✗</color>";
                Debug.Log($"  {status} {clip} (Hash={hash})");
            }
        }
    }

    /// <summary>
    /// 获取预缓存的状态Hash
    /// 简化版：只需要 direction 参数
    /// </summary>
    public int GetCachedStateHash(int direction)
    {
        // direction: 0=Down, 1=Up, 2=Side
        int dirIndex = direction == 0 ? 0 : direction == 1 ? 1 : 2;
        if (cachedStateHashes.TryGetValue(dirIndex, out int hash))
            return hash;
        return -1;
    }
    
    /// <summary>
    /// 获取当前工具类型
    /// </summary>
    public int GetCurrentToolType()
    {
        return currentToolType;
    }

    /// <summary>
    /// 获取当前工具的动画帧数
    /// </summary>
    public int GetCurrentAnimationFrameCount()
    {
        if (equippedToolData != null)
            return equippedToolData.animationFrameCount;
        return 8; // 默认值
    }

    /// <summary>
    /// 获取当前工具的动画State值
    /// </summary>
    public int GetCurrentAnimStateValue()
    {
        if (equippedToolData != null)
            return equippedToolData.GetAnimStateValue();
        return 6; // 默认Slice
    }
}

