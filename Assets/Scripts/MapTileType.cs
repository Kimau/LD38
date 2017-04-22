using UnityEngine;

[System.Serializable]
public class MapTileType : ScriptableObject
{
  public Color32 color;
  public string tilename;
  public float moveMult;

  public MapTileType()
  {
    color = Color.blue;
    tilename = "placeholder";
    moveMult = 1.0f;
  }
}