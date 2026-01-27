using UnityEngine;
using FarmGame.Data;

namespace FarmGame.World
{
    /// <summary>
    /// 箱子掉落处理器 - 处理箱子掉落后的自动放置逻辑
    /// 静态工具类，提供螺旋搜索算法查找空位
    /// </summary>
    public static class ChestDropHandler
    {
        #region 常量

        /// <summary>
        /// 掉落动画持续时间（秒）
        /// </summary>
        public const float DropAnimationDuration = 1f;

        /// <summary>
        /// 螺旋搜索最大半径
        /// </summary>
        private const float MaxSearchRadius = 5f;

        /// <summary>
        /// 螺旋搜索步长
        /// </summary>
        private const float SearchStep = 0.5f;

        /// <summary>
        /// 碰撞检测半径
        /// </summary>
        private const float CollisionCheckRadius = 0.4f;

        #endregion

        #region 公共方法

        /// <summary>
        /// 处理箱子掉落
        /// 🔥 修正：支持指定父物体
        /// </summary>
        /// <param name="storageData">箱子数据</param>
        /// <param name="dropPosition">掉落位置</param>
        /// <param name="ownership">箱子归属</param>
        /// <param name="parent">父物体（可选，如 LAYER 1/Props）</param>
        /// <returns>是否成功放置</returns>
        public static bool HandleChestDrop(StorageData storageData, Vector3 dropPosition, ChestOwnership ownership = ChestOwnership.Player, Transform parent = null)
        {
            if (storageData == null)
            {
                Debug.LogWarning("[ChestDropHandler] StorageData 为空，无法放置箱子");
                return false;
            }

            // 查找空位
            Vector3? emptyPos = FindEmptyPosition(dropPosition, MaxSearchRadius);
            
            if (!emptyPos.HasValue)
            {
                Debug.LogWarning($"[ChestDropHandler] 在 {dropPosition} 附近找不到空位放置箱子");
                // TODO: 显示UI提示"附近没有空位放置箱子"
                return false;
            }

            // 在空位放置箱子
            return SpawnPlacedChest(storageData, emptyPos.Value, ownership, parent);
        }

        /// <summary>
        /// 查找空位（螺旋搜索算法）
        /// </summary>
        /// <param name="center">搜索中心</param>
        /// <param name="maxRadius">最大搜索半径</param>
        /// <returns>找到的空位，null表示未找到</returns>
        public static Vector3? FindEmptyPosition(Vector3 center, float maxRadius)
        {
            // 首先检查中心位置
            if (!HasCollisionAt(center))
            {
                return center;
            }

            // 螺旋搜索
            float radius = SearchStep;
            int pointsPerRing = 8;

            while (radius <= maxRadius)
            {
                // 当前半径上的点数随半径增加
                int points = Mathf.Max(8, Mathf.RoundToInt(pointsPerRing * (radius / SearchStep)));
                float angleStep = 360f / points;

                for (int i = 0; i < points; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector3 checkPos = center + new Vector3(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius,
                        0f
                    );

                    if (!HasCollisionAt(checkPos))
                    {
                        return checkPos;
                    }
                }

                radius += SearchStep;
            }

            return null;
        }

        /// <summary>
        /// 在指定位置生成已放置的箱子
        /// 🔥 修正：支持指定父物体，与场景层级一致
        /// </summary>
        /// <param name="storageData">箱子数据</param>
        /// <param name="position">放置位置</param>
        /// <param name="ownership">箱子归属</param>
        /// <param name="parent">父物体（可选，如 LAYER 1/Props）</param>
        /// <returns>是否成功生成</returns>
        public static bool SpawnPlacedChest(StorageData storageData, Vector3 position, ChestOwnership ownership, Transform parent = null)
        {
            if (storageData == null || storageData.storagePrefab == null)
            {
                Debug.LogWarning("[ChestDropHandler] StorageData 或预制体为空");
                return false;
            }

            // 实例化箱子预制体
            GameObject chestObj;
            if (parent != null)
            {
                chestObj = Object.Instantiate(storageData.storagePrefab, position, Quaternion.identity, parent);
            }
            else
            {
                chestObj = Object.Instantiate(storageData.storagePrefab, position, Quaternion.identity);
            }
            
            // 获取或添加 ChestController
            ChestController controller = chestObj.GetComponent<ChestController>();
            if (controller == null)
            {
                controller = chestObj.AddComponent<ChestController>();
            }

            // 初始化箱子
            controller.Initialize(storageData, ownership);

            Debug.Log($"[ChestDropHandler] 箱子放置成功: {storageData.itemName} at {position}, parent={parent?.name ?? "null"}");
            return true;
        }

        /// <summary>
        /// 在指定位置生成已放置的箱子（旧接口，保留兼容）
        /// </summary>
        public static bool SpawnPlacedChest(StorageData storageData, Vector3 position, ChestOwnership ownership)
        {
            return SpawnPlacedChest(storageData, position, ownership, null);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查指定位置是否有碰撞体
        /// </summary>
        private static bool HasCollisionAt(Vector3 position)
        {
            var hits = Physics2D.OverlapCircleAll(position, CollisionCheckRadius);
            
            foreach (var hit in hits)
            {
                // 忽略触发器
                if (hit.isTrigger) continue;
                
                // 有非触发器碰撞体，位置被占用
                return true;
            }

            return false;
        }

        #endregion
    }
}
