using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ValheimPipes.Logic.Helper;

namespace ValheimPipes.Logic {
    public class WindmillTarget : NetworkPiece, IPullTarget {
        public HopperPriority PullPriority { get; } = HopperPriority.HopperPull;
        public bool IsPickup { get; } = false;

        private Smelter smelter;

        protected override void Awake() {
            base.Awake();
            smelter = GetComponent<Smelter>();

            foreach (FieldInfo field in typeof(Smelter).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                Plugin.Debug($"Smelter field: {field.FieldType} {field.Name}");
            }

        }

        private void Start() {
            ZDO zdo = smelter.m_nview.GetZDO();
            Plugin.Debug($"Windmill ZDO - SpawnAmount: {zdo.GetInt("SpawnAmount", -999)}");
            Plugin.Debug($"Windmill ZDO - queued: {zdo.GetInt("queued", -999)}");
            Plugin.Debug($"Windmill ZDO - items: {zdo.GetInt("items", -999)}");
            Plugin.Debug($"Windmill ZDO - SpawnOre: {zdo.GetString("SpawnOre", "NULL")}");
        }

        public bool InRange(Vector3 position) {
            if (!smelter || !smelter.m_emptyOreSwitch) return false;
            return HopperHelper.IsInRange(position, smelter.m_emptyOreSwitch.transform.position, 1.5f);
        }

        public IEnumerable<ItemDrop.ItemData> GetItems() {
            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>();
            if (!smelter) return items;

            ZDO zdo = smelter.m_nview.GetZDO();
            int count = zdo.GetInt("SpawnAmount", 0);
            if (count <= 0) return items;

            // Windmill has only one conversion, but handle multiple gracefully
            Smelter.ItemConversion conversion = smelter.m_conversion.Count > 0 
                ? smelter.m_conversion[0] 
                : null;

            if (conversion?.m_to == null) return items;

            for (int i = 0; i < count; i++)
                items.Add(conversion.m_to.m_itemData.Clone());

            return items;
        }

        public void RemoveItem(ItemDrop.ItemData item, Inventory destination, Vector2i destinationPos, ZDOID sender, int amount = 1) {
            ZDO zdo = smelter.m_nview.GetZDO();
            int count = zdo.GetInt("SpawnAmount", 0);

            int toRemove = Mathf.Min(count, amount);
            if (toRemove <= 0) return;

            smelter.m_nview.ClaimOwnership();
            zdo.Set("SpawnAmount", count - toRemove);
            destination.AddItem(item.Clone(), toRemove, destinationPos.x, destinationPos.y);
            
            Plugin.Debug($"RemoveItem: removed {toRemove} {item.m_shared.m_name} from Windmill via ZDO write");
        }
    }
}
