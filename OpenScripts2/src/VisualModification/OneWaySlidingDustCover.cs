using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class OneWaySlidingDustCover : FVRInteractiveObject
    {
        [Header("One Way Sliding Dust Cover config")]
        [Header("All the points but the dust cover itself WILL GET DELETED!")]
        [Header("Beware of tooltips!")]
        [Tooltip("Dust cover object, duh!.")]
        public Transform DustCover;
        [Tooltip("Start position the dust cover will return to if interacted with.\n must be on the dustcover's parent and empty! (Will get deleted!)")]
        public Transform DustCoverStartingPosition;

        [Tooltip("Bolt contact point must be on the bolt and empty! (they will get deleted!)")]
        public Transform BoltContactPoint;

        [Tooltip("Dust cover contact point must be on the Sliding Piece and empty! (they will get deleted!)")]
        public Transform DustCoverContactPoint;

        [Tooltip("Speed in m/s with which the dustcover returns to its starting position when interacted with.")]
        public float DustCoverResetSpeed = 0.5f;

        private TransformProxy _dustCoverStartingPositionProxy;

        private TransformProxy _boltContactPointProxy;
        private TransformProxy _dustCoverContactPointProxy;

        private Transform _parent;

        public override void Awake()
        {
            base.Awake();
            _parent = DustCover.parent;

            _dustCoverStartingPositionProxy = new(DustCoverStartingPosition, true);

            _boltContactPointProxy = new(BoltContactPoint, true);
            _dustCoverContactPointProxy = new(DustCoverContactPoint, true);



            IsSimpleInteract = true;
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            float relativeBoltContactPoint = _parent.InverseTransformPoint(_boltContactPointProxy.position).z;
            float relativeDustCoverContactPoint = _parent.InverseTransformPoint(_dustCoverContactPointProxy.position).z;

            float deltaPosition = relativeBoltContactPoint - relativeDustCoverContactPoint;
            
            if (deltaPosition < 0f)
            {
                float newPositionValue = DustCover.localPosition.z + deltaPosition;

                DustCover.ModifyLocalPositionAxisValue(OpenScripts2_BasePlugin.Axis.Z, newPositionValue);
            }
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            base.SimpleInteraction(hand);

            StopAllCoroutines();
            StartCoroutine(ResetDustcover());
        }

        private IEnumerator ResetDustcover()
        {
            while (DustCover.localPosition.NotEqual(_dustCoverStartingPositionProxy.localPosition))
            {
                float currentValue = DustCover.localPosition.z;
                float endValue = _dustCoverStartingPositionProxy.localPosition.z;

                float newValue = Mathf.MoveTowards(currentValue, endValue, DustCoverResetSpeed * Time.deltaTime);

                DustCover.ModifyLocalPositionAxisValue(OpenScripts2_BasePlugin.Axis.Z, newValue);
                yield return null;
            }
        }
    }
}