﻿using ExtensionsMethods;
using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using Rage.Native;
using System;
using System.Linq;

public class CivilianSpawnTask : SpawnTask
{
    private bool AddBlip;
    private bool AddOptionalPassengers = false;
    public bool SetPersistent = false;
    private VehicleExt LastCreatedVehicle;
    private INameProvideable Names;
    private int NextBeatNumber;
    private int OccupantsToAdd;
    private ISettingsProvideable Settings;
    private Vehicle SpawnedVehicle;
    private string UnitCode;
    private IWeapons Weapons;
    private IEntityProvideable World;
    private ICrimes Crimes;

    public CivilianSpawnTask(SpawnLocation spawnLocation, DispatchableVehicle vehicleType, DispatchablePerson personType, bool addBlip, bool addOptionalPassengers, bool setPersistent, ISettingsProvideable settings, ICrimes crimes, IWeapons weapons, INameProvideable names, IEntityProvideable world) : base(spawnLocation, vehicleType, personType)
    {

        AddBlip = addBlip;
        Settings = settings;
        Weapons = weapons;
        Names = names;
        AddOptionalPassengers = addOptionalPassengers;
        SetPersistent = setPersistent;
        World = world;
        Crimes = crimes;
    }
    private bool HasPersonToSpawn => PersonType != null;
    private bool HasVehicleToSpawn => VehicleType != null;
    private bool IsInvalidSpawnPosition => !AllowAnySpawn && Position.DistanceTo2D(Game.LocalPlayer.Character) <= 100f && Extensions.PointIsInFrontOfPed(Game.LocalPlayer.Character, Position);
    private bool LastCreatedVehicleExists => LastCreatedVehicle != null && LastCreatedVehicle.Vehicle.Exists();
    private bool WillAddPassengers => (VehicleType != null && VehicleType.MinOccupants > 1) || AddOptionalPassengers;
    public override void AttemptSpawn()
    {
        try
        {
            if (IsInvalidSpawnPosition)
            {
                EntryPoint.WriteToConsole($"CivilianSpawn: Task Invalid Spawn Position");
                return;
            }
            Setup();
            if (HasVehicleToSpawn)
            {
                AttemptVehicleSpawn();
            }
            else if (HasPersonToSpawn)
            {
                AttemptPersonOnlySpawn();
            }
        }
        catch (Exception ex)
        {
            EntryPoint.WriteToConsole($"CivilianSpawn: ERROR {ex.Message} : {ex.StackTrace}", 0);
            Cleanup(true);
        }
    }
    private void AddPassengers()
    {
        //EntryPoint.WriteToConsole($"SPAWN TASK: UnitCode {UnitCode} OccupantsToAdd {OccupantsToAdd}");
        for (int OccupantIndex = 1; OccupantIndex <= OccupantsToAdd; OccupantIndex++)
        {
            string requiredGroup = "";
            if (VehicleType != null)
            {
                requiredGroup = VehicleType.RequiredPedGroup;
            }
            if (PersonType != null)
            {
                PedExt Passenger = CreatePerson();
                if (Passenger != null && Passenger.Pedestrian.Exists() && LastCreatedVehicleExists)
                {
                    PutPedInVehicle(Passenger, OccupantIndex - 1);
                }
                else
                {
                    Cleanup(false);
                }
            }
            GameFiber.Yield();
        }
    }
    private void AttemptPersonOnlySpawn()
    {
        CreatePerson();
    }
    private void AttemptVehicleSpawn()
    {
        LastCreatedVehicle = CreateVehicle();
        if (LastCreatedVehicleExists)
        {
            if (HasPersonToSpawn)
            {
                PedExt Person = CreatePerson();
                if (Person != null && Person.Pedestrian.Exists() && LastCreatedVehicleExists)
                {
                    PutPedInVehicle(Person, -1);
                    if (WillAddPassengers)
                    {
                        AddPassengers();
                    }
                }
                else
                {
                    Cleanup(true);
                }
            }
        }
    }
    private void Cleanup(bool includePeople)
    {
        if (LastCreatedVehicle != null && LastCreatedVehicle.Vehicle.Exists())
        {
            LastCreatedVehicle.Vehicle.Delete();
            EntryPoint.WriteToConsole($"CivilianSpawn: ERROR DELETED VEHICLE", 0);
        }
        if (includePeople)
        {
            foreach (PedExt person in CreatedPeople)
            {
                if (person != null && person.Pedestrian.Exists())
                {
                    person.Pedestrian.Delete();
                    EntryPoint.WriteToConsole($"CivilianSpawn: ERROR DELETED PED", 0);
                }
            }
        }
    }
    private PedExt CreatePerson()
    {
        try
        {
            Ped createdPed;
            if (PlacePedOnGround)
            {
                createdPed = new Ped(PersonType.ModelName, new Vector3(Position.X, Position.Y, Position.Z), SpawnLocation.Heading);
            }
            else
            {
                createdPed = new Ped(PersonType.ModelName, new Vector3(Position.X, Position.Y, Position.Z + 1f), SpawnLocation.Heading);
            }
            EntryPoint.SpawnedEntities.Add(createdPed);
            GameFiber.Yield();
            if (createdPed.Exists())
            {
                if (!createdPed.Exists())
                {
                    return null;
                }
                PedExt Person = SetupPed(createdPed);
                PersonType.SetPedVariation(createdPed, null, true);
                GameFiber.Yield();
                CreatedPeople.Add(Person);
                return Person;
            }
            return null;
        }
        catch (Exception ex)
        {
            EntryPoint.WriteToConsole($"CivilianSpawn: ERROR DELETED PERSON {ex.Message} {ex.StackTrace}", 0);
            return null;
        }
    }
    private VehicleExt CreateVehicle()
    {
        try
        {
            EntryPoint.WriteToConsole($"CivilianSpawn: Attempting to spawn {VehicleType.ModelName}", 3);
            SpawnedVehicle = new Vehicle(VehicleType.ModelName, Position, SpawnLocation.Heading);
            EntryPoint.SpawnedEntities.Add(SpawnedVehicle);
            GameFiber.Yield();
            if (SpawnedVehicle.Exists())
            {
                VehicleExt CreatedVehicle = World.Vehicles.GetVehicleExt(SpawnedVehicle);
                if (CreatedVehicle == null)
                {
                    CreatedVehicle = new VehicleExt(SpawnedVehicle, Settings);
                    CreatedVehicle.Setup();
                }
                CreatedVehicle.WasModSpawned = true;

                if (SpawnedVehicle.Exists())
                {
                    CreatedVehicle.WasModSpawned = true;
                    if (SetPersistent)
                    {
                        SpawnedVehicle.IsPersistent = true;
                        EntryPoint.PersistentVehiclesCreated++;
                    }
                    else
                    {
                        SpawnedVehicle.IsPersistent = false;
                    }

 
                    CreatedVehicles.Add(CreatedVehicle);
                    if (SpawnedVehicle.Exists() && VehicleType.RequiredPrimaryColorID != -1)
                    {
                        NativeFunction.Natives.SET_VEHICLE_COLOURS(SpawnedVehicle, VehicleType.RequiredPrimaryColorID, VehicleType.RequiredSecondaryColorID == -1 ? VehicleType.RequiredPrimaryColorID : VehicleType.RequiredSecondaryColorID);
                    }
                    if (VehicleType.VehicleExtras != null)
                    {
                        foreach (DispatchableVehicleExtra extra in VehicleType.VehicleExtras)
                        {
                            if (NativeFunction.Natives.DOES_EXTRA_EXIST<bool>(SpawnedVehicle, extra))
                            {
                                NativeFunction.Natives.SET_VEHICLE_EXTRA(SpawnedVehicle, extra, 0);
                            }
                        }
                    }
                    NativeFunction.Natives.SET_VEHICLE_DIRT_LEVEL(SpawnedVehicle, RandomItems.GetRandomNumberInt(0, 15));
                    EntryPoint.WriteToConsole($"CivilianSpawn: SPAWNED {VehicleType.ModelName}", 3);
                    GameFiber.Yield();
                    return CreatedVehicle;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            EntryPoint.WriteToConsole($"CivilianSpawn: ERROR DELETED VEHICLE {ex.Message} {ex.StackTrace} ATTEMPTING {VehicleType.ModelName}", 0);
            if (SpawnedVehicle.Exists())
            {
                SpawnedVehicle.Delete();
            }
            GameFiber.Yield();
            return null;
        }
    }
    private void PutPedInVehicle(PedExt Person, int seat)
    {
        Person.Pedestrian.WarpIntoVehicle(LastCreatedVehicle.Vehicle, seat);
        Person.AssignedVehicle = LastCreatedVehicle;
        Person.AssignedSeat = seat;
        Person.UpdateVehicleState();
    }
    private void Setup()
    {
        if (VehicleType != null)
        {
            OccupantsToAdd = RandomItems.MyRand.Next(VehicleType.MinOccupants, VehicleType.MaxOccupants + 1) - 1;
        }
        else
        {
            OccupantsToAdd = 0;
        }
    }
    private PedExt SetupPed(Ped ped)
    {
        if (PlacePedOnGround)
        {
            float resultArg = ped.Position.Z;
            NativeFunction.Natives.GET_GROUND_Z_FOR_3D_COORD(ped.Position.X, ped.Position.Y, ped.Position.Z, out resultArg, false);
            ped.Position = new Vector3(ped.Position.X, ped.Position.Y, resultArg);
        }
        int DesiredHealth = RandomItems.MyRand.Next(PersonType.HealthMin, PersonType.HealthMax) + 100;
        int DesiredArmor = RandomItems.MyRand.Next(PersonType.ArmorMin, PersonType.ArmorMax);
        ped.MaxHealth = DesiredHealth;
        ped.Health = DesiredHealth;
        ped.Armor = DesiredArmor;

        if (SetPersistent)
        {
            ped.IsPersistent = true;
        }
        else
        {
            ped.IsPersistent = false;
        }
        EntryPoint.PersistentPedsCreated++;//TR
        bool isMale;
        RelationshipGroup rg = new RelationshipGroup("CIVMALE");
        if (PersonType.IsFreeMode && PersonType.ModelName.ToLower() == "mp_f_freemode_01")
        {
            isMale = false;
            rg = new RelationshipGroup("CIVFEMALE");
        }
        else
        {
            isMale = ped.IsMale;
        }
        ped.RelationshipGroup = rg;
        PedExt CreatedPedExt = new PedExt(ped, Settings, false, true, false, false, Names.GetRandomName(isMale), Crimes, Weapons, "", World, false);
        World.Pedestrians.AddEntity(CreatedPedExt);
        if (AddBlip && ped.Exists())
        {
            Blip myBlip = ped.AttachBlip();
            NativeFunction.Natives.BEGIN_TEXT_COMMAND_SET_BLIP_NAME("STRING");
            NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(CreatedPedExt.GroupName);
            NativeFunction.Natives.END_TEXT_COMMAND_SET_BLIP_NAME(myBlip);
            myBlip.Color = System.Drawing.Color.Blue;
            myBlip.Scale = 0.6f;
        }
        return CreatedPedExt;
    }
}