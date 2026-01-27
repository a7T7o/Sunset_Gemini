using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using FarmGame.Data;
using FarmGame.Events;

/// <summary>
/// 放置管理器
/// 统一管理所有物品的放置逻辑
/// </summary>
public class PlacementManager : MonoBehaviour
{
    #region 单例
    public static PlacementManager Instance { get; private set; }
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
        [SerializeField] private PlacementPreview placementPreview;

        [Tooltip("玩家 Transform")]
        [SerializeField] private Transform playerTransform;

        [Header("━━━━ 配置 ━━━━")]
        [Tooltip("玩家交互范围")]
        [SerializeField] private float interactionRange = 3f;

        [Tooltip("障碍物检测标签")]
        [SerializeField] private string[] obstacleTags = new string[] { "Tree", "Rock", "Building" };

        [Header("━━━━ 音效 ━━━━")]
        [Tooltip("放置成功音效")]
        [SerializeField] private AudioClip placeSuccessSound;

        [Tooltip("放置失败音效")]
        [SerializeField] private AudioClip placeFailSound;

        [Tooltip("撤销音效")]
        [SerializeField] private AudioClip undoSound;

        [Tooltip("音效音量")]
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.8f;

        [Header("━━━━ 特效 ━━━━")]
        [Tooltip("放置成功特效预制体")]
        [SerializeField] private GameObject placeEffectPrefab;

        [Tooltip("树苗种植特效预制体")]
        [SerializeField] private GameObject saplingPlantEffectPrefab;

        [Header("━━━━ 调试 ━━━━")]
        [SerializeField] private bool showDebugInfo = false;
        #endregion

        #region 私有字段
        private PlacementValidator validator;
        private ItemData currentPlacementItem;
        private int currentItemQuality;
        private bool isPlacementMode = false;
        private Camera mainCamera;
        
        // 放置历史（用于撤销）
        private List<PlacementHistoryEntry> placementHistory = new List<PlacementHistoryEntry>();
        private const int MAX_HISTORY_SIZE = 10;
        #endregion

        #region 属性
        /// <summary>是否处于放置模式</summary>
        public bool IsPlacementMode => isPlacementMode;
        
        /// <summary>当前放置物品</summary>
        public ItemData CurrentPlacementItem => currentPlacementItem;
        #endregion

        #region Unity 生命周期
        private void Awake()
        {
            // 单例设置
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 初始化验证器
            validator = new PlacementValidator(interactionRange);
            validator.SetObstacleTags(obstacleTags);

            // 获取主摄像机
            mainCamera = Camera.main;
        }

        private void Start()
        {
            // 查找玩家
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }

            // 创建预览组件（如果没有）
            if (placementPreview == null)
            {
                CreatePlacementPreview();
            }
        }

        private void Update()
        {
            if (!isPlacementMode) return;

            // 更新预览位置
            UpdatePreviewPosition();

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
            if (item == null || !item.isPlaceable)
            {
                if (showDebugInfo)
                    Debug.Log($"<color=yellow>[PlacementManager] 物品不可放置: {item?.itemName ?? "null"}</color>");
                return;
            }

            currentPlacementItem = item;
            currentItemQuality = quality;
            isPlacementMode = true;

            // 显示预览
            if (placementPreview != null)
            {
                placementPreview.Show(item);
            }

            OnPlacementModeChanged?.Invoke(true);

            if (showDebugInfo)
                Debug.Log($"<color=green>[PlacementManager] 进入放置模式: {item.itemName}</color>");
        }

        /// <summary>
        /// 退出放置模式
        /// </summary>
        public void ExitPlacementMode()
        {
            currentPlacementItem = null;
            currentItemQuality = 0;
            isPlacementMode = false;

            // 隐藏预览
            if (placementPreview != null)
            {
                placementPreview.Hide();
            }

            OnPlacementModeChanged?.Invoke(false);

            if (showDebugInfo)
                Debug.Log($"<color=yellow>[PlacementManager] 退出放置模式</color>");
        }
        #endregion

        #region 放置执行
        /// <summary>
        /// 尝试在当前预览位置放置物品
        /// </summary>
        public bool TryPlace()
        {
            if (!isPlacementMode || currentPlacementItem == null)
                return false;

            // 检查是否在 UI 上
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            Vector3 placePosition = placementPreview != null 
                ? placementPreview.GetPreviewPosition() 
                : GetMouseWorldPosition();

            return TryPlace(placePosition);
        }

        /// <summary>
        /// 尝试在指定位置放置物品
        /// </summary>
        public bool TryPlace(Vector3 worldPosition)
        {
            if (!isPlacementMode || currentPlacementItem == null)
                return false;

            // 检查 PlaceableItemData 的自定义验证（包括冬季检测）
            if (currentPlacementItem is PlaceableItemData placeableItem)
            {
                if (!placeableItem.CanPlaceAt(worldPosition))
                {
                    PlaySound(placeFailSound);
                    return false;
                }
            }

            // 验证位置
            var result = validator.Validate(currentPlacementItem, worldPosition, playerTransform);
            
            if (!result.IsValid)
            {
                // 放置失败
                PlaySound(placeFailSound);
                
                if (showDebugInfo)
                    Debug.Log($"<color=red>[PlacementManager] 放置失败: {result.Message}</color>");
                
                return false;
            }

            // 执行放置
            return ExecutePlacement(worldPosition);
        }

        /// <summary>
        /// 检查指定位置是否可以放置
        /// </summary>
        public bool CanPlaceAt(Vector3 worldPosition)
        {
            if (currentPlacementItem == null) return false;
            
            var result = validator.Validate(currentPlacementItem, worldPosition, playerTransform);
            return result.IsValid;
        }
        #endregion

        #region 放置执行内部方法
        /// <summary>
        /// 执行放置操作
        /// </summary>
        private bool ExecutePlacement(Vector3 position)
        {
            // 对齐位置
            Vector3 alignedPos = position;
            if (currentPlacementItem.placementType == PlacementType.Sapling ||
                currentPlacementItem.placementType == PlacementType.Building)
            {
                alignedPos = validator.AlignToGrid(position);
            }

            // 实例化预制体
            GameObject placedObject = InstantiatePlacementPrefab(alignedPos);
            if (placedObject == null)
            {
                PlaySound(placeFailSound);
                return false;
            }

            // 扣除背包物品
            if (!DeductItemFromInventory())
            {
                Destroy(placedObject);
                PlaySound(placeFailSound);
                return false;
            }

            // 创建事件数据
            var eventData = new PlacementEventData(
                alignedPos,
                currentPlacementItem,
                placedObject,
                currentPlacementItem.placementType
            );

            // 添加到历史记录
            AddToHistory(eventData);

            // 播放音效和特效
            PlaySound(placeSuccessSound);
            PlayPlaceEffect(alignedPos);

            // 广播事件
            OnItemPlaced?.Invoke(eventData);

            // 树苗特殊处理
            if (currentPlacementItem.placementType == PlacementType.Sapling)
            {
                HandleSaplingPlacement(alignedPos, placedObject);
            }

            if (showDebugInfo)
                Debug.Log($"<color=green>[PlacementManager] 放置成功: {currentPlacementItem.itemName} at {alignedPos}</color>");

            // 检查是否还有物品，没有则退出放置模式
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
                Debug.LogError($"[PlacementManager] {currentPlacementItem.itemName} 缺少放置预制体！");
                return null;
            }

            GameObject obj = Instantiate(currentPlacementItem.placementPrefab, position, Quaternion.identity);
            return obj;
        }

        /// <summary>
        /// 处理树苗放置
        /// </summary>
        private void HandleSaplingPlacement(Vector3 position, GameObject treeObject)
        {
            var saplingData = currentPlacementItem as SaplingData;
            if (saplingData == null) return;

            // 获取 TreeControllerV2 并设置为阶段 0
            var treeController = treeObject.GetComponentInChildren<TreeControllerV2>();
            if (treeController != null)
            {
                treeController.SetStage(0);
                treeController.Reset();

                // 播放树苗种植特效
                PlaySaplingPlantEffect(position);

                // 广播树苗种植事件
                var saplingEvent = new SaplingPlantedEventData(
                    position,
                    saplingData,
                    treeObject,
                    treeController
                );
                OnSaplingPlanted?.Invoke(saplingEvent);

                if (showDebugInfo)
                    Debug.Log($"<color=lime>[PlacementManager] 树苗种植成功: {saplingData.itemName}</color>");
            }
            else
            {
                Debug.LogWarning($"[PlacementManager] 树木预制体缺少 TreeControllerV2 组件！");
            }
        }

        /// <summary>
        /// 从背包扣除物品
        /// </summary>
        private bool DeductItemFromInventory()
        {
            // TODO: 与 InventoryService 集成
            // 目前返回 true，表示扣除成功
            if (showDebugInfo)
                Debug.Log($"<color=cyan>[PlacementManager] 从背包扣除: {currentPlacementItem.itemName} x1</color>");
            return true;
        }

        /// <summary>
        /// 检查是否还有更多物品
        /// </summary>
        private bool HasMoreItems()
        {
            // TODO: 与 InventoryService 集成
            // 目前返回 true，表示还有物品
            return true;
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
                    Debug.Log($"<color=yellow>[PlacementManager] 没有可撤销的放置</color>");
                return false;
            }

            var lastEntry = placementHistory[placementHistory.Count - 1];
            
            // 检查是否可以撤销（5秒内）
            if (!lastEntry.CanUndo)
            {
                if (showDebugInfo)
                    Debug.Log($"<color=yellow>[PlacementManager] 放置已超过5秒，无法撤销</color>");
                return false;
            }

            // 销毁已放置的物体
            if (lastEntry.EventData.PlacedObject != null)
            {
                Destroy(lastEntry.EventData.PlacedObject);
            }

            // 返还物品到背包
            ReturnItemToInventory(lastEntry.DeductedItemId, lastEntry.DeductedItemQuality);

            // 从历史记录中移除
            placementHistory.RemoveAt(placementHistory.Count - 1);

            // 播放撤销音效
            PlaySound(undoSound);

            if (showDebugInfo)
                Debug.Log($"<color=cyan>[PlacementManager] 撤销放置成功</color>");

            return true;
        }

        /// <summary>
        /// 添加到放置历史
        /// </summary>
        private void AddToHistory(PlacementEventData eventData)
        {
            var entry = new PlacementHistoryEntry(
                eventData,
                currentPlacementItem.itemID,
                currentItemQuality
            );

            placementHistory.Add(entry);

            // 限制历史记录大小
            while (placementHistory.Count > MAX_HISTORY_SIZE)
            {
                placementHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 返还物品到背包
        /// </summary>
        private void ReturnItemToInventory(int itemId, int quality)
        {
            // TODO: 与 InventoryService 集成
            if (showDebugInfo)
                Debug.Log($"<color=cyan>[PlacementManager] 返还物品到背包: ID={itemId}, Quality={quality}</color>");
        }
        #endregion

        #region 预览更新
        /// <summary>
        /// 更新预览位置
        /// </summary>
        private void UpdatePreviewPosition()
        {
            if (placementPreview == null) return;

            Vector3 mousePos = GetMouseWorldPosition();
            placementPreview.UpdatePosition(mousePos);

            // 更新有效性
            var result = validator.Validate(currentPlacementItem, mousePos, playerTransform);
            placementPreview.SetValid(result.IsValid);
            placementPreview.SetOutOfRange(result.Reason == PlacementInvalidReason.OutOfRange);
        }

        /// <summary>
        /// 获取鼠标世界坐标
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = -mainCamera.transform.position.z;
            return mainCamera.ScreenToWorldPoint(mousePos);
        }
        #endregion

        #region 音效和特效
        /// <summary>
        /// 播放音效
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, transform.position, soundVolume);
        }

        /// <summary>
        /// 播放放置特效
        /// </summary>
        private void PlayPlaceEffect(Vector3 position)
        {
            if (placeEffectPrefab == null) return;
            var effect = Instantiate(placeEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        /// <summary>
        /// 播放树苗种植特效
        /// </summary>
        private void PlaySaplingPlantEffect(Vector3 position)
        {
            if (saplingPlantEffectPrefab == null) return;
            var effect = Instantiate(saplingPlantEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 创建放置预览组件
        /// </summary>
        private void CreatePlacementPreview()
        {
            GameObject previewObj = new GameObject("PlacementPreview");
            previewObj.transform.SetParent(transform);
            placementPreview = previewObj.AddComponent<PlacementPreview>();
        }

        /// <summary>
        /// 设置玩家 Transform
        /// </summary>
        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }
        #endregion
}
