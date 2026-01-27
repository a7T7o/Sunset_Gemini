using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using FarmGame.Data;
using FarmGame.Events;

/// <summary>
/// 放置管理器 V2
/// 统一管理所有物品的放置逻辑
/// 支持方块中心放置、Layer 同步、动态排序、预览 UI
/// </summary>
public class PlacementManagerV2 : MonoBehaviour
{
    #region 单例
    public static PlacementManagerV2 Instance { get; private set; }
    #endregion

    #region 事件
    /// <summary>物品放置成功事件</summary>
    public static event Action<PlacementEventData> OnItemPlaced;
    
    /// <summary>树苗种植成功事件</summary>
    public static event Action<SaplingPlantedEventData> OnSaplingPlanted;
    
    /// <summary>放置模式变化事件</summary>
    public static event Action<bool> OnPlacementModeChanged;
    #endregion

    #region 序列化字段
    [Header("━━━━ 组件引用 ━━━━")]
    [Tooltip("放置预览组件")]
    [SerializeField] private PlacementPreviewV2 placementPreview;

    [Tooltip("玩家 Transform")]
    [SerializeField] private Transform playerTransform;

    [Header("━━━━ 配置 ━━━━")]
    [Tooltip("玩家交互范围")]
    [SerializeField] private float interactionRange = 3f;

    [Tooltip("障碍物检测标签")]
    [SerializeField] private string[] obstacleTags = new string[] { "Tree", "Rock", "Building" };
    
    [Tooltip("是否启用 Layer 检测")]
    [SerializeField] private bool enableLayerCheck = true;
    
    [Tooltip("是否启用导航放置")]
    [SerializeField] private bool enableNavigationPlacement = true;

    [Header("━━━━ 排序设置 ━━━━")]
    [Tooltip("排序 Order 乘数")]
    [SerializeField] private int sortingOrderMultiplier = 100;
    
    [Tooltip("排序 Order 偏移")]
    [SerializeField] private int sortingOrderOffset = 0;

    [Header("━━━━ 音效 ━━━━")]
    [Tooltip("放置成功音效")]
    [SerializeField] private AudioClip placeSuccessSound;

    [Tooltip("放置失败音效")]
    [SerializeField] private AudioClip placeFailSound;

    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.8f;

    [Header("━━━━ 特效 ━━━━")]
    [Tooltip("放置成功特效预制体")]
    [SerializeField] private GameObject placeEffectPrefab;

    [Header("━━━━ 调试 ━━━━")]
    [SerializeField] private bool showDebugInfo = false;
    #endregion

    #region 私有字段
    private PlacementValidatorV2 validator;
    private ItemData currentPlacementItem;
    private int currentItemQuality;
    private bool isPlacementMode = false;
    private Camera mainCamera;
    private List<CellState> currentCellStates = new List<CellState>();
    private int currentDetectedLayer;
    
    // 放置历史（用于撤销）
    private List<PlacementHistoryEntry> placementHistory = new List<PlacementHistoryEntry>();
    private const int MAX_HISTORY_SIZE = 10;
    
    // 导航放置
    private bool isNavigatingToPlace = false;
    private Vector3 pendingPlacePosition;
    #endregion

    #region 属性
    /// <summary>是否处于放置模式</summary>
    public bool IsPlacementMode => isPlacementMode;
    
    /// <summary>当前放置物品</summary>
    public ItemData CurrentPlacementItem => currentPlacementItem;
    
    /// <summary>当前预览是否有效</summary>
    public bool IsCurrentPreviewValid => placementPreview != null && placementPreview.IsAllValid;
    #endregion

    #region Unity 生命周期
    private void Awake()
    {
        Debug.Log($"<color=cyan>[PlacementManagerV2] Awake() 开始</color>");
        
        // 单例设置
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"<color=red>[PlacementManagerV2] 已存在实例，销毁当前对象</color>");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"<color=green>[PlacementManagerV2] 单例设置成功</color>");

        // 初始化验证器
        validator = new PlacementValidatorV2(interactionRange);
        validator.SetObstacleTags(obstacleTags);
        validator.SetEnableLayerCheck(enableLayerCheck);
        validator.SetDebugMode(showDebugInfo);

        // 获取主摄像机
        mainCamera = Camera.main;
        
        Debug.Log($"<color=cyan>[PlacementManagerV2] Awake() 完成</color>");
    }

    private void Start()
    {
        Debug.Log($"<color=cyan>[PlacementManagerV2] Start() 开始初始化</color>");
        
        // 查找玩家
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log($"<color=green>[PlacementManagerV2] 找到玩家: {player.name}</color>");
            }
            else
            {
                Debug.LogError("[PlacementManagerV2] 未找到 Player 标签的物体！");
            }
        }

        // 创建预览组件（如果没有）
        if (placementPreview == null)
        {
            Debug.Log($"<color=yellow>[PlacementManagerV2] placementPreview 为 null，正在创建...</color>");
            CreatePlacementPreview();
            Debug.Log($"<color=green>[PlacementManagerV2] 预览组件创建完成: {placementPreview != null}</color>");
        }
        else
        {
            Debug.Log($"<color=green>[PlacementManagerV2] placementPreview 已存在: {placementPreview.gameObject.name}</color>");
        }
        
        Debug.Log($"<color=cyan>[PlacementManagerV2] Start() 初始化完成, Instance={Instance != null}</color>");
    }

    private void Update()
    {
        if (!isPlacementMode) return;

        // 更新预览位置和状态
        UpdatePreview();

        // 检查取消放置
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ExitPlacementMode();
        }

        // 检查撤销
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            UndoLastPlacement();
        }
    }
    #endregion

    #region 放置模式控制
    /// <summary>
    /// 进入放置模式
    /// </summary>
    public void EnterPlacementMode(ItemData item, int quality = 0)
    {
        Debug.Log($"<color=cyan>[PlacementManagerV2] EnterPlacementMode 被调用: {item?.itemName ?? "null"}, isPlaceable={item?.isPlaceable}</color>");
        
        if (item == null || !item.isPlaceable)
        {
            Debug.Log($"<color=yellow>[PlacementManagerV2] 物品不可放置: {item?.itemName ?? "null"}</color>");
            return;
        }

        currentPlacementItem = item;
        currentItemQuality = quality;
        isPlacementMode = true;

        Debug.Log($"<color=cyan>[PlacementManagerV2] isPlacementMode 设置为 true, placementPreview={placementPreview != null}</color>");

        // 显示预览
        if (placementPreview != null)
        {
            Debug.Log($"<color=green>[PlacementManagerV2] 调用 placementPreview.Show()</color>");
            placementPreview.Show(item);
        }
        else
        {
            Debug.LogError("[PlacementManagerV2] placementPreview 为 null！");
        }

        OnPlacementModeChanged?.Invoke(true);

        Debug.Log($"<color=green>[PlacementManagerV2] 进入放置模式: {item.itemName}</color>");
    }

    /// <summary>
    /// 退出放置模式
    /// </summary>
    public void ExitPlacementMode()
    {
        currentPlacementItem = null;
        currentItemQuality = 0;
        isPlacementMode = false;
        isNavigatingToPlace = false;

        // 隐藏预览
        if (placementPreview != null)
        {
            placementPreview.Hide();
        }

        OnPlacementModeChanged?.Invoke(false);

        if (showDebugInfo)
            Debug.Log($"<color=yellow>[PlacementManagerV2] 退出放置模式</color>");
    }
    #endregion

    #region 预览更新
    /// <summary>
    /// 更新预览位置和状态
    /// </summary>
    private void UpdatePreview()
    {
        if (placementPreview == null || currentPlacementItem == null) return;

        // 获取鼠标世界坐标
        Vector3 mousePos = GetMouseWorldPosition();
        
        // 计算方块中心
        Vector3 cellCenter = PlacementGridCalculator.GetCellCenter(mousePos);
        
        // 更新预览位置
        placementPreview.UpdatePosition(mousePos);
        
        // 同步 Sorting Layer（与玩家一致）
        if (playerTransform != null)
        {
            string sortingLayerName = PlacementLayerDetector.GetPlayerSortingLayer(playerTransform);
            placementPreview.UpdateSortingLayer(sortingLayerName);
        }

        // 验证放置位置
        var result = validator.ValidateFull(
            currentPlacementItem,
            cellCenter,
            playerTransform,
            out currentCellStates
        );
        
        currentDetectedLayer = result.DetectedLayer;

        // 更新格子状态
        if (currentCellStates != null && currentCellStates.Count > 0)
        {
            placementPreview.UpdateCellStates(currentCellStates);
        }
        else
        {
            placementPreview.SetAllCellsValid(result.IsValid);
        }
    }
    #endregion

    #region 放置执行
    /// <summary>
    /// 尝试在当前预览位置放置物品
    /// </summary>
    public bool TryPlace()
    {
        Debug.Log($"<color=cyan>[PlacementManagerV2] TryPlace() 被调用</color>");
        
        if (!isPlacementMode || currentPlacementItem == null)
        {
            Debug.Log($"<color=yellow>[PlacementManagerV2] TryPlace 失败: isPlacementMode={isPlacementMode}, currentPlacementItem={currentPlacementItem != null}</color>");
            return false;
        }

        // 检查是否在 UI 上
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log($"<color=yellow>[PlacementManagerV2] TryPlace 失败: 鼠标在 UI 上</color>");
            return false;
        }

        Vector3 placePosition = placementPreview != null
            ? placementPreview.GetPreviewPosition()
            : PlacementGridCalculator.GetCellCenter(GetMouseWorldPosition());
        
        Debug.Log($"<color=cyan>[PlacementManagerV2] TryPlace 位置: {placePosition}</color>");

        return TryPlace(placePosition);
    }

    /// <summary>
    /// 尝试在指定位置放置物品
    /// </summary>
    public bool TryPlace(Vector3 worldPosition)
    {
        Debug.Log($"<color=cyan>[PlacementManagerV2] TryPlace(Vector3) 被调用: {worldPosition}</color>");
        
        if (!isPlacementMode || currentPlacementItem == null)
        {
            Debug.Log($"<color=yellow>[PlacementManagerV2] TryPlace(Vector3) 失败: 不在放置模式</color>");
            return false;
        }

        // 计算方块中心
        Vector3 cellCenter = PlacementGridCalculator.GetCellCenter(worldPosition);
        Debug.Log($"<color=cyan>[PlacementManagerV2] 方块中心: {cellCenter}</color>");

        // 检查 PlaceableItemData 的自定义验证
        if (currentPlacementItem is PlaceableItemData placeableItem)
        {
            if (!placeableItem.CanPlaceAt(cellCenter))
            {
                Debug.Log($"<color=red>[PlacementManagerV2] PlaceableItemData.CanPlaceAt 返回 false</color>");
                PlaySound(placeFailSound);
                return false;
            }
        }

        // 完整验证
        List<CellState> cellStates;
        var result = validator.ValidateFull(currentPlacementItem, cellCenter, playerTransform, out cellStates);
        
        Debug.Log($"<color=cyan>[PlacementManagerV2] 验证结果: IsValid={result.IsValid}, Reason={result.Reason}, Message={result.Message}</color>");

        if (!result.IsValid)
        {
            // 检查是否超出范围，启用导航放置
            if (result.Reason == PlacementInvalidReason.OutOfRange && enableNavigationPlacement)
            {
                Debug.Log($"<color=yellow>[PlacementManagerV2] 超出范围，启动导航放置</color>");
                StartNavigationPlacement(cellCenter);
                return false;
            }

            PlaySound(placeFailSound);

            Debug.Log($"<color=red>[PlacementManagerV2] 放置失败: {result.Message}</color>");

            return false;
        }

        // 执行放置
        Debug.Log($"<color=green>[PlacementManagerV2] 验证通过，执行放置</color>");
        return ExecutePlacement(cellCenter, result.DetectedLayer);
    }
    #endregion

    #region 放置执行内部方法
    /// <summary>
    /// 执行放置操作
    /// </summary>
    private bool ExecutePlacement(Vector3 position, int targetLayer)
    {
        // 实例化预制体
        GameObject placedObject = InstantiatePlacementPrefab(position);
        if (placedObject == null)
        {
            PlaySound(placeFailSound);
            return false;
        }

        // 同步 Layer
        SyncLayerToPlacedObject(placedObject, targetLayer);

        // 计算并设置排序 Order
        SetSortingOrder(placedObject, position);

        // 扣除背包物品
        if (!DeductItemFromInventory())
        {
            Destroy(placedObject);
            PlaySound(placeFailSound);
            return false;
        }

        // 创建事件数据
        var eventData = new PlacementEventData(
            position,
            currentPlacementItem,
            placedObject,
            currentPlacementItem.placementType
        );

        // 添加到历史记录
        AddToHistory(eventData);

        // 播放音效和特效
        PlaySound(placeSuccessSound);
        PlayPlaceEffect(position);

        // 广播事件
        OnItemPlaced?.Invoke(eventData);

        // 树苗特殊处理
        if (currentPlacementItem.placementType == PlacementType.Sapling)
        {
            HandleSaplingPlacement(position, placedObject);
        }

        if (showDebugInfo)
            Debug.Log($"<color=green>[PlacementManagerV2] 放置成功: {currentPlacementItem.itemName} at {position}, Layer: {LayerMask.LayerToName(targetLayer)}</color>");

        // 检查是否还有物品
        if (!HasMoreItems())
        {
            ExitPlacementMode();
        }

        return true;
    }

    /// <summary>
    /// 实例化放置预制体
    /// </summary>
    private GameObject InstantiatePlacementPrefab(Vector3 position)
    {
        if (currentPlacementItem.placementPrefab == null)
        {
            Debug.LogError($"[PlacementManagerV2] {currentPlacementItem.itemName} 缺少放置预制体！");
            return null;
        }

        GameObject obj = Instantiate(currentPlacementItem.placementPrefab, position, Quaternion.identity);
        return obj;
    }

    /// <summary>
    /// 同步 Layer 到放置物体及其所有子物体
    /// </summary>
    private void SyncLayerToPlacedObject(GameObject placedObject, int layer)
    {
        // 同步 GameObject Layer
        PlacementLayerDetector.SyncLayerToAllChildren(placedObject, layer);

        // 获取玩家的 Sorting Layer
        string sortingLayerName = PlacementLayerDetector.GetPlayerSortingLayer(playerTransform);
        
        // 同步 Sorting Layer
        PlacementLayerDetector.SyncSortingLayerToAllRenderers(placedObject, sortingLayerName);

        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlacementManagerV2] Layer 同步: {LayerMask.LayerToName(layer)}, SortingLayer: {sortingLayerName}</color>");
    }

    /// <summary>
    /// 计算并设置排序 Order
    /// </summary>
    private void SetSortingOrder(GameObject placedObject, Vector3 position)
    {
        // 计算 Order: -Round(Y × multiplier) + offset
        int order = -Mathf.RoundToInt(position.y * sortingOrderMultiplier) + sortingOrderOffset;

        // 获取所有 SpriteRenderer
        var renderers = placedObject.GetComponentsInChildren<SpriteRenderer>(true);
        
        foreach (var renderer in renderers)
        {
            // 检查是否是 Shadow
            bool isShadow = renderer.gameObject.name.ToLower().Contains("shadow");
            
            if (isShadow)
            {
                // Shadow 的 Order = 主体 Order - 1
                renderer.sortingOrder = order - 1;
            }
            else
            {
                renderer.sortingOrder = order;
            }
        }

        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlacementManagerV2] Order 设置: {order} (Y={position.y})</color>");
    }

    /// <summary>
    /// 处理树苗放置
    /// </summary>
    private void HandleSaplingPlacement(Vector3 position, GameObject treeObject)
    {
        var saplingData = currentPlacementItem as SaplingData;
        if (saplingData == null) return;

        var treeController = treeObject.GetComponentInChildren<TreeControllerV2>();
        if (treeController != null)
        {
            treeController.SetStage(0);
            
            // 尝试调用 Reset 方法（如果存在）
            var resetMethod = treeController.GetType().GetMethod("Reset");
            if (resetMethod != null)
            {
                resetMethod.Invoke(treeController, null);
            }

            var saplingEvent = new SaplingPlantedEventData(
                position,
                saplingData,
                treeObject,
                treeController
            );
            OnSaplingPlanted?.Invoke(saplingEvent);

            if (showDebugInfo)
                Debug.Log($"<color=lime>[PlacementManagerV2] 树苗种植成功: {saplingData.itemName}</color>");
        }
        else
        {
            Debug.LogWarning($"[PlacementManagerV2] 树木预制体缺少 TreeControllerV2 组件！");
        }
    }
    #endregion

    #region 导航放置
    /// <summary>
    /// 开始导航放置
    /// </summary>
    private void StartNavigationPlacement(Vector3 targetPosition)
    {
        if (!enableNavigationPlacement) return;

        isNavigatingToPlace = true;
        pendingPlacePosition = targetPosition;

        // 调用 PlayerAutoNavigator 导航到目标位置
        var navigator = FindFirstObjectByType<PlayerAutoNavigator>();
        if (navigator != null)
        {
            navigator.SetDestination(targetPosition);
            
            // 启动协程检测到达
            StartCoroutine(WaitForNavigationComplete(navigator, targetPosition));
        }
        else
        {
            if (showDebugInfo)
                Debug.LogWarning("[PlacementManagerV2] 未找到 PlayerAutoNavigator，无法导航放置");
            isNavigatingToPlace = false;
        }

        if (showDebugInfo)
            Debug.Log($"<color=yellow>[PlacementManagerV2] 开始导航到放置点: {targetPosition}</color>");
    }
    
    /// <summary>
    /// 等待导航完成的协程
    /// </summary>
    private System.Collections.IEnumerator WaitForNavigationComplete(PlayerAutoNavigator navigator, Vector3 targetPosition)
    {
        float timeout = 10f;
        float elapsed = 0f;
        
        while (isNavigatingToPlace && elapsed < timeout)
        {
            // 检查是否到达目标附近
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(playerTransform.position, targetPosition);
                if (distance <= interactionRange)
                {
                    // 到达目标，尝试放置
                    isNavigatingToPlace = false;
                    yield return null; // 等待一帧
                    TryPlace(pendingPlacePosition);
                    yield break;
                }
            }
            
            // 检查导航是否被取消
            if (!navigator.IsActive)
            {
                isNavigatingToPlace = false;
                if (showDebugInfo)
                    Debug.Log($"<color=red>[PlacementManagerV2] 导航被取消</color>");
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 超时
        isNavigatingToPlace = false;
        if (showDebugInfo)
            Debug.Log($"<color=red>[PlacementManagerV2] 导航超时</color>");
    }

    /// <summary>
    /// 导航完成回调
    /// </summary>
    private void OnNavigationComplete(bool success)
    {
        if (!isNavigatingToPlace) return;

        isNavigatingToPlace = false;

        if (success)
        {
            // 到达目标，尝试放置
            TryPlace(pendingPlacePosition);
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"<color=red>[PlacementManagerV2] 导航失败，取消放置</color>");
        }
    }

    /// <summary>
    /// 取消导航放置
    /// </summary>
    public void CancelNavigationPlacement()
    {
        if (isNavigatingToPlace)
        {
            isNavigatingToPlace = false;
            
            // 取消导航
            var navigator = FindFirstObjectByType<PlayerAutoNavigator>();
            if (navigator != null)
            {
                navigator.Cancel();
            }
        }
    }
    #endregion

    #region 撤销功能
    /// <summary>
    /// 撤销最近一次放置
    /// </summary>
    public bool UndoLastPlacement()
    {
        if (placementHistory.Count == 0)
        {
            if (showDebugInfo)
                Debug.Log($"<color=yellow>[PlacementManagerV2] 没有可撤销的放置</color>");
            return false;
        }

        var lastEntry = placementHistory[placementHistory.Count - 1];

        if (!lastEntry.CanUndo)
        {
            if (showDebugInfo)
                Debug.Log($"<color=yellow>[PlacementManagerV2] 放置已超过5秒，无法撤销</color>");
            return false;
        }

        if (lastEntry.EventData.PlacedObject != null)
        {
            Destroy(lastEntry.EventData.PlacedObject);
        }

        ReturnItemToInventory(lastEntry.DeductedItemId, lastEntry.DeductedItemQuality);
        placementHistory.RemoveAt(placementHistory.Count - 1);

        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlacementManagerV2] 撤销放置成功</color>");

        return true;
    }

    private void AddToHistory(PlacementEventData eventData)
    {
        var entry = new PlacementHistoryEntry(
            eventData,
            currentPlacementItem.itemID,
            currentItemQuality
        );

        placementHistory.Add(entry);

        while (placementHistory.Count > MAX_HISTORY_SIZE)
        {
            placementHistory.RemoveAt(0);
        }
    }
    #endregion

    #region 辅助方法
    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    private void CreatePlacementPreview()
    {
        Debug.Log($"<color=cyan>[PlacementManagerV2] CreatePlacementPreview() 开始</color>");
        
        GameObject previewObj = new GameObject("PlacementPreviewV2");
        previewObj.transform.SetParent(transform);
        placementPreview = previewObj.AddComponent<PlacementPreviewV2>();
        
        Debug.Log($"<color=green>[PlacementManagerV2] 预览对象创建成功: {previewObj.name}, 组件: {placementPreview != null}</color>");
    }

    private bool DeductItemFromInventory()
    {
        // TODO: 与 InventoryService 集成
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlacementManagerV2] 从背包扣除: {currentPlacementItem.itemName} x1</color>");
        return true;
    }

    private bool HasMoreItems()
    {
        // TODO: 与 InventoryService 集成
        return true;
    }

    private void ReturnItemToInventory(int itemId, int quality)
    {
        // TODO: 与 InventoryService 集成
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlacementManagerV2] 返还物品到背包: ID={itemId}, Quality={quality}</color>");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, transform.position, soundVolume);
    }

    private void PlayPlaceEffect(Vector3 position)
    {
        if (placeEffectPrefab == null) return;
        var effect = Instantiate(placeEffectPrefab, position, Quaternion.identity);
        Destroy(effect, 2f);
    }

    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }
    #endregion
}
