using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class TwitchGame : MonoBehaviour
{
  public GameObject templatePlayer;
  public MiniMap miniMap;
  public KillAnimEnd markerPrefab;
  public KillAnimEnd ping;
  public KillAnimEnd ping3;
  public KillAnimEnd labelMurderStart;
  public KillAnimEnd labelMurderEnd;
  public MapTerrain gameMap;
  public PlayerStatus srcStatus;
  public GameObject deathKnell;
  const int maxPlayerCount = 256;

  public int walkSpeed = 5;
  public int runSpeed = 15;
  public float gameSpeed = 1.0f;
  public float minTimeTillDeathSquare = 30.0f;
  public float maxTimeTillDeathSquare = 90.0f;
  public float warningTillDeath = 60.0f;
  public float TimeTillPlayerExplodes = 30.0f;

  float timeTillDeathSquare = -100000.0f;
  List<int> listOfDeathSquares = new List<int>();


  bool gameIsPlaying = false;
  int safeEndSquare;
  List<PlayerStatus> playerStatusBars = new List<PlayerStatus>();
  List<GamePlayer> playerList = new List<GamePlayer>();

  GridOverlayMap gridOverlay;

  // Use this for initialization
  void Start()
  {
    if (TwitchUDPLinker.Sub(handleMsg) == false)
    {
      // Standalone Test
      debugStandaloneTest();
    }

    transform.localScale = new Vector3(20.0f / gameMap.width, 20.0f / gameMap.width, 20.0f / gameMap.width);

    // Setup Status
    srcStatus.gameObject.SetActive(false);

    // Setup Grid
    gridOverlay = GetComponentInChildren<GridOverlayMap>();
    gridOverlay.gameObject.SetActive(false);
    StartCoroutine(gridOverlay.SetupGrid(gameMap));
  }

  void debugStandaloneTest()
  {
    string[] newfakenick = { "botty", "kimbot", "fakeme", "purplepants", "sillyputty" };
    foreach (var item in newfakenick)
    {
      GamePlayer gp = ScriptableObject.CreateInstance<GamePlayer>();
      gp.nick = item;
      gp.userid = "_" + item;
      gp.col = Random.ColorHSV(0, 1, 0.5f, 1, 0.5f, 1);
      PlayerJoin(gp);
    }

    GameStart();
  }

  // Update is called once per frame
  void Update()
  {
    float dt = 0;

    // Game is Playing
    if (gameIsPlaying)
    {
      dt = Time.deltaTime * gameSpeed;

      // Update Death Square 
      UpdateDeathSquares(dt);

      // Update Players
      for (int i = 0; i < playerList.Count; ++i)
      {
        var p = playerList[i];
        if (p == null)
        {
          Debug.LogError("Player " + i + "is null");
          continue;
        }

        if (p.doingWhat == PlayerDoing.Dead)
          return;

        UpdatePlayer(dt, p);

        if (!gameIsPlaying)
          return;
      }
    }
  }

  ///////////////////////////////////////////////////////////////////////////
  // Grid Stuff
  int GrideScore(int i, int t, bool AddBorder)
  {
    int x = (i % gameMap.gridWidth);
    int y = (i / gameMap.gridWidth);

    int score = 0;

    // Check if all squares around 
    if (AddBorder)
    {
      if (((x - 1) < 0) || (gameMap.m_gridValues[(x - 1) + y * gameMap.gridWidth] == t)) score++;
      if (((y - 1) < 0) || (gameMap.m_gridValues[x + (y - 1) * gameMap.gridWidth] == t)) score++;
      if (((x + 1) >= gameMap.gridHeight) || (gameMap.m_gridValues[(x + 1) + y * gameMap.gridWidth] == t)) score++;
      if (((y + 1) >= gameMap.gridWidth) || (gameMap.m_gridValues[x + (y + 1) * gameMap.gridWidth] == t)) score++;
    }
    else
    {
      if (((x - 1) >= 0) && (gameMap.m_gridValues[(x - 1) + y * gameMap.gridWidth] == t)) score++;
      if (((y - 1) >= 0) && (gameMap.m_gridValues[x + (y - 1) * gameMap.gridWidth] == t)) score++;
      if (((x + 1) < gameMap.gridHeight) && (gameMap.m_gridValues[(x + 1) + y * gameMap.gridWidth] == t)) score++;
      if (((y + 1) < gameMap.gridWidth) && (gameMap.m_gridValues[x + (y + 1) * gameMap.gridWidth] == t)) score++;
    }

    return score;
  }

  bool GrideOppCheck(int i, int t, bool AddBorder)
  {
    int x = (i % gameMap.gridWidth);
    int y = (i / gameMap.gridWidth);

    if (AddBorder)
    {
      if ((((x - 1) < 0) || (gameMap.m_gridValues[(x - 1) + y * gameMap.gridWidth] == t)) &&
          (((x + 1) >= gameMap.gridHeight) || (gameMap.m_gridValues[(x + 1) + y * gameMap.gridWidth] == t)))
        return true;

      if ((((y - 1) < 0) || (gameMap.m_gridValues[x + (y - 1) * gameMap.gridWidth] == t)) &&
          (((y + 1) >= gameMap.gridWidth) || (gameMap.m_gridValues[x + (y + 1) * gameMap.gridWidth] == t)))
        return true;
    }
    else
    {
      if ((((x - 1) >= 0) && (gameMap.m_gridValues[(x - 1) + y * gameMap.gridWidth] == t)) &&
         (((x + 1) < gameMap.gridHeight) && (gameMap.m_gridValues[(x + 1) + y * gameMap.gridWidth] == t)))
        return true;

      if ((((y - 1) >= 0) && (gameMap.m_gridValues[x + (y - 1) * gameMap.gridWidth] == t)) &&
         (((y + 1) < gameMap.gridWidth) && (gameMap.m_gridValues[x + (y + 1) * gameMap.gridWidth] == t)))
        return true;
    }

    return false;
  }

  void PickNewDeathSquares()
  {
    // Choose New Square
    listOfDeathSquares.Clear();

    List<int> potentialSquares = new List<int>();

    // Get List that are Still Walkable
    for (int i = 0; i < gameMap.m_gridValues.Length; i++)
      if (gameMap.m_gridValues[i] == MapTerrain.Safe)
        potentialSquares.Add(i);

    // Remove Safe Square 
    potentialSquares.Remove(safeEndSquare);

    int numOpenSquares = potentialSquares.Count;
    int numSquaresTarget = Mathf.Clamp(numOpenSquares / 3, 1, 3);

    if (potentialSquares.Count == 1)
    {
      listOfDeathSquares.Add(potentialSquares[0]);
      return; // Fuck it no more death squares
    }

    // Select Surrounded Squares
    var killSquares = potentialSquares.FindAll(x => GrideScore(x, MapTerrain.Kill, true) >= 3);
    while (killSquares.Count > 0)
    {
      int i = Random.Range(0, killSquares.Count);
      int gridI = killSquares[i];
      listOfDeathSquares.Add(gridI);
      killSquares.Remove(gridI);
      potentialSquares.Remove(gridI);

      if (listOfDeathSquares.Count >= numSquaresTarget)
        return; // -------------->>>>>
    }

    // Select Partial Squares
    killSquares = potentialSquares.FindAll(x => GrideScore(x, MapTerrain.Kill, true) == 2);
    killSquares.RemoveAll(x => GrideOppCheck(x, MapTerrain.Kill, true)); // Remove grids that have two oppisite only
    while (killSquares.Count > 0)
    {
      int i = Random.Range(0, killSquares.Count);
      int gridI = killSquares[i];
      listOfDeathSquares.Add(gridI);
      killSquares.Remove(gridI);
      potentialSquares.Remove(gridI);

      if (listOfDeathSquares.Count >= numSquaresTarget)
        return; // -------------->>>>>
    }
    // Select ones touching and edge
    killSquares = potentialSquares.FindAll(x => GrideScore(x, MapTerrain.Kill, true) == 1);
    while (killSquares.Count > 0)
    {
      int i = Random.Range(0, killSquares.Count);
      int gridI = killSquares[i];
      listOfDeathSquares.Add(gridI);
      killSquares.Remove(gridI);
      potentialSquares.Remove(gridI);

      if (listOfDeathSquares.Count >= numSquaresTarget)
        return; // -------------->>>>>
    }

    while (potentialSquares.Count > 0)
    {
      int i = Random.Range(0, potentialSquares.Count);
      int gridI = potentialSquares[i];
      listOfDeathSquares.Add(gridI);
      potentialSquares.Remove(gridI);

      if (listOfDeathSquares.Count >= numSquaresTarget)
        return; // -------------->>>>>
    }
  }

  void UpdateDeathSquares(float dt)
  {
    if (timeTillDeathSquare < -warningTillDeath)
    {
      PickNewDeathSquares();
      timeTillDeathSquare = Random.Range(minTimeTillDeathSquare, maxTimeTillDeathSquare);
    }
    else if (timeTillDeathSquare > 0)
    {
      timeTillDeathSquare -= dt;
      if (timeTillDeathSquare < 0)
      {
        // Trigger Warning
        listOfDeathSquares.ForEach(x => gameMap.m_gridValues[x] = 2);
        if (listOfDeathSquares.Count > 0)
          gridOverlay.UpdateMesh();
      }
    }
    else if (timeTillDeathSquare > -warningTillDeath)
    {
      timeTillDeathSquare -= dt;
      if (timeTillDeathSquare < -warningTillDeath)
      {
        // Make it a Kill Square
        listOfDeathSquares.ForEach(x => gameMap.m_gridValues[x] = 3);
        if (listOfDeathSquares.Count > 0)
          gridOverlay.UpdateMesh();

        PickNewDeathSquares();
        timeTillDeathSquare = Random.Range(minTimeTillDeathSquare, maxTimeTillDeathSquare);
      }
    }
  }

  ///////////////////////////////////////////////////////////////////////////////////
  // Player Stuff
  private void UpdatePlayer(float dt, GamePlayer p)
  {
    // Am I in a death Square
    int gridX, gridY;
    int gridVal = gameMap.GetGridPoint(p.mapPos, out gridX, out gridY);
    switch (gridVal)
    {
      case MapTerrain.Normal:
        break;
      case MapTerrain.Safe:
        break;
      case MapTerrain.Danger:
        break;
      case MapTerrain.Kill:
        if (p.timeTillExplode < 0)
        {
          // Start Counter
          p.timeTillExplode = TimeTillPlayerExplodes;
          // TwitchUDPLinker.Say("@" + p.nick + " your going to explode");
        }
        else
        {
          p.timeTillExplode -= dt;
          if (p.timeTillExplode < 0)
          {
            // TODO :: KABOOOM
            KillPlayer(p);
            return;
          }
        }
        break;
    }

    // Aim at point
    if (p.tarPos.x > 0)
    {
      p.travelDir = p.tarPos - p.mapPos;

      if (p.travelDir.sqrMagnitude < 0.8)
      {
        Debug.Log("Reached Target " + p.tarPos);
        p.travelDir = Vector2.zero;
        p.tarPos.x = -1;
        if ((p.doingWhat == PlayerDoing.Walking) || (p.doingWhat == PlayerDoing.Running))
        {
          p.doingWhat = PlayerDoing.Standing;
        }
      }
      else
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

  IEnumerator KillObject(GameObject obj, float seconds)
  {
    yield return new WaitForSecondsRealtime(seconds);
    Destroy(obj);
  }

  private void KillPlayer(GamePlayer p)
  {
    p.health = 0;
    p.doingWhat = PlayerDoing.Dead;

    var blood = Instantiate(deathKnell, transform);
    var pObj = playerGameObjs.Find(x => x.playerData == p);
    blood.transform.position = pObj.transform.position;
    KillObject(blood.gameObject, 2.0f);


    // Check for End of Game
    var AlivePlayers = playerList.FindAll(x => x.health > 0);
    if (AlivePlayers.Count > 1)
      return;

    // GAME END
    gameIsPlaying = false;
    StartCoroutine(SlowGameEnd(AlivePlayers[0]));
  }


  private float stepMovePlayer(GamePlayer p, float travelDist)
  {
    TileData mt = gameMap.GetMapTile(p.mapPos);
    Vector2 relDir = p.travelDir * travelDist * mt.type.moveMult;

    float travelPointsRemain = 0.0f;
    if (relDir.sqrMagnitude > 1)
    {
      relDir = p.travelDir;
      travelPointsRemain = (travelDist * mt.type.moveMult - 1.0f) / mt.type.moveMult;
    }

    Vector2 np = p.mapPos + relDir;
    if (gameMap.GetMapTile(np).type.moveMult > 0)
    {
      p.mapPos = np;
    }

    return travelPointsRemain;
  }

  public GamePlayer GetPlayer(string twitchID)
  {
    return playerList.Find(x => x.userid == twitchID);
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
    if (p != null)
    {
      miniMap.targetPlayer = p;

      if (!gameIsPlaying)
      {
        // Waiting Commands

      }
      else
      {
        if (p.doingWhat == PlayerDoing.Dead)
        {
          // Player Dead
          TwitchUDPLinker.Say("You Ded you do nothing");
          return;
        }

        // --------------- MOVEMENT -----------------------
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
        // --------------- ATTACK-----------------------
        else if (msg.msg.content.Contains("!attack"))  // Movement Command
        {
          PlayerAttack(p, msg.msg.content);
        }
        // 
        else if (msg.msg.content.Contains("!stop")) // Stop Commmand
        {
          PlayerStop(p);
        }
        //
        else
        {
          TwitchUDPLinker.Say("Commands ❕: goto, move, walk, run, attack, stop");
        }
      }
    }

  }

  void MarkerAt(Vector2 pos, Color32 col)
  {
    var marker = Instantiate(markerPrefab, transform);
    marker.transform.localPosition = new Vector3(pos.x, 0, pos.y);
    var kae = marker.GetComponent<KillAnimEnd>();
    kae.SetColour(col);
  }

  void PingAt(Vector2 pos)
  {
    var p = Instantiate(ping, transform);
    p.transform.localPosition = new Vector3(pos.x, 0, pos.y);
  }

  void PingBigAt(Vector2 pos)
  {
    var p = Instantiate(ping3, transform);
    p.transform.localPosition = new Vector3(pos.x, 0, pos.y);
  }

  //--------------------------------------------------------------------------------------------
  public void GameStart()
  {
    StartCoroutine(GameSlowStart());
  }

  IEnumerator GameSlowStart()
  {
    // Setup Players

    // Add One Player per Second
    for (int i = 0; i < playerList.Count; ++i)
    {
      AddPlayerToMap(playerList[i]);
      yield return new WaitForSeconds(1.0f);
    }

    // Show Grid
    gridOverlay.gameObject.SetActive(true);
    yield return StartCoroutine(gridOverlay.AnimateSquaresAtStartOfGame());

    playerStatusBars.ForEach(x => x.gameObject.SetActive(false));

    // Start Camera Murder Message
    Instantiate(labelMurderStart, Camera.main.transform);
    yield return new WaitForSeconds(2.5f);

    playerStatusBars.ForEach(x => x.gameObject.SetActive(true));


    // Pick Safe Square 
    List<int> potentialSquares = new List<int>();
    for (int i = 0; i < gameMap.m_gridValues.Length; i++)
      if (gameMap.m_gridValues[i] == MapTerrain.Safe)
        potentialSquares.Add(i);
    safeEndSquare = potentialSquares[Random.Range(0, potentialSquares.Count)];

    // Hide One Box at a time
    Debug.Log("Slow Start Done");
    gameIsPlaying = true;
  }

  IEnumerator SlowGameEnd(GamePlayer winner)
  {

    var lobbyLoad = SceneLoader.LoadLobbyAsync();
    lobbyLoad.allowSceneActivation = false;

    // Start Camera Murder Message
    var endMsg = Instantiate(labelMurderEnd, Camera.main.transform);
    endMsg.GetComponentInChildren<TMPro.TextMeshPro>().SetText(winner.nick);
    yield return new WaitForSeconds(9.5f);

    while (lobbyLoad.isDone == false)
    {
      lobbyLoad.allowSceneActivation = true;
      yield return new WaitForSeconds(0.1f);
    }
    SceneLoader.MakeLobbyActive();

  }

  //---------------------------------------------------------------------------------
  // Player Functions
  public void PlayerJoin(GamePlayer p)
  {
    const float stupidYhacknumberforstatus = -0.26f;

    // Setup Status Bar
    var newStatus = Instantiate(srcStatus, srcStatus.transform.parent);
    var ps = newStatus.GetComponent<PlayerStatus>();
    ps.SetPlayer(p);

    ps.transform.localPosition = srcStatus.transform.localPosition +
      new Vector3(0, playerList.Count * stupidYhacknumberforstatus, 0);
    ps.gameObject.SetActive(false);
    playerStatusBars.Add(ps);

    // Setup Player Details
    p.health = GamePlayer.maxHealth;

    // Add to Player List
    playerList.Add(p);
  }

  List<PlayerGO> playerGameObjs = new List<PlayerGO>();
  void AddPlayerToMap(GamePlayer p)
  {
    // Initial State
    p.doingWhat = PlayerDoing.Standing;
    p.mapPos = gameMap.GetRandomMapSpawn();
    p.tarPos.x = -1.0f;
    p.health = GamePlayer.maxHealth;
    p.timeTillExplode = -1.0f;

    // Add to Mapp
    var newPlayer = Instantiate(templatePlayer, transform);
    var pGo = newPlayer.GetComponent<PlayerGO>();
    pGo.SetPlayerData(ref p);
    playerGameObjs.Add(pGo);

    // Highlight
    PingAt(p.mapPos);

    var ps = playerStatusBars.Find(x => x.player == p);
    ps.gameObject.SetActive(true);

    miniMap.targetPlayer = p;
  }

  void PlayerMove(GamePlayer p, string msgCmd)
  {
    p.doingWhat = PlayerDoing.Walking;
    p.tarPos.x = -1;

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

    var m = Regex.Match(msgCmd, "!goto ([0-9\\.]*) ([0-9\\.]*)");
    if (!m.Success)
    {
      Debug.Log("Move Command Failed: " + msgCmd);
      return;
    }

    Vector2 tarPos = new Vector2(float.Parse(m.Groups[1].Value), float.Parse(m.Groups[2].Value));
    p.tarPos = gameMap.ConvertGridPointToMapPoint(tarPos);

    MarkerAt(p.tarPos, p.col);
  }

  void PlayerAttack(GamePlayer p, string msgCmd)
  {
    msgCmd = msgCmd.ToLower();

  }

  void PlayerStop(GamePlayer p)
  {
    p.doingWhat = PlayerDoing.Cover;
  }


}