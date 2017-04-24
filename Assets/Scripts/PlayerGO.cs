using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PlayerGO : MonoBehaviour
{

  public GamePlayer playerData;
  public TMPro.TextMeshPro explodeTag;
  public TMPro.TextMeshPro nameTag;
  float randomBias = 0.0f;

  // Use this for initialization
  void Start()
  {
    randomBias = Random.Range(-0.01f, +0.01f);
    explodeTag.gameObject.SetActive(false);
  }

  // Update is called once per frame
  void Update()
  {
    if (playerData == null)
    {
      Debug.Log("Null Player Data");
      return;
    }

    if (playerData.doingWhat == PlayerDoing.Dead)
    {
      gameObject.SetActive(false);
      return;
    }

    transform.localPosition = new Vector3(
    playerData.mapPos.x, 0,
    playerData.mapPos.y);


    float dir = Mathf.PI * 0.5f - Mathf.Atan2(playerData.travelDir.y, playerData.travelDir.x);
    transform.localRotation = Quaternion.Euler(0, Mathf.Rad2Deg * dir, 0);
    transform.localScale = Vector3.one * 10.0f;

    // Always Upside
    if (nameTag)
    {
      nameTag.transform.position = transform.position + Camera.main.transform.up * (randomBias + 0.5f);
      nameTag.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
    }

    if (playerData.timeTillExplode > 0)
    {
      explodeTag.gameObject.SetActive(true);
      var explodeText = "" + Mathf.FloorToInt(playerData.timeTillExplode);
      if (explodeTag.text != explodeText)
        explodeTag.SetText(explodeText);
    }
  }

  public void SetPlayerData(ref GamePlayer gp)
  {
    playerData = gp;

    gameObject.name = "Player " + playerData.nick;

    var matCols = GetComponentsInChildren<setMatColour>();
    foreach (var item in matCols)
    {
      item.Color = playerData.col;
      item.UpdateValues();
    }

    nameTag.SetText(playerData.nick);
  }
}
