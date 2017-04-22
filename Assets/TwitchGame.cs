using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class TwitchGame : MonoBehaviour
{

  public GameObject templatePlayer;
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

  }
  
  public GamePlayer GetPlayer(string twitchID) {
    for(int i=0; i < currPlayerCount; ++i)
    {
      var p = m_players[i];
      if(p.userid == twitchID)
      {
        return p;
      }
    }

    return null;
  }

  public void handleMsg(TwitchMsg msg)
  {
    if(msg.cat != 35)
    {
      return;
    }

    var p = GetPlayer(msg.msg.userid);
    if(p == null)
    {
      // Not in game
      if(msg.msg.content.Contains("!join"))
      {
        PlayerJoin(msg.msg.userid, msg.msg.nick);
      }
    } else
    {
      // Do Stuff for Player


      if (msg.msg.content.Contains("!move"))  // Movement Command
      {
        PlayerMove(p, msg.msg.content);
      } else if (msg.msg.content.Contains("!stop")) // Stop Commmand
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
    gp.mapPos = new Vector2(Random.Range(0, 512.0f), Random.Range(0, 512.0f));

    m_players[currPlayerCount] = gp;
    var newPlayer = Instantiate(templatePlayer, transform);
    newPlayer.GetComponent<PlayerGO>().SetPlayerData(ref m_players[currPlayerCount]);
  }

  void PlayerMove(GamePlayer p, string msgCmd)
  {
    p.doingWhat = PlayerDoing.Walking;

    msgCmd = msgCmd.ToLower();

    var m = Regex.Match(msgCmd, "!move ([0-9]*)");
    if(!m.Success)
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
