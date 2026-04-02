using System.Collections.Generic;
using UnityEngine;

namespace ValheimPipes.Logic {
    public class FermenterTarget : NetworkPiece, IPullTarget {
        public HopperPriority PullPriority { get; } = HopperPriority.BeehivePull; // Similar priority to beehives
        public bool IsPickup { get; } = false;

        private Fermenter fermenter;
        private const string RequestOwnershipRPC = "VH_RequestOwnership";

        protected override void Awake() {
            base.Awake();
            fermenter = GetComponent<Fermenter>();
            fermenter.m_nview.Register(RequestOwnershipRPC, RPC_RequestOwnership);
        }

        public bool InRange(Vector3 position) {
            return true;
        }

        public IEnumerable<ItemDrop.ItemData> GetItems() {
            if (fermenter.GetStatus() == Fermenter.Status.Ready) {
                string content = fermenter.m_nview.GetZDO().GetString("item");
                if (!string.IsNullOrEmpty(content)) {
                    GameObject prefab = ObjectDB.instance.GetItemPrefab(content);
                    if (prefab) {
                        ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                        if (itemDrop) {
                            yield return itemDrop.m_itemData;
                        }
                    }
                }
            }
        }

        public void RemoveItem(ItemDrop.ItemData item, Inventory destination, Vector2i destinationPos, ZDOID sender) {
            if (!fermenter.m_nview.IsOwner()) {
                fermenter.m_nview.InvokeRPC(RequestOwnershipRPC);
                return;
            }

            if (fermenter.GetStatus() != Fermenter.Status.Ready) {
                return;
            }

            // Fermenters give multiple items (usually 6)
            // But we pull them one by one.
            // Valheim fermenters don't have a 'count' in ZDO, they just reset to empty.
            // However, some mods might add a count. 
            // In vanilla, extracting mead resets the fermenter immediately.
            
            fermenter.m_nview.GetZDO().Set("status", (int)Fermenter.Status.Empty);
            fermenter.m_nview.GetZDO().Set("item", "");
            fermenter.m_nview.GetZDO().Set("startTime", 0L);
            
            // Standard mead gives 6 items. 
            // To be fair and simple, we add the full stack if it's mead, 
            // or 1 if it's a custom collector.
            int count = item.m_shared.m_name.Contains("mead") ? 6 : 1;
            destination.AddItem(item.Clone(), count, destinationPos.x, destinationPos.y);
        }

        private void RPC_RequestOwnership(long sender) {
            if (!fermenter.m_nview.IsOwner()) return;
            fermenter.m_nview.GetZDO().SetOwner(sender);
            ZDOMan.instance.ForceSendZDO(sender, fermenter.m_nview.GetZDO().m_uid);
        }
    }
}
