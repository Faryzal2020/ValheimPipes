using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimHopper.Logic.Helper;

namespace ValheimHopper.Logic {
    public class Pipe : NetworkPiece, IPushTarget, IPullTarget {
        public HopperPriority PushPriority { get; } = HopperPriority.PipePush;
        public HopperPriority PullPriority { get; } = HopperPriority.PipePull;
        public bool IsPickup { get; } = false;

        [SerializeField] private Vector3 outPos = new Vector3(0, 0, -1f);
        [SerializeField] private Vector3 outSize = new Vector3(0.25f, 0.5f, 0.5f);

        private Container container;
        private ContainerTarget containerTarget;
        private List<IPushTarget> pushTo = new List<IPushTarget>();
        private BoxVisualizer outVisualizer;
        private int lastPullFrame = -1;

        private const float ObjectSearchInterval = 3f;

        private int transferFrame;
        private int objectSearchFrame;
        private int frameOffset;


        private string DbgId => $"Pipe@{transform.position.ToString("F1")}";

        protected override void Awake() {
            base.Awake();

            container = GetComponent<Container>();
            containerTarget = GetComponent<ContainerTarget>();

            UpdateTransferRate();
            objectSearchFrame = Mathf.RoundToInt((1f / Time.fixedDeltaTime) * ObjectSearchInterval);
            frameOffset = Mathf.Abs(GetInstanceID() % transferFrame);

            outVisualizer = gameObject.AddComponent<BoxVisualizer>();
            outVisualizer.SetData(outPos, outSize, Color.yellow, Plugin.ShowPipeOutputBox);
        }

        private void UpdateTransferRate() {
            float itemsPerMinute = name.Contains("Iron") ? Plugin.IronTransferRate.Value : Plugin.BronzeTransferRate.Value;
            float interval = 30f / Mathf.Max(1f, itemsPerMinute); // 30 because it pushes every 2 * transferFrame
            transferFrame = Mathf.RoundToInt(interval / Time.fixedDeltaTime);
            if (transferFrame < 1) transferFrame = 1;
        }

        private void FixedUpdate() {
            if (!IsValid() || !zNetView.IsOwner()) {
                return;
            }

            int frame = HopperHelper.GetFixedFrameCount();
            int globalFrame = (frame + frameOffset) / transferFrame;

            if ((frame + frameOffset) % transferFrame == 0) {
                if (globalFrame % 2 == 1) {
                    PushItems();
                }
            }

            if ((frame + frameOffset + 1) % objectSearchFrame == 0) {
                FindIO();
            }
        }

        public bool InRange(Vector3 position) {
            return true;
        }

        public bool CanAddItem(ItemDrop.ItemData item) {
            return containerTarget.CanAddItem(item);
        }

        public void AddItem(ItemDrop.ItemData item, Inventory source, ZDOID sender) {
            Plugin.Debug($"[{DbgId}] AddItem '{item?.m_shared?.m_name ?? "null"}' pushed by upstream");
            containerTarget.AddItem(item, source, sender);
        }

        public IEnumerable<ItemDrop.ItemData> GetItems() {
            int frame = HopperHelper.GetFixedFrameCount();
            if (frame == lastPullFrame) {
                return Enumerable.Empty<ItemDrop.ItemData>();
            }
            return containerTarget.GetItems();
        }

        public void RemoveItem(ItemDrop.ItemData item, Inventory destination, Vector2i destinationPos, ZDOID sender) {
            Plugin.Debug($"[{DbgId}] RemoveItem '{item?.m_shared?.m_name ?? "null"}' pulled by hopper");
            lastPullFrame = HopperHelper.GetFixedFrameCount();
            containerTarget.RemoveItem(item, destination, destinationPos, sender);
        }

        private void PushItems() {
            if (pushTo.Count == 0 || container.GetInventory().NrOfItems() == 0) {
                return;
            }

            int idx = OutputCounter % pushTo.Count;
            IPushTarget to = pushTo[idx];
            OutputCounter++;

            if (!to.IsValid()) {
                Plugin.Debug($"[{DbgId}] Push target [{idx}] invalid, skipping");
                return;
            }

            ItemDrop.ItemData item = container.GetInventory().FindLastItem(i => to.CanAddItem(i));

            if (item != null) {
                Plugin.Debug($"[{DbgId}] Pushing '{item.m_shared?.m_name ?? "null"}' -> target [{idx}] ({to.GetType().Name}) (counter={OutputCounter})");
                to.AddItem(item, container.GetInventory(), zNetView.m_zdo.m_uid);
            } else {
                Plugin.Debug($"[{DbgId}] No pushable item for target [{idx}] ({to.GetType().Name}) (full or filtered)");
            }
        }

        private void FindIO() {
            Quaternion rotation = transform.rotation;
            List<IPushTarget> targets = HopperHelper.FindTargets<IPushTarget>(transform.TransformPoint(outPos), outSize, rotation, i => i.PushPriority, this);
            
            // Collect all nearby hoppers for intersection check
            Collider[] colliders = Physics.OverlapSphere(transform.position, 2.5f, LayerMask.GetMask("piece", "piece_nonsolid"));
            foreach (Collider collider in colliders) {
                Hopper hopper = collider.GetComponentInParent<Hopper>();
                if (hopper != null && !targets.Any(t => t.NetworkHashCode() == hopper.NetworkHashCode())) {
                    targets.Add(hopper);
                }
            }

            List<IPushTarget> filteredTargets = new List<IPushTarget>();
            Collider[] pipeColliders = GetComponentsInChildren<Collider>();

            foreach (IPushTarget target in targets) {
                if (target is Hopper hopper) {
                    // Check if the hopper's input box overlaps any of the pipe's colliders
                    bool intersects = HopperHelper.BoxIntersectsColliders(hopper.transform.TransformPoint(hopper.InPos), hopper.InSize, hopper.transform.rotation, pipeColliders);
                    if (intersects) {
                        filteredTargets.Add(target);
                        Plugin.Debug($"[{DbgId}] Added Hopper output via body-overlap: {hopper.gameObject.name}");
                    } else {
                        Plugin.Debug($"[{DbgId}] Skipped Hopper (no body-overlap): {hopper.gameObject.name}");
                    }
                } else {
                    // Not a hopper (e.g. another pipe), just add it
                    filteredTargets.Add(target);
                    if (target is MonoBehaviour mb) Plugin.Debug($"[{DbgId}] Added standard output target: {mb.gameObject.name}");
                }
            }

            pushTo = filteredTargets;
            Plugin.Debug($"[{DbgId}] Output list: [{string.Join(", ", pushTo.Select(t => t.GetType().Name))}]");
        }

        private void OnDrawGizmos() {
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
