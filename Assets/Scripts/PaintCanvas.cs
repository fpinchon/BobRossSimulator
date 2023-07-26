using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintCanvas : MonoBehaviour
{
	public Texture2D texture { get; private set; }
	public new Renderer renderer;

	public Color[] colors;

	public void Init(int resolution)
	{
		texture = new Texture2D(resolution, resolution);
		// texture.filterMode = FilterMode.Point;

		renderer.material.mainTexture = texture;

		FindFirstObjectByType<Terrain>().materialTemplate = renderer.material;
	}

	public void UpdateTexture(LandType[] world)
	{
		for (int i = 0; i < texture.width; i++)
		{
			for (int j = 0; j < texture.height; j++)
			{
				texture.SetPixel(i, j, GetColor(world[i + j * texture.width]));
			}
		}

		texture.Apply();
	}

	private Color GetColor(LandType landType)
	{
		return colors[(int)landType];
	}
}