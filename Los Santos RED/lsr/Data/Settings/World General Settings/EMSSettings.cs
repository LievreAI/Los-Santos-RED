﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EMSSettings : ISettingsDefaultable
{
    [Description("Allows mod spawning of EMS services in the world.")]
    public bool ManageDispatching { get; set; }
    [Description("Allows tasking of ambient EMS pedestrians in the world.")]
    public bool ManageTasking { get; set; }
    [Description("Attach a blip to any spawned EMS pedestrian")]
    public bool ShowSpawnedBlips { get; set; }
    [Description("Percent of pedestrians that are revived from unconsciousness. Max of 100")]
    public float RevivePercentage { get; set; }

    [Description("Percent of pedestrians that are revived from unconsciousness when treated by the player. Max of 100")]
    public float RevivePercentagePlayer { get; set; }

    [Description("Enable or disable ambient spawns around hospitals.")]
    public bool AllowStationSpawning { get; set; }
    [Description("If enabled, hospital ambient spawns ignore the ped and vehicle spawn limits.")]
    public bool StationSpawningIgnoresLimits { get; set; }
    public bool AllowAlerts { get; set; }





    [Description("Minimum time in milliseconds between a spawn.")]
    public int TimeBetweenSpawn { get; set; }
    public int TimeBetweenSpawn_DowntownAdditional { get; set; }
    public int TimeBetweenSpawn_WildernessAdditional { get; set; }
    public int TimeBetweenSpawn_RuralAdditional { get; set; }
    public int TimeBetweenSpawn_SuburbAdditional { get; set; }
    public int TimeBetweenSpawn_IndustrialAdditional { get; set; }



    [Description("Minimum distance in meters to spawn from the player.")]
    public float MinDistanceToSpawn { get; set; }
    [Description("Maximum distance in meters to spawn from the player.")]
    public float MaxDistanceToSpawn { get; set; }


    [Description("Total limit of spawned ems peds. Does not include vanilla members.")]
    public int TotalSpawnedMembersLimit { get; set; }



    [Description("Total limit of ambient spawned ems peds. Does not include vanilla members or ems members spawned by location.")]
    public int TotalSpawnedAmbientMembersLimit { get; set; }
    public int TotalSpawnedAmbientMembersLimit_Downtown { get; set; }
    public int TotalSpawnedAmbientMembersLimit_Wilderness { get; set; }
    public int TotalSpawnedAmbientMembersLimit_Rural { get; set; }
    public int TotalSpawnedAmbientMembersLimit_Suburb { get; set; }
    public int TotalSpawnedAmbientMembersLimit_Industrial { get; set; }

    public int TotalSpawnedAmbientMembersLimit_Investigation { get; set; }

    [Description("Percentage of the time to allow an ambient spawn. Minimum 0, maximum 100.")]
    public int AmbientSpawnPercentage { get; set; }
    public int AmbientSpawnPercentage_Wilderness { get; set; }
    public int AmbientSpawnPercentage_Rural { get; set; }
    public int AmbientSpawnPercentage_Suburb { get; set; }
    public int AmbientSpawnPercentage_Industrial { get; set; }
    public int AmbientSpawnPercentage_Downtown { get; set; }

    public int AmbientSpawnPercentage_Investigation { get; set; }


    [Description("If enabled, ambient spawns will happen regardless of the player wanted level. If disabled, gangs will not have ambient spawns when you are wanted.")]
    public bool AllowAmbientSpawningWhenPlayerWanted { get; set; }

    [Description("Max wanted level that the mod will ambiently spawn gang peds.")]
    public int AmbientSpawningWhenPlayerWantedMaxWanted { get; set; }


    public bool AllowStationSpawningWhenPlayerWanted { get; set; }
    [Description("Max wanted level that locations will spawn peds.")]
    public int StationSpawningWhenPlayerWantedMaxWanted { get; set; }


    public EMSSettings()
    {
        SetDefault();
    }
    public void SetDefault()
    {
        ManageDispatching = true;
        ManageTasking = true;
        ShowSpawnedBlips = false;
        RevivePercentage = 40f;
        RevivePercentagePlayer = 80f;
        AllowStationSpawning = true;
        StationSpawningIgnoresLimits = true;

        AllowAlerts = true;



        TimeBetweenSpawn = 60000;//10000;
        TimeBetweenSpawn_DowntownAdditional = 20000;
        TimeBetweenSpawn_WildernessAdditional = 90000;
        TimeBetweenSpawn_RuralAdditional = 70000;
        TimeBetweenSpawn_SuburbAdditional = 30000;
        TimeBetweenSpawn_IndustrialAdditional = 30000;


        MinDistanceToSpawn = 350f;// 50f;
        MaxDistanceToSpawn = 750f;// 1000f;// 150f;

        TotalSpawnedMembersLimit = 3;// 6;//5
        TotalSpawnedAmbientMembersLimit = 2;// 8;

        TotalSpawnedAmbientMembersLimit_Downtown = 2;
        TotalSpawnedAmbientMembersLimit_Wilderness = 0;
        TotalSpawnedAmbientMembersLimit_Rural = 1;
        TotalSpawnedAmbientMembersLimit_Suburb = 2;
        TotalSpawnedAmbientMembersLimit_Industrial = 2;
        TotalSpawnedAmbientMembersLimit_Investigation = 2;

        AmbientSpawnPercentage = 2;// 30;
        AmbientSpawnPercentage_Wilderness = 0;
        AmbientSpawnPercentage_Rural = 1;
        AmbientSpawnPercentage_Suburb = 1;// 15;
        AmbientSpawnPercentage_Industrial = 1;// 25;
        AmbientSpawnPercentage_Downtown = 5;// 40;
        AmbientSpawnPercentage_Investigation = 15;// 70;

        AllowStationSpawningWhenPlayerWanted = true;
        StationSpawningWhenPlayerWantedMaxWanted = 2;
        AllowAmbientSpawningWhenPlayerWanted = true;
        AmbientSpawningWhenPlayerWantedMaxWanted = 2;


    }
}