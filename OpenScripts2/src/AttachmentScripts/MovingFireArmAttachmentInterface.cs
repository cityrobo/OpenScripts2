using FistVR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class MovingFireArmAttachmentInterface : FVRFireArmAttachmentInterface
    {
        [Header("Moving FireArm Attachment Interface Config")]

        [Tooltip("One degree means linear movement, two degrees means movement on a plane, three degrees free spacial movement.")]
        public EDegreesOfFreedom DegreesOfFreedom;
        public enum EDegreesOfFreedom
        {
            Linear,
            Planar,
            Spacial
        }
        public OpenScripts2_BasePlugin.Axis LimitingAxis;
        public Vector2 XLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 YLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 ZLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);

        public bool OverridesDisableOnHoverOfMount = false;
        [Tooltip("Something placed on this mount will disabled the hover on disable piece again.")]
        public FVRFireArmAttachmentMount OverrideDisableOverrideMount;

        [Tooltip("This transforms position will be vaulted as well")]
        public Transform SecondaryPiece;
        public bool CanRotate;
        public float RotationStep = 45f;

        public bool CanSwitchModes = false;
        public Text ModeDisplay;

        [Serializable]
        public class MovementMode
        {
            public string ModeText;
            public EDegreesOfFreedom DegreesOfFreedom;
            public OpenScripts2_BasePlugin.Axis LimitingAxis;
        }

        public MovementMode[] MovementModes = new MovementMode[]
        {
            new MovementMode { ModeText = "All", DegreesOfFreedom = EDegreesOfFreedom.Spacial, LimitingAxis = OpenScripts2_BasePlugin.Axis.X },
            new MovementMode { ModeText = "X Limited", DegreesOfFreedom = EDegreesOfFreedom.Planar, LimitingAxis = OpenScripts2_BasePlugin.Axis.X },
            new MovementMode { ModeText = "Y Limited", DegreesOfFreedom = EDegreesOfFreedom.Planar, LimitingAxis = OpenScripts2_BasePlugin.Axis.Y },
            new MovementMode { ModeText = "Z Limited", DegreesOfFreedom = EDegreesOfFreedom.Planar, LimitingAxis = OpenScripts2_BasePlugin.Axis.Z },
        };

        private int _currentMode = 0;

        private Vector3 _lastPos;
        private Vector3 _lastHandPos;

        private Vector3 _startPos;

        [HideInInspector]
        public Vector3 LowerLimit;
        [HideInInspector]
        public Vector3 UpperLimit;

        [HideInInspector]
        public GameObject DisableOnHover;
        [HideInInspector]
        public GameObject EnableOnHover;

        public const string POSITION_FLAGDIC_KEY = "MovingFireArmAttachmentInterface Position";
        public const string ROTATION_FLAGDIC_KEY = "MovingFireArmAttachmentInterface Rotation";
        public const string SECONDARY_POSITION_FLAGDIC_KEY = "MovingFireArmAttachmentInterface Secondary Position";


        public override void Awake()
        {
            base.Awake();
            LowerLimit = new Vector3(XLimits.x, YLimits.x, ZLimits.x);
            UpperLimit = new Vector3(XLimits.y, YLimits.y, ZLimits.y);

            _startPos = Attachment.ObjectWrapper.GetGameObject().GetComponent<FVRFireArmAttachment>().AttachmentInterface.transform.localPosition;

            if (ModeDisplay != null) ModeDisplay.text = MovementModes[_currentMode].ModeText;
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (OverridesDisableOnHoverOfMount && Attachment.curMount.HasHoverDisablePiece)
            {
                if (Attachment.curMount.MyObject is CustomOpenScripts2Attachment attachment && attachment.AttachmentInterface is MovingFireArmAttachmentInterface attachmentInterface)
                {
                    DisableOnHover = attachmentInterface.DisableOnHover;
                    attachmentInterface.DisableOnHover = null;
                    DisableOnHover?.SetActive(true);
                }
                else 
                {
                    DisableOnHover = Attachment.curMount.DisableOnHover;
                    Attachment.curMount.DisableOnHover = null;
                    DisableOnHover?.SetActive(true);
                }
            }
            if (OverridesDisableOnHoverOfMount && Attachment.curMount.HasHoverEnablePiece)
            {
                if (Attachment.curMount.MyObject is CustomOpenScripts2Attachment attachment && attachment.AttachmentInterface is MovingFireArmAttachmentInterface attachmentInterface)
                {
                    EnableOnHover = attachmentInterface.EnableOnHover;
                    attachmentInterface.EnableOnHover = null;
                    EnableOnHover?.SetActive(false);
                }
                else
                {
                    EnableOnHover = Attachment.curMount.EnableOnHover;
                    Attachment.curMount.EnableOnHover = null;
                    EnableOnHover?.SetActive(false);
                }
            }
        }

        public override void OnDetach()
        {
            if (Attachment.curMount.MyObject is CustomOpenScripts2Attachment attachment && attachment.AttachmentInterface is MovingFireArmAttachmentInterface attachmentInterface)
            {
                attachmentInterface.DisableOnHover = DisableOnHover;
                attachmentInterface.EnableOnHover = EnableOnHover;
            }
            else
            {
                Attachment.curMount.DisableOnHover = DisableOnHover;
                Attachment.curMount.EnableOnHover = EnableOnHover;
            }
            DisableOnHover = null;
            EnableOnHover = null;          

            base.OnDetach();
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (OverridesDisableOnHoverOfMount && OverrideDisableOverrideMount != null)
            {
                if (OverrideDisableOverrideMount.HasAttachmentsOnIt()) DisableOnHover?.SetActive(false);
                else DisableOnHover?.SetActive(true);

                if (OverrideDisableOverrideMount.HasAttachmentsOnIt()) EnableOnHover?.SetActive(true);
                else EnableOnHover?.SetActive(false);
            }
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);

            _lastPos = transform.position;
            _lastHandPos = hand.Input.FilteredPos;
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);

            if (hand.Input.TriggerFloat > 0f)
            {
                Vector3 adjustedHandPosDelta = (hand.Input.FilteredPos - _lastHandPos) * hand.Input.TriggerFloat;
                Vector3 posDelta = (transform.position - _lastPos) * hand.Input.TriggerFloat;
                Vector3 newPosRaw = transform.position + adjustedHandPosDelta - posDelta;
                switch (DegreesOfFreedom)
                {
                    case EDegreesOfFreedom.Linear:
                        OneDegreeOfFreedom(newPosRaw);
                        break;
                    case EDegreesOfFreedom.Planar:
                        TwoDegreesOfFreedom(newPosRaw);
                        break;
                    case EDegreesOfFreedom.Spacial:
                        ThreeDegreesOfFreedom(newPosRaw);
                        break;
                }
            }
            else if (OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.up))
            {
                transform.localPosition = _startPos;
            }
            else if (CanRotate && OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.left))
            {
                transform.Rotate(0f, 0f, RotationStep);
            }
            else if (CanRotate && OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.right))
            {
                transform.Rotate(0f, 0f, -RotationStep);
            }
            else if (CanSwitchModes && OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.down))
            {
                SwitchMode();
            }

            _lastPos = transform.position;
            _lastHandPos = hand.Input.FilteredPos;
        }

        private void OneDegreeOfFreedom(Vector3 newPosRaw)
        {
            Vector3 newPosProjected;
            if (CanSwitchModes)
            {
                Vector3 lowLimit = LowerLimit.GetCombinedAxisVector(LimitingAxis, transform.localPosition).ApproximateInfiniteComponent(100f);
                Vector3 highLimit = UpperLimit.GetCombinedAxisVector(LimitingAxis, transform.localPosition).ApproximateInfiniteComponent(100f);
                newPosProjected = GetClosestValidPoint(lowLimit, highLimit, transform.parent.InverseTransformPoint(newPosRaw));
            }
            else
            {
                newPosProjected = transform.parent.InverseTransformPoint(newPosRaw).GetAxisVector(LimitingAxis).Clamp(LowerLimit, UpperLimit);
            }
            transform.localPosition = newPosProjected;
        }
        private void TwoDegreesOfFreedom(Vector3 newPosRaw)
        {
            Vector3 newPosProjected;
            if (CanSwitchModes)
            {
                newPosProjected = newPosRaw.ProjectOnPlaneThroughPoint(transform.position, transform.parent.GetLocalDirAxis(LimitingAxis));
            }
            else
            {
                newPosProjected = transform.parent.InverseTransformPoint(newPosRaw).RemoveAxisValue(LimitingAxis);
            }
            Vector3 newPosClamped = transform.parent.InverseTransformPoint(newPosProjected).Clamp(LowerLimit, UpperLimit);
            transform.localPosition = newPosClamped.ModifyAxisValue(LimitingAxis, _startPos.GetAxisValue(LimitingAxis));
        }
        private void ThreeDegreesOfFreedom(Vector3 newPosRaw)
        {
            transform.localPosition = transform.parent.InverseTransformPoint(newPosRaw).Clamp(LowerLimit, UpperLimit);
        }

        [ContextMenu("Copy existing Interface's values")]
        public void CopyInterface()
        {
            FVRFireArmAttachmentInterface[] attachments = GetComponents<FVRFireArmAttachmentInterface>();

            FVRFireArmAttachmentInterface toCopy = attachments.Single(c => c != this);

            toCopy.Attachment.AttachmentInterface = this;

            this.CopyComponent(toCopy);
        }

        private void SwitchMode()
        {
            _currentMode = (_currentMode + 1) % MovementModes.Length;

            if (ModeDisplay != null) ModeDisplay.text = MovementModes[_currentMode].ModeText;
            DegreesOfFreedom = MovementModes[_currentMode].DegreesOfFreedom;
            LimitingAxis = MovementModes[_currentMode].LimitingAxis;
        }
    }

#if DEBUG
    [UnityEditor.CustomEditor(typeof(MovingFireArmAttachmentInterface))]
    public class MovingFireArmAttachmentInterfaceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            MovingFireArmAttachmentInterface t = (MovingFireArmAttachmentInterface)target;
            DrawDefaultInspector();
            if (GUILayout.Button("Copy existing interface on this game object.")) t.CopyInterface();
        }
    }
#endif
}
