using System;
using System.Collections.Generic;

namespace ArrowOut
{
	[Serializable]
	public class LevelPackJSON
	{
		public List<LevelJSON> levels;
	}

	[Serializable]
	public class LevelJSON
	{
		public int levelNumber;
		public string levelName;
		public int width;
		public int height;
		public float timeLimit;
		public string difficulty;
		public List<ArrowJSON> arrows;
		public List<Vector2JSON> blockers;
		public List<Vector2JSON> holes;
		public List<Vector2JSON> portals;
	}

	[Serializable]
	public class ArrowJSON
	{
		public List<Vector2JSON> body;
	}

	[Serializable]
	public class Vector2JSON
	{
		public int x;
		public int y;
	}
}