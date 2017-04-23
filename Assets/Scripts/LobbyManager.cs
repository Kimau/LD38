using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{

  public TextMesh playerSlotStart;
  public TextMesh playerNum;
  public TextMesh startCounter;

  const int maxPlayerCount = 256;
  int currPlayerCount = 0;
  GamePlayer[] m_players = new GamePlayer[maxPlayerCount];

  int clockCounter = -1;

  // Use this for initialization
  void Start()
  {
  }

  // Update is called once per frame
  void Update()
  {

  }



  public void handleMsg(TwitchMsg msg)
  {
    if (msg.cat != 35)
    {
      return;
    }

    // Not a command
    if (msg.msg.content.Contains("!") == false)
    {
      return;
    }

    var p = GetPlayer(msg.msg.userid);
    if (p == null)
    {
      // Not in game
      if (msg.msg.content.Contains("!join"))
        PlayerJoin(msg.msg.userid, msg.msg.nick);
      else
        TwitchUDPLinker.Say("Please ❕join to play");
    }
    else
    {
      if (msg.msg.content.Contains("!quit"))
        PlayerQuit(p);
    }

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

  void UpdatePlayerList()
  {
    playerNum.text = "" + currPlayerCount;

    if (currPlayerCount == 0)
    {
      playerSlotStart.text = "________________";
      return;
    }

    string result = "";

    // Player Names
    for (int i = 0; i < currPlayerCount; ++i)
    {
      if (m_players[i].nick.Length >= 15)
      {
        result += m_players[i].nick.Substring(0, 15);
      }
      else
      {
        result += m_players[i].nick;
        for (int c = m_players[i].nick.Length; c < 15; ++c)
          result += "_";
      }
      result += "\t";

      if (((i + 1) % 4) == 0)
        result += "\n";
    }

    // Min Number
    if (currPlayerCount < 4)
    {
      for (int i = currPlayerCount; i < 4; ++i)
      {
        for (int c = 0; c < 15; ++c)
          result += "_";
        result += "\t";
      }
      result += "\n Minimum 4 Players";
    }

    playerSlotStart.text = result;

    // Handle Countdown Clock
    if ((clockCounter < 0) && (currPlayerCount >= 4))
      StartCoroutine(StartCountdown());
  }

  IEnumerator StartCountdown()
  {
    clockCounter = 60;
    startCounter.text = "" + clockCounter;
    startCounter.gameObject.SetActive(true);

    while (clockCounter > 0)
    {
      yield return new WaitForSeconds(1);
      --clockCounter;
      startCounter.text = "" + clockCounter;
    }

    startGame();
  }

  void startGame()
  {
    TwitchUDPLinker.Say("Game starting with " + currPlayerCount + " players.");

    Debug.Log("Start Game");
  }

  void PlayerJoin(string id, string nick)
  {
    GamePlayer gp = ScriptableObject.CreateInstance<GamePlayer>();
    gp.nick = nick;
    gp.userid = id;
    gp.col = Random.ColorHSV(0, 1, 0.5f, 1, 0.5f, 1);
    
    m_players[currPlayerCount++] = gp;

    // Make Name Take
    UpdatePlayerList();

  }

  void PlayerQuit(GamePlayer p)
  {
    bool isFound = false;
    for (int i = 0; i < currPlayerCount; ++i)
    {
      if (isFound)
        m_players[i - 1] = m_players[i];
      else if (m_players[i] == p)
        isFound = true;
    }

    if (isFound)
      currPlayerCount--;
    else
      Debug.LogError("Cannot find " + p.nick + "to quit");

    UpdatePlayerList();
  }

  // END 
}
