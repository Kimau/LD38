using UnityEngine;

[System.Serializable]
public class TwitchSubMsg
{
  public string userid;
  public string nick;
  public int bits;
  public string badge;
  public string content;
}

[System.Serializable]
public class TwitchMsg
{

  public int time;
  public int cat;
  public string body;
  public TwitchSubMsg msg;

  public static TwitchMsg CreateFromJSON(string jsonString)
  {
    return JsonUtility.FromJson<TwitchMsg>(jsonString);
  }

}
