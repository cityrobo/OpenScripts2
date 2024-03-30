using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using HarmonyLib;
using System.Linq;

namespace OpenScripts2
{
    public class InvertingMount : FVRFireArmAttachmentMount
    {
        private static bool _wasPatched = false;

#if !DEBUG
        public void Start()
        {
            if (!_wasPatched)
            {
                Harmony.CreateAndPatchAll(typeof(InvertingMount));
                _wasPatched = true;
            }
        }
#endif

        [HarmonyPatch(typeof(FVRFireArmAttachment), "AttachToMount")]
        [HarmonyPostfix]
        public static void FVRFireArmAttachment_AttachToMount_Postfix(FVRFireArmAttachment __instance, FVRFireArmAttachmentMount m)
        {
            if (m is InvertingMount)
            {
                Vector3 pos;
                pos = __instance.transform.localPosition;
                pos.x = -pos.x;
                __instance.transform.localPosition = pos;

                Vector3 scale;
                foreach (MeshRenderer renderer in __instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    scale = renderer.transform.localScale;
                    scale.x = -scale.x;
                    renderer.transform.localScale = scale;

                    pos = renderer.transform.localPosition;
                    pos.x = -pos.x;
                    renderer.transform.localPosition = pos;
                }

                foreach (Collider collider in __instance.m_colliders.Where(c => !c.isTrigger))
                {
                    scale = collider.transform.localScale;
                    scale.x = -scale.x;
                    collider.transform.localScale = scale;

                    pos = collider.transform.localPosition;
                    pos.x = -pos.x;
                    collider.transform.localPosition = pos;
                }

                foreach (FVRFireArmAttachmentMount mount in __instance.AttachmentMounts)
                {
                    if (mount == null) continue;

                    pos = mount.transform.localPosition;
                    pos.x = -pos.x;
                    mount.transform.localPosition = pos;

                    Vector3 eulerAngles = mount.transform.localEulerAngles;
                    eulerAngles.z = -eulerAngles.z;
                    mount.transform.localEulerAngles = eulerAngles;

                    if (mount.Point_Front.parent != mount.transform)
                    {
                        eulerAngles = mount.Point_Front.localEulerAngles;
                        eulerAngles.z = -eulerAngles.z;
                        mount.Point_Front.localEulerAngles = eulerAngles;
                    }

                    pos = mount.Point_Front.localPosition;
                    pos.x = -pos.x;
                    mount.Point_Front.localPosition = pos;

                    if (mount.Point_Rear.parent != mount.transform)
                    {
                        eulerAngles = mount.Point_Rear.localEulerAngles;
                        eulerAngles.z = -eulerAngles.z;
                        mount.Point_Rear.localEulerAngles = eulerAngles;
                    }

                    pos = mount.Point_Rear.localPosition;
                    pos.x = -pos.x;
                    mount.Point_Rear.localPosition = pos;
                }
            }
        }

        [HarmonyPatch(typeof(FVRFireArmAttachment), "DetachFromMount")]
        [HarmonyPrefix]
        public static void FVRFireArmAttachment_DetachFromMount_Prefix(FVRFireArmAttachment __instance)
        {
            if (__instance.curMount is InvertingMount)
            {
                Vector3 pos;
                pos = __instance.transform.localPosition;
                pos.x = -pos.x;
                __instance.transform.localPosition = pos;

                Vector3 scale;
                foreach (MeshRenderer renderer in __instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    scale = renderer.transform.localScale;
                    scale.x = -scale.x;
                    renderer.transform.localScale = scale;

                    pos = renderer.transform.localPosition;
                    pos.x = -pos.x;
                    renderer.transform.localPosition = pos;
                }

                foreach (Collider collider in __instance.m_colliders.Where(c => !c.isTrigger))
                {
                    scale = collider.transform.localScale;
                    scale.x = -scale.x;
                    collider.transform.localScale = scale;

                    pos = collider.transform.localPosition;
                    pos.x = -pos.x;
                    collider.transform.localPosition = pos;
                }

                foreach (FVRFireArmAttachmentMount mount in __instance.AttachmentMounts)
                {
                    if (mount == null) continue;

                    Vector3 eulerAngles = mount.transform.localEulerAngles;
                    eulerAngles.z = -eulerAngles.z;
                    mount.transform.localEulerAngles = eulerAngles;

                    pos = mount.transform.localPosition;
                    pos.x = -pos.x;
                    mount.transform.localPosition = pos;

                    if (mount.Point_Front.parent != mount.transform)
                    {
                        eulerAngles = mount.Point_Front.localEulerAngles;
                        eulerAngles.z = -eulerAngles.z;
                        mount.Point_Front.localEulerAngles = eulerAngles;
                    }

                    pos = mount.Point_Front.localPosition;
                    pos.x = -pos.x;
                    mount.Point_Front.localPosition = pos;

                    if (mount.Point_Rear.parent != mount.transform)
                    {
                        eulerAngles = mount.Point_Rear.localEulerAngles;
                        eulerAngles.z = -eulerAngles.z;
                        mount.Point_Rear.localEulerAngles = eulerAngles;
                    }

                    pos = mount.Point_Rear.localPosition;
                    pos.x = -pos.x;
                    mount.Point_Rear.localPosition = pos;
                }


            }
        }
    }
}
