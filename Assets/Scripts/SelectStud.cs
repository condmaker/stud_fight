using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectStud : MonoBehaviour
{
    [SerializeField]
    private StudType studToSelect;

    public void Select() =>
        GameSettings.Instance.Client.SendReady(Stud.GetStudByType(studToSelect));
}
