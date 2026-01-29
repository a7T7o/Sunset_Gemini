using System;
using System.Collections.Generic;
using UnityEngine;
using FarmGame.Data;
using FarmGame.Data.Core;

/// <summary>
/// 装备服务（下半部分6格，独立于背包）
/// - 不占用背包空间
/// - 提供从背包双击/拖拽装备入口
/// - 实现 IPersistentObject 支持存档
/// - 支持槽位类型限制（戒指不能戴头上）
/// 
/// 槽位映射：
/// - 0: Helmet (头盔)
/// - 1: Pants (裤子)
/// - 2: Armor (盔甲)
/// - 3: Shoes (鞋子)
/// - 4: Ring (戒指1)
/// - 5: Ring (戒指2)
/// </summary>
public class EquipmentService : MonoBehaviour, IPersistentObject
{
    public const int EquipSlots = 6;

    [SerializeField] private ItemDatabase database;
    [SerializeField] private InventoryItem[] equips = new InventoryItem[EquipSlots];
    [SerializeField] private string persistentId;

    public event Action<int> OnEquipSlotChanged; // 0..5

    #region IPersistentObject 实现

    public string PersistentId
    {
        get
        {
            if (string.IsNullOrEmpty(persistentId))
            {
                persistentId = "EquipmentService"; // 单例，使用固定 ID
            }
            return persistentId;
        }
    }

    public string ObjectType => "EquipmentService";
    
    public bool ShouldSave => true;

    public WorldObjectSaveData Save()
    {
        var data = new WorldObjectSaveData
        {
            guid = PersistentId,
            objectType = ObjectType,
            sceneName = gameObject.scene.name,
            isActive = gameObject.activeSelf
        };
        
        // 保存装备数据到 genericData
        var equipData = new EquipmentSaveData();
        equipData.slots = new InventorySlotSaveData[EquipSlots];
        
        for (int i = 0; i < equips.Length; i++)
        {
            var item = equips[i];
            if (item == null || item.IsEmpty)
            {
                equipData.slots[i] = new InventorySlotSaveData { slotIndex = i };
                continue;
            }
            
            // 死命令：Save 时必须调用 PrepareForSerialization
            item.PrepareForSerialization();
            
            equipData.slots[i] = new InventorySlotSaveData
            {
                slotIndex = i,
                itemId = item.ItemId,
                quality = item.Quality,
                amount = item.Amount,
                instanceId = item.InstanceId,
                currentDurability = item.CurrentDurability,
                maxDurability = item.MaxDurability
            };
        }
        
        data.genericData = JsonUtility.ToJson(equipData);
        return data;
    }

    public void Load(WorldObjectSaveData data)
    {
        if (data == null || string.IsNullOrEmpty(data.genericData)) return;
        
        try
        {
            var equipData = JsonUtility.FromJson<EquipmentSaveData>(data.genericData);
            if (equipData?.slots != null)
            {
                for (int i = 0; i < EquipSlots && i < equipData.slots.Length; i++)
                {
                    var slotData = equipData.slots[i];
                    if (slotData == null || slotData.IsEmpty)
                    {
                        equips[i] = null;
                        continue;
                    }
                    
                    equips[i] = SaveDataHelper.FromSaveData(slotData);
                }
                
                // 通知所有槽位更新
                for (int i = 0; i < EquipSlots; i++)
                {
                    OnEquipSlotChanged?.Invoke(i);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[EquipmentService] 加载存档失败: {e.Message}");
        }
    }

    #endregion

    void Awake()
    {
        for (int i = 0; i < equips.Length; i++)
            equips[i] = null;
    }

    public void SetDatabase(ItemDatabase db) => database = db;
    public ItemDatabase Database => database;

    #region 槽位访问（InventoryItem 版本）

    /// <summary>
    /// 获取指定槽位的装备（InventoryItem）
    /// </summary>
    public InventoryItem GetEquipItem(int index)
    {
        if (!InRange(index)) return null;
        return equips[index];
    }

    /// <summary>
    /// 设置指定槽位的装备（InventoryItem）
    /// </summary>
    public bool SetEquipItem(int index, InventoryItem item)
    {
        if (!InRange(index)) return false;
        
        // 槽位限制检查
        if (item != null && !item.IsEmpty)
        {
            var itemData = database?.GetItemByID(item.ItemId);
            if (!CanEquipAt(index, itemData))
            {
                Debug.LogWarning($"[EquipmentService] 无法装备: {itemData?.itemName} 不能放入槽位 {index}");
                return false;
            }
        }
        
        equips[index] = item;
        OnEquipSlotChanged?.Invoke(index);
        return true;
    }

    #endregion

    #region 槽位访问（ItemStack 兼容版本）

    /// <summary>
    /// 获取指定槽位的装备（ItemStack 兼容）
    /// </summary>
    public ItemStack GetEquip(int index)
    {
        if (!InRange(index)) return ItemStack.Empty;
        var item = equips[index];
        if (item == null || item.IsEmpty) return ItemStack.Empty;
        return item.ToItemStack();
    }

    /// <summary>
    /// 设置指定槽位的装备（ItemStack 兼容）
    /// 注意：这会丢失动态属性！
    /// </summary>
    public bool SetEquip(int index, ItemStack stack)
    {
        if (!InRange(index)) return false;
        
        if (stack.IsEmpty)
        {
            equips[index] = null;
            OnEquipSlotChanged?.Invoke(index);
            return true;
        }
        
        // 槽位限制检查
        var itemData = database?.GetItemByID(stack.itemId);
        if (!CanEquipAt(index, itemData))
        {
            Debug.LogWarning($"[EquipmentService] 无法装备: {itemData?.itemName} 不能放入槽位 {index}");
            return false;
        }
        
        equips[index] = InventoryItem.FromItemStack(stack);
        OnEquipSlotChanged?.Invoke(index);
        return true;
    }
    
    /// <summary>
    /// 清空指定槽位
    /// </summary>
    public void ClearEquip(int index)
    {
        if (!InRange(index)) return;
        equips[index] = null;
        OnEquipSlotChanged?.Invoke(index);
    }

    #endregion

    #region 槽位限制检查（P0 核心）

    /// <summary>
    /// 检查物品是否可以装备到指定槽位
    /// 槽位映射：
    /// - 0: Helmet
    /// - 1: Pants
    /// - 2: Armor
    /// - 3: Shoes
    /// - 4, 5: Ring
    /// </summary>
    public bool CanEquipAt(int slotIndex, ItemData itemData)
    {
        if (itemData == null) return false;
        
        // 获取装备类型
        EquipmentType eqType = EquipmentType.None;
        
        // 优先检查 EquipmentData 子类
        // 注意：EquipmentData 在 FarmGame.Data 命名空间中
        if (itemData.GetType().Name == "EquipmentData")
        {
            // 使用反射获取 equipmentType 字段
            var field = itemData.GetType().GetField("equipmentType");
            if (field != null)
            {
                eqType = (EquipmentType)field.GetValue(itemData);
            }
        }
        else
        {
            // 回退到基类的 equipmentType
            eqType = itemData.equipmentType;
        }
        
        // 如果没有装备类型，检查是否是工具/武器（旧逻辑兼容）
        if (eqType == EquipmentType.None)
        {
            // 工具和武器可以装备到任意槽位（旧逻辑兼容）
            if (itemData is ToolData || itemData is WeaponData)
            {
                return true;
            }
            return false;
        }
        
        // 槽位类型匹配检查
        return slotIndex switch
        {
            0 => eqType == EquipmentType.Helmet,
            1 => eqType == EquipmentType.Pants,
            2 => eqType == EquipmentType.Armor,
            3 => eqType == EquipmentType.Shoes,
            4 or 5 => eqType == EquipmentType.Ring,
            _ => false
        };
    }

    /// <summary>
    /// 获取指定槽位允许的装备类型
    /// </summary>
    public EquipmentType GetSlotAllowedType(int slotIndex)
    {
        return slotIndex switch
        {
            0 => EquipmentType.Helmet,
            1 => EquipmentType.Pants,
            2 => EquipmentType.Armor,
            3 => EquipmentType.Shoes,
            4 or 5 => EquipmentType.Ring,
            _ => EquipmentType.None
        };
    }

    #endregion

    #region 装备/卸下操作

    /// <summary>
    /// 从背包装备物品到装备栏
    /// </summary>
    public bool TryEquipFromInventory(InventoryService inv, int invIndex, int preferredEquipIndex = -1)
    {
        if (inv == null) return false;
        var st = inv.GetSlot(invIndex);
        if (st.IsEmpty) return false;

        var itemData = database?.GetItemByID(st.itemId);
        if (!IsEquipable(st.itemId))
        {
            Debug.Log("[EquipmentService] 非可装备物品，忽略。");
            return false;
        }

        // 确定目标槽位
        int target = -1;
        
        if (preferredEquipIndex >= 0 && InRange(preferredEquipIndex))
        {
            // 检查指定槽位是否允许该装备类型
            if (CanEquipAt(preferredEquipIndex, itemData))
            {
                target = preferredEquipIndex;
            }
        }
        
        // 如果没有指定槽位或指定槽位不允许，自动查找合适的空槽位
        if (target < 0)
        {
            target = FindSuitableEmptySlot(itemData);
        }
        
        if (target < 0)
        {
            Debug.Log("[EquipmentService] 没有合适的空槽位。");
            return false;
        }

        // 移动整堆（通常装备是不可堆叠的）
        // 使用 ItemStack 转换，因为 InventoryService 可能没有 GetItem 方法
        SetEquip(target, st);
        inv.ClearSlot(invIndex);
        return true;
    }

    /// <summary>
    /// 卸下装备到背包
    /// </summary>
    public bool UnequipToInventory(InventoryService inv, int equipIndex)
    {
        if (inv == null || !InRange(equipIndex)) return false;
        
        var item = equips[equipIndex];
        if (item == null || item.IsEmpty) return false;
        
        // 尝试添加到背包（使用 ItemStack 兼容接口）
        int remaining = inv.AddItem(item.ItemId, item.Quality, item.Amount);
        if (remaining == 0)
        {
            equips[equipIndex] = null;
            OnEquipSlotChanged?.Invoke(equipIndex);
            return true;
        }
        
        Debug.LogWarning("[EquipmentService] 背包空间不足，无法卸下。");
        return false;
    }

    /// <summary>
    /// 查找适合该物品的空槽位
    /// </summary>
    private int FindSuitableEmptySlot(ItemData itemData)
    {
        if (itemData == null) return FindFirstEmpty();
        
        // 获取装备类型
        EquipmentType eqType = EquipmentType.None;
        if (itemData.GetType().Name == "EquipmentData")
        {
            var field = itemData.GetType().GetField("equipmentType");
            if (field != null)
            {
                eqType = (EquipmentType)field.GetValue(itemData);
            }
        }
        else
        {
            eqType = itemData.equipmentType;
        }
        
        // 如果没有装备类型（工具/武器），返回第一个空槽位
        if (eqType == EquipmentType.None)
        {
            return FindFirstEmpty();
        }
        
        // 根据装备类型查找对应的空槽位
        int[] candidateSlots = eqType switch
        {
            EquipmentType.Helmet => new[] { 0 },
            EquipmentType.Pants => new[] { 1 },
            EquipmentType.Armor => new[] { 2 },
            EquipmentType.Shoes => new[] { 3 },
            EquipmentType.Ring => new[] { 4, 5 },
            _ => new int[0]
        };
        
        foreach (int slot in candidateSlots)
        {
            if (equips[slot] == null || equips[slot].IsEmpty)
            {
                return slot;
            }
        }
        
        return -1;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 供UI层调用的公开方法
    /// </summary>
    public bool IsEquipableItemPublic(int itemId)
    {
        return IsEquipable(itemId);
    }

    bool IsEquipable(int itemId)
    {
        if (database == null) return false;
        var data = database.GetItemByID(itemId);
        if (data == null) return false;
        
        // EquipmentData 子类可装备
        if (data.GetType().Name == "EquipmentData") return true;
        
        // 工具和武器可装备（旧逻辑兼容）
        return data is ToolData || data is WeaponData;
    }

    int FindFirstEmpty()
    {
        for (int i = 0; i < equips.Length; i++)
            if (equips[i] == null || equips[i].IsEmpty) return i;
        return -1;
    }

    bool InRange(int i) => i >= 0 && i < equips.Length;

    #endregion
}

/// <summary>
/// 装备存档数据
/// </summary>
[Serializable]
public class EquipmentSaveData
{
    public InventorySlotSaveData[] slots;
}
