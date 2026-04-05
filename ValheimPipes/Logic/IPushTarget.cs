namespace ValheimPipes.Logic {
    public interface IPushTarget : ITarget {
        HopperPriority PushPriority { get; }

        bool CanAddItem(ItemDrop.ItemData item);
        void AddItem(ItemDrop.ItemData item, Inventory source, ZDOID sender, int amount = 1);
    }
}
