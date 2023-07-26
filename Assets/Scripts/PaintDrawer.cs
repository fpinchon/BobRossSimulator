using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BiomeObjects
{
	public List<MapObject> objects;
}

public class PaintDrawer : MonoBehaviour
{
	[SerializeField] private int minPenSize = 1;
	[SerializeField] private int maxPenSize = 16;
	[SerializeField] private PaintCanvas _canvas;
	[SerializeField] private LayerMask _canvasLayerMask;
	[SerializeField] private LayerMask _paletteColorLayerMask;
	[SerializeField] private float _maxPaintDistance = 10f;
	[SerializeField] private TerrainManager _terrainManager;
	[SerializeField] private MapObject easel;

	public List<BiomeObjects> biomeObjects = new List<BiomeObjects>();
	private List<MapObject> spawnedObjects = new List<MapObject>();

	private LandType[] world;
	private int worldResolution;

	public float penSizeMultiplier = 0.5f;
	public float canvasHitDistance = 1f;

	private LandType _currentLandTypeDraw = LandType.Grass;

	private bool _withinRange; // if player is within painting distance
	public bool withinRange { get { return _withinRange; } }

	private bool _hoveringOverCanvas; // if within range and cursor is pointed towards the canvas
	public bool hoveringOverCanvas { get { return _hoveringOverCanvas; } }

	private void Start()
	{
		worldResolution = _terrainManager.terrain.terrainData.heightmapResolution;
		world = new LandType[worldResolution * worldResolution];

		_canvas.Init(worldResolution);
		_terrainManager.Init(worldResolution);

		UpdateWorld();
	}

	private void Update()
	{
		if (!CheckSelectColor())
		{
			Draw();
		}

		if (Input.mouseScrollDelta.y > 0)
			penSizeMultiplier += 0.1f;
		else if (Input.mouseScrollDelta.y < 0)
			penSizeMultiplier -= 0.1f;

		penSizeMultiplier = Mathf.Clamp01(penSizeMultiplier);
	}

	private bool CheckSelectColor()
	{
		// Check for hovered palette color
		if (Physics.Raycast(transform.position, transform.forward, out RaycastHit paletteColorTouch, 10f, _paletteColorLayerMask))
		{
			PaletteColor paletteColorObj = paletteColorTouch.collider.gameObject.GetComponent<PaletteColor>();
			if (!paletteColorObj) return true;

			// set hover to true - this will make the color lit up this frame, after which palettecolor will set this back to false
			paletteColorObj.hovered = true;

			// only select if clicked this frame
			if (!Input.GetMouseButtonDown(0)) return true;

			Color drawColor = paletteColorObj.matBaseColor;
			SetLandType(paletteColorObj.landType, drawColor);

			return true;
		}

		return false;
	}

	private void SetLandType(LandType landType, Color canvasDrawColor)
	{
		_currentLandTypeDraw = landType;
	}

	private void Draw()
	{
		float distanceFromCanvas = (_canvas.transform.position - transform.position).magnitude;
		_withinRange = distanceFromCanvas < _maxPaintDistance;
		if (!_withinRange) return;

		bool drawing = Input.GetMouseButton(0);
		_hoveringOverCanvas = Physics.Raycast(transform.position, transform.forward, out RaycastHit _paintTouch, _maxPaintDistance + 5, _canvasLayerMask);

		canvasHitDistance = 2 * _paintTouch.distance / (_maxPaintDistance + 5);
		Debug.Log(canvasHitDistance);

		if (!drawing || !_hoveringOverCanvas)
			return;

		Vector2 touchPosTexCoord = _paintTouch.textureCoord;

		int x = (int)(touchPosTexCoord.x * worldResolution);
		int y = (int)(touchPosTexCoord.y * worldResolution);

		float penSize = Mathf.Lerp(minPenSize, maxPenSize, penSizeMultiplier);

		int penHeight = (int)penSize;
		int penWidth = Mathf.RoundToInt(penSize * 0.75f);

		for (int i = x - penWidth; i < x + penWidth; i++)
		{
			if (i < 0 || i >= worldResolution)
				continue;

			for (int j = y - penHeight; j < y + penHeight; j++)
			{
				if (j < 0 || j >= worldResolution)
					continue;

				float xRatio = ((i - x) / (float)penWidth) * 0.5f + 0.5f;
				float yRatio = ((j - y) / (float)penHeight) * 0.5f + 0.5f;

				if (Vector2.Distance(new Vector2(xRatio, yRatio), Vector2.one * 0.5f) < 0.5f)
				{
					world[i + j * worldResolution] = _currentLandTypeDraw;
				}
			}
		}

		UpdateWorld();
	}

	private Vector2Int WorldToMapPosition(Vector3 mapPosition)
	{
		int x = Mathf.RoundToInt(Mathf.InverseLerp(0, 100, mapPosition.x) * worldResolution);
		int z = Mathf.RoundToInt(Mathf.InverseLerp(0, 100, mapPosition.z) * worldResolution);

		return new Vector2Int(x, z);
	}

	private LandType GetLandTypeAtPosition(Vector2Int position)
	{
		return world[position.x + position.y * worldResolution];
	}

	private Vector3 MapToWorldPosition(Vector2Int mapPosition)
	{
		return new Vector3((int)Mathf.Lerp(0, 100, mapPosition.x / (float)worldResolution), _terrainManager.GetHeightAtPosition(mapPosition), (int)Mathf.Lerp(0, 100, mapPosition.y / (float)worldResolution));
	}

	private void UpdateWorld()
	{
		_canvas.UpdateTexture(world);
		_terrainManager.UpdateTerrain(world);

		// Remove invalid objects
		for (int i = spawnedObjects.Count - 1; i >= 0; i--)
		{
			Vector2Int mapPosition = WorldToMapPosition(spawnedObjects[i].transform.position);

			if (spawnedObjects[i].landType != GetLandTypeAtPosition(mapPosition))
			{
				Destroy(spawnedObjects[i].gameObject);
				spawnedObjects.RemoveAt(i);
				continue;
			}

			if (Mathf.Abs(spawnedObjects[i].transform.position.y - _terrainManager.GetHeightAtPosition(mapPosition)) > 0.01f)
			{
				Destroy(spawnedObjects[i].gameObject);
				spawnedObjects.RemoveAt(i);
				continue;
			}

			if (Mathf.Abs(spawnedObjects[i].transform.position.y - _terrainManager._landTypeHeights[(int)spawnedObjects[i].landType]) > 0.1f)
			{
				Destroy(spawnedObjects[i].gameObject);
				spawnedObjects.RemoveAt(i);
				continue;
			}
		}

		// Place new objects
		int failCount = 0;
		int maxFailCount = 100;

		while (failCount < maxFailCount)
		{
			Vector2Int randomPosition = new Vector2Int(Random.Range(0, worldResolution), Random.Range(0, worldResolution));
			LandType landType = GetLandTypeAtPosition(randomPosition);

			List<MapObject> objects = biomeObjects[(int)landType].objects;

			if (objects.Count == 0)
			{
				failCount++;
				continue;
			}

			MapObject prefab = objects[Random.Range(0, objects.Count)];
			Vector3 spawnPosition = MapToWorldPosition(randomPosition);
			bool objectNearby = false;

			for (int i = 0; i < spawnedObjects.Count; i++)
			{
				if (spawnedObjects[i].landType != landType)
					continue;

				if (Vector3.Distance(spawnedObjects[i].transform.position, spawnPosition) < Mathf.Max(spawnedObjects[i].radius, prefab.radius))
				{
					objectNearby = true;
					break;
				}
			}

			if (objectNearby)
			{
				failCount++;
				continue;
			}

			if (Vector3.Distance(spawnPosition, easel.transform.position) < Mathf.Max(easel.radius, prefab.radius))
			{
				failCount++;
				continue;
			}

			if (Mathf.Abs(spawnPosition.y - _terrainManager._landTypeHeights[(int)landType]) > 0.1f)
			{
				failCount++;
				continue;
			}

			Vector2 offset = Random.insideUnitCircle * 0.5f;
			spawnPosition += new Vector3(offset.x, 0, offset.y);

			Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

			MapObject obj = Instantiate(prefab, spawnPosition, rotation);

			if (landType != LandType.Water)
				obj.gameObject.transform.localScale *= Random.Range(0.75f, 1.25f);

			obj.landType = landType;
			spawnedObjects.Add(obj);
		}
	}
}