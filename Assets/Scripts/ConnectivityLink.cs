using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectivityLink : MonoBehaviour
{
    [SerializeField]
    private string sceneToLoad;

    private void Start()
    {
        GameSettings.Instance.Client.connectivityEvent += ReceiveConnect;
    }

    private bool ReceiveConnect(Connectivity con)
    {
        if (con == Connectivity.CONNECTED)
        {
            SceneManager.LoadScene(sceneToLoad);
        }

        return true;
    }

    public void AttemptConnect()
    {
        if (!GameSettings.Instance.Client.connectEvent.Invoke())
            Debug.Log("Connect event failed.");
    }

    private void OnDestroy()
    {
        GameSettings.Instance.Client.connectivityEvent -= ReceiveConnect;
    }
}
