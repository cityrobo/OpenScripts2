using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class ManipulateSlidingPiece : OpenScripts2_BasePlugin
    {
        [Header("Outside Piece and Sliding piece must have same parent!")]
        [Header("Beware of tooltips!")]
        public Axis SlideAxis;
        [Tooltip("Piece that gets moved around by the Outside Piece limits.")]
        public Transform SlidingPiece;

        [Tooltip("Outside Piece Limits must be on the Outside Piece and empty! (they will get deleted!)")]
        public Transform OutsidePieceUpperLimit;
        [Tooltip("Outside Piece Limits must be on the Outside Piece and empty! (they will get deleted!)")]
        public Transform OutsidePieceLowerLimit;

        [Tooltip("Sliding Piece Limits must be on the Sliding Piece and empty! (they will get deleted!)")]
        public Transform SlidingPieceUpperLimit;
        [Tooltip("Sliding Piece Limits must be on the Sliding Piece and empty! (they will get deleted!)")]
        public Transform SlidingPieceLowerLimit;

        [Tooltip("If your parts keeps vibrating between both points, try using this checkbox.")]
        public bool InvertOutAndInside = false;

        private TransformProxy _outsidePieceUpperLimitProxy;
        private TransformProxy _slidingPieceUpperLimitProxy;

        private TransformProxy _outsidePieceLowerLimitProxy;
        private TransformProxy _slidingPieceLowerLimitProxy;

        private Transform _parent;

        public void Awake()
        {
            _parent = SlidingPiece.parent;

            _outsidePieceUpperLimitProxy = new(OutsidePieceUpperLimit, true);
            _slidingPieceUpperLimitProxy = new(SlidingPieceUpperLimit, true);

            _outsidePieceLowerLimitProxy = new(OutsidePieceLowerLimit, true);
            _slidingPieceLowerLimitProxy = new(SlidingPieceLowerLimit, true);
        }

        public void Update()
        {
            float relativeOutsidePositiveLimit = _parent.InverseTransformPoint(_outsidePieceUpperLimitProxy.position).GetAxisValue(SlideAxis);
            float relativeSlidingPositiveLimit = _parent.InverseTransformPoint(_slidingPieceUpperLimitProxy.position).GetAxisValue(SlideAxis);

            float relativeOutsideNegativeLimit = _parent.InverseTransformPoint(_outsidePieceLowerLimitProxy.position).GetAxisValue(SlideAxis);
            float relativeSlidingNegativeLimit = _parent.InverseTransformPoint(_slidingPieceLowerLimitProxy.position).GetAxisValue(SlideAxis);

            float positiveDelta = relativeOutsidePositiveLimit - relativeSlidingPositiveLimit;
            float negativeDelta = relativeOutsideNegativeLimit - relativeSlidingNegativeLimit;
            
            if (!InvertOutAndInside ? positiveDelta < 0f : positiveDelta > 0f)
            {
                float newPos = SlidingPiece.GetLocalPositionAxisValue(SlideAxis) + positiveDelta;

                SlidingPiece.ModifyLocalPositionAxisValue(SlideAxis, newPos);
            }
            else if (!InvertOutAndInside ? negativeDelta > 0f : negativeDelta < 0f)
            {
                float newPos = SlidingPiece.GetLocalPositionAxisValue(SlideAxis) + negativeDelta;

                SlidingPiece.ModifyLocalPositionAxisValue(SlideAxis, newPos);
            }
        }
    }
}