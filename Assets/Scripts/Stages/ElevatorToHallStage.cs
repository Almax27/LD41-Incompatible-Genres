﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorToHallStage : GameStage {

    public ElevatorController elevatorController = null;

    public override void OnStageBegan()
    {
        if (elevatorController)
        {
            elevatorController.Reset();
        }
        base.OnStageBegan();
    }

    public override void OnStageEnded()
    {
        base.OnStageEnded();
    }

    public override bool IsStageFinished()
    {
        return base.IsStageFinished();
    }

    public override void RespawnPlayer(FPSPlayerController player)
    {
        if (elevatorController)
        {
            elevatorController.OpenExit();
        }
        base.RespawnPlayer(player);
    }
}
