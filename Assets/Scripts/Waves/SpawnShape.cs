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
        /// centre: typically Vector2.zero (arena centre).
        /// radius: from DifficultyParams.spawnRadius.
        /// playerPos: used for DirectedArc only.
        /// arcAngle: total arc angle in degrees for Arc / DirectedArc.
        /// arcDirection: centre direction of the arc in degrees (0 = right).
        /// </summary>
        public static Vector2 GetPosition(
            SpawnShapeType shape,
            Vector2 centre,
            float radius,
            Vector2 playerPos = default,
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
                    return centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }

                case SpawnShapeType.DirectedArc:
                {
                    // Arc aimed at the angle from centre to player
                    var toPlayer = (playerPos - centre);
                    var baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x);
                    var half = arcAngle * 0.5f * Mathf.Deg2Rad;
                    var angle = baseAngle + Random.Range(-half, half);
                    return centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }

                case SpawnShapeType.Edges:
                {
                    // Spawn on one of 4 edges of a square arena
                    var edge = Random.Range(0, 4);
                    var t = Random.Range(-1f, 1f);
                    return edge switch
                    {
                        0 => centre + new Vector2(t * radius, -radius), // bottom
                        1 => centre + new Vector2(t * radius, radius), // top
                        2 => centre + new Vector2(-radius, t * radius), // left
                        _ => centre + new Vector2(radius, t * radius)
                    };
                }

                case SpawnShapeType.Random:
                default: // Random inside radius
                    return centre + Random.insideUnitCircle * radius;
            }
        }

        private static Vector2 RandomOnCircle(float radius)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }
}