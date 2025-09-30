
//using Player;

//namespace ZombieTweak2.zRootBotPlayerAction
//{
//    public static class zBase
//    {
//        public static class zBaseDesc
//        {
//            public static bool IsActionAllowed(CustomActionBase actual, PlayerBotActionBase.Descriptor desc)
//            {
//                if (IsMyChild(actual, desc))
//                {
//                    return true;
//                }
//                if ((this.GetAccessLayersRuntime() & desc.RequiredLayers) != PlayerBotActionBase.AccessLayers.None)
//                {
//                    float num = ((this.ActionBase != null) ? PlayerBotActionBase.Descriptor.s_minAbortPrioDiff : 0f);
//                    if (desc.Prio - this.Prio < num)
//                    {
//                        return false;
//                    }
//                }
//                return true;
//            }
//            public bool IsMyChild(CustomActionBase actual, PlayerBotActionBase.Descriptor desc)
//            {
//                if (this == desc)
//                {
//                    return true;
//                }
//                if (this.ActionBase != null)
//                {
//                    for (PlayerBotActionBase playerBotActionBase = desc.ParentActionBase; playerBotActionBase != null; playerBotActionBase = playerBotActionBase.m_descBase.ParentActionBase)
//                    {
//                        if (playerBotActionBase == this.ActionBase)
//                        {
//                            return true;
//                        }
//                    }
//                }
//                return false;
//            }
//        }
//        public static bool Update(CustomActionBase actual)
//        {
//            return !IsActive(actual);
//        }
//        public static bool IsActive(CustomActionBase actual)
//        {
//            return actual.m_descBase.Status == PlayerBotActionBase.Descriptor.StatusType.Active;
//        }
//        public static void Stop(CustomActionBase actual)
//        {
//        }
//        public static bool IsActionAllowed(CustomActionBase actual, PlayerBotActionBase.Descriptor desc)
//        {
//            return zBaseDesc.IsActionAllowed(actual, desc);
//        }
//    }
//}
