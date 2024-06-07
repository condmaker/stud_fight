using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStart : MonoBehaviour
{
    [SerializeField]
    private string sceneToSwitch;

    private void Start()
    {
        GameSettings.Instance.Client.gameEvent += StartGame;
    }

    private bool StartGame(GameState state, StudMove _)
    {
        if (state != GameState.INTERRUPTED)
            SceneManager.LoadScene(sceneToSwitch);

        return true;
    }

    private void OnDestroy()
    {
        GameSettings.Instance.Client.gameEvent -= StartGame;
    }
}
