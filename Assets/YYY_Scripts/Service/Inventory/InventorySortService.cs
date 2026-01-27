using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Data;

/// <summary>
/// 背包整理服务
/// </summary>
public class InventorySortService : MonoBehaviour
{
    [SerializeField] private InventoryService inventory;
    [SerializeField] private ItemDatabase database;
    
    void Awake()
    {
        if (inventory == null) inventory = FindFirstObjectByType<InventoryService>();
        if (database == null && inventory != null) database = inventory.Database;
    }
    
    /// <summary>
    /// 整理背包（合并 + 排序）
    /// 只整理 Up 区域（0-35），不影响装备栏
    /// </summary>
    public void SortInventory()
    {
        if (inventory == null || database == null)
        {
            Debug.LogWarning("[InventorySortService] inventory 或 database 为 null");
            return;
        }
        
        // 1. 收集所有物品
        var items = new List<ItemStack>();
        for (int i = 0; i < 36; i++)
        {
            var stack = inventory.GetSlot(i);
            if (!stack.IsEmpty)
            {
                items.Add(stack);
                inventory.SetSlot(i, ItemStack.Empty);
            }
        }
        
        // 2. 合并相同物品
        items = MergeStacks(items);
        
        // 3. 按优先级排序
        items = items.OrderBy(s => GetPriority(s))
                     .ThenBy(s => s.itemId)
                     .ThenBy(s => s.quality)
                     .ToList();
        
        // 4. 放回槽位
        int slotIndex = 0;
        foreach (var stack in items)
        {
            if (slotIndex >= 36) break;
            inventory.SetSlot(slotIndex++, stack);
        }
        
        Debug.Log($"[InventorySortService] 整理完成: {items.Count} 种物品");
    }
    
    /// <summary>
    /// 合并相同物品
    /// </summary>
    private List<ItemStack> MergeStacks(List<ItemStack> items)
    {
        var merged = new Dictionary<(int id, int quality), ItemStack>();
        
        foreach (var stack in items)
        {
            var key = (stack.itemId, stack.quality);
            
            if (merged.TryGetValue(key, out var existing))
            {
                existing.amount += stack.amount;
                merged[key] = existing;
            }
            else
            {
                merged[key] = stack;
            }
        }
        
        // 拆分超过最大堆叠的物品
        var result = new List<ItemStack>();
        foreach (var kvp in merged)
        {
            var stack = kvp.Value;
            var itemData = database.GetItemByID(stack.itemId);
            int maxStack = itemData?.maxStackSize ?? 99;
            
            while (stack.amount > 0)
            {
                int amount = Mathf.Min(stack.amount, maxStack);
                result.Add(new ItemStack
                {
                    itemId = stack.itemId,
                    quality = stack.quality,
                    amount = amount
                });
                stack.amount -= amount;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取物品排序优先级（数字越小越靠前）
    /// </summary>
    private int GetPriority(ItemStack stack)
    {
        var itemData = database?.GetItemByID(stack.itemId);
        if (itemData == null) return 999;
        
        // 工具 > 武器 > 可放置 > 种子 > 消耗品 > 材料 > 其他
        if (itemData is ToolData) return 0;
        if (itemData is WeaponData) return 1;
        if (itemData.isPlaceable) return 2;
        if (itemData is SeedData) return 3;
        if (itemData.category == ItemCategory.Consumable) return 4;
        if (itemData.category == ItemCategory.Material) return 5;
        
        return 6;
    }
}
