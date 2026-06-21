using Enemies;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Drives the buff/miniboss glow properties on the SAME renderer the
    /// enemy already uses for its Custom/Toon material -- no second mesh,
    /// no second renderer, no second material. Duplicating geometry per
    /// enemy (or even baking a permanent second submesh into every enemy
    /// prefab) would cost draw calls and vertices for the entire enemy
    /// population at all times, just to support the minority that are ever
    /// buffed -- not acceptable at the 100-enemies-on-screen target.
    ///
    /// Glow is folded directly into Toon.shader's existing forward pass as
    /// an additive term reusing the rim light's fresnel math, so an unbuffed
    /// enemy pays only a few extra ALU instructions in a shader that's
    /// already running -- zero extra geometry, zero extra draw calls.
    ///
    /// IMPORTANT CORRECTION vs an earlier version of this comment:
    /// MaterialPropertyBlock does NOT preserve SRP Batcher compatibility --
    /// per Unity's own docs, ANY renderer using one is excluded from SRP
    /// batching entirely while it's applied. The mitigation here is to only
    /// apply the block while intensity is actually non-zero (HasPropertyBlock
    /// is cleared via Renderer.SetPropertyBlock(null) when not glowing), so
    /// only the minority of enemies that are CURRENTLY buffed or miniboss
    /// fall out of batching -- the bulk of the on-screen population (unbuffed,
    /// no property block applied) stays fully SRP-batched.
    /// </summary>
    [RequireComponent(typeof(EnemyView))]
    public class EnemyGlowController : MonoBehaviour
    {
        private const float IntensityEpsilon = 0.001f;

        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");

        [Tooltip("The enemy's existing model renderer -- the one already using the Custom/Toon material. Not a separate glow-only renderer.")]
        [SerializeField] private Renderer modelRenderer;

        [SerializeField] private float minibossGlowIntensity = 1.5f;
        [SerializeField] private float buffedGlowIntensity = 1f;
        [SerializeField] private float fadeSpeed = 4f;

        private EnemyView _view;
        private MaterialPropertyBlock _propertyBlock;
        private float _currentIntensity;
        private bool _hasPropertyBlockApplied;

        private void Awake()
        {
            _view = GetComponent<EnemyView>();
            _propertyBlock = new MaterialPropertyBlock();

            Debug.Assert(modelRenderer, "EnemyGlowController: modelRenderer not assigned.", this);
        }

        private void Update()
        {
            // Miniboss glow is permanent for this enemy's lifetime -- checked every
            // frame (not cached at spawn) since this component and its renderer
            // get reused across pooled spawns, some of which may be miniboss and
            // some not.
            var targetIntensity = _view.Data.IsMiniboss ? minibossGlowIntensity
                : _view.Data.IsBuffed ? buffedGlowIntensity
                : 0f;

            _currentIntensity = Mathf.MoveTowards(_currentIntensity, targetIntensity, fadeSpeed * Time.deltaTime);
            ApplyIntensity(_currentIntensity);
        }

        private void ApplyIntensity(float intensity)
        {
            if (!modelRenderer) return;

            // Clear the property block entirely once intensity decays back to
            // zero, restoring SRP Batcher eligibility for this renderer -- an
            // enemy that isn't currently glowing shouldn't pay the batching
            // cost just because it glowed at some point in the past.
            if (intensity <= IntensityEpsilon)
            {
                if (!_hasPropertyBlockApplied) return;

                modelRenderer.SetPropertyBlock(null);
                _hasPropertyBlockApplied = false;
                return;
            }

            modelRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(GlowIntensityId, intensity);
            modelRenderer.SetPropertyBlock(_propertyBlock);
            _hasPropertyBlockApplied = true;
        }
    }
}
