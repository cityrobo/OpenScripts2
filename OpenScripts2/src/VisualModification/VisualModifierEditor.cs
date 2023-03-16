#if DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FistVR;

namespace OpenScripts2
{
    [CustomEditor(typeof(VisualModifier))]
    public class VisualModifierEditor : Editor
    {
        private VisualModifier f;
        private bool _emissionFoldOut;
        private bool _detailFoldOut;
        private bool _particleFoldOut;
        private bool _soundFoldOut;
        private bool _animatorFoldOut;
        private bool _movementFoldOut;
        private bool _debugFoldOut;

        private bool _invertX;
        private bool _invertY;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            f = target as VisualModifier;
            GUIStyle bold = new GUIStyle(EditorStyles.boldLabel);
            bold.fontStyle = FontStyle.Bold;
            bold.fontSize = 15;
            bold.fixedHeight = 30;

            GUIStyle foldout = new GUIStyle(EditorStyles.foldout);
            foldout.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField("VisualModifier", bold);
            HorizontalLine(Color.gray);

            _emissionFoldOut = EditorGUILayout.Foldout(_emissionFoldOut, "Emission Modification", foldout);
            if (_emissionFoldOut)
            {
                f.MeshRenderer = (MeshRenderer)EditorGUILayout.ObjectField(new GUIContent("Mesh Renderer", "Affected Mesh Renderer."), f.MeshRenderer, typeof(MeshRenderer), true);
                if (f.MeshRenderer != null)
                {
                    f.MaterialIndex = EditorGUILayout.IntField(new GUIContent("Material Index", "Index of the material in the MeshRenderer's materials list."), f.MaterialIndex);
                    f.EmissionExponent = EditorGUILayout.FloatField(new GUIContent("Emission Exponent", "Anton uses the squared value of heat to determine the emission weight. If you wanna replicate that behavior, leave the value as is, but feel free to go crazy if you wanna mix things up."), f.EmissionExponent);
                    f.EmissionStartsAt = EditorGUILayout.FloatField(new GUIContent("Emission Starts At", "InputValue level at which emission starts appearing."), f.EmissionStartsAt);
                    f.AffectsEmissionWeight = EditorGUILayout.Toggle("Does VisualModifier affect Emission Weight?", f.AffectsEmissionWeight);
                    f.AffectsEmissionScrollSpeed = EditorGUILayout.Toggle("Does VisualModifier affect Emission Scroll Speed?", f.AffectsEmissionScrollSpeed);
                    if (f.AffectsEmissionScrollSpeed)
                    {
                        f.MaxEmissionScrollSpeed_X = EditorGUILayout.FloatField(new GUIContent("Max Emission Scroll Speed X", "Maximum left/right emission scroll speed."), f.MaxEmissionScrollSpeed_X);
                        f.MaxEmissionScrollSpeed_Y = EditorGUILayout.FloatField(new GUIContent("Max Emission Scroll Speed Y", "Maximum up/down emission scroll speed."), f.MaxEmissionScrollSpeed_Y);
                    }

                    f.EmissionUsesAdvancedCurve = EditorGUILayout.Toggle(new GUIContent("Does Emission use Advanced Curve?", "Enables emission weight AnimationCurve system."), f.EmissionUsesAdvancedCurve);
                    if (f.EmissionUsesAdvancedCurve)
                    {
                        f.EmissionCurve = EditorGUILayout.CurveField(new GUIContent("Emission Curve", "Advanced emission weight control. Values from 0 to 1 only!"), f.EmissionCurve);
                        _invertX = EditorGUILayout.Toggle(new GUIContent("Invert Curve X Axis", "Should the X Axis of the Curve be inverted on calculation? "),_invertX);
                        _invertY = EditorGUILayout.Toggle(new GUIContent("Invert Curve Y Axis", "Should the Y Axis of the Curve be inverted on calculation? "), _invertY);
                        if (GUILayout.Button(new GUIContent("Calculate Emission Curve", "Calculates Curve with set Exponent, start value and inversion modifiers"))) f.EmissionCurve = CurveCalculator.GetCurve(f.EmissionExponent, f.EmissionStartsAt, _invertX, _invertY);
                        if (f.AffectsEmissionScrollSpeed)
                        {
                            f.EmissionScrollSpeedCurve_X = EditorGUILayout.CurveField(new GUIContent("Emission Scroll Speed Curve X", "Advanced left/right emission scroll speed control. The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-Axis) represents the actual scroll speed and is uncapped. The max value set above is ignored."), f.EmissionScrollSpeedCurve_X);                            
                            f.EmissionScrollSpeedCurve_Y = EditorGUILayout.CurveField(new GUIContent("Emission Scroll Speed Curve Y", "Advanced up/down emission scroll speed control. The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-Axis) represents the actual scroll speed and is uncapped. The max value set above is ignored."), f.EmissionScrollSpeedCurve_Y);
                            _invertX = EditorGUILayout.Toggle(new GUIContent("Invert Curve X Axis", "Should the X Axis of the Curve be inverted on calculation? "), _invertX);
                            _invertY = EditorGUILayout.Toggle(new GUIContent("Invert Curve Y Axis", "Should the Y Axis of the Curve be inverted on calculation? "), _invertY);
                            if (GUILayout.Button(new GUIContent("Calculate Scroll Curve X", "Calculates Curve with set Exponent, start value and inversion modifiers"))) f.EmissionScrollSpeedCurve_X = CurveCalculator.GetCurve(f.EmissionExponent, f.EmissionStartsAt, _invertX, _invertY);
                            if (GUILayout.Button(new GUIContent("Calculate Scroll Curve Y", "Calculates Curve with set Exponent, start value and inversion modifiers"))) f.EmissionScrollSpeedCurve_Y = CurveCalculator.GetCurve(f.EmissionExponent, f.EmissionStartsAt, _invertX, _invertY);
                        }
                    }
                }
            }
            _detailFoldOut = EditorGUILayout.Foldout(_detailFoldOut, "Detail Weight", foldout);
            if (_detailFoldOut)
            {
                f.AffectsDetailWeight = EditorGUILayout.Toggle("Does VisualModifier affects Detail Weight?", f.AffectsDetailWeight);
                if (f.AffectsDetailWeight)
                {
                    f.DetailExponent = EditorGUILayout.FloatField(new GUIContent("Detail Exponent", "Same as the Emission Exponent, but for the detail weight."), f.DetailExponent);
                    f.DetailStartsAt = EditorGUILayout.FloatField(new GUIContent("Detail Starts At", "InputValue level at which detail starts appearing."), f.DetailStartsAt);
                    f.DetailUsesAdvancedCurve = EditorGUILayout.Toggle(new GUIContent("Does Detail use Advanced Curve?", "Enables emission weight AnimationCurve system."), f.DetailUsesAdvancedCurve);
                    if (f.DetailUsesAdvancedCurve)
                    {
                        f.DetailCurve = EditorGUILayout.CurveField(new GUIContent("Detail Curve", "Advanced emission weight control. Values from 0 to 1 only!"), f.DetailCurve);
                        _invertX = EditorGUILayout.Toggle(new GUIContent("Invert Curve X Axis", "Should the X Axis of the Curve be inverted on calculation? "), _invertX);
                        _invertY = EditorGUILayout.Toggle(new GUIContent("Invert Curve Y Axis", "Should the Y Axis of the Curve be inverted on calculation? "), _invertY);
                        if (GUILayout.Button(new GUIContent("Calculate Detail Curve X", "Calculates Curve with set Exponent, start value and inversion modifiers"))) f.DetailCurve = CurveCalculator.GetCurve(f.DetailExponent, f.DetailStartsAt, _invertX, _invertY);
                    }
                }
            }
            _particleFoldOut = EditorGUILayout.Foldout(_particleFoldOut, "Particle Emission", foldout);
            if (_particleFoldOut)
            {
                f.ParticleSystem = (ParticleSystem)EditorGUILayout.ObjectField(new GUIContent("Particle System"), f.ParticleSystem, typeof(ParticleSystem), true);
                if (f.ParticleSystem != null)
                {
                    f.MaxEmissionRate = EditorGUILayout.FloatField(new GUIContent("Max Emission Rate"), f.MaxEmissionRate);
                    f.ParticleExponent = EditorGUILayout.FloatField(new GUIContent("Particles Exponent", "Same as the Emission Exponent, but for the particle emission rate."), f.ParticleExponent);
                    f.ParticlesStartAt = EditorGUILayout.FloatField(new GUIContent("Particles Starts At", "InputValue level at which particles start appearing."), f.ParticlesStartAt);
                    f.ParticlesUsesAdvancedCurve = EditorGUILayout.Toggle(new GUIContent("Do Particles use Advanced Curve?", "Enables particle rate AnimationCurve system."), f.ParticlesUsesAdvancedCurve);
                    if (f.ParticlesUsesAdvancedCurve)
                    {
                        f.ParticlesCurve = EditorGUILayout.CurveField(new GUIContent("Particle Emission Rate Curve", "Advanced particle rate control. Values from 0 to 1 only! The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-axis) acts like a multiplier of the max emission rate, clamped between 0 and 1."), f.ParticlesCurve);
                        _invertX = EditorGUILayout.Toggle(new GUIContent("Invert Curve X Axis", "Should the X Axis of the Curve be inverted on calculation? "), _invertX);
                        _invertY = EditorGUILayout.Toggle(new GUIContent("Invert Curve Y Axis", "Should the Y Axis of the Curve be inverted on calculation? "), _invertY);
                        if (GUILayout.Button(new GUIContent("Calculate Particle Curve", "Calculates Curve with set Exponent, start value and inversion modifiers"))) f.ParticlesCurve = CurveCalculator.GetCurve(f.ParticleExponent, f.ParticlesStartAt, _invertX, _invertY);
                    }
                }
            }
            _soundFoldOut = EditorGUILayout.Foldout(_soundFoldOut, "Sound Effects", foldout);
            if (_soundFoldOut)
            {
                f.SoundEffectSource = (AudioSource)EditorGUILayout.ObjectField(new GUIContent("Sound Effect Audio Source", "Place a preconfigured AudioSource in here. (Configuring AudioSources at runtime is a pain! This lets you much easier choose the desired settings as well.)"), f.SoundEffectSource, typeof(AudioSource), true);

                if (f.SoundEffectSource != null)
                {
                    f.SoundEffect = (AudioClip)EditorGUILayout.ObjectField(new GUIContent("Sound Effect Audio Clip"), f.SoundEffect, typeof(AudioClip), true);
                    if (f.SoundEffect != null)
                    {
                        f.MaxVolume = EditorGUILayout.Slider(new GUIContent("Max Volume"), f.MaxVolume, 0f, 1f);
                        f.SoundExponent = EditorGUILayout.FloatField(new GUIContent("Sound Exponent", "Same as the Emission Exponent, but for the sound volume."), f.SoundExponent);
                        f.SoundStartsAt = EditorGUILayout.FloatField(new GUIContent("Sound Starts At", "InputValue level at which audio starts."), f.SoundStartsAt);
                        f.SoundUsesAdvancedCurve = EditorGUILayout.Toggle(new GUIContent("Does Sound Volume use Advanced Curve?", "Enables sound volume AnimationCurve system."), f.SoundUsesAdvancedCurve);
                        if (f.SoundUsesAdvancedCurve)
                        {
                            f.VolumeCurve = EditorGUILayout.CurveField(new GUIContent("Volume Curve", "Advanced sound volume control. Values from 0 to 1 only!. The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-axis) is the volume at that VisualModifier level."), f.VolumeCurve);
                            _invertX = EditorGUILayout.Toggle(new GUIContent("Invert Curve X Axis", "Should the X Axis of the Curve be inverted on calculation? "), _invertX);
                            _invertY = EditorGUILayout.Toggle(new GUIContent("Invert Curve Y Axis", "Should the Y Axis of the Curve be inverted on calculation? "), _invertY);
                            if (GUILayout.Button(new GUIContent("Calculate Volume Curve", "Calculates Curve with set Exponent, start value and inversion modifiers"))) f.VolumeCurve = CurveCalculator.GetCurve(f.SoundExponent, f.SoundStartsAt, _invertX, _invertY);
                        }
                    }
                }
            }
            _movementFoldOut = EditorGUILayout.Foldout(_movementFoldOut, "Movement Control", foldout);
            if (_movementFoldOut)
            {
                SerializedProperty objectToMove = serializedObject.FindProperty("ObjectToMove");
                BetterPropertyField.DrawSerializedProperty(objectToMove, new GUIContent("Object to Move"));
                if (f.ObjectToMove != null)
                {
                    f.MovementExponent = EditorGUILayout.FloatField(new GUIContent("Movement Exponent", "Same as the Emission Exponent, but for the movement lerp."), f.MovementExponent);
                    f.MovementStartsAt = EditorGUILayout.FloatField(new GUIContent("Movement Starts At", "InputValue level at which movement starts."), f.MovementStartsAt);
                    f.MovementUsesAdvancedCurve = EditorGUILayout.Toggle(new GUIContent("Does Movement use Advanced Curve?", "Enables movement lerp AnimationCurve system."), f.MovementUsesAdvancedCurve);
                    if (f.MovementUsesAdvancedCurve)
                    {
                        f.MovementCurve = EditorGUILayout.CurveField(new GUIContent("Movement Lerp Curve", "Advanced movement lerp control. Values from 0 to 1 only! The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-axis) acts like a multiplier of the max emission rate, clamped between 0 and 1."), f.AnimationCurve);
                        _invertX = EditorGUILayout.Toggle(new GUIContent("Invert Curve X Axis", "Should the X Axis of the Curve be inverted on calculation? "), _invertX);
                        _invertY = EditorGUILayout.Toggle(new GUIContent("Invert Curve Y Axis", "Should the Y Axis of the Curve be inverted on calculation? "), _invertY);
                        if (GUILayout.Button(new GUIContent("Calculate Animation Curve", "Calculates Curve with set Exponent, start value and inversion modifiers"))) f.MovementCurve = CurveCalculator.GetCurve(f.MovementExponent, f.MovementStartsAt, _invertX, _invertY);
                    }
                }
            }
            _animatorFoldOut = EditorGUILayout.Foldout(_animatorFoldOut, "Animator Control", foldout);
            if (_animatorFoldOut)
            {
                f.Animator = (Animator) EditorGUILayout.ObjectField(new GUIContent("Animator"), f.Animator, typeof(Animator), true);
                if (f.Animator != null)
                {
                    f.AnimationNodeName = EditorGUILayout.TextField(new GUIContent("Animation Node Name", "Name of the Animation Node inside the Animator that should be affected"),f.AnimationNodeName);
                    f.AnimationExponent = EditorGUILayout.FloatField(new GUIContent("Animation Exponent", "Same as the Emission Exponent, but for the animation position."), f.AnimationExponent);
                    f.AnimationStartsAt = EditorGUILayout.FloatField(new GUIContent("Animation Starts At", "InputValue level at which animation starts."), f.AnimationStartsAt);
                    f.AnimationUsesAdvancedCurve = EditorGUILayout.Toggle(new GUIContent("Does Animation use Advanced Curve?", "Enables animation position AnimationCurve system."), f.AnimationUsesAdvancedCurve);
                    if (f.AnimationUsesAdvancedCurve)
                    {
                        f.AnimationCurve = EditorGUILayout.CurveField(new GUIContent("Animation Position Curve", "Advanced animation position control. Values from 0 to 1 only! The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-axis) acts like a multiplier of the max emission rate, clamped between 0 and 1."), f.AnimationCurve);
                        _invertX = EditorGUILayout.Toggle(new GUIContent("Invert Curve X Axis", "Should the X Axis of the Curve be inverted on calculation? "), _invertX);
                        _invertY = EditorGUILayout.Toggle(new GUIContent("Invert Curve Y Axis", "Should the Y Axis of the Curve be inverted on calculation? "), _invertY);
                        if (GUILayout.Button(new GUIContent("Calculate Animation Curve", "Calculates Curve with set Exponent, start value and inversion modifiers"))) f.AnimationCurve = CurveCalculator.GetCurve(f.AnimationExponent, f.AnimationStartsAt, _invertX, _invertY);
                    }
                }
            }


            _debugFoldOut = EditorGUILayout.Foldout(_debugFoldOut, "Debugging", foldout);
            if (_debugFoldOut)
            {
                f.ConstantUpdate = EditorGUILayout.Toggle(new GUIContent("Enables standalone updates for in editor debugging! Will cause problems if left active in game!"), f.ConstantUpdate);
                f.InputValue = EditorGUILayout.Slider(new GUIContent("InputValue"), f.InputValue, 0f, 1f);
            }


            serializedObject.ApplyModifiedProperties();
        }

        static void HorizontalLine(Color color)
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;

            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;
        }

        public static class BetterPropertyField
        {


            /// <summary>
            /// Draws a serialized property (including children) fully, even if it's an instance of a custom serializable class.
            /// Supersedes EditorGUILayout.PropertyField(serializedProperty, true);
            /// </summary>
            /// <param name="_serializedProperty">Serialized property.</param>
            /// source: https://gist.github.com/tomkail/ba8d49e1cee021b0b89d47fca68b53a2
            public static void DrawSerializedProperty(SerializedProperty _serializedProperty)
            {
                if (_serializedProperty == null)
                {
                    EditorGUILayout.HelpBox("SerializedProperty was null!", MessageType.Error);
                    return;
                }
                var serializedProperty = _serializedProperty.Copy();
                int startingDepth = serializedProperty.depth;
                EditorGUI.indentLevel = serializedProperty.depth;
                DrawPropertyField(serializedProperty);
                while (serializedProperty.NextVisible(serializedProperty.isExpanded && !PropertyTypeHasDefaultCustomDrawer(serializedProperty.propertyType)) && serializedProperty.depth > startingDepth)
                {
                    EditorGUI.indentLevel = serializedProperty.depth;
                    DrawPropertyField(serializedProperty);
                }
                EditorGUI.indentLevel = startingDepth;
            }
            public static void DrawSerializedProperty(SerializedProperty _serializedProperty, GUIContent content)
            {
                if (_serializedProperty == null)
                {
                    EditorGUILayout.HelpBox("SerializedProperty was null!", MessageType.Error);
                    return;
                }
                var serializedProperty = _serializedProperty.Copy();
                int startingDepth = serializedProperty.depth;
                EditorGUI.indentLevel = serializedProperty.depth;
                DrawPropertyField(serializedProperty, content);
                while (serializedProperty.NextVisible(serializedProperty.isExpanded && !PropertyTypeHasDefaultCustomDrawer(serializedProperty.propertyType)) && serializedProperty.depth > startingDepth)
                {
                    EditorGUI.indentLevel = serializedProperty.depth;
                    DrawPropertyField(serializedProperty);
                }
                EditorGUI.indentLevel = startingDepth;
            }

            static void DrawPropertyField(SerializedProperty serializedProperty)
            {
                if (serializedProperty.propertyType == SerializedPropertyType.Generic)
                {
                    serializedProperty.isExpanded = EditorGUILayout.Foldout(serializedProperty.isExpanded, serializedProperty.displayName, true);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedProperty);
                }
            }
            static void DrawPropertyField(SerializedProperty serializedProperty, GUIContent content)
            {
                if (serializedProperty.propertyType == SerializedPropertyType.Generic)
                {
                    serializedProperty.isExpanded = EditorGUILayout.Foldout(serializedProperty.isExpanded, serializedProperty.displayName, true);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedProperty, content);
                }
            }

            static bool PropertyTypeHasDefaultCustomDrawer(SerializedPropertyType type)
            {
                return
                type == SerializedPropertyType.AnimationCurve ||
                type == SerializedPropertyType.Bounds ||
                type == SerializedPropertyType.Color ||
                type == SerializedPropertyType.Gradient ||
                type == SerializedPropertyType.LayerMask ||
                type == SerializedPropertyType.ObjectReference ||
                type == SerializedPropertyType.Rect ||
                type == SerializedPropertyType.Vector2 ||
                type == SerializedPropertyType.Vector3;
            }
        }
    }
}
#endif