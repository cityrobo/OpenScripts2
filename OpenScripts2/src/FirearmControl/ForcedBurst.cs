using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class ForcedBurst : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;
        public float CooldownPeriod = 0.25f;

        private bool _isHooked = false;

        private bool _isBurstFiring = false;
        private int _burstAmount = 0;
        private bool _isCoolingDown = false;
        private bool _shouldCoolDown = false;

        private static readonly Dictionary<FVRFireArm, ForcedBurst> _existingForceBurst = new();

#if !DEBUG

        static ForcedBurst()
        {
            On.FistVR.ClosedBoltWeapon.DropHammer += ClosedBoltWeapon_DropHammer;
            On.FistVR.ClosedBoltWeapon.FVRUpdate += ClosedBoltWeapon_FVRUpdate;

            On.FistVR.Handgun.ReleaseSeer += Handgun_ReleaseSeer;
            On.FistVR.Handgun.FVRUpdate += Handgun_FVRUpdate;
        }

        public void Awake()
        {
            if (!_isHooked)
                switch (FireArm)
                {
                    case ClosedBoltWeapon:
                        _isHooked = true;
                        _existingForceBurst.Add(FireArm, this);
                        break;
                    case Handgun:
                        _isHooked = true;
                        _existingForceBurst.Add(FireArm, this);
                        break;
                    default:
                        LogWarning($"Firearm type not supported ({FireArm.GetType()})!");
                        break;
                }
        }

        public void OnDestroy()
        {
            if (_isHooked)
                switch (FireArm)
                {
                    case ClosedBoltWeapon:
                        _isHooked = false;
                        _existingForceBurst.Remove(FireArm);
                        break;
                    case Handgun:
                        _isHooked = false;
                        _existingForceBurst.Remove(FireArm);
                        break;
                    default:
                        break;
                }
        }

        private static void ClosedBoltWeapon_FVRUpdate(On.FistVR.ClosedBoltWeapon.orig_FVRUpdate orig, ClosedBoltWeapon self)
        {
            orig(self);

            if (_existingForceBurst.TryGetValue(self, out ForcedBurst forcedBurst))
            {
                ClosedBoltWeapon.FireSelectorModeType modeType = self.FireSelector_Modes[self.m_fireSelectorMode].ModeType;
                if (forcedBurst._isBurstFiring && forcedBurst._burstAmount > 0 && (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) && self.Bolt.CurPos == ClosedBolt.BoltPos.Forward && modeType == ClosedBoltWeapon.FireSelectorModeType.Burst)
                {
                    self.DropHammer();
                }
                else if (modeType != ClosedBoltWeapon.FireSelectorModeType.Burst || ((self.Magazine == null || !self.Magazine.HasARound()) && !self.Chamber.IsFull && !self.m_proxy.IsFull))
                {
                    forcedBurst._isBurstFiring = false;
                }
                else if (forcedBurst.CooldownPeriod > 0f && forcedBurst._shouldCoolDown && !forcedBurst._isCoolingDown) forcedBurst.StartCoroutine(forcedBurst.Cooldown());
            }
        }

        private static void ClosedBoltWeapon_DropHammer(On.FistVR.ClosedBoltWeapon.orig_DropHammer orig, ClosedBoltWeapon self)
        {
            if (_existingForceBurst.TryGetValue(self, out ForcedBurst forcedBurst) && (forcedBurst._shouldCoolDown || forcedBurst._isCoolingDown)) return;

            orig(self);

            if (forcedBurst != null)
            {
                ClosedBoltWeapon.FireSelectorModeType modeType = self.FireSelector_Modes[self.m_fireSelectorMode].ModeType;
                if (!forcedBurst._isBurstFiring && modeType == ClosedBoltWeapon.FireSelectorModeType.Burst)
                {
                    forcedBurst._isBurstFiring = true;
                    forcedBurst._burstAmount = self.m_CamBurst - 1;
                }
                else if (forcedBurst._isBurstFiring && modeType == ClosedBoltWeapon.FireSelectorModeType.Burst && forcedBurst._burstAmount > 0)
                {
                    forcedBurst._burstAmount--;
                }

                if (forcedBurst._burstAmount == 0)
                {
                    forcedBurst._shouldCoolDown = true;
                    forcedBurst._isBurstFiring = false;
                }
            }
        }
        private IEnumerator Cooldown()
        {
            _isCoolingDown = true;
            yield return new WaitForSeconds(CooldownPeriod);
            _isCoolingDown = false;
            _shouldCoolDown = false;
        }

        private static void Handgun_FVRUpdate(On.FistVR.Handgun.orig_FVRUpdate orig, Handgun self)
        {
            orig(self);

            if (_existingForceBurst.TryGetValue(self, out ForcedBurst forcedBurst))
            {
                Handgun.FireSelectorModeType modeType = self.FireSelectorModes[self.m_fireSelectorMode].ModeType;
                if (forcedBurst._isBurstFiring && forcedBurst._burstAmount > 0 && (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) && self.Slide.CurPos == HandgunSlide.SlidePos.Forward && modeType == Handgun.FireSelectorModeType.Burst)
                {
                    self.ReleaseSeer();
                }
                else if (modeType != Handgun.FireSelectorModeType.Burst || ((self.Magazine == null || !self.Magazine.HasARound()) && !self.Chamber.IsFull && !self.m_proxy.IsFull))
                {
                    forcedBurst._isBurstFiring = false;
                }
                else if (forcedBurst.CooldownPeriod > 0f && forcedBurst._shouldCoolDown && !forcedBurst._isCoolingDown) forcedBurst.StartCoroutine(forcedBurst.Cooldown());
            }
        }

        private static void Handgun_ReleaseSeer(On.FistVR.Handgun.orig_ReleaseSeer orig, Handgun self)
        {
            if (_existingForceBurst.TryGetValue(self, out ForcedBurst forcedBurst) && (forcedBurst._shouldCoolDown || forcedBurst._isCoolingDown)) return;

            if (forcedBurst != null && self.m_isHammerCocked && self.m_isSeerReady)
            {
                Handgun.FireSelectorModeType modeType = self.FireSelectorModes[self.m_fireSelectorMode].ModeType;
                if (!forcedBurst._isBurstFiring && modeType == Handgun.FireSelectorModeType.Burst)
                {
                    forcedBurst._isBurstFiring = true;
                    forcedBurst._burstAmount = self.m_CamBurst - 1;
                }
                else if (forcedBurst._isBurstFiring && modeType == Handgun.FireSelectorModeType.Burst && forcedBurst._burstAmount > 0)
                {
                    forcedBurst._burstAmount--;
                }

                if (forcedBurst._burstAmount == 0)
                {
                    forcedBurst._shouldCoolDown = true;
                    forcedBurst._isBurstFiring = false;
                }
            }

            orig(self);
        }
#endif
    }
}
