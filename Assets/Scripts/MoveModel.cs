using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class MoveModel : MonoBehaviour
{
    [SerializeField]
    protected Button button;

    protected Stud     assignedStud;
    protected StudMove assignedMove;

    public abstract void SetUpMove(StudMove move, Stud stud);

    public void LockMove()
    {
        button.onClick.RemoveAllListeners();
        button.interactable = false;
    }

    public void UnlockMove()
    {
        button.onClick.AddListener(PublishMove);
        button.interactable = true;
    }

    public void PublishMove()
    {
        LockMove();
        GameSettings.Instance.Client.SendMove(assignedStud.PerformMove(assignedMove));
        GameSettings.Instance.Client.EndTurn();
    }
}
