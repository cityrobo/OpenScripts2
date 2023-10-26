using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenScripts2;
using HarmonyLib;
using UnityEditor;
using Valve.Newtonsoft.Json.Linq;

namespace OpenScripts2
{
	public static class UniversalCopy
    {
        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            System.Reflection.FieldInfo[] fields = type.GetFields(flags);
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        public static T CopyComponent<T>(this Component target, T reference) where T : Component
		{
			Type type = reference.GetType();
			//if (type != reference.GetType()) return null; // type mis-match
			BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			PropertyInfo[] pinfos = type.GetProperties(flags);
			foreach (var pinfo in pinfos)
			{
				if (pinfo.CanWrite)
				{
					try
					{
						pinfo.SetValue(target, pinfo.GetValue(reference, null), null);
					}
					catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
				}
			}
			FieldInfo[] finfos = type.GetFields(flags);
			foreach (var finfo in finfos)
			{
				finfo.SetValue(target, finfo.GetValue(reference));
			}
			return target as T;
		}

		public static T CopyObject<T>(this UnityEngine.Object target, T reference) where T : UnityEngine.Object
		{
			Type type = reference.GetType();
			//if (type != reference.GetType()) return null; // type mis-match
			BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			PropertyInfo[] pinfos = type.GetProperties(flags);
			foreach (var pinfo in pinfos)
			{
				if (pinfo.CanWrite)
				{
					try
					{
						pinfo.SetValue(target, pinfo.GetValue(reference, null), null);
					}
					catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
				}
			}
			FieldInfo[] finfos = type.GetFields(flags);
			foreach (var finfo in finfos)
			{
				finfo.SetValue(target, finfo.GetValue(reference));
			}
			return target as T;
		}
	}

    public static class MiscUtilities
    {
        public static Sprite EmptySprite = null;

        public static Sprite CreateEmptySprite()
        {
            if (EmptySprite == null)
            {
                // Create an empty texture
                Texture2D emptyTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                emptyTexture.SetPixel(0, 0, Color.clear);
                emptyTexture.Apply();

                // Create an empty sprite
                EmptySprite = Sprite.Create(emptyTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100);
                
            }
            return EmptySprite;
        }

        public static T ParseEnum<T>(string enumString) where T : Enum
        {
            return (T)Enum.Parse(typeof(T), enumString);
        }

        public static T[] ParseToEnumArray<T>(string CSV) where T : Enum
        {
            if (CSV == null || CSV.All(char.IsWhiteSpace))
            {
                return new T[0]; // Return an empty array if the CSV is empty or null.
            }

            // Split the CSV string into individual values (assuming they are separated by commas).
            string[] values = CSV.Split(',');

            // Parse each value into its corresponding enum value.
            T[] enumValues = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                enumValues[i] = ParseEnum<T>(values[i]);
            }

            return enumValues;
        }
    }

    public static class Vector3Utils
    {
        public static Vector3 ProjectOnPlaneThroughPoint(Vector3 vector, Vector3 point, Vector3 planeNormal)
        {
            return Vector3.ProjectOnPlane(vector, planeNormal) + Vector3.Dot(point, planeNormal) * planeNormal;
        }

        public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
        {
            /*
            float lerpx = Mathf.InverseLerp(a.x, b.x, value.x);
            float lerpy = Mathf.InverseLerp(a.y, b.y, value.y);
            float lerpz = Mathf.InverseLerp(a.z, b.z, value.z);

            Vector3 lerp = new Vector3(lerpx, lerpy, lerpz);
            return lerp.magnitude;
            */

            Vector3 AB = b - a;
            Vector3 AV = value - a;
            return Mathf.Clamp01(Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB));
        }

        public static float InverseLerpUnclamped(Vector3 a, Vector3 b, Vector3 value)
        {
            /*
            float lerpx = Mathf.InverseLerp(a.x, b.x, value.x);
            float lerpy = Mathf.InverseLerp(a.y, b.y, value.y);
            float lerpz = Mathf.InverseLerp(a.z, b.z, value.z);

            Vector3 lerp = new Vector3(lerpx, lerpy, lerpz);
            return lerp.magnitude;
            */

            Vector3 AB = b - a;
            Vector3 AV = value - a;
            return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
        }
    }

    static class QuaternionUtils
    {

    }

    static class CurveCalculator
    {
        public static AnimationCurve GetCurve(float Exponent, float StartsAt = 0f, bool InvertX = false, bool InvertY = false, int NumberOfKeys = 10)
        {
            List<Keyframe> keys = new List<Keyframe>();
            float tangent;
            float step = 1f / NumberOfKeys;
            Keyframe key;

            float X = 0f;
            float Y = Mathf.Pow(X, Exponent);

            int j = 0;

            if (StartsAt != 0f)
            {
                if (InvertX)
                {
                    X = 1f - X;
                }
                if (InvertY)
                {
                    Y = 1f - Y;
                }
                key = new Keyframe(X, Y, 0, 0);
                keys.Add(key);

                X = 0;
                tangent = GetTangentAt(X,Exponent) / (1f - StartsAt);
                if (tangent == float.PositiveInfinity) tangent = Mathf.Pow(0f + step, Exponent) / (step / 1.5f);
                Y = Mathf.Pow(X, Exponent);
                X = Mathf.Lerp(StartsAt, 1f, X);
                if (InvertY)
                {
                    Y = 1f - Y;
                    tangent = -tangent;
                }
                if (InvertX)
                {
                    X = 1f - X;
                    tangent = -tangent;

                    key = new Keyframe(X, Y, tangent, 0);
                    key.tangentMode = 1;
                }
                else
                {
                    key = new Keyframe(X, Y, 0, tangent);
                    key.tangentMode = 1;
                }

                keys.Add(key);

                j = 1;
            }
            for (int i = j; i <= NumberOfKeys; i++)
            {
                X = i * step;
                tangent = GetTangentAt(X, Exponent) / (1f - StartsAt);
                if (tangent == float.PositiveInfinity) tangent = Mathf.Pow(X + step, Exponent) / (step / 1.5f);

                Y = Mathf.Pow(X, Exponent);
                X = Mathf.Lerp(StartsAt, 1f, X);
                if (InvertX)
                {
                    X = 1f - X;
                    tangent = -tangent;
                }
                if (InvertY)
                {
                    Y = 1f - Y;
                    tangent = -tangent;
                }

                key = new Keyframe(X, Y, tangent, tangent);
                keys.Add(key);
            }

            return new AnimationCurve(keys.ToArray());
        }
        public static float GetTangentAt(float X, float Exponent)
        {
            return Exponent * Mathf.Pow(X, Exponent - 1f);
        }
    }
}

namespace UnityEngine
{
    public static class UnityEngineExtensions
    {
        public static T GetComponentInDirectChildren<T>(this Component parent) where T : Component
        {
            return parent.GetComponentInDirectChildren<T>(false);
        }

        public static T GetComponentInDirectChildren<T>(this Component parent, bool includeInactive) where T : Component
        {
            foreach (Transform transform in parent.transform)
            {
                if (includeInactive || transform.gameObject.activeInHierarchy)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                    {
                        return component;
                    }
                }
            }
            return null;
        }

        public static T[] GetComponentsInDirectChildren<T>(this Component parent) where T : Component
        {
            return parent.GetComponentsInDirectChildren<T>(false);
        }

        public static T[] GetComponentsInDirectChildren<T>(this Component parent, bool includeInactive) where T : Component
        {
            List<T> tmpList = new List<T>();
            foreach (Transform transform in parent.transform)
            {
                if (includeInactive || transform.gameObject.activeInHierarchy)
                {
                    tmpList.AddRange(transform.GetComponents<T>());
                }
            }
            return tmpList.ToArray();
        }

        public static T GetComponentInSiblings<T>(this Component sibling) where T : Component
        {
            return sibling.GetComponentInSiblings<T>(false);
        }

        public static T GetComponentInSiblings<T>(this Component sibling, bool includeInactive) where T : Component
        {
            Transform parent = sibling.transform.parent;
            if (parent == null) return null;
            foreach (Transform transform in parent)
            {
                if (includeInactive || transform.gameObject.activeInHierarchy)
                {
                    if (transform != sibling)
                    {
                        T component = transform.GetComponent<T>();
                        if (component != null)
                        {
                            return component;
                        }
                    }
                }
            }
            return null;
        }

        public static T[] GetComponentsInSiblings<T>(this Component sibling) where T : Component
        {
            return sibling.GetComponentsInSiblings<T>(false);
        }

        public static T[] GetComponentsInSiblings<T>(this Component sibling, bool includeInactive) where T : Component
        {
            Transform parent = sibling.transform.parent;
            if (parent == null) return null;
            List<T> tmpList = new List<T>();
            foreach (Transform transform in parent)
            {
                if (includeInactive || transform.gameObject.activeInHierarchy)
                {
                    if (transform != sibling)
                    {
                        tmpList.AddRange(transform.GetComponents<T>());
                    }
                }
            }
            return tmpList.ToArray();
        }

        public static T GetComponentInDirectParent<T>(this Component child) where T : Component
        {
            Transform parent = child.transform.parent;
            if (parent == null) return null;
            return parent.GetComponent<T>();
        }

        public static T[] GetComponentsInDirectParent<T>(this Component child) where T : Component
        {
            Transform parent = child.transform.parent;
            if (parent == null) return null;
            return parent.GetComponents<T>();
        }

        // TryGetComponent for Components
        public static bool TryGetComponent<T>(this Component mainComponent, out T foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponent<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponentInChildren<T>(this Component mainComponent, out T foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponentInChildren<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponentInParent<T>(this Component mainComponent, out T foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponentInParent<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponents<T>(this Component mainComponent, out T[] foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponents<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponentsInChildren<T>(this Component mainComponent, out T[] foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponentsInChildren<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponentsInParent<T>(this Component mainComponent, out T[] foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponentsInParent<T>();
            return foundComponent != null;
        }

        // TryGetComponent for GameObjects
        public static bool TryGetComponent<T>(this GameObject mainComponent, out T foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponent<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponentInChildren<T>(this GameObject mainComponent, out T foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponentInChildren<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponentInParent<T>(this GameObject mainComponent, out T foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponentInParent<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponents<T>(this GameObject mainComponent, out T[] foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponents<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponentsInChildren<T>(this GameObject mainComponent, out T[] foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponentsInChildren<T>();
            return foundComponent != null;
        }
        public static bool TryGetComponentsInParent<T>(this GameObject mainComponent, out T[] foundComponent) where T : Component
        {
            foundComponent = mainComponent.GetComponentsInParent<T>();
            return foundComponent != null;
        }

        public static bool IsGreaterThan(this Vector3 local, Vector3 other)
        {
            if (local.x > other.x && local.y > other.y && local.z > other.z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsGreaterThanOrEqual(this Vector3 local, Vector3 other)
        {
            if (local.x >= other.x && local.y >= other.y && local.z >= other.z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsLessThan(this Vector3 local, Vector3 other)
        {
            if (local.x < other.x && local.y < other.y && local.z < other.z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsLessThanOrEqual(this Vector3 local, Vector3 other)
        {
            if (local.x <= other.x && local.y <= other.y && local.z <= other.z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Vector3 Clamp(this Vector3 vector, Vector3 vA, Vector3 vB)
        {
            vector.x = Mathf.Clamp(vector.x, vA.x, vB.x);
            vector.y = Mathf.Clamp(vector.y, vA.y, vB.y);
            vector.z = Mathf.Clamp(vector.z, vA.z, vB.z);
            return vector;
        }

        public static Vector3 ProjectOnPlaneThroughPoint(this Vector3 vector, Vector3 point, Vector3 planeNormal)
        {
            return Vector3.ProjectOnPlane(vector, planeNormal) + Vector3.Dot(point, planeNormal) * planeNormal;
        }

        /// <summary>
        /// Return the value of the local position associated with the supplied axis
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="axis">The Axis you want to return the value from.</param>
        /// <returns>Transform.localPosition.axis</returns>
        public static Vector3 GetLocalDirAxis(this Transform transform, OpenScripts2_BasePlugin.Axis axis)
        {
            switch (axis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    return transform.right;
                case OpenScripts2_BasePlugin.Axis.Y:
                    return transform.up;
                case OpenScripts2_BasePlugin.Axis.Z:
                    return transform.forward;
                default:
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// Return the value of the local position associated with the supplied axis
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="axis">The Axis you want to return the value from.</param>
        /// <returns>Transform.localPosition.axis</returns>
        public static float GetLocalPositionAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis)
        {
            switch (axis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    return transform.localPosition.x;
                case OpenScripts2_BasePlugin.Axis.Y:
                    return transform.localPosition.y;
                case OpenScripts2_BasePlugin.Axis.Z:
                    return transform.localPosition.z;
                default:
                    return 0f;
            }
        }

        public static float GetLocalScaleAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis)
        {
            switch (axis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    return transform.localScale.x;
                case OpenScripts2_BasePlugin.Axis.Y:
                    return transform.localScale.y;
                case OpenScripts2_BasePlugin.Axis.Z:
                    return transform.localScale.z;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Return the value of the vector associated with the supplied axis
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="axis">The Axis you want to return the value from.</param>
        /// <returns>Transform.localPosition.axis</returns>
        public static float GetAxisValue(this Vector3 vector, OpenScripts2_BasePlugin.Axis axis)
        {
            return vector[(int)axis];
        }

        /// <summary>
        /// Return a vector that only contains the supplied axis of the vector
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="axis">The Axis you want to return the value from.</param>
        /// <returns>Vector3(this.vector.axis)</returns>
        public static Vector3 GetAxisVector(this Vector3 vector, OpenScripts2_BasePlugin.Axis axis)
        {
            Vector3 strippedVector = Vector3.zero;
            switch (axis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    strippedVector.x = vector.x;
                    break;
                case OpenScripts2_BasePlugin.Axis.Y:
                    strippedVector.y = vector.y;
                    break;
                case OpenScripts2_BasePlugin.Axis.Z:
                    strippedVector.z = vector.z;
                    break;
            }
            return strippedVector;
        }

        /// <summary>
        /// Overwrite the axis value of the other vector with this one
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="axis">Axis to overwrite</param>
        /// <param name="other">The other vector to overwrite the axis value on</param>
        /// <returns>Return other vector with axis value replaced by this one</returns>
        public static Vector3 GetCombinedAxisVector(this Vector3 vector, OpenScripts2_BasePlugin.Axis axis, Vector3 other)
        {
            Vector3 strippedVector = other;
            switch (axis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    strippedVector.x = vector.x;
                    break;
                case OpenScripts2_BasePlugin.Axis.Y:
                    strippedVector.y = vector.y;
                    break;
                case OpenScripts2_BasePlugin.Axis.Z:
                    strippedVector.z = vector.z;
                    break;
            }
            return strippedVector;
        }

        public static float GetAxisValue(this Quaternion quaternion, OpenScripts2_BasePlugin.Axis axis)
        {
            return quaternion.eulerAngles[(int)axis];
        }


        public static Vector3 ModifyAxisValue(this Vector3 vector, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            vector[(int)axis] = value;
            return vector;
        }

        public static Vector3 RemoveAxisValue(this Vector3 vector, OpenScripts2_BasePlugin.Axis axis)
        {
            return vector.ModifyAxisValue(axis, 0f);
        }

        public static void ModifyLocalTransform(this Transform transform, OpenScripts2_BasePlugin.TransformType type, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            switch (type)
            {
                case OpenScripts2_BasePlugin.TransformType.Movement:
                    transform.ModifyLocalPositionAxisValue(axis, value);
                    break;
                case OpenScripts2_BasePlugin.TransformType.Rotation:
                    transform.ModifyLocalRotationAxisValue(axis, value);
                    break;
                case OpenScripts2_BasePlugin.TransformType.Scale:
                    transform.ModifyLocalScaleAxisValue(axis, value);
                    break;
            }
        }

        public static void ModifyPositionAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            Vector3 newPos = transform.position;
            newPos[(int)axis] = value;
            if (VectorsNotEqual(transform.position, newPos)) transform.position = newPos;
        }

        public static void ModifyLocalPositionAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            Vector3 newPos = transform.localPosition;
            newPos[(int)axis] = value;
            if (VectorsNotEqual(transform.localPosition, newPos)) transform.localPosition = newPos;
        }

        public static void ModifyRotationAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            Vector3 newRot = transform.rotation.eulerAngles;
            newRot[(int)axis] = value;
            if (axis == OpenScripts2_BasePlugin.Axis.X && newRot.y >= 179f && newRot.z >= 179f)
            {
                newRot.y -= 180f;
                newRot.z -= 180f;
            }
            if (QuaternionsNotEqual(transform.rotation, Quaternion.Euler(newRot))) transform.rotation = Quaternion.Euler(newRot);
        }

        public static void ModifyLocalRotationAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            Vector3 newRot = transform.localRotation.eulerAngles;
            newRot[(int)axis] = value;
            if (axis == OpenScripts2_BasePlugin.Axis.X && newRot.y >= 179f && newRot.z >= 179f)
            {
                newRot.y -= 180f;
                newRot.z -= 180f;
            }
            //if (transform.localRotation != Quaternion.Euler(newRot)) transform.localRotation = Quaternion.Euler(newRot);

            if (QuaternionsNotEqual(transform.localRotation, Quaternion.Euler(newRot))) transform.localRotation = Quaternion.Euler(newRot);
        }

        public static void ModifyLocalScaleAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            Vector3 newPos = transform.localScale;
            newPos[(int)axis] = value;
            if (VectorsNotEqual(transform.localScale, newPos)) transform.localScale = newPos;
        }

        public static void GoToTransformProxy(this Transform transform, TransformProxy proxy)
        {
            transform.position = proxy.position;
            transform.rotation = proxy.rotation;
            transform.localScale = proxy.localScale;
        }

        public static Quaternion TransformRotation(this Transform transform, Quaternion rot)
        {
            return transform.rotation * rot;
        }

        public static Quaternion InverseTransformRotation(this Transform transform, Quaternion rot)
        {
            return Quaternion.Inverse(transform.rotation) * rot;
        }

        public static Vector3 MultiplyComponentWise(this Vector3 v1, Vector3 v2)
        {
            float x = v1.x * v2.x;
            float y = v1.y * v2.y;
            float z = v1.z * v2.z;
            return new Vector3(x, y, z);
        }
        public static Vector3 ApproximateInfiniteComponent(this Vector3 v1, float infinityApproximation)
        {
            Vector3 v2 = v1;
            if (float.IsInfinity(v2.x))
            {
                v2.x = float.IsPositiveInfinity(v2.x) ? infinityApproximation : -infinityApproximation;
            }
            if (float.IsInfinity(v2.y))
            {
                v2.y = float.IsPositiveInfinity(v2.y) ? infinityApproximation : -infinityApproximation;
            }
            if (float.IsInfinity(v2.z))
            {
                v2.z = float.IsPositiveInfinity(v2.z) ? infinityApproximation : -infinityApproximation;
            }
            return v2;
        }

        public static Quaternion Subtract(this Quaternion a, Quaternion b)
        {
            return a * Quaternion.Inverse(b);
        }

        public static bool VectorsNotEqual(Vector3 a, Vector3 b)
        {
            bool x = a.x != b.x;
            bool y = a.y != b.y;
            bool z = a.z != b.z;

            return x || y || z;
        }

        public static bool QuaternionsNotEqual(Quaternion a, Quaternion b)
        {
            bool x = a.x != b.x;
            bool y = a.y != b.y;
            bool z = a.z != b.z;
            bool w = a.w != b.w;

            return x || y || z || w;
        }
    }
}
namespace System.Collections.Generic
{
    public static class CSharpExtensions
    {
        public static string ToCSV<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null || !enumerable.Any())
            {
                return string.Empty;
            }

            return string.Join(",", enumerable.Select(item => item.ToString()).ToArray());
        }
    }
}