using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class SupressableMuzzleNoiseMaker : MuzzleNoiseMaker
    {
        public override void OnShot(FVRFireArm f, FVRTailSoundClass tailClass)
        {
            if (!f.IsSuppressed()) base.OnShot(f, tailClass);
        }

        public override void OnShot(AttachableFirearm f, FVRTailSoundClass tailClass)
        {
            if (!f.IsSuppressed()) base.OnShot(f, tailClass);
        }

        [ContextMenu("Copy Noise Maker")] 
        public void CopyNoiseMaker()
        {
            MuzzleNoiseMaker[] weapon = GetComponents<MuzzleNoiseMaker>();
            MuzzleNoiseMaker toCopy = weapon.Single(c => c != this);

            foreach (var mount in toCopy.AttachmentMounts)
            {
                mount.MyObject = this;
                mount.Parent = this;
            }

            toCopy.AttachmentInterface.Attachment = this;
            toCopy.Sensor.Attachment = this;

            this.CopyComponent(toCopy);
        }
#if !DEBUG
#endif
    }
}