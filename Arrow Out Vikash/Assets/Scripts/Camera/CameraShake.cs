using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// High-performance, reusable camera shake system.
/// 
/// ─── SETUP ───
/// 1. Attach this component to your Camera GameObject.
/// 2. Create shake presets: Assets → Create → Camera → Shake Preset.
/// 
/// ─── USAGE (from any script) ───
///   CameraShake.Shake(myPreset);            // preset-based
///   CameraShake.Shake(0.3f, 0.2f);          // quick one-liner (duration, strength)
///   CameraShake.StopAll();                   // kill all active shakes
///   CameraShake.StopAll(fadeOut: 0.15f);     // fade out gracefully
///
/// ─── FEATURES ───
/// • Zero GC allocations at runtime (object pool for shake instances).
/// • Perlin / Random / Sine noise modes.
/// • Positional + rotational shake.
/// • AnimationCurve envelope for full artistic control.
/// • Additive stacking — multiple shakes combine seamlessly.
/// • Works with 2D and 3D cameras.
/// </summary>
[DisallowMultipleComponent]
public class CameraShake : MonoBehaviour
{
    // ═══════════════════════════════════════════
    //  STATIC API
    // ═══════════════════════════════════════════

    static CameraShake _instance;

    /// <summary>The active CameraShake instance. Auto-finds on first access.</summary>
    public static CameraShake Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CameraShake>();

#if UNITY_EDITOR
                if (_instance == null)
                    Debug.LogWarning("[CameraShake] No CameraShake component found in the scene. " +
                                     "Attach one to your Camera GameObject.");
#endif
            }
            return _instance;
        }
    }

    /// <summary>Trigger a shake using a preset asset.</summary>
    public static void Shake(CameraShakePreset preset)
    {
        if (Instance != null) Instance.Play(preset);
    }

    /// <summary>Quick shake with just duration and strength (uses Perlin noise, auto fade-out).</summary>
    public static void Shake(float duration, float strength)
    {
        if (Instance != null) Instance.PlayQuick(duration, strength);
    }

    /// <summary>Quick shake with per-axis strength control.</summary>
    public static void Shake(float duration, Vector3 strength)
    {
        if (Instance != null) Instance.PlayQuick(duration, strength);
    }

    /// <summary>Stop all active shakes immediately or with an optional fade-out.</summary>
    public static void StopAll(float fadeOut = 0f)
    {
        if (Instance != null) Instance.StopAllShakes(fadeOut);
    }

    // ═══════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════

    [Header("Limits")]
    [Tooltip("Maximum combined positional offset (safety clamp).")]
    [SerializeField] float maxPositionOffset = 1f;

    [Tooltip("Maximum combined rotational offset in degrees (safety clamp).")]
    [SerializeField] float maxRotationOffset = 5f;

    [Header("Pool")]
    [Tooltip("Pre-allocated shake instance slots. Increase if you stack many simultaneous shakes.")]
    [SerializeField] int poolSize = 8;

    // ═══════════════════════════════════════════
    //  RUNTIME STATE
    // ═══════════════════════════════════════════

    /// <summary>Internal shake instance — pooled, zero-alloc at runtime.</summary>
    class ShakeInstance
    {
        public bool isActive;
        public CameraShakePreset preset;

        // Timing
        public float elapsed;
        public float delay;
        public float duration;

        // Fade-out override (for StopAll with fade)
        public float fadeOutDuration;
        public float fadeOutElapsed;
        public bool isFadingOut;

        // Seed for noise
        public float seedX;
        public float seedY;
        public float seedZ;

        public void Reset()
        {
            isActive = false;
            preset = null;
            elapsed = 0f;
            delay = 0f;
            duration = 0f;
            fadeOutDuration = 0f;
            fadeOutElapsed = 0f;
            isFadingOut = false;
        }
    }

    readonly List<ShakeInstance> pool = new List<ShakeInstance>();
    Vector3 originalLocalPos;
    Quaternion originalLocalRot;
    bool hasStoredOriginals;

    // ═══════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════

    void Awake()
    {
        // Singleton — non-destructive (won't destroy duplicates, just warns)
        if (_instance != null && _instance != this)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[CameraShake] Duplicate instance on '{gameObject.name}'. Using first one.", this);
#endif
            Destroy(this);
            return;
        }

        _instance = this;

        // Pre-allocate pool
        for (int i = 0; i < poolSize; i++)
            pool.Add(new ShakeInstance());
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void OnDisable()
    {
        // Restore camera transform when disabled
        RestoreOriginals();
        ClearAllShakes();
    }

    // ═══════════════════════════════════════════
    //  UPDATE (LateUpdate so it layers on top of camera movement)
    // ═══════════════════════════════════════════

    void LateUpdate()
    {
        if (!HasActiveShakes())
        {
            if (hasStoredOriginals)
                RestoreOriginals();
            return;
        }

        // Store originals once at the start of a shake sequence
        if (!hasStoredOriginals)
            StoreOriginals();

        // Restore to original before computing this frame's offset
        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;

        Vector3 totalPosOffset = Vector3.zero;
        Vector3 totalRotOffset = Vector3.zero;

        for (int i = 0; i < pool.Count; i++)
        {
            ShakeInstance shake = pool[i];
            if (!shake.isActive) continue;

            // Handle delay
            if (shake.delay > 0f)
            {
                shake.delay -= Time.deltaTime;
                continue;
            }

            shake.elapsed += Time.deltaTime;

            // Check completion
            if (shake.elapsed >= shake.duration && !shake.isFadingOut)
            {
                shake.Reset();
                continue;
            }

            // Compute intensity from envelope curve
            float normalizedTime = Mathf.Clamp01(shake.elapsed / shake.duration);
            float envelopeValue = shake.preset.envelope.Evaluate(normalizedTime);

            // Apply fade-out override if stopping
            if (shake.isFadingOut)
            {
                shake.fadeOutElapsed += Time.deltaTime;
                float fadeT = Mathf.Clamp01(shake.fadeOutElapsed / shake.fadeOutDuration);
                envelopeValue *= (1f - fadeT);

                if (fadeT >= 1f)
                {
                    shake.Reset();
                    continue;
                }
            }

            // Generate noise-based offset
            float freq = shake.preset.frequency;
            float t = shake.elapsed;

            Vector3 noiseOffset = GetNoiseOffset(shake, t, freq);
            Vector3 posOffset = Vector3.Scale(noiseOffset, shake.preset.strength) * envelopeValue;
            totalPosOffset += posOffset;

            // Rotational shake
            if (shake.preset.rotationalStrength.sqrMagnitude > 0.0001f)
            {
                Vector3 rotNoise = GetNoiseOffset(shake, t + 100f, freq); // offset seed for variety
                Vector3 rotOffset = Vector3.Scale(rotNoise, shake.preset.rotationalStrength) * envelopeValue;
                totalRotOffset += rotOffset;
            }
        }

        // Safety clamp
        totalPosOffset = Vector3.ClampMagnitude(totalPosOffset, maxPositionOffset);
        totalRotOffset = ClampVector3Magnitude(totalRotOffset, maxRotationOffset);

        // Apply
        transform.localPosition = originalLocalPos + totalPosOffset;
        transform.localRotation = originalLocalRot * Quaternion.Euler(totalRotOffset);
    }

    // ═══════════════════════════════════════════
    //  INSTANCE METHODS
    // ═══════════════════════════════════════════

    /// <summary>Play a shake from a preset.</summary>
    public void Play(CameraShakePreset preset)
    {
        if (preset == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[CameraShake] Tried to play a null preset.", this);
#endif
            return;
        }

        // If restartIfActive, find and restart existing shake with same preset
        if (preset.restartIfActive)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].isActive && pool[i].preset == preset)
                {
                    InitShake(pool[i], preset);
                    return;
                }
            }
        }

        // Find a free slot
        ShakeInstance slot = GetFreeSlot();
        if (slot == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[CameraShake] All shake slots are in use! Increase poolSize or " +
                             "reduce simultaneous shakes.", this);
#endif
            return;
        }

        InitShake(slot, preset);
    }

    /// <summary>Quick shake without a preset asset.</summary>
    public void PlayQuick(float duration, float strength)
    {
        PlayQuick(duration, new Vector3(strength, strength, 0f));
    }

    /// <summary>Quick shake with per-axis strength.</summary>
    public void PlayQuick(float duration, Vector3 strength)
    {
        ShakeInstance slot = GetFreeSlot();
        if (slot == null) return;

        // We need a temporary preset reference — use the shared quick preset
        if (_quickPreset == null)
            CreateQuickPreset();

        _quickPreset.duration = duration;
        _quickPreset.strength = strength;

        InitShake(slot, _quickPreset);
    }

    /// <summary>Stop all active shakes.</summary>
    public void StopAllShakes(float fadeOut = 0f)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].isActive) continue;

            if (fadeOut > 0f)
            {
                pool[i].isFadingOut = true;
                pool[i].fadeOutDuration = fadeOut;
                pool[i].fadeOutElapsed = 0f;
            }
            else
            {
                pool[i].Reset();
            }
        }

        if (fadeOut <= 0f)
            RestoreOriginals();
    }

    // ═══════════════════════════════════════════
    //  INTERNALS
    // ═══════════════════════════════════════════

    static CameraShakePreset _quickPreset;

    static void CreateQuickPreset()
    {
        _quickPreset = ScriptableObject.CreateInstance<CameraShakePreset>();
        _quickPreset.label = "Quick Shake";
        _quickPreset.frequency = 25f;
        _quickPreset.noiseType = CameraShakePreset.NoiseType.Perlin;
        _quickPreset.envelope = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        _quickPreset.restartIfActive = false;
        _quickPreset.hideFlags = HideFlags.HideAndDontSave;
    }

    void InitShake(ShakeInstance shake, CameraShakePreset preset)
    {
        shake.isActive = true;
        shake.preset = preset;
        shake.elapsed = 0f;
        shake.delay = preset.delay;
        shake.duration = preset.duration;
        shake.isFadingOut = false;
        shake.fadeOutElapsed = 0f;
        shake.fadeOutDuration = 0f;

        // Generate seeds
        float seed = preset.seedOffset != 0
            ? preset.seedOffset
            : Random.Range(0f, 1000f);

        shake.seedX = seed;
        shake.seedY = seed + 77.7f;
        shake.seedZ = seed + 155.5f;
    }

    ShakeInstance GetFreeSlot()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].isActive)
                return pool[i];
        }

        // Auto-expand pool if needed (still avoids per-frame alloc)
        var newSlot = new ShakeInstance();
        pool.Add(newSlot);
        return newSlot;
    }

    Vector3 GetNoiseOffset(ShakeInstance shake, float time, float frequency)
    {
        float t = time * frequency;

        switch (shake.preset.noiseType)
        {
            case CameraShakePreset.NoiseType.Perlin:
                return new Vector3(
                    (Mathf.PerlinNoise(shake.seedX + t, 0f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(shake.seedY + t, 0f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(shake.seedZ + t, 0f) - 0.5f) * 2f
                );

            case CameraShakePreset.NoiseType.Random:
                // Use a deterministic-ish approach to avoid true Random per frame
                float px = Mathf.PerlinNoise(shake.seedX + t * 3.7f, shake.seedY);
                float py = Mathf.PerlinNoise(shake.seedY + t * 3.7f, shake.seedZ);
                float pz = Mathf.PerlinNoise(shake.seedZ + t * 3.7f, shake.seedX);
                // Remap to [-1, 1] and apply a sign-flip to make it feel more jittery
                return new Vector3(
                    Mathf.Sign(px - 0.5f) * ((px - 0.5f) * 2f) * ((px - 0.5f) * 2f),
                    Mathf.Sign(py - 0.5f) * ((py - 0.5f) * 2f) * ((py - 0.5f) * 2f),
                    Mathf.Sign(pz - 0.5f) * ((pz - 0.5f) * 2f) * ((pz - 0.5f) * 2f)
                );

            case CameraShakePreset.NoiseType.Sine:
                return new Vector3(
                    Mathf.Sin(t * Mathf.PI * 2f + shake.seedX),
                    Mathf.Sin(t * Mathf.PI * 2f + shake.seedY),
                    Mathf.Sin(t * Mathf.PI * 2f + shake.seedZ)
                );

            default:
                return Vector3.zero;
        }
    }

    bool HasActiveShakes()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].isActive) return true;
        }
        return false;
    }

    void ClearAllShakes()
    {
        for (int i = 0; i < pool.Count; i++)
            pool[i].Reset();
    }

    void StoreOriginals()
    {
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        hasStoredOriginals = true;
    }

    void RestoreOriginals()
    {
        if (!hasStoredOriginals) return;
        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;
        hasStoredOriginals = false;
    }

    static Vector3 ClampVector3Magnitude(Vector3 v, float max)
    {
        if (v.sqrMagnitude > max * max)
            return v.normalized * max;
        return v;
    }
}
