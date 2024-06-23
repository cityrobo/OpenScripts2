using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class ScopeBaseMount : FVRFireArmAttachmentMount
    {
        [Header("ScopeBaseMount Config")]
        public MovingFireArmAttachmentInterface MovingInterface;

        [Header("Points will get deleted and replaced with TransformProxies")]
        public Transform FrontRingFront;
        public Transform FrontRingRear;
        public Transform RearRingFront;
        public Transform RearRingRear;

        [HideInInspector]
        public TransformProxy FrontRingFrontProxy;
        [HideInInspector]
        public TransformProxy FrontRingRearProxy;
        [HideInInspector]
        public TransformProxy RearRingFrontProxy;
        [HideInInspector]
        public TransformProxy RearRingRearProxy;

        private bool _wasConverted = false;

        public void Start()
        {
            if (!_wasConverted)
            {
                _wasConverted = true;
                FrontRingFrontProxy = new TransformProxy(FrontRingFront, true);
                FrontRingRearProxy = new TransformProxy(FrontRingRear, true);

                RearRingFrontProxy = new TransformProxy(RearRingFront, true);
                RearRingRearProxy = new TransformProxy(RearRingRear, true);
            }
        }

        [ContextMenu("Copy existing mounts's values")]
        public void CopyExistingMount()
        {
            FVRFireArmAttachmentMount[] attachments = GetComponents<FVRFireArmAttachmentMount>();

            FVRFireArmAttachmentMount toCopy = attachments.Single(c => c != this);

            toCopy.MyObject.AttachmentMounts.Remove(toCopy);
            toCopy.MyObject.AttachmentMounts.Add(this);
            if (toCopy.MyObject is FVRFireArmAttachment attachment)
            {
                List<FVRFireArmAttachmentMount> temp = attachment.AttachmentInterface.SubMounts.ToList();
                temp.Remove(toCopy);
                temp.Add(this);
                attachment.AttachmentInterface.SubMounts = temp.ToArray();
            }

            this.CopyComponent(toCopy);
        }
    }

#if DEBUG
    [UnityEditor.CustomEditor(typeof(ScopeBaseMount))]
    public class ScopeBaseMountEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ScopeBaseMount t = (ScopeBaseMount)target;
            DrawDefaultInspector();
            if (GUILayout.Button("Copy existing mount on this game object.")) t.CopyExistingMount();
        }
    }
#endif
}
