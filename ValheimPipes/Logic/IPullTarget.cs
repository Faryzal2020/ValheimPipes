using System.Collections.Generic;

namespace ValheimPipes.Logic {
    public interface IPullTarget : ITarget {
        HopperPriority PullPriority { get; }
        bool IsPickup { get; }

        IEnumerable<ItemDrop.ItemData> GetItems();
        void RemoveItem(ItemDrop.ItemData item, Inventory destination, Vector2i destinationPos, ZDOID sender, int amount = 1);
    }
}
