using System.Collections.Generic;
using UnityEngine;
using ValheimPipes.Logic.Helper;

namespace ValheimPipes.Logic {
    public class ItemDropTarget : NetworkPiece, IPullTarget {
        public HopperPriority PullPriority { get; } = HopperPriority.ItemDropPull;
        public bool IsPickup { get; } = true;

        private ItemDrop itemDrop;

        protected override void Awake() {
            base.Awake();
            itemDrop = GetComponent<ItemDrop>();
        }

        public IEnumerable<ItemDrop.ItemData> GetItems() {
            if (itemDrop) {
                ItemHelper.CheckDropPrefab(itemDrop);
                yield return itemDrop.m_itemData;
            }
        }

        public void RemoveItem(ItemDrop.ItemData item, Inventory destination, Vector2i destinationPos, ZDOID sender, int amount = 1) {
            if (!itemDrop.m_nview.IsOwner()) {
                itemDrop.RequestOwn();
                return;
            }

            int currentStack = itemDrop.m_itemData.m_stack;
            int toRemove = Mathf.Min(currentStack, amount);

            if (toRemove <= 0) return;

            if (toRemove >= currentStack) {
                // Remove all
                itemDrop.m_nview.Destroy();
            } else {
                // Reduce stack
                itemDrop.m_itemData.m_stack -= toRemove;
                itemDrop.m_nview.GetZDO().Set(ZDOVars.s_stack, itemDrop.m_itemData.m_stack);
            }

            destination.AddItem(item.Clone(), toRemove, destinationPos.x, destinationPos.y);
        }

        public bool InRange(Vector3 position) {
            return true;
        }
    }
}
