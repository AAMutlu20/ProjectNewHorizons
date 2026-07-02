using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Determines where an enemy appears in the world.
    /// All methods are static — no allocation, no MonoBehaviour.
    /// </summary>
    public enum SpawnShapeType
    {
        Ring, // uniform random point on a circle — classic bullet heaven
        Arc, // random point within an angular arc (e.g. enemies from the left)
        Edges, // random point on the four arena edges
        Random, // random point inside a radius (debug/test use)
        DirectedArc,// arc aimed toward the player (pincer attacks)
    }

    public static class SpawnShape
    {
        /// <summary>
        /// Returns a world-space spawn position based on shape type.
        /// centre: typically Vector3.zero (arena centre, ground level).
        /// radius: from DifficultyParams.spawnRadius.
        /// playerPos: used for DirectedArc only.
        /// arcAngle: total arc angle in degrees for Arc / DirectedArc.
        /// arcDirection: centre direction of the arc in degrees (0 = +X).
        /// All positions lie on the XZ plane — Y is taken from centre.y (ground height).
        /// </summary>
        public static Vector3 GetPosition(
            SpawnShapeType shape,
            Vector3 centre,
            float radius,
            Vector3 playerPos = default,
            float arcAngle = 90f,
            float arcDirection = 0f)
        {
            switch (shape)
            {
                case SpawnShapeType.Ring:
                    return centre + RandomOnCircle(radius);

                case SpawnShapeType.Arc:
                {
                    var half = arcAngle * 0.5f * Mathf.Deg2Rad;
                    var dir = arcDirection * Mathf.Deg2Rad;
                    var angle = dir + Random.Range(-half, half);
                    return centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                }

                case SpawnShapeType.DirectedArc:
                {
                    // Arc aimed at the angle from centre to player (flattened to XZ)
                    var toPlayer = playerPos - centre;
                    var baseAngle = Mathf.Atan2(toPlayer.z, toPlayer.x);
                    var half = arcAngle * 0.5f * Mathf.Deg2Rad;
                    var angle = baseAngle + Random.Range(-half, half);
                    return centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                }

                case SpawnShapeType.Edges:
                {
                    // Spawn on one of 4 edges of a square arena (XZ plane)
                    var edge = Random.Range(0, 4);
                    var t = Random.Range(-1f, 1f);
                    return edge switch
                    {
                        0 => centre + new Vector3(t * radius, 0f, -radius), // south
                        1 => centre + new Vector3(t * radius, 0f, radius),  // north
                        2 => centre + new Vector3(-radius, 0f, t * radius), // west
                        _ => centre + new Vector3(radius, 0f, t * radius)   // east
                    };
                }

                case SpawnShapeType.Random:
                default: // Random inside radius, flattened to XZ
                {
                    var p = Random.insideUnitCircle * radius;
                    return centre + new Vector3(p.x, 0f, p.y);
                }
            }
        }

        private static Vector3 RandomOnCircle(float radius)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }
    }
}