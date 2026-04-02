using BepInEx.Configuration;
using UnityEngine;

namespace ValheimPipes.Logic.Helper {
    public class BoxVisualizer : MonoBehaviour {
        private LineRenderer lineRenderer;
        private Vector3 boxCenter;
        private Vector3 boxSize;
        private ConfigEntry<bool> config;

        public void SetData(Vector3 center, Vector3 size, Color color, ConfigEntry<bool> configEntry) {
            boxCenter = center;
            boxSize = size;
            config = configEntry;

            if (lineRenderer == null) {
                GameObject lineObj = new GameObject("BoxVisualizerLine");
                lineObj.transform.SetParent(transform, false);
                lineRenderer = lineObj.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.02f;
                lineRenderer.endWidth = 0.02f;
                lineRenderer.useWorldSpace = false;
                
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Standard");
                
                if (shader != null) {
                    lineRenderer.material = new Material(shader);
                }
                
                lineRenderer.positionCount = 16;
            }

            lineRenderer.startColor = color;
            lineRenderer.endColor = color;

            UpdateVisual();
        }

        private void Update() {
            if (config != null && lineRenderer != null) {
                bool shouldBeActive = config.Value;
                if (lineRenderer.gameObject.activeSelf != shouldBeActive) {
                    lineRenderer.gameObject.SetActive(shouldBeActive);
                }
            }
        }

        private void UpdateVisual() {
            if (lineRenderer == null) return;
            
            Vector3 h = boxSize / 2f;
            Vector3 c = boxCenter;

            // Define 8 corners
            Vector3 c1 = c + new Vector3(-h.x, -h.y, -h.z);
            Vector3 c2 = c + new Vector3(h.x, -h.y, -h.z);
            Vector3 c3 = c + new Vector3(h.x, h.y, -h.z);
            Vector3 c4 = c + new Vector3(-h.x, h.y, -h.z);
            Vector3 c5 = c + new Vector3(-h.x, -h.y, h.z);
            Vector3 c6 = c + new Vector3(h.x, -h.y, h.z);
            Vector3 c7 = c + new Vector3(h.x, h.y, h.z);
            Vector3 c8 = c + new Vector3(-h.x, h.y, h.z);

            // Path to cover all 12 edges with 16 points
            lineRenderer.SetPosition(0, c1);
            lineRenderer.SetPosition(1, c2);
            lineRenderer.SetPosition(2, c3);
            lineRenderer.SetPosition(3, c4);
            lineRenderer.SetPosition(4, c1);
            lineRenderer.SetPosition(5, c5);
            lineRenderer.SetPosition(6, c6);
            lineRenderer.SetPosition(7, c2);
            lineRenderer.SetPosition(8, c6);
            lineRenderer.SetPosition(9, c7);
            lineRenderer.SetPosition(10, c3);
            lineRenderer.SetPosition(11, c7);
            lineRenderer.SetPosition(12, c8);
            lineRenderer.SetPosition(13, c4);
            lineRenderer.SetPosition(14, c8);
            lineRenderer.SetPosition(15, c5);
        }
    }
}
