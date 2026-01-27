using System;
using UnityEngine;
using FarmGame.Data;

/// <summary>
/// 箱子库存类 - 管理箱子内的物品存储
/// 参考 InventoryService 设计，提供事件绑定支持 UI 响应
/// 非 MonoBehaviour，由 ChestController 持有
/// </summary>
[Serializable]
public class ChestInventory : IItemContainer
{
    #region 字段

    [SerializeField] private ItemStack[] _slots;
    private int _capacity;
    private ItemDatabase _database;

    #endregion

    #region 事件

    /// <summary>
    /// 单个槽位变化事件（参数：槽位索引）
    /// </summary>
    public event Action<int> OnSlotChanged;

    /// <summary>
    /// 整体库存变化事件
    /// </summary>
    public event Action OnInventoryChanged;

    #endregion

    #region 属性

    /// <summary>
    /// 库存容量
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// 物品数据库引用（IItemContainer 接口）
    /// </summary>
    public ItemDatabase Database => _database;

    /// <summary>
    /// 是否为空（所有槽位都是空的）
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            if (_slots == null || _slots.Length == 0) return true;
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 获取所有槽位（只读副本）
    /// </summary>
    public ItemStack[] GetAllSlots()
    {
        if (_slots == null) return Array.Empty<ItemStack>();
        var copy = new ItemStack[_slots.Length];
        Array.Copy(_slots, copy, _slots.Length);
        return copy;
    }

    #endregion

    #region 构造与初始化

    /// <summary>
    /// 创建指定容量的箱子库存
    /// </summary>
    public ChestInventory(int capacity, ItemDatabase database = null)
    {
        _capacity = Mathf.Max(1, capacity);
        _database = database;
        _slots = new ItemStack[_capacity];
        for (int i = 0; i < _capacity; i++)
        {
            _slots[i] = ItemStack.Empty;
        }
    }

    /// <summary>
    /// 设置物品数据库引用
    /// </summary>
    public void SetDatabase(ItemDatabase database)
    {
        _database = database;
    }

    /// <summary>
    /// 从现有数据初始化（用于读档）
    /// </summary>
    public void LoadFromData(ItemStack[] data)
    {
        if (data == null) return;
        int count = Mathf.Min(data.Length, _capacity);
        for (int i = 0; i < count; i++)
        {
            _slots[i] = data[i];
        }
        RaiseInventoryChanged();
    }

    #endregion

    #region 槽位操作

    /// <summary>
    /// 获取指定槽位的物品
    /// </summary>
    public ItemStack GetSlot(int index)
    {
        if (!InRange(index)) return ItemStack.Empty;
        return _slots[index];
    }

    /// <summary>
    /// 设置指定槽位的物品
    /// </summary>
    public bool SetSlot(int index, ItemStack stack)
    {
        if (!InRange(index)) return false;
        _slots[index] = stack;
        RaiseSlotChanged(index);
        return true;
    }

    /// <summary>
    /// 清空指定槽位
    /// </summary>
    public void ClearSlot(int index)
    {
        if (!InRange(index)) return;
        _slots[index] = ItemStack.Empty;
        RaiseSlotChanged(index);
    }

    /// <summary>
    /// 交换或合并两个槽位
    /// </summary>
    public bool SwapOrMerge(int a, int b)
    {
        if (!InRange(a) || !InRange(b) || a == b) return false;

        var slotA = _slots[a];
        var slotB = _slots[b];

        if (slotA.IsEmpty && slotB.IsEmpty) return false;

        // 尝试合并（同ID同品质且未满）
        if (!slotA.IsEmpty && !slotB.IsEmpty && slotA.CanStackWith(slotB))
        {
            int maxStack = GetMaxStack(slotA.itemId);
            int spaceInB = Mathf.Max(0, maxStack - slotB.amount);
            if (spaceInB > 0)
            {
                int move = Mathf.Min(spaceInB, slotA.amount);
                slotB.amount += move;
                slotA.amount -= move;
                _slots[a] = slotA.amount > 0 ? slotA : ItemStack.Empty;
                _slots[b] = slotB;
                RaiseSlotChanged(a);
                RaiseSlotChanged(b);
                return true;
            }
        }

        // 交换
        _slots[a] = slotB;
        _slots[b] = slotA;
        RaiseSlotChanged(a);
        RaiseSlotChanged(b);
        return true;
    }

    /// <summary>
    /// 从指定槽位移除物品
    /// </summary>
    public bool Remove(int index, int amount)
    {
        if (!InRange(index) || amount <= 0) return false;

        var slot = _slots[index];
        if (slot.IsEmpty) return false;

        slot.amount -= amount;
        if (slot.amount <= 0)
        {
            slot = ItemStack.Empty;
        }
        _slots[index] = slot;
        RaiseSlotChanged(index);
        return true;
    }

    /// <summary>
    /// 添加物品到库存，返回未能放入的剩余数量
    /// </summary>
    public int AddItem(int itemId, int quality, int amount)
    {
        if (amount <= 0) return 0;
        int remaining = amount;
        int maxStack = GetMaxStack(itemId);

        // 1) 先尝试叠加到现有堆叠
        for (int i = 0; i < _capacity && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (!slot.IsEmpty && slot.itemId == itemId && slot.quality == quality && slot.amount < maxStack)
            {
                int canAdd = Mathf.Min(remaining, maxStack - slot.amount);
                slot.amount += canAdd;
                remaining -= canAdd;
                _slots[i] = slot;
                RaiseSlotChanged(i);
            }
        }

        // 2) 再尝试放入空槽位
        for (int i = 0; i < _capacity && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty)
            {
                int put = Mathf.Min(remaining, maxStack);
                _slots[i] = new ItemStack(itemId, quality, put);
                remaining -= put;
                RaiseSlotChanged(i);
            }
        }

        if (remaining != amount)
        {
            RaiseInventoryChanged();
        }
        return remaining;
    }

    /// <summary>
    /// 检查是否可以添加指定物品
    /// </summary>
    public bool CanAddItem(int itemId, int quality, int amount)
    {
        if (amount <= 0) return true;
        int remaining = amount;
        int maxStack = GetMaxStack(itemId);

        for (int i = 0; i < _capacity && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (!slot.IsEmpty && slot.itemId == itemId && slot.quality == quality && slot.amount < maxStack)
            {
                remaining -= (maxStack - slot.amount);
            }
        }
        if (remaining <= 0) return true;

        for (int i = 0; i < _capacity && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty)
            {
                remaining -= maxStack;
            }
        }

        return remaining <= 0;
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
        var chestStack = GetSlot(chestSlot);

        if (invStack.IsEmpty && chestStack.IsEmpty) return false;

        if (!invStack.IsEmpty && !chestStack.IsEmpty && invStack.CanStackWith(chestStack))
        {
            int maxStack = GetMaxStack(invStack.itemId);
            int space = Mathf.Max(0, maxStack - chestStack.amount);
            if (space > 0)
            {
                int move = Mathf.Min(space, invStack.amount);
                chestStack.amount += move;
                invStack.amount -= move;
                inventory.SetSlot(inventorySlot, invStack.amount > 0 ? invStack : ItemStack.Empty);
                SetSlot(chestSlot, chestStack);
                return true;
            }
        }

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
                chestStack.amount -= move;
                inventory.SetSlot(inventorySlot, invStack);
                SetSlot(chestSlot, chestStack.amount > 0 ? chestStack : ItemStack.Empty);
                return true;
            }
        }

        inventory.SetSlot(inventorySlot, chestStack);
        SetSlot(chestSlot, invStack);
        return true;
    }

    #endregion

    #region 排序功能

    /// <summary>
    /// 排序箱子内的物品
    /// 规则：按 itemId 升序，同 ID 按 quality 降序，空槽位排在最后
    /// </summary>
    public void Sort()
    {
        if (_slots == null || _slots.Length == 0) return;

        // 收集所有非空物品
        var items = new System.Collections.Generic.List<ItemStack>();
        for (int i = 0; i < _capacity; i++)
        {
            if (!_slots[i].IsEmpty)
            {
                items.Add(_slots[i]);
            }
        }

        // 排序：itemId 升序，同 ID 按 quality 降序
        items.Sort((a, b) =>
        {
            if (a.itemId != b.itemId)
                return a.itemId.CompareTo(b.itemId);
            return b.quality.CompareTo(a.quality); // quality 降序
        });

        // 合并相同物品
        var merged = new System.Collections.Generic.List<ItemStack>();
        foreach (var item in items)
        {
            bool stacked = false;
            int maxStack = GetMaxStack(item.itemId);

            for (int i = 0; i < merged.Count; i++)
            {
                if (merged[i].CanStackWith(item) && merged[i].amount < maxStack)
                {
                    int canAdd = Mathf.Min(item.amount, maxStack - merged[i].amount);
                    var temp = merged[i];
                    temp.amount += canAdd;
                    merged[i] = temp;

                    if (canAdd < item.amount)
                    {
                        var remaining = item;
                        remaining.amount -= canAdd;
                        merged.Add(remaining);
                    }
                    stacked = true;
                    break;
                }
            }

            if (!stacked)
            {
                merged.Add(item);
            }
        }

        // 写回槽位
        for (int i = 0; i < _capacity; i++)
        {
            _slots[i] = i < merged.Count ? merged[i] : ItemStack.Empty;
        }

        // 🔥 触发全局刷新事件，通知 UI 更新
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
