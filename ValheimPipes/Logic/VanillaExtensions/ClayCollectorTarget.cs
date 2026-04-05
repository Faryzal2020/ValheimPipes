using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ValheimPipes.Logic {
    public class ClayCollectorTarget : NetworkPiece, IPullTarget {
        public HopperPriority PullPriority { get; } = HopperPriority.BeehivePull;
        public bool IsPickup { get; } = false;

        private Component collectorComponent;
        private MethodInfo getTarLevelMethod;
        private MethodInfo resetLevelMethod;
        private FieldInfo clayItemField;

        private const string RequestOwnershipRPC = "VH_RequestOwnership_Clay";

        protected override void Awake() {
            base.Awake();
            
            // Find the collector component by name securely
            collectorComponent = GetComponent("ClayCollector");
            if (collectorComponent != null) {
                Type type = collectorComponent.GetType();
                getTarLevelMethod = type.GetMethod("GetTarLevel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                resetLevelMethod = type.GetMethod("ResetLevel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                clayItemField = type.GetField("m_clayItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                Plugin.Debug($"[ClayCollector] Found component. Methods: GetTarLevel={(getTarLevelMethod != null)}, ResetLevel={(resetLevelMethod != null)}, m_clayItem={(clayItemField != null)}");
            } else {
                Plugin.Debug("[ClayCollector] Critical: 'ClayCollector' component not found on this object!");
            }

            if (zNetView != null) {
                zNetView.Register(RequestOwnershipRPC, RPC_RequestOwnership);
            }
        }

        public bool InRange(Vector3 position) {
            return true;
        }

        public IEnumerable<ItemDrop.ItemData> GetItems() {
            if (zNetView == null || !zNetView.IsValid() || collectorComponent == null) yield break;

            int level = GetCurrentLevel();
            if (level > 0) {
                GameObject prefab = GetClayPrefab();
                if (prefab) {
                    ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                    if (itemDrop) {
                        yield return itemDrop.m_itemData;
                    }
                }
            }
        }

        public void RemoveItem(ItemDrop.ItemData item, Inventory destination, Vector2i destinationPos, ZDOID sender, int amount = 1) {
            if (zNetView == null || !zNetView.IsValid() || collectorComponent == null) return;

            if (!zNetView.IsOwner()) {
                zNetView.InvokeRPC(RequestOwnershipRPC);
                return;
            }

            int level = GetCurrentLevel();
            if (level <= 0) return;

            // Reset the machine via reflection
            if (resetLevelMethod != null) {
                resetLevelMethod.Invoke(collectorComponent, null);
            } else {
                // Fallback: manually reset ZDO common keys if method is missing
                zNetView.GetZDO().Set("Produced", 0);
                zNetView.GetZDO().Set("level", 0);
            }

            destination.AddItem(item.Clone(), level, destinationPos.x, destinationPos.y);
        }

        private int GetCurrentLevel() {
            if (getTarLevelMethod != null) {
                return (int)getTarLevelMethod.Invoke(collectorComponent, null);
            }
            
            // Fallback for different mod versions: try ZDO count
            int count = zNetView.GetZDO().GetInt("Produced", 0);
            if (count == 0) count = zNetView.GetZDO().GetInt("level", 0);
            return count;
        }

        private GameObject GetClayPrefab() {
            if (clayItemField != null) {
                GameObject reflected = clayItemField.GetValue(collectorComponent) as GameObject;
                if (reflected) return reflected;
            }
            
            // Standard fallback including BFP_Clay
            GameObject prefab = ObjectDB.instance.GetItemPrefab("BFP_Clay");
            if (!prefab) prefab = ObjectDB.instance.GetItemPrefab("Clay");
            return prefab;
        }

        private void RPC_RequestOwnership(long sender) {
            if (zNetView == null || !zNetView.IsOwner()) return;
            zNetView.GetZDO().SetOwner(sender);
            ZDOMan.instance.ForceSendZDO(sender, zNetView.GetZDO().m_uid);
        }
    }
}
