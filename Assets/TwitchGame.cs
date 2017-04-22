using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class TwitchGame : MonoBehaviour
{
  public GameObject templatePlayer;
  public MiniMap miniMap;
  public MapTerrain gameMap;
  const int maxPlayerCount = 256;

  public int walkSpeed = 5;
  public int runSpeed = 15;
  public float gameSpeed = 1.0f;

  int currPlayerCount = 0;
  GamePlayer[] m_players;

  // Use this for initialization
  void Start()
  {
    m_players = new GamePlayer[maxPlayerCount];
    Application.runInBackground = true;

    // New Game Logic
    currPlayerCount = 0;
  }

  // Update is called once per frame
  void Update()
  {
    float dt = Time.deltaTime* gameSpeed;

    // Move Players
    for (int i = 0; i < currPlayerCount; ++i)
    {
      var p = m_players[i];
      if(p == null)
      {
        Debug.LogError("Player " + i + "is null");
        continue;
      }

      // Aim at point
      if (p.tarPos.x > 0)
      {
        p.travelDir = p.tarPos - p.mapPos;

        if (p.travelDir.sqrMagnitude < 0.8) {
          Debug.Log("Reached Target " + p.tarPos);
          p.travelDir = Vector2.zero;
          p.tarPos.x = -1;
          if((p.doingWhat == PlayerDoing.Walking) || (p.doingWhat == PlayerDoing.Running))
          {
            p.doingWhat = PlayerDoing.Standing;
          }
        } else
        {
          p.travelDir.Normalize();
        }
      }
      
      // Diffrent Actions
      switch (p.doingWhat)
      {
        case PlayerDoing.Dead:
          break;

        case PlayerDoing.Cover:
          break;

        case PlayerDoing.Standing:
          break;

        case PlayerDoing.Walking:
          {
            float travelDist = dt * walkSpeed;
            for (float f = 0; f < travelDist; f += 1.0f)
            {
              travelDist = stepMovePlayer(p, travelDist);
            }
          }
          break;

        case PlayerDoing.Running:
          {
            float travelDist = dt * runSpeed;
            for (float f = 0; f < travelDist; f += 1.0f)
            {
              travelDist = stepMovePlayer(p, travelDist);
            }
          }
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

    Vector2 np = p.mapPos + relDir;
    if(gameMap.GetMapTile(np).type.moveMult > 0)
    {
      p.mapPos = np;
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
      miniMap.targetPlayer = p;

      // Do Stuff for Player
      if (msg.msg.content.Contains("!goto"))  // Movement Command
      {
        PlayerGoto(p, msg.msg.content);
      }
       else if (msg.msg.content.Contains("!move"))  // Movement Command
      {
        PlayerMove(p, msg.msg.content);
      }
      else if (msg.msg.content.Contains("!walk"))  // Movement Command
      {
        p.doingWhat = PlayerDoing.Walking;
      }
      else if (msg.msg.content.Contains("!run"))  // Movement Command
      {
        p.doingWhat = PlayerDoing.Running;
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
    gp.tarPos.x = -1.0f;

    var newPlayer = Instantiate(templatePlayer, transform);
    newPlayer.GetComponent<PlayerGO>().SetPlayerData(ref gp);

    m_players[currPlayerCount++] = gp;
    miniMap.targetPlayer = gp;
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


  void PlayerGoto(GamePlayer p, string msgCmd)
  {
    p.doingWhat = PlayerDoing.Walking;

    msgCmd = msgCmd.ToLower();

    var m = Regex.Match(msgCmd, "!goto ([0-9]*) ([0-9]*)");
    if (!m.Success)
    {
      Debug.Log("Move Command Failed: " + msgCmd);
      return;
    }

    int x = int.Parse(m.Groups[1].Value);
    int y = int.Parse(m.Groups[2].Value);
    x = Mathf.Clamp(x, 0, gameMap.width-1);
    y = Mathf.Clamp(y, 0, gameMap.height-1);

    p.tarPos = new Vector2(x, y);
    Debug.Log(p.tarPos);
  }

  void PlayerStop(GamePlayer p)
  {
    p.doingWhat = PlayerDoing.Cover;
  }
}
