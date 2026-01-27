using UnityEngine;
using FarmGame.Data;

namespace FarmGame.Events
{
    /// <summary>
    /// 物品放置事件数据
    /// 任意物品放置成功后广播
    /// </summary>
    public struct PlacementEventData
    {
        /// <summary>放置位置（世界坐标）</summary>
        public Vector3 Position;
        
        /// <summary>放置的物品数据</summary>
        public ItemData ItemData;
        
        /// <summary>实例化的 GameObject</summary>
        public GameObject PlacedObject;
        
        /// <summary>放置类型</summary>
        public PlacementType PlacementType;
        
        /// <summary>放置时间戳</summary>
        public float Timestamp;

        public PlacementEventData(Vector3 position, ItemData itemData, GameObject placedObject, PlacementType placementType)
        {
            Position = position;
            ItemData = itemData;
            PlacedObject = placedObject;
            PlacementType = placementType;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 树苗种植事件数据
    /// 树苗放置成功后广播
    /// </summary>
    public struct SaplingPlantedEventData
    {
        /// <summary>种植位置（世界坐标，整数对齐）</summary>
        public Vector3 Position;
        
        /// <summary>树苗数据</summary>
        public SaplingData SaplingData;
        
        /// <summary>实例化的树木 GameObject</summary>
        public GameObject TreeObject;
        
        /// <summary>树木控制器引用</summary>
        public TreeControllerV2 TreeController;
        
        /// <summary>种植时间戳</summary>
        public float Timestamp;

        public SaplingPlantedEventData(Vector3 position, SaplingData saplingData, GameObject treeObject, TreeControllerV2 treeController)
        {
            Position = position;
            SaplingData = saplingData;
            TreeObject = treeObject;
            TreeController = treeController;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 放置验证结果
    /// </summary>
    public struct PlacementValidationResult
    {
        /// <summary>是否有效</summary>
        public bool IsValid;
        
        /// <summary>无效原因</summary>
        public PlacementInvalidReason Reason;
        
        /// <summary>提示消息</summary>
        public string Message;
        
        /// <summary>每个格子的状态（用于预览 UI）</summary>
        public System.Collections.Generic.List<CellState> CellStates;
        
        /// <summary>检测到的 Layer</summary>
        public int DetectedLayer;

        public static PlacementValidationResult Valid()
        {
            return new PlacementValidationResult
            {
                IsValid = true,
                Reason = PlacementInvalidReason.None,
                Message = string.Empty,
                CellStates = null,
                DetectedLayer = 0
            };
        }

        public static PlacementValidationResult Invalid(PlacementInvalidReason reason, string message = "")
        {
            return new PlacementValidationResult
            {
                IsValid = false,
                Reason = reason,
                Message = message,
                CellStates = null,
                DetectedLayer = 0
            };
        }
        
        public static PlacementValidationResult WithCellStates(bool isValid, PlacementInvalidReason reason, string message, System.Collections.Generic.List<CellState> cellStates, int detectedLayer)
        {
            return new PlacementValidationResult
            {
                IsValid = isValid,
                Reason = reason,
                Message = message,
                CellStates = cellStates,
                DetectedLayer = detectedLayer
            };
        }
    }

    /// <summary>
    /// 放置历史记录条目
    /// 用于撤销功能
    /// </summary>
    public class PlacementHistoryEntry
    {
        /// <summary>放置事件数据</summary>
        public PlacementEventData EventData;
        
        /// <summary>放置时间</summary>
        public float PlacementTime;
        
        /// <summary>扣除的物品 ID</summary>
        public int DeductedItemId;
        
        /// <summary>扣除的物品品质</summary>
        public int DeductedItemQuality;

        /// <summary>是否可以撤销（5秒内）</summary>
        public bool CanUndo => Time.time - PlacementTime < 5f;

        public PlacementHistoryEntry(PlacementEventData eventData, int itemId, int quality)
        {
            EventData = eventData;
            PlacementTime = Time.time;
            DeductedItemId = itemId;
            DeductedItemQuality = quality;
        }
    }
}
