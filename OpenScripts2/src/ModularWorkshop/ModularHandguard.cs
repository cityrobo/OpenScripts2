using System;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2.ModularWorkshop
{
    public class ModularHandguard : ModularWeaponPart
    {
        public bool ActsLikeForeGrip;
        public Vector3 AltGripTriggerGameObjectPosition;
        public bool IsTriggerComponentPosition = false;
        public Vector3 AltGripTriggerGameObjectScale;
        public bool IsTriggerComponentSize = false;
        public Vector3 AltGripTriggerGameObjectRotation;
    }
}
