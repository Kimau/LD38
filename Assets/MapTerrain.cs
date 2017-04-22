using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class MapTerrain : MonoBehaviour
{

  public int width = 512;
  public int height = 512;

  // 
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
    m_surfaceTex.filterMode = FilterMode.Point;

    RegenSurface();

    m_renderer.material.mainTexture = m_surfaceTex;
  }

  void RegenSurface()
  {
    for (int x = 0; x < width; x++)
    {
      for (int y = 0; y < height; y++)
      {
        m_colBuffer[x + y * height] = new Color32(
          (byte)Mathf.FloorToInt(Mathf.Sin(x * Mathf.PI * 2 / width) * 255),
          (byte)Mathf.FloorToInt(Mathf.Cos(y * Mathf.PI * 2 / width) * 255),
          0, 255);
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
