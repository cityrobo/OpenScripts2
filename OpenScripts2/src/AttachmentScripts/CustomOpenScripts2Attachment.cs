using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class CustomOpenScripts2Attachment : FVRFireArmAttachment
    {

        public override void Awake()
        {
            base.Awake();

            if (AttachmentInterface.gameObject.activeSelf && curMount == null) AttachmentInterface.gameObject.SetActive(false);
        }
        public override Dictionary<string, string> GetFlagDic()
        {
            Dictionary<string, string> flagDic = base.GetFlagDic();

            switch (AttachmentInterface)
            {
                case CustomReflexSightInterface i:
                    break;
                case CustomScopeInterface i:
                    break;
                case CustomLinearZoomScopeInterface i:
                    break;
                case MovingFireArmAttachmentInterface i:
                    MovingFireArmAttachmentInterfaceFlagDic(flagDic, true, i);
                    break;
            }

            return flagDic;
        }

        public override void ConfigureFromFlagDic(Dictionary<string, string> f)
        {
            base.ConfigureFromFlagDic(f);

            switch (AttachmentInterface)
            {
                case CustomReflexSightInterface i:
                    break;
                case CustomScopeInterface i:
                    break;
                case CustomLinearZoomScopeInterface i:
                    break;
                case MovingFireArmAttachmentInterface i:
                    MovingFireArmAttachmentInterfaceFlagDic(f, false, i);
                    break;
            }
        }

        private void MovingFireArmAttachmentInterfaceFlagDic(Dictionary<string,string> flagDic, bool save, MovingFireArmAttachmentInterface i)
        {
            string value;
            if (save)
            {
                value = i.transform.localPosition.ToString("F6").Replace(" ", "").Replace("(", "").Replace(")", "");
                flagDic.Add(MovingFireArmAttachmentInterface.POSITION_FLAGDIC_KEY, value);

                if (i.SecondaryPiece != null)
                {
                    value = i.SecondaryPiece.localPosition.ToString("F6").Replace(" ", "").Replace("(", "").Replace(")", "");
                    flagDic.Add(MovingFireArmAttachmentInterface.SECONDARY_POSITION_FLAGDIC_KEY, value);
                }
            }
            else if (flagDic.TryGetValue(MovingFireArmAttachmentInterface.POSITION_FLAGDIC_KEY, out value))
            {
                string[] split = value.Split(',');
                i.transform.localPosition = new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));

                if (flagDic.TryGetValue(MovingFireArmAttachmentInterface.SECONDARY_POSITION_FLAGDIC_KEY, out value) && i.SecondaryPiece != null)
                {
                    split = value.Split(',');
                    i.SecondaryPiece.localPosition = new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
                }
            }
        }

        [ContextMenu("Copy existing Attachment's values")]
        public void CopyAttachment()
        {
            FVRFireArmAttachment[] attachments = GetComponents<FVRFireArmAttachment>();

            FVRFireArmAttachment toCopy = attachments.Single(c => c != this);

            toCopy.AttachmentInterface.Attachment = this;
            toCopy.Sensor.Attachment = this;

            this.CopyComponent(toCopy);
        }
    }
}
