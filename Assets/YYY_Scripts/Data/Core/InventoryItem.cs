using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Data.Core
{
    /// <summary>
    /// 物品实例数据 - 支持动态属性的物品
    /// 
    /// 设计思路：
    /// - ItemData (ScriptableObject) 定义物品的静态属性（名称、图标、基础属性）
    /// - InventoryItem 存储物品的实例属性（耐久度、附魔、自定义数据）
    /// 
    /// 使用场景：
    /// - 工具耐久度：斧头用了 50 次，耐久度剩余 50%
    /// - 附魔效果：水壶附魔了"自动浇水"
    /// - 作物品质：这颗番茄是金色品质
    /// - 自定义数据：任何需要存储的动态属性
    /// 
    /// 为什么是 class 而不是 struct：
    /// - 需要支持 null（空槽位）
    /// - 需要引用语义（多处引用同一物品实例）
    /// - 需要继承扩展（未来可能有特殊物品类型）
    /// - 包含 Dictionary，struct 不适合
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        #region 核心字段
        
        /// <summary>
        /// 物品实例唯一 ID（用于存档关联）
        /// 格式：GUID 字符串
        /// </summary>
        [SerializeField] private string instanceId;
        
        /// <summary>
        /// 物品定义 ID（对应 ItemData.itemID）
        /// </summary>
        [SerializeField] private int itemId;
        
        /// <summary>
        /// 物品品质（0=普通, 1=稀有, 2=罕见, 3=猎奇）
        /// </summary>
        [SerializeField] private int quality;
        
        /// <summary>
        /// 堆叠数量
        /// </summary>
        [SerializeField] private int amount;
        
        #endregion
        
        #region 动态属性
        
        /// <summary>
        /// 当前耐久度（-1 表示无耐久度限制）
        /// </summary>
        [SerializeField] private int currentDurability = -1;
        
        /// <summary>
        /// 最大耐久度（-1 表示无耐久度限制）
        /// </summary>
        [SerializeField] private int maxDurability = -1;
        
        /// <summary>
        /// 动态属性字典
        /// Key: 属性名（如 "enchantment", "customName", "createdTime"）
        /// Value: 属性值（序列化为 JSON 字符串）
        /// 
        /// 注意：Unity 的 JsonUtility 不支持 Dictionary，
        /// 序列化时需要转换为 List<PropertyEntry>
        /// </summary>
        private Dictionary<string, string> properties;
        
        /// <summary>
        /// 用于序列化的属性列表
        /// </summary>
        [SerializeField] private List<PropertyEntry> serializedProperties;
        
        #endregion
        
        #region 属性访问器
        
        public string InstanceId => instanceId;
        public int ItemId => itemId;
        public int Quality => quality;
        public int Amount => amount;
        public int CurrentDurability => currentDurability;
        public int MaxDurability => maxDurability;
        
        /// <summary>
        /// 是否为空物品
        /// </summary>
        public bool IsEmpty => amount <= 0 || itemId < 0;
        
        /// <summary>
        /// 是否有耐久度系统
        /// </summary>
        public bool HasDurability => maxDurability > 0;
        
        /// <summary>
        /// 耐久度百分比（0-1）
        /// </summary>
        public float DurabilityPercent => HasDurability ? (float)currentDurability / maxDurability : 1f;
        
        #endregion
        
        #region 构造函数
        
        /// <summary>
        /// 创建空物品
        /// </summary>
        public static InventoryItem Empty => new InventoryItem(-1, 0, 0);
        
        /// <summary>
        /// 基础构造函数
        /// </summary>
        public InventoryItem(int itemId, int quality, int amount)
        {
            this.instanceId = Guid.NewGuid().ToString();
            this.itemId = itemId;
            this.quality = quality;
            this.amount = amount;
            this.properties = new Dictionary<string, string>();
            this.serializedProperties = new List<PropertyEntry>();
        }
        
        /// <summary>
        /// 带耐久度的构造函数
        /// </summary>
        public InventoryItem(int itemId, int quality, int amount, int maxDurability)
            : this(itemId, quality, amount)
        {
            this.maxDurability = maxDurability;
            this.currentDurability = maxDurability;
        }
        
        /// <summary>
        /// 从 ItemStack 转换（兼容旧系统）
        /// </summary>
        public static InventoryItem FromItemStack(ItemStack stack)
        {
            if (stack.IsEmpty) return Empty;
            return new InventoryItem(stack.itemId, stack.quality, stack.amount);
        }
        
        /// <summary>
        /// 转换为 ItemStack（兼容旧系统）
        /// </summary>
        public ItemStack ToItemStack()
        {
            if (IsEmpty) return ItemStack.Empty;
            return new ItemStack(itemId, quality, amount);
        }
        
        #endregion
        
        #region 数量操作
        
        /// <summary>
        /// 设置数量
        /// </summary>
        public void SetAmount(int newAmount)
        {
            amount = Mathf.Max(0, newAmount);
        }
        
        /// <summary>
        /// 增加数量
        /// </summary>
        public void AddAmount(int delta)
        {
            amount = Mathf.Max(0, amount + delta);
        }
        
        /// <summary>
        /// 减少数量
        /// </summary>
        public bool RemoveAmount(int delta)
        {
            if (delta > amount) return false;
            amount -= delta;
            return true;
        }
        
        #endregion
        
        #region 耐久度操作
        
        /// <summary>
        /// 设置耐久度系统
        /// </summary>
        public void SetDurability(int max, int current = -1)
        {
            maxDurability = max;
            currentDurability = current < 0 ? max : Mathf.Clamp(current, 0, max);
        }
        
        /// <summary>
        /// 消耗耐久度
        /// </summary>
        /// <returns>是否损坏（耐久度归零）</returns>
        public bool UseDurability(int amount = 1)
        {
            if (!HasDurability) return false;
            currentDurability = Mathf.Max(0, currentDurability - amount);
            return currentDurability <= 0;
        }
        
        /// <summary>
        /// 修复耐久度
        /// </summary>
        public void RepairDurability(int amount)
        {
            if (!HasDurability) return;
            currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
        }
        
        #endregion
        
        #region 动态属性操作
        
        /// <summary>
        /// 设置属性
        /// </summary>
        public void SetProperty(string key, string value)
        {
            EnsurePropertiesInitialized();
            properties[key] = value;
        }
        
        /// <summary>
        /// 设置整数属性
        /// </summary>
        public void SetProperty(string key, int value)
        {
            SetProperty(key, value.ToString());
        }
        
        /// <summary>
        /// 设置浮点属性
        /// </summary>
        public void SetProperty(string key, float value)
        {
            SetProperty(key, value.ToString("F4"));
        }
        
        /// <summary>
        /// 设置布尔属性
        /// </summary>
        public void SetProperty(string key, bool value)
        {
            SetProperty(key, value ? "1" : "0");
        }
        
        /// <summary>
        /// 获取字符串属性
        /// </summary>
        public string GetProperty(string key, string defaultValue = "")
        {
            EnsurePropertiesInitialized();
            return properties.TryGetValue(key, out var value) ? value : defaultValue;
        }
        
        /// <summary>
        /// 获取整数属性
        /// </summary>
        public int GetPropertyInt(string key, int defaultValue = 0)
        {
            var str = GetProperty(key);
            return int.TryParse(str, out var value) ? value : defaultValue;
        }
        
        /// <summary>
        /// 获取浮点属性
        /// </summary>
        public float GetPropertyFloat(string key, float defaultValue = 0f)
        {
            var str = GetProperty(key);
            return float.TryParse(str, out var value) ? value : defaultValue;
        }
        
        /// <summary>
        /// 获取布尔属性
        /// </summary>
        public bool GetPropertyBool(string key, bool defaultValue = false)
        {
            var str = GetProperty(key);
            if (string.IsNullOrEmpty(str)) return defaultValue;
            return str == "1" || str.ToLower() == "true";
        }
        
        /// <summary>
        /// 是否有指定属性
        /// </summary>
        public bool HasProperty(string key)
        {
            EnsurePropertiesInitialized();
            return properties.ContainsKey(key);
        }
        
        /// <summary>
        /// 移除属性
        /// </summary>
        public bool RemoveProperty(string key)
        {
            EnsurePropertiesInitialized();
            return properties.Remove(key);
        }
        
        private void EnsurePropertiesInitialized()
        {
            if (properties == null)
            {
                properties = new Dictionary<string, string>();
                // 从序列化列表恢复
                if (serializedProperties != null)
                {
                    foreach (var entry in serializedProperties)
                    {
                        properties[entry.key] = entry.value;
                    }
                }
            }
        }
        
        #endregion
        
        #region 序列化支持
        
        /// <summary>
        /// 准备序列化（将 Dictionary 转换为 List）
        /// </summary>
        public void PrepareForSerialization()
        {
            serializedProperties = new List<PropertyEntry>();
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    serializedProperties.Add(new PropertyEntry(kvp.Key, kvp.Value));
                }
            }
        }
        
        /// <summary>
        /// 序列化后恢复（将 List 转换为 Dictionary）
        /// </summary>
        public void OnAfterDeserialize()
        {
            properties = new Dictionary<string, string>();
            if (serializedProperties != null)
            {
                foreach (var entry in serializedProperties)
                {
                    properties[entry.key] = entry.value;
                }
            }
        }
        
        #endregion
        
        #region 堆叠判断
        
        /// <summary>
        /// 是否可以与另一个物品堆叠
        /// 注意：有动态属性的物品通常不能堆叠
        /// </summary>
        public bool CanStackWith(InventoryItem other)
        {
            if (IsEmpty || other.IsEmpty) return false;
            if (itemId != other.itemId) return false;
            if (quality != other.quality) return false;
            
            // 有耐久度的物品不能堆叠
            if (HasDurability || other.HasDurability) return false;
            
            // 有动态属性的物品不能堆叠
            EnsurePropertiesInitialized();
            other.EnsurePropertiesInitialized();
            if (properties.Count > 0 || other.properties.Count > 0) return false;
            
            return true;
        }
        
        #endregion
        
        #region 克隆
        
        /// <summary>
        /// 深拷贝
        /// </summary>
        public InventoryItem Clone()
        {
            var clone = new InventoryItem(itemId, quality, amount);
            clone.instanceId = Guid.NewGuid().ToString(); // 新实例 ID
            clone.currentDurability = currentDurability;
            clone.maxDurability = maxDurability;
            
            EnsurePropertiesInitialized();
            clone.properties = new Dictionary<string, string>(properties);
            
            return clone;
        }
        
        /// <summary>
        /// 分割堆叠
        /// </summary>
        public InventoryItem Split(int splitAmount)
        {
            if (splitAmount <= 0 || splitAmount >= amount) return null;
            
            var split = Clone();
            split.SetAmount(splitAmount);
            this.amount -= splitAmount;
            
            return split;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 属性条目（用于序列化）
    /// </summary>
    [Serializable]
    public struct PropertyEntry
    {
        public string key;
        public string value;
        
        public PropertyEntry(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }
}
