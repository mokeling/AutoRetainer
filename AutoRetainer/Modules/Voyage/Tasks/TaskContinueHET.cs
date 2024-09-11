﻿using AutoRetainer.Internal.InventoryManagement;
using ECommons.Automation;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace AutoRetainer.Modules.Voyage.Tasks;

internal static unsafe class TaskContinueHET
{
    private static void Enqueue()
    {
        VoyageUtils.Log($"Task enqueued: {nameof(TaskContinueHET)}");
        P.TaskManager.Enqueue(NpcSaleManager.EnqueueIfItemsPresent);
        P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(() => !IsOccupied(), "WaitUntilNotOccupied1", new(timeLimitMS:180 * 1000));
        P.TaskManager.Enqueue(() =>
        {
            if(VoyageUtils.ShouldEnterWorkshop())
            {
                if(Utils.GetNearestWorkshopEntrance(out _) != null)
                {
                    EnqueueImmediateEnterWorkshop();
                }
            }
        });
    }

    private static void EnqueueImmediateEnterWorkshop()
    {
        P.TaskManager.BeginStack();
        P.TaskManager.Enqueue(() => !IsOccupied() && IsScreenReady(), "WaitUntilNotOccupied2", new(timeLimitMS:180 * 1000));
        P.TaskManager.Enqueue(LockonAdditionalChambers, new(timeLimitMS:1000, abortOnTimeout:true));
        P.TaskManager.Enqueue(HouseEnterTask.Approach);
        P.TaskManager.Enqueue(AutorunOffAdd);
        P.TaskManager.Enqueue(() => { Chat.Instance.ExecuteCommand("/automove off"); });
        P.TaskManager.Enqueue(InteractAdd);
        P.TaskManager.Enqueue(SelectEnterWorkshop);
        P.TaskManager.Enqueue(() => VoyageUtils.Workshops.Contains(Svc.ClientState.TerritoryType), "Wait Until entered workshop");
        P.TaskManager.EnqueueDelay(60, true);
        P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.InsertStack();
    }

    private static void EnqueueEnterWorkshop()
    {
        P.TaskManager.Enqueue(() => !IsOccupied() && IsScreenReady(), "WaitUntilNotOccupied2", new(timeLimitMS:180 * 1000));
        P.TaskManager.Enqueue(LockonAdditionalChambers, new(timeLimitMS:1000, abortOnTimeout:true));
        P.TaskManager.Enqueue(HouseEnterTask.Approach);
        P.TaskManager.Enqueue(AutorunOffAdd);
        P.TaskManager.Enqueue(() => { Chat.Instance.ExecuteCommand("/automove off"); });
        P.TaskManager.Enqueue(InteractAdd);
        P.TaskManager.Enqueue(SelectEnterWorkshop);
        P.TaskManager.Enqueue(() => VoyageUtils.Workshops.Contains(Svc.ClientState.TerritoryType), "Wait Until entered workshop");
        P.TaskManager.EnqueueDelay(60, true);
        P.TaskManager.Enqueue(Utils.WaitForScreen);
    }

    internal static bool? SelectEnterWorkshop()
    {
        if(Utils.TrySelectSpecificEntry(Lang.EnterWorkshop, () => EzThrottler.Throttle("HET.SelectEnterWorkshop")))
        {
            DebugLog("Confirmed going to workhop");
            return true;
        }
        return false;
    }

    internal static bool? InteractAdd()
    {
        var entrance = Utils.GetNearestWorkshopEntrance(out var d);
        if(entrance != null && Svc.Targets.Target?.Address == entrance.Address && EzThrottler.Throttle("HET.InteractAdd", 1000))
        {
            DebugLog($"Interacting with entrance");
            TargetSystem.Instance()->InteractWithObject((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)entrance.Address, false);
            return true;
        }
        return false;
    }
    internal static bool? AutorunOffAdd()
    {
        var entrance = Utils.GetNearestWorkshopEntrance(out var d);
        if(entrance != null && d < 3f + Utils.Random && EzThrottler.Throttle("HET.DisableAutomoveAdd"))
        {
            DebugLog($"Disabling automove");
            Chat.Instance.ExecuteCommand("/automove off");
            return true;
        }
        return false;
    }

    internal static bool? LockonAdditionalChambers()
    {
        var entrance = Utils.GetNearestWorkshopEntrance(out _);
        if(entrance != null)
        {
            if(Svc.Targets.Target?.Address == entrance.Address)
            {
                if(EzThrottler.Throttle("HET.LockonAdd"))
                {
                    Chat.Instance.ExecuteCommand("/lockon");
                    return true;
                }
            }
            else
            {
                if(EzThrottler.Throttle("HET.SetTargetAdd", 200))
                {
                    DebugLog($"Setting entrance target ({entrance})");
                    Svc.Targets.Target = entrance;
                }
            }
        }
        return false;
    }
}
