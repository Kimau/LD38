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
  public MapTile[] m_tileData;

  // 
  int[] m_tileType;
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
    m_tileType = new int[width * height];
    m_surfaceTex.filterMode = FilterMode.Point;

    RegenSurface();

    m_renderer.material.mainTexture = m_surfaceTex;
  }

  int NearestMapTile(Color32 col)
  {
    int res = -1;
    int closeDist = 1000000;

    for (int i =0; i < m_tileData.Length; i++)
    {
      Color32 p = m_tileData[i].color;
      int dif =
              (col.r - p.r) * (col.r - p.r) +
              (col.g - p.g) * (col.g - p.g) +
              (col.b - p.b) * (col.b - p.b);

      if(dif < closeDist)
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
      

      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++)
        {
          // EXTREMELY STUPID WAY TODO THIS BUT JAM AND HACKABLE
          Color32 p = m_sourcMap.GetPixel(
            Mathf.FloorToInt((x * invWidth) * m_sourcMap.width),
            Mathf.FloorToInt((y * invHeight) * m_sourcMap.height));

          int mapID = NearestMapTile(p);
          m_tileType[x + y * height] = mapID;
          m_colBuffer[x + y * height] = m_tileData[mapID].color;
        }
      }
    } else
    {
      // Wonky Colour Test
      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++)
        {
          int mapID = Mathf.FloorToInt(Random.value * m_tileData.Length);
          m_tileType[x + y * height] = mapID;
          m_colBuffer[x + y * height] = m_tileData[mapID].color;
        }
      }
    }

    m_surfaceTex.SetPixels32(m_colBuffer);
    m_surfaceTex.Apply(false);
  }

  // Update is called once per frame
  void Update()
  {

  }
}
