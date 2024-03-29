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
    public class RSC_MagazineFollower_TubeFedShotgun : FVRInteractiveObject
    {
        [Header("RSC-MagazineFollower Config")]
        public TubeFedShotgun FireArm;
        public GameObject MagazineReloadTrigger;
        public Transform Follower;

        public OpenScripts2_BasePlugin.Axis RotationalAxis;
        public float RotSpeed = 100f;

        [Tooltip("first entry is Angle when open to load magazine, all following are for the ammount loaded in the magazine, starting with full mag. Last pos is follower with empty mag.")]
        public float[] RotationalAngles;


        private bool _open = true;
        private bool _lockedSafety = false;
        private bool _origSafetyPos;
        private float _lastRot;

        public override void Start()
        {
            base.Start();
            StartCoroutine(SetFollowerRot(RotationalAngles[0]));
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            base.SimpleInteraction(hand);

            switch (_open)
            {
                case false:
                    _open = true;
                    FireArm.PlayAudioEvent(FirearmAudioEventType.BreachOpen);
                    break;
                case true:
                    _open = false;
                    FireArm.PlayAudioEvent(FirearmAudioEventType.BreachClose);
                    break;
            }

            if (_open)
            {
                StopAllCoroutines();
                StartCoroutine(SetFollowerRot(RotationalAngles[0]));
            }
            else if (!_open)
            {
                MagazineReloadTrigger.SetActive(false);

                if (FireArm.Magazine == null)
                {
                    StopAllCoroutines();
                    StartCoroutine(SetFollowerRot(RotationalAngles[RotationalAngles.Length-1]));
                }
            }
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (_open && !_lockedSafety)
            {
                _origSafetyPos = FireArm.m_isSafetyEngaged;
                FireArm.m_isSafetyEngaged = true;

                _lockedSafety = true;
            }
            else if (_open && _lockedSafety)
            {
                FireArm.m_isSafetyEngaged = true;
            }
            else if (!_open && _lockedSafety)
            {
                FireArm.m_isSafetyEngaged = _origSafetyPos;
                _lockedSafety = false;
            }
            FVRFireArmMagazine magazine = FireArm.Magazine;
            if (!_open && FireArm.Magazine != null)
            {
                int roundCount = magazine.m_numRounds;
                int magCap = magazine.m_capacity;

                int rotIndex = magCap - roundCount + 1;

                if (_lastRot != RotationalAngles[rotIndex])
                {
                    StopAllCoroutines();
                    StartCoroutine(SetFollowerRot(RotationalAngles[rotIndex]));
                }
            }
        }

        IEnumerator SetFollowerRot(float rot)
        {
            bool rotDone = false;
            _lastRot = rot;

            Quaternion targetRotation = OpenScripts2_BasePlugin.GetTargetQuaternionFromAxis(rot, RotationalAxis);

            while (!rotDone)
            {
                Follower.localRotation = Quaternion.RotateTowards(Follower.localRotation, targetRotation, RotSpeed * Time.deltaTime);
                rotDone = Follower.localRotation == targetRotation;
                yield return null;
            }
            if (_open)
            {
                MagazineReloadTrigger.SetActive(true);
            }
        }
    }
}
