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

        private static Dictionary<FVRFireArmAttachmentMount, AttachmentMountParentToThis> _exisingAttachmentMountParentToThis = new();

#if!DEBUG
        static AttachmentMountParentToThis()
        {
            On.FistVR.FVRFireArmAttachmentMount.GetRootMount += FVRFireArmAttachmentMount_GetRootMount;
        }

        public void Awake()
        {
            _exisingAttachmentMountParentToThis.Add(Mount,this);

            Mount.ParentToThis = true;
        }

        public void OnDestoy()
        {
            _exisingAttachmentMountParentToThis.Remove(Mount);
        }
        private static FVRFireArmAttachmentMount FVRFireArmAttachmentMount_GetRootMount(On.FistVR.FVRFireArmAttachmentMount.orig_GetRootMount orig, FVRFireArmAttachmentMount self)
        {
            if (_exisingAttachmentMountParentToThis.ContainsKey(self))
            {
                return self;
            }
            else return orig(self);
        }
#endif
    }
}
