using System;
using UnityEngine;
using FarmGame.Data;

/// <summary>
/// 运行时背包服务
/// - 36格库存（3行x12列），索引0..35
/// - 0..11 为第一行，映射到 ToolBar（完全共用，不复制）
/// - 事件：库存整体变化、单格变化、热键行变化
/// - AddItem 优先：先叠加第一行，再空位第一行，再其余叠加，再其余空位
/// </summary>
public class InventoryService : MonoBehaviour, IItemContainer
{
    public const int DefaultInventorySize = 36; // 3行 * 12列
    public const int HotbarWidth = 12;          // 第一行 12 格

    [Header("数据库")]
    [SerializeField] private ItemDatabase database;

    [Header("容量")]
    [SerializeField] private int inventorySize = DefaultInventorySize;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    [SerializeField] private ItemStack[] slots;

    // 事件
    public event Action OnInventoryChanged;
    public event Action<int> OnSlotChanged;
    public event Action<int> OnHotbarSlotChanged; // index: 0..11

    public int Size => inventorySize;
    public ItemDatabase Database => database; // 公开访问器

    // IItemContainer 接口实现
    public int Capacity => inventorySize;

    void Awake()
    {
        if (inventorySize <= 0) inventorySize = DefaultInventorySize;
        if (slots == null || slots.Length != inventorySize)
        {
            slots = new ItemStack[inventorySize];
            for (int i = 0; i < inventorySize; i++) slots[i] = ItemStack.Empty;
        }
    }

    public void SetDatabase(ItemDatabase db) => database = db;

    // 读取槽位
    public ItemStack GetSlot(int index)
    {
        if (!InRange(index)) return ItemStack.Empty;
        return slots[index];
    }

    public bool TryGetSlot(int index, out ItemStack stack)
    {
        if (InRange(index))
        {
            stack = slots[index];
            return true;
        }
        stack = ItemStack.Empty;
        return false;
    }

    public bool SetSlot(int index, ItemStack stack)
    {
        if (!InRange(index)) return false;
        slots[index] = stack;
        RaiseSlotChanged(index);
        return true;
    }

    public void ClearSlot(int index)
    {
        if (!InRange(index)) return;
        slots[index] = ItemStack.Empty;
        RaiseSlotChanged(index);
    }

    public bool SwapOrMerge(int a, int b)
    {
        if (!InRange(a) || !InRange(b) || a == b) return false;
        var A = slots[a];
        var B = slots[b];
        if (A.IsEmpty && B.IsEmpty) return false;

        // 尝试合并（同ID同品质且未满）
        if (!A.IsEmpty && !B.IsEmpty && A.CanStackWith(B))
        {
            int maxStack = GetMaxStack(A.itemId);
            int spaceInB = Mathf.Max(0, maxStack - B.amount);
            if (spaceInB > 0)
            {
                int move = Mathf.Min(spaceInB, A.amount);
                B.amount += move;
                A.amount -= move;
                slots[a] = A.amount > 0 ? A : ItemStack.Empty;
                slots[b] = B;
                RaiseSlotChanged(a);
                RaiseSlotChanged(b);
                return true;
            }
        }

        // 交换
        slots[a] = B;
        slots[b] = A;
        RaiseSlotChanged(a);
        RaiseSlotChanged(b);
        return true;
    }

    /// <summary>
    /// 添加物品（优先叠加/放置在第一行）
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
        remaining = FillExistingStacksRange(itemId, quality, remaining, HotbarWidth, inventorySize);
        // 4) 其他空位
        remaining = FillEmptySlotsRange(itemId, quality, remaining, HotbarWidth, inventorySize);

        if (remaining != amount)
        {
            RaiseInventoryChanged();
        }
        return remaining;
    }

    /// <summary>
    /// 检查是否可以添加指定物品（不实际添加）
    /// 用于在触发拾取动画前检查背包是否有空间
    /// </summary>
    /// <returns>true 表示背包有空间可以容纳该物品</returns>
    public bool CanAddItem(int itemId, int quality, int amount)
    {
        if (amount <= 0) return true;
        int remaining = amount;
        int maxStack = GetMaxStack(itemId);

        // 1) 检查第一行现有堆叠空间
        remaining = CountAvailableStackSpace(itemId, quality, remaining, 0, HotbarWidth, maxStack);
        if (remaining <= 0) return true;

        // 2) 检查第一行空位
        remaining = CountEmptySlotSpace(remaining, 0, HotbarWidth, maxStack);
        if (remaining <= 0) return true;

        // 3) 检查其他行现有堆叠空间
        remaining = CountAvailableStackSpace(itemId, quality, remaining, HotbarWidth, inventorySize, maxStack);
        if (remaining <= 0) return true;

        // 4) 检查其他行空位
        remaining = CountEmptySlotSpace(remaining, HotbarWidth, inventorySize, maxStack);

        return remaining <= 0;
    }

    /// <summary>
    /// 计算指定范围内现有堆叠可容纳的空间
    /// </summary>
    int CountAvailableStackSpace(int itemId, int quality, int remaining, int start, int end, int maxStack)
    {
        for (int i = start; i < end && remaining > 0; i++)
        {
            var s = slots[i];
            if (!s.IsEmpty && s.itemId == itemId && s.quality == quality && s.amount < maxStack)
            {
                remaining -= (maxStack - s.amount);
            }
        }
        return Mathf.Max(0, remaining);
    }

    /// <summary>
    /// 计算指定范围内空位可容纳的空间
    /// </summary>
    int CountEmptySlotSpace(int remaining, int start, int end, int maxStack)
    {
        for (int i = start; i < end && remaining > 0; i++)
        {
            if (slots[i].IsEmpty)
            {
                remaining -= maxStack;
            }
        }
        return Mathf.Max(0, remaining);
    }

    int FillExistingStacksRange(int itemId, int quality, int remaining, int start, int end)
    {
        if (remaining <= 0) return 0;
        int maxStack = GetMaxStack(itemId);
        for (int i = start; i < end && remaining > 0; i++)
        {
            var s = slots[i];
            if (!s.IsEmpty && s.itemId == itemId && s.quality == quality && s.amount < maxStack)
            {
                int canAdd = Mathf.Min(remaining, maxStack - s.amount);
                s.amount += canAdd;
                remaining -= canAdd;
                slots[i] = s;
                RaiseSlotChanged(i);
            }
        }
        return remaining;
    }

    int FillEmptySlotsRange(int itemId, int quality, int remaining, int start, int end)
    {
        if (remaining <= 0) return 0;
        int maxStack = GetMaxStack(itemId);
        for (int i = start; i < end && remaining > 0; i++)
        {
            var s = slots[i];
            if (s.IsEmpty)
            {
                int put = Mathf.Min(remaining, maxStack);
                slots[i] = new ItemStack(itemId, quality, put);
                remaining -= put;
                RaiseSlotChanged(i);
            }
        }
        return remaining;
    }

    public bool RemoveFromSlot(int index, int amount)
    {
        if (!InRange(index) || amount <= 0) return false;
        var s = slots[index];
        if (s.IsEmpty) return false;
        s.amount -= amount;
        if (s.amount <= 0) s = ItemStack.Empty;
        slots[index] = s;
        RaiseSlotChanged(index);
        return true;
    }

    /// <summary>
    /// 从背包中移除指定物品
    /// 优先从第一行（Hotbar）移除，然后从其他行移除
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <param name="quality">物品品质（-1 表示任意品质）</param>
    /// <param name="amount">移除数量</param>
    /// <returns>是否成功移除全部数量</returns>
    public bool RemoveItem(int itemId, int quality, int amount)
    {
        if (amount <= 0) return true;
        int remaining = amount;

        // 1) 先从第一行移除
        remaining = RemoveFromRange(itemId, quality, remaining, 0, HotbarWidth);
        // 2) 再从其他行移除
        remaining = RemoveFromRange(itemId, quality, remaining, HotbarWidth, inventorySize);

        if (remaining != amount)
        {
            RaiseInventoryChanged();
        }

        return remaining <= 0;
    }

    /// <summary>
    /// 从指定范围的槽位中移除物品
    /// </summary>
    int RemoveFromRange(int itemId, int quality, int remaining, int start, int end)
    {
        if (remaining <= 0) return 0;
        
        for (int i = start; i < end && remaining > 0; i++)
        {
            var s = slots[i];
            if (s.IsEmpty) continue;
            if (s.itemId != itemId) continue;
            if (quality >= 0 && s.quality != quality) continue; // quality < 0 表示任意品质
            
            int canRemove = Mathf.Min(remaining, s.amount);
            s.amount -= canRemove;
            remaining -= canRemove;
            
            slots[i] = s.amount > 0 ? s : ItemStack.Empty;
            RaiseSlotChanged(i);
        }
        
        return remaining;
    }

    /// <summary>
    /// 检查背包中是否有足够数量的指定物品
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <param name="quality">物品品质（-1 表示任意品质）</param>
    /// <param name="amount">需要的数量</param>
    /// <returns>是否有足够数量</returns>
    public bool HasItem(int itemId, int quality, int amount)
    {
        if (amount <= 0) return true;
        int count = 0;
        
        for (int i = 0; i < inventorySize; i++)
        {
            var s = slots[i];
            if (s.IsEmpty) continue;
            if (s.itemId != itemId) continue;
            if (quality >= 0 && s.quality != quality) continue;
            
            count += s.amount;
            if (count >= amount) return true;
        }
        
        return false;
    }

    public int GetMaxStack(int itemId)
    {
        if (database == null) return 99;
        var data = database.GetItemByID(itemId);
        if (data == null) return 99;
        return Mathf.Max(1, data.maxStackSize);
    }

    bool InRange(int i) => i >= 0 && i < inventorySize;

    void RaiseSlotChanged(int index)
    {
        OnSlotChanged?.Invoke(index);
        if (index >= 0 && index < HotbarWidth)
            OnHotbarSlotChanged?.Invoke(index);
    }

    void RaiseInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 排序背包（不包括 Hotbar 第一行）
    /// 规则：按 itemId 升序，同 ID 按 quality 降序，空槽位排在最后
    /// </summary>
    public void Sort()
    {
        if (slots == null || slots.Length <= HotbarWidth) return;

        // 只排序第二行和第三行（索引 12-35）
        int sortStart = HotbarWidth;
        int sortEnd = inventorySize;
        int sortCount = sortEnd - sortStart;

        // 收集所有非空物品
        var items = new System.Collections.Generic.List<ItemStack>();
        for (int i = sortStart; i < sortEnd; i++)
        {
            if (!slots[i].IsEmpty)
            {
                items.Add(slots[i]);
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

        // 写回槽位（从第二行开始）
        for (int i = 0; i < sortCount; i++)
        {
            int slotIndex = sortStart + i;
            slots[slotIndex] = i < merged.Count ? merged[i] : ItemStack.Empty;
        }

        // 🔥 触发全局刷新事件，通知 UI 更新
        RaiseInventoryChanged();
        
        if (showDebugInfo)
            Debug.Log($"[InventoryService] Sort 完成，触发 OnInventoryChanged 事件");
    }
}
