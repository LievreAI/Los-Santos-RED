﻿using LosSantosRED.lsr.Locations;
using LSR.Vehicles;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LosSantosRED.lsr.Interface
{
    public interface ITaxiRideable
    {
        TaxiManager TaxiManager { get; }
        Vector3 Position { get; }
        Dispatcher Dispatcher { get; }
        VehicleExt CurrentVehicle { get; }
    }
}
