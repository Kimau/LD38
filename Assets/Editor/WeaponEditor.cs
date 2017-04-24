using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

[CustomEditor(typeof(Weapon)), CanEditMultipleObjects]
public class WeaponEditor : Editor
{
  // Preview Explosion
  public override bool HasPreviewGUI()
  {
    return true;
  }

  public override void OnPreviewGUI(Rect tarRect, GUIStyle background)
  {
    Weapon w = target as Weapon;

    if (w.weaponIcon)
    {
      EditorGUI.DrawTextureTransparent(tarRect, w.weaponIcon, ScaleMode.ScaleToFit);
    }


    Color borderCol = Color.black;
    if (w.range > 0)
      borderCol = Color.blue;
    else if (w.range > 3)
      borderCol = Color.green;
    else if (w.range > 15)
      borderCol = Color.red;


    // Damge Boxes
    float dmgBoxSize = (tarRect.width - 5.0f) / (w.maxDmg);
    float dmgBoxHeight = (tarRect.height / 8.0f);

    for (int i = 0; i < w.minDmg; i++)
      EditorGUI.DrawRect(new Rect(tarRect.xMin + dmgBoxSize * 0.1f + (dmgBoxSize * i), tarRect.yMin + 2, dmgBoxSize * 0.9f, dmgBoxHeight), borderCol);

    for (int i = w.minDmg; i < w.maxDmg; i++)
      EditorGUI.DrawRect(new Rect(tarRect.xMin + dmgBoxSize * 0.1f + (dmgBoxSize * i), tarRect.yMin + 2, dmgBoxSize * 0.9f, dmgBoxHeight * 0.5f), borderCol);

    // Label
    GUI.Label(tarRect, w.weaponName);
  }

  // For Static Thumbnails
  public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int TexWidth, int TexHeight)
  {
    // Debug.Log("New Static Preview"); - Use this to see how often it gets called
    Weapon w = target as Weapon;

    // Blank Slate
    Color[] colBlock = new Color[TexWidth * TexHeight];
    Color blankCol = new Color(0, 0, 0, 0);
    for (int i = 0; i < colBlock.Length; ++i)
      colBlock[i] = blankCol;

    if (w.weaponIcon)
    {
      Texture2D tempResize = new Texture2D(w.weaponIcon.width, w.weaponIcon.height);
      Color[] srcCol = w.weaponIcon.GetPixels();
      tempResize.SetPixels(0, 0, w.weaponIcon.width, w.weaponIcon.height, srcCol);
      tempResize.Apply();
      tempResize.Resize(TexWidth, TexHeight);
      tempResize.Apply();
      colBlock = tempResize.GetPixels();
    }

    // Border 
    Color borderCol = Color.black;
    if (w.range > 0)
      borderCol = Color.blue;
    else if (w.range > 3)
      borderCol = Color.green;
    else if (w.range > 15)
      borderCol = Color.red;

    // Draw Border
    for (int i = 0; i < TexWidth; ++i)
    {
      colBlock[i] = borderCol;
      colBlock[i + (TexHeight - 1) * TexWidth] = borderCol;
    }
    for (int i = 1; i < TexHeight; ++i)
    {
      colBlock[i * TexWidth] = borderCol;
      colBlock[i * TexWidth - 1] = borderCol;
    }

    // Damge Boxes
    if (w.maxDmg > 0)
    {      
      int dmgBoxSize = (TexWidth - 5) / (w.maxDmg);
      int dmgBoxHeight = (TexHeight / 8);

      if (dmgBoxSize < 5)
      {
        for (int i = 0; i < w.minDmg; i++)
          for (int x = 1; x < dmgBoxSize; x++)
            for (int y = 0; y < dmgBoxHeight; y++)
              colBlock[2 + (dmgBoxSize * i) + x + (TexHeight - 2 - dmgBoxHeight + y) * TexWidth] = borderCol;

        for (int i = w.minDmg; i < w.maxDmg; i++)
          for (int x = 1; x < dmgBoxSize; x++)
            for (int y = dmgBoxHeight / 2; y < dmgBoxHeight; y++)
              colBlock[2 + (dmgBoxSize * i) + x + (TexHeight - 2 - dmgBoxHeight + y) * TexWidth] = borderCol;
      }
      else
      {
        for (int i = 0; i < w.minDmg; i++)
          for (int x = 1; x < dmgBoxSize; x++)
            for (int y = 0; y < dmgBoxHeight; y++)
              colBlock[2 + (dmgBoxSize * i) + x + (TexHeight - 2 - dmgBoxHeight + y) * TexWidth] = borderCol;

        for (int i = w.minDmg; i < w.maxDmg; i++)
        {
          for (int x = 1; x < dmgBoxSize; x++)
            for (int y = 0; y < dmgBoxHeight; y += dmgBoxHeight - 2)
              colBlock[2 + (dmgBoxSize * i) + x + (TexHeight - 2 - dmgBoxHeight + y) * TexWidth] = borderCol;

          for (int x = 1; x < dmgBoxSize; x += (dmgBoxSize - 2))
            for (int y = 0; y < dmgBoxHeight; y++)
              colBlock[2 + (dmgBoxSize * i) + x + (TexHeight - 2 - dmgBoxHeight + y) * TexWidth] = borderCol;
        }
      }
    }

    //
    Texture2D staticPreview = new Texture2D(TexWidth, TexHeight);
    staticPreview.SetPixels(0, 0, TexWidth, TexHeight, colBlock);
    staticPreview.Apply();
    return staticPreview;
  }
}