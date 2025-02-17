﻿using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using System;
using System.Linq;


public class Chase : ComplexTask
{
    private bool IsCancelled = false;
    private float ChaseDistance = 5f;
    private IPlayerChaseable Cop;
    private Vehicle CopsVehicle;
    private int CopsSeat = -1;


    private SubTask CurrentSubTask;
    private Task CurrentTask = Task.Nothing;
    private uint GameTimeChaseStarted;
    private uint GameTimeGotStuck;
    private uint GameTimeVehicleStoppedMoving;
    private bool hasOwnFiber = false;
    private bool IsChasingSlowly = false;
    private Vehicle TaskedEnterVehicle;
    private int TaskedSeatIndex;
    private bool IsFirstRun;
    private bool IsStuck;
    private Vector3 LastPosition;
    private bool NeedsUpdates;
    private bool prevIsChasingSlowly = false;
    private IEntityProvideable World;
    private ISettingsProvideable Settings;
    private SeatAssigner SeatAssigner;
    public bool UseWantedLevel = true;
    public Chase(IComplexTaskable myPed, ITargetable player, IEntityProvideable world, IPlayerChaseable cop, ISettingsProvideable settings) : base(player, myPed, 500)//was 500
    {
        Name = "Chase";
        SubTaskName = "";
        World = world;
        Cop = cop;
        Settings = settings;
    }
    private enum eVehicleMissionType
    {
        Cruise = 1,
        Ram = 2,
        Block = 3,
        GoTo = 4,
        Stop = 5,
        Attack = 6,
        Follow = 7,
        Flee = 8,
        Circle = 9,
        Escort = 12,
        FollowRecording = 15,
        PoliceBehaviour = 16,
        Land = 19,
        Land2 = 20,
        Crash = 21,
        PullOver = 22,
        HeliProtect = 23
    };
    private enum SubTask
    {
        Shoot,
        Aim,
        Goto,
        None,
        CarJackPlayer,
        GoToVehicleDoor,
        Look,
        CombatPlayerInCar,
    }
    private enum Task
    {
        VehicleChase,
        VehicleChasePed,
        ExitVehicle,
        EnterVehicle,
        CarJack,
        FootChase,
        Nothing,
        StopCar,
    }
    public bool ShouldStopCar => Ped.DistanceToPlayer < 30f && Ped.Pedestrian.CurrentVehicle.Exists() && Ped.Pedestrian.CurrentVehicle.Speed > 0.5f && !Player.IsMovingFast && !ChaseRecentlyStarted && !Ped.IsInHelicopter && !Ped.IsInBoat;
    private bool ChaseRecentlyStarted => false;
    private bool ShouldAim => !UseWantedLevel || Player.WantedLevel > 1;
    private bool ShouldCarJackPlayer => (!UseWantedLevel || Player.WantedLevel > 1) && Cop.DistanceToPlayer <= 50f && Player.CurrentVehicle != null && Player.CurrentVehicle.Vehicle.Exists() && !Player.IsMovingFast && !Ped.Pedestrian.IsInAnyVehicle(true) && !Ped.IsAnimal;
    private bool ShouldGoToPlayerCar => Player.WantedLevel == 1 && Cop.DistanceToPlayer <= 50f && Player.CurrentVehicle != null && Player.CurrentVehicle.Vehicle.Exists() && !Player.IsMovingFast;
    private bool ShouldChasePedInVehicle => Ped.IsDriver && !Ped.IsAnimal && (Ped.DistanceToPlayer >= 55f || Ped.IsInBoat || Ped.IsInHelicopter || World.Pedestrians.PoliceList.Count(x => x.DistanceToPlayer <= 25f && !x.IsInVehicle) > 3);
    private bool ShouldChaseRecklessly => Player.WantedLevel >= Settings.SettingsManager.PoliceTaskSettings.RecklessVehicleChaseWantedLevelRequirement && !Player.PoliceResponse.LethalForceAuthorized;
    private bool ShouldChaseVeryRecklessly => Player.WantedLevel >= Settings.SettingsManager.PoliceTaskSettings.RecklessVehicleChaseWantedLevelRequirement && Settings.SettingsManager.PoliceTaskSettings.AllowVeryRecklessVehicleChaseWithLethalForce && Player.PoliceResponse.LethalForceAuthorized;
    private bool ShouldChaseVehicleInVehicle => Ped.IsDriver && Ped.Pedestrian.CurrentVehicle.Exists() && !ShouldExitPoliceVehicle && Player.CurrentVehicle != null;
    private bool ShouldExitPoliceVehicle => !Ped.RecentlyGotInVehicle && Ped.DistanceToPlayer < 30f && Ped.Pedestrian.CurrentVehicle.Exists() && Ped.Pedestrian.CurrentVehicle.Speed < 0.5f && !Player.IsMovingFast && !ChaseRecentlyStarted && !Ped.IsInHelicopter && !Ped.IsInBoat;
    private bool ShouldExitPlayersVehicle => Ped.Pedestrian.CurrentVehicle.Exists() && TaskedEnterVehicle.Exists() && Ped.Pedestrian.CurrentVehicle.Handle == TaskedEnterVehicle.Handle;
    private bool ShouldGetBackInCar => !Ped.RecentlyGotOutOfVehicle && Ped.Pedestrian.Exists() && CopsVehicle.Exists() && Ped.Pedestrian.DistanceTo2D(CopsVehicle) <= 35f && CopsVehicle.IsDriveable && CopsVehicle.FreeSeatsCount > 0;
    private bool VehicleIsStopped => GameTimeVehicleStoppedMoving != 0 && Game.GameTime - GameTimeVehicleStoppedMoving >= 500;//20000
    public override void Start()
    {
        if (Ped.Pedestrian.Exists())
        {
            NativeFunction.Natives.SET_PED_SHOULD_PLAY_IMMEDIATE_SCENARIO_EXIT(Ped.Pedestrian);
            SeatAssigner = new SeatAssigner(Ped, World, World.Vehicles.SimplePoliceVehicles);

            //EntryPoint.WriteToConsole($"TASKER: Chase Start: {Ped.Pedestrian.Handle} ChaseDistance: {ChaseDistance}", 5);
            GameTimeChaseStarted = Game.GameTime;
            Ped.Pedestrian.BlockPermanentEvents = Settings.SettingsManager.PoliceTaskSettings.BlockEventsDuringChase;
            Ped.Pedestrian.KeepTasks = true;
            AnimationDictionary.RequestAnimationDictionay("amb@medic@standing@timeofdeath@enter");
            AnimationDictionary.RequestAnimationDictionay("amb@medic@standing@timeofdeath@idle_a");
            if (!Ped.IsAnimal)
            {
                NativeFunction.Natives.SET_PED_PATH_CAN_USE_CLIMBOVERS(Ped.Pedestrian, true);
                NativeFunction.Natives.SET_PED_PATH_CAN_USE_LADDERS(Ped.Pedestrian, true);
                NativeFunction.Natives.SET_PED_PATH_CAN_DROP_FROM_HEIGHT(Ped.Pedestrian, true);
                if (Settings.SettingsManager.PoliceTaskSettings.SetSteerAround)
                {
                    NativeFunction.Natives.SET_PED_STEERS_AROUND_OBJECTS(Ped.Pedestrian, true);
                    NativeFunction.Natives.SET_PED_STEERS_AROUND_PEDS(Ped.Pedestrian, true);
                    NativeFunction.Natives.SET_PED_STEERS_AROUND_VEHICLES(Ped.Pedestrian, true);
                }
            }
            GameFiber.Yield();
            Update();
        }
    }
    public override void Stop()
    {

    }
    public override void Update()
    {
        if(!Ped.Pedestrian.Exists() || !ShouldUpdate || IsCancelled)
        {
            return;
        }
        if (Ped.Pedestrian.IsInAnyPoliceVehicle && !CopsVehicle.Exists())
        {
            CopsVehicle = Ped.Pedestrian.CurrentVehicle;
            CopsSeat = Ped.Pedestrian.SeatIndex;
        }
        if(!CopsVehicle.Exists())
        {
            SeatAssigner.AssignDriverSeat(true);
            if(SeatAssigner.VehicleAssigned != null && SeatAssigner.VehicleAssigned.Vehicle.Exists())
            {
                CopsVehicle = SeatAssigner.VehicleAssigned.Vehicle;
                CopsSeat = SeatAssigner.SeatAssigned;
                //EntryPoint.WriteToConsoleTestLong("ASSIGNED COP NEW VEHICLE");
            }
        }
        Task UpdatedTask = GetCurrentTaskDynamic();
        GameFiber.Yield();
        if (Ped.Pedestrian.Exists())
        {
            if (CurrentTask != UpdatedTask)
            {
                IsFirstRun = true;
                hasOwnFiber = false;
                CurrentTask = UpdatedTask;
                //NativeFunction.Natives.SET_DRIVE_TASK_CRUISE_SPEED(Ped.Pedestrian, 10f);//tr cruise speed test
                //EntryPoint.WriteToConsole($"TASKER: Chase SubTask Changed: {Ped.Pedestrian.Handle} to {CurrentTask} {CurrentDynamic}");
                ExecuteCurrentSubTask();
            }
            else if (NeedsUpdates)
            {
                ExecuteCurrentSubTask();
            }
            else if (IsChasingSlowly != prevIsChasingSlowly)
            {
                CurrentSubTask = SubTask.None;
                if (!hasOwnFiber)
                {
                    ExecuteCurrentSubTask();
                }
                prevIsChasingSlowly = IsChasingSlowly;
            }
            if (Ped.Pedestrian.Exists() && Ped.IsInVehicle)//CurrentTask == Task.VehicleChase || CurrentTask == Task.VehicleChasePed || Cu)
            {
                //NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.NoContact, true);
                SetSiren();
                if (Ped.IsInVehicle && Ped.Pedestrian.CurrentVehicle.Exists())
                {
                    if (Ped.Pedestrian.CurrentVehicle.Speed == 0f)
                    {
                        if (GameTimeVehicleStoppedMoving == 0)
                        {
                            GameTimeVehicleStoppedMoving = Game.GameTime;
                        }

                    }
                    else
                    {
                        GameTimeVehicleStoppedMoving = 0;
                    }
                }
            }
        }
        GameTimeLastRan = Game.GameTime;
        
        //EntryPoint.WriteToConsole($"TASKER: Chase UpdateEnd: {Ped.Pedestrian.Handle} InVeh: {Ped.IsInVehicle} InVeh2: {Ped.Pedestrian.IsInAnyVehicle(false)}");
    }
    public override void ReTask()
    {
        IsFirstRun = false;
        CurrentSubTask = SubTask.None;
    }
    private Task GetCurrentTaskDynamic()
    {
        if (CurrentDynamic == AIDynamic.Cop_InVehicle_Player_InVehicle)
        {
            if (ShouldExitPlayersVehicle)
            {
                return Task.ExitVehicle;
            }
            if (ShouldExitPoliceVehicle)
            {
                return Task.ExitVehicle;
            }
            else if (ShouldChaseVehicleInVehicle)
            {
                return Task.VehicleChase;
            }
            else
            {
                return Task.Nothing;
            }
        }
        else if (CurrentDynamic == AIDynamic.Cop_InVehicle_Player_OnFoot)
        {
            if (Ped.IsDriver)
            {
                if (ShouldExitPlayersVehicle)
                {
                    return Task.ExitVehicle;
                }
                else if (ShouldChasePedInVehicle)
                {
                    return Task.VehicleChasePed;
                }
                else if (ShouldStopCar)//is new
                {
                    return Task.StopCar;
                }
                else if (ShouldExitPoliceVehicle)
                {
                    return Task.ExitVehicle;
                }
                else
                {
                    return Task.Nothing;
                }
            }
            else
            {
                if (ShouldExitPlayersVehicle)
                {
                    return Task.ExitVehicle;
                }
                else if (ShouldExitPoliceVehicle)
                {
                    return Task.ExitVehicle;
                }
                else
                {
                    return Task.Nothing;
                }
            }
        }
        else if (CurrentDynamic == AIDynamic.Cop_OnFoot_Player_InVehicle)
        {
            if (ShouldCarJackPlayer)
            {
                return Task.CarJack;
            }
            else if (ShouldGoToPlayerCar)
            {
                return Task.FootChase;
            }
            else if (ShouldGetBackInCar)
            {
                return Task.EnterVehicle;
            }
            else
            {
                if (Ped.DistanceToPlayer >= 40f)
                {
                    return Task.FootChase;
                }
                else
                {
                    return Task.CarJack;
                }

            }
        }
        else if (CurrentDynamic == AIDynamic.Cop_OnFoot_Player_OnFoot)
        {
            if (Ped.DistanceToPlayer >= 50f && ShouldGetBackInCar)//this is new, was only footchase in here before, cant wait to see the bugs....
            {
                return Task.EnterVehicle;
            }
            else
            {
                return Task.FootChase;
            }
        }
        else
        {
            return Task.Nothing;
        }
    }
    private void ExecuteCurrentSubTask()
    {
        if (CurrentTask == Task.CarJack)
        {
            RunInterval = 200;
            SubTaskName = "CarJack";
            GoToPlayersCar();
        }
        else if (CurrentTask == Task.EnterVehicle)
        {
            Cop.WeaponInventory.Reset();
            RunInterval = 200;
            SubTaskName = "EnterVehicle";
            EnterVehicle();
        }
        else if (CurrentTask == Task.ExitVehicle)
        {
            Cop.WeaponInventory.Reset();
            RunInterval = 200;
            SubTaskName = "ExitVehicle";
            ExitVehicle();
        }
        else if (CurrentTask == Task.FootChase)
        {
            Cop.WeaponInventory.Reset();
            RunInterval = 200;
            SubTaskName = "FootChase";
            FootChase();
        }
        else if (CurrentTask == Task.VehicleChase)
        {
            Cop.WeaponInventory.Reset();
            RunInterval = 500;
            SubTaskName = "VehicleChase";
            VehicleChase();
        }
        else if (CurrentTask == Task.VehicleChasePed)
        {
            Cop.WeaponInventory.Reset();
            RunInterval = 500;
            SubTaskName = "VehicleChasePed";
            VehicleChasePed();
        }
        else if (CurrentTask == Task.Nothing)
        {
            Cop.WeaponInventory.Reset();
            RunInterval = 500;
            SubTaskName = "Nothing";
            //VehicleChasePed();
        }
        else if (CurrentTask == Task.StopCar)
        {
            Cop.WeaponInventory.Reset();
            RunInterval = 500;
            SubTaskName = "StopCar";
            StopCar();
        }
        GameTimeLastRan = Game.GameTime;
    }
    private void EnterVehicle()
    {
        if (Ped.Pedestrian.Exists())
        {
            if (Settings.SettingsManager.PoliceTaskSettings.BlockEventsDuringChase)
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
            }
            else
            {
                Ped.Pedestrian.BlockPermanentEvents = false;
            }
            Ped.Pedestrian.KeepTasks = true;
            NeedsUpdates = false;
            if (Ped.Pedestrian.Exists() && CopsVehicle.Exists())
            {
                int flags = Ped.DefaultEnterExitFlag | (int)eEnter_Exit_Vehicle_Flags.ECF_RESUME_IF_INTERRUPTED | (int)eEnter_Exit_Vehicle_Flags.ECF_DONT_JACK_ANYONE;
                NativeFunction.CallByName<bool>("TASK_ENTER_VEHICLE", Ped.Pedestrian, CopsVehicle, -1, CopsSeat, 2.0f, flags);// 9);
            }
        }
        //EntryPoint.WriteToConsole(string.Format("Started Enter Old Car: {0}", Ped.Pedestrian.Handle));
    }
    private void ExitVehicle()
    {
        if (Ped.Pedestrian.Exists())
        {
            NeedsUpdates = false;
            if (Settings.SettingsManager.PoliceTaskSettings.BlockEventsDuringChase)
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
            }
            else
            {
                Ped.Pedestrian.BlockPermanentEvents = false;
            }
            Ped.Pedestrian.KeepTasks = true;
            if (Ped.Pedestrian.Exists() && Ped.Pedestrian.CurrentVehicle.Exists())
            {
                TaskedEnterVehicle = null;
                //NativeFunction.CallByName<uint>("TASK_VEHICLE_TEMP_ACTION", Cop.Pedestrian, Cop.Pedestrian.CurrentVehicle, 27, 2000);
                //NativeFunction.CallByName<bool>("TASK_LEAVE_VEHICLE", Cop.Pedestrian, Cop.Pedestrian.CurrentVehicle, 256);
                if (Player.WantedLevel == 1)
                {
                    IsChasingSlowly = true;
                    unsafe
                    {
                        int lol = 0;
                        NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                        NativeFunction.CallByName<uint>("TASK_VEHICLE_TEMP_ACTION", 0, Ped.Pedestrian.CurrentVehicle, 27, 1000);
                        NativeFunction.CallByName<bool>("TASK_LEAVE_VEHICLE", 0, Ped.Pedestrian.CurrentVehicle, Ped.DefaultEnterExitFlag);//64);
                        NativeFunction.CallByName<bool>("TASK_GO_TO_ENTITY", 0, Player.Character, -1, 3f, 1.4f, 1073741824, 1); //Original and works ok
                        NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, false);
                        NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                        NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", Ped.Pedestrian, lol);
                        NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
                    }
                }
                else
                {
                    IsChasingSlowly = false;
                    unsafe
                    {
                        int lol = 0;
                        NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                        NativeFunction.CallByName<uint>("TASK_VEHICLE_TEMP_ACTION", 0, Ped.Pedestrian.CurrentVehicle, 27, 1000);
                        NativeFunction.CallByName<bool>("TASK_LEAVE_VEHICLE", 0, Ped.Pedestrian.CurrentVehicle, Ped.DefaultEnterExitFlag | (int)eEnter_Exit_Vehicle_Flags.ECF_DONT_CLOSE_DOOR | (int)eEnter_Exit_Vehicle_Flags.ECF_DONT_WAIT_FOR_VEHICLE_TO_STOP);// 256);
                        NativeFunction.CallByName<bool>("TASK_GO_TO_ENTITY", 0, Player.Character, -1, 7f, 500f, 1073741824, 1); //Original and works ok
                        NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, false);
                        NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                        NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", Ped.Pedestrian, lol);
                        NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
                    }//NATIVE PROC TASK_GO_TO_ENTITY(PED_INDEX PedIndex, ENTITY_INDEX EntityIndex, INT Time = DEFAULT_TIME_BEFORE_WARP,
                     //FLOAT SeekRadius = DEFAULT_SEEK_RADIUS,
                     //FLOAT MoveBlendRatio = PEDMOVEBLENDRATIO_RUN,
                     //FLOAT SlowDownDistance = 2.0,
                     //EGOTO_ENTITY_FLAGS GotoFlags = EGOTO_ENTITY_DEFAULT) = "0xc149e50fbb27dd70"
                }
                //EntryPoint.WriteToConsole(string.Format("Started Exit Car: {0}", Ped.Pedestrian.Handle));
            }
        }
    }
    private void FootChase()
    {
        NeedsUpdates = false;
        if(Settings.SettingsManager.PerformanceSettings.CopDisableFootChaseFiber)
        {
            return;
        }
        hasOwnFiber = true;
        Ped.IsRunningOwnFiber = true;
        CurrentSubTask = SubTask.None;
        FootChase footChase = new FootChase(Ped, Player, World, Cop, Settings);
        footChase.UseWantedLevel = UseWantedLevel;
        footChase.Setup();
        GameFiber.Yield();
        GameFiber.StartNew(delegate
        {
            try
            {
                //EntryPoint.WriteToConsoleTestLong($"STARTED Foot Chase Fiber for {(Ped.Pedestrian.Exists() ? Ped.Pedestrian.Handle : 0)}");
                while (hasOwnFiber && Ped.Pedestrian.Exists() && Ped.CurrentTask != null & Ped.CurrentTask?.Name == "Chase" && CurrentTask == Task.FootChase && !Settings.SettingsManager.PerformanceSettings.CopDisableFootChaseFiber && !IsCancelled)
                {
                    footChase.Update();
                    GameFiber.Sleep(RandomItems.GetRandomNumberInt(500, 600));
                    //GameFiber.Sleep(500);//GameFiber.Yield();
                }
                footChase.Dispose();
                Ped.IsRunningOwnFiber = false;
                //EntryPoint.WriteToConsoleTestLong($"ENDED Foot Chase Fiber for {(Ped.Pedestrian.Exists() ? Ped.Pedestrian.Handle : 0)}");
            }
            catch (Exception ex)
            {
                EntryPoint.WriteToConsole(ex.Message + " " + ex.StackTrace, 0);
                EntryPoint.ModController.CrashUnload();
            }
        }, "Run Cop Chase Logic");
    }
    private void GoToPlayersCar()
    {
        if (Ped.Pedestrian.Exists())
        {
            if (Settings.SettingsManager.PoliceTaskSettings.BlockEventsDuringChase)
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
            }
            else
            {
                Ped.Pedestrian.BlockPermanentEvents = false;
            }
            Ped.Pedestrian.KeepTasks = true;
            NeedsUpdates = true;

            if (Ped.Pedestrian.Exists() && Player.IsInVehicle && Player.Character.IsInAnyVehicle(false) && Player.CurrentVehicle != null && Player.CurrentVehicle.Vehicle.Exists())
            {
                if (CurrentSubTask != SubTask.CarJackPlayer)
                {
                    Cop.WeaponInventory.SetCompletelyUnarmed();
                    IsChasingSlowly = false;
                    TaskedEnterVehicle = Player.CurrentVehicle.Vehicle;

                    if (!Cop.BlackListedVehicles.Any(x => x == Player.CurrentVehicle.Vehicle.Handle))
                    {
                        Cop.BlackListedVehicles.Add(Player.CurrentVehicle.Vehicle.Handle);
                    }


                    //TaskedSeatIndex = Player.Character.SeatIndex;
                    NativeFunction.CallByName<bool>("TASK_ENTER_VEHICLE", Ped.Pedestrian, Player.CurrentVehicle.Vehicle, -1, Player.Character.SeatIndex, 5.0f, (int)eEnter_Exit_Vehicle_Flags.ECF_RESUME_IF_INTERRUPTED | (int)eEnter_Exit_Vehicle_Flags.ECF_JACK_ANYONE | (int)eEnter_Exit_Vehicle_Flags.ECF_JUST_PULL_PED_OUT );//   9);//caused them to get confused about getting back in thier car
                    CurrentSubTask = SubTask.CarJackPlayer;
                }
            }
            //else
            //{
            //    if(CurrentSubTask == SubTask.CarJackPlayer)
            //    {
            //        EntryPoint.WriteToConsole("Car Jack, Retasking Ped");
            //        unsafe
            //        {
            //            int lol = 0;
            //            NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
            //            NativeFunction.CallByName<bool>("TASK_GO_TO_ENTITY", 0, Player.Character, -1, 7f, 500f, 1073741824, 1); //Original and works ok
            //            NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, true);
            //            NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
            //            NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", Ped.Pedestrian, lol);
            //            NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
            //        }
            //        CurrentSubTask = SubTask.None;
            //    }
            //}
        }
    }
    private void SetSiren()
    {
        if (Settings.SettingsManager.WorldSettings.AllowSettingSirenState && Ped.Pedestrian.Exists() && Ped.IsDriver && Ped.Pedestrian.CurrentVehicle.Exists() && Ped.Pedestrian.CurrentVehicle.HasSiren && !Ped.Pedestrian.CurrentVehicle.IsSirenOn)
        {
            Ped.Pedestrian.CurrentVehicle.IsSirenOn = true;
            Ped.Pedestrian.CurrentVehicle.IsSirenSilent = false;
        }
    }
    private void StopCar()
    {
        if (Ped.Pedestrian.Exists() && Ped.Pedestrian.CurrentVehicle.Exists())
        {
            NeedsUpdates = false;
            if (Settings.SettingsManager.PoliceTaskSettings.BlockEventsDuringChase)
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
            }
            else
            {
                Ped.Pedestrian.BlockPermanentEvents = false;
            }
            Ped.Pedestrian.KeepTasks = true;
            NativeFunction.CallByName<uint>("TASK_VEHICLE_TEMP_ACTION", Ped.Pedestrian, Ped.Pedestrian.CurrentVehicle, 27, 2000);
            //EntryPoint.WriteToConsole($"CHASE Stop Car: {Ped.Pedestrian.Handle}", 5);
        }
        else
        {
            NeedsUpdates = true;
            return;
        }
    }
    private void VehicleChase()
    {
        if (Ped.Pedestrian.Exists())
        {
            NeedsUpdates = true;
            if (IsFirstRun)
            {
                VehicleChase_AssignTask();
                IsFirstRun = false;
            }
            UpdateVehicleChase();
        }
    }
    private void VehicleChase_AssignTask()
    {
        if (Ped.Pedestrian.Exists())
        {
            if (Settings.SettingsManager.PoliceTaskSettings.BlockEventsDuringVehicleChase)
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
            }
            else
            {
                Ped.Pedestrian.BlockPermanentEvents = false;
            }
            Ped.Pedestrian.KeepTasks = true;
            NativeFunction.Natives.SET_DRIVER_ABILITY(Ped.Pedestrian, Settings.SettingsManager.PoliceTaskSettings.DriverAbility);
            NativeFunction.Natives.SET_DRIVER_AGGRESSIVENESS(Ped.Pedestrian, Settings.SettingsManager.PoliceTaskSettings.DriverAggressiveness);
            if (Settings.SettingsManager.PoliceTaskSettings.DriverRacing > 0f)
            {
                NativeFunction.Natives.SET_DRIVER_RACING_MODIFIER(Ped.Pedestrian, Settings.SettingsManager.PoliceTaskSettings.DriverRacing);
            }
            if (Ped.IsInHelicopter)
            {
                NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, RandomItems.RandomPercent(50) ? -40f : 40f, RandomItems.RandomPercent(50) ? -40f : 40f, RandomItems.GetRandomNumber(40f, 60f)); //NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, RandomItems.RandomPercent(50) ? -35f : 35f, RandomItems.RandomPercent(50) ? -35f : 35f, RandomItems.GetRandomNumber(50f, 70f)); //NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, -35f, RandomItems.GetRandomNumber(-35f, 35f), RandomItems.GetRandomNumber(50f, 70f)); //NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, RandomItems.GetRandomNumber(-35f, 35f), RandomItems.GetRandomNumber(-35f, 35f), RandomItems.GetRandomNumber(80f, 120f)); // NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, -50f, 50f, 60f);

        //        NativeFunction.Natives.TASK_HELI_MISSION(Ped.Pedestrian, Ped.Pedestrian.CurrentVehicle,
        //0, Player.Character,//target vehicle and ped
        //0f, 0f, 0f,//coordinated, shouldnt be needed
        //Settings.SettingsManager.DebugSettings.HeliMission,//MISSION
        //100f,//Cruise SPeed
        //50f,//Target Reached DIst
        //-1f,//Heli Orientation
        //50,//flight height
        //50, //min hiehg tabove terrain
        //-1.0f,//slowdown distance
        //0//HELIMODE heli flags, 0 is none
        //);



            }
            //else if (Ped.IsInBoat)
            //{
            //    NativeFunction.Natives.TASK_VEHICLE_CHASE(Ped.Pedestrian, Player.Character);
            //}
            else
            {
                NativeFunction.Natives.TASK_VEHICLE_CHASE(Ped.Pedestrian, Player.Character);
                //if (Player.CurrentVehicle != null && Player.CurrentVehicle.Vehicle.Exists())
                //{

                //}
            }
        }
    }
    private void UpdateVehicleChase()
    {
        if (Ped.Pedestrian.Exists())
        {
            NativeFunction.Natives.SET_DRIVER_ABILITY(Ped.Pedestrian, Settings.SettingsManager.PoliceTaskSettings.DriverAbility);
            NativeFunction.Natives.SET_DRIVER_AGGRESSIVENESS(Ped.Pedestrian, Settings.SettingsManager.PoliceTaskSettings.DriverAggressiveness);
            if (Settings.SettingsManager.PoliceTaskSettings.DriverRacing > 0f)
            {
                NativeFunction.Natives.SET_DRIVER_RACING_MODIFIER(Ped.Pedestrian, Settings.SettingsManager.PoliceTaskSettings.DriverRacing);
            }
            //NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(Ped.Pedestrian, (int)eCustomDrivingStyles.FastEmergency);
            //NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_DisableBlockFromPursueDuringVehicleChase, true);
            //NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_DisableCruiseInFrontDuringBlockDuringVehicleChase, false);
            //NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_DisableSpinOutDuringVehicleChase, false);


            NativeFunction.Natives.SET_DRIVE_TASK_MAX_CRUISE_SPEED(Ped.Pedestrian, 100f);

            if (!Ped.IsInHelicopter && !Ped.IsInBoat)
            {
                if (ShouldChaseRecklessly && !Ped.IsOnBike)
                {

                    if (ShouldChaseVeryRecklessly)
                    {
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.FullContact, true);
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.MediumContact, false);
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.LowContact, false);
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.PIT, false);
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.NoContact, false);
                    }
                    else
                    {
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.FullContact, false);
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.MediumContact, false);
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.LowContact, false);
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.PIT, true);
                        NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.NoContact, false);
                    }

                    NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_DisableCruiseInFrontDuringBlockDuringVehicleChase, false);
                    NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_DisableSpinOutDuringVehicleChase, false);
                    NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_DisableBlockFromPursueDuringVehicleChase, false);

                    NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE(Ped.Pedestrian, 0f);
                    //NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(Ped.Pedestrian, (int)eCustomDrivingStyles.Code3);
                    //NativeFunction.Natives.SET_DRIVE_TASK_CRUISE_SPEED(Ped.Pedestrian, 70f);//tr cruise speed test
                }
                else
                {
                    NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE(Ped.Pedestrian, 15f);
                    NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.FullContact, false);
                    NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.MediumContact, false);
                    NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.LowContact, false);
                    NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.PIT, false);
                    NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(Ped.Pedestrian, (int)eChaseBehaviorFlag.NoContact, true);


                    NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_DisableCruiseInFrontDuringBlockDuringVehicleChase, true);
                    NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_DisableSpinOutDuringVehicleChase, true);
                    NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_DisableBlockFromPursueDuringVehicleChase, true);




                    //NativeFunction.Natives.SET_DRIVE_TASK_CRUISE_SPEED(Ped.Pedestrian, 70f);//tr cruise speed test
                }



                if ((Ped.RecentlySeenPlayer || Ped.DistanceToPlayer <= Settings.SettingsManager.PoliceTaskSettings.DriveBySightDuringChaseDistance) && Settings.SettingsManager.PoliceTaskSettings.AllowDriveBySightDuringChase)
                {
                    NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(Ped.Pedestrian, (int)eCustomDrivingStyles.Code3Close);
                }
                else
                {
                    NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(Ped.Pedestrian, (int)eCustomDrivingStyles.Code3);
                }


            }
            VehicleChase_CheckStuck();
        }
    }
    private void VehicleChase_CheckStuck()
    {
        if (Ped.Pedestrian.Exists())
        {
            Vector3 CurrentPosition = Ped.Pedestrian.Position;
            IsStuck = LastPosition.DistanceTo2D(CurrentPosition) <= 1.0f;
            if (IsStuck)
            {
                if (GameTimeGotStuck == 0)
                {
                    GameTimeGotStuck = Game.GameTime;
                }
            }
            else
            {
                GameTimeGotStuck = 0;
            }
            //if (IsStuck && Game.GameTime - GameTimeGotStuck >= 3000)
            //{
            //    EntryPoint.WriteToConsole($"VehicleChase Vehicle Target I AM STUCK!!: {Ped.Pedestrian.Handle}", 5);
            //}
            LastPosition = CurrentPosition;
        }
    }
    private void VehicleChasePed()
    {
        if (Ped.Pedestrian.Exists())
        {
            if (Ped.Pedestrian.CurrentVehicle.Exists())
            {
                NeedsUpdates = false;
            }
            else
            {
                NeedsUpdates = true;
                return;
            }
            if (Settings.SettingsManager.PoliceTaskSettings.BlockEventsDuringVehicleChase)
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
            }
            else
            {
                Ped.Pedestrian.BlockPermanentEvents = false;
            }
            Ped.Pedestrian.KeepTasks = true;
            float Speed = 30f;
            if (Ped.DistanceToPlayer <= 10f)
            {
                Speed = 10f;
            }
            if (IsFirstRun)
            {
                if (Ped.IsInHelicopter)
                {
                    NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, RandomItems.RandomPercent(50) ? -40f : 40f, RandomItems.RandomPercent(50) ? -40f : 40f, RandomItems.GetRandomNumber(40f, 60f)); //NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, RandomItems.RandomPercent(50) ? -35f : 35f, RandomItems.RandomPercent(50) ? -35f : 35f, RandomItems.GetRandomNumber(50f, 70f)); //NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, -35f, RandomItems.GetRandomNumber(-35f, 35f), RandomItems.GetRandomNumber(50f, 70f)); //NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, RandomItems.GetRandomNumber(-35f, 35f), RandomItems.GetRandomNumber(-35f, 35f), RandomItems.GetRandomNumber(50f, 80f));
                    //NativeFunction.Natives.TASK_HELI_CHASE(Ped.Pedestrian, Player.Character, 25f, 0f, 25f);
                }
                else if (Ped.IsInBoat)
                {
                    NativeFunction.Natives.TASK_VEHICLE_CHASE(Ped.Pedestrian, Player.Character);
                }
                else
                {
                    if (Ped.Pedestrian.CurrentVehicle.Exists())
                    {
                        unsafe
                        {
                            int lol = 0;
                            NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                            //NativeFunction.CallByName<bool>("TASK_VEHICLE_MISSION_PED_TARGET", 0, Ped.Pedestrian.CurrentVehicle, Player.Character, 7, Speed, 541327934, 8f, 0f, true);//541327934//4 | 8 | 16 | 32 | 512 | 262144
                            NativeFunction.CallByName<bool>("TASK_VEHICLE_MISSION_PED_TARGET", 0, Ped.Pedestrian.CurrentVehicle, Player.Character, 7, Speed, (int)eCustomDrivingStyles.Code3, 4f, 2f, true);
                            NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, true);
                            NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                            NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", Ped.Pedestrian, lol);
                            NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
                        }
                    }
                }
            }
            else
            {
                Speed = 30f;
                if (Ped.DistanceToPlayer <= 15f)//10f
                {
                    Speed = 10f;
                }
                if (Ped.IsInHelicopter)
                {
                    NativeFunction.Natives.SET_DRIVER_ABILITY(Ped.Pedestrian, 1.0f);
                    //if (Ped.DistanceToPlayer <= 100f && Player.Character.Speed < 20f)//32f)//70 mph
                    //{
                    //    NativeFunction.Natives.SET_DRIVE_TASK_CRUISE_SPEED(Ped.Pedestrian, 10f);
                    //}
                    //else
                    //{
                    //    NativeFunction.Natives.SET_DRIVE_TASK_CRUISE_SPEED(Ped.Pedestrian, 100f);
                    //}
                }
                else
                {
                    //NativeFunction.Natives.SET_DRIVE_TASK_CRUISE_SPEED(Ped.Pedestrian, Speed);//tr cruise speed test
                }
            }
            //NativeFunction.CallByName<bool>("TASK_VEHICLE_MISSION_PED_TARGET", Cop.Pedestrian, Cop.Pedestrian.CurrentVehicle, Player.Character, 7, 30f, 4 | 8 | 16 | 32 | 512 | 262144, 0f, 0f, true);
            //EntryPoint.WriteToConsole($"VehicleChase Ped Target: {Ped.Pedestrian.Handle}", 5);
        }
    }
}