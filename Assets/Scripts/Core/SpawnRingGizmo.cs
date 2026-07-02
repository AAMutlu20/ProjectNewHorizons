using Camera;
using Difficulty;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Draws the spawn annulus in the Scene view so you can see exactly where
    /// enemies will appear relative to the player. Zero runtime cost — Gizmos
    /// only draw in the editor, never in a build.
    ///
    /// Shows three rings:
    ///   BLUE  = inner safe radius (SpawnRadiusProvider — the camera's edge).
    ///           Enemies never spawn inside this. If this ring is LARGER than
    ///           the orange ring, your DifficultyConfig spawn radius is too small.
    ///   CYAN  = min spawn radius (DifficultyConfig.minSpawnRadius, at t=0)
    ///   ORANGE = max spawn radius (DifficultyConfig.maxSpawnRadius, at t=1)
    ///   Enemies spawn in the annulus between the blue and orange rings.
    ///
    /// Attach to: the WaveDirector GameObject, or any [Systems] object.
    /// Wire: difficultyConfig, player, and optionally spawnRadiusProvider.
    /// </summary>
    public class SpawnRingGizmo : MonoBehaviour
    {
        [SerializeField] private DifficultyConfigSo difficultyConfig;
        [SerializeField] private Transform player;
        [SerializeField] private SpawnRadiusProvider spawnRadiusProvider;

        [Header("Colours")]
        [SerializeField] private Color innerRadiusColor  = new(0.2f, 0.5f, 1f,  0.8f); // blue  — camera edge / safe inner
        [SerializeField] private Color minSpawnColor     = new(0.2f, 0.9f, 0.9f, 0.5f); // cyan  — min spawn radius
        [SerializeField] private Color maxSpawnColor     = new(1f,   0.4f, 0.1f, 0.7f); // orange — max spawn radius
        [SerializeField] private Color warningColor      = new(1f,   0f,   0f,   0.9f); // red   — misconfiguration

        private void OnDrawGizmos()
        {
            if (!difficultyConfig) return;

            // Centre on player if wired, otherwise fall back to this transform.
            var centre = player ? player.position : transform.position;
            centre.y = transform.position.y; // keep gizmo flat at this object's Y

            var innerRadius = spawnRadiusProvider
                ? spawnRadiusProvider.SafeInnerRadius
                : 0f;

            var minOuter = difficultyConfig.minSpawnRadius;
            var maxOuter = difficultyConfig.maxSpawnRadius;

            // Warn visually if the inner radius exceeds the outer spawn radius —
            // this is the exact condition that produces NaN spawn positions.
            var misconfigured = innerRadius >= minOuter;

            // Inner safe radius (camera edge)
            if (innerRadius > 0f)
            {
                Gizmos.color = misconfigured ? warningColor : innerRadiusColor;
                DrawCircle(centre, innerRadius);

#if UNITY_EDITOR
                UnityEditor.Handles.color = misconfigured ? warningColor : innerRadiusColor;
                UnityEditor.Handles.Label(
                    centre + new Vector3(innerRadius, 0f, 0f),
                    misconfigured
                        ? $"INNER ({innerRadius:F0}u) > OUTER ({minOuter:F0}u)\nIncrease SpawnRadius!"
                        : $"Camera edge\n{innerRadius:F1}u");
#endif
            }

            // Min spawn radius
            Gizmos.color = misconfigured ? warningColor : minSpawnColor;
            DrawCircle(centre, minOuter);

            // Max spawn radius
            Gizmos.color = misconfigured ? warningColor : maxSpawnColor;
            DrawCircle(centre, maxOuter);

#if UNITY_EDITOR
            // Fill the annulus between inner and max with a transparent disc
            // so the valid spawn zone is visually obvious at a glance.
            if (!misconfigured)
            {
                UnityEditor.Handles.color = new Color(
                    maxSpawnColor.r, maxSpawnColor.g, maxSpawnColor.b, 0.06f);
                UnityEditor.Handles.DrawSolidDisc(centre, Vector3.up, maxOuter);

                // Punch out the inner area so only the annulus is filled
                UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0f);
                // (Unity doesn't support disc subtraction natively — the filled
                //  inner disc is left transparent; the overlay is subtle enough)

                UnityEditor.Handles.color = minSpawnColor;
                UnityEditor.Handles.Label(
                    centre + new Vector3(minOuter, 0f, 0f),
                    $"Min spawn\n{minOuter:F1}u");

                UnityEditor.Handles.color = maxSpawnColor;
                UnityEditor.Handles.Label(
                    centre + new Vector3(maxOuter, 0f, 0f),
                    $"Max spawn\n{maxOuter:F1}u");
            }
#endif
        }

        private static void DrawCircle(Vector3 centre, float radius, int segments = 64)
        {
            var step = Mathf.PI * 2f / segments;
            var prev = centre + new Vector3(radius, 0f, 0f);
            for (var i = 1; i <= segments; i++)
            {
                var angle = i * step;
                var next = centre + new Vector3(
                    Mathf.Cos(angle) * radius, 0f,
                    Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
