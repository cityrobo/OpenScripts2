using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class ManipulateTransforms : OpenScripts2_BasePlugin
    {
		public TransformModificationGroup[] TransformGroups;

        public void Awake()
        {
            foreach (TransformModificationGroup transformModificationRoot in TransformGroups)
            {
                transformModificationRoot.Initialize();
            }
        }
        public void Update()
        {
            foreach (TransformModificationGroup transformModificationRoot in TransformGroups)
            {
                transformModificationRoot.ManipulateTransforms();
            }
        }

        [Serializable]
        public class TransformModificationGroup
        {
            public Transform ObservedTransform;
            public TransformType ObservedTransformParameter;
            public Axis AxisToObserve;
            public float LowerObservationLimit;
            public float UpperObservationLimit;
            public TransformManipulationDefinition[] ManipulationDefinitions;

            private Quaternion _lastRot;
            private float _deltaRotFloat;
            private float _currentRotation;

            public void Initialize()
            {
                _lastRot = ObservedTransform.transform.localRotation;
            }

            public void ManipulateTransforms()
            {
                foreach (TransformManipulationDefinition transformManipulationDefinition in ManipulationDefinitions)
                {
                    transformManipulationDefinition.SetLerp(GetObservationLerp());
                }
            }

            private float GetObservationLerp()
            {
                switch (ObservedTransformParameter)
                {
                    case TransformType.Movement:
                        return Mathf.InverseLerp(LowerObservationLimit, UpperObservationLimit, ObservedTransform.GetLocalPositionAxisValue(AxisToObserve));
                    case TransformType.Rotation:
                        Quaternion deltaRot = ObservedTransform.localRotation * Quaternion.Inverse(_lastRot);
                        _deltaRotFloat = deltaRot.GetAxisValue(AxisToObserve);
                        if (_deltaRotFloat > 180) _deltaRotFloat -= 360f;
                        _currentRotation += _deltaRotFloat;
                        _lastRot = ObservedTransform.localRotation;
                        return Mathf.InverseLerp(LowerObservationLimit, UpperObservationLimit, _currentRotation);
                    case TransformType.Scale:
                        return Mathf.InverseLerp(LowerObservationLimit, UpperObservationLimit, ObservedTransform.GetLocalScaleAxisValue(AxisToObserve));
                }

                return 0f;
            }
        }
        [Serializable]
        public class TransformManipulationDefinition
        {
            public Transform ManipulatedTransform;
            public TransformType ManipulatedTransformParameter;
            public Axis AxisToAffect;
            public float LowerManipulationLimit;
            public float UpperManipulationLimit;

            private float _currentValue;
            public void SetLerp(float lerp)
            {
                _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, lerp);
                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
            }
        }
    }
}
