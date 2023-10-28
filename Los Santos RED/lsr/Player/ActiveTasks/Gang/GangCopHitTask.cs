﻿using ExtensionsMethods;
using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LosSantosRED.lsr.Player.ActiveTasks
{
    public class GangCopHitTask : GangTask
    {
       //private ITaskAssignable Player;
       // private ITimeReportable Time;
       // private IGangs Gangs;
        private IAgencies Agencies;
       // private PlayerTasks PlayerTasks;
      //  private IPlacesOfInterest PlacesOfInterest;
        private List<DeadDrop> ActiveDrops = new List<DeadDrop>();
       // private ISettingsProvideable Settings;
      //  private IEntityProvideable World;
      //  private ICrimes Crimes;
     //   private Gang HiringGang;
        private GangDen HiringGangDen;
        private Agency TargetAgency;
       // private PlayerTask CurrentTask;
        private int GameTimeToWaitBeforeComplications;
        private int MoneyToRecieve;
        private bool HasAddedComplications;
        private bool WillAddComplications;




        private int KilledMembersAtStart;




      //  private PhoneContact PhoneContact;
      //  private GangTasks GangTasks;
        public int KillRequirement { get; set; } = 1;
        private bool HasTargetAgencyAndDen => TargetAgency != null && HiringGangDen != null;

        public bool JoinGangOnComplete { get; set; } = false;




        public GangCopHitTask(ITaskAssignable player, ITimeReportable time, IGangs gangs, IPlacesOfInterest placesOfInterest, ISettingsProvideable settings, IEntityProvideable world,
    ICrimes crimes, IWeapons weapons, INameProvideable names, IPedGroups pedGroups, IShopMenus shopMenus, IModItems modItems, PlayerTasks playerTasks, GangTasks gangTasks, PhoneContact hiringContact, 
    Gang hiringGang, Agency targetAgency, IAgencies agencies) : base(player, time, gangs, placesOfInterest, settings, world, crimes, weapons, names, pedGroups, shopMenus, modItems, playerTasks, gangTasks, hiringContact, hiringGang)
        {
            DebugName = "Cop Hit";
            RepOnCompletion = 2000;
            DebtOnFail = 0;
            RepOnFail = -500;
            DaysToComplete = 7;
            TargetAgency = targetAgency;
            Agencies = agencies;
        }

        public override void Setup()
        {

        }
        public override void Dispose()
        {

        }
        public override void Start()
        {
            if (PlayerTasks.CanStartNewTask(HiringGang?.ContactName))
            {
                GetTargetAgency();
                GetHiringDen();
                if (HasTargetAgencyAndDen)
                {
                    GetPayment();
                    SendInitialInstructionsMessage();
                    AddTask();
                    GameFiber PayoffFiber = GameFiber.StartNew(delegate
                    {
                        try
                        {
                            Loop();
                            FinishTask();
                        }
                        catch (Exception ex)
                        {
                            EntryPoint.WriteToConsole(ex.Message + " " + ex.StackTrace, 0);
                            EntryPoint.ModController.CrashUnload();
                        }
                    }, "PayoffFiber");
                }
                else
                {
                    GangTasks.SendGenericTooSoonMessage(HiringContact);
                }
            }
        }
        protected override void Loop()
        {
            while (true)
            {
                CurrentTask = PlayerTasks.GetTask(HiringGang.ContactName);
                if (CurrentTask == null || !CurrentTask.IsActive)
                {
                    break;
                }
                int killedCops = Player.Violations.DamageViolations.CountKilledCopsByAgency(TargetAgency.ID);
                if (killedCops >= KilledMembersAtStart + KillRequirement)
                {
                    CurrentTask.OnReadyForPayment(true);
                    break;
                }
                GameFiber.Sleep(1000);
            }
        }
        protected override void FinishTask()
        {
            if (CurrentTask != null && CurrentTask.IsActive && CurrentTask.IsReadyForPayment)
            {
                GameFiber.Sleep(RandomItems.GetRandomNumberInt(5000, 15000));
                SendMoneyPickupMessage();
            }
            else
            {
                Dispose();
            }
        }
        private void GetTargetAgency()
        {
            // TargetGang = null;
            //if (HiringGang.EnemyGangs != null && HiringGang.EnemyGangs.Any())
            //{
            //    TargetGang = Gangs.GetGang(HiringGang.EnemyGangs.PickRandom());
            //}
            if (TargetAgency == null)
            {
                TargetAgency = Agencies.GetAgenciesByResponse(ResponseType.LawEnforcement).PickRandom();// Gangs.GetAllGangs().Where(x => x.ID != HiringGang.ID).PickRandom();
            }
        }
        private void GetHiringDen()
        {
            HiringGangDen = PlacesOfInterest.GetMainDen(HiringGang.ID, World.IsMPMapLoaded);
        }
        protected override void GetPayment()
        {
            MoneyToRecieve = RandomItems.GetRandomNumberInt(HiringGang.CopHitPaymentMin, HiringGang.CopHitPaymentMax).Round(500);
            if (MoneyToRecieve <= 0)
            {
                MoneyToRecieve = 500;
            }
        }
        protected override void AddTask()
        {
            GameTimeToWaitBeforeComplications = RandomItems.GetRandomNumberInt(3000, 10000);
            HasAddedComplications = false;
            WillAddComplications = false;// RandomItems.RandomPercent(Settings.SettingsManager.TaskSettings.RivalGangHitComplicationsPercentage);
            KilledMembersAtStart = Player.Violations.DamageViolations.CountKilledCopsByAgency(TargetAgency.ID);

            //GangReputation gr = Player.RelationshipManager.GangRelationships.GetReputation(TargetAgency);
            //KilledMembersAtStart = gr.MembersKilled;

            EntryPoint.WriteToConsole($"Starting Gang Cop Hit, KilledMembersAtStart {KilledMembersAtStart}");

            PlayerTasks.AddTask(HiringGang.Contact, MoneyToRecieve, 2000, 0, -500, 7, "Cop Hit");
        }
        protected override void SendInitialInstructionsMessage()
        {
            List<string> Replies = new List<string>() {
                $"The pigs at {TargetAgency.ColorPrefix}{TargetAgency.ShortName}~s~ have fucked with us for the last time? Be sure to waste {KillRequirement} of those pricks. Once you are done come back to the {HiringGang.DenName} on {HiringGangDen.FullStreetAddress}. ${MoneyToRecieve} to you",
                $"{TargetAgency.ColorPrefix}{TargetAgency.ShortName}~s~ is starting to become an issue. Get rid of {KillRequirement} of those assholes. When you are finished, get back to the {HiringGang.DenName} on {HiringGangDen.FullStreetAddress}. I'll have ${MoneyToRecieve} waiting for you.",
                $"The pigs got their noses in our business, need you to waste {KillRequirement} of those {TargetAgency.ColorPrefix}{TargetAgency.ShortName}~s~ pricks. Come back to the {HiringGang.DenName} on {HiringGangDen.FullStreetAddress} for your payment of ${MoneyToRecieve}",
                    };
            Player.CellPhone.AddPhoneResponse(HiringGang.Contact.Name, HiringGang.Contact.IconName, Replies.PickRandom());
        }
        private void SendMoneyPickupMessage()
        {
            List<string> Replies = new List<string>() {
                                $"Seems like that thing we discussed is done? Come by the {HiringGang.DenName} on {HiringGangDen.FullStreetAddress} to collect the ${MoneyToRecieve}",
                                $"Word got around that you are done with that thing for us, Come back to the {HiringGang.DenName} on {HiringGangDen.FullStreetAddress} for your payment of ${MoneyToRecieve}",
                                $"Get back to the {HiringGang.DenName} on {HiringGangDen.FullStreetAddress} for your payment of ${MoneyToRecieve}",
                                $"{HiringGangDen.FullStreetAddress} for ${MoneyToRecieve}",
                                $"Heard you were done, see you at the {HiringGang.DenName} on {HiringGangDen.FullStreetAddress}. We owe you ${MoneyToRecieve}",
                                };
            Player.CellPhone.AddScheduledText(HiringContact, Replies.PickRandom(), 1, false);
        }
    }
}
