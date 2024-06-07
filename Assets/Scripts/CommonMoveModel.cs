using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommonMoveModel : MoveModel 
{
    [SerializeField]
    private TextMeshProUGUI moveName, damage, cost, effects;

    public override void SetUpMove(StudMove move, Stud stud)
    {
        assignedStud = stud;
        assignedMove = move;

        moveName.text = move.name;
        damage.text = move.baseDmg.ToString();
        cost.text = move.cost.ToString();

        bool firstIter = true;

        if (stud.stamina - (move.cost * (stud.effects.ContainsKey(MoveEffect.DoubleCost) ? 2 : 1)) < 0)
            LockMove();

        if (move.effects == null) 
            return;

        effects.text = "";

        foreach (KeyValuePair<MoveEffect, int> effect in move.effects)
        {
            if (!firstIter)
            {
                effects.text += ", ";
            }
            else
                firstIter = false;

            effects.text += StringExtensions.GetDescription(effect.Key);
            if (effect.Key != MoveEffect.Shield)
                effects.text += " (" + effect.Value + " turn)";
            else
                effects.text += " (NEXT)";
        }
    }
}
