using Player;
using UnityEngine;

namespace BotControl.SmartSelect
{
    public class TurretHandler
    {
        public static bool TryPlaceTurret()
        { // This logic should not be done on client, send to host over network.
            var selection = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>();
            foreach (var bot in selection)
            {
                var backpack = bot.Backpack;
                if (!backpack.TryGetBackpackItem(InventorySlot.GearClass, out BackpackItem backpackItem))
                    continue;
                bool isSentry = backpackItem.Instance.ArchetypeName == "Sentry Gun";
                bool isDeployed = backpackItem.Status == eInventoryItemStatus.Deployed;
                if (!isSentry || isDeployed)
                    continue;

                // TODO make it so that if the turret is deployed, pick it up.  then replace it in the new locaiton.

                //raycast from camera to find hit position and normal,
                //place sentry at hit position, oriented based on normal.
                Vector3 origin = zStaticRefrences.CameraTransform.position;
                Vector3 direction = zStaticRefrences.CameraTransform.forward;
                if (!Physics.Raycast(origin, direction, out RaycastHit hit, 100f, LayerManager.MASK_SENTRYGUN_CAMERARAY_MOVERHELPER))
                    continue;
                Vector3 placePosition = hit.point;
                Quaternion placeRotation = Quaternion.LookRotation(FlatForward(zStaticRefrences.CameraTransform));
                Pose sentryPose = new Pose(placePosition, placeRotation);
                if (!CanPlaceTurret(ref sentryPose))
                {
                    continue;
                }
                    
                zBotActions.SendBotToPlaceSentry(bot, sentryPose, zStaticRefrences.LocalPlayer);
                PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PUTASENTRYGUNHERE);
                zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Put a sentry here.", 1);
                ZiMain.BotBarkBack(bot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO, "Will Do.", 2f);
                return true;
            }
            
            return false;
        }
        public static bool CanPlaceTurret(ref Pose pose)
        {
            bool hasRayHit = false;
            pose.position = pose.position + Vector3.up * 0.3f;
            if (Physics.Raycast(pose.position, Vector3.down, out RaycastHit hit, 3f, LayerManager.MASK_SENTRYGUN_CAMERARAY_MOVERHELPER))
            {
                float angle = Vector3.Dot(hit.normal, Vector3.up);
                if (angle > 0.9f)
                {
                    hasRayHit = true;
                    pose.position = hit.point;
                }
                else if (angle > 0.7f && Physics.Raycast(pose.position, Vector3.down, out RaycastHit hit2, 3f, LayerManager.MASK_SENTRYGUN_CAMERARAY))
                {
                    hasRayHit = true;
                    pose.position = hit2.point;
                }
                else
                {
                    return false;
                }
            }
            Bounds localBounds = new Bounds();

            for (int i = 0; i < zStaticRefrences.SentryRaycastCorners.Length; i++)
            {
                Vector3 local = zStaticRefrences.SentryRaycastCorners[i].localPosition;
                localBounds.Encapsulate(local);
            }
            Vector3 halfExtents = localBounds.size * 0.5f;
            pose.position = pose.position + Vector3.up * 0.1f;
            Collider[] hits = Physics.OverlapBox(
                pose.position,
                halfExtents,
                pose.rotation,
                LayerManager.MASK_SENTRYGUN_CAMERARAY_MOVERHELPER
            );

            return hits.Length == 0;
        }
        private static Vector3 FlatForward(Transform transform)
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return Vector3.forward;
            return dir.normalized;
        }
    }
}
