using System;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2.ModularWorkshop
{
    public class ModularBarrel : ModularWeaponPart
    {
        public Transform MuzzlePosition;
        public FVRFireArm.MuzzleState DefaultMuzzleState;

        public override void Awake()
        {
            base.Awake();
        }
    }
}