using UnityEngine;

[System.Serializable]
public enum PlayerDoing
{
  Dead,
  Cover,
  Standing,
  Walking,
  Running,
  Attacking
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

  public Weapon weapon;
  public const int maxHealth = 100;
  public int health;
  public int score = 0;
  public float timeTillExplode = -1.0f;
  public float reloadTime = 0.0f;

  public GamePlayer()
  {
    userid = "_notset_";
    nick = "_notset_";
  }
}