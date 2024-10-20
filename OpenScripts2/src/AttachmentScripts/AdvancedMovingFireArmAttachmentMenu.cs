using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static OpenScripts2.AdvancedMovingFireArmAttachmentInterface;

namespace OpenScripts2
{
    public class AdvancedMovingFireArmAttachmentMenu : OpenScripts2_BasePlugin
    {
        public AdvancedMovingFireArmAttachmentInterface Interface;

        public Text ModeText;
        public Text LimitingAxisText;
        public Text RotationAxisText;
        public Text RotationStepText;
        public Text ResetMode;

        public OptionsPanel_ButtonSet PinButtonSet;

        public BoxCollider OverlapCollider;

        public LayerMask HitMask;

        private Vector3 _center;
        private Vector3 _halfExtends;

        private Collider[] _hits;

        public void Awake()
        {
            _hits = new Collider[32];
            
            _halfExtends = OverlapCollider.transform.lossyScale.MultiplyComponentWise(OverlapCollider.size) / 2f;
            _halfExtends.z = 0.005f;
            gameObject.SetActive(false);

            
        }

        public void Update()
        {
            if (Interface != null)
            {
                UpdateMenu();

                CheckForOverlap();
            }
        }

        private void UpdateMenu()
        {
            ModeText.text = ((int)Interface.CurrentMovementMode + 1).ToString() + "D: " + Interface.CurrentMovementMode.ToString();
            LimitingAxisText.text = LimitingAxisTextGenerator();
            RotationAxisText.text = "Rotation Axis: " + Interface.CurrentRotationalAxis.ToString();
            RotationStepText.text = "Rotation Step: " + Interface.RotationStepOptions[Interface.CurrentRotationalStepOption].ToString() + "°";

            ResetMode.text = Interface.ResetMode switch
            {
                EResetMode.Axis => "Reset Position Axis",
                EResetMode.Rotation => "Reset Rotation Axis",
                EResetMode.All => "Reset All",
                _ => throw new NotImplementedException(),
            };

            if (PinButtonSet.gameObject.activeSelf && Interface.PinCheckSource == null) PinButtonSet.gameObject.SetActive(false);
        }

        private void CheckForOverlap()
        {
            Vector3 newUIPosition = Interface.MenuPointProxy.localPosition;
            newUIPosition = Interface.MenuPointProxy.parent.localRotation.Invert() * newUIPosition;

            newUIPosition = Interface.MenuPointProxy.parent.TransformPoint(newUIPosition);

            if (transform.position.NotEqual(newUIPosition)) transform.position = newUIPosition;
            //if (transform.rotation.NotEqual(Interface.MenuPointProxy.rotation)) transform.rotation = Interface.MenuPointProxy.rotation;

            _center = OverlapCollider.transform.position;

            int hits = Physics.OverlapBoxNonAlloc(_center, _halfExtends, _hits, OverlapCollider.transform.rotation, HitMask, QueryTriggerInteraction.Ignore);

            Vector3[] directions = new Vector3[hits];
            float[] distances = new float[hits];

            if (hits > 0)
            {
                float origZSize = OverlapCollider.size.z;
                // Make Z size larger so it doesn't try moving in Z axis because that would be the longest penetration (unless the other collider is a cube that is even larger than 10cm I suppose?)
                Vector3 size = OverlapCollider.size;
                size.z = 2000f;
                OverlapCollider.size = size;
                for (int i = 0; i < hits; i++)
                {
                    Collider otherCollider = _hits[i];

                    Vector3 otherColliderPos = otherCollider.transform.position;
                    Quaternion otherColliderRot = otherCollider.transform.rotation;
                    
                    if (Physics.ComputePenetration(OverlapCollider, OverlapCollider.transform.position, OverlapCollider.transform.rotation, otherCollider, otherColliderPos, otherColliderRot, out Vector3 direction, out float distance))
                    {
                        //direction = transform.parent.InverseTransformDirection(direction);
                        //if (direction.z < 0) direction.z = -direction.z;
                        //direction = transform.parent.TransformDirection(direction);

                        directions[i] = direction;
                        distances[i] = distance * 1.1f;
                        //transform.position = transform.position + direction * distance;
                    }
                    
                }

                size.z = origZSize;
                OverlapCollider.size = size;
                int largestIndex = 0;
                float biggestDistance = float.MinValue;
                for (int i = 0; i < hits; i++)
                {
                    if (distances[i] > biggestDistance)
                    {
                        biggestDistance = distances[i];
                        largestIndex = i;
                    }
                }

                //Log($"Direction and distance of largest penetration: {directions[largestIndex]}, {distances[largestIndex]}");
                transform.position = transform.position + directions[largestIndex] * distances[largestIndex];
            }
        }

        private string LimitingAxisTextGenerator() => Interface.CurrentMovementMode switch
        {
            EMovementMode.Linear => "Current Axis: " + Interface.CurrentLimitingAxis.ToString(),
            EMovementMode.Planar => Interface.CurrentLimitingAxis switch
            {
                Axis.X => "Current Plane: YZ",
                Axis.Y => "Current Plane: XZ",
                Axis.Z => "Current Plane: XY",
                _ => string.Empty,
            },
            EMovementMode.Spacial => "Spacial Mode: Unlimited",
            _ => string.Empty,
        };

        public void SetPinButtonState(bool state) => PinButtonSet.SetSelectedButton(state);

        public void NextResetMode() => Interface.NextResetMode();

        public void PreviousResetMode() => Interface.PreviousResetMode();

        public void NextDegreeOfFreedom() => Interface.NextDegreeOfFreedom();

        public void PreviousDegreeOfFreedom() => Interface.PreviousDegreeOfFreedom();

        public void NextLimitingAxis() => Interface.NextLimitingAxis();

        public void PreviousLimitingAxis() => Interface.PreviousLimitingAxis();

        public void NextRotationalAxis() => Interface.NextRotationalAxis();

        public void PreviousRotationalAxis() => Interface.PreviousRotationalAxis();

        public void NextRotationalStepOption() => Interface.NextRotationalStepOption();

        public void PreviousRotationalStepOption() => Interface.PreviousRotationalStepOption();

        public void Reset() => Interface.Reset();

        public void CloseMenu() => Interface.CloseMenu();

        public void RotateLeft() => Interface.RotateLeft();

        public void RotateRight() => Interface.RotateRight();

        public void TryPin() => Interface.TryPin();
    }
}
