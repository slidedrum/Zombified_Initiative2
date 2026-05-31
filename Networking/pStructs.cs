using Agents;
using Enemies;
using Player;
using SNetwork;
using UnityEngine;

namespace BotControl.Networking

{
    public class pStructs
    {
        //This handles encoding and decoding objects for network transfer.

        //TODO this conversion shit needs a better naming scheme, I have no idea wtf I did here.

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

        public static PlayerAgent Get_RefFrom_pStruct(SNetStructs.pPlayer pStruct)
        {
            if (!pStruct.TryGetPlayer(out SNet_Player refrence))
                return null;
            return refrence.PlayerAgent.TryCast<PlayerAgent>();
        }
        public static GameObject Get_RefFrom_pStruct(pItemData pStruct)
        {
            GameObject item = null;
            if (pStruct.replicatorRef.TryGetID(out IReplicator rep))
                item = rep.ReplicatorSupplier.gameObject;
            return item;
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
        public static Item TryGetItemInLevelFromItemData(pItemData itemData)
        {
            //Do we need this? This should probably move to pStructss
            Item item;
            PlayerBackpackManager.TryGetItemInLevelFromItemData(itemData, out item);
            return item;
        }
        public static PlayerAgent GetAgentFrom_pStruct(SNetStructs.pPlayer player_struct)
        {
            //Do we need this? This should probably move to pStructs
            if (!player_struct.TryGetPlayer(out SNet_Player player))
                return null;
            return player.PlayerAgent.TryCast<PlayerAgent>();
        }
        public static SNetStructs.pPlayer Get_pStructFromAgent(PlayerAgent agent)
        {
            //Do we need this?
            SNetStructs.pPlayer player = new();
            player.SetPlayer(agent.Owner);
            return player;
        }
        public static PlayerAgent GetAgentFrom_pPlayer(SNetStructs.pPlayer player_struct)
        {
            //Do we need this? This should probably move to pStructs
            if (!player_struct.TryGetPlayer(out SNet_Player player))
                return null;
            return player.PlayerAgent.TryCast<PlayerAgent>();
        }
        public static SNetStructs.pPlayer Get_pPlayerFromAgent(PlayerAgent agent)
        {
            //Do we need this? This should probably move to pStructs
            SNetStructs.pPlayer player = new();
            player.SetPlayer(agent.Owner);
            return player;
        }

        //TODO consolidate these?
        public struct pBotSelections
        {
            public long data;
        }
        public struct pGenericPermission
        {
            public int playerID;
            public int actionID;
            public bool allowed;
        }
        public struct pAttackEnemyInfo
        {
            public pEnemyAgent enemy;
            public pPlayerAgent aiBot;
            public pPlayerAgent commander;
        }
        internal struct pPlaceSentryInfo
        {
            public pPlayerAgent playerAgent;
            public pPlayerAgent commander;
            public Pose Pose;
        }
        internal struct pPickupSentryInfo
        {
            public pPlayerAgent playerAgent;
            public pPlayerAgent commander;
        }
        internal struct pPickupItemInfo
        {
            public pItemData item;
            public pPlayerAgent playerAgent;
            public pPlayerAgent commander;
        }
        internal struct pShareResourceInfo
        {
            public pPlayerAgent sender;
            public pPlayerAgent receiver;
            public pPlayerAgent commander;
        }
        internal struct pFollowActionInfo
        {
            public float prio;
            public float radius;
            public float maxRadius;
        }
        internal struct pBoolOverideTreeInfo
        {
            public uint treeID;
            public uint keyId;
            public bool value;
            public bool isNull;
        }
        internal struct pIntOverideTreeInfo
        {
            public uint treeID;
            public uint keyId;
            public int value;
            public bool isNull;
        }
        internal struct pFloatOverideTreeInfo
        {
            public uint treeID;
            public uint keyId;
            public float value;
            public bool isNull;
        }
        public enum pThrowType : uint
        {
            FogRepeller,
            Glowstick,
            cFoam,
        }
        public struct pThrowDataInfo
        {
            public pPlayerAgent Commander;
            public pPlayerAgent Agent;
            public pThrowType ThrowType;
            public Vector3 MovePosition;
            public Vector3 TargetPosition;
        }
    }
}
