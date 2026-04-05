using UnityEngine;
using ValheimPipes.Logic.Helper;

namespace ValheimPipes.Logic {
    public class SmelterFuelTarget : NetworkPiece, IPushTarget {
        public HopperPriority PushPriority { get; } = HopperPriority.SmelterFuelPush;

        private Smelter smelter;

        protected override void Awake() {
            base.Awake();
            smelter = GetComponent<Smelter>();
        }

        public bool CanAddItem(ItemDrop.ItemData item) {
            bool isFuelItem = smelter.m_fuelItem && smelter.m_fuelItem.m_itemData.m_shared.m_name == item.m_shared.m_name;
            return isFuelItem && smelter.GetFuel() < smelter.m_maxFuel - 1;
        }

        public void AddItem(ItemDrop.ItemData item, Inventory source, ZDOID sender, int amount = 1) {
            float fuel = smelter.GetFuel();
            int canAddCount = Mathf.FloorToInt(smelter.m_maxFuel - fuel);
            int toAdd = Mathf.Min(amount, canAddCount);

            if (toAdd <= 0) return;

            bool removed = source.RemoveItem(item, toAdd);

            if (!removed) {
                return;
            }

            for (int i = 0; i < toAdd; i++) {
                smelter.m_nview.InvokeRPC("RPC_AddFuel");
            }
        }

        public bool InRange(Vector3 position) {
            return HopperHelper.IsInRange(position, smelter.m_addWoodSwitch.transform.position, 1f);
        }
    }
}
