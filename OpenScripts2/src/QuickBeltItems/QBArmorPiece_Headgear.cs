using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class QBArmorPiece_Headgear : QBArmorPiece_Backpack
	{

        public override void Awake()
        {
            base.Awake();

            DoesQuickbeltSlotFollowHead = false;
        }

        public override void SetQuickBeltSlot(FVRQuickBeltSlot slot)
		{
			base.SetQuickBeltSlot(slot);

            if (slot != null)
            {
                this.SetParentage(GM.CurrentPlayerBody.FilteredHead);
            }
            else
            {
                this.SetParentage(null);
            }
		}
    }
}