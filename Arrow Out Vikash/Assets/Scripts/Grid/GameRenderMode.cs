using UnityEngine;

namespace ArrowOut
{
	public enum GameRenderMode
	{
		LineRenderer2D,      // Simple 2D with LineRenderer
		SpriteMesh2D,        // 2D with sprites and mesh
		Mesh3D,              // Full 3D with tube meshes
		LineRenderer3D       // 3D with LineRenderer (NEW!)
	}

	public enum CameraMode
	{
		Orthographic2D,      // Top-down 2D/3D view
		Perspective3D        // Angled 3D view
	}

	public enum GameType
	{
		Arrow,
		Snake
	}
}