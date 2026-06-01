using Enemies;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BotControl.SmartSelect.PressTypes
{
    public class pTypeDoublePress : PressType
    {
        private HashSet<Il2CppSystem.Type> _SelectableTypes;
        private Component _CurrentComponent = null;
        private PressAction _CurrentAction = null;
        public override HashSet<Il2CppSystem.Type> SelectableTypes => _SelectableTypes;
        public override Component CurrentComponent => _CurrentComponent;
        public override PressAction CurrentAction => _CurrentAction;

        public pTypeDoublePress()
        {
            _SelectableTypes = new HashSet<Il2CppSystem.Type>();
            _SelectableTypes.Add(Il2CppType.Of<PlayerAgent>());
            _SelectableTypes.Add(Il2CppType.Of<SentryGunInstance>());
            _SelectableTypes.Add(Il2CppType.Of<LG_WeakResourceContainer>());
            _SelectableTypes.Add(Il2CppType.Of<LG_WeakDoor>());
            _SelectableTypes.Add(Il2CppType.Of<EnemyAgent>());
        }
        public override bool SetCurrentAction()
        {
            PlayerAIBot BestBot = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            Il2CppSystem.Type type = _CurrentComponent?.GetIl2CppType();
            if (type == null)
            {
                if (BestBot == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                BackpackItem item = zHelpers.GetAgentBackpackItem(BestBot.Agent, InventorySlot.GearClass);
                bool isSentry = item.Instance.ArchetypeName == "Sentry Gun"; //TODO handle mine deployer
                if (!isSentry)
                {
                    _CurrentAction = null;
                    return false;
                }
                bool isDeployed = item.Status == eInventoryItemStatus.Deployed;
                if (!isDeployed)
                {
                    _CurrentAction = PressAction.GetAction("Deploy Sentry");
                    return true;
                }
                _CurrentAction = null;
                return false;
            }
            else if (zHelpers.IsOfType<PlayerAgent>(type))
            {
                PlayerAgent Agent = _CurrentComponent.Cast<PlayerAgent>();
                _CurrentAction = PressAction.GetAction("Follow Me");
                return true;
            }
            else if (zHelpers.IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance Sentry = _CurrentComponent.Cast<SentryGunInstance>();
                _CurrentAction = PressAction.GetAction("Pickup All Sentries");
                return true;
            }
            else if (zHelpers.IsOfType<LG_WeakResourceContainer>(type))
            {
                LG_WeakResourceContainer Container = _CurrentComponent.Cast<LG_WeakResourceContainer>();
                if (BestBot == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                uint consumableId = zHelpers.GetAgentBackpackItemId(BestBot.Agent, InventorySlot.Consumable);
                uint resourceId = zHelpers.GetAgentBackpackItemId(BestBot.Agent, InventorySlot.ResourcePack);
                bool haveItem = (consumableId != 0 || resourceId != 0);
                if (!haveItem)
                {
                    _CurrentAction = null;
                    return false;
                }
                _CurrentAction = PressAction.GetAction("Place Item In Container");
                return true;
            }
            else if (zHelpers.IsOfType<LG_WeakDoor>(type))
            {
                if (BestBot == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                LG_WeakDoor Door = _CurrentComponent.Cast<LG_WeakDoor>();
                if (Door.Gate.IsTraversable)
                {
                    _CurrentAction = null;
                    return false;
                }
                if (Door.LastStatus == eDoorStatus.Destroyed)
                {
                    _CurrentAction = null;
                    return false;
                }
                _CurrentAction = PressAction.GetAction("Destroy Door");
                return true;
            }
            else if (zHelpers.IsOfType<EnemyAgent>(type))
            {
                if (BestBot == null)
                {
                    _CurrentAction = null;
                    return false;
                }
                EnemyAgent Enemy = _CurrentComponent.Cast<EnemyAgent>();
                _CurrentAction = PressAction.GetAction("Attack Countdown Enemy");
                return true;
            }
            var bot = zSmartSelect.GetBotLookingAt();
            if (bot != null)
            {
                _CurrentAction = PressAction.GetAction("Follow Me");
                return true;
            }
            _CurrentAction = null;
            return false;
        }
        public override bool SeCurrentComponent()
        {
            _CurrentComponent = zSearch.FindBestInView(zStaticRefrences.CameraTransform, SelectableTypes, MaxAngle: SelectionAngle);
            return CurrentComponent != null;
        }
    }
}
