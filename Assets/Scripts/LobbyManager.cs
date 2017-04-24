using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{

  public TMPro.TextMeshPro playerSlotStart;
  public TMPro.TextMeshPro playerNum;
  public TMPro.TextMeshPro startCounter;

  string playerText;
  string counterText;

  List<TMPro.TextMeshPro> playerSlots = new List<TMPro.TextMeshPro>();

  const int maxPlayerCount = 256;
  int currPlayerCount = 0;
  GamePlayer[] m_players = new GamePlayer[maxPlayerCount];

  int startClockValue = -1;

  // Use this for initialization
  void Start()
  {
    playerText = playerNum.text;
    counterText = startCounter.text;

    playerNum.SetText(currPlayerCount + playerText);
    startCounter.SetText(counterText + startClockValue);
    startCounter.gameObject.SetActive(false);

    playerSlotStart.gameObject.SetActive(false);
    UpdatePlayerList();    

    TwitchUDPLinker.Sub(handleMsg);
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

    // Admin Commands
    if (msg.msg.badge.Contains("C"))
    {
      bool hasAdminSpoke = true;
      if (msg.msg.content.Contains("!skipcount"))
        AdminSkipCount();
      else
        hasAdminSpoke = false;

      if (hasAdminSpoke)
        return;
    }

    // Player Commands
    var p = GetPlayer(msg.msg.userid);
    if (p != null)
    {
      if (msg.msg.content.Contains("!quit"))
        PlayerQuit(p);

      return;
    }

    // Viewer Commands
    if (msg.msg.content.Contains("!join"))
      PlayerJoin(msg.msg.userid, msg.msg.nick);

    //TwitchUDPLinker.Say("Please ❕join to play");
  }

  public void AdminSkipCount()
  {
    startClockValue = 0;
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
    int i;
    for (i = playerSlots.Count; (i < currPlayerCount) || (i < 4); ++i)
    {
      var newSlot = Instantiate(playerSlotStart, playerSlotStart.transform.parent);
      int x = i % 4;
      int y = i / 4;
      newSlot.rectTransform.position = playerSlotStart.rectTransform.position + 
        new Vector3(x * playerSlotStart.rectTransform.rect.width, 
        -y * playerSlotStart.rectTransform.rect.height, 0);
      newSlot.gameObject.SetActive(true);
      playerSlots.Add(newSlot);
    }

    // Player Names
    for (i = 0; i < currPlayerCount; ++i)
    {
      var slot = playerSlots[i];
      slot.gameObject.SetActive(true);
      slot.text = m_players[i].nick;
    }

    // Min Number
    for (; i < 4; ++i)
    {
      var slot = playerSlots[i];
      slot.gameObject.SetActive(true);
      slot.text = "<color=#FFFFFF66>[Waiting]</color>";
    }

    // Hide Unused
    for (; i < playerSlots.Count; ++i)
      playerSlots[i].gameObject.SetActive(false);

    // Handle Countdown Clock
    if ((startClockValue < 0) && (currPlayerCount >= 4))
      StartCoroutine(StartCountdown());

    playerNum.SetText(currPlayerCount + playerText);
  }

  IEnumerator StartCountdown()
  {
    startClockValue = 60;
    startCounter.SetText(counterText + startClockValue);
    startCounter.gameObject.SetActive(true);

    while (startClockValue > 0)
    {
      yield return new WaitForSeconds(1);
      --startClockValue;
      startCounter.SetText(counterText + startClockValue);
    }

    yield return startGame();
  }

  IEnumerator startGame()
  {
    TwitchUDPLinker.Say("Game starting with " + currPlayerCount + " players.");
    Debug.Log("Start Game");

    var loadOp = SceneLoader.LoadGameAsync();
    loadOp.allowSceneActivation = false;  // Ensures at least one loop
    while (loadOp.isDone == false)
    {
      yield return new WaitForSeconds(1);

      Debug.Log("Progress: " + loadOp.progress);

      // Allow Scene to Finish
      loadOp.allowSceneActivation = true;
    }

    Debug.Log("Progress: " + loadOp.progress);
    Camera.main.gameObject.SetActive(false);

    var twitchGame = FindObjectOfType<TwitchGame>();

    for (int i = 0; i < currPlayerCount; ++i)
      twitchGame.PlayerJoin(m_players[i]);

    twitchGame.GameStart();
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
