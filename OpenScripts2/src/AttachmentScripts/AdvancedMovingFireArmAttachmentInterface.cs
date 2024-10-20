using FistVR;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class AdvancedMovingFireArmAttachmentInterface : FVRFireArmAttachmentInterface
    {
        [Header("Advanced Moving FireArm Attachment Interface Config")]
        public GameObject Menu;

        public Transform MenuPoint;
        [HideInInspector]
        public TransformProxy MenuPointProxy;

        //[Tooltip("One degree means linear movement, two degrees means movement on a plane, three degrees free spacial movement.")]
        public EMovementMode CurrentMovementMode;

        public enum EMovementMode
        {
            Linear,
            Planar,
            Spacial
        }

        public float[] RotationStepOptions = { 90f, 45f, 22.5f, 11.25f, 5.625f };

        public int CurrentRotationalStepOption = 0;

        public OpenScripts2_BasePlugin.Axis CurrentLimitingAxis = OpenScripts2_BasePlugin.Axis.Z;

        public Vector2 XLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 YLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 ZLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);

        public OpenScripts2_BasePlugin.Axis CurrentRotationalAxis = OpenScripts2_BasePlugin.Axis.Z;

        public Transform PinCheckSource;
        public LayerMask PinLayerMask;

        // Pin Variables
        [HideInInspector]
        public string UnvaultingPinTransformPath = string.Empty;
        private Transform _origParent;
        private Transform _pinTarget;
        private bool _isPinned = false;

        [Header("Primarily Muzzle Device Features")]
        public bool OverridesDisableOnHoverOfMount = false;
        [Tooltip("Something placed on this mount will disable the hover on disable piece again.")]
        public FVRFireArmAttachmentMount OverrideDisableOverrideMount;

        [Header("Secondary Clamping Part")]
        [Tooltip("This transforms position will be vaulted as well")]
        public Transform SecondaryPiece;

        public enum EResetMode {Axis, Rotation, All}

        public EResetMode ResetMode = EResetMode.Axis;

        private float RotationStep => RotationStepOptions[CurrentRotationalStepOption];

        private Vector3 _lastPos;
        private Vector3 _lastHandPos;

        private Vector3 _startPos;

        private Vector3 LowerLimit => new Vector3(XLimits.x, YLimits.x, ZLimits.x);
        private Vector3 UpperLimit => new Vector3(XLimits.y, YLimits.y, ZLimits.y);

        [HideInInspector]
        public GameObject DisableOnHover;
        [HideInInspector]
        public GameObject EnableOnHover;

        public const string POSITION_FLAGDIC_KEY = "MovingFireArmAttachmentInterface Position";
        public const string ROTATION_FLAGDIC_KEY = "MovingFireArmAttachmentInterface Rotation";
        public const string SECONDARY_POSITION_FLAGDIC_KEY = "MovingFireArmAttachmentInterface Secondary Position";

        public const string PIN_TRANFORM_PATH_FLAGDIC_KEY = "MovingFireArmAttachmentInterface Pin Transform Path";

        private static AssetBundle _AMFAIM_AssetBundle;
        private static GameObject _AMFAIM_Prefab;

        private const string ASSETBUNDLENAME = "advancedmovingattachmentinterfacemenu";
        private const string PREFABNAME = "AdvancedMovingAttachmentInterfaceMenu";

        public bool IsPinned => _isPinned;

        public override void Awake()
        {
            base.Awake();
            // Create MenuPoint proxy for a tiny amount more performance
            MenuPointProxy = new(MenuPoint, true);

            // load Menu asset bundle and prefab if necessary.
            if (Menu == null && _AMFAIM_AssetBundle == null && _AMFAIM_Prefab == null)
            {
                string path = Path.Combine(Path.GetDirectoryName(OpenScripts2_BepInExPlugin.Instance.Info.Location), ASSETBUNDLENAME);

                _AMFAIM_AssetBundle = AssetBundle.LoadFromFile(path);

                _AMFAIM_Prefab = _AMFAIM_AssetBundle.LoadAsset<GameObject>(PREFABNAME);
            }
            _startPos = Attachment.ObjectWrapper.GetGameObject().GetComponent<FVRFireArmAttachment>().AttachmentInterface.transform.localPosition;

            if (Menu == null)
            {
                transform.parent.gameObject.SetActive(false);
                Menu = Instantiate(_AMFAIM_Prefab, transform.parent);
                transform.parent.gameObject.SetActive(true);
            }

            Menu.GetComponent<AdvancedMovingFireArmAttachmentMenu>().Interface = this;

            Menu.SetActive(false);
            //if (ModeDisplay != null) ModeDisplay.text = MovementModes[_currentMode].ModeText;
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (OverridesDisableOnHoverOfMount && Attachment.curMount.HasHoverDisablePiece)
            {
                if (Attachment.curMount.MyObject is CustomOpenScripts2Attachment attachment && attachment.AttachmentInterface is AdvancedMovingFireArmAttachmentInterface attachmentInterface)
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
                if (Attachment.curMount.MyObject is CustomOpenScripts2Attachment attachment && attachment.AttachmentInterface is AdvancedMovingFireArmAttachmentInterface attachmentInterface)
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

            _origParent = Attachment.transform.parent;

            if (UnvaultingPinTransformPath != string.Empty)
            {
                PinToPathTarget(UnvaultingPinTransformPath);

                UnvaultingPinTransformPath = string.Empty;
            }
        }

        public override void OnDetach()
        {
            if (Attachment.curMount.MyObject is CustomOpenScripts2Attachment attachment && attachment.AttachmentInterface is AdvancedMovingFireArmAttachmentInterface attachmentInterface)
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

            _origParent = null;

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

            if (Menu.activeSelf)
            {
                //Menu.transform.rotation = Attachment.transform.rotation;
                //Menu.transform.position = transform.position + 0.1f * Attachment.transform.up;

                UpdateGizmos();
            }
        }

        private void UpdateGizmos()
        {
            switch (CurrentMovementMode)
            {
                case EMovementMode.Linear:
                    switch (CurrentLimitingAxis)
                    {
                        case OpenScripts2_BasePlugin.Axis.X:
                            Popcron.Gizmos.Line(transform.position - transform.parent.right * 0.1f, transform.position + transform.parent.right * 0.1f, Color.red);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Y:
                            Popcron.Gizmos.Line(transform.position - transform.parent.up * 0.1f, transform.position + transform.parent.up * 0.1f, Color.green);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Z:
                            Popcron.Gizmos.Line(transform.position - transform.parent.forward * 0.1f, transform.position + transform.parent.forward * 0.1f, Color.blue);
                            break;
                    }
                    break;
                case EMovementMode.Planar:
                    switch (CurrentLimitingAxis)
                    {
                        case OpenScripts2_BasePlugin.Axis.X:
                            //Popcron.Gizmos.Square(transform.position - transform.parent.up * 0.1f - transform.parent.forward * 0.1f, transform.position + transform.parent.up * 0.1f + transform.parent.forward * 0.1f, Color.red);
                            Popcron.Gizmos.Cube(transform.position, transform.parent.rotation * Quaternion.Euler(0f, 90f, 0f), new Vector3(0.1f, 0.1f, 0f), Color.red);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Y:
                            //Popcron.Gizmos.Square(transform.position - transform.parent.forward * 0.1f - transform.parent.right * 0.1f, transform.position + transform.parent.forward * 0.1f + transform.parent.up * 0.1f, Color.green);
                            Popcron.Gizmos.Cube(transform.position, transform.parent.rotation * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.1f, 0.1f, 0f), Color.green);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Z:
                            //Popcron.Gizmos.Square(transform.position - transform.parent.up * 0.1f - transform.parent.right * 0.1f, transform.position + transform.parent.up * 0.1f + transform.parent.right * 0.1f, Color.blue);
                            Popcron.Gizmos.Cube(transform.position, transform.parent.rotation * Quaternion.Euler(0f, 0f, 0f), new Vector3(0.1f, 0.1f, 0f), Color.blue);
                            break;
                    }
                    break;
                case EMovementMode.Spacial:
                    Popcron.Gizmos.Line(transform.position - transform.parent.right * 0.1f, transform.position + transform.parent.right * 0.1f, Color.red);
                    Popcron.Gizmos.Line(transform.position - transform.parent.up * 0.1f, transform.position + transform.parent.up * 0.1f, Color.green);
                    Popcron.Gizmos.Line(transform.position - transform.parent.forward * 0.1f, transform.position + transform.parent.forward * 0.1f, Color.blue);
                    break;
            }
            switch (CurrentRotationalAxis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    Popcron.Gizmos.Circle(transform.position, 0.1f, transform.parent.rotation * Quaternion.Euler(0f, 90f, 0f), Color.red);
                    break;
                case OpenScripts2_BasePlugin.Axis.Y:
                    Popcron.Gizmos.Circle(transform.position, 0.1f, transform.parent.rotation * Quaternion.Euler(90f, 0f, 0f), Color.green);
                    break;
                case OpenScripts2_BasePlugin.Axis.Z:
                    Popcron.Gizmos.Circle(transform.position, 0.1f, transform.parent.rotation * Quaternion.Euler(0f, 0f, 0f), Color.blue);
                    break;
            }
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);

            _lastPos = transform.position;
            _lastHandPos = hand.Input.FilteredPos;

            Menu.SetActive(true);
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);

            if (hand.Input.TriggerFloat > 0f)
            {
                Vector3 adjustedHandPosDelta = (hand.Input.FilteredPos - _lastHandPos) * hand.Input.TriggerFloat;
                Vector3 posDelta = (transform.position - _lastPos) * hand.Input.TriggerFloat;
                Vector3 newPosRaw = transform.position + adjustedHandPosDelta - posDelta;
                switch (CurrentMovementMode)
                {
                    case EMovementMode.Linear:
                        OneDegreeOfFreedom(newPosRaw);
                        break;
                    case EMovementMode.Planar:
                        TwoDegreesOfFreedom(newPosRaw);
                        break;
                    case EMovementMode.Spacial:
                        ThreeDegreesOfFreedom(newPosRaw);
                        break;
                }
            }
            //else if (OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.up))
            //{
            //    transform.localPosition = _startPos;
            //}
            else if (OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.left))
            {
                RotateLeft();
            }
            else if (OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.right))
            {
                RotateRight();
            }
            //else if (CanSwitchModes && OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.down))
            //{
            //    SwitchMode();
            //}

            _lastPos = transform.position;
            _lastHandPos = hand.Input.FilteredPos;
        }

        public void TryPin()
        {
            if (!_isPinned)
            {
                if (Physics.Raycast(PinCheckSource.position, PinCheckSource.forward, out RaycastHit hit, 0.05f, PinLayerMask, QueryTriggerInteraction.Ignore))
                {
                    _isPinned = true;
                    _pinTarget = hit.collider.transform.parent;
                    Attachment.transform.parent = _pinTarget;
                    Menu.GetComponent<AdvancedMovingFireArmAttachmentMenu>().SetPinButtonState(true);
                }
                else
                {
                    _isPinned = false;
                    Attachment.transform.parent = _origParent;
                    Menu.GetComponent<AdvancedMovingFireArmAttachmentMenu>().SetPinButtonState(false);
                }
            }
            else
            {
                _isPinned = false;
                Attachment.transform.parent = _origParent;
                Menu.GetComponent<AdvancedMovingFireArmAttachmentMenu>().SetPinButtonState(false);
            }
        }

        public void RotateLeft()
        {
            RotatePart(true);
        }

        public void RotateRight()
        {
            RotatePart(false);
        }

        public void CloseMenu()
        {
            Menu.SetActive(false);
        }

        public void Reset()
        {
            switch (ResetMode)
            {
                case EResetMode.Axis:
                    transform.ModifyLocalPositionAxisValue(CurrentLimitingAxis, 0f);
                    break;
                case EResetMode.Rotation:
                    transform.ModifyLocalRotationAxisValue(CurrentRotationalAxis, 0f);
                    break;
                case EResetMode.All:
                    transform.localPosition = _startPos;
                    transform.localRotation = Quaternion.identity;
                    break;
            }
        }

        private void RotatePart(bool forward)
        {
            int forwardMult = forward ? 1 : -1;

            switch (CurrentRotationalAxis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    transform.Rotate(forwardMult * RotationStep, 0f, 0f);
                    break;
                case OpenScripts2_BasePlugin.Axis.Y:
                    transform.Rotate(0f, forwardMult * RotationStep, 0f);
                    break;
                case OpenScripts2_BasePlugin.Axis.Z:
                    transform.Rotate(0f, 0f, forwardMult * RotationStep);
                    break;
            }
        }

        public void NextResetMode()
        {
            ResetMode += 1;

            if ((int)ResetMode > 2) ResetMode = EResetMode.Axis;
        }

        public void PreviousResetMode()
        {
            ResetMode -= 1;

            if ((int)ResetMode < 0) ResetMode = EResetMode.All;
        }

        public void NextDegreeOfFreedom()
        {
            CurrentMovementMode += 1;

            if ((int)CurrentMovementMode > 2) CurrentMovementMode = EMovementMode.Linear;
        }

        public void PreviousDegreeOfFreedom()
        {
            CurrentMovementMode -= 1;

            if ((int)CurrentMovementMode < 0) CurrentMovementMode = EMovementMode.Spacial;
        }

        public void NextLimitingAxis()
        {
            CurrentLimitingAxis += 1;

            if ((int)CurrentLimitingAxis > 2) CurrentLimitingAxis = OpenScripts2_BasePlugin.Axis.X;
        }


        public void PreviousLimitingAxis() 
        {
            CurrentLimitingAxis -= 1;

            if ((int)CurrentLimitingAxis < 0) CurrentLimitingAxis = OpenScripts2_BasePlugin.Axis.Z;
        }

        public void NextRotationalAxis()
        {
            CurrentRotationalAxis += 1;

            if ((int)CurrentRotationalAxis > 2) CurrentRotationalAxis = OpenScripts2_BasePlugin.Axis.X;
        }

        public void PreviousRotationalAxis()
        {
            CurrentRotationalAxis -= 1;

            if ((int)CurrentRotationalAxis < 0) CurrentRotationalAxis = OpenScripts2_BasePlugin.Axis.Z;
        }

        public void NextRotationalStepOption()
        {
            CurrentRotationalStepOption += 1;

            if (CurrentRotationalStepOption >= RotationStepOptions.Length) CurrentRotationalStepOption = 0;
        }

        public void PreviousRotationalStepOption()
        {
            CurrentRotationalStepOption -= 1;

            if (CurrentRotationalStepOption < 0) CurrentRotationalStepOption = RotationStepOptions.Length - 1;
        }

        private void OneDegreeOfFreedom(Vector3 newPosRaw)
        {
            Vector3 lowLimit = LowerLimit.GetCombinedAxisVector(CurrentLimitingAxis, transform.localPosition).ApproximateInfiniteComponent(100f);
            Vector3 highLimit = UpperLimit.GetCombinedAxisVector(CurrentLimitingAxis, transform.localPosition).ApproximateInfiniteComponent(100f);
            Vector3 newPosProjected = GetClosestValidPoint(lowLimit, highLimit, transform.parent.InverseTransformPoint(newPosRaw));

            transform.localPosition = newPosProjected;
        }

        private void TwoDegreesOfFreedom(Vector3 newPosRaw)
        {
            Vector3 newPosProjected = newPosRaw.ProjectOnPlaneThroughPoint(transform.position, transform.parent.GetLocalDirAxis(CurrentLimitingAxis));
            Vector3 newPosClamped = transform.parent.InverseTransformPoint(newPosProjected).Clamp(LowerLimit, UpperLimit);
            transform.localPosition = newPosClamped;//.ModifyAxisValue(LimitingAxis, _startPos.GetAxisValue(LimitingAxis));
        }

        private void ThreeDegreesOfFreedom(Vector3 newPosRaw)
        {
            transform.localPosition = transform.parent.InverseTransformPoint(newPosRaw).Clamp(LowerLimit, UpperLimit);
        }

        public string GetPinTargetPath()
        {
            return GetTransformPath(_pinTarget);
        }

        public void PinToPathTarget(string path)
        {
            Transform root = Attachment.curMount.GetRootMount().MyObject.transform;
            Transform target = ReturnTransfromFromPath(root, path);

            _isPinned = true;
            _pinTarget = target;
            Attachment.transform.parent = _pinTarget;
        }

        private string GetTransformPath(Transform t, string path = "")
        {
            if (path == "")
            {
                path = t.name;
            }
            else
            {
                path = t.name + "/" + path;
            }
            if (t != Attachment.curMount.GetRootMount().MyObject.transform)
            {
                path = GetTransformPath(t.parent, path);
            }
            return path;
        }

        private Transform ReturnTransfromFromPath(Transform root, string path)
        {
            string[] splitPath = path.Split('/');
            Transform target = root;
            for (int i = 1; i < splitPath.Length; i++)
            {
                string pathPiece = splitPath[i];
                target = target.Find(pathPiece);
            }
            return target;
        }

        [ContextMenu("Copy existing Interface's values")]
        public void CopyInterface()
        {
            FVRFireArmAttachmentInterface[] attachments = GetComponents<FVRFireArmAttachmentInterface>();

            FVRFireArmAttachmentInterface toCopy = attachments.Single(c => c != this);

            toCopy.Attachment.AttachmentInterface = this;

            this.CopyComponent(toCopy);
        }

        //private void SwitchMode()
        //{
        //    _currentMode = (_currentMode + 1) % MovementModes.Length;

        //    if (ModeDisplay != null) ModeDisplay.text = MovementModes[_currentMode].ModeText;
        //    DegreesOfFreedom = MovementModes[_currentMode].DegreesOfFreedom;
        //    LimitingAxis = MovementModes[_currentMode].LimitingAxis;
        //}
    }

#if DEBUG
    [UnityEditor.CustomEditor(typeof(AdvancedMovingFireArmAttachmentInterface))]
    public class AdvancedMovingFireArmAttachmentInterfaceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            AdvancedMovingFireArmAttachmentInterface t = (AdvancedMovingFireArmAttachmentInterface)target;
            DrawDefaultInspector();
            if (GUILayout.Button("Copy existing interface on this game object.")) t.CopyInterface();
        }
    }
#endif
}
