using UnityEngine;
using System.Collections.Generic;

public static class ArrowColorPalette
{
	public static readonly Dictionary<int, Color> Colors = new Dictionary<int, Color>()
{
	{1,  new Color(0.8862745f,  0.25882354f, 0.14117648f, 1f)}, // Tomato Red
    {2,  new Color(0.23921569f, 0.41960785f, 0.9137255f,  1f)}, // Royal Blue
    {3,  new Color(0.42352942f, 0.75686276f, 0.19607843f, 1f)}, // Lime Green
    {4,  new Color(1f,          0.7176471f,  0.10980392f, 1f)}, // Amber
    {5,  new Color(0.67058825f, 0.31764707f, 0.8862745f,  1f)}, // Medium Purple
    {6,  new Color(0.93333334f, 0.5254902f,  0.654902f,   1f)}, // Light Pink
    {7,  new Color(0.30980393f, 0.7647059f,  0.87058824f, 1f)}, // Sky Blue
    {8,  new Color(0.09803922f, 0.6431373f,  0.3647059f,  1f)}, // Emerald Green
    {9,  new Color(0.9019608f,  0.44705883f, 0.15686275f, 1f)}, // Orange
    {10, new Color(0.50980395f, 0.6156863f,  0.1882353f,  1f)}, // Olive Green
    {11, new Color(0.27058825f, 0.21176471f, 0.56078434f, 1f)}, // Indigo
    {12, new Color(0.16862746f, 0.19215687f, 0.2509804f,  1f)}, // Charcoal
    {13, new Color(0.47843137f, 0.50980395f, 0.6784314f,  1f)}, // Slate Blue
    {14, new Color(0.6745098f,  0.36078432f, 0.2509804f,  1f)}, // Sienna
    {15, new Color(0.48584908f, 1f,          0.9531593f,  1f)}, // Aqua
    {16, new Color(0.4481132f,  1f,          0.58798754f, 1f)}, // Mint Green
    {17, new Color(0f,          0.7735849f,  0.015811415f,1f)}, // Pure Green
    {18, new Color(0.9645862f,  1f,          0.2783019f,  1f)}, // Lime Yellow
    {19, new Color(0.9333334f,  0.5254902f,  0.654902f,   1f)}, // Rose Pink
    {20, new Color(0.8175695f,  0.4198113f,  1f,          1f)}, // Lavender
    {21, new Color(0.48584908f, 1f,          0.9531593f,  1f)}, // Aqua
    {22, new Color(1f,          0.43867922f, 0.43867922f, 1f)}, // Salmon
    {23, new Color(0.2f, 1f, 0.2f, 1f)}, // Neon Green
    {24, new Color(0.3726415f,  0.42432198f, 1f,          1f)}, // Periwinkle
    {25, new Color(1f,          0.827451f,   0.3372549f,  1f)}, // Light Gold
    {26, new Color(0.9843137f,  0.39215687f, 0.39607844f, 1f)}, // Coral
    {27, new Color(0.9529412f,  0.16470589f, 0.16862746f, 1f)}, // Red
    {28, new Color(0.59607846f, 0.007843138f,0.02745098f, 1f)}, // Crimson
    {29, new Color(0.45882353f, 0.6039216f,  0.79607844f, 1f)}, // Cornflower Blue
    {30, new Color(0.39607844f, 0.5294118f,  0.8392157f,  1f)}, // Cerulean
    {31, new Color(0.30588236f, 0.40784314f, 0.7176471f,  1f)}, // Medium Blue
    {32, new Color(0.16078432f, 0.2509804f,  0.52156866f, 1f)}, // Navy Blue
};

	public static Color GetColor(int id)
	{
		if (Colors.TryGetValue(id, out var color))
			return color;

		Debug.LogWarning($"Color ID {id} not found. Using white.");
		return Color.white;
	}

	/// <summary>
	/// Supports: #RGB, #RRGGBB, #RRGGBBAA, RRGGBB, RRGGBBAA
	/// </summary>
	public static Color HexToColor(string hex)
	{
		if (string.IsNullOrEmpty(hex))
			return Color.white;

		// Unity requires '#'
		if (!hex.StartsWith("#"))
			hex = "#" + hex;

		if (ColorUtility.TryParseHtmlString(hex, out Color color))
			return color;

		Debug.LogWarning($"Invalid HEX color: {hex}");
		return Color.magenta; // obvious debug color
	}
}
