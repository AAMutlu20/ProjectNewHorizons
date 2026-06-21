using System;
using Core;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// World-space warning ring for AOE attacks — shows where the area is and
    /// fills over its duration so the player can see how much time is left
    /// to get out. Self-contained: owns its own countdown.
    ///
    /// Renders via a world-space Image (Canvas set to World Space, flat on
    /// the ground) using a radial fill — simplest path that reuses Unity's
    /// built-in Image fill modes instead of a custom shader. Swap the Image
    /// for a shader-driven ring later without touching this script's logic.
    ///
    /// Pooled by AoeTelegraphRingPool — never Instantiate/Destroy this directly.
    /// </summary>
    public class AoeTelegraphRing : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image ringFillImage;

        [System.NonSerialized] public AoeTelegraphRingPool Pool;

        private float _telegraphDuration;
        private float _elapsed;
        private bool _isActive;
        private float _radius;
        private Action _onComplete;

        /// <summary>Starts the telegraph at worldPosition, filling over durationSeconds. Calls onComplete when it finishes.</summary>
        public void Begin(Vector3 worldPosition, float durationSeconds, float radius, Action onComplete)
        {
            transform.position = worldPosition;
            transform.localScale = new Vector3(radius, radius, radius);

            _radius = radius;
            _telegraphDuration = durationSeconds;
            _elapsed = 0f;
            _isActive = true;
            _onComplete = onComplete;

            if (ringFillImage) ringFillImage.fillAmount = 0f;

            EventBus.Emit(new AoeTelegraphStartedEvent
            {
                Position = worldPosition,
                Radius = radius,
                Duration = durationSeconds,
            });
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsed += Time.deltaTime;
            var percentageComplete = _telegraphDuration > 0f
                ? Mathf.Clamp01(_elapsed / _telegraphDuration)
                : 1f;

            if (ringFillImage) ringFillImage.fillAmount = percentageComplete;

            if (_elapsed < _telegraphDuration) return;

            Complete();
        }

        private void Complete()
        {
            _isActive = false;
            _onComplete?.Invoke();
            EventBus.Emit(new AoeTelegraphCompleteEvent { Position = transform.position, Radius = _radius });
            Pool.Return(this);
        }
    }

    /// <summary>Emitted the moment a telegraph ring begins — used to start matching "incoming" VFX (e.g. a falling meteor streak).</summary>
    public struct AoeTelegraphStartedEvent
    {
        public Vector3 Position;
        public float Radius;
        public float Duration;
    }

    /// <summary>Emitted when a telegraph ring finishes filling — the AOE effect should land now.</summary>
    public struct AoeTelegraphCompleteEvent
    {
        public Vector3 Position;
        public float Radius;
    }
}
