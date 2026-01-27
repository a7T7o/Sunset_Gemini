using UnityEngine;
using UnityEngine.Tilemaps;

namespace FarmGame.Farm
{
    /// <summary>
    /// 耕地边界管理器
    /// 负责计算和更新耕地边界 Tile
    /// 
    /// 核心概念：
    /// - 中心块 (C)：玩家锄地创建的"真正的耕地"
    /// - 边界 Tile：围绕中心块的视觉装饰，不是耕地
    /// - 阴影 Tile：对角线方向有中心块时显示
    /// 
    /// 命名规则：
    /// - U/D/L/R 表示该方向有中心块
    /// - 边界内容贴着有中心块的那一侧
    /// </summary>
    public class FarmlandBorderManager : MonoBehaviour
    {
        #region 单例
        
        public static FarmlandBorderManager Instance { get; private set; }
        
        #endregion

        #region Tile 资源配置
        
        [Header("中心块")]
        [Tooltip("耕地中心块 Tile - 未施肥 (C0)")]
        [SerializeField] private TileBase centerTileUnfertilized;
        
        [Tooltip("耕地中心块 Tile - 已施肥 (C1)")]
        [SerializeField] private TileBase centerTileFertilized;
        
        [Header("单方向边界")]
        [Tooltip("上方有中心块，边界贴着顶部 (U)")]
        [SerializeField] private TileBase borderU;
        [Tooltip("下方有中心块，边界贴着底部 (D)")]
        [SerializeField] private TileBase borderD;
        [Tooltip("左方有中心块，边界贴着左侧 (L)")]
        [SerializeField] private TileBase borderL;
        [Tooltip("右方有中心块，边界贴着右侧 (R)")]
        [SerializeField] private TileBase borderR;
        
        [Header("双方向边界 - 对边")]
        [Tooltip("上下都有中心块，左右贯通 (UD)")]
        [SerializeField] private TileBase borderUD;
        [Tooltip("左右都有中心块，上下贯通 (LR)")]
        [SerializeField] private TileBase borderLR;
        
        [Header("双方向边界 - 相邻")]
        [Tooltip("上方和左方有中心块 (UL)")]
        [SerializeField] private TileBase borderUL;
        [Tooltip("上方和右方有中心块 (UR)")]
        [SerializeField] private TileBase borderUR;
        [Tooltip("下方和左方有中心块 (DL)")]
        [SerializeField] private TileBase borderDL;
        [Tooltip("下方和右方有中心块 (DR)")]
        [SerializeField] private TileBase borderDR;
        
        [Header("三方向边界")]
        [Tooltip("上下左都有中心块，只有右侧有边界线 (UDL)")]
        [SerializeField] private TileBase borderUDL;
        [Tooltip("上下右都有中心块，只有左侧有边界线 (UDR)")]
        [SerializeField] private TileBase borderUDR;
        [Tooltip("上左右都有中心块，只有底部有边界线 (ULR)")]
        [SerializeField] private TileBase borderULR;
        [Tooltip("下左右都有中心块，只有顶部有边界线 (DLR)")]
        [SerializeField] private TileBase borderDLR;
        
        [Header("四方向边界")]
        [Tooltip("四周都有中心块，无边界线 (UDLR)")]
        [SerializeField] private TileBase borderUDLR;
        
        [Header("角落阴影")]
        [Tooltip("左上角阴影 (SLU)")]
        [SerializeField] private TileBase shadowLU;
        [Tooltip("右上角阴影 (SRU)")]
        [SerializeField] private TileBase shadowRU;
        [Tooltip("左下角阴影 (SLD)")]
        [SerializeField] private TileBase shadowLD;
        [Tooltip("右下角阴影 (SRD)")]
        [SerializeField] private TileBase shadowRD;
        
        #endregion

        #region Debug
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        #endregion

        #region 生命周期
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        #endregion


        #region 公开接口
        
        /// <summary>
        /// 当中心块被放置时调用
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <param name="cellPosition">格子坐标</param>
        /// <param name="isFertilized">是否已施肥</param>
        public void OnCenterBlockPlaced(int layerIndex, Vector3Int cellPosition, bool isFertilized = false)
        {
            var tilemaps = FarmTileManager.Instance?.GetLayerTilemaps(layerIndex);
            if (tilemaps == null || !tilemaps.IsNewFarmlandSystemValid())
            {
                if (showDebugInfo)
                    Debug.LogWarning($"[FarmlandBorderManager] 楼层 {layerIndex} 的新版耕地 Tilemap 未配置");
                return;
            }
            
            // 1. 放置中心块（根据施肥状态选择）
            TileBase centerTile = isFertilized ? centerTileFertilized : centerTileUnfertilized;
            tilemaps.farmlandCenterTilemap.SetTile(cellPosition, centerTile);
            
            // 2. 清除该位置的边界（如果有）
            tilemaps.farmlandBorderTilemap.SetTile(cellPosition, null);
            
            // 3. 更新周围边界
            UpdateBordersAround(layerIndex, cellPosition);
            
            if (showDebugInfo)
                Debug.Log($"[FarmlandBorderManager] 放置中心块: Layer={layerIndex}, Pos={cellPosition}, Fertilized={isFertilized}");
        }
        
        /// <summary>
        /// 更新中心块的施肥状态
        /// </summary>
        public void UpdateCenterBlockFertilized(int layerIndex, Vector3Int cellPosition, bool isFertilized)
        {
            var tilemaps = FarmTileManager.Instance?.GetLayerTilemaps(layerIndex);
            if (tilemaps == null || !tilemaps.IsNewFarmlandSystemValid())
            {
                return;
            }
            
            // 只更新中心块 Tile，不影响边界
            TileBase centerTile = isFertilized ? centerTileFertilized : centerTileUnfertilized;
            tilemaps.farmlandCenterTilemap.SetTile(cellPosition, centerTile);
        }
        
        /// <summary>
        /// 当中心块被移除时调用
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <param name="cellPosition">格子坐标</param>
        public void OnCenterBlockRemoved(int layerIndex, Vector3Int cellPosition)
        {
            var tilemaps = FarmTileManager.Instance?.GetLayerTilemaps(layerIndex);
            if (tilemaps == null || !tilemaps.IsNewFarmlandSystemValid())
            {
                return;
            }
            
            // 1. 移除中心块
            tilemaps.farmlandCenterTilemap.SetTile(cellPosition, null);
            
            // 2. 更新周围边界（包括该位置本身，可能变成边界）
            UpdateBordersAround(layerIndex, cellPosition);
            
            // 3. 更新该位置本身（可能变成边界或阴影）
            UpdateBorderAt(layerIndex, cellPosition);
            
            if (showDebugInfo)
                Debug.Log($"[FarmlandBorderManager] 移除中心块: Layer={layerIndex}, Pos={cellPosition}");
        }
        
        #endregion

        #region 边界更新
        
        /// <summary>
        /// 更新指定位置周围 3×3 范围的边界
        /// </summary>
        public void UpdateBordersAround(int layerIndex, Vector3Int centerPosition)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // 跳过中心
                    
                    Vector3Int neighborPos = centerPosition + new Vector3Int(dx, dy, 0);
                    UpdateBorderAt(layerIndex, neighborPos);
                }
            }
        }
        
        /// <summary>
        /// 更新指定位置的边界 Tile
        /// </summary>
        private void UpdateBorderAt(int layerIndex, Vector3Int position)
        {
            var tilemaps = FarmTileManager.Instance?.GetLayerTilemaps(layerIndex);
            if (tilemaps == null || !tilemaps.IsNewFarmlandSystemValid())
            {
                return;
            }
            
            // 如果该位置是中心块，不需要边界
            if (IsCenterBlock(layerIndex, position))
            {
                tilemaps.farmlandBorderTilemap.SetTile(position, null);
                return;
            }
            
            // 检查周围中心块分布
            var neighbors = CheckNeighborCenters(layerIndex, position);
            
            // 选择边界 Tile
            TileBase borderTile = SelectBorderTile(
                neighbors.hasU, neighbors.hasD, neighbors.hasL, neighbors.hasR);
            
            if (borderTile != null)
            {
                // 放置边界
                tilemaps.farmlandBorderTilemap.SetTile(position, borderTile);
            }
            else
            {
                // 检查是否需要放置阴影
                TileBase shadowTile = SelectShadowTile(
                    neighbors.hasU, neighbors.hasD, neighbors.hasL, neighbors.hasR,
                    neighbors.hasLU, neighbors.hasRU, neighbors.hasLD, neighbors.hasRD);
                
                tilemaps.farmlandBorderTilemap.SetTile(position, shadowTile);
            }
        }
        
        #endregion

        #region 中心块检测
        
        /// <summary>
        /// 检查指定位置是否是中心块
        /// </summary>
        private bool IsCenterBlock(int layerIndex, Vector3Int position)
        {
            var tileData = FarmTileManager.Instance?.GetTileData(layerIndex, position);
            return tileData != null && tileData.isTilled;
        }
        
        /// <summary>
        /// 检查指定位置周围 8 个方向的中心块分布
        /// </summary>
        private (bool hasU, bool hasD, bool hasL, bool hasR, 
                 bool hasLU, bool hasRU, bool hasLD, bool hasRD) 
            CheckNeighborCenters(int layerIndex, Vector3Int position)
        {
            return (
                hasU:  IsCenterBlock(layerIndex, position + new Vector3Int(0, 1, 0)),
                hasD:  IsCenterBlock(layerIndex, position + new Vector3Int(0, -1, 0)),
                hasL:  IsCenterBlock(layerIndex, position + new Vector3Int(-1, 0, 0)),
                hasR:  IsCenterBlock(layerIndex, position + new Vector3Int(1, 0, 0)),
                hasLU: IsCenterBlock(layerIndex, position + new Vector3Int(-1, 1, 0)),
                hasRU: IsCenterBlock(layerIndex, position + new Vector3Int(1, 1, 0)),
                hasLD: IsCenterBlock(layerIndex, position + new Vector3Int(-1, -1, 0)),
                hasRD: IsCenterBlock(layerIndex, position + new Vector3Int(1, -1, 0))
            );
        }
        
        #endregion


        #region 边界选择算法
        
        /// <summary>
        /// 根据周围中心块分布选择边界 Tile
        /// </summary>
        private TileBase SelectBorderTile(bool hasU, bool hasD, bool hasL, bool hasR)
        {
            int count = (hasU ? 1 : 0) + (hasD ? 1 : 0) + (hasL ? 1 : 0) + (hasR ? 1 : 0);
            
            switch (count)
            {
                case 0:
                    return null; // 无边界，可能是阴影
                    
                case 1:
                    if (hasU) return borderU;
                    if (hasD) return borderD;
                    if (hasL) return borderL;
                    if (hasR) return borderR;
                    break;
                    
                case 2:
                    // 对边
                    if (hasU && hasD) return borderUD;
                    if (hasL && hasR) return borderLR;
                    // 相邻
                    if (hasU && hasL) return borderUL;
                    if (hasU && hasR) return borderUR;
                    if (hasD && hasL) return borderDL;
                    if (hasD && hasR) return borderDR;
                    break;
                    
                case 3:
                    if (!hasR) return borderUDL;
                    if (!hasL) return borderUDR;
                    if (!hasD) return borderULR;
                    if (!hasU) return borderDLR;
                    break;
                    
                case 4:
                    return borderUDLR;
            }
            
            return null;
        }
        
        /// <summary>
        /// 选择阴影 Tile（只有四个正方向都没有中心块时才检查）
        /// </summary>
        private TileBase SelectShadowTile(bool hasU, bool hasD, bool hasL, bool hasR,
                                          bool hasLU, bool hasRU, bool hasLD, bool hasRD)
        {
            // 只有四个正方向都没有中心块时，才放置阴影
            if (hasU || hasD || hasL || hasR)
            {
                return null;
            }
            
            // 检查对角线，优先级：左上 > 右上 > 左下 > 右下
            if (hasLU) return shadowLU;
            if (hasRU) return shadowRU;
            if (hasLD) return shadowLD;
            if (hasRD) return shadowRD;
            
            return null;
        }
        
        #endregion

        #region 批量操作
        
        /// <summary>
        /// 刷新指定楼层的所有边界
        /// </summary>
        public void RefreshAllBorders(int layerIndex)
        {
            var tilemaps = FarmTileManager.Instance?.GetLayerTilemaps(layerIndex);
            if (tilemaps == null || !tilemaps.IsNewFarmlandSystemValid())
            {
                return;
            }
            
            // 清除所有边界
            tilemaps.farmlandBorderTilemap.ClearAllTiles();
            
            // 收集所有中心块位置
            var centerPositions = new System.Collections.Generic.HashSet<Vector3Int>();
            foreach (var tileData in FarmTileManager.Instance.GetAllTilesInLayer(layerIndex))
            {
                if (tileData.isTilled)
                {
                    centerPositions.Add(tileData.position);
                    
                    // 确保中心块 Tile 存在（根据施肥状态选择）
                    // TODO: 需要从 tileData 获取施肥状态
                    TileBase centerTile = centerTileUnfertilized; // 默认未施肥
                    tilemaps.farmlandCenterTilemap.SetTile(tileData.position, centerTile);
                }
            }
            
            // 收集所有需要更新的边界位置
            var borderPositions = new System.Collections.Generic.HashSet<Vector3Int>();
            foreach (var centerPos in centerPositions)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        Vector3Int neighborPos = centerPos + new Vector3Int(dx, dy, 0);
                        if (!centerPositions.Contains(neighborPos))
                        {
                            borderPositions.Add(neighborPos);
                        }
                    }
                }
            }
            
            // 更新所有边界
            foreach (var borderPos in borderPositions)
            {
                UpdateBorderAt(layerIndex, borderPos);
            }
            
            if (showDebugInfo)
                Debug.Log($"[FarmlandBorderManager] 刷新楼层 {layerIndex} 边界: {centerPositions.Count} 个中心块, {borderPositions.Count} 个边界位置");
        }
        
        /// <summary>
        /// 刷新所有楼层的边界
        /// </summary>
        public void RefreshAllLayersBorders()
        {
            if (FarmTileManager.Instance == null) return;
            
            for (int i = 0; i < FarmTileManager.Instance.LayerCount; i++)
            {
                RefreshAllBorders(i);
            }
        }
        
        #endregion

        #region 调试
        
        /// <summary>
        /// 验证 Tile 资源配置
        /// </summary>
        public bool ValidateTileResources()
        {
            bool isValid = true;
            
            if (centerTileUnfertilized == null) { Debug.LogError("[FarmlandBorderManager] centerTileUnfertilized (C0) 未配置"); isValid = false; }
            if (centerTileFertilized == null) { Debug.LogError("[FarmlandBorderManager] centerTileFertilized (C1) 未配置"); isValid = false; }
            
            if (borderU == null) { Debug.LogError("[FarmlandBorderManager] borderU 未配置"); isValid = false; }
            if (borderD == null) { Debug.LogError("[FarmlandBorderManager] borderD 未配置"); isValid = false; }
            if (borderL == null) { Debug.LogError("[FarmlandBorderManager] borderL 未配置"); isValid = false; }
            if (borderR == null) { Debug.LogError("[FarmlandBorderManager] borderR 未配置"); isValid = false; }
            
            if (borderUD == null) { Debug.LogError("[FarmlandBorderManager] borderUD 未配置"); isValid = false; }
            if (borderLR == null) { Debug.LogError("[FarmlandBorderManager] borderLR 未配置"); isValid = false; }
            
            if (borderUL == null) { Debug.LogError("[FarmlandBorderManager] borderUL 未配置"); isValid = false; }
            if (borderUR == null) { Debug.LogError("[FarmlandBorderManager] borderUR 未配置"); isValid = false; }
            if (borderDL == null) { Debug.LogError("[FarmlandBorderManager] borderDL 未配置"); isValid = false; }
            if (borderDR == null) { Debug.LogError("[FarmlandBorderManager] borderDR 未配置"); isValid = false; }
            
            if (borderUDL == null) { Debug.LogError("[FarmlandBorderManager] borderUDL 未配置"); isValid = false; }
            if (borderUDR == null) { Debug.LogError("[FarmlandBorderManager] borderUDR 未配置"); isValid = false; }
            if (borderULR == null) { Debug.LogError("[FarmlandBorderManager] borderULR 未配置"); isValid = false; }
            if (borderDLR == null) { Debug.LogError("[FarmlandBorderManager] borderDLR 未配置"); isValid = false; }
            
            if (borderUDLR == null) { Debug.LogError("[FarmlandBorderManager] borderUDLR 未配置"); isValid = false; }
            
            if (shadowLU == null) { Debug.LogWarning("[FarmlandBorderManager] shadowLU 未配置（可选）"); }
            if (shadowRU == null) { Debug.LogWarning("[FarmlandBorderManager] shadowRU 未配置（可选）"); }
            if (shadowLD == null) { Debug.LogWarning("[FarmlandBorderManager] shadowLD 未配置（可选）"); }
            if (shadowRD == null) { Debug.LogWarning("[FarmlandBorderManager] shadowRD 未配置（可选）"); }
            
            return isValid;
        }
        
        #endregion
    }
}
