using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class PlayerGO : MonoBehaviour {

  GamePlayer playerData;
  MaterialPropertyBlock matBlock;

  public Material myMat;

  // Cached
  Renderer m_renderer;

  // Use this for initialization
  void Start()
  {
    m_renderer = GetComponent<Renderer>();
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

    transform.localScale = Vector3.one * 10.0f;

  }

  public void SetPlayerData(ref GamePlayer gp)
  {
    playerData = gp;

    gameObject.name = "Player " + playerData.nick;

    matBlock = new MaterialPropertyBlock();
    matBlock.SetColor("_Color", playerData.col);

    // Set Colour
    m_renderer = GetComponent<Renderer>();
    m_renderer.SetPropertyBlock(matBlock);
  }
}
