using System;
using UnityEngine;
using ValheimHopper.Logic.Helper;

namespace ValheimHopper.Logic {
    public class NetworkPiece : MonoBehaviour {
        protected ZNetView zNetView;
        private int localOutputCounter = 0;

        protected int OutputCounter {
            get {
                if (zNetView && zNetView.IsValid() && Plugin.SyncOutputCounter.Value) {
                    return zNetView.GetZDO().GetInt("hopper_output_counter", localOutputCounter);
                }
                return localOutputCounter;
            }
            set {
                if (zNetView && zNetView.IsValid() && Plugin.SyncOutputCounter.Value) {
                    zNetView.GetZDO().Set("hopper_output_counter", value);
                }
                localOutputCounter = value;
            }
        }

        protected virtual void Awake() {
            zNetView = GetComponentInParent<ZNetView>();
        }

        public bool IsValid() {
            return this && zNetView && HopperHelper.IsValidNetView(zNetView) && zNetView.HasOwner();
        }

        public int NetworkHashCode() {
            if (zNetView && zNetView.m_zdo != null) {
                return HopperHelper.GetNetworkHashCode(zNetView);
            }
            return this.GetInstanceID();
        }

        public bool Equals(ITarget x, ITarget y) {
            return x == y || x?.NetworkHashCode() == y?.NetworkHashCode();
        }

        public int GetHashCode(ITarget obj) {
            return obj.NetworkHashCode();
        }
    }
}
