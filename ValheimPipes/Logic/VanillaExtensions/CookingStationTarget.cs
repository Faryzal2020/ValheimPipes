using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ValheimPipes.Logic.Helper;

namespace ValheimPipes.Logic {
    public class CookingStationTarget : NetworkPiece, IPushTarget, IPullTarget {
        public HopperPriority PushPriority { get; } = HopperPriority.SmelterOrePush;

        private CookingStation cookingStation;
        private static FieldInfo cookableField;
        private static bool fieldDetected = false;

        protected override void Awake() {
            base.Awake();
            cookingStation = GetComponent<CookingStation>();

            foreach (FieldInfo field in typeof(CookingStation).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                Plugin.Debug($"CookingStation field: {field.FieldType} {field.Name}");
            }

            if (!fieldDetected) {
                cookableField = typeof(CookingStation).GetField("m_cookable",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (cookableField == null) {
                    cookableField = typeof(CookingStation).GetField("m_conversion",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                Plugin.Debug($"Checking {this.GetType().Name}");
                Plugin.Debug($"cookableField resolved to: {cookableField?.Name ?? "NULL"}");
                fieldDetected = true;
            }
        }

        private void Start() {
            string[] candidates = new[] {
                "RPC_AddFuel", "RPC_AddItem", "RPC_RemoveDoneItem", "RPC_SetSlotVisual",
                "RPC_RequestOwn", "RPC_Pick", "RPC_RemoveItem", "RPC_ClearSlot",
                "RPC_SetCookingStation", "RPC_AddItemToSlot", "RPC_RemoveSlot"
            };

            foreach (string name in candidates) {
                int hash = name.GetStableHashCode();
                if (cookingStation.m_nview.m_functions.ContainsKey(hash))
                    Plugin.Debug($"MATCHED: '{name}' = {hash}");
            }

            // Log all hashes so we can cross-reference
            foreach (var key in cookingStation.m_nview.m_functions.Keys)
                Plugin.Debug($"Registered hash: {key}");
        }

        private bool IsFuelItem(ItemDrop.ItemData item) {
            if (cookingStation.m_fuelItem == null) return false;
            return cookingStation.m_fuelItem.m_itemData.m_shared.m_name == item.m_shared.m_name;
        }

        private bool IsCookableItem(ItemDrop.ItemData item) {
            if (cookableField == null) {
                Plugin.Debug("cookableField is null — reflection failed at Awake");
                return false;
            }

            IEnumerable cookableList = cookableField.GetValue(cookingStation) as IEnumerable;
            if (cookableList == null) {
                Plugin.Debug($"cookableList cast failed on field: {cookableField.Name}");
                return false;
            }

            foreach (object conversion in cookableList) {
                FieldInfo fromField = conversion.GetType().GetField("m_from",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (fromField == null) continue;

                ItemDrop fromItem = fromField.GetValue(conversion) as ItemDrop;
                if (fromItem == null) continue;

                // Log what we're comparing so you can see the mismatch
                Plugin.Debug($"Comparing incoming '{item.m_shared.m_name}' vs conversion '{fromItem.m_itemData.m_shared.m_name}'");

                if (fromItem.m_itemData.m_shared.m_name == item.m_shared.m_name)
                    return true;
            }

            return false;
        }

        private bool IsIncompatibleItem(ItemDrop.ItemData item) {
            FieldInfo incompatibleField = typeof(CookingStation).GetField("m_incompatibleItems",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (incompatibleField == null) return false;

            IEnumerable list = incompatibleField.GetValue(cookingStation) as IEnumerable;
            if (list == null) return false;

            foreach (object entry in list) {
                FieldInfo itemField = entry.GetType().GetField("m_item",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (itemField == null) continue;
                ItemDrop drop = itemField.GetValue(entry) as ItemDrop;
                if (drop != null && drop.m_itemData.m_shared.m_name == item.m_shared.m_name)
                    return true;
            }
            return false;
        }

        private bool CanAddFuel() {
            if (cookingStation.m_fuelItem == null) return false; // doesn't use fuel

            // m_maxFuel is a float on CookingStation
            float currentFuel = cookingStation.GetFuel();
            return currentFuel < cookingStation.m_maxFuel;
        }

        public bool CanAddItem(ItemDrop.ItemData item) {
            if (IsIncompatibleItem(item)) return false;

            if (cookingStation == null) {
                Plugin.Debug($"{this.GetType().Name} cookingStation is null");
                return false;
            }

            if (IsFuelItem(item)) {
                Plugin.Debug($"{this.GetType().Name}'s {item.m_shared.m_name} is fuel");
                return CanAddFuel();
            }
            if (cookableField != null){
                if (IsCookableItem(item)) {
                    Plugin.Debug($"{this.GetType().Name}'s {item.m_shared.m_name} is cookable");
                    return cookingStation.GetFreeSlot() != -1;
                } else {
                    Plugin.Debug($"{this.GetType().Name}'s {item.m_shared.m_name} is unknown");
                    return false;
                }
            }

            // Unknown item — don't let it through
            Plugin.Debug($"cookableField null, falling back to slot check for '{item.m_shared.m_name}'");
            return cookingStation.GetFreeSlot() != -1;
        }

        public void AddItem(ItemDrop.ItemData item, Inventory source, ZDOID sender) {
            bool removed = source.RemoveItem(item, 1);
            if (!removed) return;

            if (IsFuelItem(item)) {
                cookingStation.m_nview.InvokeRPC("RPC_AddFuel");
            } else {
                cookingStation.m_nview.InvokeRPC("RPC_AddItem", item.m_dropPrefab.name);
            }
        }

        public bool InRange(Vector3 position) {
            return true;
        }

        // IPullTarget
        public HopperPriority PullPriority { get; } = HopperPriority.HopperPull;
        public bool IsPickup { get; } = false;

        public IEnumerable<ItemDrop.ItemData> GetItems() {
            List<ItemDrop.ItemData> result = new List<ItemDrop.ItemData>();
            if (cookingStation == null) return result;

            int slotCount = cookingStation.m_slots.Length;

            for (int i = 0; i < slotCount; i++) {
                string itemName = cookingStation.m_nview.GetZDO().GetString($"slot{i}");

                if (string.IsNullOrEmpty(itemName) || itemName == "0") continue;

                // Item is pullable if it's a finished cooked product (m_to match)
                // OR if it's overcooked (coal)
                bool isCooked = GetConversionForCooked(itemName) != null;
                bool isOvercooked = itemName == cookingStation.m_overCookedItem?.name;

                if (!isCooked && !isOvercooked) continue;

                GameObject prefab = ZNetScene.instance.GetPrefab(itemName);
                if (prefab == null) continue;

                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop == null) continue;

                result.Add(drop.m_itemData.Clone());
            }

            return result;
        }

        public void RemoveItem(ItemDrop.ItemData item, Inventory destination, Vector2i destinationPos, ZDOID sender) {
            int slotCount = cookingStation.m_slots.Length;
            ZDO zdo = cookingStation.m_nview.GetZDO();

            for (int i = 0; i < slotCount; i++) {
                string slotItem = zdo.GetString($"slot{i}");

                GameObject prefab = ZNetScene.instance.GetPrefab(slotItem);
                if (prefab == null) continue;

                ItemDrop slotDrop = prefab.GetComponent<ItemDrop>();
                if (slotDrop == null) continue;
                if (slotDrop.m_itemData.m_shared.m_name != item.m_shared.m_name) continue;

                // Claim ZDO ownership then clear the slot directly
                cookingStation.m_nview.ClaimOwnership();
                zdo.Set($"slot{i}", "");
                zdo.Set($"slot{i}_ct", 0f);

                // Force the visual to clear via the broadcast RPC
                cookingStation.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", i, "");

                destination.AddItem(item.Clone(), 1, destinationPos.x, destinationPos.y);
                Plugin.Debug($"RemoveItem: cleared slot {i} ({slotItem}) via ZDO write");
                return;
            }

            Plugin.Debug($"RemoveItem: no slot found for '{item.m_shared.m_name}'");
        }

        private CookingStation.ItemConversion GetConversionForCooked(string cookedItemName) {
            if (cookableField == null) return null;

            IEnumerable list = cookableField.GetValue(cookingStation) as IEnumerable;
            if (list == null) return null;

            foreach (CookingStation.ItemConversion conversion in list) {
                if (conversion.m_to?.name == cookedItemName)
                    return conversion;
            }

            return null;
        }
    }
}
