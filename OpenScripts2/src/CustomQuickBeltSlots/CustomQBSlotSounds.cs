using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    [RequireComponent(typeof(FVRQuickBeltSlot))]
    public class CustomQBSlotSounds : OpenScripts2_BasePlugin
    {
		public AudioEvent InsertSounds;
		public AudioEvent ExtractSounds;

		private FVRQuickBeltSlot _slot;
		private bool _slotHasItem = false;

        private static readonly Dictionary<FVRQuickBeltSlot, CustomQBSlotSounds> _existingCustomQBSlotSounds = new();

        public void Start()
        {
			_slot = gameObject.GetComponent<FVRQuickBeltSlot>();
            _slotHasItem = false;

            _existingCustomQBSlotSounds.Add(_slot, this);
        }

        public void OnDestroy()
        {
            _existingCustomQBSlotSounds.Remove(_slot);
        }

		public void Update()
        {
            if (!_slotHasItem && (_slot.HeldObject != null || _slot.CurObject != null))
            {
                _slotHasItem = true;
                SM.PlayGenericSound(InsertSounds, _slot.transform.position);
            }
            else if (_slotHasItem && (_slot.HeldObject == null && _slot.CurObject == null))
            {
                _slotHasItem = false;
                SM.PlayGenericSound(ExtractSounds, _slot.transform.position);
            }
        }

#if !DEBUG
        static CustomQBSlotSounds()
        {
            On.FistVR.FVRPhysicalObject.DuplicateFromSpawnLock += FVRPhysicalObject_DuplicateFromSpawnLock;
        }

        private static GameObject FVRPhysicalObject_DuplicateFromSpawnLock(On.FistVR.FVRPhysicalObject.orig_DuplicateFromSpawnLock orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            GameObject temp = orig(self, hand);

            if (_existingCustomQBSlotSounds.TryGetValue(self.QuickbeltSlot, out CustomQBSlotSounds sounds))
            {
                SM.PlayGenericSound(sounds.ExtractSounds, sounds.transform.position);
            }
            return temp;
        }
#endif
    }
}
