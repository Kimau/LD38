using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PlayerGO : MonoBehaviour {

  GamePlayer playerData;
  TMPro.TextMeshPro nameTag;
  float randomBias = 0.0f;
  
  // Use this for initialization
  void Start()
  {
    randomBias = Random.Range(-0.01f, +0.01f);
  }
	
	// Update is called once per frame
	void Update () {
    if(playerData == null)
    {
      Debug.Log("Null Player Data");
      return;
    }
    
    transform.localPosition = new Vector3(
      playerData.mapPos.x,0,
      playerData.mapPos.y);


    float dir = Mathf.PI * 0.5f - Mathf.Atan2(playerData.travelDir.y, playerData.travelDir.x);

    transform.localRotation = Quaternion.Euler(0, Mathf.Rad2Deg * dir, 0);

    transform.localScale = Vector3.one * 10.0f;

    // Always Upside
    if(nameTag)
    {
      nameTag.transform.position = transform.position + new Vector3(0, 0.1f+ randomBias, 0.5f);
      nameTag.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
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

    if(nameTag == null)
    {
      nameTag = GetComponentInChildren<TMPro.TextMeshPro>();
      nameTag.SetText(playerData.nick);
    }
    
  }
}
