using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class MapTerrain : MonoBehaviour
{

  public const int Hidden = -1;
  public const int Normal = 0;
  public const int Safe = 1;
  public const int Danger = 2;
  public const int Kill = 3;


  public int width = 1024;
  public int height = 1024;
  public int subWidth = 64;
  public int subHeight = 64;
  public int gridWidth;
  public int gridHeight;


  public Texture2D m_sourcMap;
  public MapTileType[] m_sourceTileAssets;

  // 
  public TileData[] m_tiles;
  public int[] m_gridValues;

  Color32[] m_colBuffer;
  Texture2D m_surfaceTex;

  // Cached
  Renderer m_renderer;

  // Use this for initialization
  void Start()
  {
    gridWidth = width / subWidth;
    gridHeight = width / subWidth;

    m_renderer = GetComponent<Renderer>();

    m_colBuffer = new Color32[width * height];
    m_surfaceTex = new Texture2D(width, height, TextureFormat.ARGB32, false);

    m_tiles = new TileData[width * height];
    m_gridValues = new int[gridWidth * gridHeight];
    m_surfaceTex.filterMode = FilterMode.Point;


    RegenSurface();
    SetupGridSquares();

    m_renderer.material.mainTexture = m_surfaceTex;
  }

  public TileData GetMapTile(Vector2 pos)
  {
    int xi = Mathf.FloorToInt(Mathf.Clamp(pos.x, 0, width - 0.2f));
    int yi = Mathf.FloorToInt(Mathf.Clamp(pos.y, 0, height - 0.2f));

    return m_tiles[xi + yi * width];
  }

  public int GetGridPoint(Vector2 pos, out int x, out int y)
  {
    x = Mathf.FloorToInt(pos.x / subWidth);
    y = Mathf.FloorToInt(pos.y / subHeight);

    if ((x < 0) || (x >= gridWidth) || (y < 0) || (y >= gridHeight))
      return -1;

    return m_gridValues[x + y * gridWidth];
  }

  public Vector2 ConvertGridPointToMapPoint(Vector2 pos)
  {
    return new Vector2(pos.x * subWidth, pos.y * subHeight);
  }

  int NearestMapTile(Color32 col)
  {
    int res = -1;
    int closeDist = 1000000;

    for (int i = 0; i < m_sourceTileAssets.Length; i++)
    {
      Color32 p = m_sourceTileAssets[i].color;
      int dif =
              (col.r - p.r) * (col.r - p.r) +
              (col.g - p.g) * (col.g - p.g) +
              (col.b - p.b) * (col.b - p.b);

      if (dif < closeDist)
      {
        closeDist = dif;
        res = i;
      }
    }

    return res;
  }


  void SetupGridSquares()
  {
    // Eval Square
    for (int x = 0; x < gridWidth; x++)
    {
      for (int y = 0; y < gridHeight; y++)
      {
        bool walkable = false;

        for (int sx = 0; (!walkable) && (sx < subWidth); sx++)
        {
          for (int sy = 0; (!walkable) && (sy < subHeight); sy++)
          {
            int offset = (x * subWidth + sx) + (y * subHeight + sy) * width;
            if (m_tiles[offset].type.moveMult > 0)
              walkable = true;
          }
        }

        m_gridValues[x + y * gridWidth] = (walkable) ? MapTerrain.Safe : MapTerrain.Kill;
      }
    }

  }

  void RegenSurface()
  {
    if (m_sourcMap)
    {
      float invWidth = 1.0f / (float)(width);
      float invHeight = 1.0f / (float)(height);

      Color[] srcPixelData = m_sourcMap.GetPixels();

      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++)
        {
          // EXTREMELY STUPID WAY TODO THIS BUT JAM AND HACKABLE
          int sx = Mathf.FloorToInt((x * invWidth) * m_sourcMap.width);
          int sy = Mathf.FloorToInt((y * invHeight) * m_sourcMap.height);
          Color32 p = srcPixelData[sx + sy * m_sourcMap.width];

          int mapID = NearestMapTile(p);

          TileData td = new TileData();
          td.type = m_sourceTileAssets[mapID];
          m_tiles[x + y * width] = td;
          m_colBuffer[x + y * width] = td.type.color;
        }
      }


    }
    else
    {
      // Wonky Colour Test
      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++)
        {
          int mapID = Mathf.FloorToInt(Random.value * m_sourceTileAssets.Length);

          TileData td = new TileData();
          td.type = m_sourceTileAssets[mapID];
          m_tiles[x + y * width] = td;
          m_colBuffer[x + y * width] = td.type.color;
        }
      }
    }

    m_surfaceTex.SetPixels32(m_colBuffer);
    m_surfaceTex.Apply(false);

    Debug.Log("Map Regerenated");
  }

  public Vector2 GetRandomMapSpawn()
  {
    for (int stoplock = 0; stoplock < 100; ++stoplock)
    {
      Vector2 p = new Vector2(Random.value * width, Random.value * height);

      int i = 0;
      var mt = GetMapTile(p);
      while ((i < 100) && mt.type.moveMult < 0.4)
      {
        // Move Inland
        Vector2 np = new Vector2(
          (p.x - width * 0.5f) * 0.9f + width * 0.5f,
         (p.y - height * 0.5f) * 0.9f + height * 0.5f);

        // Debug.Log("Attempting Spawn:" + np);

        p = np;
        mt = GetMapTile(p);
        i++;
      }

      if (mt.type.moveMult > 0.4)
      {
        return p;
      }

      p = new Vector2(Random.value * width, Random.value * height);
    }

    Debug.LogError("Failed to place so doing Random");
    return new Vector2(Random.value * width, Random.value * height);
  }

  // Update is called once per frame
  void Update()
  {

  }
}
