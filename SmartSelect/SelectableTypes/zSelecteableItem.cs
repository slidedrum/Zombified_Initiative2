using LevelGeneration;
using System;

namespace BotControl.SmartSelect.SelectableTypes
{
    public class zSelecteableItem : zSelectableObject
    {
        public override Il2CppSystem.Type Type => new ItemInLevel().GetIl2CppType();
        private ItemInLevel _Item;
        public ItemInLevel Item
        {
            get
            {
                if (_Item == null)
                {
                    _Item = base.gameObject.GetComponent<ItemInLevel>();
                    if (_Item == null)
                        ZiMain.log.LogWarning("Item Selection game object does not have the correct componenet!");
                }
                return _Item;
            }
        }
    }
}
