using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Tracks every active slow effect on the player (webs, ground hazards,
    /// future debuffs) and exposes the strongest one. Per the design doc's
    /// movement formula, only the single highest slow % applies — slows do
    /// not stack additively with each other.
    ///
    /// Attach to: PlayerRoot, alongside PlayerController.
    /// </summary>
    public class SlowEffectController : MonoBehaviour
    {
        private readonly List<float> _remainingDurationBySlow = new();
        private readonly List<float> _slowFractionBySlow = new();

        public float StrongestSlowFraction { get; private set; }

        private void Update()
        {
            TickDurations();
            RemoveExpiredSlows();
            RecalculateStrongestSlow();
        }

        /// <summary>Applies a slow as a fraction (0.5 = 50% slowed) for the given duration in seconds.</summary>
        public void ApplySlow(float slowFraction, float durationSeconds)
        {
            _slowFractionBySlow.Add(Mathf.Clamp01(slowFraction));
            _remainingDurationBySlow.Add(durationSeconds);
        }

        private void TickDurations()
        {
            for (var i = 0; i < _remainingDurationBySlow.Count; i++)
                _remainingDurationBySlow[i] -= Time.deltaTime;
        }

        private void RemoveExpiredSlows()
        {
            for (var i = _remainingDurationBySlow.Count - 1; i >= 0; i--)
            {
                if (_remainingDurationBySlow[i] > 0f) continue;

                _remainingDurationBySlow.RemoveAt(i);
                _slowFractionBySlow.RemoveAt(i);
            }
        }

        private void RecalculateStrongestSlow()
        {
            var strongest = 0f;
            foreach (var slowFraction in _slowFractionBySlow)
            {
                if (slowFraction > strongest)
                    strongest = slowFraction;
            }

            StrongestSlowFraction = strongest;
        }
    }
}
