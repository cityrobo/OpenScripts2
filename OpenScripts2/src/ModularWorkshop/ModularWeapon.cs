using System;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using System.Linq;

namespace OpenScripts2.ModularWorkshop
{
    public interface IModularWeapon
    {
        public Transform GetModularBarrelPosition { get; }
        public GameObject[] GetModularBarrelPrefabs { get; }
        public Transform GetModularHandguardPosition { get; }
        public GameObject[] GetModularHandguardPrefabs { get; }
        public Transform GetModularStockPosition { get; }
        public GameObject[] GetModularStockPrefabs { get; }

        public int GetSelectedModularBarrel { get; }
        public int GetSelectedModularHandguard { get; }
        public int GetSelectedModularStock { get; }

public ModularWeaponPartsAttachmentPoint[] GetModularWeaponPartsAttachmentPoints { get; }

        public void ConfigureModularWeaponPart(ModularWeaponPartsAttachmentPoint modularWeaponPartsAttachmentPoint, int index);

        public void ConfigureModularBarrel(int index);
        public void ConfigureModularHandguard(int index);

        public void ConfigureModularStock(int index);
    }
}
