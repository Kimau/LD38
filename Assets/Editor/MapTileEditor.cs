using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

[CustomEditor(typeof(MapTileType)), CanEditMultipleObjects]
public class MapTileEditor : Editor
{
  public override void OnInspectorGUI()
  {
    if (Event.current.type == EventType.Layout)
    {
      return;
    }

    
    Rect position = new Rect(0,
                             50, // Accounts for Header
                             Screen.width,
                             Screen.height - 32);

    foreach (var item in targets)
    {
      MapTileType mt = item as MapTileType;
      Rect usedRect = InspectMapTile(position, mt);
      position.y += usedRect.height;
    }
  }

  public static Rect InspectMapTile(Rect position, MapTileType mt)
  {
    GUI.changed = false;
    Rect saveOrig = position;

    // Size
    string tileName = EditorGUI.TextField(new Rect(position.x,
                                               position.y,
                                               position.width,
                                               EditorGUIUtility.singleLineHeight),
                                               "Name", mt.tilename);
    position.y += EditorGUIUtility.singleLineHeight;

    Color32 newColor = EditorGUI.ColorField(new Rect(position.x,
                                               position.y,
                                               position.width,
                                               EditorGUIUtility.singleLineHeight),
                                               "Color", mt.color);
    position.y += EditorGUIUtility.singleLineHeight;

    float newMult = EditorGUI.FloatField(new Rect(position.x,
                                               position.y,
                                               position.width,
                                               EditorGUIUtility.singleLineHeight),
                                        "Movement Speed",
                                        mt.moveMult);

    // Set Values
    mt.moveMult = newMult;
    mt.color = newColor;
    mt.tilename = tileName;

    if (GUI.changed)
      EditorUtility.SetDirty(mt);

    return new Rect(saveOrig.x, saveOrig.y, saveOrig.width, EditorGUIUtility.singleLineHeight * 3);
  }

  // Preview Explosion
  public override bool HasPreviewGUI()
  {
    return true;
  }

  public override void OnPreviewGUI(Rect tarRect, GUIStyle background)
  {
    MapTileType mt = target as MapTileType;

    EditorGUI.DrawRect(tarRect, mt.color);

    Rect inner = new Rect(tarRect.x + 5, tarRect.y + 5, tarRect.width - 10, tarRect.height - 10);
    EditorGUI.DrawRect(inner, Color.grey);
    GUI.Label(inner, "x" + mt.moveMult);
  }

  // For Static Thumbnails
  public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int TexWidth, int TexHeight)
  {
    // Debug.Log("New Static Preview"); - Use this to see how often it gets called

    MapTileType mt = target as MapTileType;
    Texture2D staticPreview = new Texture2D(TexWidth, TexHeight);
    
    // Blank Slate
    Color blankCol = new Color(0, 0, 0, 0);
    Color[] colBlock = new Color[TexWidth * TexHeight];
    for (int i = 0; i < colBlock.Length; ++i)
      colBlock[i] = mt.color;
    for (int i = 0; i < TexWidth; ++i)
    {
      colBlock[i] = blankCol;
      colBlock[i+ (TexHeight-1)*TexWidth] = blankCol;
    }
    for (int i = 1; i < TexHeight; ++i)
    {
      colBlock[i*TexWidth] = blankCol;
      colBlock[i * TexWidth-1] = blankCol;
    }

    staticPreview.SetPixels(0, 0, TexWidth, TexHeight, colBlock);
     staticPreview.Apply();
    return staticPreview;
  }
}