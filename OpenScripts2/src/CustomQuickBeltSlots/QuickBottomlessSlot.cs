using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace OpenScripts2
{
    public class QuickBottomlessSlot : FVRQuickBeltSlot
    {
        [Header("QuickBottomlessSlot settings")]
        public int MaxItems = 30;
        public float TimeBetweenMagSwitch = 0.25f;
        public Color HoverColor;
        public bool StoresMagazines = true;
        public bool StoresClips = true;
        public bool StoresSpeedloaders = true;
        public bool OnlyStoresEmpty = false;

        public AudioEvent InsertSounds;
        public AudioEvent ExtractSounds;
        public AudioEvent FailureSounds;

        public GameObject TextRoot;
        public string TextPrefix = "Items: ";
        public Text NumberOfItemsDisplay;
        public bool TextTurnsOffOnNoItemsStored = true;

        [ContextMenu("CopyQBSlot")]
        public void CopyQBSlot()
        {
            FVRQuickBeltSlot QBS = GetComponent<FVRQuickBeltSlot>();

            this.QuickbeltRoot = QBS.QuickbeltRoot;
            this.PoseOverride = QBS.PoseOverride;
            this.SizeLimit = QBS.SizeLimit;
            this.Shape = QBS.Shape;
            this.Type = QBS.Type;
            this.HoverGeo = QBS.HoverGeo;
            this.RectBounds = QBS.RectBounds;
            this.CurObject = QBS.CurObject;
            this.IsSelectable = QBS.IsSelectable;
            this.IsPlayer = QBS.IsPlayer;
            this.UseStraightAxisAlignment = QBS.UseStraightAxisAlignment;
            this.HeldObject = QBS.HeldObject;
        }

        private GameObject _currentSelectedObject;

        private List<GameObject> _storedGameObjects;

        private bool _switchingObject = false;

        private int _selectedObjectIndex = 0;

        private float _timeWaited = 0f;

        public void Start()
        {
            _storedGameObjects = new List<GameObject>();

            Hook();
        }
        void Unhook()
        {
            On.FistVR.FVRQuickBeltSlot.Update -= FVRQuickBeltSlot_Update;
            On.FistVR.FVRQuickBeltSlot.MoveContents -= FVRQuickBeltSlot_MoveContents;
            On.FistVR.FVRQuickBeltSlot.MoveContentsInstant -= FVRQuickBeltSlot_MoveContentsInstant;
            On.FistVR.FVRQuickBeltSlot.MoveContentsCheap -= FVRQuickBeltSlot_MoveContentsCheap;
        }
        void Hook()
        {
            On.FistVR.FVRQuickBeltSlot.Update += FVRQuickBeltSlot_Update;
            On.FistVR.FVRQuickBeltSlot.MoveContents += FVRQuickBeltSlot_MoveContents;
            On.FistVR.FVRQuickBeltSlot.MoveContentsInstant += FVRQuickBeltSlot_MoveContentsInstant;
            On.FistVR.FVRQuickBeltSlot.MoveContentsCheap += FVRQuickBeltSlot_MoveContentsCheap;
        }

        private void FVRQuickBeltSlot_MoveContentsCheap(On.FistVR.FVRQuickBeltSlot.orig_MoveContentsCheap orig, FVRQuickBeltSlot self, Vector3 dir)
        {
            if (self == this)
            {
                if (this._currentSelectedObject != null)
                {
                    FVRPhysicalObject mag = this._currentSelectedObject.GetComponent<FVRPhysicalObject>();
                    if (mag.IsHeld)
                    {
                        return;
                    }
                    mag.RootRigidbody.position = mag.RootRigidbody.position + dir;
                    mag.RootRigidbody.velocity = Vector3.zero;
                }
            }
            else orig(self, dir);
        }

        private void FVRQuickBeltSlot_MoveContentsInstant(On.FistVR.FVRQuickBeltSlot.orig_MoveContentsInstant orig, FVRQuickBeltSlot self, Vector3 dir)
        {
            if (self == this)
            {
                if (this._currentSelectedObject != null)
                {
                    FVRPhysicalObject mag = this._currentSelectedObject.GetComponent<FVRPhysicalObject>();
                    if (mag.IsHeld)
                    {
                        return;
                    }
                    mag.transform.position = mag.transform.position + dir;
                    mag.RootRigidbody.velocity = Vector3.zero;
                }
            }
            else orig(self, dir);
        }

        private void FVRQuickBeltSlot_MoveContents(On.FistVR.FVRQuickBeltSlot.orig_MoveContents orig, FVRQuickBeltSlot self, Vector3 dir)
        {
            if (self == this)
            {
                if (this._currentSelectedObject != null)
                {
                    FVRPhysicalObject mag = this._currentSelectedObject.GetComponent<FVRPhysicalObject>();
                    if (mag.IsHeld)
                    {
                        return;
                    }
                    mag.transform.position = mag.transform.position + dir;
                    mag.RootRigidbody.velocity = Vector3.zero;
                }
            }
            else orig(self, dir);
        }

        private void FVRQuickBeltSlot_Update(On.FistVR.FVRQuickBeltSlot.orig_Update orig, FVRQuickBeltSlot self)
        {
            if (this == self)
            {
                if (!GM.CurrentSceneSettings.IsSpawnLockingEnabled && this.HeldObject != null && (this.HeldObject as FVRPhysicalObject).m_isSpawnLock)
                {
                    (this.HeldObject as FVRPhysicalObject).m_isSpawnLock = false;
                }
                if (this.HeldObject != null)
                {
                    if ((this.HeldObject as FVRPhysicalObject).m_isSpawnLock)
                    {
                        if (!this.HoverGeo.activeSelf)
                        {
                            this.HoverGeo.SetActive(true);
                        }
                        this.m_hoverGeoRend.material.SetColor("_RimColor", new Color(0.3f, 0.3f, 1f, 1f));
                    }
                    else if ((this.HeldObject as FVRPhysicalObject).m_isHardnessed)
                    {
                        if (!this.HoverGeo.activeSelf)
                        {
                            this.HoverGeo.SetActive(true);
                        }
                        this.m_hoverGeoRend.material.SetColor("_RimColor", new Color(0.3f, 1f, 0.3f, 1f));
                    }
                    else
                    {
                        if (this.HoverGeo.activeSelf != this.IsHovered)
                        {
                            this.HoverGeo.SetActive(this.IsHovered);
                        }
                        this.m_hoverGeoRend.material.SetColor("_RimColor", HoverColor);
                    }
                }
                else
                {
                    if (this.HoverGeo.activeSelf != this.IsHovered)
                    {
                        this.HoverGeo.SetActive(this.IsHovered);
                    }
                    this.m_hoverGeoRend.material.SetColor("_RimColor", HoverColor);
                }

                if (StoresMagazines && CurObject != null && CurObject is FVRFireArmMagazine && _storedGameObjects.Count < MaxItems)
                {
                    StoreCurObject();
                }
                else if (StoresClips && CurObject != null && CurObject is FVRFireArmClip && _storedGameObjects.Count < MaxItems)
                {
                    StoreCurObject();
                }
                else if (StoresSpeedloaders && CurObject != null && CurObject is Speedloader && _storedGameObjects.Count < MaxItems)
                {
                    StoreCurObject();
                }
                else if (CurObject != null)
                {
                    EjectCurObject();
                }

                if (_storedGameObjects.Count > 0)
                {
                    _timeWaited += Time.deltaTime;
                    if (!_switchingObject)
                    {
                        _timeWaited = 0f;
                        SelectObject();
                    }
                    else if (_timeWaited > TimeBetweenMagSwitch)
                    {
                        _switchingObject = false;
                    }

                    int removalIndex = -1;

                    for (int i = 0; i < _storedGameObjects.Count; i++)
                    {
                        _storedGameObjects[i].SetActive(i == _selectedObjectIndex);

                        if (i != _selectedObjectIndex)
                        {
                            if (QuickbeltRoot != null)
                            {
                                _storedGameObjects[i].transform.position = this.QuickbeltRoot.position;
                                _storedGameObjects[i].transform.rotation = this.QuickbeltRoot.rotation;
                            }
                            else if (PoseOverride != null)
                            {
                                _storedGameObjects[i].transform.position = this.PoseOverride.position;
                                _storedGameObjects[i].transform.rotation = this.PoseOverride.rotation;
                            }
                            else
                            {
                                _storedGameObjects[i].transform.position = this.transform.position;
                                _storedGameObjects[i].transform.rotation = this.transform.rotation;
                            }
                        }

                        FVRPhysicalObject objectComponent = _storedGameObjects[i].GetComponent<FVRPhysicalObject>();
                        if (objectComponent.IsHeld)
                        {
                            removalIndex = i;
                        }
                        if (objectComponent.m_isSpawnLock)
                        {
                            objectComponent.m_isSpawnLock = false;
                        }
                    }

                    if (removalIndex >= 0)
                    {
                        _storedGameObjects[removalIndex].SetActive(true);
                        _storedGameObjects.RemoveAt(removalIndex);
                        SM.PlayGenericSound(ExtractSounds, this.transform.position);
                        _switchingObject = false;
                    }

                    if (TextRoot != null && NumberOfItemsDisplay != null)
                    {
                        if (TextTurnsOffOnNoItemsStored) TextRoot.SetActive(true);
                        NumberOfItemsDisplay.text = TextPrefix + _storedGameObjects.Count.ToString();
                    }
                }
                else if (TextRoot != null && NumberOfItemsDisplay != null)
                {
                    if (TextTurnsOffOnNoItemsStored) TextRoot.SetActive(false);
                    NumberOfItemsDisplay.text = TextPrefix + _storedGameObjects.Count.ToString();
                }

            }
            else orig(self);
        }

        void StoreCurObject()
        {
            if (!_storedGameObjects.Contains(CurObject.gameObject))
            {
                if (OnlyStoresEmpty)
                {
                    if (!CheckEmpty()) return;
                }
                _storedGameObjects.Add(CurObject.gameObject);
                CurObject.gameObject.SetActive(false);

                CurObject = null;
                HeldObject = null;

                SM.PlayGenericSound(InsertSounds, this.transform.position);
            }
        }

        bool EjectCurObject()
        {
            CurObject.SetQuickBeltSlot(null);
            CurObject = null;
            HeldObject = null;
            SM.PlayGenericSound(FailureSounds, this.transform.position);
            return false;
        }

        bool CheckEmpty()
        {
            FVRFireArmMagazine mag = CurObject.gameObject.GetComponent<FVRFireArmMagazine>();
            FVRFireArmClip clip = CurObject.gameObject.GetComponent<FVRFireArmClip>();
            Speedloader speedloader = CurObject.gameObject.GetComponent<Speedloader>();

            if (mag != null && mag.m_numRounds > 0) return EjectCurObject();
            if (clip != null && clip.m_numRounds > 0) return EjectCurObject();
            if (speedloader != null)
            {
                bool notEmpty = false;
                foreach (var chamber in speedloader.Chambers)
                {
                    notEmpty = chamber.IsLoaded;
                }
                if (notEmpty) return EjectCurObject();
            }
            return true;
        }

        Vector3 GetPointInside()
        {
            switch (Shape)
            {
                case QuickbeltSlotShape.Sphere:
                    return UnityEngine.Random.insideUnitSphere * HoverGeo.transform.localScale.x;
                case QuickbeltSlotShape.Rectalinear:
                    return new Vector3(UnityEngine.Random.Range(-RectBounds.localScale.x, RectBounds.localScale.x), UnityEngine.Random.Range(-RectBounds.localScale.y, RectBounds.localScale.y), UnityEngine.Random.Range(-RectBounds.localScale.z, RectBounds.localScale.z)); ;
                default:
                    return new Vector3();
            }
        }
        void SelectObject()
        {
            _switchingObject = true;
            _selectedObjectIndex = UnityEngine.Random.Range(0, _storedGameObjects.Count);
            _currentSelectedObject = _storedGameObjects[_selectedObjectIndex];
            _currentSelectedObject.SetActive(true);
        }
    }
}
