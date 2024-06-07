using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjs/GameSettings")]
public class GameSettings : ScriptableSingletonObject<GameSettings>
{
    [field: SerializeField]
    public Client Client { get; private set; }
}

public class ScriptableSingletonObject<T> : ScriptableObject where T : ScriptableSingletonObject<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                T[] assets = Resources.LoadAll<T>("");
                if (assets == null || assets.Length < 1)
                {
                    throw new System.Exception("Couldn't find scriptable singleton");
                }
                else if (assets.Length > 1)
                {
                    Debug.LogWarning("There's more than one instance of the singleton stupid");
                }
                instance = assets[0];
            }

            return instance;
        }
    }
}