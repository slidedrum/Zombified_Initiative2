using BotControl.Patches;
using Enemies;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BotControl.SmartSelect.PressTypes
{
    public class pTypeHoldPress : PressType
    {
        private HashSet<Il2CppSystem.Type> _SelectableTypes;
        private Component _CurrentComponent = null;
        private PressAction _CurrentAction = null;
        public override HashSet<Il2CppSystem.Type> SelectableTypes => _SelectableTypes;
        public override Component CurrentComponent => _CurrentComponent;
        public override PressAction CurrentAction => _CurrentAction;
        public pTypeHoldPress()
        {
            _SelectableTypes = new HashSet<Il2CppSystem.Type>();
            _SelectableTypes.Add(Il2CppType.Of<PlayerAgent>());
            _SelectableTypes.Add(Il2CppType.Of<ItemInLevel>());
            _SelectableTypes.Add(Il2CppType.Of<SentryGunInstance>());
            _SelectableTypes.Add(Il2CppType.Of<LG_WeakResourceContainer>());
            _SelectableTypes.Add(Il2CppType.Of<LG_WeakDoor>());
            _SelectableTypes.Add(Il2CppType.Of<EnemyAgent>());
            _SelectableTypes.Add(Il2CppType.Of<LG_PowerGenerator_Core>());
        }
        public override bool SetCurrentAction()
        {
            if (!zSmartSelect.MainSelection.Selected<PlayerAIBot>())
            {
                _CurrentAction = null;
                return false;
            }
            Il2CppSystem.Type type = _CurrentComponent?.GetIl2CppType();
            if (type == null)
            {
                bool LookingDown = Vector3.Angle(zStaticRefrences.CameraTransform.forward, Vector3.down) < 15f;
                if (LookingDown)
                    return SetCurrentActionPlayerAgent(zStaticRefrences.LocalPlayer);
                _CurrentAction = null;
                return false;
            }
            else if (zHelpers.IsOfType<PlayerAgent>(type))
            {
                PlayerAgent agent = _CurrentComponent.Cast<PlayerAgent>();
                return SetCurrentActionPlayerAgent(agent);
            }
            else if (zHelpers.IsOfType<ItemInLevel>(type))
            {
                ItemInLevel item = _CurrentComponent.Cast<ItemInLevel>();
                if (item == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                PlayerAIBot BestBot = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
                if (BestBot == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                _CurrentAction = PressAction.GetAction("Pickup Item");
                return true;
            }
            else if (zHelpers.IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance sentry = _CurrentComponent.Cast<SentryGunInstance>();
                //if (HoldPressSentryGrun(sentry))
                return SetCurrentActionSentry(sentry);
            }
            else if (zHelpers.IsOfType<LG_WeakResourceContainer>(type)) // Might need to deprioritize this if an item is in the way somehow.
            {
                LG_WeakResourceContainer container = _CurrentComponent.Cast<LG_WeakResourceContainer>();
                var BestBot = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().First();
                if (BestBot == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                if (container.ISOpen)
                {
                    _CurrentAction = null;
                    return false;
                }
                _CurrentAction = PressAction.GetAction("Open Container");
                return true;
            }
            else if (zHelpers.IsOfType<LG_WeakDoor>(type))
            {
                LG_WeakDoor Door = _CurrentComponent.Cast<LG_WeakDoor>();
                var BestBot = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().First();
                if (BestBot == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                if (Door.LastStatus == eDoorStatus.Destroyed)
                {
                    _CurrentAction = null;
                    return false;
                }
                if (zHelpers.GetAgentBackpackItemId(BestBot.Agent, InventorySlot.Consumable) == 115) // cFoamGrenade
                {
                    _CurrentAction = PressAction.GetAction("Secure Door");
                    return true;
                }
                _CurrentAction = null;
                return false;
            }
            else if (zHelpers.IsOfType<EnemyAgent>(type))
            {
                EnemyAgent enemy = _CurrentComponent.Cast<EnemyAgent>();
                var BestBot = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().First();
                if (BestBot == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                _CurrentAction = PressAction.GetAction("Attack Enemy");
                return true;
            }
            else if (zHelpers.IsOfType<LG_PowerGenerator_Core>(type))
            {
                LG_PowerGenerator_Core generator = _CurrentComponent.Cast<LG_PowerGenerator_Core>();
                var BestBot = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().First();
                if (BestBot == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                _CurrentAction = PressAction.GetAction("Insert Cell");
                return true;
            }
            _CurrentAction = null;
            return false;
        }

        private bool SetCurrentActionSentry(SentryGunInstance sentry)
        {
            HashSet<PlayerAIBot> selection = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>();
            foreach (PlayerAIBot bot in selection)
            {
                bool owned = sentry.Owner == bot.Agent;
                bool haveTool = (zHelpers.GetAgentBackpackItemId(bot.Agent, InventorySlot.ResourcePack) == (uint)ShareActionPatch.ResourceIDs.ToolPack);
                if (haveTool)
                {
                    _CurrentAction = PressAction.GetAction("Refill Sentry");
                    return true;
                }
            }
            _CurrentAction = null;
            return false;
        }

        public override bool SeCurrentComponent()
        {
            if (!zSmartSelect.MainSelection.Selected<PlayerAIBot>())
                _CurrentComponent = null;
            _CurrentComponent = zSearch.FindBestInView(zStaticRefrences.CameraTransform, _SelectableTypes, MaxAngle: SelectionAngle);
            return _CurrentComponent != null;
        }
        public bool SetCurrentActionPlayerAgent(PlayerAgent Agent)
        {
            if (Agent.Alive)
            {
                foreach (PlayerAIBot selectedBot in zSmartSelect.MainSelection.GetSelected<PlayerAIBot>())
                {
                    uint resourcePackID = zHelpers.GetAgentBackpackItemId(selectedBot.Agent, InventorySlot.ResourcePack);
                    bool needsResourceIhave = false;
                    switch (resourcePackID)
                    {
                        case (uint)ShareActionPatch.ResourceIDs.MediPack:
                            needsResourceIhave = Agent.NeedHealth();
                            break;
                        case (uint)ShareActionPatch.ResourceIDs.AmmoPack:
                            needsResourceIhave = Agent.NeedWeaponAmmo();
                            break;
                        case (uint)ShareActionPatch.ResourceIDs.ToolPack:
                            needsResourceIhave = Agent.NeedToolAmmo();
                            break;
                        case (uint)ShareActionPatch.ResourceIDs.DisinfectPack:
                            needsResourceIhave = Agent.NeedDisinfection();
                            break;
                    }
                    if (!needsResourceIhave)
                        continue;
                    _CurrentAction = PressAction.GetAction("Share Resource");
                    return true;
                }
            }
            else // Agent is dead
            {
                _CurrentAction = PressAction.GetAction("Revive");
                return true;
            }
            _CurrentAction = null;
            return false;
        }
    }
}
