using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class WristItem : FVRPhysicalObject
    {
        [Header("WristItem Config")]
        public bool RequiresEmptyHand = false;
#if !(UNITY_EDITOR || UNITY_5)
        private WristQBSlot _wristQBSlot = null;
        private FVRViveHand _wristHand;
        public WristQBSlot WristQBSlot
        {
            get
            {
                return _wristQBSlot;
            }
        }

        public FVRViveHand WristHand
        {
            get
            {
                return _wristHand;
            }
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            _wristQBSlot = m_quickbeltSlot as WristQBSlot;

            if (_wristQBSlot != null)
            {
                if (RequiresEmptyHand && _wristQBSlot.Hand.CurrentInteractable != null)
                {
                    _wristHand = null;
                    return;
                }
                _wristHand = _wristQBSlot.Hand;
            }
            else _wristHand = null;

        }
#endif
    }
}
