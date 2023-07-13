using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class OpenScripts2_BasePlugin : MonoBehaviour
    {
        public enum Axis
        {
            X = 0,
            Y = 1,
            Z = 2
        }

        public enum TransformType
        {
            Movement,
            Rotation,
            Scale
        }

        public static float GetFloatFromAxis(Vector3 vector, Axis axis) { return vector[(int)axis]; }

        public static FVRFireArmChamber GetCurrentChamber(FVRFireArm fireArm)
        {
            switch (fireArm)
            {
                case Handgun w:
                    return w.Chamber;
                case ClosedBoltWeapon w:
                    return w.Chamber;
                case OpenBoltReceiver w:
                    return w.Chamber;
                case TubeFedShotgun w:
                    return w.Chamber;
                case BoltActionRifle w:
                    return w.Chamber;
                case BreakActionWeapon w:
                    return w.Barrels[w.m_curBarrel].Chamber;
                case Revolver w:
                    return w.Chambers[w.CurChamber];
                case SingleActionRevolver w:
                    return w.Cylinder.Chambers[w.CurChamber];
                case RevolvingShotgun w:
                    return w.Chambers[w.CurChamber];
                case Flaregun w:
                    return w.Chamber;
                case RollingBlock w:
                    return w.Chamber;
                case Derringer w:
                    return w.Barrels[w.m_curBarrel].Chamber;
                case LAPD2019 w:
                    return w.Chambers[w.CurChamber];
                case BAP w:
                    return w.Chamber;
                case HCB w:
                    return w.Chamber;
                case M72 w:
                    return w.Chamber;
                case MF2_RL w:
                    return w.Chamber;
                case RGM40 w:
                    return w.Chamber;
                case RPG7 w:
                    return w.Chamber;
                case SimpleLauncher w:
                    return w.Chamber;
                case SimpleLauncher2 w:
                    return w.Chamber;
                case RemoteMissileLauncher w:
                    return w.Chamber;
                case PotatoGun w:
                    return w.Chamber;
                case GrappleGun w:
                    return w.Chambers[w.m_curChamber];
                default:
                    if (fireArm.GetChambers().Count > 0) return fireArm.GetChambers()[0];
                    else return null;
            }
        }

        public static Vector3 GetVectorFromAxis(Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return Vector3.right;
                case Axis.Y:
                    return Vector3.up;
                case Axis.Z:
                    return Vector3.forward;
            }
            return Vector3.zero;
        }

        public static bool TouchpadDirDown(FVRViveHand hand, Vector2 dir)
        {
            return hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, dir) < 45f;
        }
        public static bool TouchpadDirPressed(FVRViveHand hand, Vector2 dir)
        {
            return hand.Input.TouchpadPressed && Vector2.Angle(hand.Input.TouchpadAxes, dir) < 45f;
        }

        public static Quaternion GetTargetQuaternionFromAxis(float value, Axis axis)
        {
            return Quaternion.AngleAxis(value, GetVectorFromAxis(axis));
        }

        public static FVRViveHand GetLeftHand()
        {
            FVRViveHand[] FVRViveHands = GM.CurrentMovementManager.Hands;
            if (FVRViveHands[0].IsThisTheRightHand) return FVRViveHands[1];
            else return FVRViveHands[0];
        }
        public static FVRViveHand GetRightHand()
        {
            FVRViveHand[] FVRViveHands = GM.CurrentMovementManager.Hands;
            if (FVRViveHands[0].IsThisTheRightHand) return FVRViveHands[0];
            else return FVRViveHands[1];
        }

        public static Vector3 GetClosestValidPointUnclamped(Vector3 vA, Vector3 vB, Vector3 vPoint)
        {
            Vector3 rhs = vPoint - vA;
            Vector3 normalized = (vB - vA).normalized;
            float num2 = Vector3.Dot(normalized, rhs);
            Vector3 b = normalized * num2;
            return vA + b;
        }


        public static Action GetBaseAction(Type BaseClass, string MethodName, MonoBehaviour self)
        {
            var pointer = BaseClass.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer();

            return (Action) Activator.CreateInstance(typeof(Action), self, pointer);
        }

        public static Action<T> GetBaseAction<T>(Type BaseClass, string MethodName, MonoBehaviour self)
        {
            var pointer = BaseClass.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer();

            return (Action<T>)Activator.CreateInstance(typeof(Action<T>), self, pointer);
        }



        public static Func<T> GetBaseFunc<T>(Type BaseClass, string MethodName, MonoBehaviour self)
        {
            var pointer = BaseClass.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer();

            return (Func<T>)Activator.CreateInstance(typeof(Func<T>), self, pointer);
        }


        public void Log(string message)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogMessage($"{this}: {message}");
        }
        public void LogWarning(string message)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogWarning($"{this}: {message}");
        }
        public void LogError(string message)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogError($"{this}: {message}");
        }
        public void LogException(Exception e)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogError($"{this}: {e.Message}");
        }
    }
}