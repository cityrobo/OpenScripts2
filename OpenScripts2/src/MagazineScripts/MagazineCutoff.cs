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

        public OpenScripts2_BasePlugin.TransformType translationType;
        public OpenScripts2_BasePlugin.Axis axis;

        [Header("Sound")]
        public AudioEvent Sounds;

        private Vector3 _startPos;
        private Vector3 _stopPos;

        private Quaternion _startRot;
        private Quaternion _stopRot;

        private bool _isMoving = false;
        private bool _isActive = false;

        private OpenScripts2_BepInExPlugin plugin = OpenScripts2_BepInExPlugin.Instance;
        public override void Start()
        {
            base.Start();

            IsSimpleInteract = true;
            CalculatePositions();
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            base.SimpleInteraction(hand);

            _isActive = !_isActive;

            SM.PlayGenericSound(Sounds, CutoffLever.position);
            switch (translationType)
            {
                case OpenScripts2_BasePlugin.TransformType.Movement:
                    if (_isMoving) StopAllCoroutines();
                    StartCoroutine(Activate_Translation());
                    break;
                case OpenScripts2_BasePlugin.TransformType.Rotation:
                    if (_isMoving) StopAllCoroutines();
                    StartCoroutine(Activate_Rotation());
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, "Scale not supported!");
                    break;
            }
        }

        void CalculatePositions()
        {
            switch (translationType)
            {
                case OpenScripts2_BasePlugin.TransformType.Movement:
                    switch (axis)
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
                        default:
                            _startPos = new Vector3();
                            _stopPos = new Vector3();
                            break;
                    }
                    CutoffLever.localPosition = _startPos;
                    break;
                case OpenScripts2_BasePlugin.TransformType.Rotation:
                    switch (axis)
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
                        default:
                            _startRot = Quaternion.identity;
                            _stopRot = Quaternion.identity;
                            break;
                    }
                    CutoffLever.localRotation = _startRot;
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, "Scale not supported!");
                    break;
            }
        }

        IEnumerator Activate_Translation()
        {
            _isMoving = true;

            Vector3 target = _isActive ? _stopPos : _startPos;

            while (CutoffLever.localPosition != target)
            {
                CutoffLever.localPosition = Vector3.MoveTowards(CutoffLever.localPosition, target, Speed * Time.deltaTime);
                yield return null;
            }

            Activate_Magazine();
            _isMoving = false;
        }

        IEnumerator Activate_Rotation()
        {
            _isMoving = true;

            Quaternion target = _isActive ? _stopRot : _startRot;

            while (CutoffLever.localRotation != target)
            {
                CutoffLever.localRotation = Quaternion.RotateTowards(CutoffLever.localRotation, target, Speed * Time.deltaTime);
                yield return null;
            }

            Activate_Magazine();
            _isMoving = false;
        }

        void Activate_Magazine()
        {
            if (_isActive)
            {
                FireArm.Magazine.IsExtractable = false;
            }
            else
            {
                FireArm.Magazine.IsExtractable = true;
            }
        }
    }
}
