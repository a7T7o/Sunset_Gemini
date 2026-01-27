using System;
using FarmGame.Data;

/// <summary>
/// 物品容器接口 - 统一 ChestInventory 和 InventoryService 的访问方式
/// 用于 UI 绑定，支持槽位读取、事件订阅等
/// </summary>
public interface IItemContainer
{
    /// <summary>
    /// 容器容量
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// 物品数据库引用
    /// </summary>
    ItemDatabase Database { get; }

    /// <summary>
    /// 单个槽位变化事件（参数：槽位索引）
    /// </summary>
    event Action<int> OnSlotChanged;

    /// <summary>
    /// 整体库存变化事件
    /// </summary>
    event Action OnInventoryChanged;

    /// <summary>
    /// 获取指定槽位的物品
    /// </summary>
    ItemStack GetSlot(int index);

    /// <summary>
    /// 设置指定槽位的物品
    /// </summary>
    bool SetSlot(int index, ItemStack stack);

    /// <summary>
    /// 清空指定槽位
    /// </summary>
    void ClearSlot(int index);

    /// <summary>
    /// 交换或合并两个槽位
    /// </summary>
    bool SwapOrMerge(int a, int b);

    /// <summary>
    /// 获取物品的最大堆叠数量
    /// </summary>
    int GetMaxStack(int itemId);

    /// <summary>
    /// 排序容器内的物品
    /// </summary>
    void Sort();
}
