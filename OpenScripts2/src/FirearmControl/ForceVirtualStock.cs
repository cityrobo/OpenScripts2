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
		public Transform StockPos;

#if !MEATKIT
		public void Awake()
        {
            On.FistVR.FVRPhysicalObject.HasStockPos += FVRPhysicalObject_HasStockPos;
            On.FistVR.FVRPhysicalObject.GetStockPos += FVRPhysicalObject_GetStockPos;
        }
        
        public void OnDestroy()
        {
            On.FistVR.FVRPhysicalObject.HasStockPos -= FVRPhysicalObject_HasStockPos;
            On.FistVR.FVRPhysicalObject.GetStockPos -= FVRPhysicalObject_GetStockPos;
        }

        private Transform FVRPhysicalObject_GetStockPos(On.FistVR.FVRPhysicalObject.orig_GetStockPos orig, FVRPhysicalObject self)
        {
			return this.StockPos;
		}

        private bool FVRPhysicalObject_HasStockPos(On.FistVR.FVRPhysicalObject.orig_HasStockPos orig, FVRPhysicalObject self)
        {
			return true;
		}
#endif
	}
}
