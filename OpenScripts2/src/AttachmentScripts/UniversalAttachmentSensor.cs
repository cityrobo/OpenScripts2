using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class UniversalAttachmentSensor : FVRFireArmAttachmentSensor
    {
        [Header("Universal Attachment Sensor Config")]
        public bool UsesMountTypeWhitelist = false;
        public List<FVRFireArmAttachementMountType> MountTypeWhitelist = [];
        public bool UsesMountTypeBlacklist = false;
        public List<FVRFireArmAttachementMountType> MountTypeBlacklist = [];

        [ContextMenu("Copy existing sensor")]
        public void CopySensor()
        {
            FVRFireArmAttachmentSensor sensor = GetComponent<FVRFireArmAttachmentSensor>();
            if (sensor != null)
            {
                Attachment = sensor.Attachment;
                Attachment.Sensor = this;
            }
        }

#if !DEBUG
        static UniversalAttachmentSensor()
        {
            On.FistVR.FVRFireArmAttachmentSensor.OnTriggerEnter += FVRFireArmAttachmentSensor_OnTriggerEnter;
        }

        private static void FVRFireArmAttachmentSensor_OnTriggerEnter(On.FistVR.FVRFireArmAttachmentSensor.orig_OnTriggerEnter orig, FVRFireArmAttachmentSensor self, Collider collider)
        {
            orig(self, collider);
            if (self.Attachment.IsHeld && self.CurHoveredMount == null && self.Attachment.CanAttach() && collider.gameObject.tag == "FVRFireArmAttachmentMount")
            {
                bool canMount;
                UniversalMount universalMount = collider.GetComponent<UniversalMount>();
                if (universalMount == null)
                {
                    OpenScripts2_BepInExPlugin.Log(self, $"Mount tagged trigger \"{collider}\" has no mount component on it!");
                    return;
                }

                if (universalMount.UsesMountTypeWhitelist)
                {
                    canMount = universalMount.MountTypeWhitelist.Contains(self.Attachment.Type);
                }
                else if (universalMount.UsesMountTypeBlacklist)
                {
                    canMount = !universalMount.MountTypeBlacklist.Contains(self.Attachment.Type);
                }
                else
                {
                    canMount = true;
                }

                if (canMount && universalMount.isMountableOn(self.Attachment))
                {
                    self.SetHoveredMount(universalMount);
                    universalMount.BeginHover();
                }
            }
        }
#endif
    }
#if DEBUG
    [UnityEditor.CustomEditor(typeof(UniversalAttachmentSensor))]
    public class UniversalAttachmentSensorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UniversalAttachmentSensor t = (UniversalAttachmentSensor)target;
            DrawDefaultInspector();
            if (GUILayout.Button("Copy existing sensor on this game object.")) t.CopySensor();
        }
    }
#endif
}