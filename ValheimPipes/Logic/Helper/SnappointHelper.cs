using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimPipes.Logic.Helper {
    public static class SnappointHelper {
        public static void AddSnappoints(string name, Vector3[] points) {
            GameObject target = PrefabManager.Instance.GetPrefab(name);

            if (target == null) {
                Logger.LogWarning($"{name} not found. Cannot add snappoints");
                return;
            }

            foreach (Vector3 point in points) {
                CreateSnappoint(point, target.transform);
            }
        }

        private static void CreateSnappoint(Vector3 pos, Transform parent) {
            GameObject snappoint = new GameObject("_snappoint");
            snappoint.transform.parent = parent;
            snappoint.transform.localPosition = pos;
            snappoint.tag = "snappoint";
            snappoint.SetActive(false);

            if (Plugin.ShowSnappointHighlights.Value) {
                GameObject visual = new GameObject("_snappoint_visual");
                visual.transform.parent = snappoint.transform;
                visual.transform.localPosition = Vector3.zero;
                visual.AddComponent<SnappointVisualizer>();
                // We keep the main snappoint inactive but its visual child active, 
                // OR we can make the visual child active while the snappoint stays inactive for snapping.
                // In Valheim, snapping works on inactive objects with the "snappoint" tag? 
                // Actually, most snappoints are active gameobjects but with no renderer.
            }
        }

        public static void FixPiece(string name) {
            GameObject target = PrefabManager.Instance.GetPrefab(name);

            if (target == null) {
                Logger.LogWarning($"{name} not found. Cannot fix piece snappoints");
                return;
            }

            int snapLayer = LayerMask.NameToLayer("piece_nonsolid");
            if (snapLayer == -1) {
                Logger.LogError("Layer 'piece_nonsolid' not found!");
                return;
            }

            foreach (Transform child in target.GetComponentsInChildren<Transform>()) {
                if (child.CompareTag("snappoint")) {  // Valheim tags snap points
                    child.gameObject.layer = snapLayer;
                }
            }
        }
    }
}
