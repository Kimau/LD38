using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class setMatColour : MonoBehaviour
{
  public Color32 Color = UnityEngine.Color.white;
  public float Cutoff = 0.5f;
  public Texture2D OverrideTex;

  MaterialPropertyBlock matBlock;

  // Use this for initialization
  void Start()
  {
    UpdateValues();
  }

  public void UpdateValues()
  {
    matBlock = new MaterialPropertyBlock();
    matBlock.SetColor("_Color", Color);
    matBlock.SetFloat("_Cutoff", Cutoff);
    if (OverrideTex)
      matBlock.SetTexture("_MainTex", OverrideTex);

    // Set Colour
    var r = GetComponent<Renderer>();
    r.SetPropertyBlock(matBlock);
  }
}
