using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;
namespace OpenScripts2
{
    public class MultipleHideOnAttachForAttachments : OpenScripts2_BasePlugin
    {
        [Header("Mount to monitor for attachments:")]
        public FVRFireArmAttachment Attachment;

        [Header("List of GameObjects to affect:")]
        public List<GameObject> ObjectToHideOrShow;

        [Header("Enable to show Objects instead:")]
        public bool ShowOnAttach = false;

        public void Awake()
        {
        }
        public void Update()
        {

            if (Attachment.Sensor.CurHoveredMount != null)
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
