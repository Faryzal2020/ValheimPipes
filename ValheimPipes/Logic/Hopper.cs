using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MultiUserChest;
using ValheimPipes.Logic.Helper;
using Random = UnityEngine.Random;

namespace ValheimPipes.Logic {
    [DefaultExecutionOrder(5)]
    public class Hopper : NetworkPiece, IPushTarget, IPullTarget {
        public Piece Piece { get; private set; }
        private Container container;
        private ContainerTarget containerTarget;

        public HopperPriority PushPriority { get; } = HopperPriority.HopperPush;
        public HopperPriority PullPriority { get; } = HopperPriority.HopperPull;
        public bool IsPickup { get; } = false;

        public Vector3 InPos => inPos;
        public Vector3 InSize => inSize;

        [SerializeField] private Vector3 inPos = new Vector3(0, 0.25f * 1.5f, 0);
        [SerializeField] private Vector3 outPos = new Vector3(0, -0.25f * 1.5f, 0);
        [SerializeField] private Vector3 inSize = new Vector3(1f, 0.5f, 1f);
        [SerializeField] private Vector3 outSize = new Vector3(1f, 1f, 1f);

        private List<IPushTarget> pushTo = new List<IPushTarget>();
        internal List<IPullTarget> pullFrom = new List<IPullTarget>();
        private BoxVisualizer inVisualizer;
        private BoxVisualizer outVisualizer;
        private int lastPullFrame = -1;

        private const float ObjectSearchInterval = 3f;

        private int transferFrame;
        private int objectSearchFrame;
        private int frameOffset;

        private int pullCounter;

        public ItemFilter filter;

        public ZBool FilterItemsOption { get; private set; }
        public ZBool BlacklistModeOption { get; private set; }
        public ZBool StackModeOption { get; private set; }
        public ZBool DropItemsOption { get; private set; }
        public ZBool PickupItemsOption { get; private set; }
        public ZBool LeaveLastItemOption { get; private set; }

        private string DbgId => $"Hopper@{transform.position.ToString("F1")}";

        protected override void Awake() {
            base.Awake();

            Piece = GetComponent<Piece>();
            container = GetComponent<Container>();
            containerTarget = GetComponent<ContainerTarget>();

            FilterItemsOption = new ZBool("hopper_filter_items", false, zNetView);
            BlacklistModeOption = new ZBool("hopper_blacklist_mode", false, zNetView);
            StackModeOption = new ZBool("hopper_stack_mode", false, zNetView);
            DropItemsOption = new ZBool("hopper_drop_items", false, zNetView);
            PickupItemsOption = new ZBool("hopper_pickup_items", true, zNetView);
            LeaveLastItemOption = new ZBool("hopper_leave_last_item", false, zNetView);

            UpdateTransferRate();
            objectSearchFrame = Mathf.RoundToInt((1f / Time.fixedDeltaTime) * ObjectSearchInterval);
            frameOffset = Mathf.Abs(GetInstanceID() % transferFrame);

            inVisualizer = gameObject.AddComponent<BoxVisualizer>();
            inVisualizer.SetData(inPos, inSize, Color.blue, Plugin.ShowHopperInputBox);

            outVisualizer = gameObject.AddComponent<BoxVisualizer>();
            outVisualizer.SetData(outPos, outSize, Color.yellow, Plugin.ShowHopperOutputBox);
        }

        private void UpdateTransferRate() {
            float itemsPerMinute = name.Contains("Iron") ? Plugin.IronTransferRate.Value : Plugin.BronzeTransferRate.Value;
            float interval = 30f / Mathf.Max(1f, itemsPerMinute); // 30 because it pushes every 2 * transferFrame
            transferFrame = Mathf.RoundToInt(interval / Time.fixedDeltaTime);
            if (transferFrame < 1) transferFrame = 1;
        }

        private void Start() {
            filter = new ItemFilter(zNetView, container.GetInventory());
            container.GetInventory().m_onChanged += OnInventoryChanged;
            HopperHelper.OnTargetChanged += OnTargetChanged;
        }

        private void OnDestroy() {
            HopperHelper.OnTargetChanged -= OnTargetChanged;
        }

        private void OnInventoryChanged() {
            WakeUp();
            if (IsValid() && FilterItemsOption.Get()) {
                filter.Save();
            }
        }

        private void OnTargetChanged(ITarget target) {
            if (IsPushBlocked && pushTo.Any(t => t.NetworkHashCode() == target.NetworkHashCode())) {
                IsPushBlocked = false;
            }

            if (IsPullBlocked && pullFrom.Any(t => t.NetworkHashCode() == target.NetworkHashCode())) {
                IsPullBlocked = false;
            }
        }

        public void PasteData(Hopper copy) {
            FilterItemsOption.Set(copy.FilterItemsOption.Get());
            BlacklistModeOption.Set(copy.BlacklistModeOption.Get());
            StackModeOption.Set(copy.StackModeOption.Get());
            DropItemsOption.Set(copy.DropItemsOption.Get());
            PickupItemsOption.Set(copy.PickupItemsOption.Get());
            LeaveLastItemOption.Set(copy.LeaveLastItemOption.Get());
            filter.Copy(copy.filter);
        }

        public void ResetValues() {
            FilterItemsOption.Reset();
            BlacklistModeOption.Reset();
            StackModeOption.Reset();
            DropItemsOption.Reset();
            PickupItemsOption.Reset();
            LeaveLastItemOption.Reset();
            filter.Clear();
        }

        private void FixedUpdate() {
            if (!IsValid() || !zNetView.IsOwner() || Plugin.DisableAllSystems.Value) {
                return;
            }

            int frame = HopperHelper.GetFixedFrameCount();
            int globalFrame = (frame + frameOffset) / transferFrame;

            if ((frame + frameOffset) % transferFrame == 0) {
                if (globalFrame % 2 == 0 && !IsPullBlocked) {
                    PullItems();
                }

                if (globalFrame % 2 == 1 && !IsPushBlocked) {
                    PushItems();
                }
            }

            if ((frame + frameOffset + 1) % objectSearchFrame == 0) {
                FindIO();
            }
        }

        private void PullItems() {
            if (pullFrom.Count == 0) {
                IsPullBlocked = true;
                return;
            }

            int idx = pullCounter % pullFrom.Count;
            IPullTarget from = pullFrom[idx];
            pullCounter++;

            if (!from.IsValid()) {
                Plugin.Debug($"[{DbgId}] Pull source [{idx}] invalid, skipping");
                return;
            }

            if (!PickupItemsOption.Get() && from.IsPickup) {
                return;
            }

            foreach (ItemDrop.ItemData item in from.GetItems()) {
                if (!FindFreeSlot(item, out Vector2i pos)) {
                    Plugin.Debug($"[{DbgId}] No free slot for '{item.m_shared.m_name}' from source [{idx}]");
                    IsPullBlocked = true;
                    continue;
                }

                int amount = 1;
                if (StackModeOption.Get() && name.Contains("Iron")) {
                    int stackSize = item.m_shared.m_maxStackSize;
                    int currentInSource = item.m_stack;
                    int roomInHopper = container.GetInventory().GetRoomForItem(item);
                    amount = Mathf.Min(stackSize, currentInSource, roomInHopper);
                }

                Plugin.Debug($"[{DbgId}] Pulling {amount}x '{item.m_shared.m_name}' <- {from.GetType().Name} ({(from as MonoBehaviour)?.gameObject.name}) (counter={pullCounter})");
                from.RemoveItem(item, container.GetInventory(), pos, zNetView.m_zdo.m_uid, amount);
                HopperHelper.NotifyChange(this);
                HopperHelper.NotifyChange(from);
                return;
            }

            IsPullBlocked = true;
        }

        private void PushItems() {
            if (pushTo.Count == 0) {
                if (DropItemsOption.Get()) {
                    DropItem();
                } else {
                    IsPushBlocked = true;
                }
                return;
            }

            int idx = OutputCounter % pushTo.Count;
            IPushTarget to = pushTo[idx];
            OutputCounter++;

            if (!to.IsValid()) {
                Plugin.Debug($"[{DbgId}] Push target [{idx}] invalid, skipping");
                return;
            }

            ItemDrop.ItemData item = container.GetInventory().FindLastItem(i => to.CanAddItem(i) && CanPushItem(i));

            if (item != null) {
                int amount = 1;
                if (StackModeOption.Get() && name.Contains("Iron")) {
                    // Calculate how many of this item type can be pushed
                    int stackSize = item.m_shared.m_maxStackSize;
                    int countInHopper = container.GetInventory().CountItems(item.m_shared.m_name);
                    
                    // If LeaveLastItem is on, reserve 1
                    if (LeaveLastItemOption.Get()) countInHopper--;
                    
                    amount = Mathf.Min(stackSize, countInHopper);
                }

                Plugin.Debug($"[{DbgId}] Pushing {amount}x '{item.m_shared.m_name}' -> {to.GetType().Name} ({(to as MonoBehaviour)?.gameObject.name}) (counter={OutputCounter})");
                to.AddItem(item, container.GetInventory(), zNetView.m_zdo.m_uid, amount);
                HopperHelper.NotifyChange(this);
                HopperHelper.NotifyChange(to);
            } else {
                Plugin.Debug($"[{DbgId}] No pushable item for target [{idx}] ({to.GetType().Name}) (full, filtered, or leave-last)");
                IsPushBlocked = true;
            }
        }

        private void DropItem() {
            ItemDrop.ItemData firstItem = container.GetInventory().FindLastItem(CanPushItem);

            if (firstItem != null) {
                container.GetInventory().RemoveOneItem(firstItem);
                float angle = Random.Range(0f, (float)(2f * Math.PI));
                Vector3 randomPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.2f;
                Vector3 visualOffset = ItemHelper.GetVisualItemOffset(firstItem.m_dropPrefab.name);
                Vector3 pos = transform.TransformPoint(outPos) + visualOffset + new Vector3(randomPos.x, 0, randomPos.z);
                ItemDrop.DropItem(firstItem, 1, pos, firstItem.m_dropPrefab.transform.rotation);
            }
        }

        public bool CanAddItem(ItemDrop.ItemData item) {
            return FindFreeSlot(item, out _);
        }

        public void AddItem(ItemDrop.ItemData item, Inventory source, ZDOID sender, int amount = 1) {
            FindFreeSlot(item, out Vector2i pos);
            container.AddItemToChest(item, source, pos, sender, amount);
        }

        public IEnumerable<ItemDrop.ItemData> GetItems() {
            int frame = HopperHelper.GetFixedFrameCount();
            if (frame == lastPullFrame) {
                return Enumerable.Empty<ItemDrop.ItemData>();
            }
            return containerTarget.GetItems();
        }

        public void RemoveItem(ItemDrop.ItemData item, Inventory destination, Vector2i destinationPos, ZDOID sender, int amount = 1) {
            Plugin.Debug($"[{DbgId}] RemoveItem '{item.m_shared.m_name}' pulled by upstream");
            lastPullFrame = HopperHelper.GetFixedFrameCount();
            containerTarget.RemoveItem(item, destination, destinationPos, sender, amount);
        }

        private bool FindFreeSlot(ItemDrop.ItemData itemToAdd, out Vector2i pos) {
            pos = new Vector2i(0, 0);

            if (!container.GetInventory().CanAddItem(itemToAdd, 1)) {
                return false;
            }

            int itemHash = itemToAdd.m_dropPrefab.name.GetStableHashCode();

            for (int y = 0; y < container.m_height; y++) {
                for (int x = 0; x < container.m_width; x++) {
                    ItemDrop.ItemData item = container.GetInventory().GetItemAt(x, y);
                    bool canAdd = item == null ||
                                  item.m_stack + 1 <= item.m_shared.m_maxStackSize && item.m_shared.m_name == itemToAdd.m_shared.m_name;

                    if (!canAdd) {
                        continue;
                    }

                    if (FilterItemsOption.Get()) {
                        if (BlacklistModeOption.Get()) {
                            // In Blacklist mode, if the item is in ANY filter slot, it's rejected
                            if (filter.Contains(itemHash)) {
                                return false;
                            }
                            // Otherwise, it can go anywhere with room
                            pos = new Vector2i(x, y);
                            return true;
                        } else {
                            // Whitelist mode: per-slot filter
                            int filterHash = filter.GetItemHash(x, y);
                            bool isFiltered = filterHash == 0 || filterHash == itemHash;

                            if (isFiltered) {
                                pos = new Vector2i(x, y);
                                return true;
                            }
                        }
                    } else {
                        pos = new Vector2i(x, y);
                        return true;
                    }
                }
            }

            return false;
        }

        public bool InRange(Vector3 position) {
            return true;
        }

        private bool CanPushItem(ItemDrop.ItemData item) {
            return (!LeaveLastItemOption.Get() || container.GetInventory().CountItems(item.m_shared.m_name) > 1);
        }

        private void FindIO() {
            Quaternion rotation = transform.rotation;
            pullFrom = HopperHelper.FindTargets<IPullTarget>(transform.TransformPoint(inPos), inSize, rotation, i => i.PullPriority, this);
            int originalPullCount = pullFrom.Count;
            pullFrom.RemoveAll(i => i is Pipe);
            if (pullFrom.Count < originalPullCount) {
                Plugin.Debug($"[{DbgId}] Ignored {originalPullCount - pullFrom.Count} Upstream Pipes (Hoppers cannot pull from Pipes)");
            }
            
            pushTo = HopperHelper.FindTargets<IPushTarget>(transform.TransformPoint(outPos), outSize, rotation, i => i.PushPriority, this);
            
            Plugin.Debug($"[{DbgId}] Targets: Pull[{pullFrom.Count}] {string.Join(", ", pullFrom.Select(t => (t as MonoBehaviour)?.gameObject.name ?? t.GetType().Name))}");
            Plugin.Debug($"[{DbgId}] Targets: Push[{pushTo.Count}] {string.Join(", ", pushTo.Select(t => (t as MonoBehaviour)?.gameObject.name ?? t.GetType().Name))}");
            pullFrom.RemoveAll(pull => pushTo.Exists(push => push.NetworkHashCode() == pull.NetworkHashCode()));

            WakeUp();
        }

        private void OnDrawGizmos() {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.TransformPoint(inPos), inSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.TransformPoint(outPos), outSize);

            Gizmos.color = Color.cyan;
            foreach (Transform child in transform) {
                if (child.CompareTag("snappoint")) {
                    Gizmos.DrawSphere(child.position, .05f);
                }
            }
        }
    }
}
