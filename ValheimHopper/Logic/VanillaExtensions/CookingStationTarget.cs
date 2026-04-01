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
                cookableField = typeof(CookingStation).GetField("m_cookable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (cookableField == null) {
                    cookableField = typeof(CookingStation).GetField("m_conversions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                fieldDetected = true;
            }
        }

        public bool CanAddItem(ItemDrop.ItemData item) {
            if (cookingStation == null || cookingStation.GetFreeSlot() == -1) {
                return false;
            }

            // Use reflection to check if item is cookable
            if (cookableField != null) {
                IEnumerable cookableList = cookableField.GetValue(cookingStation) as IEnumerable;
                if (cookableList != null) {
                    foreach (object conversion in cookableList) {
                        // All conversion types (ItemConversion, Cookable) have an m_from field
                        FieldInfo fromField = conversion.GetType().GetField("m_from", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (fromField != null) {
                            ItemDrop fromItem = fromField.GetValue(conversion) as ItemDrop;
                            if (fromItem != null && fromItem.m_itemData.m_shared.m_name == item.m_shared.m_name) {
                                return true;
                            }
                        }
                    }
                }
            }

            // Fallback: If we can't find the list, we'll let the RPC handle it, 
            // but we'll return true to avoid blocking the hopper if the piece was intended to be cooked.
            // This is safer than returning false and breaking all cooking automation.
            return true;
        }

        public void AddItem(ItemDrop.ItemData item, Inventory source, ZDOID sender) {
            bool removed = source.RemoveItem(item, 1);
            if (!removed) return;

            // Add the item via RPC
            cookingStation.m_nview.InvokeRPC("RPC_AddItem", item.m_dropPrefab.name);
        }

        public bool InRange(Vector3 position) {
            return true;
        }
    }
}
