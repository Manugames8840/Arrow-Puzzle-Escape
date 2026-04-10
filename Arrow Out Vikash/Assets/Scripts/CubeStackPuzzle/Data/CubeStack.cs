using System;

namespace CubeStackPuzzle
{
    /// <summary>
    /// Defines a group of cubes to generate on the board.
    /// All cubes in a CubeStack share the same color and are generated
    /// as a connected cluster.
    /// </summary>
    [Serializable]
    public class CubeStack
    {
        /// <summary>Number of cubes to generate in this group.</summary>
        public int cubeCount = 5;

        /// <summary>Color of every cube in this group.</summary>
        public CubeColor color = CubeColor.Red;
    }
}
