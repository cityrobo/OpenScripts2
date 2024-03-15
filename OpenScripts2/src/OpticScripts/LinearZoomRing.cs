using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FistVR;
using System;

namespace OpenScripts2
{
    public class LinearZoomRing : FVRInteractiveObject
    {
        [Header("Linear Zoom Ring Config")]

        public float StartingRotationValue;
        public float LowerRotationLimit;
        public float UpperRotationLimit;

        public float ZoomRingSpeedNormal = 0.3f;
        public float ZoomRingSpeedForced = 1f;

        [Serializable]
        public class MagnificationSubtension : IComparable<MagnificationSubtension>
        {
            public float Magnification;
            public float RotationValue;

            // Makes it sortable by RotationValue
            public int CompareTo(MagnificationSubtension other) => RotationValue.CompareTo(other.RotationValue);
        }

        public MagnificationSubtension[] MagnificationSubtensions;

        public AudioSource RotationAudioSource;
        public AudioClip[] RotationAudioClips;

        private Vector3 _lastHandRotationRef = Vector3.zero;
        private Vector3 _lastParentRotationRef = Vector3.zero;

        private float _currentRot;
        private float m_smoothedCatchRotDelta;

        public override void Awake()
        {
            base.Awake();

            _currentRot = StartingRotationValue;

            Array.Sort(MagnificationSubtensions);
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);

            _lastHandRotationRef = GetHandRotationRef();
            _lastParentRotationRef = GetParentRotationRef();
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (m_smoothedCatchRotDelta > 0f)
            {
                if (!RotationAudioSource.isPlaying)
                {
                    RotationAudioSource.clip = RotationAudioClips[UnityEngine.Random.Range(0, RotationAudioClips.Length)];
                    RotationAudioSource.Play();
                    RotationAudioSource.volume = Mathf.Clamp(m_smoothedCatchRotDelta * 0.03f, 0f, 1.2f);
                }
                else
                {
                    RotationAudioSource.volume = Mathf.Clamp(m_smoothedCatchRotDelta * 0.03f, 0f, 1.2f);
                }
                m_smoothedCatchRotDelta -= 400f * ZoomRingSpeedNormal * Time.deltaTime;
            }
            else if (RotationAudioSource != null)
            {
                RotationAudioSource.volume = 0f;
                if (RotationAudioSource.isPlaying)
                {
                    RotationAudioSource.Stop();
                }
            }
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);

            transform.localRotation = Quaternion.Euler(0f, 0f, _currentRot);
        }

        public override void FVRFixedUpdate()
        {
            if (IsHeld)
            {
                float inputMultiplicator = ZoomRingSpeedNormal;
                if (m_hand.IsInStreamlinedMode ? m_hand.Input.AXButtonPressed || m_hand.Input.BYButtonPressed : m_hand.Input.TouchpadPressed) inputMultiplicator = ZoomRingSpeedForced;

                float lastRot = _currentRot;
                Vector3 handRotationRef = GetHandRotationRef();
                Vector3 projectionPlaneNormal = transform.forward;
                Vector3 parentRotationRef = GetParentRotationRef();

                // Calculating hand rotation change impact on total rotation
                Vector3 currentHandRotationRefProjectedOnForwardPlane = Vector3.ProjectOnPlane(handRotationRef, projectionPlaneNormal);
                Vector3 lastHandRotationRefProjectedOnForwardPlane = Vector3.ProjectOnPlane(_lastHandRotationRef, projectionPlaneNormal);
                float deltaRotHand = Mathf.Atan2(Vector3.Dot(projectionPlaneNormal, Vector3.Cross(currentHandRotationRefProjectedOnForwardPlane, lastHandRotationRefProjectedOnForwardPlane)), Vector3.Dot(currentHandRotationRefProjectedOnForwardPlane, lastHandRotationRefProjectedOnForwardPlane)) * Mathf.Rad2Deg;
                _currentRot -= deltaRotHand * inputMultiplicator;

                // Calculating object rotation change impact on total rotation
                Vector3 currentParentRotationRefProjectedOnForwardPlane = Vector3.ProjectOnPlane(parentRotationRef, projectionPlaneNormal);
                Vector3 lastParentRotationRefProjectedOnForwardPlane = Vector3.ProjectOnPlane(_lastParentRotationRef, projectionPlaneNormal);
                float deltaRotParent = Mathf.Atan2(Vector3.Dot(projectionPlaneNormal, Vector3.Cross(currentParentRotationRefProjectedOnForwardPlane, lastParentRotationRefProjectedOnForwardPlane)), Vector3.Dot(currentParentRotationRefProjectedOnForwardPlane, lastParentRotationRefProjectedOnForwardPlane)) * Mathf.Rad2Deg;
                _currentRot += deltaRotParent * inputMultiplicator;
                _currentRot = Mathf.Clamp(_currentRot, LowerRotationLimit, UpperRotationLimit);

                CatchRotDeltaAdd(Mathf.Abs(_currentRot - lastRot));
                _lastHandRotationRef = handRotationRef;
                _lastParentRotationRef = parentRotationRef;
            }

            base.FVRFixedUpdate();
        }

        private Vector3 GetHandRotationRef()
        {
            return m_hand.Input.Forward;
        }

        private Vector3 GetParentRotationRef()
        {
            return transform.parent.up;
        }

        public float GetCurrentlySelectedMagnification()
        {
            float magnification = 1f;

            for (int i = 0; i < MagnificationSubtensions.Length; i++)
            {
                MagnificationSubtension subtension = MagnificationSubtensions[i];
                if (_currentRot >= subtension.RotationValue && i  + 1 < MagnificationSubtensions.Length)
                {
                    MagnificationSubtension nextSubtension = MagnificationSubtensions[i + 1];
                    if (_currentRot < nextSubtension.RotationValue)
                    {
                        float inverseLerp = Mathf.InverseLerp(subtension.RotationValue, nextSubtension.RotationValue, _currentRot);
                        magnification = Mathf.Lerp(subtension.Magnification, nextSubtension.Magnification, inverseLerp);
                        break;
                    }
                }
                else if (_currentRot >= subtension.RotationValue && i + 1 == MagnificationSubtensions.Length)
                {
                    magnification = subtension.Magnification;
                }
            }

            return magnification;
        }

        private void CatchRotDeltaAdd(float f)
        {
            m_smoothedCatchRotDelta += Mathf.Abs(f);
        }
    }
}