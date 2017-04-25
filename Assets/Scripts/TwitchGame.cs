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
  public GameObject emptyAttack;
  const int maxPlayerCount = 256;

  public TMPro.TextMeshPro battleText;

  public int walkSpeed = 5;
  public int runSpeed = 15;
  public float gameSpeed = 1.0f;
  public float minTimeTillDeathSquare = 30.0f;
  public float maxTimeTillDeathSquare = 90.0f;
  public float warningTillDeath = 60.0f;
  public float TimeTillPlayerExplodes = 30.0f;

  public Weapon[] WeaponList;

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

  bool isDebugStandalone = false;
  void debugStandaloneTest()
  {
    isDebugStandalone = true;
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

      if (combatActive)
        return;

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

        UpdatePlayer(dt, p);

        // Skip Out everything else is paused
        if (!gameIsPlaying || combatActive)
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
        if (listOfDeathSquares.Count > 0)
        {
          listOfDeathSquares.ForEach(x => gameMap.m_gridValues[x] = MapTerrain.Danger);
          gridOverlay.UpdateMesh();
        }
      }
    }
    else if (timeTillDeathSquare > -warningTillDeath)
    {
      timeTillDeathSquare -= dt;
      if (timeTillDeathSquare < -warningTillDeath)
      {
        // Make it a Kill Square
        if (listOfDeathSquares.Count > 0)
        {
          playerList.ForEach(x => x.score += listOfDeathSquares.Count);
          listOfDeathSquares.ForEach(x => gameMap.m_gridValues[x] = MapTerrain.Kill);
          gridOverlay.UpdateMesh();
        }

        PickNewDeathSquares();
        timeTillDeathSquare = Random.Range(minTimeTillDeathSquare, maxTimeTillDeathSquare);
      }
    }
  }

  ///////////////////////////////////////////////////////////////////////////////////
  // Player Stuff
  private void UpdatePlayer(float dt, GamePlayer p)
  {
    if (p.doingWhat == PlayerDoing.Dead)
      return;

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
          if (p.timeTillExplode < 3)
          {
            // Move Camera Close
            PlayerCloseup(playerGameObjs.Find(x => x.playerData == p), 4.5f);
          }

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

    // Reloading
    ReloadNow(dt, p);

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

      case PlayerDoing.Attacking:
        if (p.reloadTime > 0.0f)
          return;

        // Is anyone in the square with me
        var PlayersInGrid = playerList.FindAll(x => IsInGrid(x.mapPos, gridX, gridY));
        if (PlayersInGrid.Count == 1)
        {
          // You done leeroyed
          var attack = Instantiate(emptyAttack, transform);
          var pObj = playerGameObjs.Find(x => x.playerData == p);
          attack.transform.position = pObj.transform.position;
          StartCoroutine(KillObject(attack.gameObject, 2.0f));
          p.reloadTime = p.weapon.reloadTime * Random.Range(0.9f,1.1f);
          p.doingWhat = PlayerDoing.Standing;
        }
        else
        {
          StartCoroutine(StartCombatRound(PlayersInGrid));
        }
        break;
    }
  }

  private static void ReloadNow(float dt, GamePlayer p)
  {
    if (p.reloadTime > 0)
    {
      switch (p.doingWhat)
      {
        case PlayerDoing.Dead: break;
        case PlayerDoing.Cover: p.reloadTime -= dt * 0.9f; break;
        case PlayerDoing.Standing: p.reloadTime -= dt; break;
        case PlayerDoing.Walking: p.reloadTime -= dt * 0.5f; break;
        case PlayerDoing.Running: break;
        case PlayerDoing.Attacking: p.reloadTime -= dt * 1.2f; break;
      }
    }
  }

  bool IsInGrid(Vector2 mapPos, int x, int y)
  {
    int sx, sy;
    gameMap.GetGridPoint(mapPos, out sx, out sy);
    return ((sx == x) && (sy == y));
  }

  bool combatActive = false;
  IEnumerator StartCombatRound(List<GamePlayer> fighters)
  {
    if (combatActive)
      yield break;

    combatActive = true;
    UpdateStatusBarOwnership(null);

    // Get Camera into Position
    var origPos = GameObject.FindGameObjectWithTag("CamDefaultPos");
    Vector2 avgPos = fighters[0].mapPos;
    for (int i = 1; i < fighters.Count; i++)
      avgPos += fighters[1].mapPos;
    avgPos = avgPos / fighters.Count;

    Vector3 realAvgPos = transform.TransformPoint(avgPos.x, 0, avgPos.y);
    Vector2 randPos = Random.insideUnitCircle*1.5f;
    Vector3 tarPos  = transform.TransformPoint(avgPos.x + randPos.x*gameMap.subWidth, 0, avgPos.y - gameMap.subHeight) + transform.up * 1.0f;
    yield return StartCoroutine(CameraOrbitRoutine(0.4f, tarPos, realAvgPos, transform.up));

    // 
    UpdateStatusBarOwnership(fighters);
    playerGameObjs.ForEach(px => px.nameTag.gameObject.SetActive(false));
    battleText.gameObject.SetActive(true);
    battleText.SetText("Battle Begins");
    yield return new WaitForSeconds(0.5f);

    // Combat Rounds
    while (fighters.Count > 1)
    {
      // Everyone Who can Attack Does
      for(int i=0; i < fighters.Count; i++)
      {
        var p = fighters[i];
        if((p.doingWhat == PlayerDoing.Attacking) && (p.reloadTime <= 0.0))
        {
          // ATTACK
          int tarI = Random.Range(0, fighters.Count - 1);
          if (tarI >= i)
            tarI += 1;
          var tarP = fighters[tarI];

          int dmg = Random.Range(p.weapon.minDmg, p.weapon.maxDmg);
          p.reloadTime = p.weapon.reloadTime * Random.Range(0.9f, 1.1f);

          // TODO - Terrain Check
          // TODO - Range Check
          // TODO - Status Check

          battleText.SetText(p.nick + " attacks " + tarP.nick + " with " + p.weapon.name + " and deals " + dmg);
          p.score += dmg*10;
          tarP.health -= dmg;
          if(tarP.health <= 0)
          {
            KillPlayer(tarP);
            fighters.Remove(tarP);
          }

          yield return new WaitForSeconds(1.0f);
        }
      }

      // Everyone Reloads
      fighters.ForEach(x => ReloadNow(0.1f, x));

      // Everyone Switches to Attack Stance if They weren't already
      fighters.ForEach(x => x.doingWhat = PlayerDoing.Attacking);

      // FUTURE TODO -- Combat commands
      yield return new WaitForSeconds(0.05f);
    }

    UpdateStatusBarOwnership(null);

    // Return Camera    
    yield return StartCoroutine(CameraSmoothRoutine(0.4f, origPos.transform.position, origPos.transform.rotation));

    playerGameObjs.ForEach(px => px.nameTag.gameObject.SetActive(true));
    UpdateStatusBarOwnership(playerList);

    combatActive = false;
  }

  bool cameraMoving = false;
  IEnumerator CameraSmoothRoutine(float fxTime, Vector3 tPos, Quaternion tRot)
  {
    if (cameraMoving)
      yield break;

    cameraMoving = true;
    Vector3 oldPos = Camera.main.transform.position;
    Quaternion oldRot = Camera.main.transform.rotation;

    for (float f = 0; f < fxTime; f += Time.deltaTime)
    {
      Camera.main.transform.position = Vector3.Lerp(oldPos, tPos, f / fxTime);
      Camera.main.transform.rotation = Quaternion.Lerp(oldRot, tRot, f / fxTime);
      yield return new WaitForEndOfFrame();
    }

    Camera.main.transform.position = tPos;
    Camera.main.transform.rotation = tRot;
    cameraMoving = false;
  }

  IEnumerator CameraOrbitAndBackRoutine(float fxTimeTo, float fxTimeWatch, float fxTimeBack, Vector3 tPos, Vector3 focusPt, Vector3 upVec)
  {
    yield return StartCoroutine(CameraOrbitRoutine(fxTimeTo, tPos, focusPt, upVec));
    yield return new WaitForSecondsRealtime(fxTimeWatch);
    var origPos = GameObject.FindGameObjectWithTag("CamDefaultPos");
    yield return StartCoroutine(CameraSmoothRoutine(fxTimeBack, origPos.transform.position, origPos.transform.rotation));
  }

  IEnumerator CameraOrbitRoutine(float fxTime, Vector3 tPos, Vector3 focusPt, Vector3 upVec)
  {
    if (cameraMoving)
      yield break;

    cameraMoving = true;
    Vector3 oldPos = Camera.main.transform.position;

    for (float f = 0; f < fxTime; f += Time.deltaTime)
    {
      Camera.main.transform.position = Vector3.Lerp(oldPos, tPos, f / fxTime);
      Camera.main.transform.rotation = Quaternion.LookRotation(focusPt - Camera.main.transform.position, upVec);
      yield return new WaitForEndOfFrame();
    }

    Camera.main.transform.position = tPos;
    Camera.main.transform.rotation = Quaternion.LookRotation(focusPt - Camera.main.transform.position, upVec);
    cameraMoving = false;
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
    StartCoroutine(KillObject(blood.gameObject, 2.0f));

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

  string adminMsg = "";
  void OnGUI()
  {
    if (!isDebugStandalone)
      return;

    // Make a background box
    int y = 400;
    GUI.Box(new Rect(10, y, 100, 110), "Fake Message"); y += 25;
    adminMsg = GUI.TextField(new Rect(10, y, 100, 20), adminMsg); y += 25;
    if (GUI.Button(new Rect(20, y, 80, 20), "Send"))
    {
      handleAdminMsg(adminMsg);
    }
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

    var cmdStr = msg.msg.content.ToLower();

    // Admin Commands
    if (msg.msg.badge.Contains("C"))
    {
      if (handleAdminMsg(cmdStr))
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
        if (cmdStr.Contains("!goto"))  // Movement Command
        {
          PlayerGoto(p, msg.msg.content);
        }
        else if (cmdStr.Contains("!move"))  // Movement Command
        {
          PlayerMove(p, msg.msg.content);
        }
        else if (cmdStr.Contains("!walk"))  // Movement Command
        {
          p.doingWhat = PlayerDoing.Walking;
        }
        else if (cmdStr.Contains("!run"))  // Movement Command
        {
          p.doingWhat = PlayerDoing.Running;
        }
        // --------------- ATTACK-----------------------
        else if (cmdStr.Contains("!attack"))  // Movement Command
        {
          p.doingWhat = PlayerDoing.Attacking;
        }
        // 
        else if (cmdStr.Contains("!stop")) // Stop Commmand
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

  private bool handleAdminMsg(string cmdStr)
  {
    bool hasAdminSpoke = true;
    if (cmdStr.Contains("!fastsquare"))
    {
      if (timeTillDeathSquare > 0)
        timeTillDeathSquare = 0.01f;
      else if (timeTillDeathSquare > -warningTillDeath)
        timeTillDeathSquare = -warningTillDeath + 0.01f;
    }
    else if (cmdStr.Contains("!forcebrawl"))
    {
      playerList.ForEach(p =>
      {
        if (p.doingWhat != PlayerDoing.Dead)
          p.doingWhat = PlayerDoing.Attacking;
      });
    }
    else if (cmdStr.Contains("!tp"))
    {
      AdminTeleport(cmdStr);
    }
    else if (cmdStr.Contains("!closeup"))
    {
      CloseUp(cmdStr);
    }
    else
      hasAdminSpoke = false;
    return hasAdminSpoke;
  }

  void AdminTeleport(string cmdStr)
  {
    var m = Regex.Match(cmdStr, "!tp ([0-9\\.a-z]+) ([0-9]+) ([0-9]+)");
    if (!m.Success)
    {
      // Teleport everyone to 4,4
      playerList.ForEach(x => x.mapPos =
      new Vector2(x.mapPos.x % gameMap.subWidth,
                  x.mapPos.y % gameMap.subHeight) +
      new Vector2(4 * gameMap.subWidth,
                  4 * gameMap.subHeight));
      return;
    }

    var p = playerList.Find(x => x.nick == m.Groups[1].Value);
    if (p == null)
    {
      Debug.Log("Cannot Find: " + m.Groups[1].Value);
      return;
    }

    p.mapPos =
      new Vector2(p.mapPos.x % gameMap.subWidth,
                  p.mapPos.y % gameMap.subHeight) +
      new Vector2(int.Parse(m.Groups[2].Value) * gameMap.subWidth,
                  int.Parse(m.Groups[3].Value) * gameMap.subHeight);
  }

  void CloseUp(string cmdStr)
  {
    string tarNick = cmdStr.Substring(cmdStr.IndexOf("!closeup") + "!closeup".Length).Trim();
    if (tarNick.Length < 1)
      return;

    PlayerCloseup(playerGameObjs.Find(x => x.playerData.nick == tarNick), 5.0f);
  }

  void PlayerCloseup(PlayerGO gObj, float timeForCloseup)
  {
    if (gObj == null)
      return;

    Vector3 dirToCenter = transform.TransformPoint(new Vector3(gameMap.width * 0.5f, gameMap.width * 0.5f, gameMap.height * 0.5f)) - gObj.transform.position;
    dirToCenter.y = 0;
    Vector3 newPos = gObj.transform.position - dirToCenter.normalized * 3.7f + transform.up * 2.0f;
    StartCoroutine(CameraOrbitAndBackRoutine(0.5f, timeForCloseup, 0.3f, newPos, gObj.transform.position, transform.up));
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
    const float stupidYhacknumberforstatus = -0.16f;

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

  void UpdateStatusBarOwnership(List<GamePlayer> subList)
  {
    int sBar = 0;
    if (subList != null)
    {
      for (int i = 0; i < subList.Count; i++)
      {
        var p = subList[i];
        if (p.doingWhat != PlayerDoing.Dead)
        {
          playerStatusBars[sBar].gameObject.SetActive(true);
          playerStatusBars[sBar++].SetPlayer(p);
        }
      }
    }

    for(;sBar < playerStatusBars.Count; sBar++)
      playerStatusBars[sBar].gameObject.SetActive(false);
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
    p.weapon = WeaponList[0];

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

    var m = Regex.Match(msgCmd, "!move ([0-9]+)");
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

    var m = Regex.Match(msgCmd, "!goto ([0-9\\.]+) ([0-9\\.]+)");
    if (!m.Success)
    {
      Debug.Log("Move Command Failed: " + msgCmd);
      return;
    }

    Vector2 tarPos = new Vector2(float.Parse(m.Groups[1].Value), float.Parse(m.Groups[2].Value));
    p.tarPos = gameMap.ConvertGridPointToMapPoint(tarPos);

    MarkerAt(p.tarPos, p.col);
  }

  void PlayerStop(GamePlayer p)
  {
    p.doingWhat = PlayerDoing.Cover;
  }


}