﻿using LosSantosRED.lsr.Interface;
using Rage;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

public class Restaurant : InteractableLocation
{
    public Restaurant() : base()
    {

    }
    public FoodType FoodType { get; set; } = FoodType.Generic;
    public override string TypeName { get; set; } = "Restaurant";
    public override int MapIcon { get; set; } = 621;
    public override Color MapIconColor { get; set; } = Color.White;
    public override float MapIconScale { get; set; } = 1.0f;
    public override string ButtonPromptText { get; set; }
    public Restaurant(Vector3 _EntrancePosition, float _EntranceHeading, string _Name, string _Description, string menuID,FoodType foodType) : base(_EntrancePosition, _EntranceHeading, _Name, _Description)
    {
        MenuID = menuID;
        FoodType = foodType;
    }
    public override bool CanCurrentlyInteract(ILocationInteractable player)
    {
        ButtonPromptText = $"Dine at {Name}";
        return true;
    }
    public override List<Tuple<string, string>> DirectoryInfo(int currentHour, float distanceTo)
    {
        List<Tuple<string, string>> BaseList = base.DirectoryInfo(currentHour, distanceTo).ToList();
        BaseList.Add(Tuple.Create("Type: ", FoodType.ToString()));
        return BaseList;
    }
}

