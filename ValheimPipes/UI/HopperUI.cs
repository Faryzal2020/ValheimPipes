using Jotunn;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ValheimPipes;
using ValheimPipes.Logic;

// NOTE: Namespace is ValheimHopper.UI for compatibility with the AssetBundle,
// which expects this specific namespace for script references on UI prefabs.
namespace ValheimPipes.UI {
    public class HopperUI : MonoBehaviour {
        public static HopperUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }
        private static readonly Color WhiteShade = new Color(219f / 255f, 219f / 255f, 219f / 255f);

        // Disable Field XYZ is never assigned to, and will always have its default value XX
        [SerializeField] private Text title;
        [SerializeField] private Toggle filterHopper;
        [SerializeField] private Toggle dropItems;
        [SerializeField] private Toggle pickupItems;
        [SerializeField] private Toggle leaveLastItem;
        [SerializeField] private Toggle blacklistMode;
        [SerializeField] private Toggle stackMode;

        [SerializeField] private Button copyButton;
        [SerializeField] private Button pasteButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Container filterContainer;
        public Container FilterContainer => filterContainer;

        private static GameObject uiRoot;
        private Hopper target;
        private Hopper copy;

        private void Awake() {
            Instance = this;

            if (dropItems != null) dropItems.onValueChanged.AddListener(i => { if (target != null) target.DropItemsOption.Set(i); });
            if (pickupItems != null) pickupItems.onValueChanged.AddListener(i => { if (target != null) target.PickupItemsOption.Set(i); });
            if (leaveLastItem != null) leaveLastItem.onValueChanged.AddListener(i => { if (target != null) target.LeaveLastItemOption.Set(i); });
            if (blacklistMode != null) blacklistMode.onValueChanged.AddListener(active => {
                if (target == null) return;
                target.BlacklistModeOption.Set(active);

                if (active) {
                    // Rule: Blacklist ON -> Filter ON
                    if (!target.FilterItemsOption.Get()) {
                        target.FilterItemsOption.Set(true);
                        if (filterHopper != null) filterHopper.SetIsOnWithoutNotify(true);
                    }
                }
            });
            if (stackMode != null) stackMode.onValueChanged.AddListener(i => { if (target != null) target.StackModeOption.Set(i); });

            if (filterHopper != null) filterHopper.onValueChanged.AddListener(active => {
                if (target == null) return;
                target.FilterItemsOption.Set(active);

                if (!active) {
                    // Rule: Filter OFF -> Blacklist OFF
                    target.BlacklistModeOption.Set(false);
                    if (blacklistMode != null) blacklistMode.SetIsOnWithoutNotify(false);
                }
            });

            if (copyButton != null) copyButton.onClick.AddListener(() => { if (target != null) copy = target; });
            if (pasteButton != null) pasteButton.onClick.AddListener(() => {
                if (target != null && copy != null && copy.IsValid()) {
                    target.PasteData(copy);
                }
            });
            if (resetButton != null) resetButton.onClick.AddListener(() => { if (target != null) target.ResetValues(); });
        }

        public static void Init() {
            if (Instance != null) return;
 
            if (GUIManager.CustomGUIFront == null) {
                Jotunn.Logger.LogWarning("HopperUI: Cannot init because CustomGUIFront is null. Waiting...");
                return;
            }
 
            GameObject prefab = Plugin.AssetBundle.LoadAsset<GameObject>("HopperUI");
            if (prefab == null) {
                Jotunn.Logger.LogError("HopperUI: Failed to load prefab from AssetBundle!");
                return;
            }

            // CLEANUP: UI prefabs should not have ZNetView. Remove it from the prefab before instantiating
            // to prevent AzuDevMod or Valheim from complaining about unregistered views.
            foreach (var nv in prefab.GetComponentsInChildren<ZNetView>(true)) {
                DestroyImmediate(nv, true);
            }
 
            GameObject obj = Instantiate(prefab, GUIManager.CustomGUIFront.transform, false);
            obj.SetActive(false);
            
            // Robustly find the UI root (usually the first child)
            uiRoot = obj.transform.childCount > 0 ? obj.transform.GetChild(0).gameObject : obj;
            uiRoot.SetActive(false);
            
            HopperUI ui = obj.GetComponent<HopperUI>();
            if (ui == null) {
                Jotunn.Logger.LogWarning("HopperUI component missing on prefab!");
                return;
            }
 
            Jotunn.Logger.LogInfo("HopperUI: Successfully initialized custom UI instance.");
 
            uiRoot.FixReferences(true);
        }

        public static void UpdateStatic() {
            InventoryGui gui = InventoryGui.instance;
            if (!Player.m_localPlayer || !gui || !gui.IsContainerOpen() || !gui.m_currentContainer) {
                if (Instance != null && IsOpen) {
                    Instance.target = null;
                    SetGUIState(false);
                }
                return;
            }
 
            if (gui.m_currentContainer.TryGetComponent(out Hopper hopper)) {
                if (Instance == null) {
                    Init();
                }
                if (Instance != null) {
                    Instance.UpdateInstance(hopper);
                }
            } else if (Instance != null && IsOpen) {
                Instance.target = null;
                SetGUIState(false);
            }
        }
 
        public static bool IsFilterInventory(Inventory inventory) {
            if (Instance == null || Instance.filterContainer == null) return false;
            return Instance.filterContainer.GetInventory() == inventory;
        }

        private void UpdateInstance(Hopper hopper) {
            target = hopper;
            
            if (filterContainer == null) {
                // Fallback: Try to find it by component if the serialize field is not set
                filterContainer = GetComponentInChildren<Container>(true);
                
                if (filterContainer == null) {
                    Jotunn.Logger.LogWarning("HopperUI: filterContainer is null and could not be found in children! Please check the prefab assignment.");
                } else {
                    Jotunn.Logger.LogInfo("HopperUI: Successfully found filterContainer in children.");
                }
            }
            
            SetGUIState(true);
            UpdateText();
        }
 
        private void LateUpdate() { }

        private static void SetGUIState(bool active) {
            if (IsOpen == active) {
                return;
            }

            IsOpen = active;
            uiRoot.SetActive(active);
        }

        private void UpdateText() {
            if (target == null) return;
            
            if (title != null) title.text = Localization.instance.Localize(target.Piece.m_name);
            if (filterHopper != null) filterHopper.SetIsOnWithoutNotify(target.FilterItemsOption.Get());
            if (dropItems != null) dropItems.SetIsOnWithoutNotify(target.DropItemsOption.Get());
            if (pickupItems != null) pickupItems.SetIsOnWithoutNotify(target.PickupItemsOption.Get());
            if (leaveLastItem != null) leaveLastItem.SetIsOnWithoutNotify(target.LeaveLastItemOption.Get());
            if (blacklistMode != null) blacklistMode.SetIsOnWithoutNotify(target.BlacklistModeOption.Get());
            if (stackMode != null) stackMode.SetIsOnWithoutNotify(target.StackModeOption.Get());

            if (stackMode != null) {
                bool isIron = target.name.Contains("Iron");
                stackMode.gameObject.SetActive(isIron);
            }
        }

        private static void ApplyAllComponents(GameObject root) {
            foreach (Text text in root.GetComponentsInChildren<Text>()) {
                GUIManager.Instance.ApplyTextStyle(text, GUIManager.Instance.AveriaSerif, WhiteShade, 16, false);
            }

            foreach (InputField inputField in root.GetComponentsInChildren<InputField>()) {
                GUIManager.Instance.ApplyInputFieldStyle(inputField, 16);
            }

            foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>()) {
                GUIManager.Instance.ApplyToogleStyle(toggle);
            }

            foreach (Button button in root.GetComponentsInChildren<Button>()) {
                GUIManager.Instance.ApplyButtonStyle(button);
            }
        }

        private static void ApplyLocalization() {
            foreach (Text text in uiRoot.GetComponentsInChildren<Text>()) {
                text.text = Localization.instance.Localize(text.text);
            }
        }
    }
}
