using System;
using UnityEngine;

namespace FistVR
{
    public class ClosedBoltRotatingChargingHandle : FVRInteractiveObject
    {
        [Header("Closed Bolt Rotating Charging Handle")]
        public Transform Handle;
        public Transform ReferenceVector;
        public float RotLimit;
        public ClosedBolt Bolt;
        public float ForwardSpeed = 360f;

        private float m_currentHandleZ;
        private Placement m_curPos;
        private Placement m_lastPos;

        public enum Placement
        {
            Forward,
            Middle,
            Rearward
        }

        public override void Awake()
        {
            base.Awake();
            m_currentHandleZ = RotLimit;
            Vector3 forward = Quaternion.AngleAxis(m_currentHandleZ, ReferenceVector.up) * ReferenceVector.forward;
            Handle.rotation = Quaternion.LookRotation(forward, ReferenceVector.up);
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            Vector3 target = Vector3.ProjectOnPlane(hand.Input.Pos - Handle.transform.position, ReferenceVector.up);
            Vector3 v = Vector3.RotateTowards(ReferenceVector.forward, target, Mathf.Deg2Rad * RotLimit, 1f);
            float signedAngle = AngleSigned(ReferenceVector.forward, v, ReferenceVector.up);
            m_currentHandleZ = signedAngle;
            Vector3 forward = Quaternion.AngleAxis(m_currentHandleZ, ReferenceVector.up) * ReferenceVector.forward;
            Handle.rotation = Quaternion.LookRotation(forward, ReferenceVector.up);
            float handleInverseLerp = Mathf.InverseLerp(RotLimit, -RotLimit, signedAngle);
            Bolt.UpdateHandleHeldState(true, handleInverseLerp);
        }

        public override void EndInteraction(FVRViveHand hand)
        {
            base.EndInteraction(hand);
            Bolt.UpdateHandleHeldState(false, 0f);
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();
            float handleInverseLerp = Mathf.InverseLerp(RotLimit, -RotLimit, m_currentHandleZ);
            if (handleInverseLerp < 0.01f)
            {
                m_curPos = Placement.Forward;
            }
            else if (handleInverseLerp > 0.99f)
            {
                m_curPos = Placement.Rearward;
            }
            else
            {
                m_curPos = Placement.Middle;
            }
            if (!IsHeld && Mathf.Abs(m_currentHandleZ - RotLimit) >= 0.01f)
            {
                m_currentHandleZ = Mathf.MoveTowards(m_currentHandleZ, RotLimit, Time.deltaTime * ForwardSpeed);
                Vector3 forward = Quaternion.AngleAxis(m_currentHandleZ, ReferenceVector.up) * ReferenceVector.forward;
                Handle.rotation = Quaternion.LookRotation(forward, ReferenceVector.up);
            }
            if (m_curPos == Placement.Forward && m_lastPos != Placement.Forward)
            {
                Bolt.Weapon.PlayAudioEvent(FirearmAudioEventType.HandleForward, 1f);
            }
            else if (m_lastPos == Placement.Rearward && m_lastPos != Placement.Rearward)
            {
                Bolt.Weapon.PlayAudioEvent(FirearmAudioEventType.HandleBack, 1f);
            }
            m_lastPos = m_curPos;
        }

        public float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
        {
            return Mathf.Atan2(Vector3.Dot(n, Vector3.Cross(v1, v2)), Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
        }
    }
}