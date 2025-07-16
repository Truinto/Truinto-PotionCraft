using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.BuildableInventoryItem;

namespace MagicGarden
{
    public class SortInventoryItem : IComparer<(InventoryItem item, int amount)>
    {
        public int Compare(InventoryItem x, InventoryItem y)
        {
            if (x is Seed != y is Seed)
                return x is Seed ? 1 : -1;
            if (x.sortingId != y.sortingId)
                return x.sortingId.CompareTo(y.sortingId);
            return x.price.CompareTo(y.price);
        }

        public int Compare((InventoryItem item, int amount) x, (InventoryItem item, int amount) y)
        {
            if (x.item is Seed != y.item is Seed)
                return x.item is Seed ? 1 : -1;
            if (x.item.sortingId != y.item.sortingId)
                return x.item.sortingId.CompareTo(y.item.sortingId);
            return x.item.price.CompareTo(y.item.price);
        }
    }
}
