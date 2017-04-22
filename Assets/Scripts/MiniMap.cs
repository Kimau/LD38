using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class MiniMap : MonoBehaviour
{

  public GamePlayer targetPlayer;
  public int width = 16;
  public int height = 16;
  public MapTerrain mapRef;

  Vector2 minipos;

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

    m_renderer.material.mainTexture = m_surfaceTex;

    updateAtPos(mapRef.GetRandomMapSpawn());
  }

  void updateAtPos(Vector2 targetPos)
  {
    if(minipos == targetPos)
    {
      return;
    }

    // Get Target Pos
    minipos = targetPos;    
    var targetTile = mapRef.GetMapTile(targetPos);

    for (int x = 0; x < width; ++x)
    {
      for (int y = 0; y < height; ++y)
      {
        var pos = targetPos + new Vector2(x - width * 0.5f, y - height * 0.5f);
        var tile = mapRef.GetMapTile(pos);

        if (targetTile == tile)
        {
          m_colBuffer[x + y * width] = Color32.Lerp(tile.type.color, Color.magenta, 0.5f);
        }
        else
        {
          m_colBuffer[x + y * width] = tile.type.color;
        }
      }
    }

    m_colBuffer[0] = Color.black;

    m_surfaceTex.SetPixels32(m_colBuffer);
    m_surfaceTex.Apply(false);
  }

  // Update is called once per frame
  void Update()
  {
    if (targetPlayer == null)
    {
      return;
    }

    updateAtPos(targetPlayer.mapPos);
  }
}
