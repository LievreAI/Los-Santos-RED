﻿using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class GoToOnFootTaskState : TaskState
{
    private PedExt PedGeneral;
    private IEntityProvideable World;
    private SeatAssigner SeatAssigner;

    private VehicleExt TaskedVehicle;
    private int TaskedSeat;
    private ISettingsProvideable Settings;
    private ITargetable Player;
    private bool BlockPermanentEvents = false;
    private Vector3 PlaceToWalkTo;
    private ILocationReachable LocationReachable;
    public GoToOnFootTaskState(PedExt pedGeneral, ITargetable player, IEntityProvideable world, SeatAssigner seatAssigner, ISettingsProvideable settings, bool blockPermanentEvents, Vector3 placeToWalkTo, ILocationReachable locationReachable)
    {
        PedGeneral = pedGeneral;
        Player = player;
        World = world;
        SeatAssigner = seatAssigner;
        Settings = settings;
        BlockPermanentEvents = blockPermanentEvents;
        PlaceToWalkTo = placeToWalkTo;
        LocationReachable = locationReachable;
    }
    public bool IsValid => PedGeneral != null && !PedGeneral.IsInVehicle && !LocationReachable.HasReachedLocatePosition;
    public string DebugName => $"GoToOnFootTaskState";
    public void Dispose()
    {
        Stop();
    }
    public void Start()
    {
        PedGeneral.ClearTasks(true);
        TaskEntry();
    }
    public void Stop()
    {
        PedGeneral.ClearTasks(true);
    }
    public void Update()
    {
        UpdateDistances();
    }
    private void TaskEntry()
    {
        if (!PedGeneral.Pedestrian.Exists())
        {
            return;
        }
        if (BlockPermanentEvents)
        {
            PedGeneral.Pedestrian.BlockPermanentEvents = true;
            PedGeneral.Pedestrian.KeepTasks = true;
        }
        if (PedGeneral == null || !PedGeneral.Pedestrian.Exists() || PlaceToWalkTo == null || PlaceToWalkTo == Vector3.Zero || !PedGeneral.IsDriver)
        {
            return;
        }
        NativeFunction.Natives.TASK_FOLLOW_NAV_MESH_TO_COORD(PedGeneral.Pedestrian, PlaceToWalkTo.X, PlaceToWalkTo.Y, PlaceToWalkTo.Z, 15f, -1, 0.25f, 0, 40000.0f);
    }
    private void UpdateDistances()
    {
        float DistanceToCoordinates = PedGeneral.Pedestrian.DistanceTo2D(PlaceToWalkTo);
        if (DistanceToCoordinates <= 7f)
        {
            LocationReachable.HasReachedLocatePosition = true;
            //EntryPoint.WriteToConsoleTestLong($"LOCATE TASK: Cop {Ped.Handle} HAS REACHED POSITION");
        }
    }
}

