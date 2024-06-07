using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkipMoveModel : MoveModel
{
    public override void SetUpMove(StudMove move, Stud stud)
    {
        assignedStud = stud;
        assignedMove = StudMove.Skip;
    }
}
