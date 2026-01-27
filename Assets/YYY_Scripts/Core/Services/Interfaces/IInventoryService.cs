namespace Sunset.Services
{
    /// <summary>
    /// 背包服务接口
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>添加物品到背包</summary>
        bool AddItem(int itemId, int quality, int amount);
        
        /// <summary>从背包移除物品</summary>
        bool RemoveItem(int itemId, int quality, int amount);
        
        /// <summary>获取背包槽位</summary>
        ItemStack GetSlot(int index);
        
        /// <summary>获取快捷栏槽位</summary>
        ItemStack GetHotbarSlot(int index);
        
        /// <summary>设置槽位内容</summary>
        void SetSlot(int index, ItemStack stack);
        
        /// <summary>交换两个槽位</summary>
        void SwapSlots(int indexA, int indexB);
        
        /// <summary>获取背包容量</summary>
        int GetCapacity();
        
        /// <summary>获取快捷栏容量</summary>
        int GetHotbarCapacity();
        
        /// <summary>检查是否有足够空间</summary>
        bool HasSpace(int itemId, int amount);
        
        /// <summary>获取物品总数量</summary>
        int GetItemCount(int itemId, int quality = -1);
    }
}
