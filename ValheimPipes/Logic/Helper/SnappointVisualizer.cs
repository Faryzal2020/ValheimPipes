using UnityEngine;

namespace ValheimPipes.Logic.Helper {
    public class SnappointVisualizer : MonoBehaviour {
        private LineRenderer lineRenderer;
        private static Material lineMaterial;

        private void Awake() {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 16;

            if (lineMaterial == null) {
                lineMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;

            float size = 0.1f;
            Vector3[] points = new Vector3[16];

            // Bottom square
            points[0] = new Vector3(-size, -size, -size);
            points[1] = new Vector3(size, -size, -size);
            points[2] = new Vector3(size, -size, size);
            points[3] = new Vector3(-size, -size, size);
            points[4] = points[0];

            // Transition to top
            points[5] = new Vector3(-size, size, -size);
            
            // Top square
            points[6] = new Vector3(size, size, -size);
            points[7] = new Vector3(size, size, size);
            points[8] = new Vector3(-size, size, size);
            points[9] = points[5];

            // Vertical lines
            points[10] = new Vector3(-size, size, size);
            points[11] = new Vector3(-size, -size, size);
            points[12] = new Vector3(size, -size, size);
            points[13] = new Vector3(size, size, size);
            points[14] = new Vector3(size, size, -size);
            points[15] = new Vector3(size, -size, -size);

            lineRenderer.SetPositions(points);
        }
    }
}
