using UnityEngine;


[System.Serializable]
public class Weapon : ScriptableObject
{
  public string weaponName;
  public int minDmg;
  public int maxDmg;

  public int range; // In Tiles

  public Texture2D weaponIcon;

  public Weapon()
  {

  }
}