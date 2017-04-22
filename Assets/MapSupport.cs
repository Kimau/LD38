using UnityEngine;

[System.Serializable]
public class MapTileType
{
  public Color32 color;
  public string name;
  public float moveMult;
}

[System.Serializable]
public class TileData
{
  public MapTileType type;
  public bool moveUp;
  public bool moveDown;
  public bool moveLeft;
  public bool moveRight;
}

[System.Serializable]
public enum PlayerDoing {
  Dead,
  Cover,
  Standing,
  Walking,
  Running
};

[System.Serializable]
public class GamePlayer
{
  public string userid;
  public string nick;
  public Color32 col;

  public string target;

  public PlayerDoing doingWhat;
  public Vector2 mapPos;
  public Vector2 travelDir;
}
