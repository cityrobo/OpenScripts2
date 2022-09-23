using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    public class CustomScopeInterface : FVRFireArmAttachmentInterface
    {
        [Header("Custom Scope Config")]
        public MeshRenderer ScopeLens;
        public Camera ScopeCamera;
        public int CurrentZoomIndex = 0;
        public List<float> ZoomFactor;

        public bool ZoomIncreasesReticleMagnification = false;

        [Header("If you want a Screen above the scope that shows the current Settings, use these:")]
        public GameObject TextScreenRoot;
        public Text ZoomTextField;
        public Text ZeroTextField;
        public Text ElevationTextField;
        public Text WindageTextField;
        [Tooltip("The existence of this text enables the reticle change functionality")]
        public Text ReticleTextField;

        public GameObject TextFrame;

        public string ZoomPrefix = "Zoom: ";
        public string ZeroPrefix = "Zero Distance: ";
        public string ElevationPrefix = "Elevation: ";
        public string WindagePrefix = "Windage: ";
        public string ReticlePrefix = "Reticle: ";

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
        [Header("Optimization Setting. Set to false when done testing for vanilla scope like behavior of showing a black picture when not attached to gun.")]
        public bool ActiveWithoutMount = true;

        [Header("Rotating Scope Bit")]
        public bool HasRotatingBit = false;
        public Transform RotatingBit;

        public OpenScripts2_BasePlugin.Axis RotationalAxis;

        [Tooltip("Needs to be same length as zoom levels or it will break!")]
        public float[] RotationAngles;

        [Header("Integrated Scope Settings")]
        public bool IsIntegrated = false;
        public FVRFireArm FireArm = null;
        [Header("Reticle Change Settings")]
        [Tooltip("All reticle textures. Default reticle is first entry.")]
        public List<Texture2D> ReticleTextures;
        [Tooltip("Colors of all reticles. Default reticle name is first entry.")]
        [ColorUsage(true, true, float.MaxValue, float.MaxValue, 0f, 0f)]
        public List<Color> ReticleColors;
        [Tooltip("Names of all reticles. Default reticle name is first entry.")]
        public string[] ReticleNames;

        public int CurrentSelectedReticleIndex = 0;

        [Tooltip("This enables the very specialized reticle change system.")]
        public bool DoesEachZoomFactorHaveOwnReticle = false;
        [Tooltip("Starts with default reticle, than all default reticle variants for the following zoom levels. Next entries are additional reticles and their according zoom levels, all ordered by zoom level and grouped by reticle type.")]
        public List<Texture2D> ReticlesPerZoomLevel;

        private List<float> _correspondingCameraFOV;

        private bool _hasZoomText;
        private RenderTexture _renderTexture;

        private FVRFireArmAttachment _attachment;

        private float _elevationStep;
        private float _windageStep;

        private int _currentMenu;
        private bool _initialZero = false;

        private float _baseReticleSize;

        private Material _scopeLensMaterial;

        public override void Start()
        {
            base.Start();
            _correspondingCameraFOV = new List<float>();
            _scopeLensMaterial = ScopeLens.material;

            if (TextScreenRoot != null) _hasZoomText = true;
            else _hasZoomText = false;

            for (int i = 0; i < ZoomFactor.Count; i++)
            {
                float zoomValue = 53.6f * Mathf.Pow(ZoomFactor[i], -0.9364f) - 0.3666f;
                _correspondingCameraFOV.Add(zoomValue);
            }

            _renderTexture = Instantiate(ScopeCamera.targetTexture);
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

            if ((ReticleTextField != null || DoesEachZoomFactorHaveOwnReticle))
            {
                if (CurrentSelectedReticleIndex >= ReticleTextures.Count) CurrentSelectedReticleIndex = ReticleTextures.Count - 1;
                ChangeReticle();
            }
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
                TextScreenRoot.gameObject.SetActive(true);

                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f) NextMenu();
                else if (_currentMenu == 0 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) PreviousZoomLevel();
                else if (_currentMenu == 0 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) NextZoomLevel();
                else if (_currentMenu == 1 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) PreviousZeroRange();
                else if (_currentMenu == 1 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) NextZeroRange();
                else if (_currentMenu == 2 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) DecreaseElevationAdjustment();
                else if (_currentMenu == 2 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) IncreaseElevationAdjustment();
                else if (_currentMenu == 3 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) DecreaseWindageAdjustment();
                else if (_currentMenu == 3 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) IncreaseWindageAdjustment();
                else if (_currentMenu == 4 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) PreviousReticleTexture();
                else if (_currentMenu == 4 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) NextReticleTexture();
            }
            else if (_hasZoomText) TextScreenRoot.gameObject.SetActive(false);
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

        public void NextZoomLevel()
        {
            if (CurrentZoomIndex >= ZoomFactor.Count - 1) return;
            CurrentZoomIndex++;
            SetZoom();
            if (DoesEachZoomFactorHaveOwnReticle) ChangeReticle();
            UpdateMenu();
        }

        public void PreviousZoomLevel()
        {
            if (CurrentZoomIndex <= 0) return;
            CurrentZoomIndex--;
            SetZoom();
            if (DoesEachZoomFactorHaveOwnReticle) ChangeReticle();
            UpdateMenu();
        }

        public void SetZoom()
        {
            ScopeCamera.fieldOfView = _correspondingCameraFOV[CurrentZoomIndex];

            if (ZoomIncreasesReticleMagnification)
            {
                _scopeLensMaterial.SetFloat("_ReticleScale", _baseReticleSize * ZoomFactor[CurrentZoomIndex]/ZoomFactor[0]);
            }

            if (HasRotatingBit)
            {
                Vector3 origRot = RotatingBit.localEulerAngles;

                switch (RotationalAxis)
                {
                    case OpenScripts2_BasePlugin.Axis.X:
                        RotatingBit.localEulerAngles = new Vector3(RotationAngles[CurrentZoomIndex], origRot.y, origRot.z);
                        break;
                    case OpenScripts2_BasePlugin.Axis.Y:
                        RotatingBit.localEulerAngles = new Vector3(origRot.x, RotationAngles[CurrentZoomIndex], origRot.z);
                        break;
                    case OpenScripts2_BasePlugin.Axis.Z:
                        RotatingBit.localEulerAngles = new Vector3(origRot.x, origRot.y, RotationAngles[CurrentZoomIndex]);
                        break;
                }
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
            CurrentSelectedReticleIndex++;
            if (CurrentSelectedReticleIndex >= ReticleTextures.Count) CurrentSelectedReticleIndex = 0;
            ChangeReticle();
            UpdateMenu();
        }

        public void PreviousReticleTexture()
        {
            CurrentSelectedReticleIndex--;
            if (CurrentSelectedReticleIndex <= 0) CurrentSelectedReticleIndex = ReticleTextures.Count - 1;
            ChangeReticle();
            UpdateMenu();
        }
        public void NextMenu()
        {
            if (ZoomTextField == null && ZeroTextField == null && ElevationTextField == null && WindageTextField == null)
                return;
            _currentMenu++;

            if (_currentMenu >= 5) _currentMenu = 0;

            switch (_currentMenu)
            {
                case 0:
                    if (ZoomTextField == null)
                    {
                        NextMenu();
                        return;
                    }
                    break;
                case 1:
                    if (ZeroTextField == null)
                    {
                        NextMenu();
                        return;
                    }
                    break;
                case 2:
                    if (ElevationTextField == null)
                    {
                        NextMenu();
                        return;
                    }
                    break;
                case 3:
                    if (WindageTextField == null)
                    {
                        NextMenu();
                        return;
                    }
                    break;
                case 4:
                    if (ReticleTextField == null)
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
                        if (ReticleTextField == null) break;
                        TextFrame.transform.position = ReticleTextField.transform.position;
                        break;
                }

            if (ZoomTextField != null) ZoomTextField.text = ZoomPrefix + ZoomFactor[CurrentZoomIndex] + "x";
            if (ZeroTextField != null) ZeroTextField.text = ZeroPrefix + ZeroDistances[ZeroDistanceIndex] + "m";
            if (ElevationTextField != null) ElevationTextField.text = ElevationPrefix + _elevationStep + " MOA";
            if (WindageTextField != null) WindageTextField.text = WindagePrefix + _windageStep + " MOA";
            if (ReticleTextField != null) ReticleTextField.text = ReticlePrefix + ReticleNames[CurrentSelectedReticleIndex];
        }
        public void Zero()
        {
            if (IsIntegrated || (_attachment != null && _attachment.curMount != null && _attachment.curMount.Parent != null && _attachment.curMount.Parent is FVRFireArm))
            {
                if (!IsIntegrated) FireArm = _attachment.curMount.Parent as FVRFireArm;

                if (IsIntegrated && FireArm == null) OpenScripts2_BepInExPlugin.LogError(this,"ScopeShaderZoom: FireArm not set on integrated Scope! Can't zero sight!");

                FireArmRoundType roundType = FireArm.RoundType;
                float zeroDistance = ZeroDistances[ZeroDistanceIndex];
                float num = 0.0f;
                if (AM.SRoundDisplayDataDic.ContainsKey(roundType))
                {
                    num = AM.SRoundDisplayDataDic[roundType].BulletDropCurve.Evaluate(zeroDistance * (1f / 1000f));
                }
                Vector3 p = FireArm.MuzzlePos.position + FireArm.GetMuzzle().forward * zeroDistance + FireArm.GetMuzzle().up * num;

                //Vector3 vector3_1 = Vector3.ProjectOnPlane(p - transform.forward, transform.right);
                //Vector3 vector3_2 = Quaternion.AngleAxis(_elevationStep /60f, transform.right) * vector3_1;
                //Vector3 forward = Quaternion.AngleAxis(_windageStep / 60f, transform.up) * vector3_2;
                //Vector3 projected_p = Vector3.ProjectOnPlane(p, this.transform.right) + Vector3.Dot(this.transform.position, this.transform.right) * this.transform.right;
                Vector3 projected_p = Vector3Utils.ProjectOnPlaneThroughPoint(p, transform.position, transform.right);

                ScopeCamera.transform.LookAt(projected_p, transform.up);
                ScopeCamera.transform.localEulerAngles += new Vector3(-_elevationStep / 60f, _windageStep / 60f, 0);

                //this.camera.transform.Rotate(new Vector3(-this.ElevationStep / 60f, this.WindageStep / 60f, 0));

                //this.camera.transform.LookAt(forward);

                //this.ScopeCam.ScopeCamera.transform.rotation = Quaternion.LookRotation(forward, this.transform.up);
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
            if (!DoesEachZoomFactorHaveOwnReticle)
            {
                _scopeLensMaterial.SetColor("_ReticleColor", ReticleColors[CurrentSelectedReticleIndex]);
                _scopeLensMaterial.SetTexture("_ReticleTex", ReticleTextures[CurrentSelectedReticleIndex]);
            }
            else
            {
                _scopeLensMaterial.SetColor("_ReticleColor", ReticleColors[CurrentSelectedReticleIndex]);
                _scopeLensMaterial.SetTexture("_ReticleTex", ReticlesPerZoomLevel[CurrentZoomIndex + CurrentSelectedReticleIndex * ZoomFactor.Count]);
            }
        }
    }
}