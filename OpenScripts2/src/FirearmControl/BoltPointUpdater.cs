using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class BoltPointUpdater : MonoBehaviour
    {
        public FVRFireArm FireArm;

        private struct BoltZs
        {
            public float forwardZ;
            public float lockZ;
            public float rearZ;
            public float safetyLockZ;
        }

        // X = PointForwardZ, Y = PointLockZ, Z = PointRearZ
        private Vector3 _lastBoltZs;
        private Vector3 _lastHandleZs;

        public void Start()
        {
            Vector3 curBoltZs = GetCurrentBoltZs();
            Vector3 curHandleZs = GetCurrentHandleZs();

            ApplyBoltZs(curBoltZs);
            ApplyHandleZs(curHandleZs);

            UpdateLastBoltPointPositions();
        }

        public void Update()
        {
            Vector3 curBoltZs = GetCurrentBoltZs();
            Vector3 curHandleZs = GetCurrentHandleZs();

            if (curBoltZs != _lastBoltZs)
            {
                ApplyBoltZs(curBoltZs);
            }

            if (curHandleZs != _lastHandleZs)
            {
                ApplyHandleZs(curHandleZs);
            }

            UpdateLastBoltPointPositions();
        }

        private void UpdateLastBoltPointPositions()
        {
            _lastBoltZs = FireArm switch
            {
                ClosedBoltWeapon w => new Vector3(w.Bolt.Point_Bolt_Forward.localPosition.z, w.Bolt.Point_Bolt_LockPoint.localPosition.z, w.Bolt.Point_Bolt_Rear.localPosition.z),
                Handgun w => new Vector3(w.Slide.Point_Slide_Forward.localPosition.z, w.Slide.Point_Slide_LockPoint.localPosition.z, w.Slide.Point_Slide_Rear.localPosition.z),
                _ => Vector3.zero,
            };
            _lastHandleZs = FireArm switch
            {
                ClosedBoltWeapon w => w.Handle != null ?  new Vector3(w.Handle.Point_Forward.localPosition.z, w.Handle.Point_LockPoint.localPosition.z, w.Handle.Point_Rear.localPosition.z) : Vector3.zero,
                _ => Vector3.zero,
            };
        }

        private Vector3 GetCurrentBoltZs()
        {
            return FireArm switch
            {
                ClosedBoltWeapon w => new Vector3(w.Bolt.Point_Bolt_Forward.localPosition.z, w.Bolt.Point_Bolt_LockPoint.localPosition.z, w.Bolt.Point_Bolt_Rear.localPosition.z),
                _ => Vector3.zero,
            };
        }

        private Vector3 GetCurrentHandleZs()
        {
            return FireArm switch
            {
                ClosedBoltWeapon w => w.Handle != null ? new Vector3(w.Handle.Point_Forward.localPosition.z, w.Handle.Point_LockPoint.localPosition.z, w.Handle.Point_Rear.localPosition.z) : Vector3.zero,
                _ => Vector3.zero,
            };
        }
        
        private void ApplyBoltZs(Vector3 currentBoltZs)
        {
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    w.Bolt.m_boltZ_forward = currentBoltZs.x;
                    w.Bolt.m_boltZ_lock = currentBoltZs.y;
                    w.Bolt.m_boltZ_rear = currentBoltZs.z;
                    break;
                case Handgun w:
                    w.Slide.m_slideZ_forward = currentBoltZs.x;
                    w.Slide.m_slideZ_lock = currentBoltZs.y;
                    w.Slide.m_slideZ_rear = currentBoltZs.z;
                    break;
            }
        }

        private void ApplyHandleZs(Vector3 currentHandleZs)
        {
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    if (w.Handle == null) break;
                    w.Handle.m_posZ_forward = currentHandleZs.x;
                    w.Handle.m_posZ_lock = currentHandleZs.y;
                    w.Handle.m_posZ_rear = currentHandleZs.z;
                    break;
            }
        }
    }
}