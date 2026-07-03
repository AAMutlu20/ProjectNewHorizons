using Abilities;
using Core;
using Player;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // ── Mixer ─────────────────────────────────────────────────────────────
        [Header("Mixer")]
        [Tooltip("Assign the AudioMixer asset. Create groups: Master, Music, SFX, UI.")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup uiMixerGroup;

        // Mixer parameter names — must match the Exposed Parameters in your AudioMixer.
        // Right-click a volume fader in the mixer → Expose → rename to these exact strings.
        private const string MixerMasterVolume = "MasterVolume";
        private const string MixerMusicVolume  = "MusicVolume";
        private const string MixerSfxVolume    = "SFXVolume";
        private const string MixerUiVolume     = "UIVolume";

        // PlayerPrefs keys for persisting volume across sessions
        private const string PrefMaster = "Vol_Master";
        private const string PrefMusic  = "Vol_Music";
        private const string PrefSfx    = "Vol_SFX";
        private const string PrefUi     = "Vol_UI";

        // ── Music ─────────────────────────────────────────────────────────────
        [Header("Music")]
        [Tooltip("Menu scene music. Multiple clips = random pick each time.")]
        [SerializeField] private AudioClip[] mainMenuTracks;

        [Tooltip("In-game music playlist. Plays sequentially then loops back to the first.")]
        [SerializeField] private AudioClip[] inGameTracks;

        [SerializeField][Range(0f, 5f)] private float musicCrossfadeDuration = 1.5f;

        // ── Ability SFX ───────────────────────────────────────────────────────
        [Header("Ability SFX")]
        [SerializeField] private AudioClip[] shockwaveSounds;
        [SerializeField] private AudioClip[] meteorSlamCastSounds;
        [SerializeField] private AudioClip[] laserBeamSounds;
        [SerializeField] private AudioClip[] coneOfFireSounds;
        [SerializeField] private AudioClip[] darkShieldBlockSounds;
        [SerializeField] private AudioClip   poisonAuraLoopClip;

        // ── Melee SFX ─────────────────────────────────────────────────────────
        [Header("Melee SFX")]
        [SerializeField] private AudioClip[] meleeHitSounds;
        [SerializeField] private AudioClip[] meleeCritSounds;
        [SerializeField] private AudioClip[] cleavingSounds;
        [SerializeField] private AudioClip[] lifestealSounds;

        // ── Enemy SFX ─────────────────────────────────────────────────────────
        [Header("Enemy SFX")]
        [SerializeField] private AudioClip[] enemyDeathSounds;
        [SerializeField] private AudioClip[] minibossDeathSounds;

        // ── Player SFX ────────────────────────────────────────────────────────
        [Header("Player SFX")]
        [SerializeField] private AudioClip[] playerHitSounds;
        [SerializeField] private AudioClip   playerDeathSound;
        [SerializeField] private AudioClip[] xpPickupSounds;

        // ── UI SFX ────────────────────────────────────────────────────────────
        [Header("UI SFX")]
        [SerializeField] private AudioClip[] levelUpSounds;
        [SerializeField] private AudioClip[] choicePresentedSounds;
        [SerializeField] private AudioClip[] choiceSelectedSounds;

        [Tooltip("Played when any UI button is clicked. " +
                 "Add UIButtonSound component to each button and it calls PlayButtonClick() automatically.")]
        [SerializeField] private AudioClip[] buttonClickSounds;

        // ── Internal ──────────────────────────────────────────────────────────
        private AudioSource _musicA;
        private AudioSource _musicB;
        private bool        _onA = true;
        private AudioSource Active   => _onA ? _musicA : _musicB;
        private AudioSource Inactive => _onA ? _musicB : _musicA;

        // In-game playlist state
        private int   _inGameTrackIndex;
        private bool  _isPlayingInGame;

        private AudioSource[] _sfx;
        private int           _sfxIdx;
        private const int     SfxPool = 8;

        private AudioSource _uiSource;
        private AudioSource _poisonLoop;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSources();
            LoadVolumes();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            // Advance in-game playlist when the current track finishes
            if (_isPlayingInGame && Active.clip != null && !Active.isPlaying)
                PlayNextInGameTrack();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex == 0)
            {
                _isPlayingInGame = false;
                Crossfade(Random(mainMenuTracks));
            }
            else
            {
                _isPlayingInGame = true;
                _inGameTrackIndex = 0;
                Crossfade(CurrentInGameTrack());
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<ExperienceChangedEvent>(OnXpChanged);
            EventBus.Subscribe<LevelUpEvent>(OnLevelUp);
            EventBus.Subscribe<AbilityCastEvent>(OnAbilityCast);
            EventBus.Subscribe<ShieldBlockedDamageEvent>(OnShieldBlocked);
            EventBus.Subscribe<MeleeHitEvent>(OnMeleeHit);
            EventBus.Subscribe<CleavingTriggeredEvent>(OnCleaving);
            EventBus.Subscribe<LifestealHealEvent>(OnLifesteal);
            EventBus.Subscribe<StatChoiceResolvedEvent>(OnStatChosen);
            EventBus.Subscribe<AbilityChoiceResolvedEvent>(OnAbilityChosen);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<ExperienceChangedEvent>(OnXpChanged);
            EventBus.Unsubscribe<LevelUpEvent>(OnLevelUp);
            EventBus.Unsubscribe<AbilityCastEvent>(OnAbilityCast);
            EventBus.Unsubscribe<ShieldBlockedDamageEvent>(OnShieldBlocked);
            EventBus.Unsubscribe<MeleeHitEvent>(OnMeleeHit);
            EventBus.Unsubscribe<CleavingTriggeredEvent>(OnCleaving);
            EventBus.Unsubscribe<LifestealHealEvent>(OnLifesteal);
            EventBus.Unsubscribe<StatChoiceResolvedEvent>(OnStatChosen);
            EventBus.Unsubscribe<AbilityChoiceResolvedEvent>(OnAbilityChosen);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void PlaySfxAt(AudioClip clip, Vector3 pos, float vol = 1f)
        {
            if (!clip) return;
            var src = NextSfx();
            src.transform.position = pos;
            src.PlayOneShot(clip, vol);
        }

        public void PlaySfx(AudioClip clip, float vol = 1f)
        {
            if (!clip) return;
            _uiSource.PlayOneShot(clip, vol);
        }

        public void SetPoisonAuraLoop(bool active)
        {
            if (active && !_poisonLoop.isPlaying) _poisonLoop.Play();
            else if (!active) _poisonLoop.Stop();
        }

        /// <summary>Called by UIButtonSound on every button click.</summary>
        public void PlayButtonClick() => PlaySfx(Random(buttonClickSounds));

        // ── Volume control (called by AudioSettingsController sliders) ─────────

        /// <summary>
        /// Set a mixer volume from a 0-1 slider value.
        /// The mixer uses dB internally — this converts using the standard
        /// 20*log10 formula. Slider at 0 → -80dB (silence). Slider at 1 → 0dB (full).
        /// </summary>
        public void SetMasterVolume(float sliderValue) => SetMixerVolume(MixerMasterVolume, sliderValue, PrefMaster);
        public void SetMusicVolume (float sliderValue) => SetMixerVolume(MixerMusicVolume,  sliderValue, PrefMusic);
        public void SetSfxVolume   (float sliderValue) => SetMixerVolume(MixerSfxVolume,    sliderValue, PrefSfx);
        public void SetUiVolume    (float sliderValue) => SetMixerVolume(MixerUiVolume,      sliderValue, PrefUi);

        public float GetMasterVolume() => PlayerPrefs.GetFloat(PrefMaster, 1f);
        public float GetMusicVolume()  => PlayerPrefs.GetFloat(PrefMusic,  1f);
        public float GetSfxVolume()    => PlayerPrefs.GetFloat(PrefSfx,    1f);
        public float GetUiVolume()     => PlayerPrefs.GetFloat(PrefUi,     1f);

        private void SetMixerVolume(string param, float sliderValue, string prefKey)
        {
            if (!masterMixer) return;
            // Clamp to a small positive value so log10(0) never happens
            var clamped = Mathf.Clamp(sliderValue, 0.0001f, 1f);
            masterMixer.SetFloat(param, Mathf.Log10(clamped) * 20f);
            PlayerPrefs.SetFloat(prefKey, sliderValue);
        }

        private void LoadVolumes()
        {
            SetMasterVolume(PlayerPrefs.GetFloat(PrefMaster, 1f));
            SetMusicVolume (PlayerPrefs.GetFloat(PrefMusic,  1f));
            SetSfxVolume   (PlayerPrefs.GetFloat(PrefSfx,    1f));
            SetUiVolume    (PlayerPrefs.GetFloat(PrefUi,     1f));
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void OnEnemyDied(EnemyDiedEvent e)
        {
            if (e.IsMiniboss)
                PlaySfxAt(Random(minibossDeathSounds.Length > 0 ? minibossDeathSounds : enemyDeathSounds), e.Position);
            else
                PlaySfxAt(Random(enemyDeathSounds), e.Position);
        }

        private float _prevHealth = float.MaxValue;
        private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
        {
            if (e.Current < _prevHealth) PlaySfx(Random(playerHitSounds));
            _prevHealth = e.Current;
        }

        private void OnPlayerDied(PlayerDiedEvent _) => PlaySfx(playerDeathSound);

        private float _lastXpSound;
        private void OnXpChanged(ExperienceChangedEvent _)
        {
            if (Time.time - _lastXpSound < 0.08f) return;
            _lastXpSound = Time.time;
            PlaySfx(Random(xpPickupSounds), 0.6f);
        }

        private void OnLevelUp(LevelUpEvent _)
        {
            PlaySfx(Random(levelUpSounds));
            PlaySfx(Random(choicePresentedSounds));
        }

        private void OnAbilityCast(AbilityCastEvent e)
        {
            var clips = e.AbilityName switch
            {
                "Shockwave"    => shockwaveSounds,
                "Meteor Slam"  => meteorSlamCastSounds,
                "Laser Beam"   => laserBeamSounds,
                "Cone of Fire" => coneOfFireSounds,
                _              => null
            };
            if (clips != null) PlaySfxAt(Random(clips), e.CastOrigin);
        }

        private void OnShieldBlocked(ShieldBlockedDamageEvent _) => PlaySfx(Random(darkShieldBlockSounds));

        private void OnMeleeHit(MeleeHitEvent e)
        {
            var clips = e.IsCrit && meleeCritSounds.Length > 0 ? meleeCritSounds : meleeHitSounds;
            PlaySfxAt(Random(clips), e.HitPosition);
        }

        private void OnCleaving(CleavingTriggeredEvent e) => PlaySfxAt(Random(cleavingSounds), e.SwingOrigin);
        private void OnLifesteal(LifestealHealEvent e)    => PlaySfxAt(Random(lifestealSounds), e.HitPosition);
        private void OnStatChosen(StatChoiceResolvedEvent _)       => PlaySfx(Random(choiceSelectedSounds));
        private void OnAbilityChosen(AbilityChoiceResolvedEvent _) => PlaySfx(Random(choiceSelectedSounds));

        // ── In-game playlist ──────────────────────────────────────────────────

        private AudioClip CurrentInGameTrack()
        {
            if (inGameTracks == null || inGameTracks.Length == 0) return null;
            return inGameTracks[_inGameTrackIndex % inGameTracks.Length];
        }

        private void PlayNextInGameTrack()
        {
            if (inGameTracks == null || inGameTracks.Length == 0) return;
            _inGameTrackIndex = (_inGameTrackIndex + 1) % inGameTracks.Length;
            Crossfade(CurrentInGameTrack());
        }

        // ── Music crossfade ───────────────────────────────────────────────────

        private void Crossfade(AudioClip clip)
        {
            if (!clip) return;
            if (Active.clip == clip && Active.isPlaying) return;
            StopAllCoroutines();
            StartCoroutine(CrossfadeRoutine(clip));
        }

        private IEnumerator CrossfadeRoutine(AudioClip next)
        {
            var fadeOut = Active;
            var fadeIn  = Inactive;

            fadeIn.clip   = next;
            fadeIn.volume = 0f;
            fadeIn.loop   = false; // playlist drives track changes, not AudioSource.loop
            fadeIn.Play();
            _onA = !_onA;

            var start   = fadeOut.volume;
            var elapsed = 0f;
            while (elapsed < musicCrossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = elapsed / musicCrossfadeDuration;
                fadeOut.volume = Mathf.Lerp(start, 0f, t);
                fadeIn.volume  = Mathf.Lerp(0f,   1f, t);
                yield return null;
            }

            fadeOut.Stop();
            fadeOut.clip  = null;
            fadeIn.volume = 1f;
        }

        // ── Source setup ──────────────────────────────────────────────────────

        private void BuildSources()
        {
            _musicA = Src("MusicA", loop: false, musicMixerGroup);
            _musicB = Src("MusicB", loop: false, musicMixerGroup);

            _sfx = new AudioSource[SfxPool];
            for (var i = 0; i < SfxPool; i++)
                _sfx[i] = Src($"SFX{i}", loop: false, sfxMixerGroup);

            _uiSource   = Src("UI",         loop: false, uiMixerGroup);
            _poisonLoop = Src("PoisonAura", loop: true,  sfxMixerGroup);
            _poisonLoop.clip = poisonAuraLoopClip;
        }

        private AudioSource Src(string n, bool loop, AudioMixerGroup group)
        {
            var go  = new GameObject(n);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop                  = loop;
            src.playOnAwake           = false;
            src.outputAudioMixerGroup = group;
            return src;
        }

        private AudioSource NextSfx()
        {
            var s = _sfx[_sfxIdx];
            _sfxIdx = (_sfxIdx + 1) % SfxPool;
            return s;
        }

        // Returns a random clip from an array, or null if the array is empty/null.
        private static AudioClip Random(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
    }
}
