using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class FireArmStartBurstFix : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;

        public void Awake()
        {
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    w.m_CamBurst = w.FireSelector_Modes[w.FireSelectorModeIndex].BurstAmount;
                    break;
                case Handgun w:
                    w.m_CamBurst = w.FireSelectorModes[w.FireSelectorModeIndex].BurstAmount;
                    break;
                default:
                    break;
            }
        }
    }
}
