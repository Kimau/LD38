using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour {

  public GamePlayer player;
  public TMPro.TextMeshPro nameLabel;
  public TMPro.TextMeshPro scoreLabel;
  public setMatColour healthBar;
  public setMatColour weapon;

  // Use this for initialization
  void Start () {
    if (player == null)
      return;

    SetPlayer(player);
  }
	
	// Update is called once per frame
	void Update () {

    // Health Hack :: SetHealth(Mathf.Sin(Time.timeSinceLevelLoad) * 0.5f + 0.5f);

    if (player == null)
      return;

    scoreLabel.SetText(player.score + "");
    SetHealth((float)player.health / (float)GamePlayer.maxHealth);

  }

  void SetHealth(float healthPercent)
  {
    healthBar.transform.localPosition = new Vector3(-0.5f + healthPercent*0.5f, healthBar.transform.localPosition.y, healthBar.transform.localPosition.z);
    healthBar.transform.localScale = new Vector3(healthPercent, 0.2f, 1.0f);
  }

  public void UpdateWeapon()
  {
    if (player.weapon == null)
      weapon.gameObject.SetActive(false);
    else
    {
      weapon.gameObject.SetActive(true);
      weapon.OverrideTex = player.weapon.weaponIcon;
    }
  }

  public void SetPlayer(GamePlayer p)
  {
    player = p;

    nameLabel.color = player.col;
    nameLabel.SetText(player.nick);
    scoreLabel.SetText(player.score + "");

    if (player.weapon == null)
      weapon.gameObject.SetActive(false);
    else
    {
      weapon.gameObject.SetActive(true);
      weapon.OverrideTex = player.weapon.weaponIcon;
    }

  }
}
