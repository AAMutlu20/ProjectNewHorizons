using System;
using UnityEngine;

namespace BhanuProductions.NimbleFX
{
    public class SpriteAnimations : MonoBehaviour
    {

        #region Variables
        [Tooltip("Select Effect Required.")]
        public AnimationType animationType;

        [Tooltip("Drop The Object.")]
        public GameObject targettedSprite;



        [Header("Playback Options")]
        [Tooltip("Start glowing automatically on Start().")]
        public bool autoStart = true;

        [Tooltip("Enable looping.")]
        public bool loop = false;

        [Tooltip("Delay between loop cycles.")]
        [Min(0f)]
        public float loopDelay = 0f;

        private float delayTimer = 0f;
        private bool waitingForNextLoop = false;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private Action onComplete;
        private Transform target => targettedSprite.GetComponent<Transform>();
        private SpriteRenderer targetedSprite => targettedSprite.GetComponent<SpriteRenderer>();



        [Header("Bounce Settings")]
        [Tooltip("Time taken to complete one Cycle.")]
        [Range(0.1f, 10f)]
        public float bounceDuration = 0.5f;

        [Tooltip("Scale of Bounce.")]
        [Range(0.1f, 10f)]
        public float bounceScale = 1f;

        private float timer = 0f;
        private bool bouncing = false;



        [Header("Shake Settings")]
        [Tooltip("Time taken to complete one Cycle.")]
        [Range(0.1f, 10f)]
        public float shakeDuration = 0.5f;

        [Tooltip("Intensity of Shake.")]
        [Range(0.1f, 10f)]
        public float shakeIntensity = 0.5f;

        private bool shaking = false;



        [Header("Glow Settings")]
        [Tooltip("Time taken to complete one Cycle.")]
        [Range(0.1f, 10f)]
        public float glowDuration = 1f;
        [Tooltip("Minimum alpha (transparency).")]
        [Range(0f, 1f)]
        public float minAlpha = 0.2f;
        [Tooltip("Maximum alpha (full glow).")]
        [Range(0f, 1f)]
        public float maxAlpha = 1f;
        private Color originalColor;
        private bool glowing = false;


        [Header("Rotation Settings")]
        [Tooltip("Rotation speed in degrees per second.")]
        public float rotationSpeed = 90f;
        private bool isRotating = false;

        [Header("Sprite Sheet Settings")]
        [Tooltip("Drop the rest of sprite sheet")]
        public Sprite[] spriteFrames;
        [Tooltip("Frame rate for Animations.")]
        public float frameRate = 10f; // frames per second

        private float frameTimer;
        private int currentFrame;
        private bool isPlayingSpriteSheet;


        [Header("Scale Effect Settings")]
        [Tooltip("From the rest of sprite sheet")]
        public Vector3 targetScale = Vector3.one * 1.2f;
        public float scaleDuration = 0.5f;
        public bool ResetOriginalSize = false;

        private float scaleTimer;
        private bool scalingUp = true;
        private bool isScaling = false;
        private float loopDelayTimer;
        private bool ScaleOff = true;


        [Header("Elastic Resizing")]
        public Vector3 elasticTargetScale = new Vector3(1.2f, 0.8f, 1f);
        public float elasticDuration = 0.5f;
        public float elasticBounciness = 2.5f; // spring factor

        private Vector3 elasticOriginalScale;
        private float elasticTimer;
        private bool isElasticResizing = false;
        private bool elasticReturning = false;
        private float elasticDelayTimer;
        private bool EROff = false;
        #endregion

        #region Unity Callbacks
        void Start()
        {
            StartEffect();
        }
        void Update()
        {
            UpdateElements();
        }
        private void OnDisable()
        {
            DisposeElements();
        }
        void OnEnable()
        {
            OnEnableElements();
        }


        #endregion

        #region Do Animations

        public void StartEffect()
        {
            switch (animationType)
            {
                case AnimationType.SpriteSheetEffect:
                    if (autoStart)
                        StartSpriteSheet();
                    break;
                case AnimationType.BounceEffect:
                    originalScale = target.localScale;

                    if (autoStart)
                        StartBounce();
                    break;
                case AnimationType.GlowEffect:

                    if (targetedSprite != null)
                        originalColor = targetedSprite.color;

                    if (autoStart)
                        StartGlow();
                    break;
                case AnimationType.ScaleEffect:
                    StartScale();
                    break;
                case AnimationType.ShakeEffect:

                    originalPosition = target.localPosition;

                    if (autoStart)
                        StartShake();
                    break;
                case AnimationType.RotateEffect:

                    if (autoStart)
                        StartRotation();
                    break;
                case AnimationType.ElasticResizing:
                    if (autoStart)
                        StartElasticResize();
                    break;
            }
        }
        void DisposeElements()
        {
            switch (animationType)
            {
                case AnimationType.SpriteSheetEffect:

                    break;
                case AnimationType.BounceEffect:

                    break;
                case AnimationType.GlowEffect:

                    break;
                case AnimationType.ScaleEffect:

                    break;
                case AnimationType.ShakeEffect:

                    break;
                case AnimationType.RotateEffect:

                    break;
                case AnimationType.ElasticResizing:

                    break;
            }
        }
        void UpdateElements()
        {
            switch (animationType)
            {
                case AnimationType.SpriteSheetEffect:
                    if (isPlayingSpriteSheet)
                    {
                        frameTimer += Time.deltaTime;

                        if (frameTimer >= 1f / frameRate)
                        {
                            frameTimer -= 1f / frameRate;
                            currentFrame++;

                            if (currentFrame >= spriteFrames.Length)
                            {
                                if (loop)
                                    currentFrame = 0;
                                else
                                {
                                    isPlayingSpriteSheet = false;
                                    onComplete?.Invoke();
                                    return;
                                }
                            }

                            targetedSprite.sprite = spriteFrames[currentFrame];
                        }
                    }
                    break;
                case AnimationType.BounceEffect:
                    if (!bouncing)
                    {
                        // Handle delay between loops
                        if (waitingForNextLoop)
                        {
                            delayTimer += Time.deltaTime;
                            if (delayTimer >= loopDelay)
                            {
                                delayTimer = 0f;
                                waitingForNextLoop = false;
                                StartBounce();
                            }
                        }
                        return;
                    }

                    timer += Time.deltaTime;
                    float progress = timer / bounceDuration;

                    // Bounce using sine wave
                    float scale = Mathf.LerpUnclamped(1f, bounceScale, Mathf.Sin(progress * Mathf.PI));
                    target.localScale = originalScale * scale;

                    if (progress >= 1f)
                    {
                        target.localScale = originalScale;
                        bouncing = false;

                        if (loop)
                        {
                            waitingForNextLoop = loopDelay > 0f;
                            if (!waitingForNextLoop)
                                StartBounce();
                        }
                        else
                        {
                            onComplete?.Invoke();
                        }
                    }
                    break;
                case AnimationType.GlowEffect:
                    if (!glowing)
                    {
                        if (waitingForNextLoop)
                        {
                            delayTimer += Time.deltaTime;
                            if (delayTimer >= loopDelay)
                            {
                                delayTimer = 0f;
                                waitingForNextLoop = false;
                                StartGlow();
                            }
                        }
                        return;
                    }

                    timer += Time.deltaTime;
                    float t = Mathf.PingPong(timer / glowDuration, 1f); // Ping-pong progress between 0–1
                    float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

                    Color c = targetedSprite.color;
                    c.a = alpha;
                    targetedSprite.color = c;

                    if (!loop && timer >= glowDuration)
                    {
                        // Only run once
                        glowing = false;
                        onComplete?.Invoke();
                    }
                    else if (loop && timer >= glowDuration * 2f) // complete cycle of glow (min->max->min)
                    {
                        timer = 0f;
                        waitingForNextLoop = loopDelay > 0f;
                        glowing = !waitingForNextLoop;
                    }
                    break;
                case AnimationType.ScaleEffect:
                    if (target == null && ScaleOff) return;

                    // Handle delay before next loop
                    if (!isScaling && loop && loopDelayTimer > 0f && !ScaleOff)
                    {
                        loopDelayTimer -= Time.deltaTime;
                        if (loopDelayTimer <= 0f)
                        {
                            isScaling = true;
                            scaleTimer = 0f;
                        }
                        else return;
                    }

                    if (!isScaling && ScaleOff) return;

                    scaleTimer += Time.deltaTime;
                    float s = Mathf.Clamp01(scaleTimer / scaleDuration);

                    // Scale up or down
                    if (scalingUp)
                        target.localScale = Vector3.Lerp(originalScale, targetScale, s);
                    else
                        target.localScale = Vector3.Lerp(targetScale, originalScale, s);

                    // Animation done
                    if (scaleTimer >= scaleDuration)
                    {
                        scaleTimer = 0f;

                        if (loop)
                        {
                            scalingUp = !scalingUp; // Toggle direction
                            isScaling = false;      // Pause for delay
                            loopDelayTimer = loopDelay;
                        }
                        else
                        {
                            target.localScale = originalScale;
                            ScaleOff = true;
                            isScaling = false;
                            onComplete?.Invoke();
                        }
                    }
                    break;
                case AnimationType.ShakeEffect:
                    if (!shaking)
                    {
                        if (waitingForNextLoop)
                        {
                            delayTimer += Time.deltaTime;
                            if (delayTimer >= loopDelay)
                            {
                                delayTimer = 0f;
                                waitingForNextLoop = false;
                                StartShake();
                            }
                        }
                        return;
                    }

                    timer += Time.deltaTime;
                    float progress1 = timer / shakeDuration;
                    float damping = 1f - progress1;

                    // Random offset scaled by intensity and damping
                    Vector3 offset = new Vector3(
                        (UnityEngine.Random.value - 0.5f) * 2f * shakeIntensity * damping,
                        (UnityEngine.Random.value - 0.5f) * 2f * shakeIntensity * damping,
                        0
                    );

                    target.localPosition = originalPosition + offset;

                    if (progress1 >= 1f)
                    {
                        target.localPosition = originalPosition;
                        shaking = false;

                        if (loop)
                        {
                            waitingForNextLoop = loopDelay > 0f;
                            if (!waitingForNextLoop)
                                StartShake();
                        }
                        else
                        {
                            onComplete?.Invoke();
                        }
                    }
                    break;
                case AnimationType.RotateEffect:
                    if (!isRotating)
                    {
                        if (loop && loopDelay > 0)
                        {
                            delayTimer += Time.deltaTime;
                            if (delayTimer >= loopDelay)
                            {
                                delayTimer = 0f;
                            }
                        }
                        return;
                    }

                    target.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

                    // For non-looping: rotate just once and stop
                    if (!loop)
                    {
                        isRotating = false;
                        onComplete?.Invoke();
                    }
                    break;
                case AnimationType.ElasticResizing:
                    if (target == null || EROff) return;

                    if (!isElasticResizing && !EROff)
                    {
                        // Handle loop delay
                        if (loop && elasticDelayTimer > 0f)
                        {
                            elasticDelayTimer -= Time.deltaTime;

                            if (elasticDelayTimer <= 0f)
                            {
                                // Toggle direction and restart
                                elasticReturning = !elasticReturning;
                                isElasticResizing = true;
                                elasticTimer = 0f;
                            }
                        }
                        return;
                    }

                    elasticTimer += Time.deltaTime;
                    float z = Mathf.Clamp01(elasticTimer / elasticDuration);

                    // Elastic easing: overshoots and then returns
                    float spring = Mathf.Sin(z * Mathf.PI * elasticBounciness) * (1f - z);
                    Vector3 target1 = elasticReturning ? elasticOriginalScale : elasticTargetScale;
                    Vector3 resultScale = Vector3.LerpUnclamped(elasticOriginalScale, target1, z) + Vector3.one * spring;

                    target.localScale = resultScale;

                    if (elasticTimer >= elasticDuration)
                    {
                        elasticTimer = 0f;

                        if (loop)
                        {
                            isElasticResizing = false;
                            elasticDelayTimer = loopDelay;
                        }
                        else
                        {
                            StopElasticResize();
                            onComplete?.Invoke();
                        }
                    }
                    else if (!isElasticResizing && elasticDelayTimer > 0)
                    {
                        elasticDelayTimer -= Time.deltaTime;
                        if (elasticDelayTimer <= 0f)
                        {
                            isElasticResizing = true;
                        }
                    }
                    break;
            }
        }
        void OnEnableElements()
        {
            switch (animationType)
            {
                case AnimationType.SpriteSheetEffect:

                    break;
                case AnimationType.BounceEffect:

                    break;
                case AnimationType.GlowEffect:

                    break;
                case AnimationType.ScaleEffect:

                    break;
                case AnimationType.ShakeEffect:

                    break;
                case AnimationType.RotateEffect:

                    break;
                case AnimationType.ElasticResizing:

                    break;
            }
        }

        #endregion

        #region Shake Effects

        public void StartShake(Action onShakeComplete = null)
        {
            timer = 0f;
            shaking = true;
            onComplete = onShakeComplete;
        }

        public void StopShake()
        {
            shaking = false;
            waitingForNextLoop = false;
            target.localPosition = originalPosition;
        }


        #endregion

        #region Bounce effect

        public void StartBounce(Action onBounceComplete = null)
        {
            timer = 0f;
            bouncing = true;
            onComplete = onBounceComplete;
        }
        public void StopBounce()
        {
            bouncing = false;
            waitingForNextLoop = false;
            target.localScale = originalScale;
        }

        #endregion

        #region Glow Effect

        public void StartGlow(Action onGlowComplete = null)
        {
            timer = 0f;
            glowing = true;
            onComplete = onGlowComplete;
        }

        public void StopGlow()
        {
            glowing = false;
            waitingForNextLoop = false;

            // Reset alpha to original
            if (targetedSprite != null)
            {
                Color c = targetedSprite.color;
                c.a = originalColor.a;
                targetedSprite.color = c;
            }
        }


        #endregion

        #region Rotate Effect

        public void StartRotation(Action onFinish = null)
        {
            isRotating = true;
            delayTimer = 0f;
            onComplete = onFinish;
        }

        public void StopRotation()
        {
            isRotating = false;
            delayTimer = 0f;
        }


        #endregion

        #region Sprite Sheet Effect

        public void StartSpriteSheet()
        {
            if (spriteFrames == null || spriteFrames.Length == 0 || targetedSprite == null)
            {
                Debug.LogWarning("Sprite Sheet animation requires sprites and a target renderer.");
                return;
            }

            isPlayingSpriteSheet = true;
            frameTimer = 0f;
            currentFrame = 0;
            targetedSprite.sprite = spriteFrames[0];
        }

        public void StopSpriteSheet()
        {
            isPlayingSpriteSheet = false;
            currentFrame = 0;
            if (targetedSprite != null && spriteFrames.Length > 0)
                targetedSprite.sprite = spriteFrames[0];
        }

        #endregion

        #region Scale Effect

        public void StartScale()
        {
            ScaleOff = false;
            originalScale = target.localScale;
            scaleTimer = 0f;
            loopDelayTimer = 0f;
            isScaling = true;
            scalingUp = true;
        }

        public void StopScale()
        {
            ScaleOff = true;
            isScaling = false;
            if (target != null)
                target.localScale = originalScale;
        }

        #endregion

        #region Elastic Resizing

        public void StartElasticResize()
        {
            EROff = false;
            elasticOriginalScale = target.localScale;
            elasticTimer = 0f;
            elasticDelayTimer = 0f;
            isElasticResizing = true;
            elasticReturning = false;
        }

        public void StopElasticResize()
        {
            EROff = true;
            isElasticResizing = false;
            if (target != null)
                target.localScale = elasticOriginalScale;
        }

        #endregion

        public void StartSelectedEffect()
        {
            switch (animationType)
            {
                case AnimationType.SpriteSheetEffect:
                    if (!isPlayingSpriteSheet)
                        StartSpriteSheet();
                    break;
                case AnimationType.BounceEffect:
                    if (!bouncing)
                        StartBounce();
                    break;
                case AnimationType.GlowEffect:
                    if (!glowing)
                        StartGlow();
                    break;
                case AnimationType.ScaleEffect:
                    if (!isScaling)
                        StartScale();
                    break;
                case AnimationType.ShakeEffect:
                    if (!shaking)
                        StartShake();
                    break;
                case AnimationType.RotateEffect:
                    if (!isRotating)
                        StartRotation();
                    break;
                case AnimationType.ElasticResizing:
                    if (!isElasticResizing)
                        StartElasticResize();
                    break;
            }
        }

        public void StopSelectedEffect()
        {
            switch (animationType)
            {
                case AnimationType.SpriteSheetEffect:
                    StopSpriteSheet();
                    break;
                case AnimationType.BounceEffect:
                    StopBounce();
                    break;
                case AnimationType.GlowEffect:
                    StopGlow();
                    break;
                case AnimationType.ScaleEffect:
                    StopScale();
                    break;
                case AnimationType.ShakeEffect:
                    StopShake();
                    break;
                case AnimationType.RotateEffect:
                    StopRotation();
                    break;
                case AnimationType.ElasticResizing:
                    StopElasticResize();
                    break;
            }
        }


    }

    public enum AnimationType
    {
        SpriteSheetEffect,
        BounceEffect,
        GlowEffect,
        ScaleEffect,
        ShakeEffect,
        RotateEffect,
        ElasticResizing
    }
}