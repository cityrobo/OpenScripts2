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
                if (OneWayBehavior == EOneWayBehavior.None && lerp == _lastLerp) return;
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

#if DEBUG
    [UnityEditor.CustomEditor(typeof(ManipulateTransforms))]
    public class ManipulateTransformsEditor : UnityEditor.Editor
    {
        private static GUIStyle _boldLabel;

        private static float _removeBtnWidth = 20f;

        public override void OnInspectorGUI()
        {
            ManipulateTransforms m = (target as ManipulateTransforms);
            UnityEditor.EditorGUI.BeginChangeCheck();

            GUILayout.Space(8f);

            ManipulateTransforms.TransformModificationGroup[] transformGroups = m.TransformGroups;
            List<int> toRemove = new List<int>();

            _boldLabel = new GUIStyle(GUI.skin.label);
            _boldLabel.fontStyle = FontStyle.Bold;
            _boldLabel.contentOffset = new Vector2(0f, 2f);

            if (transformGroups == null || transformGroups.Length <= 0)
            {
                transformGroups = new ManipulateTransforms.TransformModificationGroup[] { new ManipulateTransforms.TransformModificationGroup() };
            }

            for (int i = 0; i < transformGroups.Length; i++)
            {
                UnityEditor.EditorGUILayout.BeginHorizontal();
                if (transformGroups.Length > 1)
                {
                    if (GUILayout.Button(new GUIContent("-"), GUILayout.Width(_removeBtnWidth))) { toRemove.Add(i); }
                }
                else
                {
                    GUILayout.Space(24f);
                    Rect r = GUILayoutUtility.GetLastRect();
                    UnityEditor.EditorGUI.DrawRect(new Rect(r.position.x + 1f, r.position.y + (UnityEditor.EditorGUIUtility.singleLineHeight * 0.5f) + 4f, 16f, 2f), new Color(0.5f, 0.5f, 0.5f));
                }

                UnityEditor.EditorGUILayout.LabelField("Transform Group " + (i + 1).ToString(), _boldLabel);
                UnityEditor.EditorGUILayout.EndHorizontal();

                if (transformGroups.Length <= 1) { GUILayout.Space(2f); }

                DrawTransformModificationGroup(target, ref transformGroups[i]);

                GUILayout.Space(8f);
            }

            if (toRemove.Count > 0 && transformGroups.Length > 1)
            {
                var removedArray = new ManipulateTransforms.TransformModificationGroup[transformGroups.Length - toRemove.Count];
                int index = 0;
                for (int j = 0; j < transformGroups.Length; j++)
                {
                    if (!toRemove.Contains(j))
                    {
                        removedArray[index] = transformGroups[j];
                        index++;
                    }
                }

                transformGroups = removedArray;
            }

            if (GUILayout.Button(new GUIContent("+")))
            {
                var _array = new ManipulateTransforms.TransformModificationGroup[m.TransformGroups.Length + 1];
                for (int k = 0; k < transformGroups.Length; k++) { _array[k] = transformGroups[k]; }
                _array[_array.Length - 1] = new ManipulateTransforms.TransformModificationGroup();
                transformGroups = _array;
            }

            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(target, "Manipulate Transforms edited");
                m.TransformGroups = transformGroups;
            }
        }

        public void OnInspectorUpdate()
        {
            Repaint();
        }

        public void DrawTransformModificationGroup(UnityEngine.Object target, ref ManipulateTransforms.TransformModificationGroup group)
        {
            UnityEditor.EditorGUI.BeginChangeCheck();

            UnityEditor.EditorGUI.indentLevel = 1;
            Rect firstRect = GUILayoutUtility.GetLastRect();

            var ObservedTransform = (Transform)UnityEditor.EditorGUILayout.ObjectField(new GUIContent("Observed Transform"), group.ObservedTransform, typeof(Transform), true);
            var ObservedTransformParameter = (OpenScripts2_BasePlugin.TransformType)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Observed Transform Parameter"), group.ObservedTransformParameter);
            var AxisToObserve = (OpenScripts2_BasePlugin.Axis)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Axis To Observe"), group.AxisToObserve);
            var ObservationLimits = UnityEditor.EditorGUILayout.Vector2Field(new GUIContent("Min/Max Observation Limit"), new Vector2(group.LowerObservationLimit, group.UpperObservationLimit));

            var ManipulationDefinitions = group.ManipulationDefinitions;
            if (ManipulationDefinitions == null || ManipulationDefinitions.Length <= 0)
            {
                ManipulationDefinitions = new ManipulateTransforms.TransformManipulationDefinition[] { new ManipulateTransforms.TransformManipulationDefinition() };
            }

            var definitionLabel = new GUIStyle(GUI.skin.label);
            definitionLabel.fontStyle = FontStyle.Bold;
            definitionLabel.contentOffset = new Vector2(-16f, 2f);

            List<int> toRemove = new List<int>();
            GUILayout.Space(4f);
            //EditorGUILayout.LabelField (new GUIContent ("Manipulation Definitions"), boldLabel);
            for (int i = 0; i < ManipulationDefinitions.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(16f);
                if (ManipulationDefinitions.Length > 1)
                {
                    if (GUILayout.Button(new GUIContent("-"), GUILayout.Width(_removeBtnWidth))) { toRemove.Add(i); }
                }
                else
                {
                    GUILayout.Space(24f);
                    Rect r = GUILayoutUtility.GetLastRect();
                    UnityEditor.EditorGUI.DrawRect(new Rect(r.position.x + 1f, r.position.y + (UnityEditor.EditorGUIUtility.singleLineHeight * 0.5f) + 4f, 16f, 2f), new Color(0.5f, 0.5f, 0.5f));
                }
                UnityEditor.EditorGUILayout.LabelField("Manipulation Definition " + (i + 1).ToString(), definitionLabel);
                GUILayout.EndHorizontal();

                if (ManipulationDefinitions.Length <= 1) { GUILayout.Space(2f); }

                UnityEditor.EditorGUI.indentLevel = 2;
                DrawTransformManipulationDefinition(target, ref ManipulationDefinitions[i]);
                UnityEditor.EditorGUI.indentLevel = 1;

                if (i < ManipulationDefinitions.Length - 1) { GUILayout.Space(4f); }
            }

            if (toRemove.Count > 0 && ManipulationDefinitions.Length > 1)
            {
                var removedArray = new ManipulateTransforms.TransformManipulationDefinition[ManipulationDefinitions.Length - toRemove.Count];
                int index = 0;
                for (int j = 0; j < ManipulationDefinitions.Length; j++)
                {
                    if (!toRemove.Contains(j))
                    {
                        removedArray[index] = ManipulationDefinitions[j];
                        index++;
                    }
                }

                ManipulationDefinitions = removedArray;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            if (GUILayout.Button(new GUIContent("+"), GUILayout.Width(96f)))
            {
                var _array = new ManipulateTransforms.TransformManipulationDefinition[ManipulationDefinitions.Length + 1];
                for (int j = 0; j < ManipulationDefinitions.Length; j++) { _array[j] = ManipulationDefinitions[j]; }
                _array[_array.Length - 1] = new ManipulateTransforms.TransformManipulationDefinition();
                ManipulationDefinitions = _array;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            string StartingAngleTooltip = "Starting value needed for correct rotation tracking. This is the angle that the transform on that axis starts out on.";
            UnityEditor.EditorGUILayout.LabelField(new GUIContent("Required for rotation: ", StartingAngleTooltip), _boldLabel);
            float StartingAngle = UnityEditor.EditorGUILayout.FloatField(new GUIContent("Starting Angle", StartingAngleTooltip), group.StartingAngle);

            Rect lastRect = GUILayoutUtility.GetLastRect();
            UnityEditor.EditorGUI.DrawRect(new Rect(lastRect.position.x + 8f, firstRect.position.y + firstRect.size.y + 4f, 2f, (lastRect.position.y + lastRect.size.y) - (firstRect.position.y + firstRect.size.y) - 6f), new Color(0.5f, 0.5f, 0.5f));
            UnityEditor.EditorGUI.indentLevel = 0;

            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(target, "Manipulate Transforms group edited");

                group.ObservedTransform = ObservedTransform;
                group.ObservedTransformParameter = ObservedTransformParameter;
                group.AxisToObserve = AxisToObserve;
                group.LowerObservationLimit = ObservationLimits.x;
                group.UpperObservationLimit = ObservationLimits.y;
                group.ManipulationDefinitions = ManipulationDefinitions;
                group.StartingAngle = StartingAngle;
            }
        }

        public void DrawTransformManipulationDefinition(UnityEngine.Object target, ref ManipulateTransforms.TransformManipulationDefinition manipDef)
        {
            UnityEditor.EditorGUI.BeginChangeCheck();
            Rect firstRect = GUILayoutUtility.GetLastRect();

            var ManipulatedTransform = (Transform)UnityEditor.EditorGUILayout.ObjectField(new GUIContent("Manipulated Transform"), manipDef.ManipulatedTransform, typeof(Transform), true);
            var ManipulatedTransformParameter = (OpenScripts2_BasePlugin.TransformType)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Manipulated Transform Parameter"), manipDef.ManipulatedTransformParameter);
            var AxisToAffect = (OpenScripts2_BasePlugin.Axis)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Axis To Affect"), manipDef.AxisToAffect);
            var ManipulationLimits = UnityEditor.EditorGUILayout.Vector2Field(new GUIContent("Min/Max Manipulation Limits"), new Vector2(manipDef.LowerManipulationLimit, manipDef.UpperManipulationLimit));

            GUILayout.Space(4f);
            var UsesAnimationCurves = UnityEditor.EditorGUILayout.Toggle(new GUIContent("Uses Animation Curves?"), manipDef.UsesAnimationCurves);
            var ForwardCurve = UnityEditor.EditorGUILayout.CurveField(new GUIContent("Forward Curve", "This curve is also used for both ways if one-way behavior is set to \"None.\""), manipDef.ForwardCurve);
            var BackwardCurve = UnityEditor.EditorGUILayout.CurveField(new GUIContent("Backward Curve"), manipDef.BackwardCurve);

            GUILayout.Space(4f);
            var OneWayBehavior = (ManipulateTransforms.TransformManipulationDefinition.EOneWayBehavior)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("One Way Behavior"), manipDef.OneWayBehavior);
            var ReturnDuration = UnityEditor.EditorGUILayout.FloatField(new GUIContent("Return Duration", "How fast the transform will return to its original position, in seconds."), manipDef.ReturnDuration);

            Rect lastRect = GUILayoutUtility.GetLastRect();
            UnityEditor.EditorGUI.DrawRect(new Rect(lastRect.position.x + 24f, firstRect.position.y + firstRect.size.y + 4f, 2f, (lastRect.position.y + lastRect.size.y) - (firstRect.position.y + firstRect.size.y) - 6f), new Color(0.5f, 0.5f, 0.5f, 0.75f));
            UnityEditor.EditorGUI.indentLevel = 0;

            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(target, "Manipulation Definition edited");

                manipDef.ManipulatedTransform = ManipulatedTransform;
                manipDef.ManipulatedTransformParameter = ManipulatedTransformParameter;
                manipDef.AxisToAffect = AxisToAffect;
                manipDef.LowerManipulationLimit = ManipulationLimits.x;
                manipDef.UpperManipulationLimit = ManipulationLimits.y;
                manipDef.UsesAnimationCurves = UsesAnimationCurves;
                manipDef.ForwardCurve = ForwardCurve;
                manipDef.BackwardCurve = BackwardCurve;
                manipDef.OneWayBehavior = OneWayBehavior;
                manipDef.ReturnDuration = ReturnDuration;
            }
        }
    }
#endif
}
