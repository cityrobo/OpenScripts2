using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    [RequireComponent(typeof(FVRQuickBeltSlot))]
    public class CustomQBSlotSounds : OpenScripts2_BasePlugin
    {
		public AudioEvent InsertSounds;
		public AudioEvent ExtractSounds;

		private FVRQuickBeltSlot _slot;
		private bool _slotHasItem = false;
        private bool _isHooked = false;
		public void Start()
        {
			_slot = gameObject.GetComponent<FVRQuickBeltSlot>();
            _slotHasItem = false;
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

            if (!_isHooked && _slotHasItem && _slot.CurObject.m_isSpawnLock == true)
            {
                Hook();
                _isHooked = true;
            }
            else if (_isHooked && (!_slotHasItem || _slot.CurObject.m_isSpawnLock == false))
            {
                Unhook();
                _isHooked = false;
            }
        }

        void Unhook()
        {
            On.FistVR.FVRPhysicalObject.DuplicateFromSpawnLock -= FVRPhysicalObject_DuplicateFromSpawnLock;
        }

        void Hook()
        {
            On.FistVR.FVRPhysicalObject.DuplicateFromSpawnLock += FVRPhysicalObject_DuplicateFromSpawnLock;
        }

        private GameObject FVRPhysicalObject_DuplicateFromSpawnLock(On.FistVR.FVRPhysicalObject.orig_DuplicateFromSpawnLock orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            GameObject temp = orig(self, hand);
            if (self == _slot.CurObject || self == _slot.HeldObject)
            {
                SM.PlayGenericSound(ExtractSounds, _slot.transform.position);
            }
            return temp;
        }
    }
}
