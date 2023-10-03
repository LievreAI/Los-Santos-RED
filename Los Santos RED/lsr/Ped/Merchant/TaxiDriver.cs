﻿using LosSantosRED.lsr.Interface;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class TaxiDriver : PedExt
{
    public TaxiFirm TaxiFirm { get; set; }
    public TaxiRide TaxiRide { get; set; }
    public TaxiDriver(Ped _Pedestrian, ISettingsProvideable settings, ICrimes crimes, IWeapons weapons, string _Name, string groupName, IEntityProvideable world, bool wasModSpawned) : base(_Pedestrian, settings, crimes, weapons, _Name, groupName, world)
    {
        WasModSpawned = wasModSpawned;
        PedBrain = new TaxiDriverBrain(this, Settings, world, weapons, this);
    }
    public void SetTaxiRide(TaxiRide taxiRide)
    {
        if(TaxiRide != null)
        {
            TaxiRide.Cancel();
        }
        TaxiRide = taxiRide;
    }
}

