using UnityEngine;

[System.Serializable]
public enum PlayerDoing
{
  Dead,
  Cover,
  Standing,
  Walking,
  Running
};

[System.Serializable]
public class GamePlayer : ScriptableObject
{
  public string userid;
  public string nick;
  public Color32 col;

  public Vector2 tarPos;
  public string target;

  public PlayerDoing doingWhat;
  public Vector2 mapPos;
  public Vector2 travelDir;

  public GamePlayer()
  {
    userid = "_notset_";
    nick = "_notset_";
  }
}