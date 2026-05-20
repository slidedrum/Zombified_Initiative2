using Player;
using SlideMenu;
using System.Collections.Generic;
using UnityEngine;
using ZombieTweak2.Patches;

namespace ZombieTweak2.Menus
{
    public static class AttackMenuClass
    {
        public static sMenu attackMenu;
        public static sMenu.sMenuNode attackNode;
        public static sMenu.sMenuNode meleeNode;
        //public static sMenu.sMenuNode pushNode;
        public static sMenu.sMenuNode bulletNode;
        public static sMenu.sMenuNode secondaryNode;
        public static List<PlayerBotActionAttack.AttackMeansEnum> meansBlackList = new()
        {
            PlayerBotActionAttack.AttackMeansEnum.NanoSwarmDebuff,
            PlayerBotActionAttack.AttackMeansEnum.Push,
            PlayerBotActionAttack.AttackMeansEnum.Special,
        };

        public static void Setup(sMenu menu)
        {
            attackMenu = menu;
            attackNode = menu.GetNode();
            attackMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            attackMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, attackMenu.parrentMenu.Open);
            foreach (var means in AttackActionPatch.meansList)
            {
                string actionKey = "attackMeans" + means.ToString();
                OverrideTree<bool?>.Node overrideNode = zSlideComputer.ActionPermissions.AddNode(actionKey, null, "Attack", defaultValue: null, hasDefaultValue: true);
                if (meansBlackList.Contains(means))
                    continue;
                sMenu.sMenuNode menuNode = attackMenu.AddNode(means.ToString());
                overrideNode.onChanged.Listen(UpdateNodeDisplay, args: [actionKey, menuNode]);
                menuNode.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, args: [actionKey, menuNode]);
                menuNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, zSlideComputer.ActionPermissions.ResetToDefault, args: [actionKey]);
                attackMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, zSlideComputer.ActionPermissions.ResetToDefault, args: [actionKey]);
            }

            attackMenu.AddPannel(sMenu.sMenuPannel.Side.top, "This controls if the bots are allowed to atack");
            attackMenu.AddPannel(sMenu.sMenuPannel.Side.top, "And what they are allowed to attack with");
            attackMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "These settings are a bit janky atm.");
            attackMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Especially when changed in the middle of combat.");
            attackMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "I'm pretty sure that's not the fault of the mod.");
            attackMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "I'd like to see if I can improve it anyway.");
        }
        public static void UpdateNodeDisplay(string key, sMenu.sMenuNode node)
        {
            AutomaticActionMenuClass.GenericUpdateNodeDefaultDisplay(node, key, zSlideComputer.ActionPermissions);
            AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay(key, node);
        }
    }
   
}
