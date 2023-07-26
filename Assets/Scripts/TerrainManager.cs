using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LandType
{
	Grass,
	Water,
	Mountain,
	Forest,
	Desert
}

public class TerrainManager : MonoBehaviour
{
	public Terrain terrain;

	public float[] _landTypeHeights;

	private int resolution;

	private float[,] heightMap;

	public void Init(int resolution)
	{
		this.resolution = resolution;

		heightMap = new float[resolution, resolution];
		for (int x = 0; x < resolution; x++)
		{
			for (int y = 0; y < resolution; y++)
			{
				heightMap[x, y] = _landTypeHeights[0];
			}
		}

		terrain.terrainData.SetHeights(0, 0, heightMap);
	}

	public float GetHeight(LandType landType)
	{
		return _landTypeHeights[(int)landType];
	}

	public void UpdateTerrain(LandType[] world)
	{
		float heightSum = 0;
		int checkSum = 0;

		for (int i = 0; i < resolution; i++)
		{
			for (int j = 0; j < resolution; j++)
			{
				heightSum = 0;
				checkSum = 0;

				for (int k = i - 3; k < i + 3; k++)
				{
					if (k < 0 || k >= resolution)
						continue;

					for (int l = j - 3; l < j + 3; l++)
					{
						if (l < 0 || l >= resolution)
							continue;

						heightSum += GetHeight(world[k + l * resolution]);
						checkSum++;
					}
				}

				heightMap[j, i] = Mathf.Lerp(GetHeight(world[i + j * resolution]), heightSum / checkSum, 0.85f) / terrain.terrainData.heightmapScale.y;
			}
		}

		terrain.terrainData.SetHeights(0, 0, heightMap);
	}

	public float GetHeightAtPosition(Vector2Int position)
	{
		return heightMap[position.y, position.x] * terrain.terrainData.heightmapScale.y;
	}
}
