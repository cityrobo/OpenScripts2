using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class MultipleHideOnAttach : OpenScripts2_BasePlugin
    {
        [Header("Mount to monitor for attachments:")]
        public FVRFireArmAttachmentMount attachmentMount;

        [Header("List of GameObjects to affect:")]
        public List<GameObject> ObjectToHideOrShow;

        [Header("Enable to show Objects instead:")]
        public bool ShowOnAttach = false;

        [Header("Only enabled for specific attachment IDs")]
        public List<string> AttachmentIds = new();

        public void Awake()
        {
            attachmentMount.HasHoverDisablePiece = true;
            if (attachmentMount.DisableOnHover == null || ObjectToHideOrShow.Contains(attachmentMount.DisableOnHover))
            {
                attachmentMount.DisableOnHover = new GameObject("MultipleHideOnAttach_Proxy");
            }
        }

        public void Update()
        {
            if (AttachmentIds.Count == 0)
            {
                if (attachmentMount.DisableOnHover.activeInHierarchy == false)
                {
                    foreach (GameObject gameObject in ObjectToHideOrShow)
                    {
                        gameObject.SetActive(ShowOnAttach);
                    }
                }
                else
                {
                    foreach (GameObject gameObject in ObjectToHideOrShow)
                    {
                        gameObject.SetActive(!ShowOnAttach);
                    }
                }
            }
            else
            {
                bool correctIDFound = false;
                foreach (FVRFireArmAttachment attachment in attachmentMount.AttachmentsList)
                {
                    if (AttachmentIds.Contains(attachment.ObjectWrapper.ItemID)) correctIDFound = true;
                }

                if (correctIDFound)
                {
                    foreach (GameObject gameObject in ObjectToHideOrShow)
                    {
                        gameObject.SetActive(ShowOnAttach);
                    }
                }
                else
                {
                    foreach (GameObject gameObject in ObjectToHideOrShow)
                    {
                        gameObject.SetActive(!ShowOnAttach);
                    }
                }
            }
        }
    }
}
