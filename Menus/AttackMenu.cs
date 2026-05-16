using Player;
using SlideMenu;
using ZombieTweak2.CustomActions.Patches;

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

        public static void Setup(sMenu menu)
        {
            attackMenu = menu;
            attackNode = menu.GetNode();

            meleeNode = attackMenu.AddNode("Melee", AttackActionPatch.ToggleMeansPerms, PlayerBotActionAttack.AttackMeansEnum.Melee);
            //pushNode = attackMenu.AddNode("Push", AttackActionPatch.ToggleMeansPerms, PlayerBotActionAttack.AttackMeansEnum.Push);
            bulletNode = attackMenu.AddNode("Guns", AttackActionPatch.ToggleMeansPerms, PlayerBotActionAttack.AttackMeansEnum.Bullet);
            //secondaryNode = attackMenu.AddNode("Secondary", AttackActionPatch.ToggleMeansPerms, PlayerBotActionAttack.AttackMeansEnum.Special);

            attackMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "These settings are a bit janky atm.");
            attackMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Especially when changed in the middle of combat.");
            attackMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "I'm pretty sure that's not the fault of the mod.");
            attackMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "I'd like to see if I can improve it anyway.");
        }
    }
   
}
