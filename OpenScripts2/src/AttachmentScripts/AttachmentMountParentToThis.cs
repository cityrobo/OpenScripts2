using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    [RequireComponent(typeof(FVRFireArmAttachmentMount))]
    public class AttachmentMountParentToThis : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachmentMount Mount => GetComponent<FVRFireArmAttachmentMount>();

        private static List<FVRFireArmAttachmentMount> _exisingAttachmentMountParentToThis = new();

#if!DEBUG
        static AttachmentMountParentToThis()
        {
            //On.FistVR.FVRFireArmAttachmentMount.GetRootMount += FVRFireArmAttachmentMount_GetRootMount;

            On.FistVR.FVRFireArmAttachment.AttachToMount += FVRFireArmAttachment_AttachToMount;
        }

        private static void FVRFireArmAttachment_AttachToMount(On.FistVR.FVRFireArmAttachment.orig_AttachToMount orig, FVRFireArmAttachment self, FVRFireArmAttachmentMount m, bool playSound)
        {
            orig(self,m,playSound);

            if (_exisingAttachmentMountParentToThis.Contains(m))
            {
                self.SetParentage(m.transform);
            }
            else if (_exisingAttachmentMountParentToThis.Count > 0 && m.MyObject is FVRFireArmAttachment parentAttachment)
            {
                do
                {
                    if (_exisingAttachmentMountParentToThis.Contains(parentAttachment.curMount))
                    {
                        self.SetParentage(parentAttachment.curMount.transform);
                        break;
                    }
                    parentAttachment = parentAttachment.curMount.MyObject as FVRFireArmAttachment;
                } while (parentAttachment != null);
            }
        }

        public void Awake()
        {
            _exisingAttachmentMountParentToThis.Add(Mount);

            Mount.ParentToThis = true;
        }

        public void OnDestoy()
        {
            _exisingAttachmentMountParentToThis.Remove(Mount);
        }
        private static FVRFireArmAttachmentMount FVRFireArmAttachmentMount_GetRootMount(On.FistVR.FVRFireArmAttachmentMount.orig_GetRootMount orig, FVRFireArmAttachmentMount self)
        {
            if (_exisingAttachmentMountParentToThis.Contains(self))
            {
                return self;
            }
            else return orig(self);
        }
#endif
    }
}
