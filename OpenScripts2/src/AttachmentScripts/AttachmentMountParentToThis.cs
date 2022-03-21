using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class AttachmentMountParentToThis : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachmentMount Mount;

#if!MEATKIT
        public void Awake()
        {
            Mount.ParentToThis = true;
            On.FistVR.FVRFireArmAttachmentMount.GetRootMount += FVRFireArmAttachmentMount_GetRootMount;
        }

        public void OnDestoy()
        {
            On.FistVR.FVRFireArmAttachmentMount.GetRootMount -= FVRFireArmAttachmentMount_GetRootMount;
        }
        private FVRFireArmAttachmentMount FVRFireArmAttachmentMount_GetRootMount(On.FistVR.FVRFireArmAttachmentMount.orig_GetRootMount orig, FVRFireArmAttachmentMount self)
        {
            if (self == Mount)
            {
                return self;
            }
            else return orig(self);
        }
#endif
    }
}
