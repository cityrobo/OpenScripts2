using System;
using System.Collections.Generic;
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
#if !DEBUG

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
                    if (fireArm.FChambers.Count > 0) return fireArm.FChambers[0];
                    else return null;
            }
        }

        public static Vector3 GetDirVector(Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return Vector3.right;
                case Axis.Y:
                    return Vector3.up;
                case Axis.Z:
                    return Vector3.forward;
                default:
                    return Vector3.zero;
            }
        }

        public static Quaternion GetTargetQuaternion(float value, Axis axis)
        {
            return Quaternion.AngleAxis(value, GetDirVector(axis));
        }
#endif
    }
}