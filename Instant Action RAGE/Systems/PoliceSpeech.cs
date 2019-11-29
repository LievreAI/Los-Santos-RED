﻿using ExtensionsMethods;
using NAudio.Wave;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class PoliceSpeech
{
    private static Random rnd;
    private static List<string> DeadlyChaseSpeech;
    private static List<string> UnarmedChaseSpeech;
    private static List<string> CautiousChaseSpeech;
    private static List<string> ArrestedWaitSpeech;
    private static List<string> PlayerDeadSpeech;

    public static bool IsRunning { get; set; } = true;
    static PoliceSpeech()
    {
        rnd = new Random();
    }
    public static void Initialize()
    {
        SetupSpeech();
        MainLoop();
    }
    public static void Dispose()
    {
        IsRunning = false;
    }
    private static void MainLoop()
    {
        GameFiber.StartNew(delegate
        {
            try
            {
                while (IsRunning)
                {
                    CheckSpeech();
                    GameFiber.Sleep(500);
                }
            }
            catch (Exception e)
            {
                InstantAction.Dispose();
                Debugging.WriteToLog("Error", e.Message + " : " + e.StackTrace);
            }
        });
    }
    private static void SetupSpeech()
    {
        DeadlyChaseSpeech = new List<string> { "CHALLENGE_THREATEN", "COMBAT_TAUNT", "FIGHT", "GENERIC_INSULT", "GENERIC_WAR_CRY", "GET_HIM", "REQUEST_BACKUP", "REQUEST_NOOSE", "SHOOTOUT_OPEN_FIRE" };
        UnarmedChaseSpeech = new List<string> { "FOOT_CHASE", "FOOT_CHASE_AGGRESIVE", "FOOT_CHASE_LOSING", "FOOT_CHASE_RESPONSE", "GET_HIM", "SUSPECT_SPOTTED" };
        CautiousChaseSpeech = new List<string> { "DRAW_GUN", "GET_HIM", "COP_ARRIVAL_ANNOUNCE", "MOVE_IN", "MOVE_IN_PERSONAL" };
        ArrestedWaitSpeech = new List<string> { "DRAW_GUN", "GET_HIM", "COP_ARRIVAL_ANNOUNCE", "MOVE_IN", "MOVE_IN_PERSONAL","SURROUNDED" };

        PlayerDeadSpeech = new List<string> { "SUSPECT_KILLED", "WON_DISPUTE" };
    }
    private static void CheckSpeech()
    {
        try
        {
            foreach (GTACop Cop in PoliceScanning.CopPeds.Where(x => x.CanSpeak && x.DistanceToPlayer <= 45f && x.CopPed.Exists() && !x.CopPed.IsDead))
            {
                //if (rnd.Next(0, 100) <= 10)
                //    return;

                if (Cop.isTasked)
                {
                    if (InstantAction.isBusted && Cop.DistanceToPlayer <= 20f)
                    {
                        Cop.CopPed.PlayAmbientSpeech("ARREST_PLAYER");
                       // Debugging.WriteToLog("CheckSpeech", "ARREST_PLAYER");
                    }
                    else if (Police.CurrentPoliceState == Police.PoliceState.UnarmedChase)
                    {
                        string Speech = UnarmedChaseSpeech.PickRandom();
                        Cop.CopPed.PlayAmbientSpeech(Speech);
                       // Debugging.WriteToLog("CheckSpeech", Speech);
                    }
                    else if (Police.CurrentPoliceState == Police.PoliceState.CautiousChase)
                    {
                        string Speech = CautiousChaseSpeech.PickRandom();
                        Cop.CopPed.PlayAmbientSpeech(Speech);
                       // Debugging.WriteToLog("CheckSpeech", Speech);
                    }
                    else if (Police.CurrentPoliceState == Police.PoliceState.ArrestedWait)
                    {
                        string Speech = ArrestedWaitSpeech.PickRandom();
                        Cop.CopPed.PlayAmbientSpeech(Speech);
                       // Debugging.WriteToLog("CheckSpeech", Speech);
                    }
                    else if (Police.CurrentPoliceState == Police.PoliceState.DeadlyChase)
                    {
                        string Speech = DeadlyChaseSpeech.PickRandom();
                        Cop.CopPed.PlayAmbientSpeech(Speech);
                        //Debugging.WriteToLog("CheckSpeech", Speech);
                    }
                    else //Normal State
                    {
                        if(Cop.DistanceToPlayer <= 4f)
                        {
                            Cop.CopPed.PlayAmbientSpeech("CRIMINAL_WARNING");
                            //Debugging.WriteToLog("CheckSpeech", "CRIMINAL_WARNING");
                        }
                    }
                }
                else
                {
                    if(InstantAction.isDead && Cop.DistanceToPlayer <= 20f)
                    {
                        string Speech = PlayerDeadSpeech.PickRandom();
                        Cop.CopPed.PlayAmbientSpeech(Speech);
                        //Debugging.WriteToLog("CheckSpeech", Speech);
                    }
                }
                Cop.GameTimeLastSpoke = Game.GameTime - (uint)rnd.Next(500,1000);
            }
           
        }
        catch (Exception e)
        {
            Game.Console.Print(e.Message);
        }
    }

}

