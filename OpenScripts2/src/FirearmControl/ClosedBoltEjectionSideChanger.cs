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
    public class ClosedBoltEjectionSideChanger : FVRInteractiveObject
    {
        [Header("Closed Bolt Weapon Config")]
        public ClosedBoltWeapon FireArm;

        public Transform SecondaryRoundPos_Ejection;
        public Vector3 SecondaryEjectionSpeed;
        public Vector3 SecondaryEjectionSpin;

        [Header("Second Dust Cover Config")]
        public ClosedBoltReceiverDustCoverTrigger DustCoverTrigger;

        public Transform SecondaryDustCoverGeo;
        public float SecondaryOpenRot;
        public float SecondaryClosedRot;

        // Original Variables
        private Vector3 _origPos;
        private Vector3 _origTriggerPos;

        private Transform _origDustCoverGeo;
        private float _origOpenRot;
        private float _origClosedRot;

        private Transform _origEjectPos;
        private Vector3 _origEjectionSpeed;
        private Vector3 _origEjectionSpin;

        // States
        private enum EState
        {
            Primary,
            Changing,
            Secondary
        }

        private EState _state;

        [ContextMenu("Copy and Mirror Ejection Pattern")]
        public void MirrorEjectionPattern()
        {
            if (SecondaryRoundPos_Ejection == null)
            {
                GameObject go = new("Secondary_" + FireArm.RoundPos_Ejection.name);
                SecondaryRoundPos_Ejection = go.transform;
            }

            SecondaryRoundPos_Ejection.parent = FireArm.RoundPos_Ejection.transform.parent;
            SecondaryRoundPos_Ejection.localPosition = Vector3.Reflect(FireArm.RoundPos_Ejection.transform.localPosition, FireArm.transform.right);

            SecondaryEjectionSpeed = Vector3.Reflect(FireArm.EjectionSpeed, FireArm.transform.right);
            SecondaryEjectionSpin = Vector3.Reflect(FireArm.EjectionSpin, FireArm.transform.right);
        }

        [ContextMenu("Copy and Mirror Dust Cover Rotation Values")]
        public void MirrorDustCoverRotations()
        {
            SecondaryOpenRot = -DustCoverTrigger.OpenRot;
            SecondaryClosedRot = -DustCoverTrigger.ClosedRot;
        }

        public override void Awake()
        {
            IsSimpleInteract = true;

            base.Awake();

            _origPos = transform.localPosition;
            _origTriggerPos = DustCoverTrigger.transform.localPosition;

            _origDustCoverGeo = DustCoverTrigger.DustCoverGeo;
            _origOpenRot = DustCoverTrigger.OpenRot;
            _origClosedRot = DustCoverTrigger.ClosedRot;

            _origEjectPos = FireArm.RoundPos_Ejection;
            _origEjectionSpeed = FireArm.EjectionSpeed;
            _origEjectionSpin = FireArm.EjectionSpin;
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            base.SimpleInteraction(hand);

            SwitchSides();
        }

        private void SwitchSides()
        {
            switch (_state)
            {
                case EState.Primary:
                    if (DustCoverTrigger.m_isOpen) StartCoroutine(ChangeToSecondary());
                    else ChangeToSecondaryInstant();
                    break;
                case EState.Secondary:
                    if (DustCoverTrigger.m_isOpen) StartCoroutine(ChangeToPrimary());
                    else ChangeToPrimaryInstant();
                    break;
                default:
                    break;
            }
        }

        private IEnumerator ChangeToPrimary()
        {
            DustCoverTrigger.Close();
            GetComponent<Collider>().enabled = false;
            DustCoverTrigger.GetComponent<Collider>().enabled = false;

            _state = EState.Changing;

            bool gotOpened = false;
            while (!gotOpened && Mathf.Abs(DustCoverTrigger.m_tarRot - DustCoverTrigger.m_curRot) > 0.01f)
            {
                yield return new WaitForEndOfFrame();

                if (DustCoverTrigger.m_isOpen) gotOpened = true;
            }

            if (!gotOpened)
            {
                transform.localPosition = _origPos;
                DustCoverTrigger.transform.localPosition = _origTriggerPos;

                DustCoverTrigger.DustCoverGeo = _origDustCoverGeo;
                DustCoverTrigger.OpenRot = _origOpenRot;
                DustCoverTrigger.ClosedRot = _origClosedRot;

                FireArm.RoundPos_Ejection = _origEjectPos;
                FireArm.EjectionSpeed = _origEjectionSpeed;
                FireArm.EjectionSpin = _origEjectionSpin;

                DustCoverTrigger.Open();

                _state = EState.Primary;
            }
            else _state = EState.Secondary;
            GetComponent<Collider>().enabled = true;
            DustCoverTrigger.GetComponent<Collider>().enabled = true;
        }

        private void ChangeToPrimaryInstant()
        {
            transform.localPosition = _origPos;
            DustCoverTrigger.transform.localPosition = _origTriggerPos;

            DustCoverTrigger.DustCoverGeo = _origDustCoverGeo;
            DustCoverTrigger.OpenRot = _origOpenRot;
            DustCoverTrigger.ClosedRot = _origClosedRot;

            FireArm.RoundPos_Ejection = _origEjectPos;
            FireArm.EjectionSpeed = _origEjectionSpeed;
            FireArm.EjectionSpin = _origEjectionSpin;

            DustCoverTrigger.Open();
            _state = EState.Primary;
        }

        private IEnumerator ChangeToSecondary()
        {
            DustCoverTrigger.Close();
            GetComponent<Collider>().enabled = false;
            DustCoverTrigger.GetComponent<Collider>().enabled = false;

            _state = EState.Changing;

            bool gotOpened = false;
            while (!gotOpened && Mathf.Abs(DustCoverTrigger.m_tarRot - DustCoverTrigger.m_curRot) > 0.01f)
            {
                yield return new WaitForEndOfFrame();

                if (DustCoverTrigger.m_isOpen) gotOpened = true;
            }

            if (!gotOpened)
            {
                transform.localPosition = _origTriggerPos;
                DustCoverTrigger.transform.localPosition = _origPos;

                DustCoverTrigger.DustCoverGeo = SecondaryDustCoverGeo;
                DustCoverTrigger.OpenRot = SecondaryOpenRot;
                DustCoverTrigger.ClosedRot = SecondaryClosedRot;

                FireArm.RoundPos_Ejection = SecondaryRoundPos_Ejection;
                FireArm.EjectionSpeed = SecondaryEjectionSpeed;
                FireArm.EjectionSpin = SecondaryEjectionSpin;

                DustCoverTrigger.Open();
                _state = EState.Secondary;
            }
            else _state = EState.Primary;
            GetComponent<Collider>().enabled = true;
            DustCoverTrigger.GetComponent<Collider>().enabled = true;
        }

        private void ChangeToSecondaryInstant()
        {
            transform.localPosition = _origTriggerPos;
            DustCoverTrigger.transform.localPosition = _origPos;

            DustCoverTrigger.DustCoverGeo = SecondaryDustCoverGeo;
            DustCoverTrigger.OpenRot = SecondaryOpenRot;
            DustCoverTrigger.ClosedRot = SecondaryClosedRot;

            FireArm.RoundPos_Ejection = SecondaryRoundPos_Ejection;
            FireArm.EjectionSpeed = SecondaryEjectionSpeed;
            FireArm.EjectionSpin = SecondaryEjectionSpin;

            DustCoverTrigger.Open();
            _state = EState.Secondary;
        }

#if !DEBUG
#endif
    }
}
