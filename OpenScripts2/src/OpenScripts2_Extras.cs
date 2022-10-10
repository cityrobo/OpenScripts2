using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenScripts2;

namespace OpenScripts2
{
	public static class UniversalCopy
    {
        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        public static T CopyComponent<T>(this Component target, T reference) where T : Component
		{
			Type type = target.GetType();
			if (type != reference.GetType()) return null; // type mis-match
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
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
			Type type = target.GetType();
			if (type != reference.GetType()) return null; // type mis-match
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
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

        public static float GetAxisValue(this Vector3 vector, OpenScripts2_BasePlugin.Axis axis)
        {
            return vector[(int)axis];
        }

        public static Vector3 ModifyAxisValue(this Vector3 vector, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            vector[(int)axis] = value;
            return vector;
        }

        public static void ModifyPositionAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            Vector3 newPos = transform.position;
            newPos[(int)axis] = value;
            transform.localPosition = newPos;
        }

        public static void ModifyLocalPositionAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            Vector3 newPos = transform.localPosition;
            newPos[(int)axis] = value;
            transform.localPosition = newPos;
        }

        public static void ModifyRotationAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            Vector3 newRot = transform.rotation.eulerAngles;
            newRot[(int)axis] = value;
            transform.rotation = Quaternion.Euler(newRot);
        }

        public static void ModifyLocalRotationAxisValue(this Transform transform, OpenScripts2_BasePlugin.Axis axis, float value)
        {
            Vector3 newRot = transform.localRotation.eulerAngles;
            newRot[(int)axis] = value;
            transform.localRotation = Quaternion.Euler(newRot);
        }

        public static Quaternion TransformRotation(this Transform transform, Quaternion rot)
        {
            return transform.parent.rotation * rot;
        }

        public static Quaternion InverseTransformRotation(this Transform transform, Quaternion rot)
        {
            return Quaternion.Inverse(transform.rotation) * rot;
        }

        public static Quaternion Subtract(this Quaternion a, Quaternion b)
        {
            return a * Quaternion.Inverse(b);
        }
    }
}
