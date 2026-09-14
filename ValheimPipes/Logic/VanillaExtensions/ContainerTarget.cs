using System.Collections.Generic;
using MultiUserChest;
using UnityEngine;
using ValheimPipes.Logic.Helper;

namespace ValheimPipes.Logic {
    public class ContainerTarget : NetworkPiece, IPushTarget, IPullTarget {
        public HopperPriority PushPriority { get; } = HopperPriority.ContainerPush;
        public HopperPriority PullPriority { get; } = HopperPriority.ContainerPull;
        public bool IsPickup { get; } = false;

        private Container container;

        protected override void Awake() {
            base.Awake();
            container = GetComponent<Container>();
        }

        private void Start() {
            if (container != null && container.GetInventory() != null) {
                container.GetInventory().m_onChanged += OnInventoryChanged;
            }
        }

        private void OnInventoryChanged() {
            HopperHelper.NotifyChange(this);
        }

        public IEnumerable<ItemDrop.ItemData> GetItems() {
            return container.GetInventory().GetItemInReverseOrder();
        }

        public void AddItem(ItemDrop.ItemData item, Container sourceContainer, ZDOID sender, int amount = 1) {
            if (sourceContainer != null && sourceContainer.GetInventory() != null) {
                container.AddItemToChest(item, sourceContainer.GetInventory(), new Vector2i(-1, -1), sender, amount);
            } else {
                container.AddItemDirect(item, new Vector2i(-1, -1), amount);
            }
        }

        public void RemoveItem(ItemDrop.ItemData item, Container destinationContainer, Vector2i destinationPos, ZDOID sender, int amount = 1) {
            if (destinationContainer != null && destinationContainer.GetInventory() != null) {
                container.RemoveItemFromChest(item, destinationContainer.GetInventory(), destinationPos, sender, amount);
            } else {
                container.GetInventory().RemoveItem(item, amount);
            }
        }

        public bool CanAddItem(ItemDrop.ItemData item) {
            return container.GetInventory().CanAddItem(item, 1);
        }

        public bool InRange(Vector3 position) {
            return true;
        }
    }
}
