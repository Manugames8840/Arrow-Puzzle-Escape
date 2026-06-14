using Framework;
using Framework.Core;
using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class LevelSave : ISaveObject
    {
        public int MaxReachedLevelIndex = 0;

        public int RealLevelIndex = 0;
        public int DisplayLevelIndex = 0;
        public bool IsPlayingRandomLevel = false;

        public int LastPlayerLevelIndex = -1;

        public int CompletedLevelIndex = -1;

        public bool FirstStart = true;

        [System.Serializable]
        public class LevelClearedArrows
        {
            public int levelIndex;
            public System.Collections.Generic.List<string> clearedArrowTails = new System.Collections.Generic.List<string>();
        }

        public System.Collections.Generic.List<LevelClearedArrows> ClearedArrowsPerLevel = new System.Collections.Generic.List<LevelClearedArrows>();

        public bool IsArrowCleared(int levelIndex, Vector2Int tailPos)
        {
            if (ClearedArrowsPerLevel == null)
                ClearedArrowsPerLevel = new System.Collections.Generic.List<LevelClearedArrows>();

            string tailStr = $"{tailPos.x}_{tailPos.y}";
            var entry = ClearedArrowsPerLevel.Find(x => x.levelIndex == levelIndex);
            if (entry != null)
            {
                return entry.clearedArrowTails.Contains(tailStr);
            }
            return false;
        }

        public void ClearArrow(int levelIndex, Vector2Int tailPos)
        {
            if (ClearedArrowsPerLevel == null)
                ClearedArrowsPerLevel = new System.Collections.Generic.List<LevelClearedArrows>();

            string tailStr = $"{tailPos.x}_{tailPos.y}";
            var entry = ClearedArrowsPerLevel.Find(x => x.levelIndex == levelIndex);
            if (entry == null)
            {
                entry = new LevelClearedArrows { levelIndex = levelIndex };
                ClearedArrowsPerLevel.Add(entry);
            }
            if (!entry.clearedArrowTails.Contains(tailStr))
            {
                entry.clearedArrowTails.Add(tailStr);
            }
        }

        public void ResetClearedArrows(int levelIndex)
        {
            if (ClearedArrowsPerLevel == null)
                return;

            var entry = ClearedArrowsPerLevel.Find(x => x.levelIndex == levelIndex);
            if (entry != null)
            {
                ClearedArrowsPerLevel.Remove(entry);
            }
        }

        public void Flush()
        {

        }
    }
}
