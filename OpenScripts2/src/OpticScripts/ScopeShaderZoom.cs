using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    public class ScopeShaderZoom : OpenScripts2_BasePlugin
    {
        public FVRInteractiveObject AttachmentInterface;
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

        public Axis RotationalAxis;

        [Tooltip("Needs to be same length as zoom levels or it will break!")]
        public float[] RotationAngles;

        [Header("Integrated Scope Settings")]
        public bool IsIntegrated = false;
        public FVRFireArm FireArm = null;
        [Header("Reticle Change Settings")]
        [Tooltip("Additional reticles. Default reticle is first entry.")]
        public List<Texture2D> ReticleTextures;
        [Tooltip("Names of additional reticles. Default reticle name is first entry.")]
        public string[] ReticleNames;
        [Tooltip("Additional reticle colors. Default reticle color is first entry.")]
        public List<Color> ReticleColors;

        public int CurrentSelectedReticleIndex = 0;

        [Tooltip("This enables the very specialized reticle change system.")]
        public bool DoesEachZoomFactorHaveOwnReticle = false;
        [Tooltip("Starts with default reticle, than all default reticle variants for the following zoom levels. Next entries are additional reticles and their according zoom levels, all ordered by zoom level and grouped by reticle type.")]
        public List<Texture2D> AdditionalReticlesPerZoomLevel;

        private List<float> _correspondingCameraFOV;

        private bool _hasZoomText;
        private RenderTexture _renderTexture;

        private FVRFireArmAttachment _attachment;

        private float _elevationStep;
        private float _windageStep;

        private int _currentMenu;
        private bool _initialZero = false;

        private float _baseReticleSize;

        public void Start()
        {
            _correspondingCameraFOV = new List<float>();
            if (TextScreenRoot != null) _hasZoomText = true;
            else _hasZoomText = false;

            for (int i = 0; i < ZoomFactor.Count; i++)
            {
                //CorrespondingCameraFOV.Add(53.7f * Mathf.Pow(ZoomFactor[i], -0.9284f) - 0.5035f);
                //CorrespondingCameraFOV.Add(54.3f * Mathf.Pow(ZoomFactor[i], -0.9613f) - 0.1378f);
                float zoomValue = 53.6f * Mathf.Pow(ZoomFactor[i], -0.9364f) - 0.3666f;
                _correspondingCameraFOV.Add(zoomValue);
            }


            _renderTexture = ScopeCamera.targetTexture;
            _renderTexture = RenderTexture.Instantiate(_renderTexture);
            ScopeCamera.targetTexture = _renderTexture;
            ScopeLens.material.mainTexture = _renderTexture;

            if (ZoomIncreasesReticleMagnification)
            {
                _baseReticleSize = ScopeLens.material.GetFloat("_ReticleScale");
            }

            SetZoom();

            FVRFireArmAttachmentInterface attachmentInterface = AttachmentInterface as FVRFireArmAttachmentInterface;

            if (!IsIntegrated && attachmentInterface != null)
            {
                _attachment = attachmentInterface.Attachment;
            }
            else if (!IsIntegrated)
            {
                _attachment = this.gameObject.GetComponent<FVRFireArmAttachment>();
            }

            if (!IsIntegrated && _attachment == null) Debug.LogWarning("Attachment not found. Scope zeroing disabled!");

            UpdateMenu();

            ScopeEnabled(ActiveWithoutMount);

            //camera.gameObject.SetActive(activeWithoutMount);

            if (IsIntegrated) Zero();

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
                /*
                if (reticles.Count != reticleName.Length)
                {
                    reticles.Insert(0,scopeLens.material.GetTexture("_ReticleTex") as Texture2D);
                }
                if (reticleColors.Count != reticleName.Length)
                {
                    reticleColors.Insert(0, scopeLens.material.GetColor("_ReticleColor"));
                }

                if (doesEachZoomFactorHaveOwnReticle)
                {
                    for (int i = 0; i < reticles.Count; i++)
                    {
                        if (additionalReticlesPerZoomLevel[ZoomFactor.Count * i] != reticles[i])
                        {
                            additionalReticlesPerZoomLevel.Insert(ZoomFactor.Count * i, reticles[i]);
                        }
                    }
                }
                */
                if (CurrentSelectedReticleIndex >= ReticleTextures.Count) CurrentSelectedReticleIndex = ReticleTextures.Count - 1;
                ChangeReticle();
            }
        }
        public void OnDestroy()
        {
            Destroy(_renderTexture);
        }
#if !DEBUG
        public void Update()
        {
            FVRViveHand hand = AttachmentInterface.m_hand;
            if (_hasZoomText && hand != null)
            {
                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f) NextMenu();
                else if (_currentMenu == 0 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) PreviousZoom();
                else if (_currentMenu == 0 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) NextZoom();
                else if (_currentMenu == 1 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) PreviousZero();
                else if (_currentMenu == 1 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) NextZero();
                else if (_currentMenu == 2 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) DecreaseElevationAdjustment();
                else if (_currentMenu == 2 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) IncreaseElevationAdjustment();
                else if (_currentMenu == 3 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) DecreaseWindageAdjustment();
                else if (_currentMenu == 3 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) IncreaseWindageAdjustment();
                else if (_currentMenu == 4 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) PreviousReticle();
                else if (_currentMenu == 4 && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) NextReticle();

                TextScreenRoot.gameObject.SetActive(true);
            }
            else if (_hasZoomText) TextScreenRoot.gameObject.SetActive(false);

            if (!ActiveWithoutMount)
            {
                if (_attachment != null && _attachment.curMount != null) ScopeEnabled(true);
                else if (_attachment != null && _attachment.curMount == null) ScopeEnabled(false);
                else ScopeEnabled(true);
            }

            if (!_initialZero && _attachment != null && _attachment.curMount != null)
            {
                Zero();
                _initialZero = true;
            }
            else if (_initialZero && _attachment != null && _attachment.curMount == null)
            {
                Zero();
                _initialZero = false;
            }
            else if (!_initialZero && IsIntegrated)
            {
                Zero();
                _initialZero = true;
            }
        }
#endif
        public void NextZoom()
        {
            if (CurrentZoomIndex >= ZoomFactor.Count - 1) return;
            CurrentZoomIndex++;
            SetZoom();
            if (DoesEachZoomFactorHaveOwnReticle) ChangeReticle();
            UpdateMenu();
        }

        public void PreviousZoom()
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
                ScopeLens.material.SetFloat("_ReticleScale", _baseReticleSize * ZoomFactor[CurrentZoomIndex]/ZoomFactor[0]);
            }

            if (HasRotatingBit)
            {
                Vector3 origRot = RotatingBit.localEulerAngles;

                switch (RotationalAxis)
                {
                    case Axis.X:
                        RotatingBit.localEulerAngles = new Vector3(RotationAngles[CurrentZoomIndex], origRot.y, origRot.z);
                        break;
                    case Axis.Y:
                        RotatingBit.localEulerAngles = new Vector3(origRot.x, RotationAngles[CurrentZoomIndex], origRot.z);
                        break;
                    case Axis.Z:
                        RotatingBit.localEulerAngles = new Vector3(origRot.x, origRot.y, RotationAngles[CurrentZoomIndex]);
                        break;
                    default:
                        break;
                }
            }
        }

        public void NextZero()
        {
            if (ZeroDistanceIndex >= ZeroDistances.Count - 1) return;
            ZeroDistanceIndex++;
            Zero();
            UpdateMenu();
        }

        public void PreviousZero()
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

        public void NextReticle()
        {
            CurrentSelectedReticleIndex++;
            if (CurrentSelectedReticleIndex >= ReticleTextures.Count) CurrentSelectedReticleIndex = 0;
            ChangeReticle();
            UpdateMenu();
        }

        public void PreviousReticle()
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
                default:
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
                    default:
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
#if!Debug
            if (IsIntegrated || (this._attachment != null && this._attachment.curMount != null && this._attachment.curMount.Parent != null && this._attachment.curMount.Parent is FVRFireArm))
            {
                if (!IsIntegrated) FireArm = this._attachment.curMount.Parent as FVRFireArm;

                if (IsIntegrated && FireArm == null) Debug.LogError("ScopeShaderZoom: FireArm not set on integrated Scope! Can't zero sight!");

                FireArmRoundType roundType = FireArm.RoundType;
                float zeroDistance = this.ZeroDistances[this.ZeroDistanceIndex];
                float num = 0.0f;
                if (AM.SRoundDisplayDataDic.ContainsKey(roundType))
                    num = AM.SRoundDisplayDataDic[roundType].BulletDropCurve.Evaluate(zeroDistance * (1f / 1000f));
                Vector3 p = FireArm.MuzzlePos.position + FireArm.GetMuzzle().forward * zeroDistance + FireArm.GetMuzzle().up * num;
                Vector3 vector3_1 = Vector3.ProjectOnPlane(p - this.transform.forward, this.transform.right);
                Vector3 vector3_2 = Quaternion.AngleAxis(this._elevationStep /60f, this.transform.right) * vector3_1;
                Vector3 forward = Quaternion.AngleAxis(this._windageStep / 60f, this.transform.up) * vector3_2;

                //Vector3 projected_p = Vector3.ProjectOnPlane(p, this.transform.right) + Vector3.Dot(this.transform.position, this.transform.right) * this.transform.right;
                Vector3 projected_p = Vector3Utils.ProjectOnPlaneThroughPoint(p, this.transform.position, this.transform.right);
                //this.TargetAimer.rotation = Quaternion.LookRotation(forward, this.transform.up);
                //this.camera.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward - camera.transform.position, camera.transform.right), camera.transform.up);// PointTowards(p);
                this.ScopeCamera.transform.LookAt(projected_p, this.transform.up);
                this.ScopeCamera.transform.localEulerAngles += new Vector3(-this._elevationStep / 60f, this._windageStep / 60f, 0);
                //this.camera.transform.Rotate(new Vector3(-this.ElevationStep / 60f, this.WindageStep / 60f, 0));

                //this.camera.transform.LookAt(forward);

                //this.ScopeCam.ScopeCamera.transform.rotation = Quaternion.LookRotation(forward, this.transform.up);
            }
            else this.ScopeCamera.transform.localRotation = Quaternion.identity;
#endif
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
                RenderTexture.active = (RenderTexture) null;
            }
        }

        private void ChangeReticle()
        {
            if (!DoesEachZoomFactorHaveOwnReticle)
            {
                ScopeLens.material.SetColor("_ReticleColor", ReticleColors[CurrentSelectedReticleIndex]);
                ScopeLens.material.SetTexture("_ReticleTex", ReticleTextures[CurrentSelectedReticleIndex]);
            }
            else
            {
                ScopeLens.material.SetColor("_ReticleColor", ReticleColors[CurrentSelectedReticleIndex]);
                ScopeLens.material.SetTexture("_ReticleTex", AdditionalReticlesPerZoomLevel[CurrentZoomIndex + CurrentSelectedReticleIndex * ZoomFactor.Count]);
            }
        }
    }
}

