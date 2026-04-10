using UnityEngine;

namespace CubeStackPuzzle
{
    /// <summary>
    /// All available cube colors in the puzzle.
    /// </summary>
    public enum CubeColor
    {
        Red,
        Green,
        Blue,
        Yellow,
        Purple,
        Orange,
        Cyan,
        Pink,
        White
    }

    /// <summary>
    /// Maps CubeColor enum values to Unity Colors for rendering.
    /// </summary>
    public static class CubeColorUtility
    {
        public static Color ToUnityColor(CubeColor cubeColor)
        {
            return cubeColor switch
            {
                CubeColor.Red    => new Color(0.90f, 0.22f, 0.20f),
                CubeColor.Green  => new Color(0.20f, 0.78f, 0.35f),
                CubeColor.Blue   => new Color(0.22f, 0.42f, 0.95f),
                CubeColor.Yellow => new Color(1.00f, 0.85f, 0.12f),
                CubeColor.Purple => new Color(0.68f, 0.22f, 0.90f),
                CubeColor.Orange => new Color(1.00f, 0.55f, 0.10f),
                CubeColor.Cyan   => new Color(0.10f, 0.88f, 0.88f),
                CubeColor.Pink   => new Color(1.00f, 0.42f, 0.70f),
                CubeColor.White  => Color.white,
                _                => Color.white,
            };
        }
    }
}
