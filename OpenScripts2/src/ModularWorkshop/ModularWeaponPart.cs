using System;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2.ModularWorkshop
{
    public class ModularWeaponPart : MonoBehaviour
    {
        public string Name;
        public Sprite Icon;
        public FVRFireArmAttachmentMount[] AttachmentMounts;

        private Transform[] childObjects;

        public virtual void Awake()
        {
            childObjects = this.GetComponentsInDirectChildren<Transform>(true);

            foreach (Transform child in childObjects)
            {
                child.parent = transform.parent;
            }
        }

        public virtual void OnDestroy()
        {
            foreach (Transform child in childObjects)
            {
                Destroy(child.gameObject);
            }
        }
    }
    [Serializable]
    public class ModularWeaponPartsAttachmentPoint
    {
        public string GroupName;
        public Transform ModularPartPoint;
        public GameObject[] ModularWeaponPartsPrefabs;
        public int SelectedModularWeaponPart;
    }
}
