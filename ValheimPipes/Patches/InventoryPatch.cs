using HarmonyLib;
using ValheimPipes.UI;
using ValheimPipes.Logic;
using UnityEngine;

namespace ValheimPipes.Patches {
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) })]
    public static class InventoryPatch {
        [HarmonyPrefix]
        public static bool AddItem_Prefix(Inventory __instance, ItemDrop.ItemData item, int amount, int x, int y, bool skipValidPositionCheck) {
            // Check if HopperUI exists and if this inventory belongs to its filterContainer
            if (HopperUI.Instance != null) {
                // We need access to filterContainer. We can either make it public or use Reflection.
                // Since I just added it as private [SerializeField], I'll need to check how to access it.
                // Let's assume for a moment we can access it or I'll make it internal/public.
                
                // Let's check the instance comparison. 
                // We can't easily access the private field without reflection or a public property.
                // I'll go back and add a public property to HopperUI.cs for the FilterInventory.
                
                if (CheckIsFilterInventory(__instance)) {
                    Jotunn.Logger.LogInfo($"[HopperUI] Intercepted Item Placement:");
                    Jotunn.Logger.LogInfo($" - Name: {item.m_shared.m_name}");
                    Jotunn.Logger.LogInfo($" - Prefab: {item.m_dropPrefab.name}");
                    Jotunn.Logger.LogInfo($" - Slot: ({x}, {y})");
                    Jotunn.Logger.LogInfo($" - Amount: {amount}");
                    
                    // Return false to prevent the item from being added to the container.
                    // This will return it to the source inventory.
                    return false;
                }
            }
            
            return true;
        }

        private static bool CheckIsFilterInventory(Inventory inventory) {
            // This is a bit of a hack since the field is private. 
            // I will update HopperUI.cs to provide a public way to identify its filter inventory.
            return HopperUI.IsFilterInventory(inventory);
        }
    }
    [HarmonyPatch(typeof(Container), "Save")]
    public static class PatchContainerSave {
        static bool Prefix(Container __instance) {
            // Block Save() from running on our filter container
            if (HopperUI.Instance != null && __instance == HopperUI.Instance.FilterContainer)
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Container), "Load")]
    public static class PatchContainerLoad {
        static bool Prefix(Container __instance) {
            // Block Load() from running on our filter container
            if (HopperUI.Instance != null && __instance == HopperUI.Instance.FilterContainer)
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Container), "Awake")]
    public static class PatchContainerAwake {
        static bool Prefix(Container __instance) {
            // UI-only containers (e.g. HopperUI filterContainer) have no ZNetView.
            // Valheim 1.0 Container.Awake calls m_nview.GetZDO() unconditionally, throwing NullReferenceException if m_nview is null.
            if (!__instance.m_rootObjectOverride && !__instance.GetComponent<ZNetView>()) {
                if (__instance.m_inventory == null) {
                    __instance.m_inventory = new Inventory(__instance.m_name, __instance.m_bkg, __instance.m_width, __instance.m_height);
                }
                return false;
            }
            return true;
        }
    }
}
