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
#if !(UNITY_EDITOR || UNITY_5)
		public override void SetQuickBeltSlot(FVRQuickBeltSlot slot)
		{
			if (slot != null && !base.IsHeld)
			{
				if (this.AttachmentsList.Count > 0)
				{
					for (int i = 0; i < this.AttachmentsList.Count; i++)
					{
						if (this.AttachmentsList[i] != null)
						{
							this.AttachmentsList[i].SetAllCollidersToLayer(false, SubAttachmentLayerNameInsideQBSlot);
						}
					}

					_attachmentCountOnQBSlotEnter = AttachmentsList.Count;
				}
			}
			else if (this.AttachmentsList.Count > 0)
			{
				for (int j = 0; j < this.AttachmentsList.Count; j++)
				{
					if (this.AttachmentsList[j] != null)
					{
						this.AttachmentsList[j].SetAllCollidersToLayer(false, SubAttachmentLayerNameOutsideQBSlot);
					}
				}
				_attachmentCountOnQBSlotEnter = AttachmentsList.Count;
			}
			if (this.m_quickbeltSlot != null && slot != this.m_quickbeltSlot)
			{
				this.m_quickbeltSlot.HeldObject = null;
				this.m_quickbeltSlot.CurObject = null;
				this.m_quickbeltSlot.IsKeepingTrackWithHead = false;
			}
			if (slot != null && !base.IsHeld)
			{
				base.SetAllCollidersToLayer(false, MainItemLayerNameInsideQBSlot);
				slot.HeldObject = this;
				slot.CurObject = this;
				slot.IsKeepingTrackWithHead = this.DoesQuickbeltSlotFollowHead;

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
				base.SetAllCollidersToLayer(false, MainItemLayerNameOutsideQBSlot);

				if (IsDisabledInQB != null)
				{
					IsDisabledInQB.SetActive(true);
				}

				if (IsEnabledInQB != null)
				{
					IsEnabledInQB.SetActive(false);
				}
			}
			this.m_quickbeltSlot = slot;
		}

		public override void FVRUpdate()
		{
			base.FVRUpdate();

			if (m_quickbeltSlot != null)
			{
				if (this.AttachmentsList.Count > _attachmentCountOnQBSlotEnter)
				{
					if (this.AttachmentsList[_attachmentCountOnQBSlotEnter] != null)
					{
						this.AttachmentsList[_attachmentCountOnQBSlotEnter].SetAllCollidersToLayer(false, SubAttachmentLayerNameInsideQBSlot);
					}
					_attachmentCountOnQBSlotEnter = this.AttachmentsList.Count;
				}
			}
		}
#endif
	}
}