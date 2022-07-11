using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class LongRecoilSystem_TubeFedShotgun : OpenScripts2_BasePlugin
    {
        public TubeFedShotgunBolt OriginalBolt;

        public GameObject NewBolt;
        public Transform NewBoltForwardPos;
        public Transform NewBoltLockingPos;
        public Transform NewBoltRearwardPos;

        public GameObject Barrel;
        public Transform BarrelForwardPos;
        public Transform BarrelLockingPos;
        public Transform BarrelRearwardPos;
        [Range(0.01f, 0.99f)]
        public float BarrelForwardThreshhold = 0.9f;

        [Header("Sound")]
        public AudioEvent BarrelHitForward;

        private bool _wasHeld = false;
        private float _currentZ;
        private float _lastZ;

        private bool _soundPlayed = false;

        public void Start()
        {
            _currentZ = OriginalBolt.m_boltZ_current;
            _lastZ = _currentZ;
        }

        public void Update()
        {
            _currentZ = OriginalBolt.m_boltZ_current;
            if (OriginalBolt.IsHeld)
            {
                float boltLerp = OriginalBolt.GetBoltLerpBetweenRearAndFore();
                Vector3 lerpPos = Vector3.Lerp(NewBoltRearwardPos.localPosition, NewBoltForwardPos.localPosition, boltLerp);
                NewBolt.transform.localPosition = lerpPos;
                _wasHeld = true;
            }

            if (_wasHeld)
            {
                float boltLerp = OriginalBolt.GetBoltLerpBetweenRearAndFore();
                Vector3 lerpPos = Vector3.Lerp(NewBoltRearwardPos.localPosition, NewBoltForwardPos.localPosition, boltLerp);
                NewBolt.transform.localPosition = lerpPos;
                if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.Forward) _wasHeld = false;
            }

            if (!_wasHeld)
            {
                if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.ForwardToMid && _currentZ < _lastZ)
                {
                    float boltLerp = OriginalBolt.GetBoltLerpBetweenLockAndFore();
                    if (boltLerp >= (1f - BarrelForwardThreshhold))
                    {
                        float inverseLerp = Mathf.InverseLerp((1f - BarrelForwardThreshhold), 1f, boltLerp);
                        Vector3 lerpPosBolt = Vector3.Lerp(NewBoltRearwardPos.localPosition, NewBoltForwardPos.localPosition, inverseLerp);
                        Vector3 lerpPosBarrel = Vector3.Lerp(BarrelRearwardPos.localPosition, BarrelForwardPos.localPosition, inverseLerp);

                        NewBolt.transform.localPosition = lerpPosBolt;
                        Barrel.transform.localPosition = lerpPosBarrel;
                    }
                    else if (boltLerp < (1f - BarrelForwardThreshhold))
                    {
                        float inverseLerp = Mathf.InverseLerp((1f - BarrelForwardThreshhold), 0f, boltLerp);
                        Vector3 lerpPosBarrel = Vector3.Lerp(BarrelRearwardPos.localPosition, BarrelLockingPos.localPosition, inverseLerp);

                        Barrel.transform.localPosition = lerpPosBarrel;
                    }
                }
                else if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.Locked && _currentZ < _lastZ)
                {
                    NewBolt.transform.localPosition = NewBoltRearwardPos.localPosition;
                    Barrel.transform.localPosition = BarrelLockingPos.localPosition;
                }
                else if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.LockedToRear && _currentZ < _lastZ)
                {
                    float boltLerp = Mathf.InverseLerp(OriginalBolt.m_boltZ_lock, OriginalBolt.m_boltZ_rear, OriginalBolt.m_boltZ_current);
                    Vector3 lerpPosBarrel = Vector3.Lerp(BarrelLockingPos.localPosition, BarrelForwardPos.localPosition, boltLerp);

                    Barrel.transform.localPosition = lerpPosBarrel;
                }
                else if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.Rear && (_currentZ < _lastZ || _currentZ == _lastZ))
                {
                    NewBolt.transform.localPosition = NewBoltRearwardPos.localPosition;
                    Barrel.transform.localPosition = BarrelForwardPos.localPosition;
                }
                else if ((OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.LockedToRear || OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.ForwardToMid) && _currentZ > _lastZ)
                {
                    float boltLerp = OriginalBolt.GetBoltLerpBetweenRearAndFore();
                    Vector3 lerpPosBolt = Vector3.Lerp(NewBoltRearwardPos.localPosition, NewBoltForwardPos.localPosition, boltLerp);

                    NewBolt.transform.localPosition = lerpPosBolt;
                }
                else if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.Forward && (_currentZ > _lastZ || _currentZ == _lastZ))
                {
                    NewBolt.transform.localPosition = NewBoltForwardPos.localPosition;
                    Barrel.transform.localPosition = BarrelForwardPos.localPosition;

                    _soundPlayed = false;
                }
                else if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.Locked && _currentZ == _lastZ)
                {
                    NewBolt.transform.localPosition = NewBoltLockingPos.localPosition;
                    Barrel.transform.localPosition = BarrelForwardPos.localPosition;
                }

                if (!_soundPlayed && ((OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.Rear && (_currentZ < _lastZ || _currentZ == _lastZ)) || (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.LockedToRear && _currentZ > _lastZ) || (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.ForwardToMid && _currentZ > _lastZ)))
                {
                    SM.PlayGenericSound(BarrelHitForward, transform.position);
                    _soundPlayed = true;
                }
            }
            _lastZ = _currentZ;
        }
    }
}
