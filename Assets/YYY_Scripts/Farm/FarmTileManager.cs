using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace FarmGame.Farm
{
    /// <summary>
    /// 耕地状态管理器（自治版）
    /// 负责管理多楼层的耕地数据、浇水状态
    /// 直接订阅时间事件，自己处理每日重置和水渍干涸
    /// 不依赖 FarmingManagerNew
    /// </summary>
    public class FarmTileManager : MonoBehaviour
    {
        #region 单例
        
        public static FarmTileManager Instance { get; private set; }
        
        #endregion

        #region 配置
        
        [Header("多楼层配置")]
        [SerializeField] private LayerTilemaps[] layerTilemaps;
        
        [Header("水渍干涸设置")]
        [Tooltip("浇水多少小时后，水渍消失变为深色土壤")]
        [SerializeField] private float hoursUntilPuddleDry = 2f;
        
        [Header("视觉管理器引用")]
        [SerializeField] private FarmVisualManager visualManager;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        #endregion

        #region 数据存储
        
        /// <summary>
        /// 按楼层分组的耕地数据
        /// Key: layerIndex, Value: (cellPosition -> FarmTileData)
        /// </summary>
        private Dictionary<int, Dictionary<Vector3Int, FarmTileData>> farmTilesByLayer;
        
        /// <summary>
        /// 今天浇水的耕地（用于优化 OnHourChanged 遍历）
        /// </summary>
        private HashSet<(int layer, Vector3Int pos)> wateredTodayTiles;
        
        #endregion

        #region 生命周期
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeDataStructures();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnEnable()
        {
            // 订阅时间事件（静态事件）
            TimeManager.OnDayChanged += OnDayChanged;
            TimeManager.OnHourChanged += OnHourChanged;
            
            if (showDebugInfo)
                Debug.Log("[FarmTileManager] 已订阅时间事件");
        }
        
        private void OnDisable()
        {
            // 取消订阅（防止内存泄漏）
            TimeManager.OnDayChanged -= OnDayChanged;
            TimeManager.OnHourChanged -= OnHourChanged;
            
            if (showDebugInfo)
                Debug.Log("[FarmTileManager] 已取消订阅时间事件");
        }
        
        private void Start()
        {
            // 自动获取 FarmVisualManager（如果未手动设置）
            if (visualManager == null)
            {
                visualManager = FindFirstObjectByType<FarmVisualManager>();
            }
        }
        
        private void InitializeDataStructures()
        {
            farmTilesByLayer = new Dictionary<int, Dictionary<Vector3Int, FarmTileData>>();
            wateredTodayTiles = new HashSet<(int, Vector3Int)>();
            
            // 为每个楼层初始化字典
            if (layerTilemaps != null)
            {
                for (int i = 0; i < layerTilemaps.Length; i++)
                {
                    farmTilesByLayer[i] = new Dictionary<Vector3Int, FarmTileData>();
                }
            }
        }
        
        #endregion
        
        #region 时间事件处理（自治）
        
        /// <summary>
        /// 每天开始时触发：重置所有耕地的浇水状态
        /// </summary>
        private void OnDayChanged(int year, int day, int totalDays)
        {
            if (showDebugInfo)
                Debug.Log($"[FarmTileManager] 新的一天开始 - 第{day}天，重置浇水状态");
            
            // 重置所有耕地的每日浇水状态
            ResetDailyWaterState();
            
            // 刷新所有耕地视觉
            if (visualManager != null)
            {
                visualManager.RefreshAllTileVisuals();
            }
        }
        
        /// <summary>
        /// 每小时触发：更新土壤视觉状态（水渍→深色）
        /// </summary>
        private void OnHourChanged(int currentHour)
        {
            var timeManager = TimeManager.Instance;
            if (timeManager == null) return;
            
            float currentTime = timeManager.GetHour() + timeManager.GetMinute() / 60f;
            
            // 只遍历今天浇水的耕地（性能优化）
            foreach (var (layer, pos) in wateredTodayTiles)
            {
                var tileData = GetTileData(layer, pos);
                if (tileData == null) continue;
                
                // 只处理水渍状态的耕地
                if (tileData.moistureState != SoilMoistureState.WetWithPuddle) continue;
                
                float hoursSinceWatering = currentTime - tileData.waterTime;
                
                // 处理跨天情况
                if (hoursSinceWatering < 0)
                    hoursSinceWatering += 20f; // 游戏一天 20 小时
                
                // 超过设定时间：水渍消失，变为深色
                if (hoursSinceWatering >= hoursUntilPuddleDry)
                {
                    tileData.moistureState = SoilMoistureState.WetDark;
                    
                    var tilemaps = GetLayerTilemaps(layer);
                    if (tilemaps != null && visualManager != null)
                    {
                        visualManager.UpdateTileVisual(tilemaps, pos, tileData);
                    }
                    
                    if (showDebugInfo)
                        Debug.Log($"[FarmTileManager] 水渍干涸: Layer={layer}, Pos={pos}");
                }
            }
        }
        
        #endregion

        #region 楼层检测
        
        /// <summary>
        /// 获取玩家当前所在楼层索引
        /// </summary>
        /// <param name="playerPosition">玩家世界坐标</param>
        /// <returns>楼层索引（0 = LAYER 1）</returns>
        public int GetCurrentLayerIndex(Vector3 playerPosition)
        {
            // TODO: 实现基于玩家位置的楼层检测
            // 目前默认返回 0（LAYER 1）
            // 后续可以根据玩家所在的 Transform 父级或 Y 坐标判断
            return 0;
        }
        
        /// <summary>
        /// 获取指定楼层的 Tilemap 配置
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <returns>楼层 Tilemap 配置，无效时返回 null</returns>
        public LayerTilemaps GetLayerTilemaps(int layerIndex)
        {
            if (layerTilemaps == null || layerIndex < 0 || layerIndex >= layerTilemaps.Length)
            {
                return null;
            }
            return layerTilemaps[layerIndex];
        }
        
        /// <summary>
        /// 获取楼层数量
        /// </summary>
        public int LayerCount => layerTilemaps?.Length ?? 0;
        
        #endregion

        #region 耕地 CRUD 操作
        
        /// <summary>
        /// 获取指定位置的耕地数据
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <param name="cellPosition">格子坐标</param>
        /// <returns>耕地数据，不存在时返回 null</returns>
        public FarmTileData GetTileData(int layerIndex, Vector3Int cellPosition)
        {
            if (!farmTilesByLayer.TryGetValue(layerIndex, out var layerTiles))
            {
                return null;
            }
            
            layerTiles.TryGetValue(cellPosition, out FarmTileData data);
            return data;
        }
        
        /// <summary>
        /// 创建耕地
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <param name="cellPosition">格子坐标</param>
        /// <returns>是否创建成功</returns>
        public bool CreateTile(int layerIndex, Vector3Int cellPosition)
        {
            // 检查楼层是否有效
            if (!farmTilesByLayer.TryGetValue(layerIndex, out var layerTiles))
            {
                if (showDebugInfo)
                    Debug.LogWarning($"[FarmTileManager] 无效的楼层索引: {layerIndex}");
                return false;
            }
            
            // 检查是否已存在耕地
            if (layerTiles.ContainsKey(cellPosition))
            {
                var existingTile = layerTiles[cellPosition];
                if (existingTile.isTilled)
                {
                    if (showDebugInfo)
                        Debug.Log($"[FarmTileManager] 该位置已有耕地: Layer={layerIndex}, Pos={cellPosition}");
                    return false;
                }
            }
            
            // 检查是否可以耕作（有地面 Tile）
            var tilemaps = GetLayerTilemaps(layerIndex);
            if (tilemaps == null || !tilemaps.IsValid())
            {
                Debug.LogError($"[FarmTileManager] 楼层 {layerIndex} 的 Tilemap 配置无效");
                return false;
            }
            
            if (tilemaps.groundTilemap != null)
            {
                TileBase groundTile = tilemaps.groundTilemap.GetTile(cellPosition);
                if (groundTile == null)
                {
                    if (showDebugInfo)
                        Debug.Log($"[FarmTileManager] 该位置没有地面 Tile: {cellPosition}");
                    return false;
                }
            }
            
            // 创建耕地数据
            FarmTileData newTile = new FarmTileData(cellPosition, layerIndex);
            newTile.isTilled = true;
            newTile.moistureState = SoilMoistureState.Dry;
            
            layerTiles[cellPosition] = newTile;
            
            // 通知边界管理器更新边界
            if (FarmlandBorderManager.Instance != null)
            {
                FarmlandBorderManager.Instance.OnCenterBlockPlaced(layerIndex, cellPosition);
            }
            
            if (showDebugInfo)
                Debug.Log($"[FarmTileManager] 创建耕地: Layer={layerIndex}, Pos={cellPosition}");
            
            return true;
        }
        
        /// <summary>
        /// 检查指定位置是否可以耕作
        /// </summary>
        public bool CanTillAt(int layerIndex, Vector3Int cellPosition)
        {
            // 检查楼层是否有效
            if (!farmTilesByLayer.TryGetValue(layerIndex, out var layerTiles))
            {
                return false;
            }
            
            // 检查是否已存在耕地
            if (layerTiles.TryGetValue(cellPosition, out var existingTile))
            {
                if (existingTile.isTilled)
                {
                    return false;
                }
            }
            
            // 检查是否有地面 Tile
            var tilemaps = GetLayerTilemaps(layerIndex);
            if (tilemaps == null || !tilemaps.IsValid())
            {
                return false;
            }
            
            if (tilemaps.groundTilemap != null)
            {
                TileBase groundTile = tilemaps.groundTilemap.GetTile(cellPosition);
                if (groundTile == null)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 移除耕地
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <param name="cellPosition">格子坐标</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveTile(int layerIndex, Vector3Int cellPosition)
        {
            // 检查楼层是否有效
            if (!farmTilesByLayer.TryGetValue(layerIndex, out var layerTiles))
            {
                return false;
            }
            
            // 检查是否存在耕地
            if (!layerTiles.TryGetValue(cellPosition, out var existingTile) || !existingTile.isTilled)
            {
                return false;
            }
            
            // 移除耕地数据
            layerTiles.Remove(cellPosition);
            
            // 从今天浇水集合中移除
            wateredTodayTiles.Remove((layerIndex, cellPosition));
            
            // 通知边界管理器更新边界
            if (FarmlandBorderManager.Instance != null)
            {
                FarmlandBorderManager.Instance.OnCenterBlockRemoved(layerIndex, cellPosition);
            }
            
            if (showDebugInfo)
                Debug.Log($"[FarmTileManager] 移除耕地: Layer={layerIndex}, Pos={cellPosition}");
            
            return true;
        }
        
        #endregion

        #region 浇水状态管理
        
        /// <summary>
        /// 设置耕地浇水状态
        /// </summary>
        /// <param name="layerIndex">楼层索引</param>
        /// <param name="cellPosition">格子坐标</param>
        /// <param name="waterTime">浇水时间（游戏小时）</param>
        /// <param name="puddleVariant">水渍变体索引（0-2）</param>
        /// <returns>是否设置成功</returns>
        public bool SetWatered(int layerIndex, Vector3Int cellPosition, float waterTime, int puddleVariant = -1)
        {
            var tileData = GetTileData(layerIndex, cellPosition);
            if (tileData == null || !tileData.isTilled)
            {
                return false;
            }
            
            // 今天已经浇过水
            if (tileData.wateredToday)
            {
                return false;
            }
            
            // 随机水渍变体
            if (puddleVariant < 0)
            {
                puddleVariant = Random.Range(0, 3);
            }
            
            tileData.SetWatered(waterTime, puddleVariant);
            
            // 添加到今天浇水的集合
            wateredTodayTiles.Add((layerIndex, cellPosition));
            
            if (showDebugInfo)
                Debug.Log($"[FarmTileManager] 浇水: Layer={layerIndex}, Pos={cellPosition}, Time={waterTime:F1}");
            
            return true;
        }
        
        /// <summary>
        /// 重置所有耕地的每日浇水状态（每天开始时调用）
        /// </summary>
        public void ResetDailyWaterState()
        {
            int resetCount = 0;
            
            foreach (var layerKvp in farmTilesByLayer)
            {
                foreach (var tileKvp in layerKvp.Value)
                {
                    FarmTileData tile = tileKvp.Value;
                    if (tile.isTilled)
                    {
                        tile.ResetDailyWaterState();
                        resetCount++;
                    }
                }
            }
            
            // 清空今天浇水的集合
            wateredTodayTiles.Clear();
            
            if (showDebugInfo)
                Debug.Log($"[FarmTileManager] 重置每日浇水状态: {resetCount} 块耕地");
        }
        
        /// <summary>
        /// 获取今天浇水的耕地集合（用于优化 OnHourChanged 遍历）
        /// </summary>
        public IEnumerable<(int layer, Vector3Int pos)> GetWateredTodayTiles()
        {
            return wateredTodayTiles;
        }
        
        #endregion

        #region 遍历方法
        
        /// <summary>
        /// 获取指定楼层的所有耕地数据
        /// </summary>
        public IEnumerable<FarmTileData> GetAllTilesInLayer(int layerIndex)
        {
            if (farmTilesByLayer.TryGetValue(layerIndex, out var layerTiles))
            {
                return layerTiles.Values;
            }
            return System.Array.Empty<FarmTileData>();
        }
        
        /// <summary>
        /// 获取所有楼层的所有耕地数据
        /// </summary>
        public IEnumerable<FarmTileData> GetAllTiles()
        {
            foreach (var layerKvp in farmTilesByLayer)
            {
                foreach (var tileKvp in layerKvp.Value)
                {
                    yield return tileKvp.Value;
                }
            }
        }
        
        /// <summary>
        /// 获取所有有作物的耕地
        /// </summary>
        public IEnumerable<FarmTileData> GetAllTilesWithCrops()
        {
            foreach (var tile in GetAllTiles())
            {
                if (tile.HasCrop())
                {
                    yield return tile;
                }
            }
        }
        
        #endregion

        #region 调试
        
        private void OnDrawGizmos()
        {
            if (!showDebugInfo || farmTilesByLayer == null) return;
            
            foreach (var layerKvp in farmTilesByLayer)
            {
                int layerIndex = layerKvp.Key;
                var tilemaps = GetLayerTilemaps(layerIndex);
                if (tilemaps == null || tilemaps.farmlandTilemap == null) continue;
                
                foreach (var tileKvp in layerKvp.Value)
                {
                    FarmTileData data = tileKvp.Value;
                    if (!data.isTilled) continue;
                    
                    Vector3 worldPos = tilemaps.GetCellCenterWorld(tileKvp.Key);
                    
                    // 根据状态设置颜色
                    if (data.HasCrop())
                    {
                        Gizmos.color = data.cropData.isWithered ? Color.red : Color.green;
                    }
                    else if (data.moistureState == SoilMoistureState.WetWithPuddle)
                    {
                        Gizmos.color = Color.cyan;
                    }
                    else if (data.moistureState == SoilMoistureState.WetDark)
                    {
                        Gizmos.color = Color.blue;
                    }
                    else
                    {
                        Gizmos.color = new Color(0.6f, 0.4f, 0.2f);
                    }
                    
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.3f);
                }
            }
        }
        
        #endregion
    }
}
