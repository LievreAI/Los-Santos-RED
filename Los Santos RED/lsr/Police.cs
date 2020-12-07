﻿using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LosSantosRED.lsr
{
    public class Police
    {
        public bool IsRunning { get; set; }
        public bool AnyCanSeePlayer { get; private set; }
        public bool AnyCanHearPlayerShooting { get; private set; }
        public bool AnyCanRecognizePlayer { get; private set; }
        public bool AnyRecentlySeenPlayer { get; private set; }
        public bool AnySeenPlayerCurrentWanted { get; private set; }
        public Vector3 PlaceLastSeenPlayer { get; set; }
        public Vector3 PlayerLastSeenForwardVector { get; set; }
        public int PreviousWantedLevel { get; set; }
        public bool WasPlayerLastSeenInVehicle { get; set; }
        public float PlayerLastSeenHeading { get; set; }
        public float ActiveDistance
        {
            get
            {
                return 400f + (Mod.Player.WantedLevel * 200f);//500f
            }
        }
        private float TimeToRecognizePlayer
        {
            get
            {
                if (Mod.Player.IsNightTime)
                    return 3500;
                else if (Mod.Player.IsInVehicle)
                    return 750;
                else
                    return 2000;
            }
        }
        public void Tick()
        {
            if (IsRunning)
            {
                UpdateCops();
                UpdateRecognition();
            }
        }
        public void Reset()
        {
            AnySeenPlayerCurrentWanted = false;
        }
        private void UpdateCops()
        {
            PedManager.Cops.RemoveAll(x => !x.Pedestrian.Exists());
            PedManager.K9Peds.RemoveAll(x => !x.Pedestrian.Exists());
            foreach (Cop Cop in PedManager.Cops)
            {
                Cop.Update();
                if (Cop.ShouldBustPlayer)
                {
                    Mod.Player.StartManualArrest();
                }
            }
            foreach (Cop Cop in PedManager.Cops.Where(x => x.Pedestrian.IsDead))
            {
                PoliceSpawningManager.MarkNonPersistent(Cop);
            }
            PedManager.Cops.RemoveAll(x => x.Pedestrian.IsDead);
            VehicleManager.PoliceVehicles.RemoveAll(x => !x.Exists());
        }
        private void UpdateRecognition()
        {
            AnyCanSeePlayer = PedManager.Cops.Any(x => x.CanSeePlayer);
            AnyCanHearPlayerShooting = PedManager.Cops.Any(x => x.WithinWeaponsAudioRange);

            if (AnyCanSeePlayer)
                AnyRecentlySeenPlayer = true;
            else
                AnyRecentlySeenPlayer = PedManager.Cops.Any(x => x.SeenPlayerSince(SettingsManager.MySettings.Police.PoliceRecentlySeenTime));

            AnyCanRecognizePlayer = PedManager.Cops.Any(x => x.TimeContinuoslySeenPlayer >= TimeToRecognizePlayer || (x.CanSeePlayer && x.DistanceToPlayer <= 20f) || (x.DistanceToPlayer <= 7f && x.DistanceToPlayer > 0.01f));

            if (!AnySeenPlayerCurrentWanted && AnyRecentlySeenPlayer)
                AnySeenPlayerCurrentWanted = true;

            if (AnyRecentlySeenPlayer)
            {
                if (!AnySeenPlayerCurrentWanted)
                    PlaceLastSeenPlayer = WantedLevelManager.PlaceWantedStarted;
                else if (!Mod.Player.AreStarsGreyedOut)
                    PlaceLastSeenPlayer = Game.LocalPlayer.Character.Position;
            }

            NativeFunction.CallByName<bool>("SET_PLAYER_WANTED_CENTRE_POSITION", Game.LocalPlayer, PlaceLastSeenPlayer.X, PlaceLastSeenPlayer.Y, PlaceLastSeenPlayer.Z);
        }

    }


}
