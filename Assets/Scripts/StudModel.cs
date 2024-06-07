using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StudModel : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI studName, health, stamina, shield, effects, move;

    [SerializeField]
    private Slider healthSlider, staminaSlider;

    [SerializeField]
    private Sprite pimpySonOppSprite, chumLeeSprite, redrumSprite, 
        mrMiagiSprite, bapeSprite, doughBoySprite, toleToleSprite;

    [SerializeField]
    private bool isClient = false;

    [SerializeField]
    private CommonMoveModel[] moveModel;

    [SerializeField]
    private SkipMoveModel skipMove;

    [SerializeField]
    private Image image;

    private Stud stud;
    private WaitForSeconds seconds;

    private void Awake()
    {
        SetUp();

        GameSettings.Instance.Client.gameEvent += UpdateModel;
        GameSettings.Instance.Client.gameEvent += ShowMoveEnd;
        GameSettings.Instance.Client.sendMove  += ShowMoveStart;
        GameSettings.Instance.Client.endTurn   += LockUpMoves;

        seconds = new WaitForSeconds(1);
    }

    public void SetUp()
    {
        stud = isClient ?
            GameSettings.Instance.Client.PlayerStud :
            GameSettings.Instance.Client.OpponentStud;

        // This is bad but I didn't want to do a companion obj on the class itself
        switch (stud.type)
        {
            case StudType.PimpySonOpp:
                image.sprite = pimpySonOppSprite;
                break;
            case StudType.ChumLee:
                image.sprite = chumLeeSprite;
                break;
            case StudType.Redrum:
                image.sprite = redrumSprite;
                break;
            case StudType.MrMiagi:
                image.sprite = mrMiagiSprite;
                break;
            case StudType.Bape:
                image.sprite = bapeSprite;
                break;
            case StudType.Doughboy:
                image.sprite = doughBoySprite;
                break;
            case StudType.ToleTole:
                image.sprite = toleToleSprite;
                break;
        }

        studName.text = StringExtensions.GetDescription(stud.type);

        UpdateModel(GameSettings.Instance.Client.CurrentTurn, new StudMove());
    }

    public bool UpdateModel(GameState state, StudMove move)
    {
        stud = isClient ?
            GameSettings.Instance.Client.PlayerStud :
            GameSettings.Instance.Client.OpponentStud;

        if (stud == null)
        {
            Debug.LogError("Stud is not set!");
            return false;
        }

        if (isClient)
            SetUpMoves(state);

        health.text = stud.health.ToString() + " / " + stud.MaxHealth;
        healthSlider.maxValue = stud.MaxHealth;
        healthSlider.value = stud.health;

        if (isClient)
        {
            stamina.text = stud.stamina.ToString() + " / " + stud.MaxStamina;
            staminaSlider.maxValue = stud.MaxStamina;
            staminaSlider.value = stud.stamina;
        }

        shield.text = stud.shield.ToString();

        if (stud.effects == null)
            return true;

        bool firstIter = true;
        effects.text = "";

        foreach (KeyValuePair<MoveEffect, int> effect in stud.effects)
        {
            if (effect.Key == MoveEffect.None) continue;

            if (!firstIter)
            {
                effects.text += ", ";
            }
            else
                firstIter = false;

            effects.text += StringExtensions.GetDescription(effect.Key);

            if (effect.Key != MoveEffect.Shield && effect.Value > 0)
                effects.text += " (" + effect.Value + " turn)";
            else if (effect.Key == MoveEffect.Shield)
                effects.text += " (NEXT)";
        }

        return true;
    }

    private bool ShowMoveStart(GameState turn, StudMove move)
    {
        bool show = isClient;

        if (show && move.id != 17) StartCoroutine(ShowMoveC(move));

        return true;
    }

    private bool ShowMoveEnd(GameState turn, StudMove move)
    {
        bool show = !isClient;

        if (show && move.id != 17) StartCoroutine(ShowMoveC(move));

        return true;
    }

    IEnumerator ShowMoveC(StudMove move)
    {
        this.move.text = move.name;
        this.move.DOFade(1, 500);

        yield return seconds;

        this.move.text = "";
        this.move.DOFade(1, 500);
    }

    private void LockUpMoves()
    {
        if (!isClient)
        {
            UpdateModel(GameState.ISTURN, StudMove.Skip);
            return;
        }

        for (int i = 0; i < moveModel.Length; i++)
        {
            moveModel[i].LockMove();
        }

        skipMove.LockMove();
    }

    private void SetUpMoves(GameState state)
    {
        if (moveModel.Length > 4 && stud.moves.Count > 4)
        {
            Debug.LogError("Check your moves!");
            return;
        }

        for (int i = 0; i < moveModel.Length; i++)
        {
            if (state == GameState.ISTURN)
                moveModel[i].UnlockMove();
            else
                moveModel[i].LockMove();

            moveModel[i].SetUpMove(stud.moves[i], stud);
        }

        if (state == GameState.ISTURN)
            skipMove.UnlockMove();
        else
            skipMove.LockMove();

        skipMove.SetUpMove(StudMove.Skip, stud);
    }

    private void OnDestroy()
    {
        GameSettings.Instance.Client.gameEvent -= UpdateModel;
        GameSettings.Instance.Client.gameEvent -= ShowMoveStart;
        GameSettings.Instance.Client.sendMove  -= ShowMoveEnd;
        GameSettings.Instance.Client.endTurn   -= LockUpMoves;
    }
}