﻿using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


public class VanillaCarGeneratorManager
{
    private bool isRandomEventsDisabled;
    private bool IsVanillaRespawnActive = true;
    private ISettingsProvideable Settings;
    private List<Vector3> CarGenerators;

    public VanillaCarGeneratorManager(ISettingsProvideable settings)
    {
        Settings = settings;
    }
    public void Setup()
    {
        CarGenerators = new List<Vector3>()
        {
        //POLICE
        //Mission Row
        new Vector3(407.7554f, -984.2084f, 29.89806f),
        new Vector3(408.0923f, -988.8488f, 29.85523f),
        new Vector3(407.9883f, -998.3094f, 29.81112f),
        new Vector3(427.5909f, -1027.707f, 29.22805f),
        new Vector3(434.9848f, -1027.103f, 29.12844f),
        new Vector3(442.5143f, -1026.687f, 28.98147f),
        new Vector3(446.3985f, -1026.087f, 28.92508f),
        new Vector3(496.3687f, -996.015f, 28.3387f),
        new Vector3(496.3721f, -1016.798f, 28.687f),
        //Davis
        new Vector3(388.9854f, -1612.977f, 29.21355f),
        new Vector3(392.7548f, -1608.376f, 29.21355f),
        new Vector3(399.393f, -1621.396f, 29.20119f),
        new Vector3(351.3006f, -1556.711f, 29.24393f),
        //Del Perro Pier
        new Vector3(-1625.343f, -1013.629f, 13.89048f),
        //Vespucci
        new Vector3(-1072.822f, -880.3561f, 4.809089f),
        new Vector3(-1075.95f, -882.3492f, 4.809089f),
        new Vector3(-1051.726f, -867.1277f, 4.809089f),
        new Vector3(-1045.53f, -861.5321f, 4.809089f),
        new Vector3(-1042.226f, -857.9979f, 4.809089f),
        new Vector3(-1047.814f, -846.7044f, 4.809089f),
        new Vector3(-1052.352f, -846.8544f, 4.809089f),
        new Vector3(-1055.076f, -849.5068f, 4.809089f),
        new Vector3(-1058.752f, -851.4465f, 4.809089f),
        new Vector3(-1126.5f, -864.8307f, 13.63185f),
        new Vector3(-1122.896f, -863.4746f, 13.6122f),
        new Vector3(-1115.937f, -857.7859f, 13.65187f),
        new Vector3(-1138.613f, -845.7916f, 13.98058f),
        //Richman
        new Vector3(-574.256f, -168.7862f, 38.49216f),
        new Vector3(-557.7739f, -161.9565f, 38.62593f),
        new Vector3(-559.7783f, -147.1107f, 38.65532f),
        new Vector3(-555.6528f, -145.4567f, 38.72502f),
        new Vector3(-551.0397f, -144.0132f, 38.65663f),
        //Paleto
        new Vector3(-454.4307f, 6040.472f, 31.17424f),
        new Vector3(-457.8087f, 6043.849f, 31.17424f),
        new Vector3(-468.1928f, 6038.506f, 31.17422f),
        new Vector3(-475.1396f, 6031.421f, 31.17419f),
        new Vector3(-458.788f, 6005.529f, 31.17422f),
        new Vector3(-455.154f, 6001.894f, 31.17422f),
        //Sandy
        new Vector3(1831.629f, 3662.91f, 33.92607f),
        new Vector3(1835.127f, 3664.892f, 33.92607f),
        new Vector3(1847.131f, 3672.587f, 33.92607f),
        //Vinewood
        new Vector3(666.4646f, -11.79645f, 82.76681f),
        new Vector3(621.6934f, 26.27448f, 88.66011f),
        new Vector3(615.3292f, 28.48364f, 89.28476f),
        new Vector3(609.7616f, 30.84756f, 89.91243f),
        new Vector3(597.8726f, 34.8121f, 91.07567f),
        new Vector3(586.7384f, 37.78894f, 92.30818f),
        new Vector3(580.8351f, 38.85468f, 92.82274f),
        //La Mesa
        new Vector3(822.8635f, -1258.039f, 26.34347f),
        new Vector3(828.0373f, -1258.039f, 26.34347f),
        new Vector3(833.577f, -1258.954f, 26.34347f),
        new Vector3(838.7137f, -1271.55f, 26.34347f),
        new Vector3(833.4279f, -1271.55f, 26.34347f),
        new Vector3(822.5837f, -1271.55f, 26.34347f),
        new Vector3(828.1719f, -1333.792f, 26.18776f),
        new Vector3(828.1719f, -1339.649f, 26.18776f),
        new Vector3(828.1719f, -1345.815f, 26.18776f),
        new Vector3(828.362f, -1351.482f, 26.21234f),
        new Vector3(843.9627f, -1334.354f, 26.17253f),
        new Vector3(843.874f, -1340.518f, 26.18776f),
        new Vector3(844.3544f, -1346.3f, 26.21234f),
        new Vector3(843.7897f, -1352.283f, 26.21234f),
        new Vector3(865.7624f, -1378.407f, 26.21234f),
        new Vector3(862.8425f, -1383.55f, 26.21234f),
        new Vector3(859.8381f, -1388.58f, 26.21234f),
        new Vector3(857.2202f, -1393.802f, 26.21234f),
        new Vector3(854.2248f, -1398.952f, 26.21234f),
        };


        if (Settings.SettingsManager.VanillaSettings.BlockVanillaPoliceCarGenerators)
        {
            foreach (Vector3 carGenSPot in CarGenerators)
            {
                NativeFunction.Natives.SET_ALL_VEHICLE_GENERATORS_ACTIVE_IN_AREA(carGenSPot.X - 2f, carGenSPot.Y - 2f, carGenSPot.Z - 2f, carGenSPot.X + 2f, carGenSPot.Y + 2f, carGenSPot.Z + 2f, false, false);


                NativeFunction.Natives.REMOVE_VEHICLES_FROM_GENERATORS_IN_AREA(carGenSPot.X - 2f, carGenSPot.Y - 2f, carGenSPot.Z - 2f, carGenSPot.X + 2f, carGenSPot.Y + 2f, carGenSPot.Z + 2f, false);
            }
        }



        List<Vector3> CarScenarios = new List<Vector3>()
        {
            new Vector3(838.9286f, -1259.166f, 25.38143f),
            new Vector3(837.0612f, -1257.972f, 25.36833f),
            new Vector3(824.9189f, -1263.116f, 25.26017f),
            new Vector3(455.6195f, -1026.146f, 27.47295f),
            new Vector3(438.6183f, -1026.667f, 27.7867f),
            new Vector3(407.7591f, -1005.385f, 28.26613f),
            new Vector3(407.9486f, -1008.231f, 28.26636f),
            new Vector3(-562.8094f, -148.4117f, 37.01614f),
            new Vector3(-548.7076f, -141.6616f, 37.29326f),
            new Vector3(-1039.343f, -855.5767f, 3.875723f),
            new Vector3(-1048.334f, -864.5882f, 3.992599f),
            new Vector3(-1069.269f, -878.6844f, 3.855752f),
            new Vector3(-1128.474f, -866.9715f, 12.51155f),
            new Vector3(-1120.375f, -858.11f, 12.52164f),
            new Vector3(-1112.189f, -855.1146f, 12.53287f),
            new Vector3(-1125.52f, -816.9467f, 14.99161f),
            new Vector3(-1104.038f, -797.9f, 17.30989f),
            new Vector3(-1112.705f, -804.9168f, 16.46241f),
            new Vector3(1868.631f, 3685.344f, 32.75556f),
            new Vector3(1866.067f, 3683.386f, 32.72648f),
            new Vector3(1862.838f, 3682.098f, 32.71787f),
            new Vector3(1853.62f, 3675.808f, 32.75896f),
            new Vector3(-482.731f, 6024.876f, 30.34054f),
            new Vector3(-479.5346f, 6028.117f, 30.34054f),
            new Vector3(-437.7312f, 6087.632f, 30.42623f),
            new Vector3(-431.8985f, 6032.511f, 30.34053f),
        };


        if(Settings.SettingsManager.VanillaSettings.BlockVanillaPoliceCarGenerators)
        {
            float ScenarioBlockingDistance = 2f;
            foreach (Vector3 carScenario in CarScenarios)
            {
                NativeFunction.Natives.ADD_SCENARIO_BLOCKING_AREA<int>(carScenario.X - ScenarioBlockingDistance, carScenario.Y - ScenarioBlockingDistance, carScenario.Z - ScenarioBlockingDistance, carScenario.X + ScenarioBlockingDistance, carScenario.Y + ScenarioBlockingDistance, carScenario.Z + ScenarioBlockingDistance, false, true, true, true);
            }
        }
    }
    public void Dispose()
    {
        NativeFunction.Natives.SET_ALL_VEHICLE_GENERATORS_ACTIVE();
        NativeFunction.Natives.REMOVE_SCENARIO_BLOCKING_AREAS();
    }
    public void Tick()
    {
      
    }
}

