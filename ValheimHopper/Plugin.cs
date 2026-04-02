using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Utils;
using Jotunn.Managers;
using ValheimHopper.Logic.Helper;
using ValheimHopper.UI;
using ValheimHopper.Logic;

namespace ValheimHopper {
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("com.maxsch.valheim.MultiUserChest")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    public class Plugin : BaseUnityPlugin {
        [PublicAPI] public const string ModName = "ValheimPipes";
        [PublicAPI] public const string ModGuid = "com.faryzal2020.valheim.ValheimPipes";
        [PublicAPI] public const string ModVersion = "1.0.0";

        private static ConfigEntry<bool> addSmelterSnappoints;
        private static ConfigEntry<bool> debugLogs;
        public static ConfigEntry<float> BronzeTransferRate;
        public static ConfigEntry<float> IronTransferRate;
        public static ConfigEntry<bool> ShowHopperInputBox;
        public static ConfigEntry<bool> ShowHopperOutputBox;
        public static ConfigEntry<bool> ShowPipeOutputBox;
        public static ConfigEntry<bool> ShowSnappointHighlights;
        public static ConfigEntry<string> ExtraCompatiblePrefabs;




        public static Plugin Instance { get; private set; }
        public static AssetBundle AssetBundle { get; private set; }

        private Harmony harmony;

        private void Awake() {
            Instance = this;

            harmony = new Harmony(ModGuid);
            harmony.PatchAll();

            ConfigurationManagerAttributes syncedAttr = new ConfigurationManagerAttributes { IsAdminOnly = true };

            BronzeTransferRate = Config.Bind("General", "Bronze Transfer Rate", 60f,
                new ConfigDescription("Items per minute for bronze hoppers and pipes.", null, syncedAttr));

            IronTransferRate = Config.Bind("General", "Iron Transfer Rate", 120f,
                new ConfigDescription("Items per minute for iron hoppers.", null, syncedAttr));

            ExtraCompatiblePrefabs = Config.Bind("Compatibility", "Extra Compatible Prefabs", "BCP_ClayCollector,RDP_beehive",
                new ConfigDescription("A comma-separated list of prefab names to treat as compatible (containers, beehives, smelters, etc.).", null, syncedAttr));

            addSmelterSnappoints = Config.Bind("General", "Add Smelter Snappoints", true, "Adds snappoints to inputs/outputs of the smelter, charcoal kiln, blastfurnace, windmill and spinning wheel. Requires a restart to take effect.");
            debugLogs = Config.Bind("General", "Debug Logs", false, "Enable debug logging.");

            BronzeTransferRate = Config.Bind("General", "Bronze Transfer Rate", 60f, "Items per minute for bronze hoppers and pipes.");
            IronTransferRate = Config.Bind("General", "Iron Transfer Rate", 120f, "Items per minute for iron hoppers.");

            ShowHopperInputBox = Config.Bind("Debug", "Show Hopper Input Box", false, "Show the hopper input bounding box in-game.");
            ShowHopperOutputBox = Config.Bind("Debug", "Show Hopper Output Box", false, "Show the hopper output bounding box in-game.");
            ShowPipeOutputBox = Config.Bind("Debug", "Show Pipe Output Box", false, "Show the pipe output bounding box in-game.");
            ShowSnappointHighlights = Config.Bind("Debug", "Show Snappoint Highlights", false, "Show a visual marker for custom snappoints added by the mod. Requires local restart of the area (teleport or logout) to take effect.");

            SynchronizationManager.OnConfigurationSynchronized += (obj, attr) => {
                if (!attr.InitialSynchronization) {
                    Jotunn.Logger.LogMessage("Server synced new config values.");
                    // Re-apply transfer rates if needed
                }
            };

            CustomLocalization localization = LocalizationManager.Instance.GetLocalization();
            localization.AddJsonFile("English", AssetUtils.LoadTextFromResources("Localization.English.json"));
            localization.AddJsonFile("German", AssetUtils.LoadTextFromResources("Localization.German.json"));
            localization.AddJsonFile("Russian", AssetUtils.LoadTextFromResources("Localization.Russian.json"));
            localization.AddJsonFile("Portuguese_Brazilian", AssetUtils.LoadTextFromResources("Localization.Portuguese_Brazilian.json"));

            AssetBundle = AssetUtils.LoadAssetBundleFromResources("ValheimHopper_AssetBundle");

            AddBronzePiece("HopperBronzeDown", 6, 4);
            AddBronzePiece("HopperBronzeSide", 6, 4);
            AddBronzePiece("MS_PipeBronzeSide", 4, 2);
            AddBronzePiece("MS_PipeBronzeSide_2m", 2, 1);
            AddBronzePiece("MS_PipeBronze_Vertical_Up_4m", 4, 2);
            AddBronzePiece("MS_PipeBronze_Vertical_Down_4m", 4, 2);
            AddBronzePiece("MS_PipeBronze_Vertical_Up_2m", 2, 1);
            AddBronzePiece("MS_PipeBronze_Vertical_Down_2m", 2, 1);
            AddBronzePiece("MS_PipeBronze_Diagonal_45_Up_4m", 4, 2);
            AddBronzePiece("MS_PipeBronze_Diagonal_45_Down_4m", 4, 2);
            AddBronzePiece("MS_PipeBronze_Diagonal_26_Up_4m", 4, 2);
            AddBronzePiece("MS_PipeBronze_Diagonal_26_Down_4m", 4, 2);
            AddIronPiece("HopperIronDown", 6, 2);
            AddIronPiece("HopperIronSide", 6, 2);

            PrefabManager.OnVanillaPrefabsAvailable += AddSnappoints;
            GUIManager.OnCustomGUIAvailable += HopperUI.Init;
        }

        private static void AddSnappoints() {
            if (addSmelterSnappoints.Value) {
                SnappointHelper.AddSnappoints("smelter", new[] {
                    new Vector3(0f, 1.6f, -1.2f),
                    new Vector3(0f, 1.6f, 1.2f),
                });

                SnappointHelper.AddSnappoints("charcoal_kiln", new[] {
                    new Vector3(0f, 1.1f, 2f),
                });

                SnappointHelper.AddSnappoints("blastfurnace", new[] {
                    new Vector3(-0.5f, 1.72001f, 1.55f),
                    new Vector3(-0.6f, 1.72001f, 1.55f),
                    new Vector3(0.57f, 1.72f, 1.55001f),
                    new Vector3(0.73f, 1.72f, 1.55001f),
                });

                SnappointHelper.AddSnappoints("windmill", new[] {
                    new Vector3(0f, 1.55f, -1.55f),
                    new Vector3(-0.05f, 0.83f, 2.3f),
                });
                SnappointHelper.FixPiece("windmill");

                SnappointHelper.AddSnappoints("piece_oven", new[] {
                    new Vector3(0f, 1.1f, 2f), // Food
                    new Vector3(0f, 0.72f, 0.55f), // Fuel
                });

                SnappointHelper.AddSnappoints("piece_spinningwheel", new[] {
                    new Vector3(0.72f, 1.8f, 0f),
                    new Vector3(0f, 0.95f, 1.75f),
                });
            }

            PrefabManager.OnVanillaPrefabsAvailable -= AddSnappoints;
        }

        private static void AddBronzePiece(string assetName, int wood, int nails) {
            PieceConfig config = new PieceConfig {
                Requirements = new[] {
                    new RequirementConfig("Wood", wood, 0, true),
                    new RequirementConfig("BronzeNails", nails, 0, true)
                },
                PieceTable = "Hammer",
                CraftingStation = "piece_workbench",
                Category = "Crafting",
            };


            CustomPiece customPiece = new CustomPiece(AssetBundle, assetName, true, config);
            EnsureCorrectComponent(customPiece);
            PieceManager.Instance.AddPiece(customPiece);
        }

        private static void EnsureCorrectComponent(CustomPiece customPiece) {
            GameObject prefab = customPiece.PiecePrefab;
            if (prefab.name.Contains("Pipe")) {
                Hopper hopper = prefab.GetComponent<Hopper>();
                if (hopper != null) {
                    // Copy fields before removing
                    FieldInfo outPosField = typeof(Hopper).GetField("outPos", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo outSizeField = typeof(Hopper).GetField("outSize", BindingFlags.NonPublic | BindingFlags.Instance);

                    Vector3 outPos = (Vector3)outPosField.GetValue(hopper);
                    Vector3 outSize = (Vector3)outSizeField.GetValue(hopper);

                    DestroyImmediate(hopper);
                    Pipe pipe = prefab.AddComponent<Pipe>();

                    // Set Pipe fields
                    FieldInfo pipeOutPosField = typeof(Pipe).GetField("outPos", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo pipeOutSizeField = typeof(Pipe).GetField("outSize", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (pipeOutPosField != null) pipeOutPosField.SetValue(pipe, outPos);
                    if (pipeOutSizeField != null) pipeOutSizeField.SetValue(pipe, outSize);
                }
            } else if (prefab.name.Contains("Hopper") && prefab.GetComponent<Hopper>() == null) {
                prefab.AddComponent<Hopper>();
                if (prefab.GetComponent<Pipe>() != null) {
                    DestroyImmediate(prefab.GetComponent<Pipe>());
                }
            }
        }

        private static void AddIronPiece(string assetName, int wood, int nails) {
            PieceConfig config = new PieceConfig {
                Requirements = new[] {
                    new RequirementConfig("Wood", wood, 0, true),
                    new RequirementConfig("IronNails", nails, 0, true)
                },
                PieceTable = "Hammer",
                CraftingStation = "piece_workbench",
                Category = "Crafting",
            };


            CustomPiece customPiece = new CustomPiece(AssetBundle, assetName, true, config);
            EnsureCorrectComponent(customPiece);
            PieceManager.Instance.AddPiece(customPiece);
        }

        public static void Debug(object data) {
            if (debugLogs != null && debugLogs.Value) {
                Jotunn.Logger.LogInfo(data);
            }
        }
    }
}
