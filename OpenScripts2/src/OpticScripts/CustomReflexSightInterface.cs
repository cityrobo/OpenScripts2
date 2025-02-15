using FistVR;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


namespace OpenScripts2
{
    public class CustomReflexSightInterface : FVRFireArmAttachmentInterface
    {
        public MeshRenderer ReticleMesh;

        [Header("Reticle Settings")]
        [Tooltip("Index. Starts at 0.")]
        public int CurrentSelectedReticle = 0;
        [Tooltip("All reticle textures. Default reticle is first entry.")]
        public Texture2D[] ReticleTextures;
        [ColorUsage(true, true, 0f, float.MaxValue, 0f, float.MaxValue)]
        public Color[] ReticleColors;
        [Tooltip("Names of all reticles. Default reticle name is first entry.")]
        public string[] ReticleText;

        [Header("Information Text Configuration")]
        public Transform TextFrame;
        public Text ReticleTextScreen;
        public Text ZeroTextScreen;
        public Text BrightnessTextScreen;

        public string ReticleTestPrefix = "Reticle: ";
        public string ZeroTextPrefix = "Zero Distance: ";
        public string BrightnessTextPrefix = "Brightness: ";
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

        [Header("Moving Switch Settings")]
        [Tooltip("Switch that moves with the selected texture")]
        public Transform SwitchObject;
        public OpenScripts2_BasePlugin.Axis SwitchAxis;
        public float[] SwitchPositions;

        [Header("Brightness Settings")]
        [Tooltip("Index of the Array below, not the actual value. Starts at 0.")]
        public int CurrentBrightnessIndex = 3;
        public float[] HDRBrightnessLevels = new float[] { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f, 2.5f, 3f };
        public float[] BrightnessAlphaLevels = new float[] { 0.25f, 0.5f, 0.75f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        public string[] BrightnessTexts = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", };

        private int _currentMenu = 0;

        private const string NameOfTextureVariable = "_RedDotTex";
        private const string NameOfColorVariable = "_DotColor";
        private const string NameOfDistanceVariable = "_RedDotDist";
        private const string NameOfXOffsetVariable = "_MuzzleOffsetX";
        private const string NameOfYOffsetVariable = "_MuzzleOffsetY";

        private Transform _muzzlePos;

        private bool _isAttached = false;

        private Vector3 _leftEyePosition;
        private Vector3 _rightEye;
        private bool _disableOcclusionCulling = false;

        private MaterialPropertyBlock _materialPropertyBlock;

        private Quaternion _originalReticleRotation;

        public override void Start()
        {
            base.Start();

            SetupOnStart();

            if (IsIntegrated && FireArm != null)
            {
                Zero();

                ForceInteractable = true;
            }
            else if (IsIntegrated && FireArm == null) OpenScripts2_BepInExPlugin.LogWarning(this, "Sight set to \"Is Integrated\" but no FireArm assigned. Either implementation error or Modular Weapon Part.");

            StartScreen();

            _leftEyePosition = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.right * -0.032f;
            _rightEye = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.right * +0.032f;

            if (LensCollider == null) _disableOcclusionCulling = true;

            ReticleMesh.SetPropertyBlock(_materialPropertyBlock);
        }

        private void SetupOnStart()
        {
            _originalReticleRotation = ReticleMesh.transform.localRotation;

            _materialPropertyBlock = new MaterialPropertyBlock();

            if (CurrentSelectedReticle >= ReticleTextures.Length) CurrentSelectedReticle = 0;
            if (CurrentZeroDistance >= ZeroDistances.Length) CurrentZeroDistance = 0;
            if (ReticleTextures.Length != 0) _materialPropertyBlock.SetTexture(NameOfTextureVariable, ReticleTextures[CurrentSelectedReticle]);

            SwitchObject?.ModifyLocalPositionAxisValue(SwitchAxis, SwitchPositions[CurrentSelectedReticle]);

            if (ReticleTextures.Length <= 1)
            {
                _currentMenu = 1;
            }
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            if (hand != null)
            {
                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f)
                {
                    switch (_currentMenu)
                    {
                        case 0:
                            PreviousReticleTexture();
                            break;
                        case 1:
                            PreviousZeroDistance();
                            break;
                        case 2:
                            PreviousBrightnessSetting();
                            break;
                    }
                }
                else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f)
                {
                    switch (_currentMenu)
                    {
                        case 0:
                            NextReticleTexture();
                            break;
                        case 1:
                            NextZeroDistance();
                            break;
                        case 2:
                            NextBrightnessSetting();
                            break;
                    }
                }
                else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f) ShowNextMenu();
            }
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (!IsIntegrated && Attachment.curMount != null && !_isAttached)
            {
                _isAttached = true;
                FireArm = Attachment.curMount.GetRootMount().MyObject as FVRFireArm;
                if (FireArm != null)
                {
                    Zero();
                }
            }
            else if (!IsIntegrated && Attachment.curMount == null && _isAttached)
            {
                _isAttached = false;
                Zero(true);
            }

            _leftEyePosition = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.right * -0.032f;
            _rightEye = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.right * +0.032f;

            if (!_disableOcclusionCulling && (IsIntegrated || _isAttached)) CheckReticleVisibility();

            ReticleMesh.SetPropertyBlock(_materialPropertyBlock);
        }

        public void OffsetReticle()
        {
            if (_materialPropertyBlock == null) SetupOnStart();
            Zero();
            gameObject.SetActive(true);
        }

        private void ShowNextMenu() 
        {
            if (ReticleTextScreen == null && ZeroTextScreen == null && BrightnessTextScreen == null) return;
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
                case 2:
                    if (BrightnessTextScreen == null)
                    {
                        ShowNextMenu();
                        return;
                    }
                    break;
            }
            UpdateScreen();
        }
        private void StartScreen()
        {
            if (ReticleTextScreen != null) ReticleTextScreen.text = ReticleTestPrefix + ReticleText[CurrentSelectedReticle];
            if (ZeroTextScreen != null) ZeroTextScreen.text = ZeroTextPrefix + ZeroDistances[CurrentZeroDistance] + "m";
            if (BrightnessTextScreen != null) BrightnessTextScreen.text = BrightnessTextPrefix + BrightnessTexts[CurrentBrightnessIndex];
        }

        private void UpdateScreen()
        {
            if (ReticleTextScreen != null && _currentMenu == 0)
            {
                if (TextFrame != null) TextFrame.localPosition = ReticleTextScreen.transform.localPosition;
                ReticleTextScreen.text = ReticleTestPrefix + ReticleText[CurrentSelectedReticle];
            }
            else if (ZeroTextScreen != null && _currentMenu == 1)
            {
                if (TextFrame != null) TextFrame.localPosition = ZeroTextScreen.transform.localPosition;
                ZeroTextScreen.text = ZeroTextPrefix + ZeroDistances[CurrentZeroDistance] + "m";
            }
            else if (BrightnessTextScreen != null && _currentMenu == 2)
            {
                if (TextFrame != null) TextFrame.localPosition = BrightnessTextScreen.transform.localPosition;
                BrightnessTextScreen.text = BrightnessTextPrefix + BrightnessTexts[CurrentBrightnessIndex];
            }
        }
        public void NextReticleTexture()
        {
            CurrentSelectedReticle = (CurrentSelectedReticle + 1) % ReticleTextures.Length;

            _materialPropertyBlock.SetTexture(NameOfTextureVariable, ReticleTextures[CurrentSelectedReticle]);
            if (ReticleColors != null && ReticleColors.Length == ReticleTextures.Length) _materialPropertyBlock.SetColor(NameOfColorVariable, ReticleColors[CurrentSelectedReticle]);
            if (SwitchObject != null) SwitchObject.ModifyLocalPositionAxisValue(SwitchAxis, SwitchPositions[CurrentSelectedReticle]);

            if (BrightnessTextScreen != null) UpdateBrightness();
            UpdateScreen();
        }

        public void PreviousReticleTexture()
        {
            CurrentSelectedReticle = (CurrentSelectedReticle + ReticleTextures.Length - 1) % ReticleTextures.Length;

            _materialPropertyBlock.SetTexture(NameOfTextureVariable, ReticleTextures[CurrentSelectedReticle]);
            if (ReticleColors != null && ReticleColors.Length == ReticleTextures.Length) _materialPropertyBlock.SetColor(NameOfColorVariable, ReticleColors[CurrentSelectedReticle]);
            if (SwitchObject != null) SwitchObject.ModifyLocalPositionAxisValue(SwitchAxis, SwitchPositions[CurrentSelectedReticle]);

            if (BrightnessTextScreen != null) UpdateBrightness();
            UpdateScreen();
        }

        public void NextZeroDistance()
        {
            if (CurrentZeroDistance < ZeroDistances.Length - 1) CurrentZeroDistance++;
            Zero();

            UpdateScreen();
        }

        public void PreviousZeroDistance()
        {
            if (CurrentZeroDistance > 0) CurrentZeroDistance--;
            Zero();

            UpdateScreen();
        }

        public void NextBrightnessSetting()
        {
            if (CurrentBrightnessIndex < HDRBrightnessLevels.Length - 1) CurrentBrightnessIndex++;

            UpdateBrightness();
            UpdateScreen();
        }
        public void PreviousBrightnessSetting()
        {
            if (CurrentBrightnessIndex > 0) CurrentBrightnessIndex--;

            UpdateBrightness();
            UpdateScreen();
        }
        public void UpdateBrightness()
        {
            float factor = Mathf.Pow(2, HDRBrightnessLevels[CurrentBrightnessIndex] - 1f);

            if (ReticleColors == null || ReticleColors.Length == 0)
            {
                OpenScripts2_BepInExPlugin.LogError(this,"Trying to change brightness but reference color array is empty!");
                return;
            }
            Color currentReticleColor;
            try
            {
                currentReticleColor = ReticleColors[CurrentSelectedReticle];
            }
            catch (System.Exception)
            {
                OpenScripts2_BepInExPlugin.LogError(this, "Trying to change brightness but reference color array is empty at selected texture index!");
                return;
            }
            Color color = new Color(currentReticleColor.r * factor, currentReticleColor.g * factor, currentReticleColor.b * factor, currentReticleColor.a);
            color.a *= BrightnessAlphaLevels[CurrentBrightnessIndex];

            _materialPropertyBlock.SetColor(NameOfColorVariable, color);
        }

        private void CheckReticleVisibility()
        {
            bool scopeHit = false;

            if (LensCollider != null)
            {
                // Right Eye Occlusion Test
                float distanceHeadSight = Vector3.Distance(ReticleMesh.transform.position, GM.CurrentPlayerBody.Head.position) + 0.2f;
                Vector3 direction = ReticleMesh.transform.position + ReticleMesh.transform.forward * ZeroDistances[CurrentZeroDistance] - _rightEye;
                bool angleGood = Vector3.Angle(GM.CurrentPlayerBody.Head.forward, ReticleMesh.transform.forward) < 45f;
                if (angleGood)
                {
                    Ray ray = new(_rightEye, direction);
                    if (LensCollider.Raycast(ray, out _, distanceHeadSight))
                    {
                        ReticleMesh.gameObject.SetActive(true);
                        scopeHit = true;
                    }
                }

                if (!scopeHit)
                {
                    // Left Eye Occlusion Test
                    direction = ReticleMesh.transform.position + ReticleMesh.transform.forward * ZeroDistances[CurrentZeroDistance] - _leftEyePosition;
                    angleGood = Vector3.Angle(GM.CurrentPlayerBody.Head.forward, ReticleMesh.transform.forward) < 45f;
                    if (angleGood)
                    {
                        Ray ray = new(_leftEyePosition, direction);
                        if (LensCollider.Raycast(ray, out _, distanceHeadSight))
                        {
                            ReticleMesh.gameObject.SetActive(true);
                            scopeHit = true;
                        }
                    }
                }

                if (!scopeHit) ReticleMesh.gameObject.SetActive(false);
            }
            else
            {
                OpenScripts2_BepInExPlugin.LogWarning(this, "No usable colliders for reticle occlusion found! If you are a modmaker, please add colliders or a lens collider, or disable occlusion culling with the checkbox!\n Disabling Occlusion culling now!");
                _disableOcclusionCulling = true;
            }
        }

        private void Zero(bool reset = false)
        {
            if (!reset)
            {
                _muzzlePos = FireArm.CurrentMuzzle;
                Vector3 worldPosition = _muzzlePos.position + _muzzlePos.forward * ZeroDistances[CurrentZeroDistance];
                ReticleMesh.transform.LookAt(worldPosition, ReticleMesh.transform.up);

                _materialPropertyBlock.SetFloat(NameOfDistanceVariable, ZeroDistances[CurrentZeroDistance]);
            }
            else
            {
                ReticleMesh.transform.localRotation = _originalReticleRotation;
                FireArm = null;
                _muzzlePos = null;
            }
        }
    }
}