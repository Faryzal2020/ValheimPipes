using UnityEngine;
using ValheimPipes.Logic.Helper;

namespace ValheimPipes.Logic {
    public class FireplaceFuelTarget : NetworkPiece, IPushTarget {
        public HopperPriority PushPriority { get; } = HopperPriority.SmelterFuelPush; // Same as smelter fuel

        private Fireplace fireplace;

        protected override void Awake() {
            base.Awake();
            fireplace = GetComponent<Fireplace>();
        }

        public bool CanAddItem(ItemDrop.ItemData item) {
            bool isFuelItem = fireplace.m_fuelItem && fireplace.m_fuelItem.m_itemData.m_shared.m_name == item.m_shared.m_name;
            float fuel = fireplace.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
            return isFuelItem && fuel < fireplace.m_maxFuel - 1;
        }

        public void AddItem(ItemDrop.ItemData item, Inventory source, ZDOID sender, int amount = 1) {
            float fuel = fireplace.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
            int canAddCount = Mathf.FloorToInt(fireplace.m_maxFuel - fuel);
            int toAdd = Mathf.Min(amount, canAddCount);

            if (toAdd <= 0) return;

            bool removed = source.RemoveItem(item, toAdd);
            if (!removed) return;

            for (int i = 0; i < toAdd; i++) {
                fireplace.m_nview.InvokeRPC("RPC_AddFuel");
            }
        }

        public bool InRange(Vector3 position) {
            return true;
        }
    }
}
