using Il2CppInterop.Runtime;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BotControl
{
    public static class zStaticRefrences
    {
        public static PUI_CommunicationMenu _CommsMenu;
        public static PUI_CommunicationMenu CommsMenu
        {
            get
            {
                if (_CommsMenu == null)
                {
                    _CommsMenu = GameObject.Find("GUI/CellUI_Camera(Clone)/PlayerLayer/MovementRoot/PUI_CommunicationMenu(Clone)").GetComponent<PUI_CommunicationMenu>();
                }
                return _CommsMenu;
            }
        }
        public static PUI_Subtitles _Subtitles;
        public static PUI_Subtitles Subtitles
        {
            get
            {
                if (_Subtitles == null)
                    _Subtitles = GameObject.Find("GUI/CellUI_Camera(Clone)/PlayerLayer/MovementRoot/PUI_Subtitles_CellUI(Clone)").GetComponent<PUI_Subtitles>();
                return _Subtitles;
            }
        }
        public static PlayerAgent _LocalPlayer;
        public static PlayerAgent LocalPlayer
        {
            get
            {
                if (_LocalPlayer == null)
                    _LocalPlayer = PlayerManager.GetLocalPlayerAgent();
                return _LocalPlayer;
            }
        }
        public static Transform _CameraTransform;
        public static Transform CameraTransform
        {
            get
            {
                if (_CameraTransform == null)
                    _CameraTransform = LocalPlayer.FPSCamera.transform;
                return _CameraTransform;
            }
        }
        private static HashSet<PlayerAgent> _AllBotAgents;
        public static HashSet<PlayerAgent> AllBotAgents
        {
            get
            {
                bool dirty = (_AllBotAgents == null || _AllBotAgents.Any(obj => obj == null));
                if (dirty)
                    _AllBotAgents = PlayerManager.PlayerAgentsInLevel.ToArray().Where(agent => agent.Owner.IsBot).ToHashSet();
                return _AllBotAgents;
            }
        }
        private static HashSet<GameObject> _AllBotObjects;
        public static HashSet<GameObject> AllBotObjects
        {
            get
            {
                bool dirty = (_AllBotObjects == null || _AllBotObjects.Any(obj => obj == null));
                if (dirty)
                    _AllBotObjects = PlayerManager.PlayerAgentsInLevel.ToArray().Where(agent => agent.Owner.IsBot).Select(agent => agent.gameObject).ToHashSet();
                return _AllBotObjects;
            }
        }
        public static Transform[] _SentryRaycastCorners;
        public static Transform[] SentryRaycastCorners
        { 
            get 
            {
                if (_SentryRaycastCorners == null)
                {
                    _SentryRaycastCorners = LoadSentryCorners();
                }
                return _SentryRaycastCorners;
            } 
        }
        private static Transform[] LoadSentryCorners()
        {
            return Resources.FindObjectsOfTypeAll(Il2CppType.Of<SentryGunFirstPerson>())
                ?.FirstOrDefault(x => x.name == "SentryGunFirstPerson")
                ?.TryCast<SentryGunFirstPerson>()
                ?.m_placementIndicatorPrefab
                ?.GetComponent<SentryGunPlacementIndicator>()
                ?.m_raycastCorners;
        }
    }
}
