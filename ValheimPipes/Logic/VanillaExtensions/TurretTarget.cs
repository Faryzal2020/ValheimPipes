using System;
using UnityEngine;
using ValheimPipes.Logic.Helper;

namespace ValheimPipes.Logic {
    public class TurretTarget : NetworkPiece, IPushTarget {
        public HopperPriority PushPriority { get; } = HopperPriority.TurretPush;

        private Turret turret;

        protected override void Awake() {
            base.Awake();
            turret = GetComponent<Turret>();
        }

        public void AddItem(ItemDrop.ItemData item, Inventory source, ZDOID sender, int amount = 1) {
            int ammo = turret.GetAmmo();
            int canAddCount = turret.m_maxAmmo - ammo;
            int toAdd = Mathf.Min(amount, canAddCount);

            if (toAdd <= 0) return;

            bool removed = source.RemoveItem(item, toAdd);

            if (!removed) {
                return;
            }

            for (int i = 0; i < toAdd; i++) {
                turret.m_nview.InvokeRPC("RPC_AddAmmo", item.m_dropPrefab.name);
            }
        }

        public bool CanAddItem(ItemDrop.ItemData item) {
            if (!turret.IsItemAllowed(item.m_dropPrefab.name)) {
                return false;
            }

            if (turret.GetAmmo() > 0 && turret.GetAmmoType() != item.m_dropPrefab.name) {
                return false;
            }

            if (turret.GetAmmo() >= turret.m_maxAmmo) {
                return false;
            }

            return true;
        }

        public bool InRange(Vector3 position) {
            return true;
        }
    }
}
