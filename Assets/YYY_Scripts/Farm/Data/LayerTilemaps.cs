using UnityEngine;
using UnityEngine.Tilemaps;

namespace FarmGame.Farm
{
    /// <summary>
    /// 楼层 Tilemap 配置
    /// 每个楼层（LAYER 1/2/3）有独立的 Tilemap 集合
    /// </summary>
    [System.Serializable]
    public class LayerTilemaps
    {
        /// <summary>
        /// 楼层名称（"LAYER 1", "LAYER 2", "LAYER 3"）
        /// </summary>
        [Tooltip("楼层名称，用于调试和识别")]
        public string layerName;
        
        /// <summary>
        /// [旧版] 耕地 Tilemap（使用 Rule Tile 自动拼接）
        /// 注意：新版系统使用 farmlandCenterTilemap + farmlandBorderTilemap
        /// 此字段保留用于兼容旧场景配置
        /// </summary>
        [Tooltip("【旧版】耕地 Tilemap，新版请使用 farmlandCenterTilemap")]
        public Tilemap farmlandTilemap;
        
        /// <summary>
        /// [旧版] 水渍叠加 Tilemap（浇水后显示）
        /// 注意：新版系统的水渍功能待实现
        /// 此字段保留用于兼容旧场景配置
        /// </summary>
        [Tooltip("【旧版】水渍叠加 Tilemap，新版水渍功能待实现")]
        public Tilemap waterPuddleTilemap;
        
        /// <summary>
        /// 耕地中心块 Tilemap（新版耕地系统）
        /// </summary>
        [Header("耕地 Tile 系统（新）")]
        [Tooltip("耕地中心块 Tilemap，只放置中心块 C")]
        public Tilemap farmlandCenterTilemap;
        
        /// <summary>
        /// 耕地边界 Tilemap（新版耕地系统）
        /// </summary>
        [Tooltip("耕地边界 Tilemap，放置边界装饰 Tile")]
        public Tilemap farmlandBorderTilemap;
        
        /// <summary>
        /// 地面 Tilemap（用于检测可耕作区域）
        /// </summary>
        [Tooltip("地面 Tilemap，用于检测该位置是否可以耕作")]
        public Tilemap groundTilemap;
        
        /// <summary>
        /// 楼层的 Transform 根节点（用于放置作物 GameObject）
        /// </summary>
        [Tooltip("楼层的 Props 容器，作物 GameObject 将放置在此下")]
        public Transform propsContainer;
        
        /// <summary>
        /// 检查配置是否有效
        /// 支持新版配置（只配置 farmlandCenterTilemap）和旧版配置
        /// </summary>
        public bool IsValid()
        {
            // 新版配置：只需要 farmlandCenterTilemap
            if (farmlandCenterTilemap != null)
            {
                return true;
            }
            
            // 旧版配置：需要 farmlandTilemap
            return farmlandTilemap != null;
        }
        
        /// <summary>
        /// 检查新版耕地系统配置是否有效
        /// </summary>
        public bool IsNewFarmlandSystemValid()
        {
            return farmlandCenterTilemap != null && farmlandBorderTilemap != null;
        }
        
        /// <summary>
        /// 获取世界坐标对应的格子坐标
        /// 优先使用新版字段 farmlandCenterTilemap，回退到旧版字段
        /// </summary>
        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            // 优先使用新版字段
            if (farmlandCenterTilemap != null)
            {
                return farmlandCenterTilemap.WorldToCell(worldPosition);
            }
            
            // 回退到旧版字段
            if (farmlandTilemap != null)
            {
                return farmlandTilemap.WorldToCell(worldPosition);
            }
            
            // 最后尝试使用 groundTilemap
            if (groundTilemap != null)
            {
                return groundTilemap.WorldToCell(worldPosition);
            }
            
            return Vector3Int.zero;
        }
        
        /// <summary>
        /// 获取格子坐标对应的世界中心坐标
        /// 优先使用新版字段 farmlandCenterTilemap，回退到旧版字段
        /// </summary>
        public Vector3 GetCellCenterWorld(Vector3Int cellPosition)
        {
            // 优先使用新版字段
            if (farmlandCenterTilemap != null)
            {
                return farmlandCenterTilemap.GetCellCenterWorld(cellPosition);
            }
            
            // 回退到旧版字段
            if (farmlandTilemap != null)
            {
                return farmlandTilemap.GetCellCenterWorld(cellPosition);
            }
            
            // 最后尝试使用 groundTilemap
            if (groundTilemap != null)
            {
                return groundTilemap.GetCellCenterWorld(cellPosition);
            }
            
            return Vector3.zero;
        }
    }
}
