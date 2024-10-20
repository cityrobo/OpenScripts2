using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class ScopeBaseOptic : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachment Optic;

        public Transform FrontAreaFront;
        public Transform FrontAreaRear;
        public Transform RearAreaFront;
        public Transform RearAreaRear;

        private TransformProxy _frontAreaFrontProxy;
        private TransformProxy _frontAreaRearProxy;
        private TransformProxy _rearAreaFrontProxy;
        private TransformProxy _rearAreaRearProxy;

        private Vector2 _oldLowerLimit;
        private Vector2 _oldUpperLimit;

        private MovingFireArmAttachmentInterface _interface;

        public void Awake()
        {
            _frontAreaFrontProxy = new TransformProxy(FrontAreaFront, true);
            _frontAreaRearProxy = new TransformProxy(FrontAreaRear, true);

            _rearAreaFrontProxy = new TransformProxy(RearAreaFront, true);
            _rearAreaRearProxy = new TransformProxy(RearAreaRear, true);
        }

        public void Update()
        {
            if (_interface == null && Optic.curMount != null && Optic.curMount is ScopeBaseMount scopeBase && Optic.curMount.MyObject is FVRFireArmAttachment curAttachment)
            {
                _interface = curAttachment.AttachmentInterface as MovingFireArmAttachmentInterface;

                if (_interface != null)
                {
                    _oldLowerLimit = _interface.LowerLimit;
                    _oldUpperLimit = _interface.UpperLimit;

                    Vector2 NewXLimits = Vector2.zero;
                    Vector2 NewYLimits = Vector2.zero;
                    Vector2 NewZLimits = Vector2.zero;

                    float maximumForward;
                    float maximumRearward;

                    // Transforming points into local coordinate system of the interface's parent
                    float FrontRingFront = _interface.transform.parent.InverseTransformPoint(scopeBase.FrontRingFrontProxy.position).z;
                    float FrontRingRear = _interface.transform.parent.InverseTransformPoint(scopeBase.FrontRingRearProxy.position).z;

                    float RearRingFront = _interface.transform.parent.InverseTransformPoint(scopeBase.RearRingFrontProxy.position).z;
                    float RearRingRear = _interface.transform.parent.InverseTransformPoint(scopeBase.RearRingRearProxy.position).z;

                    float FrontAreaFront = _interface.transform.parent.InverseTransformPoint(_frontAreaFrontProxy.position).z;
                    float FrontAreaRear = _interface.transform.parent.InverseTransformPoint(_frontAreaRearProxy.position).z;

                    float RearAreaFront = _interface.transform.parent.InverseTransformPoint(_rearAreaFrontProxy.position).z;
                    float RearAreaRear = _interface.transform.parent.InverseTransformPoint(_rearAreaRearProxy.position).z;

                    // Scope attached front to back
                    if (Vector3.Distance(scopeBase.FrontRingFrontProxy.position, _frontAreaFrontProxy.position) < Vector3.Distance(scopeBase.RearRingRearProxy.position, _frontAreaFrontProxy.position))
                    {
                        float distanceFrontFrontToFrontFront = FrontRingFront - FrontAreaFront;
                        float distanceRearFrontToRearFront = RearRingFront - RearAreaFront;

                        //maximumRearward = distanceFrontFrontToFrontFront > distanceRearFrontToRearFront ? distanceFrontFrontToFrontFront : distanceRearFrontToRearFront;
                        maximumRearward = Mathf.Max(distanceFrontFrontToFrontFront, distanceRearFrontToRearFront);

                        float distanceFrontRearToFrontRear = FrontRingRear - FrontAreaRear;
                        float distanceRearRearToReaRear = RearRingRear - RearAreaRear;

                        //maximumForward = distanceFrontRearToFrontRear < distanceRearRearToReaRear ? distanceFrontRearToFrontRear : distanceRearRearToReaRear;
                        maximumForward = Mathf.Min(distanceFrontRearToFrontRear, distanceRearRearToReaRear);
                    }
                    // Scope attached back to front
                    else
                    {
                        float distanceRearRearToFrontFront = FrontRingFront - RearAreaRear;
                        float distanceFrontRearToRearFront = RearRingFront - FrontAreaRear;

                        //maximumRearward = distanceFrontFrontToFrontFront > distanceRearFrontToRearFront ? distanceFrontFrontToFrontFront : distanceRearFrontToRearFront;
                        maximumRearward = Mathf.Max(distanceRearRearToFrontFront, distanceFrontRearToRearFront);

                        float distanceRearFrontToFrontRear = FrontRingRear - RearAreaFront;
                        float distanceFrontFrontToRearRear = RearRingRear - FrontAreaFront;

                        //maximumForward = distanceFrontRearToFrontRear < distanceRearRearToReaRear ? distanceFrontRearToFrontRear : distanceRearRearToReaRear;
                        maximumForward = Mathf.Min(distanceRearFrontToFrontRear, distanceFrontFrontToRearRear);
                    }

                    NewZLimits.x = maximumRearward;
                    NewZLimits.y = maximumForward;

                    _interface.LowerLimit = new Vector3(NewXLimits.x, NewYLimits.x, NewZLimits.x);
                    _interface.UpperLimit = new Vector3(NewXLimits.y, NewYLimits.y, NewZLimits.y);

                    // Make sure scope is position in the middle of the limits after attaching
                    _interface.transform.ModifyLocalPositionAxisValue(Axis.Z, (NewZLimits.y - NewZLimits.x) / 2f + NewZLimits.x);
                }
            }
            else if (_interface != null && Optic.curMount == null)
            {
                _interface.LowerLimit = _oldLowerLimit;
                _interface.UpperLimit = _oldUpperLimit;

                _interface = null;
            }
        }
    }
}
