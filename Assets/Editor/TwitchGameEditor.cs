using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

public class TwitchGameEditor : Editor
{
  // Creating New Map Tile
  [MenuItem("TwitchGame/Create Map Tile")]
  static void CreateMapTile()
  {
    string path = EditorUtility.SaveFilePanel("Create Map Tile", "Assets/Tiles/", "tile.asset", "asset");
    if (path == "")
      return;

    path = FileUtil.GetProjectRelativePath(path);

    MapTileType mt = CreateInstance<MapTileType>();
    AssetDatabase.CreateAsset(mt, path);
    AssetDatabase.SaveAssets();
  }

  // Creating New Weapon
  [MenuItem("TwitchGame/Create Weapon")]
  static void CreateWeapon()
  {
    string path = EditorUtility.SaveFilePanel("Create Weapon Tile", "Assets/Weapons/", "weapon.asset", "asset");
    if (path == "")
      return;

    path = FileUtil.GetProjectRelativePath(path);

    Weapon weap = CreateInstance<Weapon>();
    AssetDatabase.CreateAsset(weap, path);
    AssetDatabase.SaveAssets();
  }
}