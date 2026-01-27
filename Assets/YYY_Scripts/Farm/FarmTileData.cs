using UnityEngine;

namespace FarmGame.Farm
{
    /// <summary>
    /// 单个耕地格子的数据
    /// 支持多楼层、水渍变体、新版作物数据结构
    /// </summary>
    [System.Serializable]
    public class FarmTileData
    {
        #region 位置信息
        
        /// <summary>
        /// 格子在 Tilemap 中的坐标
        /// </summary>
        public Vector3Int position;
        
        /// <summary>
        /// 所属楼层索引（0 = LAYER 1, 1 = LAYER 2, 2 = LAYER 3）
        /// </summary>
        public int layerIndex;
        
        #endregion

        #region 耕地状态
        
        /// <summary>
        /// 是否已耕作
        /// </summary>
        public bool isTilled;
        
        #endregion

        #region 浇水状态（逻辑）
        
        /// <summary>
        /// 今天是否已浇水（记录参数，第二天才生效作物生长）
        /// </summary>
        public bool wateredToday;

        /// <summary>
        /// 昨天是否浇过水（实际影响作物生长）
        /// </summary>
        public bool wateredYesterday;
        
        #endregion

        #region 浇水状态（视觉）
        
        /// <summary>
        /// 浇水的游戏时间（小时，用于计算视觉状态切换）
        /// </summary>
        public float waterTime;

        /// <summary>
        /// 当前土壤湿度视觉状态
        /// </summary>
        public SoilMoistureState moistureState;
        
        /// <summary>
        /// 水渍变体索引（0-2，用于随机选择水渍样式）
        /// </summary>
        public int puddleVariant;
        
        #endregion

        #region 作物数据
        
        /// <summary>
        /// 当前种植的作物实例数据（新版纯数据结构）
        /// </summary>
        public CropInstanceData cropData;
        
        /// <summary>
        /// [已废弃] 旧版作物实例，保留用于兼容
        /// </summary>
        [System.Obsolete("使用 cropData 替代")]
        public CropInstance crop;
        
        #endregion

        #region 构造函数
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public FarmTileData()
        {
            position = Vector3Int.zero;
            layerIndex = 0;
            isTilled = false;
            wateredToday = false;
            wateredYesterday = false;
            waterTime = -1f;
            moistureState = SoilMoistureState.Dry;
            puddleVariant = 0;
            cropData = null;
        }
        
        /// <summary>
        /// 创建指定位置的耕地数据
        /// </summary>
        /// <param name="pos">格子坐标</param>
        /// <param name="layer">楼层索引（默认 0）</param>
        public FarmTileData(Vector3Int pos, int layer = 0)
        {
            position = pos;
            layerIndex = layer;
            isTilled = false;
            wateredToday = false;
            wateredYesterday = false;
            waterTime = -1f;
            moistureState = SoilMoistureState.Dry;
            puddleVariant = 0;
            cropData = null;
        }
        
        #endregion

        #region 状态查询
        
        /// <summary>
        /// 是否可以种植（已耕作且无作物）
        /// </summary>
        public bool CanPlant()
        {
            return isTilled && cropData == null;
        }

        /// <summary>
        /// 是否有作物
        /// </summary>
        public bool HasCrop()
        {
            return cropData != null;
        }
        
        #endregion

        #region 作物操作
        
        /// <summary>
        /// 设置作物数据
        /// </summary>
        /// <param name="data">作物实例数据</param>
        public void SetCropData(CropInstanceData data)
        {
            cropData = data;
        }
        
        /// <summary>
        /// 清除作物数据
        /// </summary>
        public void ClearCropData()
        {
            cropData = null;
        }
        
        /// <summary>
        /// [已废弃] 清除旧版作物数据
        /// </summary>
        [System.Obsolete("使用 ClearCropData() 替代")]
        public void ClearCrop()
        {
            #pragma warning disable 0618
            if (crop != null && crop.cropObject != null)
            {
                Object.Destroy(crop.cropObject);
            }
            crop = null;
            #pragma warning restore 0618
            
            // 同时清除新版数据
            cropData = null;
        }
        
        #endregion

        #region 浇水操作
        
        /// <summary>
        /// 设置浇水状态
        /// </summary>
        /// <param name="currentTime">当前游戏时间（小时）</param>
        /// <param name="variant">水渍变体索引（0-2）</param>
        public void SetWatered(float currentTime, int variant = 0)
        {
            wateredToday = true;
            waterTime = currentTime;
            moistureState = SoilMoistureState.WetWithPuddle;
            puddleVariant = Mathf.Clamp(variant, 0, 2);
        }
        
        /// <summary>
        /// 重置每日浇水状态（每天开始时调用）
        /// </summary>
        public void ResetDailyWaterState()
        {
            wateredYesterday = wateredToday;
            wateredToday = false;
            waterTime = -1f;
            moistureState = SoilMoistureState.Dry;
        }
        
        #endregion
    }
}
