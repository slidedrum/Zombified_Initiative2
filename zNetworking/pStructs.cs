using Agents;
using Enemies;
using Player;
using SNetwork;
using UnityEngine;

namespace ZombieTweak2.zNetworking

{
    public class pStructs
    {
        //This handles encoding and decoding objects for network transfer.
        public static PlayerAgent Get_RefFrom_pStruct(pPlayerAgent pStruct)
        {
            if (!pStruct.TryGet(out PlayerAgent refrence))
                return null;
            return refrence;
        }
        public static Agent Get_RefFrom_pStruct(pAgent pStruct)
        {
            if (!pStruct.TryGet(out Agent refrence))
                return null;
            return refrence;
        }
        public static EnemyAgent Get_RefFrom_pStruct(pEnemyAgent pStruct)
        {
            if (!pStruct.TryGet(out EnemyAgent refrence))
                return null;
            return refrence;
        }
        public static GameObject Get_RefFrom_pStruct(pItemData pStruct)
        {
            GameObject item = null;
            if (pStruct.replicatorRef.TryGetID(out IReplicator rep))
                item = rep.ReplicatorSupplier.gameObject;
            return item;
        }
        public static PlayerAgent Get_RefFrom_pStruct(SNetStructs.pPlayer pStruct)
        {
            if (!pStruct.TryGetPlayer(out SNet_Player refrence))
                return null;
            return refrence.PlayerAgent.TryCast<PlayerAgent>();
        }
        public static SNetStructs.pPlayer Get_pPlayerFromRefrence(PlayerAgent refrence)
        {
            SNetStructs.pPlayer pStruct = new();
            pStruct.SetPlayer(refrence.Owner);
            return pStruct;
        }
        public static pAgent Get_pStructFromRefrence(Agent refrence)
        {
            pAgent pStruct = new();
            pStruct.Set(refrence);
            return pStruct;
        }
        public static pPlayerAgent Get_pStructFromRefrence(PlayerAgent refrence)
        {
            pPlayerAgent pStruct = new();
            pStruct.Set(refrence);
            return pStruct;
        }
        public static pEnemyAgent Get_pStructFromRefrence(EnemyAgent refrence)
        {
            pEnemyAgent pStruct = new();
            pStruct.Set(refrence);
            return pStruct;
        }
        public static pItemData Get_pStructFromRefrence(Item refrence)
        {
            pItemData pStruct = refrence.Get_pItemData();
            return pStruct;
        }
        //TODO consolidate these?
        public struct pBotSelections
        {
            public long data;
        }
        public struct pItemPrioDisable
        {
            public uint id;
            public bool allowed;
        }
        public struct pItemPrio
        {
            public uint id;
            public float prio;
        }
        public struct pPickupPermission
        {
            public int playerID;
            public bool allowed;
        }
        public struct pSharePermission
        {
            public int playerID;
            public bool allowed;
        }
        public struct pResourceThresholdDisable
        {
            public uint id;
            public bool allowed;
        }
        public struct pResourceThreshold
        {
            public uint id;
            public int threshold;
        }
    }
}
