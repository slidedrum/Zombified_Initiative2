using GameData;
using HarmonyLib;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2
{
    public static class zDebug
    {//This class is unused, but it's where i put all the stuff I need for debugging.
        private static GameObject debugSphere;
        internal static void ShowDebugSphere(Vector3 position, float radius)
        {
            if (debugSphere == null)
            {
                debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = "LookDirectionDebugSphere";

                // Remove collider so it doesn’t interfere with physics
                UnityEngine.Object.Destroy(debugSphere.GetComponent<Collider>());

                // Make it semi-transparent
                var renderer = debugSphere.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0f, 1f, 0f, 0.3f); // green, 30% opacity
                renderer.material.SetFloat("_Mode", 3); // Transparent mode
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }

            debugSphere.transform.position = position;
            debugSphere.transform.localScale = Vector3.one * (radius * 2f); // scale to match search radius
        }
        private static void printAllNames()
        {
            System.Collections.Generic.HashSet<string> ArchetypeNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> terminalItemShortNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> terminalItemLongNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> PublicNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> DataBlockPublicNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.List<Item> allItems = GameObject.FindObjectsOfType<Item>().ToList();
            foreach (Item item in allItems) 
            {
                PublicNames.Add(item?.PublicName ?? "");
                DataBlockPublicNames.Add(item?.ItemDataBlock?.publicName ?? "");
                ArchetypeNames.Add(item?.ArchetypeName ?? "");
                terminalItemShortNames.Add(item?.ItemDataBlock?.terminalItemShortName ?? "");
                terminalItemLongNames.Add(item?.ItemDataBlock?.terminalItemLongName ?? "");
            }
            ZiMain.log.LogInfo("PublicNames:");
            ZiMain.log.LogInfo(string.Join("\n", PublicNames));
            ZiMain.log.LogInfo("DataBlockPublicNames:");
            ZiMain.log.LogInfo(string.Join("\n", DataBlockPublicNames));
            ZiMain.log.LogInfo("ArchetypeNames:");
            ZiMain.log.LogInfo(string.Join("\n", ArchetypeNames));
            ZiMain.log.LogInfo("terminalItemShortNames:");
            ZiMain.log.LogInfo(string.Join("\n", terminalItemShortNames));
            ZiMain.log.LogInfo("terminalItemLongNames:");
            ZiMain.log.LogInfo(string.Join("\n", terminalItemLongNames));
        }
        private static void printAllInventoryItems()
        {
            var allItemTypes = ItemSpawnManager.m_itemDataPerInventorySlot;
            for (int i = 0; i < allItemTypes.Count; i++)
            {
                var slotName = ((InventorySlot)i).ToString();
                var items = allItemTypes[i];
                ZiMain.log.LogInfo($"{slotName} ({items.Count}):");
                foreach (var item in items)
                {
                    ZiMain.log.LogInfo($"\t - ({ItemDataBlock.s_blockIDByName[item.name]}){item.publicName}");
                }
            }
        }
        private static void printWhatBotsCanPickUp()
        {
            //ItemDataBlock.s_blockIDByName has all ids
            //RootPlayerBotAction.s_itemBasePrios has what bots can pick up
            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<uint,float> item in RootPlayerBotAction.s_itemBasePrios)
            {
                uint id = item.Key;
                float priority = item.Value;
                ItemDataBlock block = ItemDataBlock.s_blockByID[id];
                var name = block.name;
                ZiMain.log.LogMessage($"{name}:{priority}");
            }
        }
        private static string printNameFromID(uint id)
        {
            if (ItemDataBlock.s_blockByID.ContainsKey(id))
            {
                string name = ItemDataBlock.s_blockByID[id].name + " - " + ItemDataBlock.s_blockByID[id].publicName;
                ZiMain.log.LogInfo($"{id}: {name}");
                return name;
            }
            return "";
        }
        private static ItemDataBlock getDatablockFromId(uint id)
        {
            if (ItemDataBlock.s_blockByID.ContainsKey(id))
            {
                return ItemDataBlock.s_blockByID[id];
            }
            return null;
        }

    }
}
