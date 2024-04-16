using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    public class CustomLinearZoomScopeInterface : FVRFireArmAttachmentInterface
    {
        [Header("Custom Linear Zoom Scope Config")]
        public MeshRenderer ScopeLens;
        public Camera ScopeCamera;
        
        [Header("Zoom Settings")]
        public float MinZoom = 1f;
        public float MaxZoom = 6f;

        [Tooltip("Between 0f and 1f. 0f is MinZoom while 1f is MaxZoom.")]
        public float ZoomLerp = 0f;
        //public List<float> ZoomFactor;
        [Tooltip("If true, Reticle will scale with increasing zoom level.")]
        public bool ZoomIncreasesReticleMagnification = false;

        [Header("Text Screen Settings")]
        public GameObject TextScreenRoot;
        public Text ZoomTextField;
        public Text ZeroTextField;
        public Text ElevationTextField;
        public Text WindageTextField;
        [Tooltip("The existence of this text enables the reticle change functionality.")]
        public Text ReticleText;

        public GameObject TextFrame;

        public string ZoomPrefix = "Zoom: ";
        public string ZeroPrefix = "Zero Distance: ";
        public string ElevationPrefix = "Elevation: ";
        public string WindagePrefix = "Windage: ";
        public string ReticlePrefix = "Reticle: ";

        [Header("Zeroing System Settings")]
        public int ZeroDistanceIndex = 0;
        public List<float> ZeroDistances = new List<float>()
        {
          100f,
          150f,
          200f,
          250f,
          300f,
          350f,
          400f,
          450f,
          500f,
          600f,
          700f,
          800f,
          900f,
          1000f
        };
        public float ElevationIncreasePerClick = 0.25f;
        public float WindageIncreasePerClick = 0.25f;

        [Header("Reticle Change System Settings")]
        [Tooltip("All reticle textures. Default reticle is first entry.")]
        public List<Texture2D> Reticles;
        [Tooltip("Colors of all reticles. Default reticle name is first entry.")]
        [ColorUsage(true, true, float.MaxValue, float.MaxValue, 0f, 0f)]
        public List<Color> ReticleColors;
        [Tooltip("Names of all reticles. Default reticle name is first entry.")]
        public string[] ReticleNames;

        public int CurrentReticle = 0;

        [Header("Rotating Scope Bit Monitoring Settings")]
        [Tooltip("Will Monitor this part and adjust the Zoom Lerp accordingly.")]
        public Transform RotatingBit;

        public OpenScripts2_BasePlugin.Axis Axis;

        public float MinRotation;
        public float MaxRotation;

        [Header("Integrated Scope Settings")]
        public bool IsIntegrated = false;
        public FVRFireArm FireArm = null;

        [Header("Optimization Setting. Set to false when done testing for vanilla scope like behavior of showing a black picture when not attached to gun.")]
        public bool ActiveWithoutMount = true;

        private bool _hasZoomText;
        private RenderTexture _renderTexture;

        private FVRFireArmAttachment _attachment;

        private float _elevationStep;
        private float _windageStep;

        private int _currentMenu;
        private bool _initialZero = false;

        private float _baseReticleSize;

        private float _zoomFactor;
        private float _lastZoomLerp;

        private Material _scopeLensMaterial;

        public override void Start()
        {
            base.Start();
            _scopeLensMaterial = ScopeLens.material;

            if (TextScreenRoot != null) _hasZoomText = true;
            else _hasZoomText = false;


            _zoomFactor = Mathf.Lerp(MinZoom, MaxZoom, ZoomLerp);

            _renderTexture = ScopeCamera.targetTexture;
            _renderTexture = Instantiate(_renderTexture);
            ScopeCamera.targetTexture = _renderTexture;
            _scopeLensMaterial.mainTexture = _renderTexture;

            if (ZoomIncreasesReticleMagnification)
            {
                _baseReticleSize = _scopeLensMaterial.GetFloat("_ReticleScale");
            }

            SetZoom();

            if (!IsIntegrated)
            {
                _attachment = Attachment;
            }
            else if (!IsIntegrated)
            {
                _attachment = gameObject.GetComponent<FVRFireArmAttachment>();
            }

            if (!IsIntegrated && _attachment == null) OpenScripts2_BepInExPlugin.LogWarning(this,"Attachment not found. Scope zeroing disabled!");

            UpdateMenu();

            ScopeEnabled(ActiveWithoutMount);

            if (!_initialZero && IsIntegrated)
            {
                Zero();
                _initialZero = true;
            }

            if (ZoomTextField == null) 
            { 
                _currentMenu++;
                if (ZeroTextField == null)
                {
                    _currentMenu++;
                    if (ElevationTextField == null)
                    {
                        _currentMenu++;
                        if (WindageTextField == null)
                        {
                            _currentMenu = 0;
                            _hasZoomText = false;
                        }
                    }
                }
            }

            if ((ReticleText != null))
            {
                if (CurrentReticle >= Reticles.Count) CurrentReticle = Reticles.Count - 1;
                ChangeReticle();
            }

            if (RotatingBit != null) SetZoomLerp();
            _lastZoomLerp = ZoomLerp;
        }
        public override void OnDestroy()
        {
            Destroy(_renderTexture);
            Destroy(_scopeLensMaterial);
            base.OnDestroy();
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            if (_hasZoomText && hand != null)
            {
                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f) NextMenu();
                else if (_currentMenu == 0 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) PreviousZeroRange();
                else if (_currentMenu == 0 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) NextZeroRange();
                else if (_currentMenu == 1 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) DecreaseElevationAdjustment();
                else if (_currentMenu == 1 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) IncreaseElevationAdjustment();
                else if (_currentMenu == 2 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) DecreaseWindageAdjustment();
                else if (_currentMenu == 2 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) IncreaseWindageAdjustment();
                else if (_currentMenu == 3 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) PreviousReticleTexture();
                else if (_currentMenu == 3 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) NextReticleTexture();

                TextScreenRoot.gameObject.SetActive(true);
            }
            else if (_hasZoomText) TextScreenRoot.gameObject.SetActive(false);

            if (!ActiveWithoutMount)
            {
                if (_attachment != null && _attachment.curMount != null) ScopeEnabled(true);
                else if (_attachment != null && _attachment.curMount == null) ScopeEnabled(false);
                else ScopeEnabled(true);
            }
        }
        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (RotatingBit != null) SetZoomLerp();
            if (_lastZoomLerp != ZoomLerp)
            {
                SetZoom();
                _lastZoomLerp = ZoomLerp;
            }
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (!_initialZero && _attachment != null)
            {
                Zero();
                _initialZero = true;
            }

            ScopeEnabled(true);
        }
        public override void OnDetach()
        {
            base.OnDetach();

            if (_initialZero && _attachment != null)
            {
                Zero();
                _initialZero = false;
            }

            ScopeEnabled(false);
        }

        public void SetZoomLerp()
        {
            Vector3 currentRotatingBitEulers = RotatingBit.localEulerAngles;
            float currentRotatingBitRot = currentRotatingBitEulers[(int)Axis];

            ZoomLerp = Mathf.InverseLerp(MinRotation, MaxRotation, currentRotatingBitRot);
        }

        public void SetZoom()
        {
            _zoomFactor = Mathf.Lerp(MinZoom, MaxZoom, ZoomLerp);

            float cameraFOV = 53.6f * Mathf.Pow(_zoomFactor, -0.9364f) - 0.3666f;

            ScopeCamera.fieldOfView = cameraFOV;

            if (ZoomIncreasesReticleMagnification)
            {
                _scopeLensMaterial.SetFloat("_ReticleScale", _baseReticleSize * _zoomFactor/MinZoom);
            }
        }

        public void NextZeroRange()
        {
            if (ZeroDistanceIndex >= ZeroDistances.Count - 1) return;
            ZeroDistanceIndex++;
            Zero();
            UpdateMenu();
        }

        public void PreviousZeroRange()
        {
            if (ZeroDistanceIndex <= 0) return;
            ZeroDistanceIndex--;
            Zero();
            UpdateMenu();
        }

        public void IncreaseElevationAdjustment()
        {
            _elevationStep += ElevationIncreasePerClick;
            Zero();
            UpdateMenu();
        }
        public void DecreaseElevationAdjustment()
        {
            _elevationStep -= ElevationIncreasePerClick;
            Zero();
            UpdateMenu();
        }

        public void IncreaseWindageAdjustment()
        {
            _windageStep += WindageIncreasePerClick;
            Zero();
            UpdateMenu();
        }
        public void DecreaseWindageAdjustment()
        {
            _windageStep -= WindageIncreasePerClick;
            Zero();
            UpdateMenu();
        }

        public void NextReticleTexture()
        {
            CurrentReticle++;
            if (CurrentReticle >= Reticles.Count) CurrentReticle = 0;
            ChangeReticle();
            UpdateMenu();
        }

        public void PreviousReticleTexture()
        {
            CurrentReticle--;
            if (CurrentReticle <= 0) CurrentReticle = Reticles.Count - 1;
            ChangeReticle();
            UpdateMenu();
        }
        public void NextMenu()
        {
            if (ZoomTextField == null && ZeroTextField == null && ElevationTextField == null && WindageTextField == null)
                return;
            _currentMenu++;

            if (_currentMenu >= 4) _currentMenu = 0;

            switch (_currentMenu)
            {
                case 0:
                    if (ZeroTextField == null)
                    {
                        NextMenu();
                        return;
                    }
                    break;
                case 1:
                    if (ElevationTextField == null)
                    {
                        NextMenu();
                        return;
                    }
                    break;
                case 2:
                    if (WindageTextField == null)
                    {
                        NextMenu();
                        return;
                    }
                    break;
                case 3:
                    if (ReticleText == null)
                    {
                        NextMenu();
                        return;
                    }
                    break;
            }

            UpdateMenu();
        }

        public void UpdateMenu()
        {
            if (TextFrame != null)
                switch (_currentMenu)
                {
                    case 0:
                        if (ZoomTextField == null) break;
                        TextFrame.transform.position = ZoomTextField.transform.position;
                        break;
                    case 1:
                        if (ZeroTextField == null) break;
                        TextFrame.transform.position = ZeroTextField.transform.position;
                        break;
                    case 2:
                        if (ElevationTextField == null) break;
                        TextFrame.transform.position = ElevationTextField.transform.position;
                        break;
                    case 3:
                        if (WindageTextField == null) break;
                        TextFrame.transform.position = WindageTextField.transform.position;
                        break;
                    case 4:
                        if (ReticleText == null) break;
                        TextFrame.transform.position = ReticleText.transform.position;
                        break;
                }

            if (ZoomTextField != null) ZoomTextField.text = ZoomPrefix + _zoomFactor + "x";
            if (ZeroTextField != null) ZeroTextField.text = ZeroPrefix + ZeroDistances[ZeroDistanceIndex] + "m";
            if (ElevationTextField != null) ElevationTextField.text = ElevationPrefix + _elevationStep + " MOA";
            if (WindageTextField != null) WindageTextField.text = WindagePrefix + _windageStep + " MOA";
            if (ReticleText != null) ReticleText.text = ReticlePrefix + ReticleNames[CurrentReticle];
        }

        public void Zero()
        {
            if (IsIntegrated || (_attachment != null && _attachment.curMount != null && _attachment.curMount.Parent != null && _attachment.curMount.Parent is FVRFireArm))
            {
                if (!IsIntegrated) FireArm = _attachment.curMount.Parent as FVRFireArm;

                if (IsIntegrated && FireArm == null)
                {
                    OpenScripts2_BepInExPlugin.LogError(this, "ScopeShaderZoom: FireArm not found and scope not set to integrated scope! Can't zero sight!");
                    return;
                }

                FireArmRoundType roundType = FireArm.RoundType;
                float zeroDistance = ZeroDistances[ZeroDistanceIndex];
                float num = 0.0f;
                if (AM.SRoundDisplayDataDic.ContainsKey(roundType))
                    num = AM.SRoundDisplayDataDic[roundType].BulletDropCurve.Evaluate(zeroDistance * (1f / 1000f));
                Vector3 p = FireArm.MuzzlePos.position + FireArm.GetMuzzle().forward * zeroDistance + FireArm.GetMuzzle().up * num;

                //Vector3 vector3_1 = Vector3.ProjectOnPlane(p - transform.forward, transform.right);
                //Vector3 vector3_2 = Quaternion.AngleAxis(_elevationStep /60f, transform.right) * vector3_1;
                //Vector3 forward = Quaternion.AngleAxis(_windageStep / 60f, transform.up) * vector3_2;
                //Vector3 projected_p = Vector3.ProjectOnPlane(p, transform.right) + Vector3.Dot(transform.position, transform.right) * transform.right;

                Vector3 projected_p = Vector3Utils.ProjectOnPlaneThroughPoint(p, transform.position, transform.right);

                ScopeCamera.transform.LookAt(projected_p, transform.up);
                ScopeCamera.transform.localEulerAngles += new Vector3(-_elevationStep / 60f, _windageStep / 60f, 0);
            }
            else ScopeCamera.transform.localRotation = Quaternion.identity;
        }

        public void ScopeEnabled(bool state)
        {
            if (state)
            {
                ScopeCamera.gameObject.SetActive(true);
            }
            else
            {
                ScopeCamera.gameObject.SetActive(false);
                RenderTexture.active = _renderTexture;
                GL.Clear(false, true, Color.black);
                RenderTexture.active = null;
            }
        }

        private void ChangeReticle()
        {
            _scopeLensMaterial.SetColor("_ReticleColor", ReticleColors[CurrentReticle]);
            _scopeLensMaterial.SetTexture("_ReticleTex", Reticles[CurrentReticle]);
        }
    }
}