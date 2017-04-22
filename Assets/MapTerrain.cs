using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class MapTerrain : MonoBehaviour
{

  public int width = 512;
  public int height = 512;
  public Texture2D m_sourcMap;
  public MapTileType[] m_tileData;

  // 
  TileData[] m_tiles;
  Color32[] m_colBuffer;
  Texture2D m_surfaceTex;

  // Cached
  Renderer m_renderer;

  // Use this for initialization
  void Start()
  {
    m_renderer = GetComponent<Renderer>();

    m_colBuffer = new Color32[width * height];
    m_surfaceTex = new Texture2D(width, height, TextureFormat.ARGB32, false);
    m_tiles = new TileData[width * height];
    m_surfaceTex.filterMode = FilterMode.Point;

    RegenSurface();

    m_renderer.material.mainTexture = m_surfaceTex;
  }

  public TileData GetMapTile(Vector2 pos)
  {
    int xi = Mathf.FloorToInt(Mathf.Clamp(pos.x, 0, width - 0.2f));
    int yi = Mathf.FloorToInt(Mathf.Clamp(pos.y, 0, height - 0.2f));

    return m_tiles[xi + yi * width];
  }

  int NearestMapTile(Color32 col)
  {
    int res = -1;
    int closeDist = 1000000;

    for (int i = 0; i < m_tileData.Length; i++)
    {
      Color32 p = m_tileData[i].color;
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
          Color32 p = srcPixelData[
            Mathf.FloorToInt((x * invWidth) * m_sourcMap.width) +
            Mathf.FloorToInt((y * invHeight) * m_sourcMap.height) * width
            ];

          int mapID = NearestMapTile(p);

          TileData td = new TileData();
          td.type = m_tileData[mapID];
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
          int mapID = Mathf.FloorToInt(Random.value * m_tileData.Length);

          TileData td = new TileData();
          td.type = m_tileData[mapID];
          m_tiles[x + y * width] = td;
          m_colBuffer[x + y * width] = td.type.color;
        }
      }
    }

    // Do Map Checks
    for (int x = 0; x < width; x++)
    {
      for (int y = 0; y < height; y++)
      {
        TileData td = m_tiles[x + y * width];
        td.moveUp = ((y + 1) < width) && (m_tiles[x + (y + 1) * width].type.moveMult > 0);
        td.moveDown = (y > 0) && (m_tiles[x + (y - 1) * width].type.moveMult > 0);
        td.moveLeft = (x > 0) && (m_tiles[(x - 1) + y * width].type.moveMult > 0);
        td.moveRight = ((x + 1) < width) && (m_tiles[(x + 1) + y * width].type.moveMult > 0);
      }
    }

    m_surfaceTex.SetPixels32(m_colBuffer);
    m_surfaceTex.Apply(false);
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

        Debug.DrawLine(new Vector3(p.x, 1.0f, p.y), new Vector3(np.x, 1.0f, np.y));

        p = np;
        mt = GetMapTile(p);
        i++;
      }

      if (mt.type.moveMult < 0.4)
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
