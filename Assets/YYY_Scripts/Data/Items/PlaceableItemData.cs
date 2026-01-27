using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 可放置物品数据基类
    /// 所有可放置物品（树苗、工作台、存储容器等）的共同基类
    /// </summary>
    public abstract class PlaceableItemData : ItemData
    {
        [Header("=== 放置专属配置 ===")]
        [Tooltip("放置位置偏移")]
        public Vector3 placementOffset = Vector3.zero;

        [Tooltip("是否可旋转放置")]
        public bool canRotate = false;

        [Tooltip("旋转步进角度")]
        [Range(15f, 180f)]
        public float rotationStep = 90f;

        /// <summary>
        /// 获取放置类型（由子类实现）
        /// </summary>
        public abstract PlacementType GetPlacementType();

        /// <summary>
        /// 获取放置预制体（由子类实现）
        /// </summary>
        public abstract GameObject GetPlacementPrefab();

        /// <summary>
        /// 检查是否可以在指定位置放置（可被子类重写）
        /// </summary>
        public virtual bool CanPlaceAt(Vector3 position)
        {
            return true;
        }

        /// <summary>
        /// 放置成功后的回调（可被子类重写）
        /// </summary>
        public virtual void OnPlaced(Vector3 position, GameObject instance)
        {
            // 子类可重写实现特殊逻辑
        }

        /// <summary>
        /// 移除后的回调（可被子类重写）
        /// </summary>
        public virtual void OnRemoved(GameObject instance)
        {
            // 子类可重写实现特殊逻辑
        }

        /// <summary>
        /// 验证数据完整性
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            // 自动设置为可放置
            isPlaceable = true;
            
            // 自动设置放置类型
            placementType = GetPlacementType();
            
            // 自动设置放置预制体
            var prefab = GetPlacementPrefab();
            if (prefab != null)
            {
                placementPrefab = prefab;
            }
        }
    }
}
