using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class FirearmFeatureChangerAttachment : FVRFireArmAttachment
    {
        public enum EFirearmFeature
        {
            BoltRelease,
            BoltCatch,
            MagazineRelease,
            FireSelector
        }

        public EFirearmFeature FirearmFeature;
        public bool ActivateFeature = true;

        private FVRFireArm _firearm = null;

        public override void FVRUpdate()
        {
            base.FVRUpdate();
            if (curMount == null && _firearm != null)
            {
                ChangeFeature(!ActivateFeature);
                _firearm = null;
            }
        }

        public override void AttachToMount(FVRFireArmAttachmentMount m, bool playSound)
        {
            base.AttachToMount(m, playSound);

            _firearm = m.GetRootMount().MyObject as FVRFireArm;
            if (_firearm != null)
            {
                ChangeFeature(ActivateFeature);
            }
        }

        private void ChangeFeature(bool active)
        {
            switch (_firearm)
            {
                case ClosedBoltWeapon w:
                    ChangeFeatureClosedBolt(w,active); 
                    break;
                case OpenBoltReceiver w:
                    ChangeFeatureOpenBolt(w, active);
                    break;
                case Handgun w:
                    ChangeFeatureHandgun(w, active);
                    break;
                case BoltActionRifle w:
                    ChangeFeatureBoltAction(w, active);
                    break;
                case TubeFedShotgun w:
                    ChangeFeatureTubeFed(w, active);
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, "Firearm Type not supported!");
                    break;
            }
        }

        private void ChangeFeatureClosedBolt(ClosedBoltWeapon w, bool active)
        {
            switch (FirearmFeature)
            {
                case EFirearmFeature.BoltRelease:
                    w.HasBoltReleaseButton = active;
                    break;
                case EFirearmFeature.BoltCatch:
                    w.HasBoltCatchButton = active;
                    break;
                case EFirearmFeature.MagazineRelease:
                    w.HasMagReleaseButton = active;
                    break;
                case EFirearmFeature.FireSelector:
                    w.HasFireSelectorButton = active;
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, $"{FirearmFeature} not supported for  {w.GetType()}!");
                    break;
            }
        }

        private void ChangeFeatureOpenBolt(OpenBoltReceiver w, bool active)
        {
            switch (FirearmFeature)
            {
                case EFirearmFeature.MagazineRelease:
                    w.HasMagReleaseButton = active;
                    break;
                case EFirearmFeature.FireSelector:
                    w.HasFireSelectorButton = active;
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, $"{FirearmFeature} not supported for  {w.GetType()}!");
                    break;
            }
        }

        private void ChangeFeatureHandgun(Handgun w, bool active)
        {
            switch (FirearmFeature)
            {
                case EFirearmFeature.BoltRelease:
                    w.HasSlideReleaseControl = active;
                    break;
                case EFirearmFeature.MagazineRelease:
                    w.HasMagReleaseButton = active;
                    break;
                case EFirearmFeature.FireSelector:
                    w.HasSafetyControl = active;
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, $"{FirearmFeature} not supported for  {w.GetType()}!");
                    break;
            }
        }

        private void ChangeFeatureBoltAction(BoltActionRifle w, bool active)
        {
            switch (FirearmFeature)
            {
                case EFirearmFeature.MagazineRelease:
                    w.HasMagEjectionButton = active;
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, $"{FirearmFeature} not supported for  {w.GetType()}!");
                    break;
            }
        }

        private void ChangeFeatureTubeFed(TubeFedShotgun w, bool active)
        {
            {
                switch (FirearmFeature)
                {
                    case EFirearmFeature.BoltRelease:
                        w.HasSlideReleaseButton = active;
                        break;
                    default:
                        OpenScripts2_BepInExPlugin.LogWarning(this, $"{FirearmFeature} not supported for  {w.GetType()}!");
                        break;
                }
            }
        }

        [ContextMenu("Copy existing Attachment's values")]
        public void CopyAttachment()
        {
            FVRFireArmAttachment[] attachments = GetComponents<FVRFireArmAttachment>();

            FVRFireArmAttachment toCopy = attachments.Single(c => c != this);

            toCopy.AttachmentInterface.Attachment = this;
            toCopy.Sensor.Attachment = this;

            this.CopyComponent(toCopy);
        }
    }
}