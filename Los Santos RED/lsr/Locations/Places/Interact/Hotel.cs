﻿using ExtensionsMethods;
using LosSantosRED.lsr.Interface;
using Rage;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

public class Hotel : InteractableLocation
{
    private bool KeepInteractionGoing = false;
    public Hotel() : base()
    {

    }
    public override string TypeName { get; set; } = "Hotel";
    public override int MapIcon { get; set; } = 475;
    public override Color MapIconColor { get; set; } = Color.White;
    public override float MapIconScale { get; set; } = 1.0f;
    public override string ButtonPromptText { get; set; }

    public List<HotelRoom> HotelRooms { get; set; } = new List<HotelRoom>();

    public Hotel(Vector3 _EntrancePosition, float _EntranceHeading, string _Name, string _Description, string menuID) : base(_EntrancePosition, _EntranceHeading, _Name, _Description)
    {
        MenuID = menuID;
    }
    public override bool CanCurrentlyInteract(ILocationInteractable player)
    {
        ButtonPromptText = $"Stay At {Name}";
        return true;
    }
    public override void OnInteract(ILocationInteractable player, IModItems modItems, IEntityProvideable world, ISettingsProvideable settings, IWeapons weapons, ITimeControllable time, IPlacesOfInterest placesOfInterest)
    {
        Player = player;
        ModItems = modItems;
        World = world;
        Settings = settings;
        Weapons = weapons;
        Time = time;

        if (IsLocationClosed())
        {
            return;
        }

        if (CanInteract)
        {
            Player.ActivityManager.IsInteractingWithLocation = true;
            CanInteract = false;
            GameFiber.StartNew(delegate
            {
                try
                {
                    StoreCamera = new LocationCamera(this, Player);
                    StoreCamera.Setup();
                    CreateInteractionMenu();
                    InteractionMenu.Visible = true;
                    InteractionMenu.OnItemSelect += InteractionMenu_OnItemSelect;
                    GenerateHotelMenu();
                    while (IsAnyMenuVisible || Time.IsFastForwarding || KeepInteractionGoing)
                    {
                        MenuPool.ProcessMenus();
                        GameFiber.Yield();
                    }
                    DisposeInteractionMenu();
                    StoreCamera.Dispose();
                    Player.ActivityManager.IsInteractingWithLocation = false;
                    CanInteract = true;
                }
                catch (Exception ex)
                {
                    EntryPoint.WriteToConsole("Location Interaction" + ex.Message + " " + ex.StackTrace, 0);
                    EntryPoint.ModController.CrashUnload();
                }
            }, "HotelInteract");
        }
    }
    private void GenerateHotelMenu()
    {
        foreach(MenuItem menuItem in Menu.Items)
        {
            ModItem modItem = ModItems.Get(menuItem.ModItemName);
            AddRoomEntry(menuItem, modItem);
        }
    }
    private void AddRoomEntry(MenuItem cii, ModItem myItem)
    {
        if (cii != null && myItem != null)
        {
            string formattedPurchasePrice = cii.PurchasePrice.ToString("C0");
            string description = myItem.Description;
            if (description == "")
            {
                description = $"{cii.ModItemName} {formattedPurchasePrice}";
            }
            description += "~n~~s~";
            InteractionMenu.AddItem(new UIMenuNumericScrollerItem<int>(cii.ModItemName, description, 1, 7, 1) { Formatter = v => $"{(v == 1 && myItem.MeasurementName == "Item" ? "" : v.ToString() + " ")}{(myItem.MeasurementName != "Item" || v > 1 ? myItem.MeasurementName : "")}{(v > 1 ? "(s)" : "")}{(myItem.MeasurementName != "Item" || v > 1 ? " - " : "")}${(v * cii.PurchasePrice)}", Value = 1 });
        }
    }
    private void InteractionMenu_OnItemSelect(RAGENativeUI.UIMenu sender, UIMenuItem selectedItem, int index)
    {
        ModItem selectedModItem = ModItems.Get(selectedItem.Text);
        MenuItem menuItem = Menu.Items.Where(x => x.ModItemName == selectedItem.Text).FirstOrDefault();
        if (selectedModItem != null && menuItem != null)
        {
            int TotalNights = 1;
            if (selectedItem.GetType() == typeof(UIMenuNumericScrollerItem<int>))
            {
                UIMenuNumericScrollerItem<int> myItem = (UIMenuNumericScrollerItem<int>)selectedItem;
                TotalNights = myItem.Value;
            }
            int TotalPrice = menuItem.PurchasePrice * TotalNights;
            StayAtHotel(TotalPrice, TotalNights);
        }
    }
    private void StayAtHotel(int Price, int Nights)
    {
        if (Player.BankAccounts.Money >= Price)
        {
            Player.BankAccounts.GiveMoney(-1 * Price);
            Time.FastForward(new DateTime(Time.CurrentYear, Time.CurrentMonth, Time.CurrentDay + Nights, 11, 0, 0));
            Player.IsResting = true;
            Player.IsSleeping = true;
            KeepInteractionGoing = true;
            InteractionMenu.Visible = false;

            bool isInRoom = false;

            if(HotelRooms.Any() && Settings.SettingsManager.WorldSettings.HotelsUsesRooms)
            {
                
                HotelRoom hotelRoom = HotelRooms.PickRandom();
                if(hotelRoom != null)
                {
                    isInRoom = true;
                    StoreCamera.MoveToPosition(hotelRoom.CameraPosition, hotelRoom.CameraDirection, hotelRoom.CameraRotation);
                }

            }

            Player.ButtonPrompts.AddPrompt("HotelStay", "Cancel Stay", "CancelHotelStay", Settings.SettingsManager.KeySettings.InteractCancel, 99);
            GameFiber FastForwardWatcher = GameFiber.StartNew(delegate
            {
                while (Time.IsFastForwarding)
                {
                    if (!Settings.SettingsManager.NeedsSettings.ApplyNeeds)
                    {
                        Player.HealthManager.ChangeHealth(1);
                    }
                    if (Player.ButtonPrompts.IsPressed("CancelHotelStay"))
                    {
                        Time.StopFastForwarding();
                    }
                    GameFiber.Yield();
                }
                Player.IsResting = false;
                Player.IsSleeping = false;
                Player.ButtonPrompts.RemovePrompts("HotelStay");
                Game.DisplayNotification("CHAR_BLANK_ENTRY", "CHAR_BLANK_ENTRY", Name, "~g~Purchased", $"Thank you for staying at {Name}");
                InteractionMenu.Visible = true;
                KeepInteractionGoing = false;


                if(isInRoom)
                {
                    isInRoom = false;
                    StoreCamera.ReHighlightStoreWithCamera();
                }

            }, "FastForwardWatcher");
        }
        else
        {
            Game.DisplayNotification("CHAR_BLANK_ENTRY", "CHAR_BLANK_ENTRY", Name, "~r~Purchase Failed", "We are sorry, we are unable to complete this transation. Please make sure you have the funds.");
        }
    
    }
}

