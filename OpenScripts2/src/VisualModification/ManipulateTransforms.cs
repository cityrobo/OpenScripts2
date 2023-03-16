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

            public enum EOneWayBehavior
            {
                None,
                ForwardReturningAtStart,
                ForwardReturningAtEnd,
                BackwardReturningAtStart,
                BackwardReturningAtEnd
            }

            public EOneWayBehavior OneWayBehavior;

            [Tooltip("How fast the transform will return to its original position in seconds.")]
            public float ReturnDuration = 1f;

            private float _currentValue;
            private bool _oneWayLockEnabled = false;

            private float _delayedSnappingLerp = -1f;

            public void SetLerp(float lerp)
            {
                switch (OneWayBehavior)
                {
                    case EOneWayBehavior.None:
                        _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, lerp);
                        ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                        break;
                    case EOneWayBehavior.ForwardReturningAtStart:
                        if (!_oneWayLockEnabled)
                        {
                            if (_delayedSnappingLerp > 0f) _delayedSnappingLerp = ReturnDuration > 0f ? Mathf.Clamp01(_delayedSnappingLerp - Time.deltaTime / ReturnDuration) : 0f;
                            else _delayedSnappingLerp = -1f;

                            if (lerp < 1f)
                            {
                                _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, _delayedSnappingLerp != -1f ? Mathf.Clamp(_delayedSnappingLerp, lerp, 1f) : lerp);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                            }
                            else
                            {
                                _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, 1f);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                                _oneWayLockEnabled = true;
                            }
                        }
                        else if (_oneWayLockEnabled && lerp <= 0f)
                        {
                            _oneWayLockEnabled = false;
                            _delayedSnappingLerp = 1f;
                        }
                        break;
                    case EOneWayBehavior.ForwardReturningAtEnd:
                        if (!_oneWayLockEnabled)
                        {
                            if (lerp < 1f)
                            {
                                _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, lerp);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                            }
                            else
                            {
                                _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, 1f);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                                _oneWayLockEnabled = true;
                                _delayedSnappingLerp = 1f;

                            }
                        }
                        else
                        {
                            _delayedSnappingLerp = ReturnDuration > 0f ? Mathf.Clamp01(_delayedSnappingLerp - Time.deltaTime / ReturnDuration) : 0f;
                            _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, Mathf.Clamp(_delayedSnappingLerp, 0f, lerp));
                            ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                            if (lerp <= 0f) _oneWayLockEnabled = false;
                        }
                        break;
                    case EOneWayBehavior.BackwardReturningAtStart:
                        if (!_oneWayLockEnabled)
                        {
                            if (_delayedSnappingLerp < 1f && _delayedSnappingLerp != -1f) _delayedSnappingLerp = ReturnDuration > 0f ? Mathf.Clamp01(_delayedSnappingLerp + Time.deltaTime / ReturnDuration) : 0f;
                            else _delayedSnappingLerp = -1f;

                            if (lerp > 0f)
                            {
                                _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, _delayedSnappingLerp != -1f ? Mathf.Clamp(_delayedSnappingLerp, 0f, lerp) : lerp);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                            }
                            else
                            {
                                _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, 0f);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                                _oneWayLockEnabled = true;
                            }
                        }
                        else if (_oneWayLockEnabled && lerp >= 1f)
                        {
                            _oneWayLockEnabled = false;
                            _delayedSnappingLerp = 0f;
                        }

                        break;
                    case EOneWayBehavior.BackwardReturningAtEnd:
                        if (!_oneWayLockEnabled)
                        {
                            if (lerp > 0f)
                            {
                                _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, lerp);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                            }
                            else
                            {
                                _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, 0f);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                                _oneWayLockEnabled = true;
                                _delayedSnappingLerp = 0f;
                            }
                        }
                        else
                        {
                            _delayedSnappingLerp = ReturnDuration > 0f ? Mathf.Clamp01(_delayedSnappingLerp + Time.deltaTime / ReturnDuration) : 1f;
                            _currentValue = Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, Mathf.Clamp(_delayedSnappingLerp, lerp, 1f));
                            ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);

                            if (lerp >= 1f) _oneWayLockEnabled = false;
                        }
                        break;
                }
            }
        }
        [Serializable]
        public class TransformObservationDefinition
        {
            public Transform ObservedTransform;
            public TransformType ObservedTransformParameter;
            public Axis AxisToObserve;
            public float LowerObservationLimit;
            public float UpperObservationLimit;

            private Quaternion _lastRot;
            private float _deltaRotFloat;
            private float _currentRotation;

            public void Initialize()
            {
                _lastRot = ObservedTransform.transform.localRotation;
            }

            public float GetObservationLerp()
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

    }
}
