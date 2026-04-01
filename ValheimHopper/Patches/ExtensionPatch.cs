using HarmonyLib;
using ValheimHopper.Logic;
using System.Linq;
using UnityEngine;

namespace ValheimHopper.Patches {
    [HarmonyPatch]
    public class ExtensionPatch {
        [HarmonyPatch(typeof(Container), nameof(Container.Awake)), HarmonyPostfix]
        private static void ContainerAwakePostfix(Container __instance) {
            if (!__instance.GetComponent<ContainerTarget>()) {
                __instance.gameObject.AddComponent<ContainerTarget>();
            }
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake)), HarmonyPostfix]
        private static void ItemDropAwakePostfix(ItemDrop __instance) {
            if (!__instance.GetComponent<ItemDropTarget>()) {
                __instance.gameObject.AddComponent<ItemDropTarget>();
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.Awake)), HarmonyPostfix]
        private static void SmelterAwakePostfix(Smelter __instance) {
            if (__instance.m_addWoodSwitch && !__instance.GetComponent<SmelterFuelTarget>()) {
                __instance.gameObject.AddComponent<SmelterFuelTarget>();
            }

            if (__instance.m_addOreSwitch && !__instance.GetComponent<SmelterOreTarget>()) {
                __instance.gameObject.AddComponent<SmelterOreTarget>();
            }
        }

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Awake)), HarmonyPostfix]
        private static void BeehiveAwakePostfix(Beehive __instance) {
            if (!__instance.GetComponent<BeehiveTarget>()) {
                __instance.gameObject.AddComponent<BeehiveTarget>();
            }
        }

        [HarmonyPatch(typeof(Turret), nameof(Turret.Awake)), HarmonyPostfix]
        private static void TurretAwakePostfix(Turret __instance) {
            if (!__instance.GetComponent<TurretTarget>()) {
                __instance.gameObject.AddComponent<TurretTarget>();
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Awake)), HarmonyPostfix]
        private static void CookingStationAwakePostfix(CookingStation __instance) {
            if (!__instance.GetComponent<CookingStationTarget>()) {
                __instance.gameObject.AddComponent<CookingStationTarget>();
            }
        }

        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Awake)), HarmonyPostfix]
        private static void FireplaceAwakePostfix(Fireplace __instance) {
            if (!__instance.GetComponent<FireplaceFuelTarget>()) {
                __instance.gameObject.AddComponent<FireplaceFuelTarget>();
            }
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
        private static void ZNetSceneAwakePostfix(ZNetScene __instance) {
            if (string.IsNullOrEmpty(Plugin.ExtraCompatiblePrefabs.Value)) return;

            string[] names = Plugin.ExtraCompatiblePrefabs.Value.Split(',').Select(x => x.Trim()).ToArray();
            foreach (string name in names) {
                GameObject prefab = __instance.GetPrefab(name);
                if (prefab) {
                    bool added = false;
                    
                    // Container
                    if (prefab.GetComponent<Container>() && !prefab.GetComponent<ContainerTarget>()) {
                        prefab.AddComponent<ContainerTarget>();
                        Plugin.Debug($"Added ContainerTarget to {name}");
                        added = true;
                    }
                    
                    // Beehive
                    if (prefab.GetComponent<Beehive>() && !prefab.GetComponent<BeehiveTarget>()) {
                        prefab.AddComponent<BeehiveTarget>();
                        Plugin.Debug($"Added BeehiveTarget to {name}");
                        added = true;
                    }
                    
                    // Smelter
                    if (prefab.GetComponent<Smelter>()) {
                        Smelter smelter = prefab.GetComponent<Smelter>();
                        if (smelter.m_addWoodSwitch && !prefab.GetComponent<SmelterFuelTarget>()) {
                            prefab.AddComponent<SmelterFuelTarget>();
                            Plugin.Debug($"Added SmelterFuelTarget to {name}");
                            added = true;
                        }
                        if (smelter.m_addOreSwitch && !prefab.GetComponent<SmelterOreTarget>()) {
                            prefab.AddComponent<SmelterOreTarget>();
                            Plugin.Debug($"Added SmelterOreTarget to {name}");
                            added = true;
                        }
                    }
                    
                    // Fermenter
                    if (prefab.GetComponent<Fermenter>() && !prefab.GetComponent<FermenterTarget>()) {
                        prefab.AddComponent<FermenterTarget>();
                        Plugin.Debug($"Added FermenterTarget to {name}");
                        added = true;
                    }
                    
                    // Fireplace
                    if (prefab.GetComponent<Fireplace>() && !prefab.GetComponent<FireplaceFuelTarget>()) {
                        prefab.AddComponent<FireplaceFuelTarget>();
                        Plugin.Debug($"Added FireplaceFuelTarget to {name}");
                        added = true;
                    }

                    // CookingStation
                    if (prefab.GetComponent<CookingStation>() && !prefab.GetComponent<CookingStationTarget>()) {
                        prefab.AddComponent<CookingStationTarget>();
                        Plugin.Debug($"Added CookingStationTarget to {name}");
                        added = true;
                    }
                    
                    // Turret
                    if (prefab.GetComponent<Turret>() && !prefab.GetComponent<TurretTarget>()) {
                        prefab.AddComponent<TurretTarget>();
                        Plugin.Debug($"Added TurretTarget to {name}");
                        added = true;
                    }

                    // Specialized Clay Collector (blacks7ar)
                    if (!added && (prefab.GetComponent("ClayCollector") != null) && !prefab.GetComponent<ClayCollectorTarget>()) {
                        prefab.AddComponent<ClayCollectorTarget>();
                        Plugin.Debug($"Added ClayCollectorTarget to {name}");
                        added = true;
                    }

                    if (!added) {
                        string components = string.Join(", ", prefab.GetComponents<Component>().Select(c => c.GetType().Name));
                        Plugin.Debug($"Compatibility info: Prefab '{name}' found but no standard production component detected. Components found: {components}");
                    }
                } else {
                    Plugin.Debug($"Compatibility warning: Prefab '{name}' not found in ZNetScene.");
                }
            }
        }
    }
}
