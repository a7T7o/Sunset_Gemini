using System;
using UnityEngine;
using FarmGame.Data;

/// <summary>
/// 装备服务（下半部分6格，独立于背包）
/// - 不占用背包空间
/// - 提供从背包双击/拖拽装备入口
/// - 暂定可装备判断：ToolData 或 WeaponData
/// </summary>
public class EquipmentService : MonoBehaviour
{
    public const int EquipSlots = 6;

    [SerializeField] private ItemDatabase database;
    [SerializeField] private ItemStack[] equips = new ItemStack[EquipSlots];

    public event Action<int> OnEquipSlotChanged; // 0..5

    void Awake()
    {
        for (int i = 0; i < equips.Length; i++)
            equips[i] = ItemStack.Empty;
    }

    public void SetDatabase(ItemDatabase db) => database = db;

    public ItemStack GetEquip(int index)
    {
        if (!InRange(index)) return ItemStack.Empty;
        return equips[index];
    }

    public bool SetEquip(int index, ItemStack stack)
    {
        if (!InRange(index)) return false;
        equips[index] = stack;
        OnEquipSlotChanged?.Invoke(index);
        return true;
    }
    
    public void ClearEquip(int index)
    {
        if (!InRange(index)) return;
        equips[index] = ItemStack.Empty;
        OnEquipSlotChanged?.Invoke(index);
    }

    public bool TryEquipFromInventory(InventoryService inv, int invIndex, int preferredEquipIndex = -1)
    {
        if (inv == null) return false;
        var st = inv.GetSlot(invIndex);
        if (st.IsEmpty) return false;

        if (!IsEquipable(st.itemId))
        {
            Debug.Log("[EquipmentService] 非可装备物品，忽略。");
            return false;
        }

        int target = preferredEquipIndex >= 0 && InRange(preferredEquipIndex)
            ? preferredEquipIndex
            : FindFirstEmpty();
        if (target < 0)
        {
            Debug.Log("[EquipmentService] 装备栏已满。");
            return false;
        }

        // 移动整堆（通常装备是不可堆叠的）
        SetEquip(target, st);
        inv.ClearSlot(invIndex);
        return true;
    }

    public bool UnequipToInventory(InventoryService inv, int equipIndex)
    {
        if (inv == null || !InRange(equipIndex)) return false;
        var st = equips[equipIndex];
        if (st.IsEmpty) return false;
        int rem = inv.AddItem(st.itemId, st.quality, st.amount);
        if (rem == 0)
        {
            equips[equipIndex] = ItemStack.Empty;
            OnEquipSlotChanged?.Invoke(equipIndex);
            return true;
        }
        Debug.LogWarning("[EquipmentService] 背包空间不足，无法卸下。");
        return false;
    }

    // 供UI层调用的公开方法
    public bool IsEquipableItemPublic(int itemId)
    {
        return IsEquipable(itemId);
    }

    bool IsEquipable(int itemId)
    {
        if (database == null) return false;
        var data = database.GetItemByID(itemId);
        if (data == null) return false;
        // 先按已存在的数据类型判断；未来可替换为 data.isEquippable 标记
        return data is ToolData || data is WeaponData;
    }

    int FindFirstEmpty()
    {
        for (int i = 0; i < equips.Length; i++)
            if (equips[i].IsEmpty) return i;
        return -1;
    }

    bool InRange(int i) => i >= 0 && i < equips.Length;
}
