using Agents;
using Enemies;
using LevelGeneration;
using Player;
using SNetwork;
using UnityEngine;
using static Il2CppSystem.Globalization.CultureInfo;

namespace ZombieTweak2
{
    public class pStructs
    {
        public static PlayerAgent Get_RefFrom_pStruct(Agents.pPlayerAgent pStruct)
        {
            if (!pStruct.TryGet(out PlayerAgent refrence))
                return null;
            return refrence;
        }
        public static Agent Get_RefFrom_pStruct(Agents.pAgent pStruct)
        {
            if (!pStruct.TryGet(out Agent refrence))
                return null;
            return refrence;
        }
        public static EnemyAgent Get_RefFrom_pStruct(Agents.pEnemyAgent pStruct)
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
        public static Agents.pAgent Get_pStructFromRefrence(Agent refrence)
        {
            Agents.pAgent pStruct = new();
            pStruct.Set(refrence);
            return pStruct;
        }
        public static Agents.pPlayerAgent Get_pStructFromRefrence(PlayerAgent refrence)
        {
            Agents.pPlayerAgent pStruct = new();
            pStruct.Set(refrence);
            return pStruct;
        }
        public static Agents.pEnemyAgent Get_pStructFromRefrence(EnemyAgent refrence)
        {
            Agents.pEnemyAgent pStruct = new();
            pStruct.Set(refrence);
            return pStruct;
        }
        public static pItemData Get_pStructFromRefrence(Item refrence)
        {
            pItemData pStruct = refrence.Get_pItemData();
            return pStruct;
        }
    }
}
