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

        // Preallocated variables so they don't need to be garbage collected all the time
        private float _boltLerp;
        float _inverseLerp;
        private Vector3 _lerpPosBolt;
        private Vector3 _lerpPosBarrel;

        private bool _soundPlayed = false;

        public void Start()
        {
            _currentZ = OriginalBolt.m_boltZ_current;
            _lastZ = _currentZ;
        }

        public void Update()
        {
            _currentZ = OriginalBolt.m_boltZ_current;
            if (OriginalBolt.IsHeld || _wasHeld)
            {
                _boltLerp = OriginalBolt.GetBoltLerpBetweenRearAndFore();
                _lerpPosBolt = Vector3.Lerp(NewBoltRearwardPos.localPosition, NewBoltForwardPos.localPosition, _boltLerp);
                NewBolt.transform.localPosition = _lerpPosBolt;
                if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.Forward) _wasHeld = false;
                else _wasHeld = true;
            }
            else if (!_wasHeld)
            {
                if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.ForwardToMid && _currentZ < _lastZ)
                {
                    _boltLerp = OriginalBolt.GetBoltLerpBetweenLockAndFore();
                    if (_boltLerp >= (1f - BarrelForwardThreshhold))
                    {
                        _inverseLerp = Mathf.InverseLerp((1f - BarrelForwardThreshhold), 1f, _boltLerp);
                        _lerpPosBolt = Vector3.Lerp(NewBoltRearwardPos.localPosition, NewBoltForwardPos.localPosition, _inverseLerp);
                        _lerpPosBarrel = Vector3.Lerp(BarrelRearwardPos.localPosition, BarrelForwardPos.localPosition, _inverseLerp);

                        NewBolt.transform.localPosition = _lerpPosBolt;
                        Barrel.transform.localPosition = _lerpPosBarrel;
                    }
                    else if (_boltLerp < (1f - BarrelForwardThreshhold))
                    {
                        _inverseLerp = Mathf.InverseLerp((1f - BarrelForwardThreshhold), 0f, _boltLerp);
                        _lerpPosBarrel = Vector3.Lerp(BarrelRearwardPos.localPosition, BarrelLockingPos.localPosition, _inverseLerp);

                        NewBolt.transform.localPosition = NewBoltRearwardPos.localPosition;
                        Barrel.transform.localPosition = _lerpPosBarrel;
                    }
                }
                else if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.Locked && _currentZ < _lastZ)
                {
                    NewBolt.transform.localPosition = NewBoltRearwardPos.localPosition;
                    Barrel.transform.localPosition = BarrelLockingPos.localPosition;
                }
                else if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.LockedToRear && _currentZ < _lastZ)
                {
                    _boltLerp = Mathf.InverseLerp(OriginalBolt.m_boltZ_lock, OriginalBolt.m_boltZ_rear, OriginalBolt.m_boltZ_current);
                    _lerpPosBarrel = Vector3.Lerp(BarrelLockingPos.localPosition, BarrelForwardPos.localPosition, _boltLerp);

                    NewBolt.transform.localPosition = NewBoltRearwardPos.localPosition;
                    Barrel.transform.localPosition = _lerpPosBarrel;
                }
                else if (OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.Rear && (_currentZ < _lastZ || _currentZ == _lastZ))
                {
                    NewBolt.transform.localPosition = NewBoltRearwardPos.localPosition;
                    Barrel.transform.localPosition = BarrelForwardPos.localPosition;
                }
                else if ((OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.LockedToRear || OriginalBolt.CurPos == TubeFedShotgunBolt.BoltPos.ForwardToMid) && _currentZ > _lastZ)
                {
                    _boltLerp = OriginalBolt.GetBoltLerpBetweenRearAndFore();
                    _lerpPosBolt = Vector3.Lerp(NewBoltRearwardPos.localPosition, NewBoltForwardPos.localPosition, _boltLerp);

                    NewBolt.transform.localPosition = _lerpPosBolt;
                    Barrel.transform.localPosition = BarrelForwardPos.localPosition;
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
                // Sound
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
