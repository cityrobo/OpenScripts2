using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    class MagazineSafety : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm = null;
        public int SafetyFireModePosition = 0;

        private int _lastFireMode;
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
                default:
                    break;
            }
        }

        void CheckState(OpenBoltReceiver s)
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
        void CheckState(ClosedBoltWeapon s)
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
        void CheckState(BoltActionRifle s)
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
    }
}
