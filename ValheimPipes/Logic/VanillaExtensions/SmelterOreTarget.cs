using UnityEngine;
using ValheimPipes.Logic.Helper;

namespace ValheimPipes.Logic {
    public class SmelterOreTarget : NetworkPiece, IPushTarget {
        public HopperPriority PushPriority { get; } = HopperPriority.SmelterOrePush;

        private Smelter smelter;

        protected override void Awake() {
            base.Awake();
            smelter = GetComponent<Smelter>();
        }

        public bool CanAddItem(ItemDrop.ItemData item) {
            return smelter.IsItemAllowed(item) && smelter.GetQueueSize() < smelter.m_maxOre;
        }

        public void AddItem(ItemDrop.ItemData item, Inventory source, ZDOID sender, int amount = 1) {
            int queueSize = smelter.GetQueueSize();
            int canAddCount = smelter.m_maxOre - queueSize;
            int toAdd = Mathf.Min(amount, canAddCount);

            if (toAdd <= 0) return;

            source.RemoveItem(item, toAdd);

            for (int i = 0; i < toAdd; i++) {
                smelter.m_nview.InvokeRPC("RPC_AddOre", item.m_dropPrefab.name);
            }
        }

        public bool InRange(Vector3 position) {
            return HopperHelper.IsInRange(position, smelter.m_addOreSwitch.transform.position, 1f);
        }
    }
}
