using Abilities;
using Core;
using Player;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Audio
{
    /// <summary>
    /// Central audio system. Owns every AudioSource in the game.
    /// No other script touches AudioSource directly — all audio is triggered
    /// by listening to EventBus events, same pattern as particle systems.
    ///
    /// STRUCTURE:
    ///   Two looping AudioSources for music (crossfaded so track switches
    ///   fade smoothly). Eight round-robin one-shot sources for SFX so
    ///   overlapping sounds never cancel each other. One dedicated UI source
    ///   and one persistent loop source for Poison Aura.
    ///
    /// SETUP:
    ///   Attach to a persistent [Audio] GameObject (DontDestroyOnLoad).
    ///   Wire AudioClip fields in the Inspector. All clips are optional —
    ///   leave null to silence that category without errors.
    ///   Wire an AudioMixer if you want volume sliders in settings.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // ── Mixer ─────────────────────────────────────────────────────────────
        [Header("Mixer (optional)")]
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup uiMixerGroup;

        // ── Music ─────────────────────────────────────────────────────────────
        [Header("Music")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip inGameMusic;
        [SerializeField] private AudioClip bossMusic;
        [SerializeField][Range(0f, 5f)] private float musicCrossfadeDuration = 1.5f;

        // ── Ability SFX ───────────────────────────────────────────────────────
        [Header("Ability SFX")]
        [SerializeField] private AudioClip shockwaveSound;
        [SerializeField] private AudioClip meteorSlamCastSound;
        [SerializeField] private AudioClip meteorSlamImpactSound;
        [SerializeField] private AudioClip laserBeamSound;
        [SerializeField] private AudioClip coneOfFireSound;
        [SerializeField] private AudioClip darkShieldBlockSound;
        [SerializeField] private AudioClip poisonAuraLoopClip;

        // ── Melee SFX ─────────────────────────────────────────────────────────
        [Header("Melee SFX")]
        [SerializeField] private AudioClip meleeHitSound;
        [SerializeField] private AudioClip meleeCritSound;
        [SerializeField] private AudioClip cleavingSound;
        [SerializeField] private AudioClip lifestealSound;

        // ── Enemy SFX ─────────────────────────────────────────────────────────
        [Header("Enemy SFX")]
        [SerializeField] private AudioClip enemyDeathSound;
        [SerializeField] private AudioClip minibossDeathSound;
        [SerializeField] private AudioClip bossDeathSound;

        // ── Player SFX ────────────────────────────────────────────────────────
        [Header("Player SFX")]
        [SerializeField] private AudioClip playerHitSound;
        [SerializeField] private AudioClip playerDeathSound;
        [SerializeField] private AudioClip xpPickupSound;

        // ── UI SFX ────────────────────────────────────────────────────────────
        [Header("UI SFX")]
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField] private AudioClip choicePresentedSound;
        [SerializeField] private AudioClip choiceSelectedSound;
        [SerializeField] private AudioClip bossPhaseStartStinger;

        // ── Internal ──────────────────────────────────────────────────────────
        private AudioSource _musicA;
        private AudioSource _musicB;
        private bool _onA = true;
        private AudioSource Active   => _onA ? _musicA : _musicB;
        private AudioSource Inactive => _onA ? _musicB : _musicA;

        private AudioSource[] _sfx;
        private int _sfxIdx;
        private const int SfxPool = 8;

        private AudioSource _uiSource;
        private AudioSource _poisonLoop;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSources();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Called by Unity after every scene load completes — including the
        /// initial scene. Switches music based on which scene just loaded.
        /// Doing it here instead of in MenuManager.StartGame or
        /// WaveDirector.Start means the crossfade coroutine always runs on
        /// a fully-loaded scene, never gets killed mid-fade by the scene
        /// transition itself.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Scene 0 = main menu, any other scene = game.
            // Adjust the index if your build order differs.
            if (scene.buildIndex == 0)
                Crossfade(mainMenuMusic);
            else
                Crossfade(inGameMusic);
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

        public void PlayMainMenuMusic() => Crossfade(mainMenuMusic);

        public void PlayInGameMusic()
        {
            // Stop any currently playing music immediately rather than crossfading —
            // when transitioning from menu to game scene instantly there is nothing
            // worth fading from; a crossfade would just cause the menu track to
            // briefly play into the game scene before switching.
            StopAllCoroutines();
            _musicA.Stop();
            _musicB.Stop();
            _onA = true;
            _musicA.clip   = inGameMusic;
            _musicA.volume = 1f;
            _musicA.Play();
        }

        /// <summary>
        /// Play a one-shot SFX at a world position.
        /// Public so ZombieExplodeBehaviour and other direct callers can use it.
        /// </summary>
        public void PlaySfxAt(AudioClip clip, Vector3 pos, float vol = 1f)
        {
            if (!clip) return;
            var src = NextSfx();
            src.transform.position = pos;
            src.PlayOneShot(clip, vol);
        }

        /// <summary>Play a non-positional one-shot (UI, stingers).</summary>
        public void PlaySfx(AudioClip clip, float vol = 1f)
        {
            if (!clip) return;
            _uiSource.PlayOneShot(clip, vol);
        }

        /// <summary>Start/stop the Poison Aura loop. Called by PoisonAuraController.</summary>
        public void SetPoisonAuraLoop(bool active)
        {
            if (active && !_poisonLoop.isPlaying) _poisonLoop.Play();
            else if (!active) _poisonLoop.Stop();
        }

        // ── Event Handlers ────────────────────────────────────────────────────
        private void OnEnemyDied(EnemyDiedEvent e)
        {
            if (e.IsMiniboss)
                PlaySfxAt(minibossDeathSound ? minibossDeathSound : enemyDeathSound, e.Position);
            else
                PlaySfxAt(enemyDeathSound, e.Position);
        }

        private float _prevHealth = float.MaxValue;
        private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
        {
            if (e.Current < _prevHealth) PlaySfx(playerHitSound);
            _prevHealth = e.Current;
        }
        private void OnPlayerDied(PlayerDiedEvent _)    => PlaySfx(playerDeathSound);

        private float _lastXpSound;
        private void OnXpChanged(ExperienceChangedEvent _)
        {
            // Throttle — rapid multi-orb pickups shouldn't spam the sound
            if (Time.time - _lastXpSound < 0.08f) return;
            _lastXpSound = Time.time;
            PlaySfx(xpPickupSound, 0.6f);
        }

        private void OnLevelUp(LevelUpEvent _)
        {
            PlaySfx(levelUpSound);
            PlaySfx(choicePresentedSound);
        }

        private void OnAbilityCast(AbilityCastEvent e)
        {
            var clip = e.AbilityName switch
            {
                "Shockwave"     => shockwaveSound,
                "Meteor Slam"   => meteorSlamCastSound,
                "Laser Beam"    => laserBeamSound,
                "Cone of Fire"  => coneOfFireSound,
                _               => null
            };
            PlaySfxAt(clip, e.CastOrigin);
        }

        private void OnShieldBlocked(ShieldBlockedDamageEvent _) => PlaySfx(darkShieldBlockSound);

        private void OnMeleeHit(MeleeHitEvent e)
        {
            var clip = e.IsCrit && meleeCritSound ? meleeCritSound : meleeHitSound;
            PlaySfxAt(clip, e.HitPosition);
        }

        private void OnCleaving(CleavingTriggeredEvent e) => PlaySfxAt(cleavingSound, e.SwingOrigin);
        private void OnLifesteal(LifestealHealEvent e)    => PlaySfxAt(lifestealSound, e.HitPosition);
        private void OnStatChosen(StatChoiceResolvedEvent _)     => PlaySfx(choiceSelectedSound);
        private void OnAbilityChosen(AbilityChoiceResolvedEvent _) => PlaySfx(choiceSelectedSound);

        // ── Music Crossfade ───────────────────────────────────────────────────

        private void Crossfade(AudioClip clip)
        {
            if (!clip) return;
            if (Active.clip == clip && Active.isPlaying) return;
            StopAllCoroutines();
            StartCoroutine(CrossfadeRoutine(clip));
        }

        private System.Collections.IEnumerator CrossfadeRoutine(AudioClip next)
        {
            var fadeOut = Active;
            var fadeIn  = Inactive;

            fadeIn.clip   = next;
            fadeIn.volume = 0f;
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

        // ── Source Setup ──────────────────────────────────────────────────────

        private void BuildSources()
        {
            _musicA = Src("MusicA", loop: true,  musicMixerGroup);
            _musicB = Src("MusicB", loop: true,  musicMixerGroup);

            _sfx = new AudioSource[SfxPool];
            for (var i = 0; i < SfxPool; i++)
                _sfx[i] = Src($"SFX{i}", loop: false, sfxMixerGroup);

            _uiSource   = Src("UI",          loop: false, uiMixerGroup);
            _poisonLoop = Src("PoisonAura",  loop: true,  sfxMixerGroup);
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
    }
}
