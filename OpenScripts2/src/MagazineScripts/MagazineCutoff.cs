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
    public class MagazineCutoff : FVRInteractiveObject
    {
        [Header("Magazine Cutoff Config")]
        public FVRFireArm FireArm;
        public Transform CutoffLever;

        public float StartLimit;
        public float StopLimit;
        public float Speed;

        public OpenScripts2_BasePlugin.TransformType MovementType;
        public OpenScripts2_BasePlugin.Axis Axis;

        [Header("Sound")]
        public AudioEvent Sounds;

        private Vector3 _startPos;
        private Vector3 _stopPos;

        private Quaternion _startRot;
        private Quaternion _stopRot;

        private bool _MagazineCuttoffActive = false;

        private FVRFireArmMagazine _mag;

        private Quaternion _targetRotation;
        private Vector3 _targetPosition;

        public override void Awake()
        {
            base.Start();

            IsSimpleInteract = true;
            CalculatePositions();

            _mag = FireArm.Magazine;
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            base.SimpleInteraction(hand);

            _MagazineCuttoffActive = !_MagazineCuttoffActive;

            SM.PlayGenericSound(Sounds, CutoffLever.position);
            switch (MovementType)
            {
                case OpenScripts2_BasePlugin.TransformType.Movement:
                    _targetPosition = _MagazineCuttoffActive ? _stopPos : _startPos;
                    break;
                case OpenScripts2_BasePlugin.TransformType.Rotation:
                    _targetRotation = _MagazineCuttoffActive ? _stopRot : _startRot;
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, "Scale not supported!");
                    break;
            }
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (_MagazineCuttoffActive && _mag != null)
            {
                if (_mag.FireArm == FireArm)
                {
                    _mag.IsExtractable = false;
                }
                else
                {
                    _mag.IsExtractable = true;
                    _mag = null;
                }
            }
            else if (!_MagazineCuttoffActive && _mag != null)
            {
                _mag.IsExtractable = true;
            }

            _mag = FireArm.Magazine;


            if (MovementType == OpenScripts2_BasePlugin.TransformType.Rotation && CutoffLever.localRotation != _targetRotation)
            {
                CutoffLever.localRotation = Quaternion.RotateTowards(CutoffLever.localRotation, _targetRotation, Speed * Time.deltaTime);
            }
            else if (MovementType == OpenScripts2_BasePlugin.TransformType.Movement && CutoffLever.localPosition != _targetPosition)
            {
                CutoffLever.localPosition = Vector3.MoveTowards(CutoffLever.localPosition, _targetPosition, Speed * Time.deltaTime);
            }
        }


        private void CalculatePositions()
        {
            switch (MovementType)
            {
                case OpenScripts2_BasePlugin.TransformType.Movement:
                    switch (Axis)
                    {
                        case OpenScripts2_BasePlugin.Axis.X:
                            _startPos = new Vector3(StartLimit, CutoffLever.localPosition.y, CutoffLever.localPosition.z);
                            _stopPos = new Vector3(StopLimit, CutoffLever.localPosition.y, CutoffLever.localPosition.z);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Y:
                            _startPos = new Vector3(CutoffLever.localPosition.x, StartLimit, CutoffLever.localPosition.z);
                            _stopPos = new Vector3(CutoffLever.localPosition.x, StopLimit, CutoffLever.localPosition.z);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Z:
                            _startPos = new Vector3(CutoffLever.localPosition.x, CutoffLever.localPosition.y, StartLimit);
                            _stopPos = new Vector3(CutoffLever.localPosition.x, CutoffLever.localPosition.y, StopLimit);
                            break;
                    }
                    CutoffLever.localPosition = _startPos;
                    break;
                case OpenScripts2_BasePlugin.TransformType.Rotation:
                    switch (Axis)
                    {
                        case OpenScripts2_BasePlugin.Axis.X:
                            _startRot = Quaternion.Euler(StartLimit, CutoffLever.localEulerAngles.y, CutoffLever.localEulerAngles.z);
                            _stopRot = Quaternion.Euler(StopLimit, CutoffLever.localEulerAngles.y, CutoffLever.localEulerAngles.z);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Y:
                            _startRot = Quaternion.Euler(CutoffLever.localEulerAngles.x, StartLimit, CutoffLever.localEulerAngles.z);
                            _stopRot = Quaternion.Euler(CutoffLever.localEulerAngles.x, StopLimit, CutoffLever.localEulerAngles.z);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Z:
                            _startRot = Quaternion.Euler(CutoffLever.localEulerAngles.x, CutoffLever.localEulerAngles.y, StartLimit);
                            _stopRot = Quaternion.Euler(CutoffLever.localEulerAngles.x, CutoffLever.localEulerAngles.y, StopLimit);
                            break;
                    }
                    CutoffLever.localRotation = _startRot;
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, "Scale not supported!");
                    break;
            }
        }
    }
}
