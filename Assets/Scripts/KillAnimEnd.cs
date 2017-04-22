using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillAnimEnd : MonoBehaviour {


  MaterialPropertyBlock matBlock;
  public void SetColour(Color32 col)
  {
    matBlock = new MaterialPropertyBlock();
    matBlock.SetColor("_Color", col);

    // Set Colour
    foreach(var r in GetComponentsInChildren<Renderer>())
    {
      r.SetPropertyBlock(matBlock); 
    }    
  }

  // Use this for initialization
  void Start () {
		
	}

  public void AnimEnd()
  {
    Destroy(gameObject);
  }


}
