using Difficulty;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Draws the spawn ring in the Scene view so you can see where enemies appear.
    /// Zero runtime cost — Gizmos only draw in the editor.
    ///
    /// Attach to: SpawnRing_Debug empty GameObject in [World].
    /// Wire: difficultyScaler so it reads the current spawn radius.
    /// </summary>
    public class SpawnRingGizmo : MonoBehaviour
    {
        [SerializeField] private DifficultyConfig difficultyConfig;
        [SerializeField] private Color ringColor = new(1f, 0.4f, 0.1f, 0.6f);
        [SerializeField] private Color minRingColor = new(0.2f, 0.8f, 1f, 0.3f);

        void OnDrawGizmos()
        {
            if (!difficultyConfig) return;

            // Draw min and max spawn radius
            Gizmos.color = minRingColor;
            DrawCircle(transform.position, difficultyConfig.minSpawnRadius);

            Gizmos.color = ringColor;
            DrawCircle(transform.position, difficultyConfig.maxSpawnRadius);
        }

        private static void DrawCircle(Vector3 centre, float radius, int segments = 64)
        {
            var step = Mathf.PI * 2f / segments;
            var prev = centre + new Vector3(radius, 0f, 0f);
            for (var i = 1; i <= segments; i++)
            {
                var angle = i * step;
                var next = centre + new Vector3(Mathf.Cos(angle) * radius, 0f,
                    Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
