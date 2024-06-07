using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TurnListener : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI turnText;

    // Start is called before the first frame update
    void Start()
    {
        GameSettings.Instance.Client.gameEvent += ChangeTurn;

        ChangeTurn(GameSettings.Instance.Client.CurrentTurn, new StudMove());
    }

    private bool ChangeTurn(GameState state, StudMove _)
    {
        switch (state)
        {
            case GameState.ISTURN:
                turnText.text = "YOUR TURN";
                turnText.color = Color.blue;
                break;
            case GameState.NOTTURN:
                turnText.text = "ENEMY TURN";
                turnText.color = Color.red;
                break;
            default:
            case GameState.INTERRUPTED:
                turnText.text = "END";
                turnText.color = Color.gray;
                break;
        }

        return true;
    }

    private void OnDestroy()
    {
        GameSettings.Instance.Client.gameEvent -= ChangeTurn;
    }
}
