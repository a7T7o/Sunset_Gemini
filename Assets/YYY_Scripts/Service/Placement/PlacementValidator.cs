using UnityEngine;
using FarmGame.Data;
using FarmGame.Events;

/// <summary>
/// 放置验证器
/// 负责验证放置位置是否有效
/// </summary>
public class PlacementValidator
{
        #region 配置参数
        /// <summary>玩家交互范围</summary>
        private float playerInteractionRange = 3f;
        
        /// <summary>障碍物检测标签</summary>
        private string[] obstacleTags = new string[] { "Tree", "Rock", "Building" };
        
        /// <summary>水域检测层</summary>
        private LayerMask waterLayer;
        
        /// <summary>地形检测层</summary>
        private LayerMask terrainLayer;
        #endregion

        #region 构造函数
        public PlacementValidator(float interactionRange = 3f)
        {
            playerInteractionRange = interactionRange;
            waterLayer = LayerMask.GetMask("Water");
            terrainLayer = LayerMask.GetMask("Ground", "Terrain");
        }
        #endregion

        #region 主验证方法
        /// <summary>
        /// 验证放置位置是否有效
        /// </summary>
        public PlacementValidationResult Validate(ItemData item, Vector3 position, Transform playerTransform)
        {
            if (item == null)
                return PlacementValidationResult.Invalid(PlacementInvalidReason.InvalidTerrain, "物品数据为空");

            if (!item.isPlaceable)
                return PlacementValidationResult.Invalid(PlacementInvalidReason.InvalidTerrain, "该物品不可放置");

            // 检查玩家范围
            if (!IsWithinPlayerRange(position, playerTransform))
                return PlacementValidationResult.Invalid(PlacementInvalidReason.OutOfRange, "超出放置范围");

            // 根据放置类型进行特定验证
            switch (item.placementType)
            {
                case PlacementType.Sapling:
                    return ValidateSaplingPlacement(item as SaplingData, position);
                
                case PlacementType.Decoration:
                    return ValidateDecorationPlacement(item, position);
                
                case PlacementType.Building:
                    return ValidateBuildingPlacement(item, position);
                
                case PlacementType.Furniture:
                    return ValidateFurniturePlacement(item, position);
                
                default:
                    return PlacementValidationResult.Invalid(PlacementInvalidReason.InvalidTerrain, "未知的放置类型");
            }
        }
        #endregion

        #region 树苗验证
        /// <summary>
        /// 验证树苗放置位置
        /// </summary>
        public PlacementValidationResult ValidateSaplingPlacement(SaplingData sapling, Vector3 position)
        {
            if (sapling == null)
                return PlacementValidationResult.Invalid(PlacementInvalidReason.InvalidTerrain, "树苗数据为空");

            // 对齐到整数坐标
            Vector3 alignedPos = AlignToGrid(position);

            // 检查冬季（使用 SaplingData 的 IsWinter 方法）
            if (sapling.IsWinter())
                return PlacementValidationResult.Invalid(PlacementInvalidReason.WrongSeason, "冬天无法种植树木");

            // 检查是否在耕地上
            if (IsOnFarmland(alignedPos))
                return PlacementValidationResult.Invalid(PlacementInvalidReason.OnFarmland, "不能在耕地上种植树苗");

            // 检查是否在水域
            if (IsOnWater(alignedPos))
                return PlacementValidationResult.Invalid(PlacementInvalidReason.OnWater, "不能在水域种植树苗");

            // 获取成长边距参数
            float vMargin, hMargin;
            if (!sapling.GetStage0Margins(out vMargin, out hMargin))
            {
                // 使用默认值
                vMargin = 0.2f;
                hMargin = 0.15f;
            }

            // 检查成长边距内是否有障碍物
            if (HasObstacleInMargin(alignedPos, vMargin, hMargin, obstacleTags))
                return PlacementValidationResult.Invalid(PlacementInvalidReason.InsufficientSpace, "空间不足，附近有障碍物");

            // 检查是否距离其他树木太近
            if (HasTreeInMargin(alignedPos, vMargin, hMargin))
                return PlacementValidationResult.Invalid(PlacementInvalidReason.TreeTooClose, "距离其他树木太近");

            return PlacementValidationResult.Valid();
        }

        /// <summary>
        /// 检查成长边距内是否有障碍物
        /// </summary>
        public bool HasObstacleInMargin(Vector3 center, float vMargin, float hMargin, string[] tags)
        {
            if (tags == null || tags.Length == 0) return false;

            // 检测四个方向
            Vector2[] directions = new Vector2[]
            {
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right
            };

            float[] distances = new float[]
            {
                vMargin,
                vMargin,
                hMargin,
                hMargin
            };

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2 checkPoint = (Vector2)center + directions[i] * distances[i];
                float checkRadius = 0.1f;
                
                Collider2D[] hits = Physics2D.OverlapCircleAll(checkPoint, checkRadius);
                foreach (var hit in hits)
                {
                    if (HasAnyTag(hit.transform, tags))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查边距内是否有其他树木
        /// </summary>
        public bool HasTreeInMargin(Vector3 center, float vMargin, float hMargin)
        {
            // 使用较大的检测范围
            float maxMargin = Mathf.Max(vMargin, hMargin);
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, maxMargin);

            foreach (var hit in hits)
            {
                // 检查是否是树木
                var treeController = hit.GetComponentInParent<TreeControllerV2>();
                if (treeController != null)
                    return true;
            }

            return false;
        }
        #endregion

        #region 装饰物验证
        /// <summary>
        /// 验证装饰物放置位置
        /// </summary>
        private PlacementValidationResult ValidateDecorationPlacement(ItemData item, Vector3 position)
        {
            // 检查是否在耕地上
            if (IsOnFarmland(position))
                return PlacementValidationResult.Invalid(PlacementInvalidReason.OnFarmland, "不能在耕地上放置装饰物");

            // 检查是否在水域
            if (IsOnWater(position))
                return PlacementValidationResult.Invalid(PlacementInvalidReason.OnWater, "不能在水域放置装饰物");

            // 检查是否有障碍物
            if (HasObstacleAtPoint(position))
                return PlacementValidationResult.Invalid(PlacementInvalidReason.ObstacleBlocking, "该位置有障碍物");

            return PlacementValidationResult.Valid();
        }
        #endregion

        #region 建筑验证
        /// <summary>
        /// 验证建筑放置位置
        /// </summary>
        private PlacementValidationResult ValidateBuildingPlacement(ItemData item, Vector3 position)
        {
            Vector3 alignedPos = AlignToGrid(position);
            Vector2Int size = item.buildingSize;

            // 检查建筑占用的所有格子
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3 checkPos = alignedPos + new Vector3(x, y, 0);

                    if (IsOnWater(checkPos))
                        return PlacementValidationResult.Invalid(PlacementInvalidReason.OnWater, "建筑区域包含水域");

                    if (HasObstacleAtPoint(checkPos))
                        return PlacementValidationResult.Invalid(PlacementInvalidReason.BuildingOverlap, "建筑区域有障碍物");
                }
            }

            return PlacementValidationResult.Valid();
        }
        #endregion

        #region 家具验证
        /// <summary>
        /// 验证家具放置位置
        /// </summary>
        private PlacementValidationResult ValidateFurniturePlacement(ItemData item, Vector3 position)
        {
            // TODO: 检查是否在室内
            // 目前简单处理，与装饰物相同
            return ValidateDecorationPlacement(item, position);
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 检查是否在玩家交互范围内
        /// </summary>
        public bool IsWithinPlayerRange(Vector3 position, Transform playerTransform)
        {
            if (playerTransform == null) return false;
            
            // 使用玩家 Collider 中心
            var playerCollider = playerTransform.GetComponent<Collider2D>();
            Vector3 playerCenter = playerCollider != null 
                ? playerCollider.bounds.center 
                : playerTransform.position;

            float distance = Vector3.Distance(position, playerCenter);
            return distance <= playerInteractionRange;
        }

        /// <summary>
        /// 检查是否在耕地上
        /// </summary>
        public bool IsOnFarmland(Vector3 position)
        {
            // TODO: 与 FarmingSystem 集成
            // 目前返回 false，表示不在耕地上
            return false;
        }

        /// <summary>
        /// 检查是否在水域
        /// </summary>
        public bool IsOnWater(Vector3 position)
        {
            Collider2D hit = Physics2D.OverlapPoint(position, waterLayer);
            return hit != null;
        }

        /// <summary>
        /// 检查指定点是否有障碍物
        /// </summary>
        public bool HasObstacleAtPoint(Vector3 position)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, 0.1f);
            foreach (var hit in hits)
            {
                if (HasAnyTag(hit.transform, obstacleTags))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 对齐到整数网格
        /// </summary>
        public Vector3 AlignToGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x),
                Mathf.Round(position.y),
                position.z
            );
        }

        /// <summary>
        /// 检查 Transform 或其父级是否有指定标签
        /// </summary>
        private bool HasAnyTag(Transform t, string[] tags)
        {
            Transform current = t;
            while (current != null)
            {
                foreach (var tag in tags)
                {
                    if (current.CompareTag(tag))
                        return true;
                }
                current = current.parent;
            }
            return false;
        }
        #endregion

        #region 配置方法
        /// <summary>
        /// 设置玩家交互范围
        /// </summary>
        public void SetInteractionRange(float range)
        {
            playerInteractionRange = range;
        }

        /// <summary>
        /// 设置障碍物检测标签
        /// </summary>
        public void SetObstacleTags(string[] tags)
        {
            obstacleTags = tags;
        }
        #endregion
    }
