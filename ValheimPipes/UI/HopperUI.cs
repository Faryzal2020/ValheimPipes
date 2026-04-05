using Jotunn;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ValheimPipes;
using ValheimPipes.Logic;

// NOTE: Namespace is ValheimHopper.UI for compatibility with the AssetBundle,
// which expects this specific namespace for script references on UI prefabs.
namespace ValheimHopper.UI {
    public class HopperUI : MonoBehaviour {
        public static HopperUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }
        private static readonly Color WhiteShade = new Color(219f / 255f, 219f / 255f, 219f / 255f);

        // Disable Field XYZ is never assigned to, and will always have its default value XX
#pragma warning disable 0649
        [SerializeField] private Text title;
        [SerializeField] private Toggle filterHopper;
        [SerializeField] private Toggle dropItems;
        [SerializeField] private Toggle pickupItems;
        [SerializeField] private Toggle leaveLastItem;
        private Toggle blacklistMode;
        private Toggle stackMode;

        [SerializeField] private Button copyButton;
        [SerializeField] private Button pasteButton;
        [SerializeField] private Button resetButton;
#pragma warning restore 0649

        private static GameObject uiRoot;
        private Hopper target;
        private Hopper copy;

        private void Awake() {
            Instance = this;

            dropItems.onValueChanged.AddListener(i => { if (target != null) target.DropItemsOption.Set(i); });
            pickupItems.onValueChanged.AddListener(i => { if (target != null) target.PickupItemsOption.Set(i); });
            leaveLastItem.onValueChanged.AddListener(i => { if (target != null) target.LeaveLastItemOption.Set(i); });

            filterHopper.onValueChanged.AddListener(active => {
                if (target == null) return;
                target.FilterItemsOption.Set(active);

                if (active) {
                    target.filter.Save();
                } else {
                    target.filter.Clear();
                }
            });

            copyButton.onClick.AddListener(() => { if (target != null) copy = target; });
            pasteButton.onClick.AddListener(() => {
                if (target != null && copy != null && copy.IsValid()) {
                    target.PasteData(copy);
                }
            });
            resetButton.onClick.AddListener(() => { if (target != null) target.ResetValues(); });
        }

        private void SetupNewToggles() {
            blacklistMode.onValueChanged.AddListener(i => { if (target != null) target.BlacklistModeOption.Set(i); });
            stackMode.onValueChanged.AddListener(i => { if (target != null) target.StackModeOption.Set(i); });
        }

        public static void Init() {
            GameObject prefab = Plugin.AssetBundle.LoadAsset<GameObject>("HopperUI");
            GameObject obj = Instantiate(prefab, GUIManager.CustomGUIFront.transform, false);
            uiRoot = obj.transform.GetChild(0).gameObject;
            HopperUI ui = obj.GetComponent<HopperUI>();

            if (ui == null) {
                Jotunn.Logger.LogWarning("HopperUI component missing on prefab! If you renamed the namespace, you must restore it to ValheimHopper.UI for AssetBundle compatibility.");
                uiRoot.SetActive(false);
                return;
            }

            ApplyAllComponents(uiRoot);
            GUIManager.Instance.ApplyTextStyle(ui.title, GUIManager.Instance.AveriaSerifBold, GUIManager.Instance.ValheimOrange, 20);
            ApplyLocalization();

            // Create new toggles
            ui.blacklistMode = CreateToggle(ui.filterHopper, "BlacklistMode", "$hopper_options_blacklist", new Vector2(0, -110));
            ui.stackMode = CreateToggle(ui.filterHopper, "StackMode", "$hopper_options_stack", new Vector2(0, -140));
            ui.SetupNewToggles();

            uiRoot.AddComponent<DragWindowCntrl>();
            uiRoot.SetActive(false);
            uiRoot.FixReferences(true);
        }

        private void LateUpdate() {
            if (!Player.m_localPlayer) {
                target = null;
                SetGUIState(false);
                return;
            }

            InventoryGui gui = InventoryGui.instance;

            if (!gui || !gui.IsContainerOpen() || !gui.m_currentContainer) {
                target = null;
                SetGUIState(false);
                return;
            }

            if (gui.m_currentContainer.TryGetComponent(out Hopper hopper)) {
                target = hopper;
                SetGUIState(true);
                UpdateText();
            } else {
                target = null;
                SetGUIState(false);
            }
        }

        private static void SetGUIState(bool active) {
            if (IsOpen == active) {
                return;
            }

            IsOpen = active;
            uiRoot.SetActive(active);
        }

        private void UpdateText() {
            title.text = Localization.instance.Localize(target.Piece.m_name);
            filterHopper.SetIsOnWithoutNotify(target.FilterItemsOption.Get());
            dropItems.SetIsOnWithoutNotify(target.DropItemsOption.Get());
            pickupItems.SetIsOnWithoutNotify(target.PickupItemsOption.Get());
            leaveLastItem.SetIsOnWithoutNotify(target.LeaveLastItemOption.Get());
            blacklistMode.SetIsOnWithoutNotify(target.BlacklistModeOption.Get());
            stackMode.SetIsOnWithoutNotify(target.StackModeOption.Get());

            bool isIron = target.name.Contains("Iron");
            stackMode.gameObject.SetActive(isIron);
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

        private static Toggle CreateToggle(Toggle source, string name, string label, Vector2 offset) {
            Toggle toggle = Instantiate(source, source.transform.parent);
            toggle.name = name;
            toggle.transform.localPosition += (Vector3)offset;
            toggle.GetComponentInChildren<Text>().text = Localization.instance.Localize(label);
            GUIManager.Instance.ApplyToogleStyle(toggle);
            return toggle;
        }
    }
}
