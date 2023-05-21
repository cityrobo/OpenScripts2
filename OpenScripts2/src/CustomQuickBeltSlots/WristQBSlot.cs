using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class WristQBSlot : FVRQuickBeltSlot
    {
        [Header("WristQBSlot Config")]
		public Vector3 WristOffsetPosition;
        public Vector3 WristOffsetRotation;

        public enum EWrist
        {
			leftWrist,
			rightWrist
        }

		[SearchableEnum]
		public EWrist Wrist;

        [ContextMenu("CopyQBSlot")]
        public void CopyQBSlot()
        {
            this.CopyComponent(GetComponent<FVRQuickBeltSlot>());
        }

        public FVRViveHand Hand => m_hand;

        private FVRViveHand m_hand;

		public void Start()
        {
            if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.LeftHand != null && GM.CurrentPlayerBody.RightHand != null)
            {
                switch (Wrist)
                {
                    case EWrist.leftWrist:
                        transform.SetParent(GM.CurrentPlayerBody.LeftHand);
                        m_hand = !GM.CurrentMovementManager.Hands[0].IsThisTheRightHand ? GM.CurrentMovementManager.Hands[0] : GM.CurrentMovementManager.Hands[1];
                        break;
                    case EWrist.rightWrist:
                        transform.SetParent(GM.CurrentPlayerBody.RightHand);
                        m_hand = GM.CurrentMovementManager.Hands[0].IsThisTheRightHand ? GM.CurrentMovementManager.Hands[0] : GM.CurrentMovementManager.Hands[1];
                        break;
                    default:
                        break;
                }
            }

            transform.localPosition = WristOffsetPosition;
            transform.localRotation = Quaternion.Euler(WristOffsetRotation);
        }
	}
}
