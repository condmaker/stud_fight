using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputReader : MonoBehaviour
{
    [SerializeField]
    TMP_InputField input;

    public void SendInput() =>
        GameSettings.Instance.Client.serverIpAddress = input.text;
}
