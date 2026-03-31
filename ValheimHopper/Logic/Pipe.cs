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
        [SerializeField] private Vector3 outSize = new Vector3(0.5f, 0.5f, 0.5f);

        private Container container;
        private ContainerTarget containerTarget;
        private List<IPushTarget> pushTo = new List<IPushTarget>();
        private List<Hopper> nearHoppers = new List<Hopper>();

        private const float TransferInterval = 0.2f;
        private const float ObjectSearchInterval = 3f;

        private int transferFrame;
        private int objectSearchFrame;
        private int frameOffset;


        protected override void Awake() {
            base.Awake();

            zNetView = GetComponent<ZNetView>();
            container = GetComponent<Container>();
            containerTarget = GetComponent<ContainerTarget>();

            transferFrame = Mathf.RoundToInt((1f / Time.fixedDeltaTime) * TransferInterval);
            objectSearchFrame = Mathf.RoundToInt((1f / Time.fixedDeltaTime) * ObjectSearchInterval);
            frameOffset = Mathf.Abs(GetInstanceID() % transferFrame);
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
            containerTarget.AddItem(item, source, sender);
        }

        public IEnumerable<ItemDrop.ItemData> GetItems() {
            return containerTarget.GetItems();
        }

        public void RemoveItem(ItemDrop.ItemData item, Inventory destination, Vector2i destinationPos, ZDOID sender) {
            containerTarget.RemoveItem(item, destination, destinationPos, sender);
            OutputCounter++; // Increment branch turn on passive pull
        }

        private void PushItems() {
            var activePullers = nearHoppers.Where(hopper => hopper.pullFrom.Exists(p => p.NetworkHashCode() == this.NetworkHashCode())).ToList();
            int totalBranches = pushTo.Count + activePullers.Count;

            if (totalBranches == 0) {
                return;
            }

            if (container.GetInventory().NrOfItems() == 0) {
                return;
            }

            int currentTurn = OutputCounter % totalBranches;

            if (currentTurn < pushTo.Count) {
                IPushTarget to = pushTo[currentTurn];
                if (!to.IsValid()) {
                    OutputCounter++;
                    return;
                }

                ItemDrop.ItemData item = container.GetInventory().FindLastItem(i => to.CanAddItem(i));

                if (item != null) {
                    to.AddItem(item, container.GetInventory(), zNetView.m_zdo.m_uid);
                    OutputCounter++;
                } else {
                    OutputCounter++; // Target rejects all items (full/filter)
                }
            } else {
                int pullerIndex = currentTurn - pushTo.Count;
                Hopper hopper = activePullers[pullerIndex];
                
                if (!hopper.IsValid()) {
                    OutputCounter++;
                    return;
                }

                ItemDrop.ItemData item = container.GetInventory().FindLastItem(i => hopper.CanAddItem(i));
                if (item == null) {
                    OutputCounter++; // Hopper full/filter rejects
                }
                // Else: we stall push and let Hopper pull to complete the turn.
            }
        }

        private void FindIO() {
            Quaternion rotation = transform.rotation;
            pushTo = HopperHelper.FindTargets<IPushTarget>(transform.TransformPoint(outPos), outSize, rotation, i => i.PushPriority, this);
            nearHoppers = HopperHelper.FindTargets<Hopper>(transform.position, Vector3.one * 3f, rotation, i => i.PullPriority, this);
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
