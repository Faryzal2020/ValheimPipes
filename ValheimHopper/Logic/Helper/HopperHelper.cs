using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimHopper.Logic.Helper {
    public static class HopperHelper {
        private static readonly Collider[] tempColliders = new Collider[256];
        private static int PieceMask { get; } = LayerMask.GetMask("piece", "piece_nonsolid");
        private static int ItemMask { get; } = LayerMask.GetMask("item");

        public static bool IsInRange(Vector3 position, Vector3 target, float range) {
            return Vector3.SqrMagnitude(target - position) < range * range;
        }

        public static bool IsInRange(Vector3 position, Collider collider, float range) {
            Bounds bounds = collider.bounds;
            bounds.Expand(range);
            return bounds.Contains(position);
        }

        public static bool IsValidNetView(ZNetView netView) {
            return netView && netView.IsValid() && netView.m_zdo != null;
        }

        public static int GetNetworkHashCode(ZNetView netView) {
            return netView.m_zdo.m_uid.GetHashCode();
        }

        public static int GetFixedFrameCount() {
            return Mathf.RoundToInt(Time.fixedTime / Time.fixedDeltaTime);
        }

        public static List<T> FindTargets<T>(Vector3 pos, Vector3 size, Quaternion rotation, Func<T, HopperPriority> orderBy, ITarget exclude) where T : ITarget {
            List<T> targets = new List<T>();
            int count = Physics.OverlapBoxNonAlloc(pos, size / 2f, tempColliders, rotation, PieceMask | ItemMask);

            for (int i = 0; i < count; i++) {
                List<T> possibleTargets = tempColliders[i]
                                          .GetComponentsInParent<T>()
                                          .Where(target => target.InRange(pos) && target.IsValid() && target.NetworkHashCode() != exclude.NetworkHashCode())
                                          .ToList();

                targets.AddRange(possibleTargets);
            }

            return targets.OrderByDescending(orderBy).GroupBy(t => t.NetworkHashCode()).Select(t => t.First()).ToList();
        }

        public static float GetIntersectionPercentage(Vector3 boxPos, Vector3 boxSize, Quaternion boxRot, Collider targetCollider) {
            int samplesPerAxis = 3;
            int insideCount = 0;
            int totalCount = samplesPerAxis * samplesPerAxis * samplesPerAxis;

            for (int x = 0; x < samplesPerAxis; x++) {
                for (int y = 0; y < samplesPerAxis; y++) {
                    for (int z = 0; z < samplesPerAxis; z++) {
                        float fx = (x + 0.5f) / samplesPerAxis - 0.5f;
                        float fy = (y + 0.5f) / samplesPerAxis - 0.5f;
                        float fz = (z + 0.5f) / samplesPerAxis - 0.5f;

                        Vector3 localPoint = new Vector3(fx * boxSize.x, fy * boxSize.y, fz * boxSize.z);
                        Vector3 worldPoint = boxPos + boxRot * localPoint;

                        if (IsPointInCollider(worldPoint, targetCollider)) {
                            insideCount++;
                        }
                    }
                }
            }

            return (float)insideCount / totalCount;
        }

        private static bool IsPointInCollider(Vector3 point, Collider collider) {
            if (collider is BoxCollider box) {
                Vector3 p = box.transform.InverseTransformPoint(point) - box.center;
                return Mathf.Abs(p.x) <= box.size.x / 2f &&
                       Mathf.Abs(p.y) <= box.size.y / 2f &&
                       Mathf.Abs(p.z) <= box.size.z / 2f;
            }

            return collider.ClosestPoint(point) == point;
        }
    }
}
