using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;
using FistVR;
using MonoMod.Cil;
using Mono.Cecil.Cil;

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

        public enum ETouchpadDir
        {
            Up,
            Down,
            Left,
            Right
        }

        public static float GetFloatFromAxis(Vector3 vector, Axis axis) { return vector[(int)axis]; }

        public static Vector3 GetVectorFromAxis(Axis axis) => axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _ => Vector3.zero,
        };

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

        public static FVRFireArmChamber GetCurrentChamber(FVRFireArm fireArm)
        {
            FVRFireArmChamber chamber;
            int i;
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
                    chamber = w.Barrels[w.m_curBarrel].Chamber;
                    i = w.m_curBarrel + 1;
                    while ((chamber.IsSpent || !chamber.IsFull) && i < w.Barrels.Length)
                    {
                        chamber = w.Barrels[i].Chamber;
                        i++;
                    }
                    if (chamber.IsSpent || !chamber.IsFull) chamber = w.Barrels[w.m_curBarrel].Chamber;
                    return chamber;
                case Revolver w:
                    chamber = w.Chambers[w.CurChamber];
                    i = w.CurChamber + 1;
                    while ((chamber.IsSpent || !chamber.IsFull) && i < w.Chambers.Length)
                    {
                        chamber = w.Chambers[i];
                        i++;
                    }
                    if (chamber.IsSpent || !chamber.IsFull) chamber = w.Chambers[w.CurChamber];
                    return chamber;
                case SingleActionRevolver w:
                    chamber = w.Cylinder.Chambers[w.CurChamber];
                    i = w.CurChamber + 1;
                    while ((chamber.IsSpent || !chamber.IsFull) && i < w.Cylinder.Chambers.Length)
                    {
                        chamber = w.Cylinder.Chambers[i];
                        i++;
                    }
                    if (chamber.IsSpent || !chamber.IsFull) chamber = w.Cylinder.Chambers[w.CurChamber];
                    return chamber;
                case RevolvingShotgun w:
                    return w.Chambers[w.CurChamber];
                case Flaregun w:
                    return w.Chamber;
                case RollingBlock w:
                    return w.Chamber;
                case Derringer w:
                    chamber = w.Barrels[w.m_curBarrel].Chamber;
                    i = w.m_curBarrel + 1;
                    while ((chamber.IsSpent || !chamber.IsFull) && i < w.Barrels.Count)
                    {
                        chamber = w.Barrels[i].Chamber;
                        i++;
                    }
                    if (chamber.IsSpent || !chamber.IsFull) chamber = w.Barrels[w.m_curBarrel].Chamber;
                    return chamber;
                case LAPD2019 w:
                    chamber = w.Chambers[w.CurChamber];
                    i = w.CurChamber + 1;
                    while ((chamber.IsSpent || !chamber.IsFull) && i < w.Chambers.Length)
                    {
                        chamber = w.Chambers[i];
                        i++;
                    }
                    if (chamber.IsSpent || !chamber.IsFull) chamber = w.Chambers[w.CurChamber];
                    return chamber;
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
                    chamber = w.Chambers[w.m_curChamber];
                    i = w.m_curChamber + 1;
                    while ((chamber.IsSpent || !chamber.IsFull) && i < w.Chambers.Length)
                    {
                        chamber = w.Chambers[i];
                        i++;
                    }
                    if (chamber.IsSpent || !chamber.IsFull) chamber = w.Chambers[w.m_curChamber];
                    return chamber;
                case Airgun w:
                    return w.Chamber;
                case CarlGustaf w:
                    return w.Chamber;
                case MeatNailer w:
                    return w.Chamber;
                case Girandoni w:
                    return w.Chamber;
                default:
                    if (fireArm.GetChambers().Count > 0) return fireArm.GetChambers()[0];
                    else return fireArm.GetComponentInChildren<FVRFireArmChamber>();
            }
        }

        [Serializable]
        public class RecoilMultipliers
        {
            public float VerticalRotPerShot = 1f;
            public float MaxVerticalRot_Bipodded = 1f;
            public float MaxVerticalRot = 1f;
            public float VerticalRotRecovery = 1f;

            public float HorizontalRotPerShot = 1f;
            public float MaxHorizontalRot_Bipodded = 1f;
            public float MaxHorizontalRot = 1f;
            public float HorizontalRotRecovery = 1f;

            public float ZLinearPerShot = 1f;
            public float ZLinearMax = 1f;
            public float ZLinearRecovery = 1f;

            public float XYLinearPerShot = 1f;
            public float XYLinearMax = 1f;
            public float XYLinearRecovery = 1f;
        }

        public static FVRFireArmRecoilProfile CopyAndAdjustRecoilProfile(FVRFireArmRecoilProfile orig, float recoilFactor)
        {
            FVRFireArmRecoilProfile copy = Instantiate(orig);
            copy.VerticalRotPerShot *= recoilFactor;
            copy.MaxVerticalRot_Bipodded *= recoilFactor;
            copy.MaxVerticalRot *= recoilFactor;
            copy.VerticalRotRecovery /= recoilFactor;

            copy.HorizontalRotPerShot *= recoilFactor;
            copy.MaxHorizontalRot_Bipodded *= recoilFactor;
            copy.MaxHorizontalRot *= recoilFactor;
            copy.HorizontalRotRecovery /= recoilFactor;

            copy.ZLinearPerShot *= recoilFactor;
            copy.ZLinearMax *= recoilFactor;
            copy.ZLinearRecovery /= recoilFactor;

            copy.XYLinearPerShot *= recoilFactor;
            copy.XYLinearMax *= recoilFactor;
            copy.XYLinearRecovery /= recoilFactor;

            return copy;
        }

        public static FVRFireArmRecoilProfile CopyAndAdjustRecoilProfile(FVRFireArmRecoilProfile orig, float recoilFactor, float recoveryFactor)
        {
            FVRFireArmRecoilProfile copy = Instantiate(orig);
            copy.VerticalRotPerShot *= recoilFactor;
            copy.MaxVerticalRot_Bipodded *= recoilFactor;
            copy.MaxVerticalRot *= recoilFactor;
            copy.VerticalRotRecovery /= recoveryFactor;

            copy.HorizontalRotPerShot *= recoilFactor;
            copy.MaxHorizontalRot_Bipodded *= recoilFactor;
            copy.MaxHorizontalRot *= recoilFactor;
            copy.HorizontalRotRecovery /= recoveryFactor;

            copy.ZLinearPerShot *= recoilFactor;
            copy.ZLinearMax *= recoilFactor;
            copy.ZLinearRecovery /= recoveryFactor;

            copy.XYLinearPerShot *= recoilFactor;
            copy.XYLinearMax *= recoilFactor;
            copy.XYLinearRecovery /= recoveryFactor;

            return copy;
        }

        public static FVRFireArmRecoilProfile CopyAndAdjustRecoilProfile(FVRFireArmRecoilProfile orig, RecoilMultipliers multipliers)
        {
            FVRFireArmRecoilProfile copy = Instantiate(orig);
            copy.VerticalRotPerShot *= multipliers.VerticalRotPerShot;
            copy.MaxVerticalRot_Bipodded *= multipliers.MaxVerticalRot_Bipodded;
            copy.MaxVerticalRot *= multipliers.MaxVerticalRot;
            copy.VerticalRotRecovery *= multipliers.VerticalRotRecovery;

            copy.HorizontalRotPerShot *= multipliers.HorizontalRotPerShot;
            copy.MaxHorizontalRot_Bipodded *= multipliers.MaxHorizontalRot_Bipodded;
            copy.MaxHorizontalRot *= multipliers.MaxHorizontalRot;
            copy.HorizontalRotRecovery *= multipliers.HorizontalRotRecovery;

            copy.ZLinearPerShot *= multipliers.ZLinearPerShot;
            copy.ZLinearMax *= multipliers.ZLinearMax;
            copy.ZLinearRecovery *= multipliers.ZLinearRecovery;

            copy.XYLinearPerShot *= multipliers.XYLinearPerShot;
            copy.XYLinearMax *= multipliers.XYLinearMax;
            copy.XYLinearRecovery *= multipliers.XYLinearRecovery;

            return copy;
        }

        // Methods for getting a reference to call the base method of something for patching virtual methods.
        public static Action GetBaseAction(Type BaseClass, string MethodName, MonoBehaviour self)
        {
            var pointer = BaseClass.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer();

            return (Action)Activator.CreateInstance(typeof(Action), self, pointer);
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

        // Logging
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

        public static void ProjectileFired(FVRFireArm fireArm, ref BallisticProjectile projectile)
        {
            ProjectileFiredEvent?.Invoke(fireArm, ref projectile);
        }

        public static event ProjectileFired ProjectileFiredEvent;

        public static bool IsInEditor
        {
            get
            {
                return Application.isEditor;
            }
        }

        /// <summary>
        /// Clears the editor log.
        /// </summary>
        public static void ClearEditorLog()
        {
            if (!IsInEditor) return;
#if DEBUG
            var activeEditorTrackerAssembly = Assembly.GetAssembly(typeof(UnityEditor.ActiveEditorTracker));
            var unityEditorInternalLogEntriesType = activeEditorTrackerAssembly.GetType("UnityEditorInternal.LogEntries");
            var clearMethodInfo = unityEditorInternalLogEntriesType.GetMethod("Clear");
            clearMethodInfo.Invoke(new object(), null);
#endif
        }

        private static Assembly s_unityEditorAssembly;
        private static PropertyInfo s_isPlayingPropertyInfo;
        private static bool? s_inUnityEditor = null;

#if !DEBUG
        public static void FVRFireArm_Fire_ProjectileFiredEventHook(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(
                MoveType.Before,
                i => i.MatchLdloc(8),
                i => i.MatchLdloc(8),
                i => i.MatchLdfld<BallisticProjectile>(nameof(BallisticProjectile.MuzzleVelocityBase)),
                i => i.MatchLdarg(1)
                //i => i.MatchCallvirt<BallisticProjectile>(nameof(BallisticProjectile.Fire))
            );

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, 8);
            // Same as "c.Emit(OpCodes.Call, typeof(OpenScripts2_BasePlugin).GetMethod(nameof(OpenScripts2_BasePlugin.ProjectileFired), new Type[] { typeof(FVRFireArm), typeof(BallisticProjectile&) }));"
            c.EmitDelegate(ProjectileFired);
        }
#endif
    }

    public static class ETouchpadDir_Extension
    {
        public static Vector2 GetDir(this OpenScripts2_BasePlugin.ETouchpadDir dir) => dir switch
        {
            OpenScripts2_BasePlugin.ETouchpadDir.Up => Vector2.up,
            OpenScripts2_BasePlugin.ETouchpadDir.Down => Vector2.down,
            OpenScripts2_BasePlugin.ETouchpadDir.Left => Vector2.left,
            OpenScripts2_BasePlugin.ETouchpadDir.Right => Vector2.right,
            _ => Vector2.zero,
        };
    }

    public delegate void ProjectileFired(FVRFireArm fireArm, ref BallisticProjectile projectile);
}