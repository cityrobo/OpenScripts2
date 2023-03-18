using System;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using System.Linq;

namespace OpenScripts2.ModularWorkshop
{
    public class ModularRevolver : Revolver , IModularWeapon
    {
        [Header("Modular Configuration")]
        public Transform ModularBarrelPosition;
        public Transform GetModularBarrelPosition => ModularBarrelPosition;
        public GameObject[] ModularBarrelPrefabs;
        public GameObject[] GetModularBarrelPrefabs => ModularBarrelPrefabs;
        public Transform ModularHandguardPosition;
        public Transform GetModularHandguardPosition => ModularHandguardPosition;
        public GameObject[] ModularHandguardPrefabs;
        public GameObject[] GetModularHandguardPrefabs => ModularHandguardPrefabs;
        public Transform ModularStockPosition;
        public Transform GetModularStockPosition => ModularStockPosition;
        public GameObject[] ModularStockPrefabs;
        public GameObject[] GetModularStockPrefabs => ModularStockPrefabs;

        public int SelectedModularBarrel = 0;
        public int GetSelectedModularBarrel => SelectedModularBarrel;
        public int SelectedModularHandguard = 0;
        public int GetSelectedModularHandguard => SelectedModularHandguard;
        public int SelectedModularStock = 0;
        public int GetSelectedModularStock => SelectedModularStock;

        public ModularWeaponPartsAttachmentPoint[] ModularWeaponPartsAttachmentPoints;
        public ModularWeaponPartsAttachmentPoint[] GetModularWeaponPartsAttachmentPoints => ModularWeaponPartsAttachmentPoints;

        private const string c_modularBarrelKey = "ModulBarrel";
        private const string c_modularHandguardKey = "ModulHandguard";
        private const string c_modularStockKey = "ModulStock";

        public override void Awake()
        {
            base.Awake();

            ConfigureAll();
        }


        public override void ConfigureFromFlagDic(Dictionary<string, string> f)
        {
            base.ConfigureFromFlagDic(f);

            string indexString;
            if (f.TryGetValue(c_modularBarrelKey, out indexString)) ConfigureModularBarrel(int.Parse(indexString));
            if (f.TryGetValue(c_modularHandguardKey, out indexString)) ConfigureModularHandguard(int.Parse(indexString));
            if (f.TryGetValue(c_modularStockKey, out indexString)) ConfigureModularStock(int.Parse(indexString));

            foreach (var modularWeaponPartsAttachmentPoint in ModularWeaponPartsAttachmentPoints)
            {
                if (f.TryGetValue("Modul" + modularWeaponPartsAttachmentPoint.GroupName, out indexString)) ConfigureModularWeaponPart(modularWeaponPartsAttachmentPoint, int.Parse(indexString));
            }
        }

        public override Dictionary<string, string> GetFlagDic()
        {
            Dictionary<string, string> flagDic = base.GetFlagDic();

            if (ModularBarrelPrefabs.Length > 0) flagDic.Add(c_modularBarrelKey, SelectedModularBarrel.ToString());
            if (ModularHandguardPrefabs.Length > 0) flagDic.Add(c_modularHandguardKey, SelectedModularHandguard.ToString());
            if (ModularStockPrefabs.Length > 0) flagDic.Add(c_modularStockKey, SelectedModularStock.ToString());

            foreach (var modularWeaponPartsAttachmentPoint in ModularWeaponPartsAttachmentPoints)
            {
                if (modularWeaponPartsAttachmentPoint.ModularWeaponPartsPrefabs.Length > 0) flagDic.Add("Modul" + modularWeaponPartsAttachmentPoint.GroupName, modularWeaponPartsAttachmentPoint.SelectedModularWeaponPart.ToString());
            }

            return flagDic;
        }

        public void ConfigureModularWeaponPart(ModularWeaponPartsAttachmentPoint modularWeaponPartsAttachmentPoint, int index)
        {
            modularWeaponPartsAttachmentPoint.SelectedModularWeaponPart = index;
            GameObject modularWeaponPartPrefab = Instantiate(modularWeaponPartsAttachmentPoint.ModularWeaponPartsPrefabs[index], modularWeaponPartsAttachmentPoint.ModularPartPoint.position, modularWeaponPartsAttachmentPoint.ModularPartPoint.rotation, modularWeaponPartsAttachmentPoint.ModularPartPoint.parent);
            ModularWeaponPart modularWeaponPart = modularWeaponPartPrefab.GetComponent<ModularWeaponPart>();

            foreach (var mount in modularWeaponPartsAttachmentPoint.ModularPartPoint.GetComponent<ModularWeaponPart>().AttachmentMounts)
            {
                AttachmentMounts.Remove(mount);
            }
            AttachmentMounts.AddRange(modularWeaponPart.AttachmentMounts);

            Destroy(modularWeaponPartsAttachmentPoint.ModularPartPoint.gameObject);
            modularWeaponPartsAttachmentPoint.ModularPartPoint = modularWeaponPart.transform;
        }

        public void ConfigureModularBarrel(int index)
        {
            SelectedModularBarrel = index;

            GameObject modularBarrelPrefab = Instantiate(ModularBarrelPrefabs[index], ModularBarrelPosition.position, ModularBarrelPosition.rotation, ModularBarrelPosition.parent);
            
            //modularBarrelPrefab.transform.localPosition = ModularBarrelPosition.localPosition;
            //modularBarrelPrefab.transform.localRotation = ModularBarrelPosition.localRotation;
            //modularBarrelPrefab.transform.parent = modularBarrelPrefab.transform.parent;
            
            ModularBarrel modularBarrel = modularBarrelPrefab.GetComponent<ModularBarrel>();

            MuzzlePos.position = modularBarrel.MuzzlePosition.position;
            MuzzlePos.rotation = modularBarrel.MuzzlePosition.rotation;
            DefaultMuzzleState = modularBarrel.DefaultMuzzleState;

            foreach (var mount in ModularBarrelPosition.GetComponent<ModularWeaponPart>().AttachmentMounts)
            {
                AttachmentMounts.Remove(mount);
            }
            AttachmentMounts.AddRange(modularBarrel.AttachmentMounts);

            Destroy(ModularBarrelPosition.gameObject);
            ModularBarrelPosition = modularBarrelPrefab.transform;
        }
        public void ConfigureModularHandguard(int index)
        {
            SelectedModularHandguard = index;

            GameObject modularHandguardPrefab = Instantiate(ModularHandguardPrefabs[index], ModularHandguardPosition.position, ModularHandguardPosition.rotation, ModularHandguardPosition.parent);
            ModularHandguard modularHandguard = modularHandguardPrefab.GetComponent<ModularHandguard>();

            AltGrip.gameObject.SetActive(modularHandguard.ActsLikeForeGrip);
            Collider grabTrigger = AltGrip.GetComponent<Collider>();
            switch (grabTrigger)
            {
                case BoxCollider c:
                    if (modularHandguard.IsTriggerComponentPosition)
                    {
                        c.center = modularHandguard.AltGripTriggerGameObjectPosition;
                    }
                    else
                    {
                        AltGrip.transform.localPosition = modularHandguard.AltGripTriggerGameObjectPosition;
                    }

                    if (modularHandguard.IsTriggerComponentSize)
                    {
                        c.size = modularHandguard.AltGripTriggerGameObjectScale;
                    }
                    else
                    {
                        AltGrip.transform.localScale = modularHandguard.AltGripTriggerGameObjectScale;
                    }
                    break;
                case CapsuleCollider c:
                    if (modularHandguard.IsTriggerComponentPosition)
                    {
                        c.center = modularHandguard.AltGripTriggerGameObjectPosition;
                    }
                    else
                    {
                        AltGrip.transform.localPosition = modularHandguard.AltGripTriggerGameObjectPosition;
                    }

                    if (modularHandguard.IsTriggerComponentSize)
                    {
                        c.radius = modularHandguard.AltGripTriggerGameObjectScale.x;
                        c.height = modularHandguard.AltGripTriggerGameObjectScale.y;
                    }
                    else
                    {
                        AltGrip.transform.localScale = modularHandguard.AltGripTriggerGameObjectScale;
                    }
                    break;
            }
            AltGrip.transform.localRotation = Quaternion.Euler(modularHandguard.AltGripTriggerGameObjectRotation);

            foreach (var mount in ModularHandguardPosition.GetComponent<ModularWeaponPart>().AttachmentMounts)
            {
                AttachmentMounts.Remove(mount);
            }
            AttachmentMounts.AddRange(modularHandguard.AttachmentMounts);

            Destroy(ModularHandguardPosition.gameObject);
            ModularHandguardPosition = modularHandguardPrefab.transform;
        }
        public void ConfigureModularStock(int index)
        {
            SelectedModularStock = index;

            GameObject modularStockPrefab = Instantiate(ModularStockPrefabs[index], ModularStockPosition.position, ModularStockPosition.rotation, ModularStockPosition.parent);
            ModularStock modularStock = modularStockPrefab.GetComponent<ModularStock>();

            HasActiveShoulderStock = modularStock.ActsLikeStock;
            StockPos = modularStock.StockPoint;
            if (modularStock.CollapsingStock != null) modularStock.CollapsingStock.Firearm = this;
            if (modularStock.FoldingStockX != null) modularStock.FoldingStockX.FireArm = this;
            if (modularStock.FoldingStockY != null) modularStock.FoldingStockY.FireArm = this;

            foreach (var mount in ModularStockPosition.GetComponent<ModularWeaponPart>().AttachmentMounts)
            {
                AttachmentMounts.Remove(mount);
            }
            AttachmentMounts.AddRange(modularStock.AttachmentMounts);

            Destroy(ModularStockPosition.gameObject);
            ModularStockPosition = modularStockPrefab.transform;
        }

        public void ConfigureAll()
        {
            if (ModularBarrelPrefabs.Length > 0) ConfigureModularBarrel(SelectedModularBarrel);
            OpenScripts2_BepInExPlugin.Log(this, $"Configured with {ModularBarrelPrefabs.Length} Barrels.");
            if (GetModularHandguardPrefabs.Length > 0) ConfigureModularHandguard(SelectedModularHandguard);
            if (ModularStockPrefabs.Length > 0) ConfigureModularStock(SelectedModularStock);
            foreach (ModularWeaponPartsAttachmentPoint attachmentPoint in ModularWeaponPartsAttachmentPoints)
            {
                if (attachmentPoint.ModularWeaponPartsPrefabs.Length > 0) ConfigureModularWeaponPart(attachmentPoint, attachmentPoint.SelectedModularWeaponPart);
                OpenScripts2_BepInExPlugin.Log(this, $"Configured with {attachmentPoint.ModularWeaponPartsPrefabs.Length} {attachmentPoint.GroupName}.");
            }
        }

        [ContextMenu("Copy Existing Firearm Component")]
        public void CopyFirearm()
        {
            Revolver[] attachments = GetComponents<Revolver>();
            Revolver toCopy = attachments.Single(c => c != this);
            toCopy.Cylinder.Revolver = this;

            foreach (var mount in toCopy.AttachmentMounts)
            {
                mount.MyObject = this;
                mount.Parent = this;
            }

            this.CopyComponent(toCopy);
        }
    }
}
