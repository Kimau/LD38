using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {

  
  const int lobbyBuildIndex = 1;
  const int gameBuildIndex = 2;


  // Use this for initialization
  void Start () {
    var lobbyScene = SceneManager.GetSceneByBuildIndex(lobbyBuildIndex);
    var gameScene = SceneManager.GetSceneByBuildIndex(gameBuildIndex);

    if (lobbyScene.isLoaded == false)
      SceneManager.LoadScene(lobbyBuildIndex, LoadSceneMode.Additive);

    if (gameScene.isLoaded)
      SceneManager.UnloadSceneAsync(gameScene);
  }

  public static AsyncOperation LoadGameAsync()
  {
    return SceneManager.LoadSceneAsync(gameBuildIndex, LoadSceneMode.Additive);
}

  public static void MakeGameActive()
  {
    var gameScene = SceneManager.GetSceneByBuildIndex(gameBuildIndex);
    SceneManager.SetActiveScene(gameScene);

    var lobbyScene = SceneManager.GetSceneByBuildIndex(lobbyBuildIndex);
    SceneManager.UnloadSceneAsync(lobbyScene);
  }

}
