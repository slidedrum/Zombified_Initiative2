using LevelGeneration;
using Player;
using System.Linq;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionPickupItem : PressAction
    {
        public override string FriendlyName => "Pickup Item";
        public override string FriendlyNameShort => "Pickup";
        public override bool Invoke(Component BestComponent)
        {
            ItemInLevel Item = BestComponent.Cast<ItemInLevel>();
            if (Item == null)
                return false;
            PlayerAIBot BestBot = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot == null)
                return false;
            zBotActions.SendBotToPickupItem(BestBot, Item, zStaticRefrences.LocalPlayer);
            return true;
        }
    }
}
