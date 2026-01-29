using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Data.Core
{
    /// <summary>
    /// 玩家背包数据核心 - 纯数据类
    /// 
    /// 设计原则：
    /// - 内部使用 InventoryItem[] 存储，支持耐久度、附魔等动态属性
    /// - 不依赖 MonoBehaviour，纯粹的数据逻辑
    /// - 提供 ItemStack 兼容接口供旧 UI 使用
    /// - 支持完整的序列化/反序列化
    /// 
    /// 与 ChestInventoryV2 的区别：
    /// - 支持 Hotbar 优先逻辑（前 12 格优先）
    /// - 支持 Sort 排序（不排序 Hotbar）
    /// </summary>
    [Serializable]
    public class PlayerInventoryData
    {
        #region 常量
        
        public const int DefaultSize = 36;
        public const int HotbarWidth = 12;
        
        #endregion
        
        #region 字段
        
        /// <summary>
        /// 核心数据：InventoryItem 数组
        /// </summary>
        [SerializeField] private InventoryItem[] _items;
        
        private int _capacity;
        private ItemDatabase _database;
        
        #endregion
        
        #region 事件
        
        public event Action<int> OnSlotChanged;
        public event Action OnInventoryChanged;
        
        #endregion
        
        #region 属性
        
        public int Capacity => _capacity;
        public ItemDatabase Database => _database;
        
        #endregion
        
        #region 构造与初始化
        
        public PlayerInventoryData(int capacity = DefaultSize, ItemDatabase database = null)
        {
            _capacity = Mathf.Max(1, capacity);
            _database = database;
            _items = new InventoryItem[_capacity];
            
            // 初始化为 null（空槽位）
            for (int i = 0; i < _capacity; i++)
            {
                _items[i] = null;
            }
        }
        
        public void SetDatabase(ItemDatabase database)
        {
            _database = database;
        }
        
        #endregion
        
        #region InventoryItem 操作（核心 API）
        
        /// <summary>
        /// 获取指定槽位的 InventoryItem
        /// </summary>
        public InventoryItem GetItem(int index)
        {
            if (!InRange(index)) return null;
            return _items[index];
        }
        
        /// <summary>
        /// 设置指定槽位的 InventoryItem
        /// </summary>
        public bool SetItem(int index, InventoryItem item)
        {
            if (!InRange(index)) return false;
            _items[index] = item;
            RaiseSlotChanged(index);
            return true;
        }
        
        /// <summary>
        /// 清空指定槽位
        /// </summary>
        public void ClearItem(int index)
        {
            if (!InRange(index)) return;
            _items[index] = null;
            RaiseSlotChanged(index);
        }
        
        #endregion
        
        #region ItemStack 兼容接口
        
        /// <summary>
        /// 获取指定槽位的 ItemStack（兼容旧 UI）
        /// </summary>
        public ItemStack GetSlot(int index)
        {
            var item = GetItem(index);
            if (item == null || item.IsEmpty) return ItemStack.Empty;
            return item.ToItemStack();
        }
        
        /// <summary>
        /// 设置指定槽位（从 ItemStack 转换）
        /// 注意：这会丢失动态属性！仅用于简单物品
        /// </summary>
        public bool SetSlot(int index, ItemStack stack)
        {
            if (!InRange(index)) return false;
            
            if (stack.IsEmpty)
            {
                _items[index] = null;
            }
            else
            {
                _items[index] = InventoryItem.FromItemStack(stack);
            }
            
            RaiseSlotChanged(index);
            return true;
        }
        
        #endregion
        
        #region 添加物品（Hotbar 优先）
        
        /// <summary>
        /// 添加物品（优先叠加/放置在第一行 Hotbar）
        /// 返回未能放入的剩余数量
        /// </summary>
        public int AddItem(int itemId, int quality, int amount)
        {
            if (amount <= 0) return 0;
            int remaining = amount;
            
            // 1) 第一行叠加
            remaining = FillExistingStacksRange(itemId, quality, remaining, 0, HotbarWidth);
            // 2) 第一行空位
            remaining = FillEmptySlotsRange(itemId, quality, remaining, 0, HotbarWidth);
            // 3) 其他叠加
            remaining = FillExistingStacksRange(itemId, quality, remaining, HotbarWidth, _capacity);
            // 4) 其他空位
            remaining = FillEmptySlotsRange(itemId, quality, remaining, HotbarWidth, _capacity);
            
            if (remaining != amount)
            {
                RaiseInventoryChanged();
            }
            return remaining;
        }
        
        /// <summary>
        /// 添加 InventoryItem（支持动态属性）
        /// </summary>
        public bool AddInventoryItem(InventoryItem item)
        {
            if (item == null || item.IsEmpty) return false;
            
            // 有动态属性的物品不能堆叠，直接找空位
            if (item.HasDurability || HasAnyProperty(item))
            {
                // 优先 Hotbar
                for (int i = 0; i < HotbarWidth; i++)
                {
                    if (_items[i] == null || _items[i].IsEmpty)
                    {
                        _items[i] = item;
                        RaiseSlotChanged(i);
                        RaiseInventoryChanged();
                        return true;
                    }
                }
                // 其他槽位
                for (int i = HotbarWidth; i < _capacity; i++)
                {
                    if (_items[i] == null || _items[i].IsEmpty)
                    {
                        _items[i] = item;
                        RaiseSlotChanged(i);
                        RaiseInventoryChanged();
                        return true;
                    }
                }
                return false; // 没有空位
            }
            
            // 普通物品：使用标准添加逻辑
            int remaining = AddItem(item.ItemId, item.Quality, item.Amount);
            return remaining == 0;
        }
        
        private bool HasAnyProperty(InventoryItem item)
        {
            // 简单检查：如果有任何动态属性
            return item.HasProperty("enchantment") || 
                   item.HasProperty("customName") ||
                   item.HasProperty("createdTime");
        }
        
        private int FillExistingStacksRange(int itemId, int quality, int remaining, int start, int end)
        {
            if (remaining <= 0) return 0;
            int maxStack = GetMaxStack(itemId);
            
            for (int i = start; i < end && remaining > 0; i++)
            {
                var item = _items[i];
                if (item != null && !item.IsEmpty && 
                    item.ItemId == itemId && item.Quality == quality &&
                    !item.HasDurability && item.Amount < maxStack)
                {
                    int canAdd = Mathf.Min(remaining, maxStack - item.Amount);
                    item.AddAmount(canAdd);
                    remaining -= canAdd;
                    RaiseSlotChanged(i);
                }
            }
            return remaining;
        }
        
        private int FillEmptySlotsRange(int itemId, int quality, int remaining, int start, int end)
        {
            if (remaining <= 0) return 0;
            int maxStack = GetMaxStack(itemId);
            
            for (int i = start; i < end && remaining > 0; i++)
            {
                if (_items[i] == null || _items[i].IsEmpty)
                {
                    int put = Mathf.Min(remaining, maxStack);
                    _items[i] = new InventoryItem(itemId, quality, put);
                    remaining -= put;
                    RaiseSlotChanged(i);
                }
            }
            return remaining;
        }
        
        #endregion
        
        #region 移除物品
        
        /// <summary>
        /// 从指定槽位移除物品
        /// </summary>
        public bool RemoveFromSlot(int index, int amount)
        {
            if (!InRange(index) || amount <= 0) return false;
            
            var item = _items[index];
            if (item == null || item.IsEmpty) return false;
            
            item.AddAmount(-amount);
            if (item.IsEmpty) _items[index] = null;
            
            RaiseSlotChanged(index);
            return true;
        }
        
        /// <summary>
        /// 从背包中移除指定物品
        /// </summary>
        public bool RemoveItem(int itemId, int quality, int amount)
        {
            if (amount <= 0) return true;
            int remaining = amount;
            
            // 1) 先从第一行移除
            remaining = RemoveFromRange(itemId, quality, remaining, 0, HotbarWidth);
            // 2) 再从其他行移除
            remaining = RemoveFromRange(itemId, quality, remaining, HotbarWidth, _capacity);
            
            if (remaining != amount)
            {
                RaiseInventoryChanged();
            }
            
            return remaining <= 0;
        }
        
        private int RemoveFromRange(int itemId, int quality, int remaining, int start, int end)
        {
            if (remaining <= 0) return 0;
            
            for (int i = start; i < end && remaining > 0; i++)
            {
                var item = _items[i];
                if (item == null || item.IsEmpty) continue;
                if (item.ItemId != itemId) continue;
                if (quality >= 0 && item.Quality != quality) continue;
                
                int canRemove = Mathf.Min(remaining, item.Amount);
                item.AddAmount(-canRemove);
                remaining -= canRemove;
                
                if (item.IsEmpty) _items[i] = null;
                RaiseSlotChanged(i);
            }
            
            return remaining;
        }
        
        #endregion
        
        #region 查询
        
        /// <summary>
        /// 检查是否可以添加指定物品
        /// </summary>
        public bool CanAddItem(int itemId, int quality, int amount)
        {
            if (amount <= 0) return true;
            int remaining = amount;
            int maxStack = GetMaxStack(itemId);
            
            // 检查现有堆叠空间
            for (int i = 0; i < _capacity && remaining > 0; i++)
            {
                var item = _items[i];
                if (item != null && !item.IsEmpty && 
                    item.ItemId == itemId && item.Quality == quality &&
                    !item.HasDurability && item.Amount < maxStack)
                {
                    remaining -= (maxStack - item.Amount);
                }
            }
            
            // 检查空位
            for (int i = 0; i < _capacity && remaining > 0; i++)
            {
                if (_items[i] == null || _items[i].IsEmpty)
                {
                    remaining -= maxStack;
                }
            }
            
            return remaining <= 0;
        }
        
        /// <summary>
        /// 检查是否有足够数量的指定物品
        /// </summary>
        public bool HasItem(int itemId, int quality, int amount)
        {
            if (amount <= 0) return true;
            int count = 0;
            
            for (int i = 0; i < _capacity; i++)
            {
                var item = _items[i];
                if (item == null || item.IsEmpty) continue;
                if (item.ItemId != itemId) continue;
                if (quality >= 0 && item.Quality != quality) continue;
                
                count += item.Amount;
                if (count >= amount) return true;
            }
            
            return false;
        }
        
        #endregion
        
        #region 交换与合并
        
        /// <summary>
        /// 交换或合并两个槽位
        /// </summary>
        public bool SwapOrMerge(int a, int b)
        {
            if (!InRange(a) || !InRange(b) || a == b) return false;
            
            var itemA = _items[a];
            var itemB = _items[b];
            
            bool aEmpty = itemA == null || itemA.IsEmpty;
            bool bEmpty = itemB == null || itemB.IsEmpty;
            
            if (aEmpty && bEmpty) return false;
            
            // 尝试合并
            if (!aEmpty && !bEmpty && itemA.CanStackWith(itemB))
            {
                int maxStack = GetMaxStack(itemA.ItemId);
                int spaceInB = Mathf.Max(0, maxStack - itemB.Amount);
                if (spaceInB > 0)
                {
                    int move = Mathf.Min(spaceInB, itemA.Amount);
                    itemB.AddAmount(move);
                    itemA.AddAmount(-move);
                    
                    if (itemA.IsEmpty) _items[a] = null;
                    
                    RaiseSlotChanged(a);
                    RaiseSlotChanged(b);
                    return true;
                }
            }
            
            // 交换
            _items[a] = itemB;
            _items[b] = itemA;
            RaiseSlotChanged(a);
            RaiseSlotChanged(b);
            return true;
        }
        
        #endregion
        
        #region 排序
        
        /// <summary>
        /// 排序背包（不包括 Hotbar 第一行）
        /// </summary>
        public void Sort()
        {
            if (_items == null || _items.Length <= HotbarWidth) return;
            
            int sortStart = HotbarWidth;
            int sortEnd = _capacity;
            
            // 收集所有非空物品
            var items = new List<InventoryItem>();
            for (int i = sortStart; i < sortEnd; i++)
            {
                if (_items[i] != null && !_items[i].IsEmpty)
                {
                    items.Add(_items[i]);
                }
            }
            
            // 排序：itemId 升序，同 ID 按 quality 降序
            items.Sort((a, b) =>
            {
                if (a.ItemId != b.ItemId)
                    return a.ItemId.CompareTo(b.ItemId);
                return b.Quality.CompareTo(a.Quality);
            });
            
            // 写回槽位（不合并有动态属性的物品）
            for (int i = sortStart; i < sortEnd; i++)
            {
                int listIndex = i - sortStart;
                _items[i] = listIndex < items.Count ? items[listIndex] : null;
            }
            
            RaiseInventoryChanged();
        }
        
        #endregion
        
        #region 序列化支持
        
        /// <summary>
        /// 导出为存档数据
        /// </summary>
        public InventorySaveData ToSaveData()
        {
            var data = new InventorySaveData
            {
                capacity = _capacity,
                slots = new List<InventorySlotSaveData>()
            };
            
            for (int i = 0; i < _capacity; i++)
            {
                var item = _items[i];
                if (item == null || item.IsEmpty)
                {
                    data.slots.Add(new InventorySlotSaveData { slotIndex = i });
                    continue;
                }
                
                item.PrepareForSerialization();
                
                var slotData = new InventorySlotSaveData
                {
                    slotIndex = i,
                    itemId = item.ItemId,
                    quality = item.Quality,
                    amount = item.Amount,
                    instanceId = item.InstanceId,
                    currentDurability = item.CurrentDurability,
                    maxDurability = item.MaxDurability
                };
                
                data.slots.Add(slotData);
            }
            
            return data;
        }
        
        /// <summary>
        /// 从存档数据恢复
        /// </summary>
        public void LoadFromSaveData(InventorySaveData data)
        {
            if (data == null || data.slots == null) return;
            
            // 清空现有数据
            for (int i = 0; i < _capacity; i++)
            {
                _items[i] = null;
            }
            
            // 恢复数据
            foreach (var slotData in data.slots)
            {
                if (slotData.slotIndex < 0 || slotData.slotIndex >= _capacity) continue;
                if (slotData.IsEmpty) continue;
                
                var item = SaveDataHelper.FromSaveData(slotData);
                _items[slotData.slotIndex] = item;
            }
            
            RaiseInventoryChanged();
        }
        
        #endregion
        
        #region 辅助方法
        
        private bool InRange(int index) => index >= 0 && index < _capacity;
        
        public int GetMaxStack(int itemId)
        {
            if (_database == null) return 99;
            var data = _database.GetItemByID(itemId);
            if (data == null) return 99;
            return Mathf.Max(1, data.maxStackSize);
        }
        
        private void RaiseSlotChanged(int index)
        {
            OnSlotChanged?.Invoke(index);
        }
        
        private void RaiseInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
        
        #endregion
    }
}
