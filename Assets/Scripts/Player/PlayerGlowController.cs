using Core;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Player equivalent of EnemyGlowController -- reuses the same Toon shader
    /// _GlowIntensity property via a MaterialPropertyBlock on the player's own
    /// model renderer, so the on-hit flash and the miniboss/buffed glow share
    /// one shader code path instead of needing two separate visual effects.
    ///
    /// Triggers a brief pulse whenever PlayerHealthChangedEvent reports a HP
    /// DECREASE specifically -- Heal() and passive regen also fire that same
    /// event, and shouldn't make the player flash.
    ///
    /// Attach to: PlayerRoot, alongside PlayerHealth.
    /// </summary>
    public class PlayerGlowController : MonoBehaviour
    {
        private const float IntensityEpsilon = 0.001f;
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");

        [Tooltip("The player's model renderer -- the one using the Custom/Toon material.")]
        [SerializeField] private Renderer modelRenderer;

        [SerializeField] private float hitGlowIntensity = 1.5f;
        [Tooltip("Faster than the enemy glow's fade -- this is a quick hit flash, not a sustained buff glow.")]
        [SerializeField] private float fadeSpeed = 6f;

        [Tooltip("Overrides the material's default _GlowColor (gold, meant for the miniboss/buff use case) " +
                 "just for this renderer, via the property block -- the shared material and enemy glow color are untouched.")]
        [SerializeField] private Color hitGlowColor = Color.red;

        private MaterialPropertyBlock _propertyBlock;
        private float _currentIntensity;
        private bool _hasPropertyBlockApplied;
        private float _lastKnownHp = -1f; // -1 sentinel: haven't received a real reading yet, so the first event never counts as a "decrease"

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            Debug.Assert(modelRenderer, "PlayerGlowController: modelRenderer not assigned.", this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        }

        private void Update()
        {
            _currentIntensity = Mathf.MoveTowards(_currentIntensity, 0f, fadeSpeed * Time.deltaTime);
            ApplyIntensity(_currentIntensity);
        }

        private void OnHealthChanged(PlayerHealthChangedEvent healthChanged)
        {
            if (_lastKnownHp >= 0f && healthChanged.Current < _lastKnownHp)
            {
                _currentIntensity = hitGlowIntensity; // snap to full brightness immediately, then Update() fades it back out
            }

            _lastKnownHp = healthChanged.Current;
        }

        private void ApplyIntensity(float intensity)
        {
            if (!modelRenderer) return;

            if (intensity <= IntensityEpsilon)
            {
                if (!_hasPropertyBlockApplied) return;
                modelRenderer.SetPropertyBlock(null); // restores SRP Batcher eligibility once not glowing, same reasoning as EnemyGlowController
                _hasPropertyBlockApplied = false;
                return;
            }

            modelRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(GlowIntensityId, intensity);
            _propertyBlock.SetColor(GlowColorId, hitGlowColor);
            modelRenderer.SetPropertyBlock(_propertyBlock);
            _hasPropertyBlockApplied = true;
        }
    }
}
