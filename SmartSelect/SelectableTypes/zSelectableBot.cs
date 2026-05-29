using BotControl;
using BotControl.SmartSelect;
using Player;
using System;
using UnityEngine;

namespace SlideDrum.SmartSelect.SelectableTypes
{
    public class zSelectableBot : zSelectableObject
    {
        public override Type Type => typeof(PlayerAIBot);
        private PlayerAIBot _Bot;
        public PlayerAIBot Bot 
        { 
            get 
            { 
                if (_Bot == null)
                {
                    _Bot = base.gameObject.GetComponent<PlayerAIBot>();
                    if (_Bot == null)
                        ZiMain.log.LogWarning("Bot Selection game object does not have the correct componenet!");
                }
                return _Bot;
            } 
        }
    }
}
