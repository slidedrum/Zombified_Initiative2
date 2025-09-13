using Agents;
using Dissonance;
using Enemies;
using GameData;
using Gear;
using GTFO.API;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LevelGeneration;
using Localization;
using Player;
using SNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ZombieTweak2;
using static Il2CppSystem.Xml.XmlWellFormedWriter.AttributeValueCache;

namespace Zombified_Initiative
{
    public class botAction(PlayerAIBot bot, Item targetItem, LG_ResourceContainer_Storage targetContainer, Vector3 targetPosition, float prio, float haste, string message, int func, int slot, int itemtype, int itemserial, int agentid,int depth, PlayerBotActionBase.Descriptor descriptor)
    {
        public PlayerAIBot Bot = bot;
        public Item TargetItem = targetItem;
        public LG_ResourceContainer_Storage TargetContainer = targetContainer;
        public Vector3 TargetPosition = targetPosition;
        public float Prio = prio;
        public float Haste = haste;
        public string Message = message;
        public int Func = func;
        public int Slot = slot;
        public int Itemtype = itemtype;
        public int Itemserial = itemserial;
        public int Agentid = agentid;
        public int Depth = depth;
        public PlayerBotActionBase.Descriptor descriptor = descriptor;
        public int incrementDepth()
        {
            Depth += 1;
            return Depth;
        }
    }
    public class zController : MonoBehaviour
    {

        // private CustomMenu _customMenu;
        public static CommunicationNode zombmenu;

        public static int _highlightedMenuButtonIndex = 0;
        public static float _manualActionsPriority = 5f;
        public static float _manualActionsHaste = 1f;
        public static bool _preventAutoPickups = true;
        public static bool _preventAutoUses = true;
        public static bool _preventManual = false;
        public static bool _debug = true;
        public static bool _menuadded = false;
        private static float lastupdatetime = 0f;
        


        public static void ReceiveZINetInfo(ulong sender, Zi.ZINetInfo netInfo)
        {
            // funktio attack 0
            // funktio toggleshare 1
            // funktio togglepickup 2
            // funktio pickuppack 3
            // funktio sharepack 4
            // funktio cancel 5

            EnemyAgent? enemy = null;
            ItemInLevel? item = null;
            Agent agent = null;
            zComputer? zombie = null;
            String botname = "";
            int itemtype = netInfo.ITEMTYPE;
            int itemserial = netInfo.ITEMSERIAL;
            int senderindex = 9; //default index 9 means not found

            // if we get data from host or client, we do it here
            Debug.Log($"received data from sender " + sender + ": func:" + netInfo.FUNC + " slot:" + netInfo.SLOT + " itemtype:" + netInfo.ITEMTYPE + " itemserial:" + netInfo.ITEMSERIAL + " enemyid:" + netInfo.AGENTID); // debug poista
            if (!SNet.IsMaster) return;

            for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
            {//check each player to find the sender
                var iAgent = PlayerManager.PlayerAgentsInLevel[i];
                if (sender == iAgent.m_replicator.OwningPlayer.Lookup) senderindex = i;
            }
            if (senderindex == 9) return; // sender not found

            PlayerAgent senderAgent = PlayerManager.PlayerAgentsInLevel[senderindex];
            Zi.log.LogInfo($"player {senderAgent.PlayerName} is sender {senderAgent.Sync.Replicator.OwningPlayer.Lookup} in slot {senderAgent.PlayerSlotIndex}");
            // get agent by repkey
            if (netInfo.AGENTID > 0)
            {
                SNetStructs.pReplicator pRep;
                pRep.keyPlusOne = (ushort)netInfo.AGENTID;
                pAgent _agent;
                _agent.pRep = pRep;
                _agent.TryGet(out agent);
            }

            // get item by type and serial
            if (itemtype > 0 && itemserial > 0)
            {
                foreach (var dimension in Builder.CurrentFloor.m_dimensions)
                    foreach (var tile in dimension.Tiles)
                        foreach (var pickup in tile.m_geoRoot.GetComponentsInChildren<ResourcePackPickup>())
                        {
                            if (pickup.m_packType == eResourceContainerSpawnType.AmmoWeapon     && netInfo.ITEMTYPE == 1 && pickup.m_serialNumber == netInfo.ITEMSERIAL) item = pickup.TryCast<ItemInLevel>();
                            if (pickup.m_packType == eResourceContainerSpawnType.AmmoTool       && netInfo.ITEMTYPE == 2 && pickup.m_serialNumber == netInfo.ITEMSERIAL) item = pickup.TryCast<ItemInLevel>();
                            if (pickup.m_packType == eResourceContainerSpawnType.Health         && netInfo.ITEMTYPE == 3 && pickup.m_serialNumber == netInfo.ITEMSERIAL) item = pickup.TryCast<ItemInLevel>();
                            if (pickup.m_packType == eResourceContainerSpawnType.Disinfection   && netInfo.ITEMTYPE == 4 && pickup.m_serialNumber == netInfo.ITEMSERIAL) item = pickup.TryCast<ItemInLevel>();
                        }//jesus christ is this inefficient but I can't think of a better way right now /shrug 
            }
            if (item == null)
            {
                print(senderAgent.PlayerName + " item not found, type " + itemtype + " serial " + itemserial);
            }

            // get bot by slot id
            if (netInfo.SLOT < 8)
            {
                foreach (PlayerAIBot bot in Zi.BotTable.Values) 
                    if (bot.Agent.PlayerSlotIndex == netInfo.SLOT)
                    {
                        zombie = bot.GetComponent<zComputer>();
                        botname = bot.Agent.PlayerName;
                    }
            }

            if (netInfo.FUNC == 0) //attack
            {
                enemy = agent.TryCast<EnemyAgent>();
                if (enemy == null) return;
                if (netInfo.SLOT == 8)//all bots
                {
                    foreach (KeyValuePair<String, PlayerAIBot> bt in Zi.BotTable)
                    {
                        Zi.SendBotToKillEnemy(bt.Key, enemy, PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
                    }
                }
                else if (netInfo.SLOT < 8) //one bot
                {
                    Zi.SendBotToKillEnemy(botname, enemy, PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
                }
            }


            if (netInfo.FUNC == 1) //toggleshare
            {
                if (netInfo.SLOT == 8)//all bots
                {
                    foreach (PlayerAIBot iBot in Zi.BotTable.Values)
                    {
                        iBot.GetComponent<zComputer>().toggleSharePermission();
                    }
                }
                else if (netInfo.SLOT < 8)//one bot
                {
                    zombie.toggleSharePermission();
                }
            }
            if (netInfo.FUNC == 2) //togglepickup
            {
                if (netInfo.SLOT == 8)//all bots
                {
                    foreach (PlayerAIBot iBot in Zi.BotTable.Values)
                    {
                        iBot.GetComponent<zComputer>().togglePickupPermission();
                    }
                }
                else if (netInfo.SLOT < 8)//one bot
                {
                    zombie.togglePickupPermission();
                }
            }
            if (netInfo.FUNC == 3) //pickup pack
            {
                Zi.SendBotToPickupItem(botname, item);
    //            Zi.ExecuteBotAction(zombie.GetComponent<PlayerAIBot>(), new PlayerBotActionCollectItem.Descriptor(zombie.GetComponent<PlayerAIBot>())
    //            {
    //                TargetItem = item,
    //                TargetContainer = item.container,
    //                TargetPosition = item.transform.position,
    //                Prio = _manualActionsPriority,
    //                Haste = _manualActionsHaste,
    //            },
    //"Added collect item action to " + botname, 4, zombie.GetComponent<PlayerAgent>().PlayerSlotIndex, itemtype, itemserial, 0);
            }

            if (netInfo.FUNC == 4) //share pack
            {
                Zi.SendBotToShareResourcePack(botname, agent.TryCast<PlayerAgent>(), senderAgent);
    //            PlayerAgent human = agent.TryCast<PlayerAgent>();
    //            if (human == null) return;

    //            BackpackItem backpackItem = null;
    //            var gotBackpackItem = zombie.GetComponent<PlayerAIBot>().Backpack.HasBackpackItem(InventorySlot.ResourcePack) &&
    //                                  zombie.GetComponent<PlayerAIBot>().Backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out backpackItem);
    //            if (!gotBackpackItem)
    //                return;

    //            var resourcePack = backpackItem.Instance.Cast<ItemEquippable>();
    //            zombie.GetComponent<PlayerAIBot>().Inventory.DoEquipItem(resourcePack);

    //            Zi.ExecuteBotAction(zombie.GetComponent<PlayerAIBot>(), new PlayerBotActionShareResourcePack.Descriptor(zombie.GetComponent<PlayerAIBot>())
    //            {
    //                Receiver = human,
    //                Item = resourcePack,
    //                Prio = _manualActionsPriority,
    //                Haste = _manualActionsHaste,
    //            },
    //"Added share resource action to " + zombie.GetComponent<PlayerAIBot>().Agent.PlayerName, 4, zombie.GetComponent<PlayerAIBot>().m_playerAgent.PlayerSlotIndex, 0, 0, human.m_replicator.Key + 1);
            }


            if (netInfo.FUNC == 5) //cancel
            {
                if (netInfo.SLOT == 8)//all bots
                {
                    foreach (PlayerAIBot iBot in Zi.BotTable.Values)
                    {
                        iBot.GetComponent<zComputer>().PreventManualActions();
                    }
                }
                else if (netInfo.SLOT < 8)
                {
                    zombie.PreventManualActions();
                }
            }
        }

        public void Awake()
        {
            Zi.BotTable.Clear();
        }

        public void OnFactoryBuildDone()
        {
            Zi.BotTable.Clear();
            foreach (var p in PlayerManager.PlayerAgentsInLevel) if (p.Owner.IsBot) Zi.BotTable.Add(p.PlayerName, p.GetComponent<PlayerAIBot>());
            Zi._menu = FindObjectOfType<PUI_CommunicationMenu>();
            if (!_menuadded)
            {
                AddZombifiedText();
                AddZombifiedMenu();
                Zi.rootmenusetup = true;
                _menuadded = true;
            }
        }


        public static void AddZombifiedText()
        {
            TextDataBlock zombtext1 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext1", English = "Zombified Initiative" };
            TextDataBlock zombtext2 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext2", English = "AllBots attack my target" };
            TextDataBlock zombtext3 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext3", English = "AllBots toggle pickup permission" };
            TextDataBlock zombtext4 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext4", English = "AllBots clear command queue" };
            TextDataBlock zombtext5 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext5", English = "AllBots toggle share permission" };
            TextDataBlock zombtext6 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext6", English = "All Bots" };
            TextDataBlock zombtext7 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext7", English = "AllBots toggle sentry mode" };


            TextDataBlock.AddBlock(zombtext1);
            TextDataBlock.AddBlock(zombtext2);
            TextDataBlock.AddBlock(zombtext3);
            TextDataBlock.AddBlock(zombtext4);
            TextDataBlock.AddBlock(zombtext5);
            TextDataBlock.AddBlock(zombtext6);
            TextDataBlock.AddBlock(zombtext7);

            var localizationService = Text.TextLocalizationService.TryCast<GameDataTextLocalizationService>();
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext1"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext1"), zombtext1.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext2"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext2"), zombtext2.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext3"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext3"), zombtext3.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext4"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext4"), zombtext4.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext5"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext5"), zombtext5.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext6"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext6"), zombtext6.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext7"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext7"), zombtext7.GetText(localizationService.CurrentLanguage));

        }

        public void Initialize()
        {
            if (!SNet.IsMaster) foreach (KeyValuePair<String, PlayerAIBot> bt in Zi.BotTable)
                {
                    var tmpcomp = bt.Value.gameObject.AddComponent<zComputer>();
                    tmpcomp.Initialize();
                }
        }

        private void Update()
        {
            zActionSub.update();
            ZMenuManger.Update();
            bool ready = (FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead);
            if (ready)
            {
                zMenus.setupRadialMenus();
                zSmartSelect.update();
                if (Input.GetKeyDown(KeyCode.L))
                    SwitchDebug();
                //if (Input.GetKeyDown(KeyCode.P))
                /// bot under aim, stop? no aim? all stop?
                //  PreventManualActions();

                if (Input.GetKeyDown(KeyCode.J))
                {
                    if (SNet.IsMaster)
                    {
                        foreach (KeyValuePair<string, PlayerAIBot> bt in Zi.BotTable)
                        {
                            zComputer zombie = bt.Value.GetComponent<zComputer>();
                            zombie.allowedpickups = !zombie.allowedpickups;
                            zombie.updateExtraInfo();
                        }
                    }
                    if (!SNet.IsMaster) NetworkAPI.InvokeEvent<Zi.ZINetInfo>("ZINetInfo", new Zi.ZINetInfo(2, 8, 0, 0, 0));
                    Print("Automatic resource pickups toggled for all bots");
                }

                if (Input.GetKeyDown(KeyCode.K))
                {
                    if (SNet.IsMaster)
                    {
                        foreach (KeyValuePair<string, PlayerAIBot> bt in Zi.BotTable)
                        {
                            zComputer zombie = bt.Value.GetComponent<zComputer>();
                            zombie.allowedshare = !zombie.allowedshare;
                            zombie.updateExtraInfo();
                        }
                    }
                    if (!SNet.IsMaster) NetworkAPI.InvokeEvent<Zi.ZINetInfo>("ZINetInfo", new Zi.ZINetInfo(1, 8, 0, 0, 0));
                    Print("Automatic resource uses toggled for all bots");
                }

                if (Input.GetKey(KeyCode.Alpha8))
                    SendBot("Dauda");

                if (Input.GetKey(KeyCode.Alpha9))
                    SendBot("Hackett");

                if (Input.GetKey(KeyCode.Alpha0))
                    SendBot("Bishop");

                if (Input.GetKey(KeyCode.F6))
                    SendBot("Woods");
            }
            void SendBot(String bot)
            {
                if (Input.GetMouseButtonDown(2))
                {
                    var monster = zSearch.GetMonsterUnderPlayerAim();
                    if (monster != null)
                    {
                        Zi.SendBotToKillEnemy(bot, monster,
                            PlayerBotActionAttack.StanceEnum.All,
                            PlayerBotActionAttack.AttackMeansEnum.All,
                            PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
                    }
                }

                if (Input.GetKeyDown(KeyCode.U) && ready)
                {
                    var item = zSearch.GetItemUnderPlayerAim();
                    if (item != null)
                        Zi.SendBotToPickupItem(bot, item);
                }

                if (Input.GetKeyDown(KeyCode.I) && ready)
                    Zi.SendBotToShareResourcePack(bot, zSearch.GetHumanUnderPlayerAim());
            }
            if (Time.time > lastupdatetime + 1f)
            {
                Zi.slowUpdate();
                
                lastupdatetime = Time.time;
            }
            
        }

        public static void AddZombifiedMenu()
        {
            uint zombtb1 = TextDataBlock.GetBlockID("zombtext1");
            uint zombtb2 = TextDataBlock.GetBlockID("zombtext2");
            uint zombtb3 = TextDataBlock.GetBlockID("zombtext3");
            uint zombtb4 = TextDataBlock.GetBlockID("zombtext4");
            uint zombtb5 = TextDataBlock.GetBlockID("zombtext5");
            uint zombtb6 = TextDataBlock.GetBlockID("zombtext6");
            uint zombtb7 = TextDataBlock.GetBlockID("zombtext7");

            //ZombifiedInitiative.log.LogInfo($"debug {zombtb1} {zombtb2} {zombtb3} {zombtb4} {zombtb5} {zombtb6}");
            CommunicationNode allmenu = new(zombtb6, CommunicationNode.ScriptType.None);
            allmenu.IsLastNode = false;
            allmenu.TextId = zombtb6;
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb2, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb3, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb4, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb5, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb7, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes[0].DialogID = 314;
            allmenu.m_ChildNodes[1].DialogID = 314;
            allmenu.m_ChildNodes[2].DialogID = 314;
            allmenu.m_ChildNodes[3].DialogID = 314;
            allmenu.m_ChildNodes[4].DialogID = 314;

            CommunicationNode zombmenu = new(zombtb1, CommunicationNode.ScriptType.None);
            zombmenu.IsLastNode = false;
            zombmenu.TextId = zombtb1;
            zombmenu.m_ChildNodes.Add(allmenu);

            Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes.Add(zombmenu);
        }















        public static void SwitchDebug()
        {
            _debug = !_debug;
            Print("Debug log " + (_debug ? "enabled" : "disabled"), true);
        }


        public static void Print(string text, bool forced = false)
        {
            if (_debug || forced)
                Zi.log.LogInfo(text);
        } // print
    } // ZombieController mono
}
