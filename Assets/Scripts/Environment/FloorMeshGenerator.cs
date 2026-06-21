using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Generates a near-flat floor mesh with subtle height variation, per
    /// the design doc's "uneven ground to simulate sinking and floor cracks."
    /// Deliberately simple and low-amplitude per the actual target -- a
    /// gentle, barely-noticeable undulation, not visible hills or pits.
    ///
    /// Generated ONCE (on Awake, or via the Inspector context menu while
    /// editing) and baked into a static Mesh -- there is no per-frame cost.
    ///
    /// Collision uses a flat BoxCollider rather than a MeshCollider matching
    /// the generated shape -- since the height variation is intentionally
    /// barely noticeable, a MeshCollider's per-triangle cooking cost buys
    /// almost no real collision accuracy over a simple box. The box's X/Z
    /// size matches floorSizeXz exactly; its height is a flat 0.1, just
    /// enough for the player/enemies to stand on without sinking through,
    /// regardless of the mesh's own (tiny) height variation.
    ///
    /// Cracks are represented via vertex colour darkening rather than actual
    /// carved geometry -- cheap (zero extra vertices/triangles) and easy for
    /// a shader to read (see the Toon shader's _BaseColor, which could be
    /// extended to multiply by vertex colour if you want this to visually
    /// read as cracks; left as a hook for a future shader pass rather than
    /// hardcoded here, since this script's job is geometry, not shading).
    ///
    /// Attach to: a GameObject with MeshFilter and MeshRenderer (BoxCollider
    /// added automatically if missing).
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class FloorMeshGenerator : MonoBehaviour
    {
        // Mathf.PerlinNoise produces ridging artifacts and eventually flattens
        // out entirely once sample coordinates exceed roughly 65535 -- keeping
        // the random per-generation offset well under that avoids the issue
        // while still giving enough range that repeated generations (or
        // multiple floor tiles) don't visibly repeat the same pattern.
        private const float MaxNoiseSeedOffset = 1000f;

        // Flat collision height for the BoxCollider, in world units -- not
        // exposed as a tunable field since the design intent is a fixed,
        // always-thin collision slab regardless of floor size or noise settings.
        private const float ColliderHeight = 0.1f;

        [Header("Size")]
        [Tooltip("Total floor size in world units. X maps to world X, Z maps to world Z " +
                 "(named XZ rather than a plain Vector2's default X/Y, since the floor lies flat on the XZ plane).")]
        [SerializeField] private Vector2 floorSizeXz = new(60f, 60f);

        [Tooltip("World units between each grid vertex. Smaller = more vertices = smoother but costlier. " +
                 "For a near-flat look, a coarser grid is both cheaper AND looks more appropriately subtle.")]
        [SerializeField] private float cellSize = 2f;

        [Header("Height Variation (keep small -- this should read as 'nearly flat')")]
        [Tooltip("Maximum height deviation from flat, in world units. E.g. 0.15 means the floor " +
                 "never rises/sinks more than 15cm from level.")]
        [SerializeField] private float heightAmplitude = 0.15f;

        [Tooltip("Lower = broader, gentler undulation. Higher = more frequent bumps. " +
                 "Keep low for a 'ground sinking' feel rather than a bumpy/rocky one.")]
        [SerializeField] private float noiseScale = 0.08f;

        [Header("Crack Tint (vertex colour darkening, no extra geometry)")]
        [SerializeField] private bool generateCrackTint = true;
        [Tooltip("How much darker the tint gets at its strongest crack points. 0 = no visible tint.")]
        [SerializeField] [Range(0f, 1f)] private float crackTintStrength = 0.25f;
        [Tooltip("Higher = more, finer crack-like tint variation. Independent of heightAmplitude/noiseScale.")]
        [SerializeField] private float crackNoiseScale = 0.35f;

        private MeshFilter _meshFilter;
        private BoxCollider _boxCollider;

        private void Awake()
        {
            Generate();
        }

        /// <summary>Builds and assigns the mesh and collider. Safe to call multiple times (e.g. from the Inspector) -- regenerates from scratch each time.</summary>
        [ContextMenu("Generate Floor Mesh")]
        public void Generate()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _boxCollider = GetComponent<BoxCollider>();
            if (!_boxCollider) _boxCollider = gameObject.AddComponent<BoxCollider>();

            var mesh = BuildMesh();
            _meshFilter.sharedMesh = mesh;

            UpdateBoxCollider();
        }

        private void UpdateBoxCollider()
        {
            // Centred at local origin with a small downward offset so the top
            // face of the box sits at y = 0 (the floor's nominal flat level),
            // matching where the visual mesh's vertices hover around.
            _boxCollider.center = new Vector3(0f, -ColliderHeight * 0.5f, 0f);
            _boxCollider.size = new Vector3(floorSizeXz.x, ColliderHeight, floorSizeXz.y);
        }

        private Mesh BuildMesh()
        {
            var columns = Mathf.Max(1, Mathf.RoundToInt(floorSizeXz.x / cellSize));
            var rows = Mathf.Max(1, Mathf.RoundToInt(floorSizeXz.y / cellSize));

            var heightNoiseOffset = new Vector2(
                Random.Range(0f, MaxNoiseSeedOffset),
                Random.Range(0f, MaxNoiseSeedOffset));
            var crackNoiseOffset = new Vector2(
                Random.Range(0f, MaxNoiseSeedOffset),
                Random.Range(0f, MaxNoiseSeedOffset));

            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uvs = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];

            BuildVertices(columns, rows, heightNoiseOffset, crackNoiseOffset, vertices, uvs, colors);
            var triangles = BuildTriangles(columns, rows);

            var mesh = new Mesh { name = "GeneratedFloor" };

            // A 60x60 unit floor at cellSize 2 is ~31x31 vertices (~1900 vertices,
            // ~3600 triangles) -- comfortably under the 16-bit index limit, but
            // using 32-bit indices removes any risk if floorSizeXz/cellSize are
            // tuned larger later without anyone remembering this constraint.
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void BuildVertices(int columns, int rows, Vector2 heightNoiseOffset, Vector2 crackNoiseOffset,
            Vector3[] vertices, Vector2[] uvs, Color[] colors)
        {
            var halfWidth = floorSizeXz.x * 0.5f;
            var halfLength = floorSizeXz.y * 0.5f;

            for (var row = 0; row <= rows; row++)
            {
                for (var column = 0; column <= columns; column++)
                {
                    var index = row * (columns + 1) + column;

                    var worldX = -halfWidth + column * cellSize;
                    var worldZ = -halfLength + row * cellSize;
                    var height = SampleHeight(worldX, worldZ, heightNoiseOffset);

                    vertices[index] = new Vector3(worldX, height, worldZ);
                    uvs[index] = new Vector2((float)column / columns, (float)row / rows);
                    colors[index] = SampleCrackTint(worldX, worldZ, crackNoiseOffset);
                }
            }
        }

        private float SampleHeight(float worldX, float worldZ, Vector2 noiseOffset)
        {
            var noiseValue = Mathf.PerlinNoise(
                worldX * noiseScale + noiseOffset.x,
                worldZ * noiseScale + noiseOffset.y);

            // Perlin noise returns roughly 0..1 -- remap to -1..1 first so the
            // floor varies both up and down around its base level (sinking
            // AND slightly raised patches), rather than only ever sinking
            // downward from a flat ceiling.
            var signedNoise = noiseValue * 2f - 1f;
            return signedNoise * heightAmplitude;
        }

        private Color SampleCrackTint(float worldX, float worldZ, Vector2 noiseOffset)
        {
            if (!generateCrackTint) return Color.white;

            var noiseValue = Mathf.PerlinNoise(
                worldX * crackNoiseScale + noiseOffset.x,
                worldZ * crackNoiseScale + noiseOffset.y);

            // Only darken, never brighten -- a crack reads as a dark recess,
            // not a bright highlight. Clamping noiseValue's lower range means
            // most of the floor stays near-white (no tint) and only the
            // noise's darkest pockets show a visible crack-like darkening,
            // rather than the whole floor having an even grey wash.
            var darkening = Mathf.Clamp01((noiseValue - 0.5f) * -2f) * crackTintStrength;
            var brightness = 1f - darkening;

            return new Color(brightness, brightness, brightness, 1f);
        }

        private static int[] BuildTriangles(int columns, int rows)
        {
            var triangles = new int[columns * rows * 6];
            var triangleIndex = 0;

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var topLeft = row * (columns + 1) + column;
                    var topRight = topLeft + 1;
                    var bottomLeft = topLeft + (columns + 1);
                    var bottomRight = bottomLeft + 1;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;

                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = bottomRight;
                }
            }

            return triangles;
        }
    }
}
