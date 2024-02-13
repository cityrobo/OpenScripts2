using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class MagazineSafety : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm = null;
        public int SafetyFireModePosition = 0;

        private int _lastFireMode;
        private bool _lastSafetyState;
        private bool _magSafetyEngaged;

        public void Update()
        {
            switch (FireArm)
            {
                case OpenBoltReceiver s:
                    CheckState(s);
                    break;
                case ClosedBoltWeapon s:
                    CheckState(s);
                    break;
                case BoltActionRifle s:
                    CheckState(s);
                    break;
                case TubeFedShotgun s:
                    CheckState(s);
                    break;
                default:
                    LogWarning($"Firearm type \"{FireArm.GetType()}\" not supported!");
                    break;
            }
        }

        private void CheckState(OpenBoltReceiver s)
        {
            if (s.Magazine == null && s.m_fireSelectorMode != SafetyFireModePosition)
            {
                _lastFireMode = s.m_fireSelectorMode;
                s.m_fireSelectorMode = SafetyFireModePosition;
                _magSafetyEngaged = true;
            }
            else if (s.Magazine != null && _magSafetyEngaged)
            {
                s.m_fireSelectorMode = _lastFireMode;
                _magSafetyEngaged = false;
            }
        }
        private void CheckState(ClosedBoltWeapon s)
        {
            if (s.Magazine == null && s.m_fireSelectorMode != SafetyFireModePosition)
            {
                _lastFireMode = s.m_fireSelectorMode;
                s.m_fireSelectorMode = SafetyFireModePosition;
                _magSafetyEngaged = true;
            }
            else if (s.Magazine != null && _magSafetyEngaged)
            {
                s.m_fireSelectorMode = _lastFireMode;
                _magSafetyEngaged = false;
            }
        }
        private void CheckState(BoltActionRifle s)
        {
            if (s.Magazine == null && s.m_fireSelectorMode != SafetyFireModePosition)
            {
                _lastFireMode = s.m_fireSelectorMode;
                s.m_fireSelectorMode = SafetyFireModePosition;
                _magSafetyEngaged = true;
            }
            else if (s.Magazine != null && _magSafetyEngaged)
            {
                s.m_fireSelectorMode = _lastFireMode;
                _magSafetyEngaged = false;
            }
        }
        private void CheckState(TubeFedShotgun s)
        {
            if (s.Magazine == null && !s.IsSafetyEngaged)
            {
                _lastSafetyState = s.IsSafetyEngaged;
                s.m_isSafetyEngaged = true;
                _magSafetyEngaged = true;
            }
            else if (s.Magazine != null && _magSafetyEngaged)
            {
                s.m_isSafetyEngaged = _lastSafetyState;
                _magSafetyEngaged = false;
            }
        }
    }
}
