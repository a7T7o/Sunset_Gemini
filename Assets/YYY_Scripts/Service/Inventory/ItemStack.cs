using System;
using FarmGame.Data;

[Serializable]
public struct ItemStack
{
    public int itemId;
    public int quality;
    public int amount;

    public static readonly ItemStack Empty = new ItemStack(-1, 0, 0);

    public ItemStack(int itemId, int quality, int amount)
    {
        this.itemId = itemId;
        this.quality = quality;
        this.amount = amount;
    }

    public bool IsEmpty => amount <= 0 || itemId < 0;

    public void Clear()
    {
        itemId = -1;
        quality = 0;
        amount = 0;
    }

    public bool CanStackWith(ItemStack other)
    {
        return !IsEmpty && !other.IsEmpty && itemId == other.itemId && quality == other.quality;
    }
}
