using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Data.Core
{
    /// <summary>
    /// 存档数据传输对象（DTOs）
    /// 
    /// 设计原则：
    /// - 纯数据类，无逻辑
    /// - 支持 JSON 序列化（Unity JsonUtility 或 Newtonsoft.Json）
    /// - 与运行时对象解耦
    /// - 版本兼容性考虑
    /// </summary>
    
    #region 游戏存档根结构
    
    /// <summary>
    /// 游戏存档根数据
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        /// <summary>存档版本号（用于兼容性处理）</summary>
        public int version = 1;
        
        /// <summary>存档创建时间</summary>
        public string createdTime;
        
        /// <summary>最后保存时间</summary>
        public string lastSaveTime;
        
        /// <summary>游戏时间数据</summary>
        public GameTimeSaveData gameTime;
        
        /// <summary>玩家数据</summary>
        public PlayerSaveData player;
        
        /// <summary>背包数据</summary>
        public InventorySaveData inventory;
        
        /// <summary>世界对象数据</summary>
        public List<WorldObjectSaveData> worldObjects;
        
        /// <summary>农田数据</summary>
        public List<FarmTileSaveData> farmTiles;
        
        public GameSaveData()
        {
            createdTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lastSaveTime = createdTime;
            worldObjects = new List<WorldObjectSaveData>();
            farmTiles = new List<FarmTileSaveData>();
        }
    }
    
    #endregion
    
    #region 游戏时间
    
    /// <summary>
    /// 游戏时间存档数据
    /// </summary>
    [Serializable]
    public class GameTimeSaveData
    {
        /// <summary>当前天数</summary>
        public int day = 1;
        
        /// <summary>当前季节（0=春, 1=夏, 2=秋, 3=冬）</summary>
        public int season = 0;
        
        /// <summary>当前年份</summary>
        public int year = 1;
        
        /// <summary>当前小时（0-23）</summary>
        public int hour = 6;
        
        /// <summary>当前分钟（0-59）</summary>
        public int minute = 0;
    }
    
    #endregion
    
    #region 玩家数据
    
    /// <summary>
    /// 玩家存档数据
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        /// <summary>玩家位置 X</summary>
        public float positionX;
        
        /// <summary>玩家位置 Y</summary>
        public float positionY;
        
        /// <summary>当前场景名称</summary>
        public string sceneName;
        
        /// <summary>当前楼层</summary>
        public int currentLayer = 1;
        
        /// <summary>当前选中的快捷栏槽位</summary>
        public int selectedHotbarSlot = 0;
        
        /// <summary>金币数量</summary>
        public int gold = 0;
        
        /// <summary>当前体力</summary>
        public int stamina = 100;
        
        /// <summary>最大体力</summary>
        public int maxStamina = 100;
    }
    
    #endregion
    
    #region 背包数据
    
    /// <summary>
    /// 背包存档数据
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        /// <summary>背包容量</summary>
        public int capacity = 36;
        
        /// <summary>所有槽位数据</summary>
        public List<InventorySlotSaveData> slots;
        
        public InventorySaveData()
        {
            slots = new List<InventorySlotSaveData>();
        }
    }
    
    /// <summary>
    /// 背包槽位存档数据
    /// </summary>
    [Serializable]
    public class InventorySlotSaveData
    {
        /// <summary>槽位索引</summary>
        public int slotIndex;
        
        /// <summary>物品定义 ID（-1 表示空）</summary>
        public int itemId = -1;
        
        /// <summary>物品品质</summary>
        public int quality = 0;
        
        /// <summary>堆叠数量</summary>
        public int amount = 0;
        
        /// <summary>物品实例 ID（用于关联动态属性）</summary>
        public string instanceId;
        
        /// <summary>当前耐久度（-1 表示无耐久度）</summary>
        public int currentDurability = -1;
        
        /// <summary>最大耐久度（-1 表示无耐久度）</summary>
        public int maxDurability = -1;
        
        /// <summary>动态属性（序列化为 JSON 字符串列表）</summary>
        public List<PropertyEntrySaveData> properties;
        
        public InventorySlotSaveData()
        {
            properties = new List<PropertyEntrySaveData>();
        }
        
        /// <summary>是否为空槽位</summary>
        public bool IsEmpty => itemId < 0 || amount <= 0;
    }
    
    /// <summary>
    /// 属性条目存档数据
    /// </summary>
    [Serializable]
    public class PropertyEntrySaveData
    {
        public string key;
        public string value;
        
        public PropertyEntrySaveData() { }
        
        public PropertyEntrySaveData(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }
    
    #endregion
    
    #region 世界对象数据
    
    /// <summary>
    /// 世界对象存档数据
    /// 用于存储场景中的动态对象（箱子、作物、放置的物品等）
    /// </summary>
    [Serializable]
    public class WorldObjectSaveData
    {
        /// <summary>对象唯一 ID（GUID）</summary>
        public string guid;
        
        /// <summary>对象类型标识（用于反序列化时创建正确的对象）</summary>
        public string objectType;
        
        /// <summary>预制体路径或 ID（用于实例化）</summary>
        public string prefabId;
        
        /// <summary>所在场景名称</summary>
        public string sceneName;
        
        /// <summary>所在楼层</summary>
        public int layer = 1;
        
        /// <summary>位置 X</summary>
        public float positionX;
        
        /// <summary>位置 Y</summary>
        public float positionY;
        
        /// <summary>位置 Z</summary>
        public float positionZ;
        
        /// <summary>旋转角度（2D 游戏通常只用 Z 轴）</summary>
        public float rotationZ;
        
        /// <summary>是否激活</summary>
        public bool isActive = true;
        
        /// <summary>通用数据（JSON 字符串，存储对象特有数据）</summary>
        public string genericData;
        
        /// <summary>
        /// 设置位置
        /// </summary>
        public void SetPosition(Vector3 pos)
        {
            positionX = pos.x;
            positionY = pos.y;
            positionZ = pos.z;
        }
        
        /// <summary>
        /// 获取位置
        /// </summary>
        public Vector3 GetPosition()
        {
            return new Vector3(positionX, positionY, positionZ);
        }
    }
    
    #endregion
    
    #region 特定对象数据
    
    /// <summary>
    /// 箱子存档数据（存储在 WorldObjectSaveData.genericData 中）
    /// </summary>
    [Serializable]
    public class ChestSaveData
    {
        /// <summary>箱子容量</summary>
        public int capacity = 20;
        
        /// <summary>箱子内物品</summary>
        public List<InventorySlotSaveData> slots;
        
        /// <summary>是否锁定</summary>
        public bool isLocked = false;
        
        /// <summary>自定义名称</summary>
        public string customName;
        
        public ChestSaveData()
        {
            slots = new List<InventorySlotSaveData>();
        }
    }
    
    /// <summary>
    /// 树木存档数据（存储在 WorldObjectSaveData.genericData 中）
    /// </summary>
    [Serializable]
    public class TreeSaveData
    {
        /// <summary>生长阶段索引</summary>
        public int growthStageIndex;
        
        /// <summary>当前血量</summary>
        public int currentHealth;
        
        /// <summary>最大血量</summary>
        public int maxHealth;
        
        /// <summary>已生长天数</summary>
        public int daysGrown;
        
        /// <summary>树木状态（0=正常, 1=被砍, 2=树桩）</summary>
        public int state;
    }
    
    /// <summary>
    /// 农田格子存档数据
    /// </summary>
    [Serializable]
    public class FarmTileSaveData
    {
        /// <summary>格子位置 X（Tilemap 坐标）</summary>
        public int tileX;
        
        /// <summary>格子位置 Y（Tilemap 坐标）</summary>
        public int tileY;
        
        /// <summary>所在楼层</summary>
        public int layer = 1;
        
        /// <summary>土地状态（0=未耕作, 1=已耕作, 2=已浇水）</summary>
        public int soilState;
        
        /// <summary>种植的作物 ID（-1 表示无作物）</summary>
        public int cropId = -1;
        
        /// <summary>作物生长阶段</summary>
        public int cropGrowthStage;
        
        /// <summary>作物品质</summary>
        public int cropQuality;
        
        /// <summary>已生长天数</summary>
        public int daysGrown;
        
        /// <summary>是否已浇水（当天）</summary>
        public bool isWatered;
        
        /// <summary>连续未浇水天数</summary>
        public int daysWithoutWater;
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 存档数据转换辅助类
    /// </summary>
    public static class SaveDataHelper
    {
        /// <summary>
        /// 将 InventoryItem 转换为存档数据
        /// </summary>
        public static InventorySlotSaveData ToSaveData(InventoryItem item, int slotIndex)
        {
            if (item == null || item.IsEmpty)
            {
                return new InventorySlotSaveData { slotIndex = slotIndex };
            }
            
            item.PrepareForSerialization();
            
            var data = new InventorySlotSaveData
            {
                slotIndex = slotIndex,
                itemId = item.ItemId,
                quality = item.Quality,
                amount = item.Amount,
                instanceId = item.InstanceId,
                currentDurability = item.CurrentDurability,
                maxDurability = item.MaxDurability
            };
            
            // 转换动态属性
            // 注意：这里需要访问 InventoryItem 的内部属性
            // 实际实现时可能需要调整
            
            return data;
        }
        
        /// <summary>
        /// 从存档数据恢复 InventoryItem
        /// </summary>
        public static InventoryItem FromSaveData(InventorySlotSaveData data)
        {
            if (data == null || data.IsEmpty)
            {
                return InventoryItem.Empty;
            }
            
            var item = new InventoryItem(data.itemId, data.quality, data.amount);
            
            if (data.maxDurability > 0)
            {
                item.SetDurability(data.maxDurability, data.currentDurability);
            }
            
            // 恢复动态属性
            if (data.properties != null)
            {
                foreach (var prop in data.properties)
                {
                    item.SetProperty(prop.key, prop.value);
                }
            }
            
            return item;
        }
    }
    
    #endregion
}
