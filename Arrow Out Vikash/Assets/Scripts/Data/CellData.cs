using System;
using System.Collections.Generic;
using UnityEngine;

public enum CellType { Empty, Arrow, Blocker, Hole, Portal }
public enum ArrowDirection { Up, Down, Left, Right }

[Serializable]
public class CellData
{
	public CellType cellType;
	public ArrowPath arrowPath;   // ONLY for Arrow
}

[Serializable]
public class ArrowPath
{
	public Color color = Color.white;
	public List<Vector2Int> body; // includes head as last
	public int colorId;
}

