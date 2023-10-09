using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class ForceVirtualStock : OpenScripts2_BasePlugin
    {
        public FVRPhysicalObject PhysicalObject = null;
		public Transform StockPos = null;

        private static readonly Dictionary<FVRPhysicalObject,ForceVirtualStock> _existingForceVirtualStock = new();
#if !DEBUG

        static ForceVirtualStock()
        {
            On.FistVR.FVRPhysicalObject.HasStockPos += FVRPhysicalObject_HasStockPos;
            On.FistVR.FVRPhysicalObject.GetStockPos += FVRPhysicalObject_GetStockPos;
        }

        public void Awake()
        {
            _existingForceVirtualStock.Add(PhysicalObject,this);
        }

        public void OnDestroy()
        {
            _existingForceVirtualStock.Remove(PhysicalObject);
        }

        private static Transform FVRPhysicalObject_GetStockPos(On.FistVR.FVRPhysicalObject.orig_GetStockPos orig, FVRPhysicalObject self)
        {
            if (_existingForceVirtualStock.TryGetValue(self, out ForceVirtualStock forceVirtualStock)) return forceVirtualStock.StockPos;
            else return orig(self);
        }

        private static bool FVRPhysicalObject_HasStockPos(On.FistVR.FVRPhysicalObject.orig_HasStockPos orig, FVRPhysicalObject self)
        {
            if (_existingForceVirtualStock.ContainsKey(self)) return true;
            else return orig(self);
		}
#endif
	}
}
