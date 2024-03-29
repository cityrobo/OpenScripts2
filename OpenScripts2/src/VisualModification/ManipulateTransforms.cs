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
            [Header("Required for rotation:")]
            [Tooltip("Starting value needed for correct rotation tracking. This is the angle that the transform on that axis starts out on.")]
            public float StartingAngle;

            private Quaternion _lastRot;
            private float _deltaRotFloat;
            private float _currentRotation;

            public void Initialize()
            {
                _currentRotation = StartingAngle;
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

            public bool UsesAnimationCurves;
            [Tooltip("This curve is also used for in both ways if one way behavior is set to \"None\".")]
            public AnimationCurve ForwardCurve = new();
            public AnimationCurve BackwardCurve = new();

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
            private float _lastLerp;
            private bool _oneWayLockEnabled = false;

            private float _delayedSnappingLerp = -1f;

            public void SetLerp(float lerp)
            {
                if (lerp == _lastLerp) return;
                switch (OneWayBehavior)
                {
                    case EOneWayBehavior.None:
                        _currentValue = !UsesAnimationCurves
                            ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, lerp)
                            : ForwardCurve.Evaluate(lerp);
                        ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                        break;
                    case EOneWayBehavior.ForwardReturningAtStart:
                        if (!_oneWayLockEnabled)
                        {
                            if (_delayedSnappingLerp > 0f) _delayedSnappingLerp = ReturnDuration > 0f ? Mathf.Clamp01(_delayedSnappingLerp - Time.deltaTime / ReturnDuration) : 0f;
                            else _delayedSnappingLerp = -1f;

                            if (lerp < 1f)
                            {
                                _currentValue = !UsesAnimationCurves
                                    ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, _delayedSnappingLerp != -1f ? Mathf.Clamp(_delayedSnappingLerp, lerp, 1f) : lerp)
                                    : _delayedSnappingLerp != -1f ? ForwardCurve.Evaluate(Mathf.Clamp(_delayedSnappingLerp, lerp, 1f)) : ForwardCurve.Evaluate(lerp);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                            }
                            else
                            {
                                _currentValue = !UsesAnimationCurves
                                    ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, 1f)
                                    : ForwardCurve.Evaluate(1f);
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
                                
                                _currentValue = !UsesAnimationCurves
                                    ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, lerp)
                                    : ForwardCurve.Evaluate(lerp);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                            }
                            else
                            {
                                _currentValue = !UsesAnimationCurves 
                                    ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, 1f) 
                                    : ForwardCurve.Evaluate(1f);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                                _oneWayLockEnabled = true;
                                _delayedSnappingLerp = 1f;
                            }
                        }
                        else
                        {
                            _delayedSnappingLerp = ReturnDuration > 0f ? Mathf.Clamp01(_delayedSnappingLerp - Time.deltaTime / ReturnDuration) : 0f;
                            _currentValue = !UsesAnimationCurves
                                ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, Mathf.Clamp(_delayedSnappingLerp, 0f, lerp))
                                : ForwardCurve.Evaluate(Mathf.Clamp(_delayedSnappingLerp, 0f, lerp));
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
                                _currentValue = !UsesAnimationCurves
                                    ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, _delayedSnappingLerp != -1f ? Mathf.Clamp(_delayedSnappingLerp, 0f, lerp) : lerp)
                                    : _delayedSnappingLerp != -1f ? BackwardCurve.Evaluate(Mathf.Clamp(_delayedSnappingLerp, 0f, lerp)) : BackwardCurve.Evaluate(lerp);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                            }
                            else
                            {
                                _currentValue = !UsesAnimationCurves
                                    ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, 0f)
                                    : BackwardCurve.Evaluate(0f);
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
                                _currentValue = !UsesAnimationCurves
                                    ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, lerp)
                                    : BackwardCurve.Evaluate(lerp);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                            }
                            else
                            {
                                _currentValue = !UsesAnimationCurves
                                    ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, 0f)
                                    : BackwardCurve.Evaluate(0f);
                                ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);
                                _oneWayLockEnabled = true;
                                _delayedSnappingLerp = 0f;
                            }
                        }
                        else
                        {
                            _delayedSnappingLerp = ReturnDuration > 0f ? Mathf.Clamp01(_delayedSnappingLerp + Time.deltaTime / ReturnDuration) : 1f;
                            _currentValue = !UsesAnimationCurves
                                ? Mathf.Lerp(LowerManipulationLimit, UpperManipulationLimit, Mathf.Clamp(_delayedSnappingLerp, lerp, 1f))
                                : BackwardCurve.Evaluate(Mathf.Clamp(_delayedSnappingLerp, lerp, 1f));
                            ManipulatedTransform.ModifyLocalTransform(ManipulatedTransformParameter, AxisToAffect, _currentValue);

                            if (lerp >= 1f) _oneWayLockEnabled = false;
                        }
                        break;
                }

                _lastLerp = lerp;
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
