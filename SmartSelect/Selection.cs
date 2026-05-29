using UnityEngine;

namespace BotControl.SmartSelect
{
    public class Selection
    {
        //This class handles a selection instance
        private GameObject item = null;
        private GameObject bot = null;
        private GameObject enemy = null;
        public void setItem(GameObject newItem) { item = newItem; }
        public GameObject getItem()
        {
            if (item != null && !item.activeInHierarchy)
            {
                item = null;
            }
            return item;
        }
        public void setBot(GameObject newBot) { bot = newBot; }
        public GameObject getBotGobject()
        {
            if (bot != null && !bot.activeInHierarchy)
            {
                bot = null;
            }
            return bot;
        }
    }
}
