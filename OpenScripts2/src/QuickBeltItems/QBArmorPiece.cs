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
						AttachmentsList[i]?.SetAllCollidersToLayer(false, SubAttachmentLayerNameInsideQBSlot);
					}

					_attachmentCountOnQBSlotEnter = AttachmentsList.Count;
				}
			}
			else if (AttachmentsList.Count > 0)
			{
				for (int j = 0; j < AttachmentsList.Count; j++)
				{
					AttachmentsList[j]?.SetAllCollidersToLayer(false, SubAttachmentLayerNameOutsideQBSlot);
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

                IsDisabledInQB?.SetActive(false);

				IsEnabledInQB?.SetActive(true);
			}
			else
			{
				SetAllCollidersToLayer(false, MainItemLayerNameOutsideQBSlot);

				IsDisabledInQB?.SetActive(true);

				IsEnabledInQB?.SetActive(false);
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
					AttachmentsList[_attachmentCountOnQBSlotEnter]?.SetAllCollidersToLayer(false, SubAttachmentLayerNameInsideQBSlot);
					_attachmentCountOnQBSlotEnter = AttachmentsList.Count;
				}
			}
		}

        public override Dictionary<string, string> GetFlagDic()
        {
            Dictionary<string, string> flagDic = base.GetFlagDic();

            SkinChanger skinChanger = GetComponentInChildren<SkinChanger>();
            if (skinChanger != null) SkinChangerFlagDic(flagDic, true, skinChanger);

            return flagDic;
        }

        public override void ConfigureFromFlagDic(Dictionary<string, string> f)
        {
            base.ConfigureFromFlagDic(f);

            SkinChanger skinChanger = GetComponentInChildren<SkinChanger>();
            if (skinChanger != null) SkinChangerFlagDic(f, false, skinChanger);
        }
        private void SkinChangerFlagDic(Dictionary<string, string> flagDic, bool save, SkinChanger i)
        {
            string value;
            if (save)
            {
                value = i.CurrentSkinIndex.ToString();
                flagDic.Add(SkinChanger.SKINFLAGDICKEY, value);
            }
            else if (flagDic.TryGetValue(SkinChanger.SKINFLAGDICKEY, out value))
            {
                int skinIndex = int.Parse(value);
                i.SelectSkin(skinIndex);
            }
        }
    }
}