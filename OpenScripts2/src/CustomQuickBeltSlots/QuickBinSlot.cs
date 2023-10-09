using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class QuickBinSlot : FVRQuickBeltSlot
    {
        [Header("QuickBinSlot settings")]
        public Color HoverColor;

        public AudioEvent DeleteSounds;
        public AudioEvent DeleteFailureSounds;

        [ContextMenu("CopyQBSlot")]
        public void CopyQBSlot()
        {
            FVRQuickBeltSlot QBS = GetComponent<FVRQuickBeltSlot>();

            this.CopyComponent(QBS);
        }

#if !DEBUG
        static QuickBinSlot()
        {
            On.FistVR.FVRQuickBeltSlot.Update += FVRQuickBeltSlot_Update;
        }

        private static void FVRQuickBeltSlot_Update(On.FistVR.FVRQuickBeltSlot.orig_Update orig, FVRQuickBeltSlot self)
        {
            if (self is QuickBinSlot quickBinSlot)
            {
                if (!GM.CurrentSceneSettings.IsSpawnLockingEnabled && quickBinSlot.HeldObject != null && (quickBinSlot.HeldObject as FVRPhysicalObject).m_isSpawnLock)
                {
                    (quickBinSlot.HeldObject as FVRPhysicalObject).m_isSpawnLock = false;
                }
                if (quickBinSlot.HeldObject != null)
                {
                    if ((quickBinSlot.HeldObject as FVRPhysicalObject).m_isSpawnLock)
                    {
                        if (!quickBinSlot.HoverGeo.activeSelf)
                        {
                            quickBinSlot.HoverGeo.SetActive(true);
                        }
                        quickBinSlot.m_hoverGeoRend.material.SetColor("_RimColor", new Color(0.3f, 0.3f, 1f, 1f));
                    }
                    else if ((quickBinSlot.HeldObject as FVRPhysicalObject).m_isHardnessed)
                    {
                        if (!quickBinSlot.HoverGeo.activeSelf)
                        {
                            quickBinSlot.HoverGeo.SetActive(true);
                        }
                        quickBinSlot.m_hoverGeoRend.material.SetColor("_RimColor", new Color(0.3f, 1f, 0.3f, 1f));
                    }
                    else
                    {
                        if (quickBinSlot.HoverGeo.activeSelf != quickBinSlot.IsHovered)
                        {
                            quickBinSlot.HoverGeo.SetActive(quickBinSlot.IsHovered);
                        }
                        quickBinSlot.m_hoverGeoRend.material.SetColor("_RimColor", quickBinSlot.HoverColor);
                    }
                }
                else
                {
                    if (quickBinSlot.HoverGeo.activeSelf != quickBinSlot.IsHovered)
                    {
                        quickBinSlot.HoverGeo.SetActive(quickBinSlot.IsHovered);
                    }
                    quickBinSlot.m_hoverGeoRend.material.SetColor("_RimColor", quickBinSlot.HoverColor);
                }

                if (quickBinSlot.CurObject != null && quickBinSlot.CurObject is FVRFireArmMagazine)
                {
                    Destroy(quickBinSlot.CurObject.gameObject);
                    quickBinSlot.CurObject = null;
                    quickBinSlot.HeldObject = null;
                    quickBinSlot.IsHovered = false;
                    SM.PlayGenericSound(quickBinSlot.DeleteSounds, quickBinSlot.transform.position);
                }

                if (quickBinSlot.CurObject != null && quickBinSlot.CurObject is not FVRFireArmMagazine)
                {
                    quickBinSlot.CurObject.SetQuickBeltSlot(null);
                    quickBinSlot.CurObject = null;
                    quickBinSlot.HeldObject = null;
                    quickBinSlot.IsHovered = false;
                    SM.PlayGenericSound(quickBinSlot.DeleteFailureSounds, quickBinSlot.transform.position);
                }
            }
            else orig(self);
        }
#endif
    }
}
