using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{	
    public class QuickBeltAreaCollisionDetector : OpenScripts2_BasePlugin
	{
        public QuickBeltArea ConnectedQuickBeltArea;

        [HideInInspector]
        public FVRPhysicalObject PhysicalObjectToDetect;
        public void OnCollisionEnter(Collision col)
        {
            if (col == null || col.collider == null || col.collider.attachedRigidbody == null) return;
            FVRPhysicalObject physicalObject = col.collider.attachedRigidbody.GetComponent<FVRPhysicalObject>();

            if (physicalObject != null && physicalObject == PhysicalObjectToDetect)
            {
                ConnectedQuickBeltArea.ItemDidCollide = true;
                PhysicalObjectToDetect = null;
            }
        }
    }
}
