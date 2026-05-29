using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BotControl
{
    public static class zStaticRefrences
    {
        public static PUI_CommunicationMenu _CommsMenu = null;
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
        public static PUI_Subtitles _Subtitles = null;
        public static PUI_Subtitles Subtitles
        {
            get
            {
                if (_Subtitles == null)
                    _Subtitles = GameObject.Find("GUI/CellUI_Camera(Clone)/PlayerLayer/MovementRoot/PUI_Subtitles_CellUI(Clone)").GetComponent<PUI_Subtitles>();
                return _Subtitles;
            }
        }
        public static PlayerAgent LocalPlayer => PlayerManager.GetLocalPlayerAgent();
        public static Transform CameraTransform = LocalPlayer.FPSCamera.transform;
        public static HashSet<PlayerAgent> AllBotAgents => PlayerManager.PlayerAgentsInLevel.ToArray().Where(agent => agent.Owner.IsBot).ToHashSet();
    }
}
