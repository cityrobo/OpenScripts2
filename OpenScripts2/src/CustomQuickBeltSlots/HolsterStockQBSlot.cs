using FistVR;
using OpenScripts2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class HolsterStockQBSlot : FVRQuickBeltSlot
	{
        [Header("Holster Config")]
        public FVRPhysicalObject PhysicalObject;

        public Transform StockCap;
        public OpenScripts2_BasePlugin.Axis CapAxis;
        public float CapClosed;
        public float CapOpen;

        private static readonly List<FVRQuickBeltSlot> _existingHolsterStockQBSlots = new();

        private const string DEFAULT_LAYER = "Default";
        private const string INTERACTABLE_LAYER = "Interactable";

        public void Start()
        {
            _existingHolsterStockQBSlots.Add(this);
        }

        public void OnDestroy()
        {
            _existingHolsterStockQBSlots.Remove(this);
        }

#if !DEBUG
        static HolsterStockQBSlot()
        {
            On.FistVR.FVRQuickBeltSlot.Update += FVRQuickBeltSlot_Update;
        }

        private static void FVRQuickBeltSlot_Update(On.FistVR.FVRQuickBeltSlot.orig_Update orig, FVRQuickBeltSlot self)
        {
            orig(self);

            if (self is HolsterStockQBSlot holsterStockQBSlot && _existingHolsterStockQBSlots.Contains(self))
            {
                if (holsterStockQBSlot.HeldObject != null && holsterStockQBSlot.PhysicalObject.QuickbeltSlot != null)
                {
                    holsterStockQBSlot.PhysicalObject.gameObject.layer = LayerMask.NameToLayer(DEFAULT_LAYER);
                    holsterStockQBSlot.gameObject.layer = LayerMask.NameToLayer(INTERACTABLE_LAYER);
                    holsterStockQBSlot.PhysicalObject.QuickbeltSlot.IsSelectable = false;
                }
                else if (holsterStockQBSlot.HeldObject == null && holsterStockQBSlot.PhysicalObject.QuickbeltSlot != null)
                {
                    holsterStockQBSlot.PhysicalObject.gameObject.layer = LayerMask.NameToLayer(INTERACTABLE_LAYER);
                    holsterStockQBSlot.gameObject.layer = LayerMask.NameToLayer(INTERACTABLE_LAYER);
                    holsterStockQBSlot.PhysicalObject.QuickbeltSlot.IsSelectable = true;
                }
                else holsterStockQBSlot.gameObject.layer = LayerMask.NameToLayer(INTERACTABLE_LAYER);

                holsterStockQBSlot.IsSelectable = holsterStockQBSlot.CapAxis switch
                {
                    OpenScripts2_BasePlugin.Axis.X => holsterStockQBSlot.StockCap.localRotation == Quaternion.Euler(holsterStockQBSlot.CapOpen, 0f, 0f),
                    OpenScripts2_BasePlugin.Axis.Y => holsterStockQBSlot.StockCap.localRotation == Quaternion.Euler(0f, holsterStockQBSlot.CapOpen, 0f),
                    OpenScripts2_BasePlugin.Axis.Z => holsterStockQBSlot.StockCap.localRotation == Quaternion.Euler(0f, 0f, holsterStockQBSlot.CapOpen),
                    _ => false,
                };
            }
        }
#endif
    }
}
