using UnityEngine;

/// <summary>
/// Reusable shake preset asset.
/// Create via: Assets → Create → Camera → Shake Preset
/// </summary>
[CreateAssetMenu(fileName = "NewShakePreset", menuName = "Camera/Shake Preset")]
public class CameraShakePreset : ScriptableObject
{
    // ───────────────── GENERAL ─────────────────
    [Tooltip("Friendly label shown in debug logs.")]
    public string label = "Default Shake";

    // ───────────────── TIMING ─────────────────
    [Tooltip("Total duration of the shake in seconds.")]
    [Min(0.01f)]
    public float duration = 0.3f;

    [Tooltip("Delay before the shake starts (useful for chaining).")]
    [Min(0f)]
    public float delay;

    // ───────────────── STRENGTH ─────────────────
    [Tooltip("Maximum positional offset (world units). Use Z = 0 for 2D games.")]
    public Vector3 strength = new Vector3(0.15f, 0.15f, 0f);

    [Tooltip("Maximum rotational offset in degrees per axis.")]
    public Vector3 rotationalStrength = Vector3.zero;

    // ───────────────── SHAPE ─────────────────
    [Tooltip("How the shake intensity fades over its lifetime.\n" +
             "Left = start, Right = end.\n" +
             "Flat 1→1 = constant. 1→0 = fade out. 0→1→0 = punch.")]
    public AnimationCurve envelope = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Tooltip("Vibration frequency (oscillations per second). Higher = more frantic.")]
    [Range(1f, 100f)]
    public float frequency = 25f;

    // ───────────────── NOISE ─────────────────
    [Tooltip("Type of noise used to generate the shake pattern.")]
    public NoiseType noiseType = NoiseType.Perlin;

    [Tooltip("Random seed offset so two identical presets can look different.\n" +
             "Set to 0 for a random seed each time.")]
    public int seedOffset;

    // ───────────────── STACKING ─────────────────
    [Tooltip("If true, triggering this shake while it's already active will restart it.\n" +
             "If false, the new shake stacks additively.")]
    public bool restartIfActive = true;

    public enum NoiseType
    {
        /// <summary>Smooth, organic motion via Perlin noise.</summary>
        Perlin,
        /// <summary>Sharp, jittery random offsets each frame.</summary>
        Random,
        /// <summary>Sine-based — uniform oscillation, good for rumbles.</summary>
        Sine
    }
}
