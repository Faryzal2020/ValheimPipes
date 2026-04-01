using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ValheimHopper.Logic.Helper;

namespace ValheimHopper.Logic {
    public class CookingStationTarget : NetworkPiece, IPushTarget {
        public HopperPriority PushPriority { get; } = HopperPriority.SmelterOrePush;

        private CookingStation cookingStation;
        private static FieldInfo cookableField;
        private static bool fieldDetected = false;

        protected override void Awake() {
            base.Awake();
            cookingStation = GetComponent<CookingStation>();

            if (!fieldDetected) {
                cookableField = typeof(CookingStation).GetField("m_cookable",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (cookableField == null) {
                    cookableField = typeof(CookingStation).GetField("m_conversions",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                fieldDetected = true;
            }
        }

        private bool IsFuelItem(ItemDrop.ItemData item) {
            if (cookingStation.m_fuelItem == null) return false;
            return cookingStation.m_fuelItem.m_itemData.m_shared.m_name == item.m_shared.m_name;
        }

        private bool IsCookableItem(ItemDrop.ItemData item) {
            if (cookableField == null) return false;

            IEnumerable cookableList = cookableField.GetValue(cookingStation) as IEnumerable;
            if (cookableList == null) return false;

            foreach (object conversion in cookableList) {
                FieldInfo fromField = conversion.GetType().GetField("m_from",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fromField != null) {
                    ItemDrop fromItem = fromField.GetValue(conversion) as ItemDrop;
                    if (fromItem != null && fromItem.m_itemData.m_shared.m_name == item.m_shared.m_name)
                        return true;
                }
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
            if (cookingStation == null) return false;

            if (IsFuelItem(item))
                return CanAddFuel();

            if (IsCookableItem(item))
                return cookingStation.GetFreeSlot() != -1;

            // Unknown item — don't let it through
            return false;
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
    }
}
