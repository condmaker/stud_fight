using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectivityModel : MonoBehaviour
{
    [SerializeField]
    private bool activateMsgOnConEvent = true;

    [SerializeField]
    private TextMeshProUGUI text;
    [SerializeField]
    private Image msgImg;
    [SerializeField]
    private TextMeshProUGUI msgObjTxt;
    [SerializeField]
    private Image buttonImg;
    [SerializeField]
    private TextMeshProUGUI buttonTxt;

    // Start is called before the first frame update
    void Start()
    {
        GameSettings.Instance.Client.connectivityEvent += UpdateConnectivity;
        GameSettings.Instance.Client.connectEvent      += Connect;
        GameSettings.Instance.Client.clientEvent       += UpdateClient;

        UpdateConnectivity(GameSettings.Instance.Client.CurrentStatus);
    }

    // Update is called once per frame
    bool UpdateConnectivity(Connectivity connectivity)
    {
        switch (connectivity)
        {
            case Connectivity.CONNECTED:
                text.color = Color.green;
                text.text = "CONNECTED";
                break;
            case Connectivity.CONNECTING:
                text.color = Color.gray;
                text.text = "CONNECTING";
                if (activateMsgOnConEvent)
                    ActivateMsgBox("Attempting connection...");
                break;
            case Connectivity.DISCONNECTED:
            default:
                text.color = Color.red;
                text.text = "DISCONNECTED";
                if (activateMsgOnConEvent) 
                    ActivateMsgBox("No response from server.");
                break;
        }

        return true;
    }

    bool UpdateClient(ClientState clientState)
    {
        switch (clientState)
        {
            case ClientState.READY:
                ActivateMsgBox("Waiting for another player...");
                break;
            case ClientState.OFF:
                ActivateMsgBox("No response from server.");
                break;
            case ClientState.WIN:
                ActivateMsgBox("You won the game.");
                break;
            case ClientState.DEAD:
            default:
                ActivateMsgBox("You lost the game.");
                break;
        }

        return true;
    }

    public void ActivateMsgBox(string message)
    {
        if (msgImg == null) return;

        msgImg.enabled    = true;
        msgObjTxt.enabled = true;
        buttonImg.enabled = true;
        buttonTxt.enabled = true;

        msgObjTxt.text = message;
    }

    // Use this later
    public void DeactivateMsgBox()
    {
        msgImg.enabled    = false;
        msgObjTxt.enabled = false;
        buttonImg.enabled = false;
        buttonTxt.enabled = false;
    }

    public bool Connect() =>
        GameSettings.Instance.Client.Connect();

    private void OnDestroy()
    {
        GameSettings.Instance.Client.connectivityEvent -= UpdateConnectivity;
        GameSettings.Instance.Client.connectEvent      -= Connect;
        GameSettings.Instance.Client.clientEvent       -= UpdateClient;
    }
}