﻿using LosSantosRED.lsr.Interface;
using LosSantosRED.lsr.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class DrinkItem : ConsumableItem
{
    public override bool CanConsume { get; set; } = true;
    public DrinkItem()
    {
    }
    public DrinkItem(string name, ItemType itemType) : base(name, itemType)
    {

    }
    public DrinkItem(string name, string description, ItemType itemType) : base(name, description, itemType)
    {

    }
    public override bool UseItem(IActionable actionable, ISettingsProvideable settings, IEntityProvideable world, ICameraControllable cameraControllable, IIntoxicants intoxicants, ITimeControllable time)
    {
        DrinkingActivity activity = new DrinkingActivity(actionable, settings, this, intoxicants);
        if (activity.CanPerform(actionable))
        {
            base.UseItem(actionable, settings, world, cameraControllable, intoxicants, time);
            actionable.ActivityManager.StartUpperBodyActivity(activity);
            return true;
        }
        return false;
    }
    public override string PurchaseMenuDescription(ISettingsProvideable settings)
    {
        string description = base.PurchaseMenuDescription(settings);
        if (ConsumeOnPurchase)
        {
            description += $"~n~~r~Dine-In Only~s~";
        }
        return description;
    }
}

