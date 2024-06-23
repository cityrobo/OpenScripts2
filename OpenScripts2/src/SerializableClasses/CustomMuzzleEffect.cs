using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    [Serializable]
    public class CustomMuzzleEffect
    {
        public MuzzleEffectConfig Entry;
        public MuzzleEffectSize Size;
        public Transform OverridePoint;
        public bool EmitWhenGunSuppressed = true;
        public bool EmitWhenGunHasMuzzleDevices = true;
    }
}
