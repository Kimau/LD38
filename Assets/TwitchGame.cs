using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class TwitchGame : MonoBehaviour
{
  public GameObject templatePlayer;
  public MapTerrain gameMap;
  const int maxPlayerCount = 256;

  int currPlayerCount = 0;
  GamePlayer[] m_players;

  // Use this for initialization
  void Start()
  {
    m_players = new GamePlayer[maxPlayerCount];

    // New Game Logic
    currPlayerCount = 0;
  }

  // Update is called once per frame
  void Update()
  {
    // Move Players
    for (int i = 0; i < currPlayerCount; ++i)
    {
      var p = m_players[i];
      if(p == null)
      {
        Debug.LogError("Player " + i + "is null");
        continue;
      }
      

      switch (p.doingWhat)
      {
        case PlayerDoing.Dead:
          break;

        case PlayerDoing.Cover:
          break;

        case PlayerDoing.Standing:
          break;

        case PlayerDoing.Walking:

          float travelDist = Time.deltaTime * 100;
          for (float f = 0; f < travelDist; f += 1.0f)
          {
            travelDist = stepMovePlayer(p, travelDist);
          }
          break;

        case PlayerDoing.Running:

          break;
      }
    }
  }

  private float stepMovePlayer(GamePlayer p, float travelDist)
  {
    TileData mt = gameMap.GetMapTile(p.mapPos);
    Vector2 relDir = p.travelDir * travelDist * mt.type.moveMult;

    float travelPointsRemain = 0.0f;
    if (relDir.sqrMagnitude > 1) {
      relDir = p.travelDir;
      travelPointsRemain = (travelDist * mt.type.moveMult - 1.0f) / mt.type.moveMult;
    }

    // Move in main direction first
    if (Mathf.Abs(relDir.x) > Mathf.Abs(relDir.y))
    {
      // Do X first
      if (((relDir.x < 0) && (mt.moveLeft || (0 < (relDir.x + Mathf.Repeat(p.mapPos.x, 1.0f))))) ||
           (mt.moveRight || (1 > (relDir.x + Mathf.Repeat(p.mapPos.x, 1.0f))))
           )
      {
        p.mapPos.x += relDir.x;
      }

      // Update Tile to be safe
      mt = gameMap.GetMapTile(p.mapPos);
      if (((relDir.y < 0) && (mt.moveDown || (0 < (relDir.y + Mathf.Repeat(p.mapPos.y, 1.0f))))) ||
           (mt.moveUp || (1 > (relDir.y + Mathf.Repeat(p.mapPos.y, 1.0f))))
           )
      {
        p.mapPos.y += relDir.y;
      }
    }
    else
    {
      // Do Y first
      if (((relDir.y < 0) && (mt.moveDown || (0 < (relDir.y + Mathf.Repeat(p.mapPos.y, 1.0f))))) ||
           (mt.moveUp || (1 > (relDir.y + Mathf.Repeat(p.mapPos.y, 1.0f))))
           )
      {
        p.mapPos.y += relDir.y;
      }

      // Update Tile to be safe
      mt = gameMap.GetMapTile(p.mapPos);
      if (((relDir.x < 0) && (mt.moveLeft || (0 < (relDir.x + Mathf.Repeat(p.mapPos.x, 1.0f))))) ||
             (mt.moveRight || (1 > (relDir.x + Mathf.Repeat(p.mapPos.x, 1.0f))))
             )
      {
        p.mapPos.x += relDir.x;
      }
    }

    return travelPointsRemain;
  }

  public GamePlayer GetPlayer(string twitchID)
  {
    for (int i = 0; i < currPlayerCount; ++i)
    {
      var p = m_players[i];
      if (p.userid == twitchID)
      {
        return p;
      }
    }

    return null;
  }

  public void handleMsg(TwitchMsg msg)
  {
    if (msg.cat != 35)
    {
      return;
    }

    var p = GetPlayer(msg.msg.userid);
    if (p == null)
    {
      // Not in game
      if (msg.msg.content.Contains("!join"))
      {
        PlayerJoin(msg.msg.userid, msg.msg.nick);
      }
    }
    else
    {
      // Do Stuff for Player


      if (msg.msg.content.Contains("!move"))  // Movement Command
      {
        PlayerMove(p, msg.msg.content);
      }
      else if (msg.msg.content.Contains("!stop")) // Stop Commmand
      {
        PlayerStop(p);
      }
    }

  }


  //---------------------------------------------------------------------------------
  // Player Functions
  void PlayerJoin(string id, string nick)
  {
    GamePlayer gp = new GamePlayer();
    gp.nick = nick;
    gp.userid = id;
    gp.col = Random.ColorHSV(0, 1, 0.5f, 1, 0.5f, 1);
    gp.doingWhat = PlayerDoing.Standing;
    gp.mapPos = gameMap.GetRandomMapSpawn();

    var newPlayer = Instantiate(templatePlayer, transform);
    newPlayer.GetComponent<PlayerGO>().SetPlayerData(ref gp);

    m_players[currPlayerCount++] = gp;
  }

  void PlayerMove(GamePlayer p, string msgCmd)
  {
    p.doingWhat = PlayerDoing.Walking;

    msgCmd = msgCmd.ToLower();

    var m = Regex.Match(msgCmd, "!move ([0-9]*)");
    if (!m.Success)
    {
      Debug.Log("Move Command Failed: " + msgCmd);
      return;
    }

    int deg = int.Parse(m.Groups[1].Value);
    float rad = (deg * Mathf.PI / 180.0f);

    p.travelDir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
  }

  void PlayerStop(GamePlayer p)
  {
    p.doingWhat = PlayerDoing.Cover;
  }
}
