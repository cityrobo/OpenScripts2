using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class QBArmorPiece : FVRPhysicalObject
    {
		[Header("QBArmorPiece Config")]
		public string MainItemLayerNameInsideQBSlot = "Default";
		public string MainItemLayerNameOutsideQBSlot = "Default";

		public string SubAttachmentLayerNameInsideQBSlot = "Default";
		public string SubAttachmentLayerNameOutsideQBSlot = "Default";

		public GameObject IsEnabledInQB;
		public GameObject IsDisabledInQB;

		private int _attachmentCountOnQBSlotEnter;
		public override void SetQuickBeltSlot(FVRQuickBeltSlot slot)
		{
			if (slot != null && !IsHeld)
			{
				if (AttachmentsList.Count > 0)
				{
					for (int i = 0; i < AttachmentsList.Count; i++)
					{
						if (AttachmentsList[i] != null)
						{
							AttachmentsList[i].SetAllCollidersToLayer(false, SubAttachmentLayerNameInsideQBSlot);
						}
					}

					_attachmentCountOnQBSlotEnter = AttachmentsList.Count;
				}
			}
			else if (AttachmentsList.Count > 0)
			{
				for (int j = 0; j < AttachmentsList.Count; j++)
				{
					if (AttachmentsList[j] != null)
					{
						AttachmentsList[j].SetAllCollidersToLayer(false, SubAttachmentLayerNameOutsideQBSlot);
					}
				}
				_attachmentCountOnQBSlotEnter = AttachmentsList.Count;
			}
			if (m_quickbeltSlot != null && slot != m_quickbeltSlot)
			{
				m_quickbeltSlot.HeldObject = null;
				m_quickbeltSlot.CurObject = null;
				m_quickbeltSlot.IsKeepingTrackWithHead = false;
			}
			if (slot != null && !IsHeld)
			{
				SetAllCollidersToLayer(false, MainItemLayerNameInsideQBSlot);
				slot.HeldObject = this;
				slot.CurObject = this;
				slot.IsKeepingTrackWithHead = DoesQuickbeltSlotFollowHead;

                if (IsDisabledInQB != null)
                {
					IsDisabledInQB.SetActive(false);
                }

				if (IsEnabledInQB != null)
				{
					IsEnabledInQB.SetActive(true);
				}
			}
			else
			{
				SetAllCollidersToLayer(false, MainItemLayerNameOutsideQBSlot);

				if (IsDisabledInQB != null)
				{
					IsDisabledInQB.SetActive(true);
				}

				if (IsEnabledInQB != null)
				{
					IsEnabledInQB.SetActive(false);
				}
			}
			m_quickbeltSlot = slot;
		}

		public override void FVRUpdate()
		{
			base.FVRUpdate();

			if (m_quickbeltSlot != null)
			{
				if (AttachmentsList.Count > _attachmentCountOnQBSlotEnter)
				{
					if (AttachmentsList[_attachmentCountOnQBSlotEnter] != null)
					{
						AttachmentsList[_attachmentCountOnQBSlotEnter].SetAllCollidersToLayer(false, SubAttachmentLayerNameInsideQBSlot);
					}
					_attachmentCountOnQBSlotEnter = AttachmentsList.Count;
				}
			}
		}
	}
}