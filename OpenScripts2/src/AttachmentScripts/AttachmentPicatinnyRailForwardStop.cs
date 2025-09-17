using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using HarmonyLib;


namespace OpenScripts2
{
    public class AttachmentPicatinnyRailForwardStop : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachment Attachment;

        private bool _externallyActivated = false;

        public void Awake()
        {
            if (!_externallyActivated && Attachment != null)
            {
                AttachmentMountPicatinnyRail.ExistingForwardStops.Add(Attachment, this);
            }
        }

        public void ExternalActivation()
        {
            if (Attachment != null)
            {
                AttachmentMountPicatinnyRail.ExistingForwardStops.Add(Attachment, this);

                _externallyActivated = true;
            }
            else
            {
                LogError("Attachment is null during ExternalActivation!");
            }
        }

        public void ExternalDeactivation()
        {
            AttachmentMountPicatinnyRail.ExistingForwardStops.Remove(Attachment);
        }

        public void OnDestroy()
        {
            if (!_externallyActivated) AttachmentMountPicatinnyRail.ExistingForwardStops.Remove(Attachment);
        }
    }
}
