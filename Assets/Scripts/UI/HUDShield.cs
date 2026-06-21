using System.Collections.Generic;
using Core;
using Player;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Shows one pip per shield layer. Pip count adapts to MaxLayers (1-4
    /// depending on rarity) rather than a fixed number — instantiates from
    /// pipPrefab into pipContainer the first time MaxLayers is known, then
    /// just toggles active state on subsequent updates.
    ///
    /// Hidden entirely if the shield was never granted (MaxLayers stays 0).
    ///
    /// Attach to: HUD canvas. Wire pipContainer (a horizontal layout group)
    /// and pipPrefab (a single Image).
    /// </summary>
    public class HUDShield : MonoBehaviour
    {
        [SerializeField] private GameObject shieldRoot;
        [SerializeField] private Transform pipContainer;
        [SerializeField] private Image pipPrefab;

        private readonly List<Image> _pips = new();
        private int _maxLayers;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ShieldChangedEvent>(OnShieldChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ShieldChangedEvent>(OnShieldChanged);
        }

        private void OnShieldChanged(ShieldChangedEvent shieldChanged)
        {
            SetVisible(true);

            if (shieldChanged.MaxLayers != _maxLayers)
                RebuildPips(shieldChanged.MaxLayers);

            UpdatePipStates(shieldChanged.CurrentLayers);
        }

        private void RebuildPips(int maxLayers)
        {
            _maxLayers = maxLayers;

            foreach (var pip in _pips)
                if (pip) Destroy(pip.gameObject);
            _pips.Clear();

            for (var i = 0; i < maxLayers; i++)
                _pips.Add(Instantiate(pipPrefab, pipContainer));
        }

        private void UpdatePipStates(int currentLayers)
        {
            for (var i = 0; i < _pips.Count; i++)
                _pips[i].gameObject.SetActive(i < currentLayers);
        }

        private void SetVisible(bool isVisible)
        {
            if (shieldRoot) shieldRoot.SetActive(isVisible);
        }
    }
}
