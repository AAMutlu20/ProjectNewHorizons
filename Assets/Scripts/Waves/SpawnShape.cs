using UnityEngine;

namespace Waves
{
    public enum SpawnShapeType
    {
        Ring,
        Arc,
        Edges,
        Random,
        DirectedArc,
    }

    /// <summary>
    /// Pure math — no MonoBehaviour, no scene object needed.
    /// Called by WaveDirector to roll a candidate XZ position.
    /// </summary>
    public static class SpawnShape
    {
        /// <summary>
        /// Returns a candidate world-space position for a spawn.
        /// centre    : player's position (ring follows the player).
        /// radius    : outer ring radius from DifficultyConfig.
        /// innerRadius: minimum distance from centre — set to SpawnRadiusProvider.SafeInnerRadius
        ///             so spawns are always off-screen. Clamped to radius-epsilon if >= radius.
        /// </summary>
        public static Vector3 GetPosition(
            SpawnShapeType shape,
            Vector3 centre,
            float radius,
            Vector3 playerPos = default,
            float arcAngle = 90f,
            float arcDirection = 0f,
            float innerRadius = 0f)
        {
            switch (shape)
            {
                case SpawnShapeType.Ring:
                {
                    // Clamp so inner is always strictly less than outer.
                    // If misconfigured (inner >= outer) we spawn on the outer ring edge
                    // rather than producing NaN — WaveDirector's gizmo will show the warning.
                    var safeInner = Mathf.Min(innerRadius, radius * 0.99f);
                    return centre + AnnulusPoint(safeInner, radius);
                }

                case SpawnShapeType.Arc:
                {
                    var half  = arcAngle * 0.5f * Mathf.Deg2Rad;
                    var dir   = arcDirection * Mathf.Deg2Rad;
                    var angle = dir + Random.Range(-half, half);
                    return centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                }

                case SpawnShapeType.DirectedArc:
                {
                    var toPlayer  = playerPos - centre;
                    var baseAngle = Mathf.Atan2(toPlayer.z, toPlayer.x);
                    var half      = arcAngle * 0.5f * Mathf.Deg2Rad;
                    var angle     = baseAngle + Random.Range(-half, half);
                    return centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                }

                case SpawnShapeType.Edges:
                {
                    var edge = Random.Range(0, 4);
                    var t    = Random.Range(-1f, 1f);
                    return edge switch
                    {
                        0 => centre + new Vector3(t * radius, 0f, -radius),
                        1 => centre + new Vector3(t * radius, 0f,  radius),
                        2 => centre + new Vector3(-radius, 0f, t * radius),
                        _ => centre + new Vector3( radius, 0f, t * radius),
                    };
                }

                default:
                {
                    var p = Random.insideUnitCircle * radius;
                    return centre + new Vector3(p.x, 0f, p.y);
                }
            }
        }

        // Area-correct annulus sample: uniform angle + sqrt-weighted radius.
        // Ensures even density across the ring, not biased toward the inner edge.
        private static Vector3 AnnulusPoint(float inner, float outer)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var t     = Random.value;
            var r     = Mathf.Sqrt(t * (outer * outer - inner * inner) + inner * inner);
            return new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
        }
    }
}
