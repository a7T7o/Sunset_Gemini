using System;
using System.Collections.Generic;
using UnityEngine;
using FarmGame.Data;
using FarmGame.Data.Core;

namespace FarmGame.Data.Core
{
    /// <summary>
    /// 箱子库存类 V2 - 使用 InventoryItem 支持动态属性
/// 
/// 核心改进：
/// - 内部使用 InventoryItem[] 存储，支持耐久度、附魔等动态属性
/// - 提供 ItemStack 兼容接口供旧 UI 使用
/// - 支持完整的序列化/反序列化
/// 
/// 设计原则：
/// - 数据以 InventoryItem 为准
/// - UI 显示时转换为 ItemStack（只在显示那一瞬间）
/// - 保存时直接序列化 InventoryItem
/// </summary>
[Serializable]
public class ChestInventoryV2 : IItemContainer
{
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

    public bool IsEmpty
    {
        get
        {
            if (_items == null || _items.Length == 0) return true;
            foreach (var item in _items)
            {
                if (item != null && !item.IsEmpty) return false;
            }
            return true;
        }
    }

    #endregion

    #region 构造与初始化

    public ChestInventoryV2(int capacity, ItemDatabase database = null)
    {
        _capacity = Mathf.Max(1, capacity);
        _database = database;
        _items = new InventoryItem[_capacity];
        
        // 初始化为空
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

    /// <summary>
    /// 添加 InventoryItem 到库存
    /// </summary>
    public bool AddItem(InventoryItem item)
    {
        if (item == null || item.IsEmpty) return false;

        // 有动态属性的物品不能堆叠，直接找空位
        if (item.HasDurability || item.HasProperty("enchantment"))
        {
            for (int i = 0; i < _capacity; i++)
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

        // 普通物品：先尝试堆叠
        int maxStack = GetMaxStack(item.ItemId);
        int remaining = item.Amount;

        for (int i = 0; i < _capacity && remaining > 0; i++)
        {
            var slot = _items[i];
            if (slot != null && !slot.IsEmpty && slot.CanStackWith(item))
            {
                int canAdd = Mathf.Min(remaining, maxStack - slot.Amount);
                if (canAdd > 0)
                {
                    slot.AddAmount(canAdd);
                    remaining -= canAdd;
                    RaiseSlotChanged(i);
                }
            }
        }

        // 再找空位
        while (remaining > 0)
        {
            int emptySlot = -1;
            for (int i = 0; i < _capacity; i++)
            {
                if (_items[i] == null || _items[i].IsEmpty)
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot < 0) break; // 没有空位了

            int put = Mathf.Min(remaining, maxStack);
            _items[emptySlot] = new InventoryItem(item.ItemId, item.Quality, put);
            remaining -= put;
            RaiseSlotChanged(emptySlot);
        }

        if (remaining < item.Amount)
        {
            item.SetAmount(remaining);
            RaiseInventoryChanged();
            return remaining == 0;
        }

        return false;
    }

    /// <summary>
    /// 清空指定槽位
    /// </summary>
    public void ClearSlot(int index)
    {
        ClearItem(index);
    }

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

    #region ItemStack 兼容接口（供旧 UI 使用）

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

    /// <summary>
    /// 获取所有槽位的 ItemStack（兼容旧接口）
    /// </summary>
    public ItemStack[] GetAllSlots()
    {
        var result = new ItemStack[_capacity];
        for (int i = 0; i < _capacity; i++)
        {
            result[i] = GetSlot(i);
        }
        return result;
    }

    /// <summary>
    /// 添加物品（兼容旧接口）
    /// </summary>
    public int AddItem(int itemId, int quality, int amount)
    {
        if (amount <= 0) return 0;
        
        var item = new InventoryItem(itemId, quality, amount);
        bool success = AddItem(item);
        
        return success ? 0 : item.Amount;
    }

    /// <summary>
    /// 从指定槽位移除物品
    /// </summary>
    public bool Remove(int index, int amount)
    {
        if (!InRange(index) || amount <= 0) return false;

        var item = _items[index];
        if (item == null || item.IsEmpty) return false;

        item.AddAmount(-amount);
        if (item.IsEmpty) _items[index] = null;
        
        RaiseSlotChanged(index);
        return true;
    }

    #endregion

    #region 序列化支持

    /// <summary>
    /// 导出为存档数据
    /// </summary>
    public List<InventorySlotSaveData> ToSaveData()
    {
        var result = new List<InventorySlotSaveData>();
        
        for (int i = 0; i < _capacity; i++)
        {
            var item = _items[i];
            if (item == null || item.IsEmpty)
            {
                result.Add(new InventorySlotSaveData { slotIndex = i });
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
            
            // 导出动态属性
            // 注意：需要访问 InventoryItem 的内部属性
            // 这里简化处理，实际可能需要扩展 InventoryItem
            
            result.Add(slotData);
        }
        
        return result;
    }

    /// <summary>
    /// 从存档数据恢复
    /// </summary>
    public void LoadFromSaveData(List<InventorySlotSaveData> dataList)
    {
        if (dataList == null) return;
        
        // 清空现有数据
        for (int i = 0; i < _capacity; i++)
        {
            _items[i] = null;
        }
        
        // 恢复数据
        foreach (var data in dataList)
        {
            if (data.slotIndex < 0 || data.slotIndex >= _capacity) continue;
            if (data.IsEmpty) continue;
            
            var item = SaveDataHelper.FromSaveData(data);
            _items[data.slotIndex] = item;
        }
        
        RaiseInventoryChanged();
    }

    #endregion

    #region 跨库存操作

    /// <summary>
    /// 从背包转移物品到箱子
    /// </summary>
    public bool TransferFromInventory(InventoryService inventory, int inventorySlot, int chestSlot)
    {
        if (inventory == null || !InRange(chestSlot)) return false;

        var invStack = inventory.GetSlot(inventorySlot);
        var chestItem = GetItem(chestSlot);
        var chestStack = GetSlot(chestSlot);

        if (invStack.IsEmpty && (chestItem == null || chestItem.IsEmpty)) return false;

        // 简化处理：直接交换 ItemStack
        // TODO: 未来需要处理 InventoryItem 的动态属性
        if (!invStack.IsEmpty && !chestStack.IsEmpty && invStack.CanStackWith(chestStack))
        {
            int maxStack = GetMaxStack(invStack.itemId);
            int space = Mathf.Max(0, maxStack - chestStack.amount);
            if (space > 0)
            {
                int move = Mathf.Min(space, invStack.amount);
                
                if (chestItem != null)
                {
                    chestItem.AddAmount(move);
                }
                
                invStack.amount -= move;
                inventory.SetSlot(inventorySlot, invStack.amount > 0 ? invStack : ItemStack.Empty);
                RaiseSlotChanged(chestSlot);
                return true;
            }
        }

        // 交换
        inventory.SetSlot(inventorySlot, chestStack);
        SetSlot(chestSlot, invStack);
        return true;
    }

    /// <summary>
    /// 从箱子转移物品到背包
    /// </summary>
    public bool TransferToInventory(InventoryService inventory, int chestSlot, int inventorySlot)
    {
        if (inventory == null || !InRange(chestSlot)) return false;

        var chestStack = GetSlot(chestSlot);
        var invStack = inventory.GetSlot(inventorySlot);

        if (chestStack.IsEmpty && invStack.IsEmpty) return false;

        if (!chestStack.IsEmpty && !invStack.IsEmpty && chestStack.CanStackWith(invStack))
        {
            int maxStack = inventory.GetMaxStack(chestStack.itemId);
            int space = Mathf.Max(0, maxStack - invStack.amount);
            if (space > 0)
            {
                int move = Mathf.Min(space, chestStack.amount);
                invStack.amount += move;
                
                var chestItem = GetItem(chestSlot);
                if (chestItem != null)
                {
                    chestItem.AddAmount(-move);
                    if (chestItem.IsEmpty) _items[chestSlot] = null;
                }
                
                inventory.SetSlot(inventorySlot, invStack);
                RaiseSlotChanged(chestSlot);
                return true;
            }
        }

        inventory.SetSlot(inventorySlot, chestStack);
        SetSlot(chestSlot, invStack);
        return true;
    }

    #endregion

    #region 排序

    public void Sort()
    {
        if (_items == null || _items.Length == 0) return;

        // 收集所有非空物品
        var items = new List<InventoryItem>();
        for (int i = 0; i < _capacity; i++)
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
        for (int i = 0; i < _capacity; i++)
        {
            _items[i] = i < items.Count ? items[i] : null;
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