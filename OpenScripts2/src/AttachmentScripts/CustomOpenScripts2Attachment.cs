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
                case AdvancedMovingFireArmAttachmentInterface i:
                    AdvancedMovingFireArmAttachmentInterfaceFlagDic(flagDic, true, i);
                    break;
            }

            SkinChanger skinChanger = GetComponent<SkinChanger>();
            if (skinChanger != null) SkinChangerFlagDic(flagDic, true, skinChanger);

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
                case AdvancedMovingFireArmAttachmentInterface i:
                    AdvancedMovingFireArmAttachmentInterfaceFlagDic(f, false, i);
                    break;
            }

            SkinChanger skinChanger = GetComponent<SkinChanger>();
            if (skinChanger != null) SkinChangerFlagDic(f, false, skinChanger);
        }

        private void MovingFireArmAttachmentInterfaceFlagDic(Dictionary<string,string> flagDic, bool save, MovingFireArmAttachmentInterface i)
        {
            string value;
            if (save)
            {
                value = i.transform.localPosition.ToString("F6").Replace(" ", "").Replace("(", "").Replace(")", "");
                flagDic.Add(MovingFireArmAttachmentInterface.POSITION_FLAGDIC_KEY, value);

                value = i.transform.localRotation.ToString("F6").Replace(" ", "").Replace("(", "").Replace(")", "");
                flagDic.Add(MovingFireArmAttachmentInterface.ROTATION_FLAGDIC_KEY, value);

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

                if (flagDic.TryGetValue(MovingFireArmAttachmentInterface.ROTATION_FLAGDIC_KEY, out value))
                {
                    split = value.Split(',');
                    i.transform.localRotation = new Quaternion(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
                }

                if (flagDic.TryGetValue(MovingFireArmAttachmentInterface.SECONDARY_POSITION_FLAGDIC_KEY, out value) && i.SecondaryPiece != null)
                {
                    split = value.Split(',');
                    i.SecondaryPiece.localPosition = new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
                }
            }
        }

        private void AdvancedMovingFireArmAttachmentInterfaceFlagDic(Dictionary<string, string> flagDic, bool save, AdvancedMovingFireArmAttachmentInterface i)
        {
            string value;
            if (save)
            {
                value = i.transform.localPosition.ToString("F6").Replace(" ", "").Replace("(", "").Replace(")", "");
                flagDic.Add(AdvancedMovingFireArmAttachmentInterface.POSITION_FLAGDIC_KEY, value);

                value = i.transform.localRotation.ToString("F6").Replace(" ", "").Replace("(", "").Replace(")", "");
                flagDic.Add(AdvancedMovingFireArmAttachmentInterface.ROTATION_FLAGDIC_KEY, value);

                if (i.SecondaryPiece != null)
                {
                    value = i.SecondaryPiece.localPosition.ToString("F6").Replace(" ", "").Replace("(", "").Replace(")", "");
                    flagDic.Add(AdvancedMovingFireArmAttachmentInterface.SECONDARY_POSITION_FLAGDIC_KEY, value);
                }

                if (i.IsPinned)
                {
                    value = i.GetPinTargetPath();
                    flagDic.Add(AdvancedMovingFireArmAttachmentInterface.PIN_TRANFORM_PATH_FLAGDIC_KEY, value);
                }
            }
            else if (flagDic.TryGetValue(AdvancedMovingFireArmAttachmentInterface.POSITION_FLAGDIC_KEY, out value))
            {
                string[] split = value.Split(',');
                i.transform.localPosition = new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));

                if (flagDic.TryGetValue(AdvancedMovingFireArmAttachmentInterface.ROTATION_FLAGDIC_KEY, out value))
                {
                    split = value.Split(',');
                    i.transform.localRotation = new Quaternion(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
                }

                if (flagDic.TryGetValue(AdvancedMovingFireArmAttachmentInterface.SECONDARY_POSITION_FLAGDIC_KEY, out value) && i.SecondaryPiece != null)
                {
                    split = value.Split(',');
                    i.SecondaryPiece.localPosition = new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
                }

                if (flagDic.TryGetValue(AdvancedMovingFireArmAttachmentInterface.PIN_TRANFORM_PATH_FLAGDIC_KEY, out value))
                {
                    i.UnvaultingPinTransformPath = value;
                }
            }
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

#if DEBUG
    [UnityEditor.CustomEditor(typeof(CustomOpenScripts2Attachment))]
    public class CustomOpenScripts2AttachmentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CustomOpenScripts2Attachment t = (CustomOpenScripts2Attachment)target;
            DrawDefaultInspector();
            if (GUILayout.Button("Copy existing attachment on this game object.")) t.CopyAttachment();
        }
    }
#endif
}
