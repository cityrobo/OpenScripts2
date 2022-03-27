using FistVR;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


namespace OpenScripts2
{
    public class AdjustableReflexSight : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachment Attachment;
        [Tooltip("This can be an AttachmentInterface or a standalone FVRInteractiveObject set to \"is simple interact\" ")]
        public FVRInteractiveObject ReflexSightInterface;
        public MeshRenderer Reticle;

        [Tooltip("Switch that moves with the selected texture")]
        public Transform SwitchObject;

        public Axis SwitchAxis;
        public float[] SwitchPositions;

        public Texture2D[] ReticleTextures;

        public int CurrentReticleTexture = 0;

        [Header("Information Text Configuration")]
        public Transform TextFrame;
        public Text ReticleTextScreen;
        public Text ZeroTextScreen;

        public string ReticleTestPrefix = "Reticle: ";
        public string[] ReticleText;

        public string ZeroTextPrefix = "Zero Distance: ";
        [Tooltip("Index of the Array below, not the actual value")]
        public int CurrentZeroDistance = 3;
        [Tooltip("In meters. Miss me with that imperial shit!")]
        public float[] ZeroDistances = new float[7] { 2, 5, 10, 15, 25, 50, 100 };
        [Header("Intergrated Sight configuration")]
        [Tooltip("Check this box if integrated.")]
        public bool IsIntegrated = false;
        public FVRFireArm FireArm;

        [Header("Reticle Occlusion culling")]
        [Tooltip("Use this for reticle occlusion culling.")]
        public Collider LensCollider;

        private FVRViveHand _hand;
        private int _currentMenu = 0;

        private bool _zeroOnlyMode = false;
        private string _nameOfTexture = "_RedDotTex";
        private string _nameOfDistanceVariable = "_RedDotDist";
        private string _nameOfXOffset = "_MuzzleOffsetX";
        private string _nameOfYOffset = "_MuzzleOffsetY";

        private Transform _muzzlePos;

        private bool _attached = false;

        private Vector3 _leftEye;
        private Vector3 _rightEye;
        private bool _disableOcclusionCulling = false;
        public void Start()
        {
            if (CurrentReticleTexture >= ReticleTextures.Length) CurrentReticleTexture = 0;
            if (CurrentZeroDistance >= ZeroDistances.Length) CurrentZeroDistance = 0;
            if (ReticleTextures.Length != 0) Reticle.material.SetTexture(_nameOfTexture, ReticleTextures[CurrentReticleTexture]);
            Reticle.material.SetFloat(_nameOfDistanceVariable, ZeroDistances[CurrentZeroDistance]);

            if (SwitchObject != null) SwitchObject.ModifyLocalPositionAxis(SwitchAxis, SwitchPositions[CurrentReticleTexture]);

            if (ReflexSightInterface.IsSimpleInteract) Hook();

            if (ReticleTextures.Length <= 1) 
            { 
                _zeroOnlyMode = true;
                _currentMenu = 1;
            }

            if (IsIntegrated)
            {
                _muzzlePos = FireArm.MuzzlePos;
                Vector3 muzzleOffset = _muzzlePos.InverseTransformPoint(Reticle.transform.position);

                Reticle.material.SetFloat(_nameOfXOffset, -muzzleOffset.x);
                Reticle.material.SetFloat(_nameOfYOffset, -muzzleOffset.y);
            }

            StartScreen();

            _leftEye = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.right * -0.032f;
            _rightEye = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.right * +0.032f;

            if (LensCollider == null) _disableOcclusionCulling = true;
        }
#if !DEBUG
        public void OnDestroy()
        {
            Unhook();
        }

        public void Update()
        {
            if (!ReflexSightInterface.IsSimpleInteract)
            {
                _hand = ReflexSightInterface.m_hand;
                if (_hand != null)
                {
                    if (_hand.Input.TouchpadDown && Vector2.Angle(_hand.Input.TouchpadAxes, Vector2.left) < 45f && _currentMenu == 0) UsePreviousTexture();
                    else if (_hand.Input.TouchpadDown && Vector2.Angle(_hand.Input.TouchpadAxes, Vector2.right) < 45f && _currentMenu == 0) UseNextTexture();
                    if (_hand.Input.TouchpadDown && Vector2.Angle(_hand.Input.TouchpadAxes, Vector2.left) < 45f && _currentMenu == 1) UsePreviousZeroDistance();
                    else if (_hand.Input.TouchpadDown && Vector2.Angle(_hand.Input.TouchpadAxes, Vector2.right) < 45f && _currentMenu == 1) UseNextZeroDistance();
                    else if ((_hand.Input.TouchpadDown && Vector2.Angle(_hand.Input.TouchpadAxes, Vector2.up) < 45f) && !_zeroOnlyMode) ShowNextMenu();
                }
            }
            if (!IsIntegrated && Attachment.curMount != null && !_attached)
            {
                _attached = true;
                FireArm = Attachment.curMount.GetRootMount().MyObject as FVRFireArm;
                if (FireArm != null)
                {
                    _muzzlePos = FireArm.CurrentMuzzle;

                    Vector3 muzzleOffset = _muzzlePos.InverseTransformPoint(Reticle.transform.position);

                    Reticle.material.SetFloat(_nameOfXOffset, -muzzleOffset.x);
                    Reticle.material.SetFloat(_nameOfYOffset, -muzzleOffset.y);
                }
            }
            else if (!IsIntegrated && Attachment.curMount == null && _attached)
            {
                _attached = false;
                Reticle.material.SetFloat(_nameOfXOffset, 0f);
                Reticle.material.SetFloat(_nameOfYOffset, 0f);
                FireArm = null;
                _muzzlePos = null;
            }

            _leftEye = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.right * -0.032f;
            _rightEye = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.right * +0.032f;

            if (!_disableOcclusionCulling && (IsIntegrated || _attached)) CheckReticleVisibility();
        }
#endif
        public void UseNextTexture()
        {
            CurrentReticleTexture = (CurrentReticleTexture + 1) % ReticleTextures.Length;

            Reticle.material.SetTexture(_nameOfTexture, ReticleTextures[CurrentReticleTexture]);
            if (SwitchObject != null) SwitchObject.ModifyLocalPositionAxis(SwitchAxis, SwitchPositions[CurrentReticleTexture]);
            UpdateScreen();
        }

        public void UsePreviousTexture()
        {
            CurrentReticleTexture = (CurrentReticleTexture + ReticleTextures.Length - 1) % ReticleTextures.Length;

            Reticle.material.SetTexture(_nameOfTexture, ReticleTextures[CurrentReticleTexture]);
            if (SwitchObject != null) SwitchObject.ModifyLocalPositionAxis(SwitchAxis, SwitchPositions[CurrentReticleTexture]);
            UpdateScreen();
        }

        private void ShowNextMenu() 
        {
            if (ReticleTextScreen == null && ZeroTextScreen == null) return;
            _currentMenu++;

            if (_currentMenu > 2) _currentMenu = 0;

            switch (_currentMenu)
            {
                case 0:
                    if (ReticleTextScreen == null)
                    {
                        ShowNextMenu();
                        return;
                    }
                    break;
                case 1:
                    if (ZeroTextScreen == null)
                    {
                        ShowNextMenu();
                        return;
                    }
                    break;
                default:
                    _currentMenu = 0;
                    break;
            }
            UpdateScreen();
        }

        private void UpdateScreen()
        {
            if (ReticleTextScreen != null && _currentMenu == 0)
            {
                if (TextFrame != null) TextFrame.localPosition = ReticleTextScreen.transform.localPosition;
                ReticleTextScreen.text = ReticleTestPrefix + ReticleText[CurrentReticleTexture];
            }
            else if (ReticleTextScreen == null)
            {
                _currentMenu = 1;
            }

            if (ZeroTextScreen != null && _currentMenu == 1)
            {
                if (TextFrame != null) TextFrame.localPosition = ZeroTextScreen.transform.localPosition;
                ZeroTextScreen.text = ZeroTextPrefix + ZeroDistances[CurrentZeroDistance] + "m";
            }
            
        }

        private void StartScreen()
        {
            if (ReticleTextScreen != null) ReticleTextScreen.text = ReticleTestPrefix + ReticleText[CurrentReticleTexture];
            if (ZeroTextScreen != null) ZeroTextScreen.text = ZeroTextPrefix + ZeroDistances[CurrentZeroDistance] + "m";
        }
        public void UseNextZeroDistance()
        {
            if (CurrentZeroDistance < ZeroDistances.Length - 1) CurrentZeroDistance++;
            Reticle.material.SetFloat(_nameOfDistanceVariable, ZeroDistances[CurrentZeroDistance]);
            UpdateScreen();
        }

        public void UsePreviousZeroDistance()
        {
            if (CurrentZeroDistance > 0) CurrentZeroDistance--;
            Reticle.material.SetFloat(_nameOfDistanceVariable, ZeroDistances[CurrentZeroDistance]);
            UpdateScreen();
        }

        private void CheckReticleVisibility()
        {
            bool scopeHit = false;

            if (LensCollider != null)
            {
                // Right Eye Occlusion Test
                float distance = Vector3.Distance(this.gameObject.transform.position, GM.CurrentPlayerBody.Head.position) + 0.2f;
                Vector3 direction = _muzzlePos.position + this.transform.forward * ZeroDistances[CurrentZeroDistance] - _rightEye;
                bool angleGood = Vector3.Angle(GM.CurrentPlayerBody.Head.forward, this.transform.forward) < 45f;
                if (angleGood)
                {
                    Ray ray = new Ray(_rightEye,direction);
                    RaycastHit hit;
                    if (LensCollider.Raycast(ray, out hit, distance))
                    {
                        Reticle.gameObject.SetActive(true);
                        scopeHit = true;
                    }
                }

                if (!scopeHit)
                {
                    // Left Eye Occlusion Test
                    direction = _muzzlePos.position + this.transform.forward * ZeroDistances[CurrentZeroDistance] - _leftEye;
                    angleGood = Vector3.Angle(GM.CurrentPlayerBody.Head.forward, this.transform.forward) < 45f;
                    if (angleGood)
                    {
                        Ray ray = new Ray(_leftEye, direction);
                        RaycastHit hit;
                        if (LensCollider.Raycast(ray, out hit, distance))
                        {
                            Reticle.gameObject.SetActive(true);
                            scopeHit = true;
                        }
                    }
                }

                if (!scopeHit) Reticle.gameObject.SetActive(false);
            }
            else
            {
                OpenScripts2_BepInExPlugin.LogWarning(this, "No usable colliders for reticle occlusion found! If you are a modmaker, please add colliders or a lens collider, or disable occlusion culling with the checkbox!\n Disabling Occlusion culling now!");
                _disableOcclusionCulling = true;
            }
        }

        private void Unhook()
        {
#if !DEBUG
            On.FistVR.FVRInteractiveObject.SimpleInteraction -= FVRInteractiveObject_SimpleInteraction;
#endif
        }

        private void Hook()
        {
#if !DEBUG
            On.FistVR.FVRInteractiveObject.SimpleInteraction += FVRInteractiveObject_SimpleInteraction;
#endif
        }


#if !DEBUG
        private void FVRInteractiveObject_SimpleInteraction(On.FistVR.FVRInteractiveObject.orig_SimpleInteraction orig, FVRInteractiveObject self, FVRViveHand hand)
        {
            orig(self, hand);
            if (self == ReflexSightInterface) UseNextTexture();
        }
#endif
    }
}
