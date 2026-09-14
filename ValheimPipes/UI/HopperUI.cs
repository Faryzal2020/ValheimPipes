using System;
using System.Linq;
using Jotunn;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ValheimPipes;
using ValheimPipes.Logic;

// NOTE: Namespace is ValheimHopper.UI for compatibility with the AssetBundle,
// which expects this specific namespace for script references on UI prefabs.
namespace ValheimPipes.UI {
    public class FilterSlotClickHandler : MonoBehaviour, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler {
        public int SlotIndex;
        public Action<int, PointerEventData.InputButton> OnClick;
        public Image BackgroundImage;
        private Color defaultColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color HoverColor = new Color(0.25f, 0.25f, 0.25f, 0.7f);

        public void SetDefaultColor(Color color) {
            defaultColor = color;
            if (BackgroundImage != null) BackgroundImage.color = defaultColor;
        }

        public void OnPointerClick(PointerEventData eventData) {
            OnClick?.Invoke(SlotIndex, eventData.button);
        }

        public void OnDrop(PointerEventData eventData) {
            OnClick?.Invoke(SlotIndex, eventData.button);
        }

        public void OnPointerEnter(PointerEventData eventData) {
            if (BackgroundImage != null) {
                BackgroundImage.color = HoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (BackgroundImage != null) {
                BackgroundImage.color = defaultColor;
            }
        }
    }

    public class HopperUI : MonoBehaviour {
        public static HopperUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }
        private static readonly Color WhiteShade = new Color(219f / 255f, 219f / 255f, 219f / 255f);

        private class FilterSlotUI {
            public int Index;
            public GameObject SlotObject;
            public Image BackgroundImage;
            public Image IconImage;
            public UITooltip Tooltip;
        }

        private readonly FilterSlotUI[] filterSlots = new FilterSlotUI[3];

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

        private static GameObject uiInstance;
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
                    UpdateText();
                }
            });
            if (resetButton != null) resetButton.onClick.AddListener(() => {
                if (target != null) {
                    target.ResetValues();
                    UpdateText();
                }
            });
        }

        public static void Init() {
            if (Instance != null) return;
 
            Transform parent = (InventoryGui.instance != null) ? InventoryGui.instance.transform : GUIManager.CustomGUIFront?.transform;
            if (parent == null) {
                Jotunn.Logger.LogWarning("HopperUI: Cannot init because parent canvas is null. Waiting...");
                return;
            }
 
            GameObject prefab = Plugin.AssetBundle.LoadAsset<GameObject>("HopperUI");
            if (prefab == null) {
                Jotunn.Logger.LogError("HopperUI: Failed to load prefab from AssetBundle!");
                return;
            }

            // CLEANUP: UI prefabs should not have ZNetView or Container components.
            // Remove them from the prefab before instantiating to prevent other mods
            // (such as AzuCraftyBoxes or QuickStack) from hooking into them or Valheim complaining.
            foreach (var nv in prefab.GetComponentsInChildren<ZNetView>(true)) {
                DestroyImmediate(nv, true);
            }
            foreach (var c in prefab.GetComponentsInChildren<Container>(true)) {
                DestroyImmediate(c, true);
            }
 
            uiInstance = Instantiate(prefab, parent, false);
            
            // Robustly find the UI root (usually the first child)
            uiRoot = uiInstance.transform.childCount > 0 ? uiInstance.transform.GetChild(0).gameObject : uiInstance;
            
            HopperUI ui = uiInstance.GetComponent<HopperUI>();
            if (ui == null) {
                Jotunn.Logger.LogWarning("HopperUI component missing on prefab!");
                return;
            }

            ApplyAllComponents(uiRoot);
            if (ui.title != null) {
                GUIManager.Instance.ApplyTextStyle(ui.title, GUIManager.Instance.AveriaSerifBold, GUIManager.Instance.ValheimOrange, 20);
            }
            ApplyLocalization();

            if (uiRoot.GetComponent<DragWindowCntrl>() == null) {
                uiRoot.AddComponent<DragWindowCntrl>();
            }

            ui.SetupFilterSlots();

            uiRoot.FixReferences(true);
            IsOpen = false;
            uiRoot.SetActive(false);
            uiInstance.SetActive(false);
 
            Jotunn.Logger.LogInfo("HopperUI: Successfully initialized custom UI instance.");
        }

        private void OnDestroy() {
            if (Instance == this) {
                Instance = null;
                uiInstance = null;
                uiRoot = null;
                IsOpen = false;
            }
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
            target.LoadFilterInventory();
            

            
            SetGUIState(true);
            UpdateText();
        }
 
        private void LateUpdate() { }

        private static void SetGUIState(bool active) {
            if (IsOpen == active) {
                return;
            }

            IsOpen = active;
            if (uiInstance != null) {
                uiInstance.SetActive(active);
                if (active) {
                    uiInstance.transform.SetAsLastSibling();
                }
            }
            if (uiRoot != null) uiRoot.SetActive(active);
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

            UpdateFilterSlots();
        }

        private static Sprite FindSlotBackgroundSprite() {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null) return null;

            if (gui.m_containerGrid != null) {
                if (gui.m_containerGrid.m_elementPrefab != null) {
                    var img = gui.m_containerGrid.m_elementPrefab.GetComponent<Image>() ?? gui.m_containerGrid.m_elementPrefab.GetComponentInChildren<Image>();
                    if (img != null && img.sprite != null) return img.sprite;
                }
                if (gui.m_containerGrid.m_elements != null && gui.m_containerGrid.m_elements.Count > 0) {
                    var elem = gui.m_containerGrid.m_elements[0];
                    var img = elem.GetComponent<Image>();
                    if (img != null && img.sprite != null) return img.sprite;
                }
            }
            if (gui.m_playerGrid != null) {
                if (gui.m_playerGrid.m_elementPrefab != null) {
                    var img = gui.m_playerGrid.m_elementPrefab.GetComponent<Image>() ?? gui.m_playerGrid.m_elementPrefab.GetComponentInChildren<Image>();
                    if (img != null && img.sprite != null) return img.sprite;
                }
                if (gui.m_playerGrid.m_elements != null && gui.m_playerGrid.m_elements.Count > 0) {
                    var elem = gui.m_playerGrid.m_elements[0];
                    var img = elem.GetComponent<Image>();
                    if (img != null && img.sprite != null) return img.sprite;
                }
            }
            return null;
        }

        private static GameObject FindTooltipPrefab() {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null) return null;

            var tt = gui.GetComponentsInChildren<UITooltip>(true).FirstOrDefault(t => t.m_tooltipPrefab != null);
            return tt != null ? tt.m_tooltipPrefab : null;
        }

        private void SetupFilterSlots() {
            if (uiRoot == null) return;

            Sprite slotBkgSprite = FindSlotBackgroundSprite();
            GameObject tooltipPrefab = FindTooltipPrefab();
            Color defaultColor = new Color(0f, 0f, 0f, 0.45f);

            for (int i = 0; i < 3; i++) {
                string slotName = $"Slot_{i}";
                Transform slotTr = uiRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == slotName);
                if (slotTr == null) {
                    Jotunn.Logger.LogWarning($"HopperUI: Could not find {slotName} in prefab!");
                    continue;
                }

                GameObject slotGo = slotTr.gameObject;

                // Remove any Button component immediately so Unity UI Selectable doesn't overwrite background color
                Button existingBtn = slotGo.GetComponent<Button>();
                if (existingBtn != null) DestroyImmediate(existingBtn);

                Image bgImg = slotGo.GetComponent<Image>();
                if (bgImg == null) bgImg = slotGo.AddComponent<Image>();

                if (slotBkgSprite != null) {
                    bgImg.sprite = slotBkgSprite;
                    bgImg.type = Image.Type.Sliced;
                } else {
                    bgImg.sprite = null;
                }
                bgImg.color = defaultColor;

                // Find or create _icon child
                Transform iconTr = slotGo.transform.Find("_icon");
                if (iconTr == null) {
                    GameObject iconGo = new GameObject("_icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    iconGo.transform.SetParent(slotGo.transform, false);
                    iconTr = iconGo.transform;
                }
                Image iconImg = iconTr.GetComponent<Image>();
                if (iconImg == null) iconImg = iconTr.gameObject.AddComponent<Image>();

                RectTransform iconRect = iconImg.rectTransform;
                iconRect.anchorMin = new Vector2(0.1f, 0.1f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false; // clicks pass through to slot
                iconImg.color = Color.white;
                iconImg.enabled = false;

                // UITooltip
                UITooltip tooltip = slotGo.GetComponent<UITooltip>();
                if (tooltip == null) tooltip = slotGo.AddComponent<UITooltip>();
                if (tooltipPrefab != null) tooltip.m_tooltipPrefab = tooltipPrefab;
                tooltip.enabled = false;

                // Click / Drop / Hover handler
                int slotIdx = i;
                FilterSlotClickHandler clickHandler = slotGo.GetComponent<FilterSlotClickHandler>();
                if (clickHandler == null) clickHandler = slotGo.AddComponent<FilterSlotClickHandler>();
                clickHandler.SlotIndex = slotIdx;
                clickHandler.OnClick = OnSlotClicked;
                clickHandler.BackgroundImage = bgImg;
                clickHandler.SetDefaultColor(defaultColor);

                filterSlots[i] = new FilterSlotUI {
                    Index = slotIdx,
                    SlotObject = slotGo,
                    BackgroundImage = bgImg,
                    IconImage = iconImg,
                    Tooltip = tooltip
                };
            }
        }

        private void OnSlotClicked(int slotIndex, PointerEventData.InputButton button) {
            if (target == null) return;

            if (button == PointerEventData.InputButton.Right) {
                UITooltip.HideTooltip();
                target.ClearFilterItem(slotIndex);
                UpdateFilterSlots();
                return;
            }

            if (button == PointerEventData.InputButton.Left) {
                InventoryGui gui = InventoryGui.instance;
                if (gui != null && gui.m_dragItem != null) {
                    target.SetFilterItem(slotIndex, gui.m_dragItem);
                } else {
                    UITooltip.HideTooltip();
                    target.ClearFilterItem(slotIndex);
                }
                UpdateFilterSlots();
            }
        }

        private void UpdateFilterSlots() {
            if (target == null) return;

            Sprite slotBkgSprite = FindSlotBackgroundSprite();
            Color defaultColor = new Color(0f, 0f, 0f, 0.45f);

            for (int i = 0; i < filterSlots.Length; i++) {
                FilterSlotUI slot = filterSlots[i];
                if (slot == null) continue;

                if (slot.BackgroundImage != null) {
                    var handler = slot.SlotObject?.GetComponent<FilterSlotClickHandler>();
                    if (slot.BackgroundImage.sprite == null && slotBkgSprite != null) {
                        slot.BackgroundImage.sprite = slotBkgSprite;
                        slot.BackgroundImage.type = Image.Type.Sliced;
                    }
                    slot.BackgroundImage.color = defaultColor;
                    handler?.SetDefaultColor(defaultColor);
                }

                ItemDrop.ItemData item = target.GetFilterItem(i);
                if (item != null) {
                    slot.IconImage.sprite = item.GetIcon();
                    slot.IconImage.color = Color.white;
                    slot.IconImage.enabled = true;
                    if (slot.Tooltip != null) {
                        if (slot.Tooltip.m_tooltipPrefab == null) {
                            slot.Tooltip.m_tooltipPrefab = FindTooltipPrefab();
                        }
                        slot.Tooltip.m_topic = "";
                        slot.Tooltip.m_text = item.m_shared != null ? Localization.instance.Localize(item.m_shared.m_name) : "";
                        slot.Tooltip.enabled = slot.Tooltip.m_tooltipPrefab != null;
                    }
                } else {
                    slot.IconImage.sprite = null;
                    slot.IconImage.enabled = false;
                    if (slot.Tooltip != null) {
                        UITooltip.HideTooltip();
                        slot.Tooltip.m_topic = "";
                        slot.Tooltip.m_text = "";
                        slot.Tooltip.enabled = false;
                    }
                }
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
